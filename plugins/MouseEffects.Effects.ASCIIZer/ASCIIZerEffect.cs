using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ASCIIZer;

/// <summary>
/// ASCIIZer effect that renders the screen as ASCII art with multiple filter styles.
/// </summary>
public sealed class ASCIIZerEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "asciizer",
        Name = "ASCIIZer",
        Description = "Renders the screen as ASCII art with multiple filter styles",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;
    private ISamplerState? _pointSampler;

    // Character atlas
    private readonly CharacterAtlas _atlas = new();

    // Animation time and mouse position
    private float _totalTime;
    private Vector2 _mousePosition;

    // Global settings
    private FilterType _filterType = FilterType.ASCIIClassic;
    private bool _advancedMode;

    // Basic settings
    private int _layoutMode;
    private float _radius = 200f;
    private float _rectWidth = 400f;
    private float _rectHeight = 300f;
    private float _cellWidth = 8f;
    private float _cellHeight = 16f;
    private int _charsetPreset;
    private string _customCharset = "";
    private int _colorMode;
    private Vector4 _foreground = new(0f, 1f, 0f, 1f);
    private Vector4 _background = new(0f, 0f, 0f, 1f);

    // Advanced: Character settings
    private int _fontFamily;
    private int _fontWeight;
    private float _charSpacing;
    private float _lineSpacing;
    private bool _aspectCorrection = true;

    // Advanced: Color settings
    private int _paletteType;
    private float _saturation = 1f;
    private int _quantizeLevels = 256;
    private bool _preserveLuminance;

    // Advanced: Brightness & Contrast
    private float _brightness;
    private float _contrast = 1f;
    private float _gamma = 1f;
    private bool _invert;
    private bool _autoContrast;

    // Advanced: Cell Sampling
    private bool _lockAspect = true;
    private int _sampleMode;
    private float _sampleRadius = 0.5f;

    // Advanced: Visual Effects
    private bool _scanlines;
    private float _scanlineIntensity = 0.3f;
    private int _scanlineSpacing = 2;
    private bool _crtCurvature;
    private float _crtAmount = 0.1f;
    private bool _phosphorGlow;
    private float _phosphorRadius = 2f;
    private float _phosphorIntensity = 0.5f;
    private bool _chromatic;
    private float _chromaticOffset = 1f;
    private bool _vignette;
    private float _vignetteIntensity = 0.3f;
    private float _vignetteRadius = 0.8f;
    private bool _noise;
    private float _noiseAmount = 0.1f;
    private bool _flicker;
    private float _flickerSpeed = 1f;

    // Advanced: Character Rendering
    private int _antialiasing;
    private bool _charShadow;
    private Vector2 _shadowOffset = new(1f, 1f);
    private Vector4 _shadowColor = new(0f, 0f, 0f, 0.5f);
    private bool _charOutline;
    private float _outlineThickness = 1f;
    private Vector4 _outlineColor = new(0f, 0f, 0f, 1f);
    private bool _glowOnBright;
    private float _glowThreshold = 0.8f;
    private float _glowRadius = 3f;
    private bool _gridLines;
    private float _gridThickness = 1f;
    private Vector4 _gridColor = new(0.2f, 0.2f, 0.2f, 1f);

    // Advanced: Edge & Shape
    private float _edgeSoftness = 20f;
    private float _shapeFeather;
    private bool _innerGlow;
    private Vector4 _innerGlowColor = new(1f, 1f, 1f, 0.3f);
    private float _innerGlowSize = 10f;

    public override EffectMetadata Metadata => _metadata;

    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ASCIIClassic.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<ASCIIClassicParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create samplers
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
        _pointSampler = context.CreateSamplerState(SamplerDescription.PointClamp);

        // Create initial character atlas
        UpdateCharacterAtlas(context);
    }

    private void UpdateCharacterAtlas(IRenderContext context)
    {
        string charset = _charsetPreset == 4 && !string.IsNullOrEmpty(_customCharset)
            ? _customCharset
            : CharacterAtlas.GetCharsetPreset(_charsetPreset);

        string fontName = _fontFamily switch
        {
            0 => "Consolas",
            1 => "Courier New",
            2 => "Lucida Console",
            3 => "Terminal",
            _ => "Consolas"
        };

        bool bold = _fontWeight == 1;
        int cellW = (int)_cellWidth;
        int cellH = (int)_cellHeight;

        _atlas.CreateOrUpdateAtlas(context, charset, cellW, cellH, fontName, bold);
    }

    protected override void OnConfigurationChanged()
    {
        // Global
        if (Configuration.TryGet("filterType", out int filterType))
            _filterType = (FilterType)filterType;
        if (Configuration.TryGet("advancedMode", out bool advancedMode))
            _advancedMode = advancedMode;

        // Basic
        if (Configuration.TryGet("layoutMode", out int layoutMode))
            _layoutMode = layoutMode;
        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;
        if (Configuration.TryGet("rectWidth", out float rectWidth))
            _rectWidth = rectWidth;
        if (Configuration.TryGet("rectHeight", out float rectHeight))
            _rectHeight = rectHeight;
        if (Configuration.TryGet("cellWidth", out float cellWidth))
            _cellWidth = cellWidth;
        if (Configuration.TryGet("cellHeight", out float cellHeight))
            _cellHeight = cellHeight;
        if (Configuration.TryGet("charsetPreset", out int charsetPreset))
            _charsetPreset = charsetPreset;
        if (Configuration.TryGet("customCharset", out string? customCharset))
            _customCharset = customCharset ?? "";
        if (Configuration.TryGet("colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("foreground", out Vector4 foreground))
            _foreground = foreground;
        if (Configuration.TryGet("background", out Vector4 background))
            _background = background;

        // Advanced: Character
        if (Configuration.TryGet("fontFamily", out int fontFamily))
            _fontFamily = fontFamily;
        if (Configuration.TryGet("fontWeight", out int fontWeight))
            _fontWeight = fontWeight;
        if (Configuration.TryGet("charSpacing", out float charSpacing))
            _charSpacing = charSpacing;
        if (Configuration.TryGet("lineSpacing", out float lineSpacing))
            _lineSpacing = lineSpacing;
        if (Configuration.TryGet("aspectCorrection", out bool aspectCorrection))
            _aspectCorrection = aspectCorrection;

        // Advanced: Color
        if (Configuration.TryGet("paletteType", out int paletteType))
            _paletteType = paletteType;
        if (Configuration.TryGet("saturation", out float saturation))
            _saturation = saturation;
        if (Configuration.TryGet("quantizeLevels", out int quantizeLevels))
            _quantizeLevels = quantizeLevels;
        if (Configuration.TryGet("preserveLuminance", out bool preserveLuminance))
            _preserveLuminance = preserveLuminance;

        // Advanced: Brightness
        if (Configuration.TryGet("brightness", out float brightness))
            _brightness = brightness;
        if (Configuration.TryGet("contrast", out float contrast))
            _contrast = contrast;
        if (Configuration.TryGet("gamma", out float gamma))
            _gamma = gamma;
        if (Configuration.TryGet("invert", out bool invert))
            _invert = invert;
        if (Configuration.TryGet("autoContrast", out bool autoContrast))
            _autoContrast = autoContrast;

        // Advanced: Sampling
        if (Configuration.TryGet("lockAspect", out bool lockAspect))
            _lockAspect = lockAspect;
        if (Configuration.TryGet("sampleMode", out int sampleMode))
            _sampleMode = sampleMode;
        if (Configuration.TryGet("sampleRadius", out float sampleRadius))
            _sampleRadius = sampleRadius;

        // Advanced: Visual Effects
        if (Configuration.TryGet("scanlines", out bool scanlines))
            _scanlines = scanlines;
        if (Configuration.TryGet("scanlineIntensity", out float scanlineIntensity))
            _scanlineIntensity = scanlineIntensity;
        if (Configuration.TryGet("scanlineSpacing", out int scanlineSpacing))
            _scanlineSpacing = scanlineSpacing;
        if (Configuration.TryGet("crtCurvature", out bool crtCurvature))
            _crtCurvature = crtCurvature;
        if (Configuration.TryGet("crtAmount", out float crtAmount))
            _crtAmount = crtAmount;
        if (Configuration.TryGet("phosphorGlow", out bool phosphorGlow))
            _phosphorGlow = phosphorGlow;
        if (Configuration.TryGet("phosphorRadius", out float phosphorRadius))
            _phosphorRadius = phosphorRadius;
        if (Configuration.TryGet("phosphorIntensity", out float phosphorIntensity))
            _phosphorIntensity = phosphorIntensity;
        if (Configuration.TryGet("chromatic", out bool chromatic))
            _chromatic = chromatic;
        if (Configuration.TryGet("chromaticOffset", out float chromaticOffset))
            _chromaticOffset = chromaticOffset;
        if (Configuration.TryGet("vignette", out bool vignette))
            _vignette = vignette;
        if (Configuration.TryGet("vignetteIntensity", out float vignetteIntensity))
            _vignetteIntensity = vignetteIntensity;
        if (Configuration.TryGet("vignetteRadius", out float vignetteRadius))
            _vignetteRadius = vignetteRadius;
        if (Configuration.TryGet("noise", out bool noise))
            _noise = noise;
        if (Configuration.TryGet("noiseAmount", out float noiseAmount))
            _noiseAmount = noiseAmount;
        if (Configuration.TryGet("flicker", out bool flicker))
            _flicker = flicker;
        if (Configuration.TryGet("flickerSpeed", out float flickerSpeed))
            _flickerSpeed = flickerSpeed;

        // Advanced: Character Rendering
        if (Configuration.TryGet("antialiasing", out int antialiasing))
            _antialiasing = antialiasing;
        if (Configuration.TryGet("charShadow", out bool charShadow))
            _charShadow = charShadow;
        if (Configuration.TryGet("shadowOffset", out Vector2 shadowOffset))
            _shadowOffset = shadowOffset;
        if (Configuration.TryGet("shadowColor", out Vector4 shadowColor))
            _shadowColor = shadowColor;
        if (Configuration.TryGet("charOutline", out bool charOutline))
            _charOutline = charOutline;
        if (Configuration.TryGet("outlineThickness", out float outlineThickness))
            _outlineThickness = outlineThickness;
        if (Configuration.TryGet("outlineColor", out Vector4 outlineColor))
            _outlineColor = outlineColor;
        if (Configuration.TryGet("glowOnBright", out bool glowOnBright))
            _glowOnBright = glowOnBright;
        if (Configuration.TryGet("glowThreshold", out float glowThreshold))
            _glowThreshold = glowThreshold;
        if (Configuration.TryGet("glowRadius", out float glowRadius))
            _glowRadius = glowRadius;
        if (Configuration.TryGet("gridLines", out bool gridLines))
            _gridLines = gridLines;
        if (Configuration.TryGet("gridThickness", out float gridThickness))
            _gridThickness = gridThickness;
        if (Configuration.TryGet("gridColor", out Vector4 gridColor))
            _gridColor = gridColor;

        // Advanced: Edge
        if (Configuration.TryGet("edgeSoftness", out float edgeSoftness))
            _edgeSoftness = edgeSoftness;
        if (Configuration.TryGet("shapeFeather", out float shapeFeather))
            _shapeFeather = shapeFeather;
        if (Configuration.TryGet("innerGlow", out bool innerGlow))
            _innerGlow = innerGlow;
        if (Configuration.TryGet("innerGlowColor", out Vector4 innerGlowColor))
            _innerGlowColor = innerGlowColor;
        if (Configuration.TryGet("innerGlowSize", out float innerGlowSize))
            _innerGlowSize = innerGlowSize;

        // Update atlas if needed
        if (Context != null)
        {
            UpdateCharacterAtlas(Context);
        }
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _totalTime = (float)gameTime.TotalTime.TotalSeconds;
        _mousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;
        if (_paramsBuffer == null) return;

        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        var atlasTexture = _atlas.Texture;
        if (atlasTexture == null) return;

        // Build and update constant buffer
        var cbParams = new ASCIIClassicParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            Time = _totalTime,
            LayoutMode = _layoutMode,
            Radius = _radius,
            EdgeSoftness = _edgeSoftness,

            RectWidth = _rectWidth,
            RectHeight = _rectHeight,
            ShapeFeather = _shapeFeather,
            _pad1 = 0,

            CellWidth = _cellWidth,
            CellHeight = _cellHeight,
            CharCount = _atlas.CharacterCount,
            SampleMode = _sampleMode,

            ColorMode = _colorMode,
            Saturation = _saturation,
            QuantizeLevels = _quantizeLevels,
            PreserveLuminance = _preserveLuminance ? 1f : 0f,
            ForegroundColor = _foreground,
            BackgroundColor = _background,

            Brightness = _brightness,
            Contrast = _contrast,
            Gamma = _gamma,
            Invert = _invert ? 1f : 0f,

            Scanlines = _scanlines ? 1f : 0f,
            ScanlineIntensity = _scanlineIntensity,
            ScanlineSpacing = _scanlineSpacing,
            CrtCurvature = _crtCurvature ? 1f : 0f,

            CrtAmount = _crtAmount,
            PhosphorGlow = _phosphorGlow ? 1f : 0f,
            PhosphorRadius = _phosphorRadius,
            PhosphorIntensity = _phosphorIntensity,

            Chromatic = _chromatic ? 1f : 0f,
            ChromaticOffset = _chromaticOffset,
            Vignette = _vignette ? 1f : 0f,
            VignetteIntensity = _vignetteIntensity,

            VignetteRadius = _vignetteRadius,
            Noise = _noise ? 1f : 0f,
            NoiseAmount = _noiseAmount,
            Flicker = _flicker ? 1f : 0f,

            FlickerSpeed = _flickerSpeed,
            Antialiasing = _antialiasing,
            CharShadow = _charShadow ? 1f : 0f,
            CharOutline = _charOutline ? 1f : 0f,
            ShadowColor = _shadowColor,

            ShadowOffset = _shadowOffset,
            OutlineThickness = _outlineThickness,
            GlowOnBright = _glowOnBright ? 1f : 0f,
            OutlineColor = _outlineColor,

            GlowThreshold = _glowThreshold,
            GlowRadius = _glowRadius,
            GridLines = _gridLines ? 1f : 0f,
            GridThickness = _gridThickness,
            GridColor = _gridColor,

            InnerGlow = _innerGlow ? 1f : 0f,
            InnerGlowSize = _innerGlowSize,
            _pad2 = Vector2.Zero,
            InnerGlowColor = _innerGlowColor
        };

        context.UpdateBuffer(_paramsBuffer, cbParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetShaderResource(ShaderStage.Pixel, 1, atlasTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);
        context.SetSampler(ShaderStage.Pixel, 1, _pointSampler!);

        // Draw fullscreen quad
        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Cleanup
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
        context.SetShaderResource(ShaderStage.Pixel, 1, (ITexture?)null);
    }

    protected override void OnDispose()
    {
        _atlas.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _paramsBuffer?.Dispose();
        _linearSampler?.Dispose();
        _pointSampler?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ASCIIZerEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.ASCIIZer.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Constant buffer structure for ASCII Classic shader.
    /// Size must be multiple of 16 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 320)]
    private struct ASCIIClassicParams
    {
        // Core (32 bytes)
        public Vector2 MousePosition;
        public Vector2 ViewportSize;
        public float Time;
        public float LayoutMode;
        public float Radius;
        public float EdgeSoftness;

        // Shape (16 bytes)
        public float RectWidth;
        public float RectHeight;
        public float ShapeFeather;
        public float _pad1;

        // Cell (16 bytes)
        public float CellWidth;
        public float CellHeight;
        public float CharCount;
        public float SampleMode;

        // Color mode (48 bytes)
        public float ColorMode;
        public float Saturation;
        public float QuantizeLevels;
        public float PreserveLuminance;
        public Vector4 ForegroundColor;
        public Vector4 BackgroundColor;

        // Brightness (16 bytes)
        public float Brightness;
        public float Contrast;
        public float Gamma;
        public float Invert;

        // Visual effects flags (16 bytes)
        public float Scanlines;
        public float ScanlineIntensity;
        public float ScanlineSpacing;
        public float CrtCurvature;

        // CRT effects (16 bytes)
        public float CrtAmount;
        public float PhosphorGlow;
        public float PhosphorRadius;
        public float PhosphorIntensity;

        // More effects (16 bytes)
        public float Chromatic;
        public float ChromaticOffset;
        public float Vignette;
        public float VignetteIntensity;

        // Even more (16 bytes)
        public float VignetteRadius;
        public float Noise;
        public float NoiseAmount;
        public float Flicker;

        // Character rendering (32 bytes)
        public float FlickerSpeed;
        public float Antialiasing;
        public float CharShadow;
        public float CharOutline;
        public Vector4 ShadowColor;

        // Outline & glow (32 bytes)
        public Vector2 ShadowOffset;
        public float OutlineThickness;
        public float GlowOnBright;
        public Vector4 OutlineColor;

        // Grid (32 bytes)
        public float GlowThreshold;
        public float GlowRadius;
        public float GridLines;
        public float GridThickness;
        public Vector4 GridColor;

        // Inner glow (32 bytes)
        public float InnerGlow;
        public float InnerGlowSize;
        public Vector2 _pad2;
        public Vector4 InnerGlowColor;
    }
}
