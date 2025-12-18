using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Hearts;

public sealed class HeartsEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "hearts",
        Name = "Hearts",
        Description = "Floating heart particles following the mouse cursor with gentle wobble animation",
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
    private struct HeartInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (float + wobble)
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Heart size
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float RotationAngle;       // 4 bytes - Current rotation = 48
        public float WobblePhase;         // 4 bytes - Phase for wobble oscillation
        public float WobbleAmplitude;     // 4 bytes - Wobble strength
        public float FloatSpeed;          // 4 bytes - Individual float speed
        public float GlowIntensity;       // 4 bytes = 64
        public float SparklePhase;        // 4 bytes - Phase for sparkle effect
        public float ColorVariant;        // 4 bytes - Color mode variant
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 80
    }

    // Constants
    private const int MaxHearts = 500;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _heartBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Heart management (CPU side)
    private readonly HeartInstance[] _hearts = new HeartInstance[MaxHearts];
    private readonly HeartInstance[] _gpuHearts = new HeartInstance[MaxHearts];
    private int _nextHeartIndex;
    private int _activeHeartCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Rainbow hue tracking
    private float _rainbowHue;

    // Configuration fields (h_ prefix for config keys)
    private int _heartCount = 15;
    private float _floatSpeed = 40f;
    private float _wobbleAmount = 30f;
    private float _wobbleFrequency = 1.2f;
    private float _minSize = 12f;
    private float _maxSize = 25f;
    private float _rotationAmount = 0.3f;
    private float _glowIntensity = 1.0f;
    private float _sparkleIntensity = 0.5f;
    private float _heartLifetime = 8f;
    private int _colorMode = 0; // 0=Red, 1=Pink, 2=Mixed, 3=Rainbow

    // Public properties for UI binding
    public int HeartCount { get => _heartCount; set => _heartCount = value; }
    public float FloatSpeed { get => _floatSpeed; set => _floatSpeed = value; }
    public float WobbleAmount { get => _wobbleAmount; set => _wobbleAmount = value; }
    public float WobbleFrequency { get => _wobbleFrequency; set => _wobbleFrequency = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float RotationAmount { get => _rotationAmount; set => _rotationAmount = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float SparkleIntensity { get => _sparkleIntensity; set => _sparkleIntensity = value; }
    public float HeartLifetime { get => _heartLifetime; set => _heartLifetime = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("HeartsShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create heart structured buffer
        _heartBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<HeartInstance>() * MaxHearts,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<HeartInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("h_heartCount", out int count))
            _heartCount = count;
        if (Configuration.TryGet("h_floatSpeed", out float floatSpd))
            _floatSpeed = floatSpd;
        if (Configuration.TryGet("h_wobbleAmount", out float wobble))
            _wobbleAmount = wobble;
        if (Configuration.TryGet("h_wobbleFrequency", out float freq))
            _wobbleFrequency = freq;
        if (Configuration.TryGet("h_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("h_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("h_rotationAmount", out float rotation))
            _rotationAmount = rotation;
        if (Configuration.TryGet("h_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("h_sparkleIntensity", out float sparkle))
            _sparkleIntensity = sparkle;
        if (Configuration.TryGet("h_lifetime", out float lifetime))
            _heartLifetime = lifetime;
        if (Configuration.TryGet("h_colorMode", out int colorMode))
            _colorMode = colorMode;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update rainbow hue for rainbow mode
        if (_colorMode == 3)
        {
            _rainbowHue += 0.3f * deltaTime;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        // Update existing hearts
        UpdateHearts(deltaTime, totalTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Spawn hearts continuously when mouse moves
        if (distanceFromLast > 0.1f)
        {
            // Spawn rate based on heart count setting
            float spawnRate = _heartCount * 1.2f; // Hearts per second
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnHeart(mouseState.Position, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateHearts(float deltaTime, float totalTime)
    {
        _activeHeartCount = 0;
        for (int i = 0; i < MaxHearts; i++)
        {
            if (_hearts[i].Lifetime > 0)
            {
                ref var heart = ref _hearts[i];

                // Age heart
                heart.Lifetime -= deltaTime;

                if (heart.Lifetime > 0)
                {
                    // Apply upward float
                    heart.Velocity.Y = -heart.FloatSpeed; // Negative = upward

                    // Apply wobble effect (side-to-side oscillation)
                    float wobbleEffect = MathF.Sin(totalTime * _wobbleFrequency + heart.WobblePhase) * heart.WobbleAmplitude;
                    heart.Velocity.X = wobbleEffect;

                    // Update position
                    heart.Position += heart.Velocity * deltaTime;

                    // Update rotation (gentle tilt)
                    heart.RotationAngle = MathF.Sin(totalTime * 0.8f + heart.WobblePhase) * _rotationAmount;

                    // Update sparkle phase
                    heart.SparklePhase += deltaTime * 3f;

                    // Despawn if floated above screen
                    if (heart.Position.Y < -100f)
                    {
                        heart.Lifetime = 0f;
                    }

                    _activeHeartCount++;
                }
            }
        }
    }

    private void SpawnHeart(Vector2 position, float time)
    {
        ref var heart = ref _hearts[_nextHeartIndex];
        _nextHeartIndex = (_nextHeartIndex + 1) % MaxHearts;

        // Random offset around cursor (spawn slightly below and around cursor)
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = Random.Shared.NextSingle() * 50f;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        heart.Position = position + offset;
        heart.Lifetime = _heartLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        heart.MaxLifetime = heart.Lifetime;

        // Initial velocity (will be overridden in update)
        heart.Velocity = new Vector2(0, -_floatSpeed);

        // Random size
        heart.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random rotation
        heart.RotationAngle = 0f;

        // Random wobble phase for varied oscillation
        heart.WobblePhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        heart.WobbleAmplitude = _wobbleAmount * (0.7f + Random.Shared.NextSingle() * 0.6f);

        // Random float speed variation
        heart.FloatSpeed = _floatSpeed * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Random glow intensity
        heart.GlowIntensity = _glowIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Random sparkle phase
        heart.SparklePhase = Random.Shared.NextSingle() * MathF.PI * 2f;

        // Get color based on color mode
        float colorVariant = Random.Shared.NextSingle();
        heart.ColorVariant = colorVariant;
        heart.Color = GetHeartColor(colorVariant);
        heart.Padding1 = 0f;
        heart.Padding2 = 0f;
    }

    private Vector4 GetHeartColor(float variant)
    {
        return _colorMode switch
        {
            0 => new Vector4(1f, 0f, 0f, 1f), // Classic Red
            1 => GetPinkColor(variant), // Pink variations
            2 => GetMixedColor(variant), // Mixed red/pink/rose gold
            3 => HueToRgb(_rainbowHue + variant * 0.1f), // Rainbow
            _ => new Vector4(1f, 0f, 0f, 1f)
        };
    }

    private static Vector4 GetPinkColor(float variant)
    {
        if (variant < 0.33f)
        {
            // Hot Pink (#FF69B4)
            return new Vector4(1f, 0.41f, 0.71f, 1f);
        }
        else if (variant < 0.66f)
        {
            // Rose Pink (#FF007F)
            return new Vector4(1f, 0f, 0.5f, 1f);
        }
        else
        {
            // Light Pink (#FFB6C1)
            return new Vector4(1f, 0.71f, 0.76f, 1f);
        }
    }

    private static Vector4 GetMixedColor(float variant)
    {
        if (variant < 0.2f)
        {
            // Classic Red (#FF0000)
            return new Vector4(1f, 0f, 0f, 1f);
        }
        else if (variant < 0.4f)
        {
            // Hot Pink (#FF69B4)
            return new Vector4(1f, 0.41f, 0.71f, 1f);
        }
        else if (variant < 0.6f)
        {
            // Rose Pink (#FF007F)
            return new Vector4(1f, 0f, 0.5f, 1f);
        }
        else if (variant < 0.8f)
        {
            // Rose Gold (#B76E79)
            return new Vector4(0.72f, 0.43f, 0.47f, 1f);
        }
        else
        {
            // Coral (#FF7F50)
            return new Vector4(1f, 0.5f, 0.31f, 1f);
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
        if (_activeHeartCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU heart buffer - only include alive hearts
        int gpuIndex = 0;
        for (int i = 0; i < MaxHearts && gpuIndex < MaxHearts; i++)
        {
            if (_hearts[i].Lifetime > 0)
            {
                _gpuHearts[gpuIndex++] = _hearts[i];
            }
        }

        // Fill remaining with zeroed hearts
        for (int i = gpuIndex; i < MaxHearts; i++)
        {
            _gpuHearts[i] = default;
        }

        // Update heart buffer
        context.UpdateBuffer(_heartBuffer!, (ReadOnlySpan<HeartInstance>)_gpuHearts.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _heartBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _heartBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced hearts (6 vertices per quad, one instance per heart)
        context.DrawInstanced(6, MaxHearts, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _heartBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Hearts.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
