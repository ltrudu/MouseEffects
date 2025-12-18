using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.EmojiRain;

public sealed class EmojiRainEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "emojirain",
        Name = "Emoji Rain",
        Description = "Falling emoji faces from the mouse cursor with rotation and tumble",
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
    private struct EmojiInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (fall + tumble)
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Emoji size
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float RotationAngle;       // 4 bytes - Current rotation = 48
        public float RotationSpeed;       // 4 bytes - Rotation speed
        public int EmojiType;             // 4 bytes - Which emoji face (0-5)
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 64
    }

    // Constants
    private const int MaxEmojis = 500;
    private const int EmojiTypeHappy = 0;
    private const int EmojiTypeSad = 1;
    private const int EmojiTypeWink = 2;
    private const int EmojiTypeHeartEyes = 3;
    private const int EmojiTypeStarEyes = 4;
    private const int EmojiTypeSurprised = 5;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _emojiBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Emoji management (CPU side)
    private readonly EmojiInstance[] _emojis = new EmojiInstance[MaxEmojis];
    private readonly EmojiInstance[] _gpuEmojis = new EmojiInstance[MaxEmojis];
    private int _nextEmojiIndex;
    private int _activeEmojiCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields (er_ prefix for config keys)
    private int _emojiCount = 15;
    private float _fallSpeed = 100f;
    private float _minSize = 20f;
    private float _maxSize = 40f;
    private float _rotationAmount = 2.0f;
    private float _emojiLifetime = 6f;
    private bool _enableHappy = true;
    private bool _enableSad = true;
    private bool _enableWink = true;
    private bool _enableHeartEyes = true;
    private bool _enableStarEyes = true;
    private bool _enableSurprised = true;

    // Public properties for UI binding
    public int EmojiCount { get => _emojiCount; set => _emojiCount = value; }
    public float FallSpeed { get => _fallSpeed; set => _fallSpeed = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float RotationAmount { get => _rotationAmount; set => _rotationAmount = value; }
    public float EmojiLifetime { get => _emojiLifetime; set => _emojiLifetime = value; }
    public bool EnableHappy { get => _enableHappy; set => _enableHappy = value; }
    public bool EnableSad { get => _enableSad; set => _enableSad = value; }
    public bool EnableWink { get => _enableWink; set => _enableWink = value; }
    public bool EnableHeartEyes { get => _enableHeartEyes; set => _enableHeartEyes = value; }
    public bool EnableStarEyes { get => _enableStarEyes; set => _enableStarEyes = value; }
    public bool EnableSurprised { get => _enableSurprised; set => _enableSurprised = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("EmojiRainShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create emoji structured buffer
        _emojiBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<EmojiInstance>() * MaxEmojis,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<EmojiInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("er_emojiCount", out int count))
            _emojiCount = count;
        if (Configuration.TryGet("er_fallSpeed", out float fall))
            _fallSpeed = fall;
        if (Configuration.TryGet("er_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("er_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("er_rotationAmount", out float rotation))
            _rotationAmount = rotation;
        if (Configuration.TryGet("er_lifetime", out float lifetime))
            _emojiLifetime = lifetime;
        if (Configuration.TryGet("er_enableHappy", out bool happy))
            _enableHappy = happy;
        if (Configuration.TryGet("er_enableSad", out bool sad))
            _enableSad = sad;
        if (Configuration.TryGet("er_enableWink", out bool wink))
            _enableWink = wink;
        if (Configuration.TryGet("er_enableHeartEyes", out bool heartEyes))
            _enableHeartEyes = heartEyes;
        if (Configuration.TryGet("er_enableStarEyes", out bool starEyes))
            _enableStarEyes = starEyes;
        if (Configuration.TryGet("er_enableSurprised", out bool surprised))
            _enableSurprised = surprised;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing emojis
        UpdateEmojis(deltaTime, totalTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Spawn emojis continuously when mouse moves
        if (distanceFromLast > 0.1f)
        {
            // Spawn rate based on emoji count setting
            float spawnRate = _emojiCount * 1.5f; // Emojis per second
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnEmoji(mouseState.Position, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateEmojis(float deltaTime, float totalTime)
    {
        _activeEmojiCount = 0;
        for (int i = 0; i < MaxEmojis; i++)
        {
            if (_emojis[i].Lifetime > 0)
            {
                ref var emoji = ref _emojis[i];

                // Age emoji
                emoji.Lifetime -= deltaTime;

                if (emoji.Lifetime > 0)
                {
                    // Apply gravity (downward fall)
                    emoji.Velocity.Y = _fallSpeed;

                    // Update position
                    emoji.Position += emoji.Velocity * deltaTime;

                    // Update rotation (tumble effect)
                    emoji.RotationAngle += emoji.RotationSpeed * deltaTime;

                    // Despawn if fallen below screen
                    if (emoji.Position.Y > 2160f) // Max screen height assumption
                    {
                        emoji.Lifetime = 0f;
                    }

                    _activeEmojiCount++;
                }
            }
        }
    }

    private void SpawnEmoji(Vector2 position, float time)
    {
        ref var emoji = ref _emojis[_nextEmojiIndex];
        _nextEmojiIndex = (_nextEmojiIndex + 1) % MaxEmojis;

        // Random offset around cursor (spawn slightly above cursor)
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = Random.Shared.NextSingle() * 80f;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        // Bias spawn position upward
        offset.Y -= 60f; // Spawn above cursor

        emoji.Position = position + offset;
        emoji.Lifetime = _emojiLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        emoji.MaxLifetime = emoji.Lifetime;

        // Initial velocity (will be overridden in update, but set for first frame)
        emoji.Velocity = new Vector2(0, _fallSpeed);

        // Random size
        emoji.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random rotation and rotation speed (tumble effect)
        emoji.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
        emoji.RotationSpeed = (Random.Shared.NextSingle() - 0.5f) * _rotationAmount * 4f;

        // Yellow emoji face color
        emoji.Color = new Vector4(1f, 0.9f, 0.1f, 1f); // Classic emoji yellow

        // Random emoji type from enabled types
        emoji.EmojiType = GetRandomEmojiType();
        emoji.Padding1 = 0f;
        emoji.Padding2 = 0f;
    }

    private int GetRandomEmojiType()
    {
        var availableTypes = new List<int>();
        if (_enableHappy) availableTypes.Add(EmojiTypeHappy);
        if (_enableSad) availableTypes.Add(EmojiTypeSad);
        if (_enableWink) availableTypes.Add(EmojiTypeWink);
        if (_enableHeartEyes) availableTypes.Add(EmojiTypeHeartEyes);
        if (_enableStarEyes) availableTypes.Add(EmojiTypeStarEyes);
        if (_enableSurprised) availableTypes.Add(EmojiTypeSurprised);

        if (availableTypes.Count == 0)
            return EmojiTypeHappy; // Fallback to happy

        return availableTypes[Random.Shared.Next(availableTypes.Count)];
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeEmojiCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU emoji buffer - only include alive emojis
        int gpuIndex = 0;
        for (int i = 0; i < MaxEmojis && gpuIndex < MaxEmojis; i++)
        {
            if (_emojis[i].Lifetime > 0)
            {
                _gpuEmojis[gpuIndex++] = _emojis[i];
            }
        }

        // Fill remaining with zeroed emojis
        for (int i = gpuIndex; i < MaxEmojis; i++)
        {
            _gpuEmojis[i] = default;
        }

        // Update emoji buffer
        context.UpdateBuffer(_emojiBuffer!, (ReadOnlySpan<EmojiInstance>)_gpuEmojis.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _emojiBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _emojiBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced emojis (6 vertices per quad, one instance per emoji)
        context.DrawInstanced(6, MaxEmojis, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _emojiBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.EmojiRain.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
