using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.DandelionSeeds;

public sealed class DandelionSeedsEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "dandelionseeds",
        Name = "Dandelion Seeds",
        Description = "Delicate dandelion seeds floating away from the mouse cursor on the wind",
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

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct SeedInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (float + wind)
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Seed size
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float RotationAngle;       // 4 bytes - Current rotation = 48
        public float RotationSpeed;       // 4 bytes - Tumble speed
        public float WindPhase;           // 4 bytes - Phase offset for wind oscillation
        public float GlowIntensity;       // 4 bytes - Individual glow strength
        public float PappusPhase;         // 4 bytes - Pappus animation phase = 64
        public float UpwardDrift;         // 4 bytes - Upward drift component
        public float Opacity;             // 4 bytes - Individual opacity
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 80
    }

    // Constants
    private const int MaxSeeds = 500;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _seedBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Seed management (CPU side)
    private readonly SeedInstance[] _seeds = new SeedInstance[MaxSeeds];
    private readonly SeedInstance[] _gpuSeeds = new SeedInstance[MaxSeeds];
    private int _nextSeedIndex;
    private int _activeSeedCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields
    private int _seedCount = 20;
    private float _floatSpeed = 40f;
    private float _windStrength = 50f;
    private float _windFrequency = 0.3f;
    private float _minSize = 12f;
    private float _maxSize = 25f;
    private float _tumbleSpeed = 0.8f;
    private float _glowIntensity = 1.0f;
    private float _spawnRadius = 120f;
    private float _seedLifetime = 12f;
    private float _upwardDriftStrength = 20f;

    // Public properties for UI binding
    public int SeedCount { get => _seedCount; set => _seedCount = value; }
    public float FloatSpeed { get => _floatSpeed; set => _floatSpeed = value; }
    public float WindStrength { get => _windStrength; set => _windStrength = value; }
    public float WindFrequency { get => _windFrequency; set => _windFrequency = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float TumbleSpeed { get => _tumbleSpeed; set => _tumbleSpeed = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float SpawnRadius { get => _spawnRadius; set => _spawnRadius = value; }
    public float SeedLifetime { get => _seedLifetime; set => _seedLifetime = value; }
    public float UpwardDriftStrength { get => _upwardDriftStrength; set => _upwardDriftStrength = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("DandelionSeedsShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create seed structured buffer
        _seedBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<SeedInstance>() * MaxSeeds,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<SeedInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("ds_seedCount", out int count))
            _seedCount = count;
        if (Configuration.TryGet("ds_floatSpeed", out float floatSpd))
            _floatSpeed = floatSpd;
        if (Configuration.TryGet("ds_windStrength", out float wind))
            _windStrength = wind;
        if (Configuration.TryGet("ds_windFrequency", out float freq))
            _windFrequency = freq;
        if (Configuration.TryGet("ds_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("ds_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("ds_tumbleSpeed", out float tumble))
            _tumbleSpeed = tumble;
        if (Configuration.TryGet("ds_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("ds_spawnRadius", out float radius))
            _spawnRadius = radius;
        if (Configuration.TryGet("ds_lifetime", out float lifetime))
            _seedLifetime = lifetime;
        if (Configuration.TryGet("ds_upwardDrift", out float upward))
            _upwardDriftStrength = upward;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing seeds
        UpdateSeeds(deltaTime, totalTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Spawn seeds continuously when mouse moves
        if (distanceFromLast > 0.1f)
        {
            // Spawn rate based on seed count setting
            float spawnRate = _seedCount * 1.5f; // Seeds per second
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnSeed(mouseState.Position, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateSeeds(float deltaTime, float totalTime)
    {
        _activeSeedCount = 0;
        for (int i = 0; i < MaxSeeds; i++)
        {
            if (_seeds[i].Lifetime > 0)
            {
                ref var seed = ref _seeds[i];

                // Age seed
                seed.Lifetime -= deltaTime;

                if (seed.Lifetime > 0)
                {
                    // Very light upward drift
                    seed.Velocity.Y = -_upwardDriftStrength;

                    // Strong wind effect (horizontal movement)
                    float windEffect = MathF.Sin(totalTime * _windFrequency + seed.WindPhase) * _windStrength;
                    seed.Velocity.X = windEffect;

                    // Add gentle floating motion (vertical oscillation)
                    float floatOscillation = MathF.Sin(totalTime * 1.2f + seed.PappusPhase) * _floatSpeed * 0.3f;
                    seed.Velocity.Y += floatOscillation;

                    // Update position
                    seed.Position += seed.Velocity * deltaTime;

                    // Update rotation (gentle tumble)
                    seed.RotationAngle += seed.RotationSpeed * deltaTime;

                    // Update pappus animation phase
                    seed.PappusPhase += deltaTime * 0.5f;

                    // Calculate opacity based on lifetime (fade in and out)
                    float lifeFraction = seed.Lifetime / seed.MaxLifetime;
                    float fadeIn = MathF.Min(1f, (1f - lifeFraction) * 3f);
                    float fadeOut = MathF.Min(1f, lifeFraction * 2f);
                    seed.Opacity = MathF.Min(fadeIn, fadeOut);

                    _activeSeedCount++;
                }
            }
        }
    }

    private void SpawnSeed(Vector2 position, float time)
    {
        ref var seed = ref _seeds[_nextSeedIndex];
        _nextSeedIndex = (_nextSeedIndex + 1) % MaxSeeds;

        // Random offset around cursor
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = Random.Shared.NextSingle() * _spawnRadius;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        seed.Position = position + offset;
        seed.Lifetime = _seedLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        seed.MaxLifetime = seed.Lifetime;

        // Initial velocity (will be overridden in update)
        seed.Velocity = new Vector2(0, -_upwardDriftStrength);

        // Random size
        seed.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random rotation and tumble speed (very gentle)
        seed.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
        seed.RotationSpeed = (Random.Shared.NextSingle() - 0.5f) * _tumbleSpeed;

        // Random wind phase for varied oscillation
        seed.WindPhase = Random.Shared.NextSingle() * MathF.PI * 2f;

        // Random pappus animation phase
        seed.PappusPhase = Random.Shared.NextSingle() * MathF.PI * 2f;

        // Random upward drift variation
        seed.UpwardDrift = _upwardDriftStrength * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Random glow intensity
        seed.GlowIntensity = _glowIntensity * (0.7f + Random.Shared.NextSingle() * 0.6f);

        // White/cream color with slight variation
        float colorVariation = 0.95f + Random.Shared.NextSingle() * 0.05f;
        seed.Color = new Vector4(colorVariation, colorVariation * 0.98f, colorVariation * 0.92f, 1f); // Slight cream tint

        seed.Opacity = 1f;
        seed.Padding1 = 0f;
        seed.Padding2 = 0f;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeSeedCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU seed buffer - only include alive seeds
        int gpuIndex = 0;
        for (int i = 0; i < MaxSeeds && gpuIndex < MaxSeeds; i++)
        {
            if (_seeds[i].Lifetime > 0)
            {
                _gpuSeeds[gpuIndex++] = _seeds[i];
            }
        }

        // Fill remaining with zeroed seeds
        for (int i = gpuIndex; i < MaxSeeds; i++)
        {
            _gpuSeeds[i] = default;
        }

        // Update seed buffer
        context.UpdateBuffer(_seedBuffer!, (ReadOnlySpan<SeedInstance>)_gpuSeeds.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _seedBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _seedBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced seeds (6 vertices per quad, one instance per seed)
        context.DrawInstanced(6, MaxSeeds, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _seedBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.DandelionSeeds.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
