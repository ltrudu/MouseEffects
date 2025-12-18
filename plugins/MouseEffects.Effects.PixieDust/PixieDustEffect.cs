using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.PixieDust;

public sealed class PixieDustEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "pixiedust",
        Name = "Pixie Dust",
        Description = "Magical sparkle particles that follow the mouse cursor with floating and fading effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 16
        public Vector4 Padding;           // 16 bytes = 32
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct ParticleInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (drift)
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Particle size
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float RotationAngle;       // 4 bytes - Rotation for variety = 48
        public float GlowIntensity;       // 4 bytes - Individual glow strength
        public float SpinSpeed;           // 4 bytes - Rotation speed
        public float BirthTime;           // 4 bytes - When particle was born
        public float Padding;             // 4 bytes = 64
    }

    // Constants
    private const int MaxParticles = 2000;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _particleBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Particle management (CPU side)
    private readonly ParticleInstance[] _particles = new ParticleInstance[MaxParticles];
    private readonly ParticleInstance[] _gpuParticles = new ParticleInstance[MaxParticles];
    private int _nextParticleIndex;
    private int _activeParticleCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Rainbow hue tracking
    private float _rainbowHue;

    // Configuration fields
    private int _particleCount = 10;
    private float _particleSize = 15f;
    private float _particleLifetime = 2.0f;
    private float _spawnRate = 0.05f;
    private float _glowIntensity = 1.2f;
    private float _driftSpeed = 30f;
    private bool _rainbowMode = true;
    private float _rainbowSpeed = 0.5f;
    private Vector4 _fixedColor = new(1f, 0.8f, 0.2f, 1f);

    // Trigger settings
    private bool _mouseMoveEnabled = true;
    private float _moveDistanceThreshold = 15f;
    private bool _leftClickEnabled = true;
    private int _leftClickBurstCount = 30;
    private bool _rightClickEnabled = true;
    private int _rightClickBurstCount = 50;

    // Public properties for UI binding
    public int ParticleCount { get => _particleCount; set => _particleCount = value; }
    public float ParticleSize { get => _particleSize; set => _particleSize = value; }
    public float ParticleLifetime { get => _particleLifetime; set => _particleLifetime = value; }
    public float SpawnRate { get => _spawnRate; set => _spawnRate = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float DriftSpeed { get => _driftSpeed; set => _driftSpeed = value; }
    public bool RainbowMode { get => _rainbowMode; set => _rainbowMode = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }
    public Vector4 FixedColor { get => _fixedColor; set => _fixedColor = value; }
    public bool MouseMoveEnabled { get => _mouseMoveEnabled; set => _mouseMoveEnabled = value; }
    public float MoveDistanceThreshold { get => _moveDistanceThreshold; set => _moveDistanceThreshold = value; }
    public bool LeftClickEnabled { get => _leftClickEnabled; set => _leftClickEnabled = value; }
    public int LeftClickBurstCount { get => _leftClickBurstCount; set => _leftClickBurstCount = value; }
    public bool RightClickEnabled { get => _rightClickEnabled; set => _rightClickEnabled = value; }
    public int RightClickBurstCount { get => _rightClickBurstCount; set => _rightClickBurstCount = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("PixieDustShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create particle structured buffer
        _particleBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<ParticleInstance>() * MaxParticles,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ParticleInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("pd_particleCount", out int count))
            _particleCount = count;
        if (Configuration.TryGet("pd_particleSize", out float size))
            _particleSize = size;
        if (Configuration.TryGet("pd_lifetime", out float lifetime))
            _particleLifetime = lifetime;
        if (Configuration.TryGet("pd_spawnRate", out float rate))
            _spawnRate = rate;
        if (Configuration.TryGet("pd_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("pd_driftSpeed", out float drift))
            _driftSpeed = drift;
        if (Configuration.TryGet("pd_rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;
        if (Configuration.TryGet("pd_rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;
        if (Configuration.TryGet("pd_fixedColor", out Vector4 color))
            _fixedColor = color;

        // Trigger settings
        if (Configuration.TryGet("pd_mouseMoveEnabled", out bool moveEnabled))
            _mouseMoveEnabled = moveEnabled;
        if (Configuration.TryGet("pd_moveDistanceThreshold", out float moveDist))
            _moveDistanceThreshold = moveDist;
        if (Configuration.TryGet("pd_leftClickEnabled", out bool leftEnabled))
            _leftClickEnabled = leftEnabled;
        if (Configuration.TryGet("pd_leftClickBurstCount", out int leftCount))
            _leftClickBurstCount = leftCount;
        if (Configuration.TryGet("pd_rightClickEnabled", out bool rightEnabled))
            _rightClickEnabled = rightEnabled;
        if (Configuration.TryGet("pd_rightClickBurstCount", out int rightCount))
            _rightClickBurstCount = rightCount;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update rainbow hue
        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * deltaTime;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        // Update existing particles
        UpdateParticles(deltaTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Handle mouse move trigger
        if (_mouseMoveEnabled && distanceFromLast > 0.1f)
        {
            _accumulatedDistance += distanceFromLast;

            if (_accumulatedDistance >= _moveDistanceThreshold)
            {
                SpawnParticles(mouseState.Position, _particleCount, totalTime);
                _accumulatedDistance = 0f;
            }
        }

        // Handle left click trigger
        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        if (_leftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnParticles(mouseState.Position, _leftClickBurstCount, totalTime);
        }
        _wasLeftPressed = leftPressed;

        // Handle right click trigger
        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);
        if (_rightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnParticles(mouseState.Position, _rightClickBurstCount, totalTime);
        }
        _wasRightPressed = rightPressed;

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateParticles(float deltaTime)
    {
        _activeParticleCount = 0;
        for (int i = 0; i < MaxParticles; i++)
        {
            if (_particles[i].Lifetime > 0)
            {
                ref var p = ref _particles[i];

                // Age particle
                p.Lifetime -= deltaTime;

                if (p.Lifetime > 0)
                {
                    // Update position with velocity (drift)
                    p.Position += p.Velocity * deltaTime;

                    // Update rotation
                    p.RotationAngle += p.SpinSpeed * deltaTime;

                    _activeParticleCount++;
                }
            }
        }
    }

    private void SpawnParticles(Vector2 position, int count, float time)
    {
        for (int i = 0; i < count; i++)
        {
            ref var p = ref _particles[_nextParticleIndex];
            _nextParticleIndex = (_nextParticleIndex + 1) % MaxParticles;

            // Random spread around cursor
            float spreadRadius = 10f;
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float radius = Random.Shared.NextSingle() * spreadRadius;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

            p.Position = position + offset;
            p.Lifetime = _particleLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
            p.MaxLifetime = p.Lifetime;
            p.BirthTime = time;

            // Upward drift with random horizontal component
            float driftAngle = -MathF.PI / 2f + (Random.Shared.NextSingle() - 0.5f) * MathF.PI / 3f;
            float driftMagnitude = _driftSpeed * (0.7f + Random.Shared.NextSingle() * 0.6f);
            p.Velocity = new Vector2(MathF.Cos(driftAngle), MathF.Sin(driftAngle)) * driftMagnitude;

            // Random size
            p.Size = _particleSize * (0.6f + Random.Shared.NextSingle() * 0.8f);

            // Random rotation and spin
            p.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
            p.SpinSpeed = (Random.Shared.NextSingle() - 0.5f) * 4f;

            // Random glow intensity
            p.GlowIntensity = _glowIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);

            // Get color
            p.Color = GetParticleColor();
            p.Padding = 0f;
        }
    }

    private Vector4 GetParticleColor()
    {
        if (_rainbowMode)
        {
            // Add some variation to the hue
            float hue = _rainbowHue + Random.Shared.NextSingle() * 0.2f;
            return HueToRgb(hue);
        }
        else
        {
            // Use fixed color with slight brightness variation
            Vector4 color = _fixedColor;
            float brightness = 0.8f + Random.Shared.NextSingle() * 0.4f;
            color.X *= brightness;
            color.Y *= brightness;
            color.Z *= brightness;
            return color;
        }
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

    protected override void OnRender(IRenderContext context)
    {
        if (_activeParticleCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU particle buffer - only include alive particles
        int gpuIndex = 0;
        for (int i = 0; i < MaxParticles && gpuIndex < MaxParticles; i++)
        {
            if (_particles[i].Lifetime > 0)
            {
                _gpuParticles[gpuIndex++] = _particles[i];
            }
        }

        // Fill remaining with zeroed particles
        for (int i = gpuIndex; i < MaxParticles; i++)
        {
            _gpuParticles[i] = default;
        }

        // Update particle buffer
        context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<ParticleInstance>)_gpuParticles.AsSpan());

        // Update constant buffer
        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _particleBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced particles (6 vertices per quad, one instance per particle)
        context.DrawInstanced(6, MaxParticles, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _particleBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.PixieDust.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
