using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Snowfall;

public sealed class SnowfallEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "snowfall",
        Name = "Snowfall",
        Description = "Gentle snowflakes falling around the mouse cursor with wind physics",
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
    private struct SnowflakeInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (fall + wind)
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Snowflake size
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float RotationAngle;       // 4 bytes - Current rotation = 48
        public float RotationSpeed;       // 4 bytes - Rotation speed
        public float WindPhase;           // 4 bytes - Phase offset for wind oscillation
        public float GlowIntensity;       // 4 bytes - Individual glow strength
        public float Padding;             // 4 bytes = 64
    }

    // Constants
    private const int MaxSnowflakes = 500;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _snowflakeBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Snowflake management (CPU side)
    private readonly SnowflakeInstance[] _snowflakes = new SnowflakeInstance[MaxSnowflakes];
    private readonly SnowflakeInstance[] _gpuSnowflakes = new SnowflakeInstance[MaxSnowflakes];
    private int _nextSnowflakeIndex;
    private int _activeSnowflakeCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields
    private int _snowflakeCount = 50;
    private float _fallSpeed = 80f;
    private float _windStrength = 30f;
    private float _windFrequency = 0.5f;
    private float _minSize = 8f;
    private float _maxSize = 20f;
    private float _rotationSpeed = 1.0f;
    private float _glowIntensity = 1.0f;
    private float _spawnRadius = 150f;
    private float _snowflakeLifetime = 8f;

    // Public properties for UI binding
    public int SnowflakeCount { get => _snowflakeCount; set => _snowflakeCount = value; }
    public float FallSpeed { get => _fallSpeed; set => _fallSpeed = value; }
    public float WindStrength { get => _windStrength; set => _windStrength = value; }
    public float WindFrequency { get => _windFrequency; set => _windFrequency = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float RotationSpeed { get => _rotationSpeed; set => _rotationSpeed = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float SpawnRadius { get => _spawnRadius; set => _spawnRadius = value; }
    public float SnowflakeLifetime { get => _snowflakeLifetime; set => _snowflakeLifetime = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("SnowfallShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create snowflake structured buffer
        _snowflakeBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<SnowflakeInstance>() * MaxSnowflakes,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<SnowflakeInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("sf_snowflakeCount", out int count))
            _snowflakeCount = count;
        if (Configuration.TryGet("sf_fallSpeed", out float fall))
            _fallSpeed = fall;
        if (Configuration.TryGet("sf_windStrength", out float wind))
            _windStrength = wind;
        if (Configuration.TryGet("sf_windFrequency", out float freq))
            _windFrequency = freq;
        if (Configuration.TryGet("sf_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("sf_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("sf_rotationSpeed", out float rotSpeed))
            _rotationSpeed = rotSpeed;
        if (Configuration.TryGet("sf_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("sf_spawnRadius", out float radius))
            _spawnRadius = radius;
        if (Configuration.TryGet("sf_lifetime", out float lifetime))
            _snowflakeLifetime = lifetime;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing snowflakes
        UpdateSnowflakes(deltaTime, totalTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Spawn snowflakes continuously when mouse moves
        if (distanceFromLast > 0.1f)
        {
            // Spawn rate based on snowflake count setting (more snowflakes = faster spawn)
            float spawnRate = _snowflakeCount * 2f; // Snowflakes per second
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnSnowflake(mouseState.Position, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateSnowflakes(float deltaTime, float totalTime)
    {
        _activeSnowflakeCount = 0;
        for (int i = 0; i < MaxSnowflakes; i++)
        {
            if (_snowflakes[i].Lifetime > 0)
            {
                ref var sf = ref _snowflakes[i];

                // Age snowflake
                sf.Lifetime -= deltaTime;

                if (sf.Lifetime > 0)
                {
                    // Apply gravity (downward fall)
                    sf.Velocity.Y = _fallSpeed;

                    // Apply wind effect (oscillating horizontal movement)
                    float windEffect = MathF.Sin(totalTime * _windFrequency + sf.WindPhase) * _windStrength;
                    sf.Velocity.X = windEffect;

                    // Update position
                    sf.Position += sf.Velocity * deltaTime;

                    // Update rotation
                    sf.RotationAngle += sf.RotationSpeed * deltaTime;

                    // Respawn at top if fallen below screen
                    if (sf.Position.Y > 1080f) // Assume max screen height, will be clipped by viewport
                    {
                        sf.Position.Y = -50f; // Start above screen
                        sf.Position.X += (Random.Shared.NextSingle() - 0.5f) * 200f; // Random horizontal offset
                    }

                    _activeSnowflakeCount++;
                }
            }
        }
    }

    private void SpawnSnowflake(Vector2 position, float time)
    {
        ref var sf = ref _snowflakes[_nextSnowflakeIndex];
        _nextSnowflakeIndex = (_nextSnowflakeIndex + 1) % MaxSnowflakes;

        // Random offset around cursor (spawn above cursor in a radius)
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = Random.Shared.NextSingle() * _spawnRadius;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        // Bias spawn position upward
        offset.Y -= _spawnRadius * 0.5f; // Spawn above cursor

        sf.Position = position + offset;
        sf.Lifetime = _snowflakeLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        sf.MaxLifetime = sf.Lifetime;

        // Initial velocity (will be overridden in update, but set for first frame)
        sf.Velocity = new Vector2(0, _fallSpeed);

        // Random size
        sf.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random rotation and rotation speed
        sf.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
        sf.RotationSpeed = (Random.Shared.NextSingle() - 0.5f) * _rotationSpeed * 2f;

        // Random wind phase for varied oscillation
        sf.WindPhase = Random.Shared.NextSingle() * MathF.PI * 2f;

        // Random glow intensity
        sf.GlowIntensity = _glowIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // White/light blue color
        float colorVariation = 0.9f + Random.Shared.NextSingle() * 0.1f;
        sf.Color = new Vector4(colorVariation, colorVariation, 1f, 1f); // Slight blue tint
        sf.Padding = 0f;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeSnowflakeCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU snowflake buffer - only include alive snowflakes
        int gpuIndex = 0;
        for (int i = 0; i < MaxSnowflakes && gpuIndex < MaxSnowflakes; i++)
        {
            if (_snowflakes[i].Lifetime > 0)
            {
                _gpuSnowflakes[gpuIndex++] = _snowflakes[i];
            }
        }

        // Fill remaining with zeroed snowflakes
        for (int i = gpuIndex; i < MaxSnowflakes; i++)
        {
            _gpuSnowflakes[i] = default;
        }

        // Update snowflake buffer
        context.UpdateBuffer(_snowflakeBuffer!, (ReadOnlySpan<SnowflakeInstance>)_gpuSnowflakes.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _snowflakeBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _snowflakeBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced snowflakes (6 vertices per quad, one instance per snowflake)
        context.DrawInstanced(6, MaxSnowflakes, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _snowflakeBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Snowfall.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
