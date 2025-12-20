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

    /// <summary>
    /// Enable screen capture when diffraction effect is active and there are bubbles.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => _diffractionEnabled && _activeBubbleCount > 0;

    // Animation types
    private enum AppearsAnimation { None = 0, FadeIn = 1, ZoomIn = 2 }
    private enum DisappearsAnimation { None = 0, FadeOut = 1, ZoomOut = 2, PopOut = 3 }

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 16
        public float DiffractionEnabled;  // 4 bytes (0 or 1)
        public float DiffractionStrength; // 4 bytes
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 32
    }

    [StructLayout(LayoutKind.Sequential, Size = 112)]
    private struct BubbleInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (float + drift)
        public Vector4 BaseColor;         // 16 bytes - Base tint color = 32
        public float Size;                // 4 bytes - Bubble radius (base size)
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
        public float AlphaMultiplier;     // 4 bytes - Animation alpha multiplier (appears/disappears)
        public float ScaleMultiplier;     // 4 bytes - Animation scale multiplier (appears/disappears) = 96
        public float AppearProgress;      // 4 bytes - Appear animation progress (0-1)
        public float DisappearProgress;   // 4 bytes - Disappear animation progress (0-1)
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 112
    }

    // Constants
    private const int HardMaxBubbles = 500; // Maximum buffer allocation
    private int _maxBubbles = 150; // Configurable limit

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _bubbleBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private ISamplerState? _linearSampler;

    // Bubble management (CPU side)
    private readonly BubbleInstance[] _bubbles = new BubbleInstance[HardMaxBubbles];
    private readonly BubbleInstance[] _gpuBubbles = new BubbleInstance[HardMaxBubbles];
    private int _nextBubbleIndex;
    private int _activeBubbleCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields (b_ prefix for config keys)
    private int _bubbleCount = 10;
    private float _minSize = 15f;
    private float _maxSize = 35f;
    private float _floatSpeed = 27f;
    private float _wobbleAmount = 15f;
    private float _wobbleFrequency = 1.36f;
    private float _driftSpeed = 20f;
    private float _iridescenceIntensity = 1.3f;
    private float _iridescenceSpeed = 0.5f;
    private float _bubbleLifetime = 15f;
    private float _transparency = 1.0f;
    private float _rimThickness = 0.088f;

    // Diffraction settings
    private bool _diffractionEnabled = true;
    private float _diffractionStrength = 0.4f;

    // Animation settings - Appears
    private AppearsAnimation _appearsAnimation = AppearsAnimation.ZoomIn;
    private float _fadeInSpeed = 0.5f;
    private float _fadeInStartAlpha = 0f;
    private float _fadeInEndAlpha = 1f;
    private float _zoomInSpeed = 0.5f;
    private float _zoomInStartScale = 0f;
    private float _zoomInEndScale = 1.1f;

    // Animation settings - Disappears
    private DisappearsAnimation _disappearsAnimation = DisappearsAnimation.PopOut;
    private float _fadeOutSpeed = 0.5f;
    private float _fadeOutStartAlpha = 1f;
    private float _fadeOutEndAlpha = 0f;
    private float _zoomOutSpeed = 0.5f;
    private float _zoomOutStartScale = 1f;
    private float _zoomOutEndScale = 0f;
    private float _popDuration = 0.24f;

    // Public properties for UI binding
    public int MaxBubbles { get => _maxBubbles; set => _maxBubbles = Math.Clamp(value, 1, HardMaxBubbles); }
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
    public float Transparency { get => _transparency; set => _transparency = value; }
    public float RimThickness { get => _rimThickness; set => _rimThickness = value; }

    // Diffraction properties
    public bool DiffractionEnabled { get => _diffractionEnabled; set => _diffractionEnabled = value; }
    public float DiffractionStrength { get => _diffractionStrength; set => _diffractionStrength = value; }

    // Animation properties
    public int AppearsAnimationType { get => (int)_appearsAnimation; set => _appearsAnimation = (AppearsAnimation)value; }
    public float FadeInSpeed { get => _fadeInSpeed; set => _fadeInSpeed = value; }
    public float FadeInStartAlpha { get => _fadeInStartAlpha; set => _fadeInStartAlpha = value; }
    public float FadeInEndAlpha { get => _fadeInEndAlpha; set => _fadeInEndAlpha = value; }
    public float ZoomInSpeed { get => _zoomInSpeed; set => _zoomInSpeed = value; }
    public float ZoomInStartScale { get => _zoomInStartScale; set => _zoomInStartScale = value; }
    public float ZoomInEndScale { get => _zoomInEndScale; set => _zoomInEndScale = value; }

    public int DisappearsAnimationType { get => (int)_disappearsAnimation; set => _disappearsAnimation = (DisappearsAnimation)value; }
    public float FadeOutSpeed { get => _fadeOutSpeed; set => _fadeOutSpeed = value; }
    public float FadeOutStartAlpha { get => _fadeOutStartAlpha; set => _fadeOutStartAlpha = value; }
    public float FadeOutEndAlpha { get => _fadeOutEndAlpha; set => _fadeOutEndAlpha = value; }
    public float ZoomOutSpeed { get => _zoomOutSpeed; set => _zoomOutSpeed = value; }
    public float ZoomOutStartScale { get => _zoomOutStartScale; set => _zoomOutStartScale = value; }
    public float ZoomOutEndScale { get => _zoomOutEndScale; set => _zoomOutEndScale = value; }
    public float PopDuration { get => _popDuration; set => _popDuration = value; }

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
            Size = Marshal.SizeOf<BubbleInstance>() * HardMaxBubbles,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<BubbleInstance>()
        });

        // Create sampler for screen texture (used in diffraction mode)
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
    }

    protected override void OnConfigurationChanged()
    {
        // Max bubbles setting
        if (Configuration.TryGet("b_maxBubbles", out int maxBubbles))
            _maxBubbles = Math.Clamp(maxBubbles, 1, HardMaxBubbles);

        // Bubble settings
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
        if (Configuration.TryGet("b_transparency", out float trans))
            _transparency = trans;
        if (Configuration.TryGet("b_rimThickness", out float rim))
            _rimThickness = rim;

        // Diffraction settings
        if (Configuration.TryGet("b_diffractionEnabled", out bool diffractionEnabled))
            _diffractionEnabled = diffractionEnabled;
        if (Configuration.TryGet("b_diffractionStrength", out float diffractionStrength))
            _diffractionStrength = diffractionStrength;

        // Appears animation settings
        if (Configuration.TryGet("b_appearsAnimation", out int appearsAnim))
            _appearsAnimation = (AppearsAnimation)appearsAnim;
        if (Configuration.TryGet("b_fadeInSpeed", out float fadeInSpeed))
            _fadeInSpeed = fadeInSpeed;
        if (Configuration.TryGet("b_fadeInStartAlpha", out float fadeInStartAlpha))
            _fadeInStartAlpha = fadeInStartAlpha;
        if (Configuration.TryGet("b_fadeInEndAlpha", out float fadeInEndAlpha))
            _fadeInEndAlpha = fadeInEndAlpha;
        if (Configuration.TryGet("b_zoomInSpeed", out float zoomInSpeed))
            _zoomInSpeed = zoomInSpeed;
        if (Configuration.TryGet("b_zoomInStartScale", out float zoomInStartScale))
            _zoomInStartScale = zoomInStartScale;
        if (Configuration.TryGet("b_zoomInEndScale", out float zoomInEndScale))
            _zoomInEndScale = zoomInEndScale;

        // Disappears animation settings
        if (Configuration.TryGet("b_disappearsAnimation", out int disappearsAnim))
            _disappearsAnimation = (DisappearsAnimation)disappearsAnim;
        if (Configuration.TryGet("b_fadeOutSpeed", out float fadeOutSpeed))
            _fadeOutSpeed = fadeOutSpeed;
        if (Configuration.TryGet("b_fadeOutStartAlpha", out float fadeOutStartAlpha))
            _fadeOutStartAlpha = fadeOutStartAlpha;
        if (Configuration.TryGet("b_fadeOutEndAlpha", out float fadeOutEndAlpha))
            _fadeOutEndAlpha = fadeOutEndAlpha;
        if (Configuration.TryGet("b_zoomOutSpeed", out float zoomOutSpeed))
            _zoomOutSpeed = zoomOutSpeed;
        if (Configuration.TryGet("b_zoomOutStartScale", out float zoomOutStartScale))
            _zoomOutStartScale = zoomOutStartScale;
        if (Configuration.TryGet("b_zoomOutEndScale", out float zoomOutEndScale))
            _zoomOutEndScale = zoomOutEndScale;
        if (Configuration.TryGet("b_popDuration", out float popDur))
            _popDuration = popDur;
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
        for (int i = 0; i < HardMaxBubbles; i++)
        {
            if (_bubbles[i].Lifetime > 0)
            {
                ref var bubble = ref _bubbles[i];

                // Age bubble
                bubble.Lifetime -= deltaTime;

                if (bubble.Lifetime > 0)
                {
                    // Calculate appear animation progress
                    float appearDuration = _appearsAnimation switch
                    {
                        AppearsAnimation.FadeIn => _fadeInSpeed,
                        AppearsAnimation.ZoomIn => _zoomInSpeed,
                        _ => 0f
                    };

                    if (appearDuration > 0f && bubble.AppearProgress < 1f)
                    {
                        bubble.AppearProgress += deltaTime / appearDuration;
                        bubble.AppearProgress = MathF.Min(bubble.AppearProgress, 1f);
                    }

                    // Calculate disappear trigger time based on animation type
                    float disappearDuration = _disappearsAnimation switch
                    {
                        DisappearsAnimation.FadeOut => _fadeOutSpeed,
                        DisappearsAnimation.ZoomOut => _zoomOutSpeed,
                        DisappearsAnimation.PopOut => _popDuration,
                        _ => 0f
                    };

                    // Start disappear animation when lifetime remaining is less than animation duration
                    bool shouldStartDisappear = disappearDuration > 0f && bubble.Lifetime <= disappearDuration;

                    if (shouldStartDisappear && bubble.DisappearProgress == 0f)
                    {
                        bubble.DisappearProgress = 0.001f; // Start disappear animation
                        if (_disappearsAnimation == DisappearsAnimation.PopOut)
                        {
                            bubble.PopProgress = 0.001f;
                        }
                    }

                    // Update disappear animation progress
                    if (bubble.DisappearProgress > 0f && disappearDuration > 0f)
                    {
                        bubble.DisappearProgress += deltaTime / disappearDuration;
                        if (_disappearsAnimation == DisappearsAnimation.PopOut)
                        {
                            bubble.PopProgress = bubble.DisappearProgress;
                        }
                        if (bubble.DisappearProgress >= 1f)
                        {
                            bubble.Lifetime = 0f; // Bubble finished disappearing
                            continue;
                        }
                    }

                    // Calculate alpha and scale multipliers based on animation state
                    CalculateAnimationMultipliers(ref bubble);

                    // Apply upward float
                    bubble.Velocity.Y = -bubble.FloatSpeed; // Negative = upward

                    // Apply horizontal drift (gentle side movement)
                    float driftWave = MathF.Sin(totalTime * 0.5f + bubble.WobblePhase);
                    bubble.Velocity.X = driftWave * bubble.DriftSpeed;

                    // Update position
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

    private void CalculateAnimationMultipliers(ref BubbleInstance bubble)
    {
        float alphaMultiplier = 1f;
        float scaleMultiplier = 1f;

        // Apply appear animation
        if (bubble.AppearProgress < 1f)
        {
            float t = bubble.AppearProgress;
            switch (_appearsAnimation)
            {
                case AppearsAnimation.FadeIn:
                    alphaMultiplier = _fadeInStartAlpha + (_fadeInEndAlpha - _fadeInStartAlpha) * t;
                    break;
                case AppearsAnimation.ZoomIn:
                    scaleMultiplier = _zoomInStartScale + (_zoomInEndScale - _zoomInStartScale) * t;
                    break;
            }
        }

        // Apply disappear animation (overrides appear if both active, but appear should be done by then)
        if (bubble.DisappearProgress > 0f)
        {
            float t = MathF.Min(bubble.DisappearProgress, 1f);
            switch (_disappearsAnimation)
            {
                case DisappearsAnimation.FadeOut:
                    alphaMultiplier = _fadeOutStartAlpha + (_fadeOutEndAlpha - _fadeOutStartAlpha) * t;
                    break;
                case DisappearsAnimation.ZoomOut:
                    scaleMultiplier = _zoomOutStartScale + (_zoomOutEndScale - _zoomOutStartScale) * t;
                    break;
                case DisappearsAnimation.PopOut:
                    // Pop is handled by PopProgress in shader - scale expands then alpha drops
                    // Keep multipliers at 1, the shader handles pop animation
                    break;
            }
        }

        bubble.AlphaMultiplier = alphaMultiplier;
        bubble.ScaleMultiplier = scaleMultiplier;
    }

    private void SpawnBubble(Vector2 position, float time)
    {
        // Check if we've reached the max bubbles limit
        if (_activeBubbleCount >= _maxBubbles)
            return;

        ref var bubble = ref _bubbles[_nextBubbleIndex];
        _nextBubbleIndex = (_nextBubbleIndex + 1) % HardMaxBubbles;

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

        // Animation progress - start at 0
        bubble.AppearProgress = _appearsAnimation != AppearsAnimation.None ? 0f : 1f;
        bubble.DisappearProgress = 0f;
        bubble.PopProgress = 0f;

        // Visual properties
        bubble.RimThickness = _rimThickness * (0.8f + Random.Shared.NextSingle() * 0.4f);
        bubble.Transparency = _transparency * (0.9f + Random.Shared.NextSingle() * 0.2f);
        bubble.HighlightIntensity = _iridescenceIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Initial animation multipliers
        bubble.AlphaMultiplier = _appearsAnimation == AppearsAnimation.FadeIn ? _fadeInStartAlpha : 1f;
        bubble.ScaleMultiplier = _appearsAnimation == AppearsAnimation.ZoomIn ? _zoomInStartScale : 1f;

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
        for (int i = 0; i < HardMaxBubbles && gpuIndex < HardMaxBubbles; i++)
        {
            if (_bubbles[i].Lifetime > 0)
            {
                _gpuBubbles[gpuIndex++] = _bubbles[i];
            }
        }

        // Fill remaining with zeroed bubbles
        for (int i = gpuIndex; i < HardMaxBubbles; i++)
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
            DiffractionEnabled = _diffractionEnabled ? 1f : 0f,
            DiffractionStrength = _diffractionStrength,
            Padding1 = 0f,
            Padding2 = 0f
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _bubbleBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _bubbleBuffer!);

        // Set screen texture and sampler when diffraction is enabled
        if (_diffractionEnabled && context.ScreenTexture != null)
        {
            context.SetShaderResource(ShaderStage.Pixel, 1, context.ScreenTexture);
            context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);
        }

        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced bubbles (6 vertices per quad, one instance per bubble)
        context.DrawInstanced(6, HardMaxBubbles, 0, 0);

        // Unbind screen texture if we used it
        if (_diffractionEnabled)
        {
            context.SetShaderResource(ShaderStage.Pixel, 1, (ITexture?)null);
        }

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _bubbleBuffer?.Dispose();
        _linearSampler?.Dispose();
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
