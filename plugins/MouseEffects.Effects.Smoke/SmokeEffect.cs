using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Smoke;

public sealed class SmokeEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "smoke",
        Name = "Smoke",
        Description = "Soft, wispy smoke trails following the mouse cursor with rising motion and turbulence",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 16
        public float Softness;            // 4 bytes
        public float Opacity;             // 4 bytes
        public Vector2 Padding;           // 8 bytes = 32
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct SmokeParticle
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Current size
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float Age;                 // 4 bytes - How long particle has existed = 48
        public float ExpansionRate;       // 4 bytes - How fast it expands
        public float TurbulencePhase;     // 4 bytes - Random phase for turbulence
        public float RotationAngle;       // 4 bytes - For variation
        public float InitialSize;         // 4 bytes - Starting size = 64
        public Vector4 Padding;           // 16 bytes = 80
    }

    // Constants
    private const int MaxParticles = 2000;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _particleBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Particle management (CPU side)
    private readonly SmokeParticle[] _particles = new SmokeParticle[MaxParticles];
    private readonly SmokeParticle[] _gpuParticles = new SmokeParticle[MaxParticles];
    private int _nextParticleIndex;
    private int _activeParticleCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private float _timeSinceLastSpawn;

    // Perlin noise helper for turbulence
    private readonly Random _random = new();

    // Configuration fields (sm_ prefix for Smoke)
    private int _particleCount = 100;
    private float _particleSize = 20f;
    private float _particleLifetime = 3.0f;
    private float _spawnRate = 0.05f;
    private float _riseSpeed = 50f;
    private float _expansionRate = 15f;
    private float _turbulenceStrength = 30f;
    private float _opacity = 0.6f;
    private float _softness = 0.8f;
    private int _colorMode = 0; // 0=gray, 1=white, 2=black, 3=colored
    private Vector4 _smokeColor = new(0.8f, 0.8f, 0.8f, 1f);

    // Trigger settings
    private bool _mouseMoveEnabled = true;
    private float _moveDistanceThreshold = 10f;
    private bool _leftClickEnabled = true;
    private int _leftClickBurstCount = 50;
    private bool _rightClickEnabled = true;
    private int _rightClickBurstCount = 80;

    // Public properties for UI binding
    public int ParticleCount { get => _particleCount; set => _particleCount = value; }
    public float ParticleSize { get => _particleSize; set => _particleSize = value; }
    public float ParticleLifetime { get => _particleLifetime; set => _particleLifetime = value; }
    public float SpawnRate { get => _spawnRate; set => _spawnRate = value; }
    public float RiseSpeed { get => _riseSpeed; set => _riseSpeed = value; }
    public float ExpansionRate { get => _expansionRate; set => _expansionRate = value; }
    public float TurbulenceStrength { get => _turbulenceStrength; set => _turbulenceStrength = value; }
    public float Opacity { get => _opacity; set => _opacity = value; }
    public float Softness { get => _softness; set => _softness = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public Vector4 SmokeColor { get => _smokeColor; set => _smokeColor = value; }
    public bool MouseMoveEnabled { get => _mouseMoveEnabled; set => _mouseMoveEnabled = value; }
    public float MoveDistanceThreshold { get => _moveDistanceThreshold; set => _moveDistanceThreshold = value; }
    public bool LeftClickEnabled { get => _leftClickEnabled; set => _leftClickEnabled = value; }
    public int LeftClickBurstCount { get => _leftClickBurstCount; set => _leftClickBurstCount = value; }
    public bool RightClickEnabled { get => _rightClickEnabled; set => _rightClickEnabled = value; }
    public int RightClickBurstCount { get => _rightClickBurstCount; set => _rightClickBurstCount = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("SmokeShader.hlsl");
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
            Size = Marshal.SizeOf<SmokeParticle>() * MaxParticles,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<SmokeParticle>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("sm_particleCount", out int count))
            _particleCount = count;
        if (Configuration.TryGet("sm_particleSize", out float size))
            _particleSize = size;
        if (Configuration.TryGet("sm_particleLifetime", out float lifetime))
            _particleLifetime = lifetime;
        if (Configuration.TryGet("sm_spawnRate", out float rate))
            _spawnRate = rate;
        if (Configuration.TryGet("sm_riseSpeed", out float rise))
            _riseSpeed = rise;
        if (Configuration.TryGet("sm_expansionRate", out float expansion))
            _expansionRate = expansion;
        if (Configuration.TryGet("sm_turbulenceStrength", out float turbulence))
            _turbulenceStrength = turbulence;
        if (Configuration.TryGet("sm_opacity", out float opacity))
            _opacity = opacity;
        if (Configuration.TryGet("sm_softness", out float softness))
            _softness = softness;
        if (Configuration.TryGet("sm_colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("sm_smokeColor", out Vector4 color))
            _smokeColor = color;

        // Trigger settings
        if (Configuration.TryGet("sm_mouseMoveEnabled", out bool moveEnabled))
            _mouseMoveEnabled = moveEnabled;
        if (Configuration.TryGet("sm_moveDistanceThreshold", out float moveDist))
            _moveDistanceThreshold = moveDist;
        if (Configuration.TryGet("sm_leftClickEnabled", out bool leftEnabled))
            _leftClickEnabled = leftEnabled;
        if (Configuration.TryGet("sm_leftClickBurstCount", out int leftCount))
            _leftClickBurstCount = leftCount;
        if (Configuration.TryGet("sm_rightClickEnabled", out bool rightEnabled))
            _rightClickEnabled = rightEnabled;
        if (Configuration.TryGet("sm_rightClickBurstCount", out int rightCount))
            _rightClickBurstCount = rightCount;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;

        // Update existing particles
        UpdateParticles(deltaTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Track time for spawn rate
        _timeSinceLastSpawn += deltaTime;

        // Handle mouse move trigger
        if (_mouseMoveEnabled && distanceFromLast > 0.1f)
        {
            _accumulatedDistance += distanceFromLast;

            if (_accumulatedDistance >= _moveDistanceThreshold && _timeSinceLastSpawn >= _spawnRate)
            {
                SpawnParticles(mouseState.Position, _particleCount);
                _accumulatedDistance = 0f;
                _timeSinceLastSpawn = 0f;
            }
        }

        // Handle left click trigger
        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        if (_leftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnParticles(mouseState.Position, _leftClickBurstCount);
        }
        _wasLeftPressed = leftPressed;

        // Handle right click trigger
        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);
        if (_rightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnParticles(mouseState.Position, _rightClickBurstCount);
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
                p.Age += deltaTime;

                if (p.Lifetime > 0)
                {
                    // Apply upward motion (rise)
                    p.Velocity.Y -= _riseSpeed * deltaTime;

                    // Apply turbulence (noise-based horizontal drift)
                    float turbulenceX = MathF.Sin(p.TurbulencePhase + p.Age * 2f) * _turbulenceStrength;
                    float turbulenceY = MathF.Cos(p.TurbulencePhase * 1.3f + p.Age * 1.5f) * _turbulenceStrength * 0.3f;
                    p.Velocity.X = turbulenceX;
                    p.Velocity.Y += turbulenceY;

                    // Update position with velocity
                    p.Position += p.Velocity * deltaTime;

                    // Expand smoke over time
                    p.Size += p.ExpansionRate * deltaTime;

                    // Slight rotation for variety
                    p.RotationAngle += deltaTime * 0.5f;

                    _activeParticleCount++;
                }
            }
        }
    }

    private void SpawnParticles(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            ref var p = ref _particles[_nextParticleIndex];
            _nextParticleIndex = (_nextParticleIndex + 1) % MaxParticles;

            // Random spread around cursor
            float spreadRadius = 5f;
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float radius = Random.Shared.NextSingle() * spreadRadius;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

            p.Position = position + offset;
            p.Lifetime = _particleLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
            p.MaxLifetime = p.Lifetime;
            p.Age = 0f;

            // Initial velocity (slight upward bias)
            float driftAngle = -MathF.PI / 2f + (Random.Shared.NextSingle() - 0.5f) * MathF.PI / 4f;
            float driftMagnitude = _riseSpeed * 0.5f * (0.7f + Random.Shared.NextSingle() * 0.6f);
            p.Velocity = new Vector2(MathF.Cos(driftAngle), MathF.Sin(driftAngle)) * driftMagnitude;

            // Random initial size
            p.InitialSize = _particleSize * (0.6f + Random.Shared.NextSingle() * 0.8f);
            p.Size = p.InitialSize;

            // Expansion rate variation
            p.ExpansionRate = _expansionRate * (0.8f + Random.Shared.NextSingle() * 0.4f);

            // Random turbulence phase for organic motion
            p.TurbulencePhase = Random.Shared.NextSingle() * MathF.PI * 2f;

            // Random rotation
            p.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;

            // Get color based on mode
            p.Color = GetSmokeColor();
            p.Padding = Vector4.Zero;
        }
    }

    private Vector4 GetSmokeColor()
    {
        return _colorMode switch
        {
            1 => new Vector4(0.95f, 0.95f, 0.95f, 1f),  // White smoke
            2 => new Vector4(0.15f, 0.15f, 0.15f, 1f),  // Black smoke
            3 => _smokeColor,                            // Colored smoke
            _ => new Vector4(0.6f, 0.6f, 0.6f, 1f)      // Gray smoke (default)
        };
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
        context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<SmokeParticle>)_gpuParticles.AsSpan());

        // Update constant buffer
        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Softness = _softness,
            Opacity = _opacity,
            Padding = Vector2.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _particleBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced particles (6 vertices per quad, one instance per particle)
        context.DrawInstanced(6, MaxParticles, 0, 0);
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
        var resourceName = $"MouseEffects.Effects.Smoke.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
