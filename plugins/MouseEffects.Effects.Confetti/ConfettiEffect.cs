using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using MouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.Confetti;

public sealed class ConfettiEffect : EffectBase
{
    private struct ConfettiParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public float Rotation;
        public float RotationSpeed;
        public int ShapeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ParticleGPU
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public float Rotation;
        public int ShapeType;
        public float Padding1;
        public float Padding2;
        public float Padding3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float GravityStrength;
        public float FlutterAmount;
        public float HdrMultiplier;
        public float Padding1;
        public float Padding2;
        public Vector4 Padding3;
        public Vector4 Padding4;
    }

    private const int MaxParticlesLimit = 10000;
    private const int ShapeRectangle = 0;
    private const int ShapeCircle = 1;
    private const int ShapeRibbon = 2;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "confetti",
        Name = "Confetti",
        Description = "Colorful confetti particles bursting from clicks or following the cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    private IBuffer? _particleBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly ConfettiParticle[] _particles = new ConfettiParticle[MaxParticlesLimit];
    private readonly ParticleGPU[] _gpuParticles = new ParticleGPU[MaxParticlesLimit];
    private int _nextParticle;
    private int _activeParticleCount;

    private Vector2 _lastMousePos;
    private float _lastSpawnDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private float _rainbowHue;

    // Configuration values
    private int _maxParticles = 5000;
    private int _burstCount = 50;
    private float _particleLifespan = 3.0f;
    private float _minParticleSize = 8f;
    private float _maxParticleSize = 16f;
    private float _gravity = 200f;
    private float _airResistance = 0.985f;
    private float _flutterAmount = 2.0f;
    private float _burstForce = 400f;
    private float _trailSpacing = 20f;
    private bool _burstOnClick = true;
    private bool _trailOnMove = true;
    private bool _rainbowMode = true;
    private float _rainbowSpeed = 0.5f;
    private bool _useRectangles = true;
    private bool _useCircles = true;
    private bool _useRibbons = true;

    // Party palette colors
    private static readonly Vector4[] PartyColors =
    [
        new Vector4(1.0f, 0.0f, 0.0f, 1.0f),     // Red
        new Vector4(1.0f, 0.55f, 0.0f, 1.0f),    // Orange
        new Vector4(1.0f, 0.84f, 0.0f, 1.0f),    // Yellow
        new Vector4(0.2f, 0.8f, 0.2f, 1.0f),     // Green
        new Vector4(0.12f, 0.56f, 1.0f, 1.0f),   // Blue
        new Vector4(0.6f, 0.2f, 0.8f, 1.0f),     // Purple
        new Vector4(1.0f, 0.41f, 0.71f, 1.0f),   // Pink
        new Vector4(0.0f, 0.81f, 0.82f, 1.0f)    // Cyan
    ];

    public override EffectMetadata Metadata => _metadata;

    public int MaxParticles { get => _maxParticles; set => _maxParticles = value; }
    public int BurstCount { get => _burstCount; set => _burstCount = value; }
    public float ParticleLifespan { get => _particleLifespan; set => _particleLifespan = value; }
    public float MinParticleSize { get => _minParticleSize; set => _minParticleSize = value; }
    public float MaxParticleSize { get => _maxParticleSize; set => _maxParticleSize = value; }
    public float Gravity { get => _gravity; set => _gravity = value; }
    public float AirResistance { get => _airResistance; set => _airResistance = value; }
    public float FlutterAmount { get => _flutterAmount; set => _flutterAmount = value; }
    public float BurstForce { get => _burstForce; set => _burstForce = value; }
    public float TrailSpacing { get => _trailSpacing; set => _trailSpacing = value; }
    public bool BurstOnClick { get => _burstOnClick; set => _burstOnClick = value; }
    public bool TrailOnMove { get => _trailOnMove; set => _trailOnMove = value; }
    public bool RainbowMode { get => _rainbowMode; set => _rainbowMode = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }
    public bool UseRectangles { get => _useRectangles; set => _useRectangles = value; }
    public bool UseCircles { get => _useCircles; set => _useCircles = value; }
    public bool UseRibbons { get => _useRibbons; set => _useRibbons = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        var particleDesc = new BufferDescription
        {
            Size = MaxParticlesLimit * Marshal.SizeOf<ParticleGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ParticleGPU>()
        };
        _particleBuffer = context.CreateBuffer(particleDesc, default);

        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc, default);

        string shaderSource = LoadEmbeddedShader("ConfettiShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        for (int i = 0; i < MaxParticlesLimit; i++)
            _particles[i] = new ConfettiParticle { Life = 0f };
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("maxParticles", out int maxPart))
            _maxParticles = Math.Clamp(maxPart, 100, MaxParticlesLimit);
        if (Configuration.TryGet("burstCount", out int burst))
            _burstCount = burst;
        if (Configuration.TryGet("particleLifespan", out float lifespan))
            _particleLifespan = lifespan;
        if (Configuration.TryGet("minParticleSize", out float minSize))
            _minParticleSize = minSize;
        if (Configuration.TryGet("maxParticleSize", out float maxSize))
            _maxParticleSize = maxSize;
        if (Configuration.TryGet("gravity", out float gravity))
            _gravity = gravity;
        if (Configuration.TryGet("airResistance", out float air))
            _airResistance = air;
        if (Configuration.TryGet("flutterAmount", out float flutter))
            _flutterAmount = flutter;
        if (Configuration.TryGet("burstForce", out float force))
            _burstForce = force;
        if (Configuration.TryGet("trailSpacing", out float spacing))
            _trailSpacing = spacing;
        if (Configuration.TryGet("burstOnClick", out bool burstClick))
            _burstOnClick = burstClick;
        if (Configuration.TryGet("trailOnMove", out bool trailMove))
            _trailOnMove = trailMove;
        if (Configuration.TryGet("rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;
        if (Configuration.TryGet("rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;
        if (Configuration.TryGet("useRectangles", out bool rects))
            _useRectangles = rects;
        if (Configuration.TryGet("useCircles", out bool circles))
            _useCircles = circles;
        if (Configuration.TryGet("useRibbons", out bool ribbons))
            _useRibbons = ribbons;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float dt = (float)gameTime.DeltaTime.TotalSeconds;
        float totalTime = (float)gameTime.TotalTime.TotalSeconds;

        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * dt;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        UpdateParticles(dt);

        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);

        if (_burstOnClick && (leftPressed && !_wasLeftPressed || rightPressed && !_wasRightPressed))
        {
            SpawnBurst(mouseState.Position, _burstCount);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;

        if (_trailOnMove)
        {
            float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);
            _lastSpawnDistance += distanceFromLast;
            if (_lastSpawnDistance >= _trailSpacing)
            {
                SpawnTrailParticles(mouseState.Position, Math.Max(3, _burstCount / 10));
                _lastSpawnDistance = 0f;
            }
        }

        _lastMousePos = mouseState.Position;
    }

    private void UpdateParticles(float dt)
    {
        _activeParticleCount = 0;
        for (int i = 0; i < _maxParticles; i++)
        {
            ref ConfettiParticle p = ref _particles[i];
            if (p.Life <= 0f) continue;

            p.Life -= dt;
            if (p.Life <= 0f) continue;

            p.Position += p.Velocity * dt;
            p.Velocity.Y += _gravity * dt;
            p.Velocity *= _airResistance;

            // Flutter effect - oscillating rotation based on fall speed
            float fallSpeed = Math.Abs(p.Velocity.Y);
            p.Rotation += p.RotationSpeed * dt * (1f + fallSpeed * 0.01f * _flutterAmount);

            _activeParticleCount++;
        }
    }

    private void SpawnBurst(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float speed = _burstForce * (0.5f + Random.Shared.NextSingle() * 0.5f);
            Vector2 velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

            SpawnParticle(position, velocity);
        }
    }

    private void SpawnTrailParticles(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float speed = _burstForce * 0.3f * Random.Shared.NextSingle();
            Vector2 velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

            SpawnParticle(position, velocity);
        }
    }

    private void SpawnParticle(Vector2 position, Vector2 velocity)
    {
        int startIndex = _nextParticle;
        do
        {
            ref ConfettiParticle p = ref _particles[_nextParticle];
            _nextParticle = (_nextParticle + 1) % _maxParticles;

            if (p.Life <= 0f)
            {
                p.Position = position;
                p.Velocity = velocity;
                p.Color = GetParticleColor();
                p.Size = _minParticleSize + Random.Shared.NextSingle() * (_maxParticleSize - _minParticleSize);
                p.Life = _particleLifespan * (0.7f + Random.Shared.NextSingle() * 0.6f);
                p.MaxLife = p.Life;
                p.Rotation = Random.Shared.NextSingle() * MathF.PI * 2f;
                p.RotationSpeed = (Random.Shared.NextSingle() - 0.5f) * 8f;
                p.ShapeType = GetRandomShape();
                break;
            }
        } while (_nextParticle != startIndex);
    }

    private int GetRandomShape()
    {
        var availableShapes = new List<int>();
        if (_useRectangles) availableShapes.Add(ShapeRectangle);
        if (_useCircles) availableShapes.Add(ShapeCircle);
        if (_useRibbons) availableShapes.Add(ShapeRibbon);

        if (availableShapes.Count == 0)
            return ShapeRectangle;

        return availableShapes[Random.Shared.Next(availableShapes.Count)];
    }

    private Vector4 GetParticleColor()
    {
        if (_rainbowMode)
            return HueToRgb(_rainbowHue + Random.Shared.NextSingle() * 0.1f);

        return PartyColors[Random.Shared.Next(PartyColors.Length)];
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null)
            return;

        if (_activeParticleCount == 0)
            return;

        float totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        var frameData = new FrameData
        {
            ViewportSize = context.ViewportSize,
            Time = totalTime,
            GravityStrength = _gravity,
            FlutterAmount = _flutterAmount,
            HdrMultiplier = context.HdrPeakBrightness
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int activeIndex = 0;
        for (int i = 0; i < _maxParticles && activeIndex < _maxParticles; i++)
        {
            ref ConfettiParticle p = ref _particles[i];
            if (p.Life <= 0f) continue;

            _gpuParticles[activeIndex] = new ParticleGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife,
                Rotation = p.Rotation,
                ShapeType = p.ShapeType
            };
            activeIndex++;
        }

        for (int j = activeIndex; j < _maxParticles; j++)
        {
            _gpuParticles[j] = default;
        }

        context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<ParticleGPU>)_gpuParticles.AsSpan(0, _maxParticles));
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.DrawInstanced(6, _maxParticles, 0, 0);
    }

    protected override void OnDispose()
    {
        _particleBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
    }

    private static Vector4 HueToRgb(float hue)
    {
        hue -= MathF.Floor(hue);
        float h = hue * 6f;
        float x = 1f - MathF.Abs(h % 2f - 1f);

        Vector3 rgb = (int)h switch
        {
            0 => new Vector3(1f, x, 0f),
            1 => new Vector3(x, 1f, 0f),
            2 => new Vector3(0f, 1f, x),
            3 => new Vector3(0f, x, 1f),
            4 => new Vector3(x, 0f, 1f),
            _ => new Vector3(1f, 0f, x),
        };

        return new Vector4(rgb.X, rgb.Y, rgb.Z, 1f);
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ConfettiEffect).Assembly;
        string resourceName = $"MouseEffects.Effects.Confetti.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
