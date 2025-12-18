using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Bubbles;

public sealed class BubblesEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "bubbles",
        Name = "Bubbles",
        Description = "Floating soap bubbles with rainbow iridescence following the mouse cursor",
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

    [StructLayout(LayoutKind.Sequential, Size = 96)]
    private struct BubbleInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (float + drift)
        public Vector4 BaseColor;         // 16 bytes - Base tint color = 32
        public float Size;                // 4 bytes - Bubble radius
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float IridescencePhase;    // 4 bytes - Phase for iridescence shift = 48
        public float IridescenceSpeed;    // 4 bytes - How fast colors shift
        public float WobblePhase;         // 4 bytes - Phase for wobble oscillation
        public float WobbleAmplitudeX;    // 4 bytes - Horizontal wobble strength
        public float WobbleAmplitudeY;    // 4 bytes - Vertical wobble strength = 64
        public float FloatSpeed;          // 4 bytes - Individual float speed
        public float DriftSpeed;          // 4 bytes - Horizontal drift speed
        public float PopProgress;         // 4 bytes - Pop animation progress (0-1, 0=no pop)
        public float RimThickness;        // 4 bytes - Thickness of bubble rim = 80
        public float Transparency;        // 4 bytes - Overall transparency
        public float HighlightIntensity;  // 4 bytes - Reflection highlight strength
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 96
    }

    // Constants
    private const int MaxBubbles = 300;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _bubbleBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Bubble management (CPU side)
    private readonly BubbleInstance[] _bubbles = new BubbleInstance[MaxBubbles];
    private readonly BubbleInstance[] _gpuBubbles = new BubbleInstance[MaxBubbles];
    private int _nextBubbleIndex;
    private int _activeBubbleCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields (b_ prefix for config keys)
    private int _bubbleCount = 10;
    private float _minSize = 15f;
    private float _maxSize = 35f;
    private float _floatSpeed = 25f;
    private float _wobbleAmount = 15f;
    private float _wobbleFrequency = 1.5f;
    private float _driftSpeed = 20f;
    private float _iridescenceIntensity = 1.0f;
    private float _iridescenceSpeed = 0.5f;
    private float _bubbleLifetime = 12f;
    private bool _popEnabled = true;
    private float _popDuration = 0.3f;
    private float _transparency = 0.7f;
    private float _rimThickness = 0.08f;

    // Public properties for UI binding
    public int BubbleCount { get => _bubbleCount; set => _bubbleCount = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float FloatSpeed { get => _floatSpeed; set => _floatSpeed = value; }
    public float WobbleAmount { get => _wobbleAmount; set => _wobbleAmount = value; }
    public float WobbleFrequency { get => _wobbleFrequency; set => _wobbleFrequency = value; }
    public float DriftSpeed { get => _driftSpeed; set => _driftSpeed = value; }
    public float IridescenceIntensity { get => _iridescenceIntensity; set => _iridescenceIntensity = value; }
    public float IridescenceSpeed { get => _iridescenceSpeed; set => _iridescenceSpeed = value; }
    public float BubbleLifetime { get => _bubbleLifetime; set => _bubbleLifetime = value; }
    public bool PopEnabled { get => _popEnabled; set => _popEnabled = value; }
    public float PopDuration { get => _popDuration; set => _popDuration = value; }
    public float Transparency { get => _transparency; set => _transparency = value; }
    public float RimThickness { get => _rimThickness; set => _rimThickness = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("BubblesShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create bubble structured buffer
        _bubbleBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<BubbleInstance>() * MaxBubbles,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<BubbleInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("b_bubbleCount", out int count))
            _bubbleCount = count;
        if (Configuration.TryGet("b_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("b_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("b_floatSpeed", out float floatSpd))
            _floatSpeed = floatSpd;
        if (Configuration.TryGet("b_wobbleAmount", out float wobble))
            _wobbleAmount = wobble;
        if (Configuration.TryGet("b_wobbleFrequency", out float freq))
            _wobbleFrequency = freq;
        if (Configuration.TryGet("b_driftSpeed", out float drift))
            _driftSpeed = drift;
        if (Configuration.TryGet("b_iridescenceIntensity", out float iridInt))
            _iridescenceIntensity = iridInt;
        if (Configuration.TryGet("b_iridescenceSpeed", out float iridSpd))
            _iridescenceSpeed = iridSpd;
        if (Configuration.TryGet("b_lifetime", out float lifetime))
            _bubbleLifetime = lifetime;
        if (Configuration.TryGet("b_popEnabled", out bool popEnabled))
            _popEnabled = popEnabled;
        if (Configuration.TryGet("b_popDuration", out float popDur))
            _popDuration = popDur;
        if (Configuration.TryGet("b_transparency", out float trans))
            _transparency = trans;
        if (Configuration.TryGet("b_rimThickness", out float rim))
            _rimThickness = rim;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing bubbles
        UpdateBubbles(deltaTime, totalTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Spawn bubbles continuously when mouse moves
        if (distanceFromLast > 0.1f)
        {
            // Spawn rate based on bubble count setting
            float spawnRate = _bubbleCount * 0.8f; // Bubbles per second
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnBubble(mouseState.Position, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateBubbles(float deltaTime, float totalTime)
    {
        _activeBubbleCount = 0;
        for (int i = 0; i < MaxBubbles; i++)
        {
            if (_bubbles[i].Lifetime > 0)
            {
                ref var bubble = ref _bubbles[i];

                // Age bubble
                bubble.Lifetime -= deltaTime;

                if (bubble.Lifetime > 0)
                {
                    // Check if bubble should start popping
                    if (_popEnabled && bubble.Lifetime <= _popDuration && bubble.PopProgress == 0f)
                    {
                        bubble.PopProgress = 0.001f; // Start pop animation
                    }

                    // Update pop animation
                    if (bubble.PopProgress > 0f)
                    {
                        bubble.PopProgress += deltaTime / _popDuration;
                        if (bubble.PopProgress >= 1f)
                        {
                            bubble.Lifetime = 0f; // Bubble popped
                            continue;
                        }
                    }

                    // Apply upward float
                    bubble.Velocity.Y = -bubble.FloatSpeed; // Negative = upward

                    // Apply horizontal drift (gentle side movement)
                    float driftWave = MathF.Sin(totalTime * 0.5f + bubble.WobblePhase);
                    bubble.Velocity.X = driftWave * bubble.DriftSpeed;

                    // Apply wobble effect (oscillating position offset)
                    float wobbleX = MathF.Sin(totalTime * _wobbleFrequency + bubble.WobblePhase) * bubble.WobbleAmplitudeX;
                    float wobbleY = MathF.Cos(totalTime * _wobbleFrequency * 1.3f + bubble.WobblePhase * 1.7f) * bubble.WobbleAmplitudeY;

                    // Update position (velocity + wobble offset applied in shader via phase)
                    bubble.Position += bubble.Velocity * deltaTime;

                    // Update iridescence phase for color shifting
                    bubble.IridescencePhase += bubble.IridescenceSpeed * deltaTime * _iridescenceSpeed;

                    // Despawn if floated above screen
                    if (bubble.Position.Y < -100f)
                    {
                        bubble.Lifetime = 0f;
                    }

                    _activeBubbleCount++;
                }
            }
        }
    }

    private void SpawnBubble(Vector2 position, float time)
    {
        ref var bubble = ref _bubbles[_nextBubbleIndex];
        _nextBubbleIndex = (_nextBubbleIndex + 1) % MaxBubbles;

        // Random offset around cursor (spawn below and around cursor)
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = Random.Shared.NextSingle() * 40f;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        bubble.Position = position + offset;
        bubble.Lifetime = _bubbleLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        bubble.MaxLifetime = bubble.Lifetime;

        // Initial velocity (will be overridden in update)
        bubble.Velocity = new Vector2(0, -_floatSpeed);

        // Random size (variety of small and large bubbles)
        bubble.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random wobble phase for varied oscillation
        bubble.WobblePhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        bubble.WobbleAmplitudeX = _wobbleAmount * (0.7f + Random.Shared.NextSingle() * 0.6f);
        bubble.WobbleAmplitudeY = _wobbleAmount * 0.5f * (0.7f + Random.Shared.NextSingle() * 0.6f);

        // Random float and drift speed variation
        bubble.FloatSpeed = _floatSpeed * (0.8f + Random.Shared.NextSingle() * 0.4f);
        bubble.DriftSpeed = _driftSpeed * (0.7f + Random.Shared.NextSingle() * 0.6f);

        // Iridescence settings
        bubble.IridescencePhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        bubble.IridescenceSpeed = 0.8f + Random.Shared.NextSingle() * 0.4f;

        // Pop settings
        bubble.PopProgress = 0f; // Not popping yet

        // Visual properties
        bubble.RimThickness = _rimThickness * (0.8f + Random.Shared.NextSingle() * 0.4f);
        bubble.Transparency = _transparency * (0.9f + Random.Shared.NextSingle() * 0.2f);
        bubble.HighlightIntensity = _iridescenceIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Base color (slight tint variation)
        float tint = Random.Shared.NextSingle();
        bubble.BaseColor = new Vector4(
            0.95f + tint * 0.05f,
            0.95f + tint * 0.05f,
            1.0f,
            1f
        );

        bubble.Padding1 = 0f;
        bubble.Padding2 = 0f;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeBubbleCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU bubble buffer - only include alive bubbles
        int gpuIndex = 0;
        for (int i = 0; i < MaxBubbles && gpuIndex < MaxBubbles; i++)
        {
            if (_bubbles[i].Lifetime > 0)
            {
                _gpuBubbles[gpuIndex++] = _bubbles[i];
            }
        }

        // Fill remaining with zeroed bubbles
        for (int i = gpuIndex; i < MaxBubbles; i++)
        {
            _gpuBubbles[i] = default;
        }

        // Update bubble buffer
        context.UpdateBuffer(_bubbleBuffer!, (ReadOnlySpan<BubbleInstance>)_gpuBubbles.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _bubbleBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _bubbleBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced bubbles (6 vertices per quad, one instance per bubble)
        context.DrawInstanced(6, MaxBubbles, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _bubbleBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Bubbles.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
