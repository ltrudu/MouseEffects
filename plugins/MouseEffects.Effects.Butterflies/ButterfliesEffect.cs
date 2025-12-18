using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Butterflies;

public sealed class ButterfliesEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "butterflies",
        Name = "Butterflies",
        Description = "Beautiful animated butterflies that flutter around and follow the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
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

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct ButterflyInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Butterfly size
        public float WingFlapPhase;       // 4 bytes - Phase for wing animation
        public float WingFlapSpeed;       // 4 bytes - How fast wings flap
        public float TargetDistance;      // 4 bytes - How close to follow cursor = 48
        public float WanderAngle;         // 4 bytes - Wandering direction
        public float WanderSpeed;         // 4 bytes - Speed of wandering
        public float BodyRotation;        // 4 bytes - Butterfly body rotation
        public float GlowIntensity;       // 4 bytes = 64
        public float PatternVariant;      // 4 bytes - Wing pattern variation
        public float Lifetime;            // 4 bytes - How long butterfly has existed
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 80
    }

    // Constants
    private const int MaxButterflies = 20;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _butterflyBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Butterfly management (CPU side)
    private readonly ButterflyInstance[] _butterflies = new ButterflyInstance[MaxButterflies];
    private readonly ButterflyInstance[] _gpuButterflies = new ButterflyInstance[MaxButterflies];
    private int _activeButterflyCount;

    // Configuration fields
    private int _butterflyCount = 8;
    private float _minSize = 15f;
    private float _maxSize = 30f;
    private float _wingFlapSpeed = 8f;
    private float _followDistance = 100f;
    private float _followStrength = 0.3f;
    private float _wanderStrength = 50f;
    private float _glowIntensity = 1.0f;
    private int _colorMode = 0; // 0=Rainbow, 1=Pastel, 2=Nature
    private float _rainbowSpeed = 0.3f;

    // Rainbow hue tracking
    private float _rainbowHue;

    // Public properties for UI binding
    public int ButterflyCount { get => _butterflyCount; set => _butterflyCount = Math.Clamp(value, 1, MaxButterflies); }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float WingFlapSpeed { get => _wingFlapSpeed; set => _wingFlapSpeed = value; }
    public float FollowDistance { get => _followDistance; set => _followDistance = value; }
    public float FollowStrength { get => _followStrength; set => _followStrength = value; }
    public float WanderStrength { get => _wanderStrength; set => _wanderStrength = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("ButterfliesShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create butterfly structured buffer
        _butterflyBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<ButterflyInstance>() * MaxButterflies,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ButterflyInstance>()
        });

        // Initialize butterflies (will spawn on first update)
        _activeButterflyCount = 0;
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("bf_butterflyCount", out int count))
            _butterflyCount = count;
        if (Configuration.TryGet("bf_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("bf_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("bf_wingFlapSpeed", out float flapSpeed))
            _wingFlapSpeed = flapSpeed;
        if (Configuration.TryGet("bf_followDistance", out float followDist))
            _followDistance = followDist;
        if (Configuration.TryGet("bf_followStrength", out float followStr))
            _followStrength = followStr;
        if (Configuration.TryGet("bf_wanderStrength", out float wanderStr))
            _wanderStrength = wanderStr;
        if (Configuration.TryGet("bf_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("bf_colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("bf_rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update rainbow hue
        _rainbowHue += _rainbowSpeed * deltaTime;
        if (_rainbowHue > 1f) _rainbowHue -= 1f;

        // Spawn butterflies if we don't have enough
        while (_activeButterflyCount < _butterflyCount)
        {
            SpawnButterfly(mouseState.Position, totalTime);
        }

        // Remove excess butterflies if count was reduced
        while (_activeButterflyCount > _butterflyCount)
        {
            _activeButterflyCount--;
        }

        // Update existing butterflies
        for (int i = 0; i < _activeButterflyCount; i++)
        {
            UpdateButterfly(ref _butterflies[i], mouseState.Position, deltaTime, totalTime);
        }
    }

    private void SpawnButterfly(Vector2 mousePos, float time)
    {
        ref var bf = ref _butterflies[_activeButterflyCount];
        _activeButterflyCount++;

        // Spawn near cursor with random offset
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = 50f + Random.Shared.NextSingle() * 100f;
        bf.Position = mousePos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        // Random size
        bf.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random velocity
        bf.Velocity = Vector2.Zero;

        // Wing animation
        bf.WingFlapPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        bf.WingFlapSpeed = _wingFlapSpeed * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Following behavior
        bf.TargetDistance = _followDistance * (0.7f + Random.Shared.NextSingle() * 0.6f);

        // Wandering
        bf.WanderAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
        bf.WanderSpeed = 0.5f + Random.Shared.NextSingle() * 1.5f;

        // Visual properties
        bf.BodyRotation = 0f;
        bf.GlowIntensity = _glowIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);
        bf.PatternVariant = Random.Shared.NextSingle();
        bf.Color = GetButterflyColor();
        bf.Lifetime = 0f;
        bf.Padding1 = 0f;
        bf.Padding2 = 0f;
    }

    private void UpdateButterfly(ref ButterflyInstance bf, Vector2 cursorPos, float deltaTime, float time)
    {
        bf.Lifetime += deltaTime;

        // Update wing flap phase
        bf.WingFlapPhase += bf.WingFlapSpeed * deltaTime;

        // Update wander angle
        bf.WanderAngle += (Random.Shared.NextSingle() - 0.5f) * deltaTime * bf.WanderSpeed;

        // Calculate target position with wandering
        Vector2 wanderOffset = new Vector2(
            MathF.Cos(bf.WanderAngle),
            MathF.Sin(bf.WanderAngle)
        ) * _wanderStrength;

        Vector2 targetPos = cursorPos + wanderOffset;

        // Calculate distance to target
        Vector2 toTarget = targetPos - bf.Position;
        float distance = toTarget.Length();

        // Only follow if beyond target distance
        if (distance > bf.TargetDistance)
        {
            // Soft follow: accelerate toward cursor
            Vector2 desired = Vector2.Normalize(toTarget) * 100f;
            Vector2 steer = (desired - bf.Velocity) * _followStrength;
            bf.Velocity += steer * deltaTime;
        }
        else
        {
            // Gentle slowdown when close enough
            bf.Velocity *= 0.95f;
        }

        // Add slight upward bias (butterflies tend to fly upward)
        bf.Velocity.Y -= 5f * deltaTime;

        // Limit velocity
        float maxSpeed = 150f;
        float speed = bf.Velocity.Length();
        if (speed > maxSpeed)
        {
            bf.Velocity = Vector2.Normalize(bf.Velocity) * maxSpeed;
        }

        // Update position
        bf.Position += bf.Velocity * deltaTime;

        // Calculate body rotation based on velocity
        if (speed > 1f)
        {
            float targetRotation = MathF.Atan2(bf.Velocity.Y, bf.Velocity.X);
            bf.BodyRotation = LerpAngle(bf.BodyRotation, targetRotation, deltaTime * 5f);
        }

        // Wrap around screen
        if (bf.Position.X < -100f) bf.Position.X = ViewportSize.X + 100f;
        if (bf.Position.X > ViewportSize.X + 100f) bf.Position.X = -100f;
        if (bf.Position.Y < -100f) bf.Position.Y = ViewportSize.Y + 100f;
        if (bf.Position.Y > ViewportSize.Y + 100f) bf.Position.Y = -100f;

        // Update color if in rainbow mode
        if (_colorMode == 0)
        {
            bf.Color = GetButterflyColor();
        }
    }

    private Vector4 GetButterflyColor()
    {
        return _colorMode switch
        {
            0 => HueToRgb(_rainbowHue + Random.Shared.NextSingle() * 0.1f), // Rainbow
            1 => GetPastelColor(), // Pastel
            2 => GetNatureColor(), // Nature
            _ => new Vector4(1f, 1f, 1f, 1f)
        };
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

    private static Vector4 GetPastelColor()
    {
        // Soft pastel colors
        Vector4[] pastelColors = [
            new Vector4(1.0f, 0.8f, 0.9f, 1f), // Pink
            new Vector4(0.9f, 0.9f, 1.0f, 1f), // Lavender
            new Vector4(0.8f, 1.0f, 0.9f, 1f), // Mint
            new Vector4(1.0f, 1.0f, 0.8f, 1f), // Cream
            new Vector4(0.9f, 0.8f, 1.0f, 1f), // Purple
            new Vector4(0.8f, 0.9f, 1.0f, 1f), // Sky blue
        ];
        return pastelColors[Random.Shared.Next(pastelColors.Length)];
    }

    private static Vector4 GetNatureColor()
    {
        // Natural butterfly colors
        Vector4[] natureColors = [
            new Vector4(1.0f, 0.5f, 0.0f, 1f), // Monarch orange
            new Vector4(0.2f, 0.6f, 1.0f, 1f), // Blue morpho
            new Vector4(1.0f, 0.8f, 0.0f, 1f), // Yellow swallowtail
            new Vector4(1.0f, 1.0f, 1.0f, 1f), // White cabbage
            new Vector4(0.4f, 0.2f, 0.1f, 1f), // Brown wood nymph
            new Vector4(0.0f, 0.5f, 0.3f, 1f), // Green birdwing
        ];
        return natureColors[Random.Shared.Next(natureColors.Length)];
    }

    private static float LerpAngle(float from, float to, float t)
    {
        float delta = ((to - from + MathF.PI) % (MathF.PI * 2f)) - MathF.PI;
        return from + delta * t;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeButterflyCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Copy butterflies to GPU buffer
        for (int i = 0; i < MaxButterflies; i++)
        {
            if (i < _activeButterflyCount)
            {
                _gpuButterflies[i] = _butterflies[i];
            }
            else
            {
                _gpuButterflies[i] = default;
            }
        }
        context.UpdateBuffer(_butterflyBuffer!, (ReadOnlySpan<ButterflyInstance>)_gpuButterflies.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _butterflyBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _butterflyBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced butterflies (6 vertices per quad, one instance per butterfly)
        context.DrawInstanced(6, MaxButterflies, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _butterflyBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Butterflies.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
