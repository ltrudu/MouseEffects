using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Fireflies;

public sealed class FirefliesEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "fireflies",
        Name = "Fireflies",
        Description = "Glowing fireflies that swarm around the mouse cursor with pulsing bioluminescence",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;
        public float Time;
        public float HdrMultiplier;
        public Vector4 Padding;
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct FireflyInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Firefly glow size
        public float PulsePhase;          // 4 bytes - Phase offset for pulsing
        public float PulseSpeed;          // 4 bytes - Individual pulse speed
        public float Brightness;          // 4 bytes = 48
        public float WanderAngle;         // 4 bytes - Current wander direction
        public float TargetDistance;      // 4 bytes - Preferred distance from cursor
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 64
    }

    // Constants
    private const int MaxFireflies = 500;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _fireflyBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Firefly management (CPU side)
    private readonly FireflyInstance[] _fireflies = new FireflyInstance[MaxFireflies];
    private readonly FireflyInstance[] _gpuFireflies = new FireflyInstance[MaxFireflies];
    private int _activeFireflyCount;

    // Configuration fields
    private int _fireflyCount = 15;
    private float _glowSize = 20f;
    private Vector4 _glowColor = new(0.8f, 1.0f, 0.3f, 1f);
    private float _pulseSpeed = 3.0f;
    private float _pulseRandomness = 0.5f;
    private float _minBrightness = 0.2f;
    private float _maxBrightness = 1.0f;
    private float _attractionStrength = 0.5f;
    private float _wanderStrength = 30f;
    private float _maxSpeed = 100f;
    private float _wanderChangeRate = 2.0f;
    private float _hdrMultiplier = 1.5f;

    // Explosion settings
    private bool _explosionEnabled = true;
    private float _explosionStrength = 500f;

    // Public properties for UI binding
    public int FireflyCount { get => _fireflyCount; set => _fireflyCount = Math.Clamp(value, 5, MaxFireflies); }
    public float GlowSize { get => _glowSize; set => _glowSize = value; }
    public Vector4 GlowColor { get => _glowColor; set => _glowColor = value; }
    public float PulseSpeed { get => _pulseSpeed; set => _pulseSpeed = value; }
    public float PulseRandomness { get => _pulseRandomness; set => _pulseRandomness = value; }
    public float MinBrightness { get => _minBrightness; set => _minBrightness = value; }
    public float MaxBrightness { get => _maxBrightness; set => _maxBrightness = value; }
    public float AttractionStrength { get => _attractionStrength; set => _attractionStrength = value; }
    public float WanderStrength { get => _wanderStrength; set => _wanderStrength = value; }
    public float MaxSpeed { get => _maxSpeed; set => _maxSpeed = value; }
    public float WanderChangeRate { get => _wanderChangeRate; set => _wanderChangeRate = value; }
    public float HdrMultiplier { get => _hdrMultiplier; set => _hdrMultiplier = value; }
    public bool ExplosionEnabled { get => _explosionEnabled; set => _explosionEnabled = value; }
    public float ExplosionStrength { get => _explosionStrength; set => _explosionStrength = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("FirefliesShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create firefly structured buffer
        _fireflyBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FireflyInstance>() * MaxFireflies,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<FireflyInstance>()
        });

        // Initialize fireflies (will spawn on first update)
        _activeFireflyCount = 0;
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("ff_fireflyCount", out int count))
            _fireflyCount = count;
        if (Configuration.TryGet("ff_glowSize", out float size))
            _glowSize = size;
        if (Configuration.TryGet("ff_glowColor", out Vector4 color))
            _glowColor = color;
        if (Configuration.TryGet("ff_pulseSpeed", out float pulseSpd))
            _pulseSpeed = pulseSpd;
        if (Configuration.TryGet("ff_pulseRandomness", out float pulseRnd))
            _pulseRandomness = pulseRnd;
        if (Configuration.TryGet("ff_minBrightness", out float minBright))
            _minBrightness = minBright;
        if (Configuration.TryGet("ff_maxBrightness", out float maxBright))
            _maxBrightness = maxBright;
        if (Configuration.TryGet("ff_attractionStrength", out float attraction))
            _attractionStrength = attraction;
        if (Configuration.TryGet("ff_wanderStrength", out float wander))
            _wanderStrength = wander;
        if (Configuration.TryGet("ff_maxSpeed", out float speed))
            _maxSpeed = speed;
        if (Configuration.TryGet("ff_wanderChangeRate", out float wanderRate))
            _wanderChangeRate = wanderRate;
        if (Configuration.TryGet("ff_hdrMultiplier", out float hdr))
            _hdrMultiplier = hdr;
        if (Configuration.TryGet("ff_explosionEnabled", out bool explosionEnabled))
            _explosionEnabled = explosionEnabled;
        if (Configuration.TryGet("ff_explosionStrength", out float explosion))
            _explosionStrength = explosion;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Detect mouse click (left or right button pressed this frame)
        bool clicked = mouseState.IsButtonPressed(MouseButtons.Left) || mouseState.IsButtonPressed(MouseButtons.Right);

        // Explode fireflies on click (if enabled)
        if (_explosionEnabled && clicked)
        {
            ExplodeFireflies(mouseState.Position);
        }

        // Spawn fireflies if we don't have enough
        while (_activeFireflyCount < _fireflyCount)
        {
            SpawnFirefly(mouseState.Position, totalTime);
        }

        // Remove excess fireflies if count was reduced
        while (_activeFireflyCount > _fireflyCount)
        {
            _activeFireflyCount--;
        }

        // Update existing fireflies
        for (int i = 0; i < _activeFireflyCount; i++)
        {
            UpdateFirefly(ref _fireflies[i], mouseState.Position, deltaTime, totalTime);
        }
    }

    private void ExplodeFireflies(Vector2 explosionCenter)
    {
        for (int i = 0; i < _activeFireflyCount; i++)
        {
            ref var ff = ref _fireflies[i];

            // Calculate direction away from explosion center
            Vector2 direction = ff.Position - explosionCenter;
            float distance = direction.Length();

            if (distance < 1f)
            {
                // If firefly is at explosion center, give it a random direction
                float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                distance = 1f;
            }
            else
            {
                direction = Vector2.Normalize(direction);
            }

            // Apply explosion force (stronger for closer fireflies)
            float falloff = MathF.Max(0.1f, 1f - (distance / 500f));
            float force = _explosionStrength * falloff;

            // Add random variation to make it more organic
            force *= 0.7f + Random.Shared.NextSingle() * 0.6f;

            ff.Velocity += direction * force;
        }
    }

    private void SpawnFirefly(Vector2 mousePos, float time)
    {
        ref var ff = ref _fireflies[_activeFireflyCount];
        _activeFireflyCount++;

        // Spawn near cursor with random offset
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = 100f + Random.Shared.NextSingle() * 150f;
        ff.Position = mousePos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        // Random size variation
        ff.Size = _glowSize * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Initial velocity
        ff.Velocity = Vector2.Zero;

        // Pulse properties - each firefly has unique timing
        ff.PulsePhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        ff.PulseSpeed = _pulseSpeed * (1f - _pulseRandomness + Random.Shared.NextSingle() * _pulseRandomness * 2f);

        // Initial brightness
        ff.Brightness = _minBrightness + Random.Shared.NextSingle() * (_maxBrightness - _minBrightness);

        // Wandering behavior
        ff.WanderAngle = Random.Shared.NextSingle() * MathF.PI * 2f;

        // Preferred distance from cursor (creates swarm distribution)
        ff.TargetDistance = 50f + Random.Shared.NextSingle() * 150f;

        // Color (slight variation)
        ff.Color = _glowColor;
        float colorVar = 0.9f + Random.Shared.NextSingle() * 0.2f;
        ff.Color.X *= colorVar;
        ff.Color.Y *= colorVar;
        ff.Color.Z *= colorVar;

        ff.Padding1 = 0f;
        ff.Padding2 = 0f;
    }

    private void UpdateFirefly(ref FireflyInstance ff, Vector2 cursorPos, float deltaTime, float time)
    {
        // Update pulse phase
        ff.PulsePhase += ff.PulseSpeed * deltaTime;

        // Calculate pulsing brightness (smooth sine wave)
        float pulseValue = (MathF.Sin(ff.PulsePhase) + 1f) * 0.5f; // 0 to 1
        ff.Brightness = _minBrightness + pulseValue * (_maxBrightness - _minBrightness);

        // Update wander angle (random walk)
        ff.WanderAngle += (Random.Shared.NextSingle() - 0.5f) * _wanderChangeRate * deltaTime;

        // Calculate wander offset
        Vector2 wanderOffset = new Vector2(
            MathF.Cos(ff.WanderAngle),
            MathF.Sin(ff.WanderAngle)
        ) * _wanderStrength;

        // Calculate target position (cursor + wander)
        Vector2 targetPos = cursorPos + wanderOffset;

        // Vector from firefly to target
        Vector2 toTarget = targetPos - ff.Position;
        float distance = toTarget.Length();

        // Apply attraction force (stronger when far from target distance)
        if (distance > 1f)
        {
            float distanceError = distance - ff.TargetDistance;

            // Only attract if beyond target distance, otherwise let it wander
            if (distanceError > 0f)
            {
                Vector2 desiredVelocity = Vector2.Normalize(toTarget) * MathF.Min(distanceError * 2f, _maxSpeed);
                Vector2 steering = (desiredVelocity - ff.Velocity) * _attractionStrength;
                ff.Velocity += steering * deltaTime;
            }
            else
            {
                // Gentle slowdown when within target distance
                ff.Velocity *= 0.95f;
            }
        }

        // Add slight upward drift (fireflies tend to hover upward)
        ff.Velocity.Y -= 8f * deltaTime;

        // Limit velocity
        float speed = ff.Velocity.Length();
        if (speed > _maxSpeed)
        {
            ff.Velocity = Vector2.Normalize(ff.Velocity) * _maxSpeed;
        }

        // Update position
        ff.Position += ff.Velocity * deltaTime;

        // Wrap around screen edges
        if (ff.Position.X < -100f) ff.Position.X = ViewportSize.X + 100f;
        if (ff.Position.X > ViewportSize.X + 100f) ff.Position.X = -100f;
        if (ff.Position.Y < -100f) ff.Position.Y = ViewportSize.Y + 100f;
        if (ff.Position.Y > ViewportSize.Y + 100f) ff.Position.Y = -100f;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeFireflyCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Copy fireflies to GPU buffer
        for (int i = 0; i < MaxFireflies; i++)
        {
            if (i < _activeFireflyCount)
            {
                _gpuFireflies[i] = _fireflies[i];
            }
            else
            {
                _gpuFireflies[i] = default;
            }
        }
        context.UpdateBuffer(_fireflyBuffer!, (ReadOnlySpan<FireflyInstance>)_gpuFireflies.AsSpan());

        // Update constant buffer
        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness * _hdrMultiplier,
            Padding = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _fireflyBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _fireflyBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced fireflies (6 vertices per quad, one instance per firefly)
        context.DrawInstanced(6, MaxFireflies, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _fireflyBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Fireflies.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
