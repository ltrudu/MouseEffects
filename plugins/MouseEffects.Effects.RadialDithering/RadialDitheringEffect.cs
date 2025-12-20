using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.RadialDithering;

/// <summary>
/// Radial dithering effect that creates a Bayer-pattern dithering effect around the mouse cursor.
/// Captures the screen and applies dithering shader in real-time.
/// </summary>
public sealed class RadialDitheringEffect : EffectBase
{
    private const float DefaultRadius = 200.0f;
    private const float DefaultIntensity = 0.5f;
    private const float DefaultPatternScale = 2.0f;
    private const float DefaultAnimationSpeed = 1.0f;
    private const float DefaultEdgeSoftness = 0.3f;
    private const bool DefaultEnableAnimation = false;
    private const bool DefaultInvertPattern = false;
    private const int DefaultFalloffType = 1; // Smooth
    private const float DefaultRingWidth = 0.3f;
    private const bool DefaultEnableGlow = false;
    private const float DefaultGlowIntensity = 0.3f;
    private const int DefaultColorBlendMode = 0; // Replace
    private const float DefaultThreshold = 0.0f;
    private const bool DefaultEnableNoise = false;
    private const float DefaultNoiseAmount = 0.2f;
    private const float DefaultAlpha = 1.0f;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "radial-dithering",
        Name = "Radial Dithering",
        Description = "Creates a Bayer-pattern dithering effect in a circular area around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.VisualFilter
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;

    // Effect parameters
    private float _radius = DefaultRadius;
    private float _intensity = DefaultIntensity;
    private float _patternScale = DefaultPatternScale;
    private float _animationSpeed = DefaultAnimationSpeed;
    private float _edgeSoftness = DefaultEdgeSoftness;
    private bool _enableAnimation = DefaultEnableAnimation;
    private bool _invertPattern = DefaultInvertPattern;
    private Vector4 _color1 = new(1.0f, 1.0f, 1.0f, 1.0f); // White
    private Vector4 _color2 = new(0.0f, 0.0f, 0.0f, 1.0f); // Black
    private int _falloffType = DefaultFalloffType;
    private float _ringWidth = DefaultRingWidth;
    private bool _enableGlow = DefaultEnableGlow;
    private float _glowIntensity = DefaultGlowIntensity;
    private Vector4 _glowColor = new(0.3f, 0.5f, 1.0f, 1.0f); // Blue
    private int _colorBlendMode = DefaultColorBlendMode;
    private float _threshold = DefaultThreshold;
    private bool _enableNoise = DefaultEnableNoise;
    private float _noiseAmount = DefaultNoiseAmount;
    private float _alpha = DefaultAlpha;
    private float _time;
    private Vector2 _mousePosition;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// RadialDithering effect requires screen capture to apply dithering to screen content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("RadialDithering.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<DitheringParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create linear sampler for texture sampling
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;

        if (Configuration.TryGet("intensity", out float intensity))
            _intensity = intensity;

        if (Configuration.TryGet("patternScale", out float patternScale))
            _patternScale = patternScale;

        if (Configuration.TryGet("animationSpeed", out float animSpeed))
            _animationSpeed = animSpeed;

        if (Configuration.TryGet("edgeSoftness", out float edgeSoftness))
            _edgeSoftness = edgeSoftness;

        if (Configuration.TryGet("enableAnimation", out bool enableAnim))
            _enableAnimation = enableAnim;

        if (Configuration.TryGet("invertPattern", out bool invertPattern))
            _invertPattern = invertPattern;

        if (Configuration.TryGet("color1", out Vector4 color1))
            _color1 = color1;

        if (Configuration.TryGet("color2", out Vector4 color2))
            _color2 = color2;

        if (Configuration.TryGet("falloffType", out int falloffType))
            _falloffType = falloffType;

        if (Configuration.TryGet("ringWidth", out float ringWidth))
            _ringWidth = ringWidth;

        if (Configuration.TryGet("enableGlow", out bool enableGlow))
            _enableGlow = enableGlow;

        if (Configuration.TryGet("glowIntensity", out float glowIntensity))
            _glowIntensity = glowIntensity;

        if (Configuration.TryGet("glowColor", out Vector4 glowColor))
            _glowColor = glowColor;

        if (Configuration.TryGet("colorBlendMode", out int blendMode))
            _colorBlendMode = blendMode;

        if (Configuration.TryGet("threshold", out float threshold))
            _threshold = threshold;

        if (Configuration.TryGet("enableNoise", out bool enableNoise))
            _enableNoise = enableNoise;

        if (Configuration.TryGet("noiseAmount", out float noiseAmount))
            _noiseAmount = noiseAmount;

        if (Configuration.TryGet("alpha", out float alpha))
            _alpha = alpha;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _time += (float)gameTime.DeltaTime.TotalSeconds;
        _mousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Get the screen texture from context
        var screenTexture = context.ScreenTexture;
        if (screenTexture == null)
        {
            return;
        }

        // Update parameters
        var ditheringParams = new DitheringParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            Radius = _radius,
            Intensity = _intensity,
            PatternScale = _patternScale,
            Time = _time,
            AnimationSpeed = _animationSpeed,
            EdgeSoftness = _edgeSoftness,
            EnableAnimation = _enableAnimation ? 1.0f : 0.0f,
            InvertPattern = _invertPattern ? 1.0f : 0.0f,
            Color1 = _color1,
            Color2 = _color2,
            FalloffType = _falloffType,
            RingWidth = _ringWidth,
            EnableGlow = _enableGlow ? 1.0f : 0.0f,
            GlowIntensity = _glowIntensity,
            GlowColor = _glowColor,
            ColorBlendMode = _colorBlendMode,
            Threshold = _threshold,
            EnableNoise = _enableNoise ? 1.0f : 0.0f,
            NoiseAmount = _noiseAmount,
            Alpha = _alpha
        };

        context.UpdateBuffer(_paramsBuffer!, ditheringParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Enable alpha blending
        context.SetBlendState(BlendMode.Alpha);

        // Draw fullscreen quad (vertices generated procedurally in shader)
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind screen texture
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        // No texture recreation needed - we use the screen capture
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _paramsBuffer?.Dispose();
        _linearSampler?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(RadialDitheringEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.RadialDithering.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 144)]
    private struct DitheringParams
    {
        // Must match HLSL cbuffer layout exactly!
        // HLSL float4 requires 16-byte alignment
        // Total size: 144 bytes (9 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float Radius;               // 4 bytes, offset 16
        public float Intensity;            // 4 bytes, offset 20
        public float PatternScale;         // 4 bytes, offset 24
        public float Time;                 // 4 bytes, offset 28
        public float AnimationSpeed;       // 4 bytes, offset 32
        public float EdgeSoftness;         // 4 bytes, offset 36
        public float EnableAnimation;      // 4 bytes, offset 40
        public float InvertPattern;        // 4 bytes, offset 44
        public Vector4 Color1;             // 16 bytes, offset 48 (aligned)
        public Vector4 Color2;             // 16 bytes, offset 64 (aligned)
        public float FalloffType;          // 4 bytes, offset 80
        public float RingWidth;            // 4 bytes, offset 84
        public float EnableGlow;           // 4 bytes, offset 88
        public float GlowIntensity;        // 4 bytes, offset 92
        public Vector4 GlowColor;          // 16 bytes, offset 96 (aligned)
        public float ColorBlendMode;       // 4 bytes, offset 112
        public float Threshold;            // 4 bytes, offset 116
        public float EnableNoise;          // 4 bytes, offset 120
        public float NoiseAmount;          // 4 bytes, offset 124
        public float Alpha;                // 4 bytes, offset 128
        private float _padding1;           // 4 bytes, offset 132
        private float _padding2;           // 4 bytes, offset 136
        private float _padding3;           // 4 bytes, offset 140
    }

    #endregion
}
