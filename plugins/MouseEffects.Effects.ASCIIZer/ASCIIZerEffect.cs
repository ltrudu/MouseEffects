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
/// Uses split constant buffers: b0 for shared post-effects, b1 for filter-specific params.
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
        Category = EffectCategory.VisualFilter
    };

    // GPU resources - shared
    private IBuffer? _postEffectsBuffer;    // b0 - shared post-effects
    private IBuffer? _filterParamsBuffer;   // b1 - filter-specific params (resized per filter)
    private ISamplerState? _linearSampler;
    private ISamplerState? _pointSampler;

    // ASCII Classic shader
    private IShader? _asciiClassicVS;
    private IShader? _asciiClassicPS;

    // Dot Matrix shader
    private IShader? _dotMatrixVS;
    private IShader? _dotMatrixPS;

    // Matrix Rain shader
    private IShader? _matrixRainVS;
    private IShader? _matrixRainPS;

    // Braille shader
    private IShader? _brailleVS;
    private IShader? _braillePS;

    // Typewriter shader
    private IShader? _typewriterVS;
    private IShader? _typewriterPS;

    // Edge ASCII shader
    private IShader? _edgeASCIIVS;
    private IShader? _edgeASCIIPS;

    // Character atlas
    private readonly CharacterAtlas _atlas = new();
    private bool _atlasNeedsUpdate;

    // Animation time and mouse position
    private float _totalTime;
    private Vector2 _mousePosition;

    // Global settings
    private FilterType _filterType = FilterType.ASCIIClassic;
    private bool _advancedMode;

    #region Shared Post-Effects Properties

    private bool _scanlines;
    private float _scanlineIntensity = 0.3f;
    private int _scanlineSpacing = 2;
    private bool _crtCurvature;
    private float _crtAmount = 0.1f;
    private bool _phosphorGlow;
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

    #endregion

    #region ASCII Classic Filter Properties

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
    private int _fontFamily;
    private int _fontWeight;
    private float _saturation = 1f;
    private int _quantizeLevels = 256;
    private bool _preserveLuminance;
    private float _brightness;
    private float _contrast = 1f;
    private float _gamma = 1f;
    private bool _invert;
    private int _sampleMode;
    private int _antialiasing;
    private bool _charShadow;
    private Vector2 _shadowOffset = new(1f, 1f);
    private Vector4 _shadowColor = new(0f, 0f, 0f, 0.5f);
    private bool _glowOnBright;
    private float _glowThreshold = 0.8f;
    private float _glowRadius = 3f;
    private bool _gridLines;
    private float _gridThickness = 1f;
    private Vector4 _gridColor = new(0.2f, 0.2f, 0.2f, 1f);
    private float _edgeSoftness = 20f;
    private float _shapeFeather;
    private bool _innerGlow;
    private Vector4 _innerGlowColor = new(1f, 1f, 1f, 0.3f);
    private float _innerGlowSize = 10f;

    #endregion

    #region Matrix Rain Filter Properties

    private int _mr_layoutMode;
    private float _mr_radius = 200f;
    private float _mr_rectWidth = 400f;
    private float _mr_rectHeight = 300f;
    private float _mr_fallSpeed = 2f;
    private float _mr_trailLength = 15f;
    private float _mr_charCycleSpeed = 1f;
    private float _mr_columnDensity = 0.7f;
    private float _mr_glowIntensity = 0.8f;
    private float _mr_cellWidth = 10f;
    private float _mr_cellHeight = 16f;
    private Vector4 _mr_primaryColor = new(0f, 1f, 0.25f, 1f);  // Matrix green
    private Vector4 _mr_glowColor = new(0.67f, 1f, 0.67f, 1f);  // Light green
    private float _mr_brightness;
    private float _mr_contrast = 1f;
    private float _mr_backgroundFade = 0.3f;
    private float _mr_edgeSoftness = 20f;
    private float _mr_shapeFeather;

    #endregion

    #region Dot Matrix Filter Properties

    private int _dm_layoutMode;
    private float _dm_radius = 200f;
    private float _dm_rectWidth = 400f;
    private float _dm_rectHeight = 300f;
    private float _dm_cellSize = 8f;
    private float _dm_dotSize = 0.8f;
    private float _dm_dotSpacing = 1f;
    private int _dm_ledShape;
    private float _dm_offBrightness = 0.05f;
    private bool _dm_rgbMode;
    private int _dm_colorMode;
    private Vector4 _dm_foregroundColor = new(0f, 1f, 0f, 1f);
    private Vector4 _dm_backgroundColor = new(0f, 0f, 0f, 1f);
    private float _dm_brightness;
    private float _dm_contrast = 1f;
    private float _dm_gamma = 1f;
    private float _dm_saturation = 1f;
    private float _dm_edgeSoftness = 20f;
    private float _dm_shapeFeather;

    #endregion

    #region Braille Filter Properties

    private int _br_layoutMode;
    private float _br_radius = 200f;
    private float _br_rectWidth = 400f;
    private float _br_rectHeight = 300f;
    private float _br_threshold = 0.5f;
    private bool _br_adaptiveThreshold;
    private float _br_dotSize = 0.8f;
    private float _br_dotSpacing = 0.5f;
    private bool _br_invertDots;
    private float _br_cellWidth = 8f;
    private float _br_cellHeight = 16f;
    private Vector4 _br_foregroundColor = new(1f, 1f, 1f, 1f);
    private Vector4 _br_backgroundColor = new(0f, 0f, 0f, 1f);
    private float _br_brightness;
    private float _br_contrast = 1f;
    private float _br_edgeSoftness = 20f;
    private float _br_shapeFeather;

    #endregion

    #region Typewriter Filter Properties

    private int _tw_layoutMode;
    private float _tw_radius = 200f;
    private float _tw_rectWidth = 400f;
    private float _tw_rectHeight = 300f;
    private float _tw_cellWidth = 8f;
    private float _tw_cellHeight = 16f;
    private float _tw_inkVariation = 0.2f;
    private float _tw_positionJitter = 0.5f;
    private bool _tw_ribbonWear;
    private bool _tw_doubleStrike;
    private float _tw_ageEffect = 0.3f;
    private Vector4 _tw_inkColor = new(0.1f, 0.1f, 0.1f, 1f);
    private Vector4 _tw_paperColor = new(0.96f, 0.96f, 0.86f, 1f); // Beige
    private float _tw_brightness;
    private float _tw_contrast = 1f;
    private float _tw_edgeSoftness = 20f;
    private float _tw_shapeFeather;

    #endregion

    #region Edge ASCII Filter Properties

    private int _ea_layoutMode;
    private float _ea_radius = 200f;
    private float _ea_rectWidth = 400f;
    private float _ea_rectHeight = 300f;
    private float _ea_cellWidth = 8f;
    private float _ea_cellHeight = 16f;
    private float _ea_edgeThreshold = 0.1f;
    private float _ea_lineThickness = 1f;
    private bool _ea_showCorners = true;
    private bool _ea_fillBackground;
    private float _ea_backgroundOpacity = 0.2f;
    private float _ea_edgeBrightness = 1f;
    private Vector4 _ea_edgeColor = new(0f, 1f, 0f, 1f); // Green
    private Vector4 _ea_backgroundColor = new(0f, 0f, 0f, 1f); // Black
    private float _ea_brightness;
    private float _ea_contrast = 1f;
    private float _ea_edgeSoftness = 20f;
    private float _ea_shapeFeather;

    #endregion

    public override EffectMetadata Metadata => _metadata;
    public override bool RequiresContinuousScreenCapture => true;

    #region Public Properties - Global

    public FilterType FilterType { get => _filterType; set => _filterType = value; }
    public bool AdvancedMode { get => _advancedMode; set => _advancedMode = value; }

    #endregion

    #region Public Properties - Shared Post-Effects

    public bool Scanlines { get => _scanlines; set => _scanlines = value; }
    public float ScanlineIntensity { get => _scanlineIntensity; set => _scanlineIntensity = Math.Clamp(value, 0f, 1f); }
    public int ScanlineSpacing { get => _scanlineSpacing; set => _scanlineSpacing = Math.Clamp(value, 1, 4); }
    public bool CrtCurvature { get => _crtCurvature; set => _crtCurvature = value; }
    public float CrtAmount { get => _crtAmount; set => _crtAmount = Math.Clamp(value, 0f, 0.5f); }
    public bool PhosphorGlow { get => _phosphorGlow; set => _phosphorGlow = value; }
    public float PhosphorIntensity { get => _phosphorIntensity; set => _phosphorIntensity = Math.Clamp(value, 0f, 1f); }
    public bool Chromatic { get => _chromatic; set => _chromatic = value; }
    public float ChromaticOffset { get => _chromaticOffset; set => _chromaticOffset = Math.Clamp(value, 0.5f, 4f); }
    public bool Vignette { get => _vignette; set => _vignette = value; }
    public float VignetteIntensity { get => _vignetteIntensity; set => _vignetteIntensity = Math.Clamp(value, 0f, 1f); }
    public float VignetteRadius { get => _vignetteRadius; set => _vignetteRadius = Math.Clamp(value, 0.3f, 1f); }
    public bool Noise { get => _noise; set => _noise = value; }
    public float NoiseAmount { get => _noiseAmount; set => _noiseAmount = Math.Clamp(value, 0f, 0.5f); }
    public bool Flicker { get => _flicker; set => _flicker = value; }
    public float FlickerSpeed { get => _flickerSpeed; set => _flickerSpeed = Math.Clamp(value, 0.5f, 5f); }

    #endregion

    #region Public Properties - ASCII Classic Filter

    public int LayoutMode { get => _layoutMode; set => _layoutMode = value; }
    public float Radius { get => _radius; set => _radius = Math.Clamp(value, 50f, 500f); }
    public float RectWidth { get => _rectWidth; set => _rectWidth = Math.Clamp(value, 100f, 800f); }
    public float RectHeight { get => _rectHeight; set => _rectHeight = Math.Clamp(value, 100f, 600f); }
    public float CellWidth { get => _cellWidth; set { _cellWidth = Math.Clamp(value, 4f, 32f); _atlasNeedsUpdate = true; } }
    public float CellHeight { get => _cellHeight; set { _cellHeight = Math.Clamp(value, 8f, 48f); _atlasNeedsUpdate = true; } }
    public int CharsetPreset { get => _charsetPreset; set { _charsetPreset = value; _atlasNeedsUpdate = true; } }
    public string CustomCharset { get => _customCharset; set { _customCharset = value ?? ""; _atlasNeedsUpdate = true; } }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public Vector4 Foreground { get => _foreground; set => _foreground = value; }
    public Vector4 Background { get => _background; set => _background = value; }
    public int FontFamily { get => _fontFamily; set { _fontFamily = value; _atlasNeedsUpdate = true; } }
    public int FontWeight { get => _fontWeight; set { _fontWeight = value; _atlasNeedsUpdate = true; } }
    public float Saturation { get => _saturation; set => _saturation = Math.Clamp(value, 0f, 2f); }
    public int QuantizeLevels { get => _quantizeLevels; set => _quantizeLevels = Math.Clamp(value, 2, 256); }
    public bool PreserveLuminance { get => _preserveLuminance; set => _preserveLuminance = value; }
    public float Brightness { get => _brightness; set => _brightness = Math.Clamp(value, -1f, 1f); }
    public float Contrast { get => _contrast; set => _contrast = Math.Clamp(value, 0.5f, 3f); }
    public float Gamma { get => _gamma; set => _gamma = Math.Clamp(value, 0.5f, 2.5f); }
    public bool Invert { get => _invert; set => _invert = value; }
    public int SampleMode { get => _sampleMode; set => _sampleMode = value; }
    public int Antialiasing { get => _antialiasing; set => _antialiasing = Math.Clamp(value, 0, 2); }
    public bool CharShadow { get => _charShadow; set => _charShadow = value; }
    public Vector2 ShadowOffset { get => _shadowOffset; set => _shadowOffset = value; }
    public Vector4 ShadowColor { get => _shadowColor; set => _shadowColor = value; }
    public bool GlowOnBright { get => _glowOnBright; set => _glowOnBright = value; }
    public float GlowThreshold { get => _glowThreshold; set => _glowThreshold = Math.Clamp(value, 0.5f, 1f); }
    public float GlowRadius { get => _glowRadius; set => _glowRadius = Math.Clamp(value, 1f, 8f); }
    public bool GridLines { get => _gridLines; set => _gridLines = value; }
    public float GridThickness { get => _gridThickness; set => _gridThickness = Math.Clamp(value, 0.5f, 2f); }
    public Vector4 GridColor { get => _gridColor; set => _gridColor = value; }
    public float EdgeSoftness { get => _edgeSoftness; set => _edgeSoftness = Math.Clamp(value, 0f, 100f); }
    public float ShapeFeather { get => _shapeFeather; set => _shapeFeather = Math.Clamp(value, 0f, 50f); }
    public bool InnerGlow { get => _innerGlow; set => _innerGlow = value; }
    public Vector4 InnerGlowColor { get => _innerGlowColor; set => _innerGlowColor = value; }
    public float InnerGlowSize { get => _innerGlowSize; set => _innerGlowSize = Math.Clamp(value, 0f, 50f); }

    #endregion

    #region Public Properties - Matrix Rain Filter

    public int MR_LayoutMode { get => _mr_layoutMode; set => _mr_layoutMode = value; }
    public float MR_Radius { get => _mr_radius; set => _mr_radius = Math.Clamp(value, 50f, 500f); }
    public float MR_RectWidth { get => _mr_rectWidth; set => _mr_rectWidth = Math.Clamp(value, 100f, 800f); }
    public float MR_RectHeight { get => _mr_rectHeight; set => _mr_rectHeight = Math.Clamp(value, 100f, 600f); }
    public float MR_FallSpeed { get => _mr_fallSpeed; set => _mr_fallSpeed = Math.Clamp(value, 0.5f, 5f); }
    public float MR_TrailLength { get => _mr_trailLength; set => _mr_trailLength = Math.Clamp(value, 3f, 25f); }
    public float MR_CharCycleSpeed { get => _mr_charCycleSpeed; set => _mr_charCycleSpeed = Math.Clamp(value, 0.5f, 3f); }
    public float MR_ColumnDensity { get => _mr_columnDensity; set => _mr_columnDensity = Math.Clamp(value, 0.3f, 1f); }
    public float MR_GlowIntensity { get => _mr_glowIntensity; set => _mr_glowIntensity = Math.Clamp(value, 0f, 1f); }
    public float MR_CellWidth { get => _mr_cellWidth; set { _mr_cellWidth = Math.Clamp(value, 6f, 16f); _atlasNeedsUpdate = true; } }
    public float MR_CellHeight { get => _mr_cellHeight; set { _mr_cellHeight = Math.Clamp(value, 10f, 24f); _atlasNeedsUpdate = true; } }
    public Vector4 MR_PrimaryColor { get => _mr_primaryColor; set => _mr_primaryColor = value; }
    public Vector4 MR_GlowColor { get => _mr_glowColor; set => _mr_glowColor = value; }
    public float MR_Brightness { get => _mr_brightness; set => _mr_brightness = Math.Clamp(value, -0.3f, 0.3f); }
    public float MR_Contrast { get => _mr_contrast; set => _mr_contrast = Math.Clamp(value, 0.5f, 2f); }
    public float MR_BackgroundFade { get => _mr_backgroundFade; set => _mr_backgroundFade = Math.Clamp(value, 0f, 1f); }
    public float MR_EdgeSoftness { get => _mr_edgeSoftness; set => _mr_edgeSoftness = Math.Clamp(value, 0f, 100f); }
    public float MR_ShapeFeather { get => _mr_shapeFeather; set => _mr_shapeFeather = Math.Clamp(value, 0f, 50f); }

    #endregion

    #region Public Properties - Dot Matrix Filter

    public int DM_LayoutMode { get => _dm_layoutMode; set => _dm_layoutMode = value; }
    public float DM_Radius { get => _dm_radius; set => _dm_radius = Math.Clamp(value, 50f, 500f); }
    public float DM_RectWidth { get => _dm_rectWidth; set => _dm_rectWidth = Math.Clamp(value, 100f, 800f); }
    public float DM_RectHeight { get => _dm_rectHeight; set => _dm_rectHeight = Math.Clamp(value, 100f, 600f); }
    public float DM_CellSize { get => _dm_cellSize; set => _dm_cellSize = Math.Clamp(value, 4f, 24f); }
    public float DM_DotSize { get => _dm_dotSize; set => _dm_dotSize = Math.Clamp(value, 0.3f, 0.95f); }
    public float DM_DotSpacing { get => _dm_dotSpacing; set => _dm_dotSpacing = Math.Clamp(value, 0f, 4f); }
    public int DM_LedShape { get => _dm_ledShape; set => _dm_ledShape = Math.Clamp(value, 0, 2); }
    public float DM_OffBrightness { get => _dm_offBrightness; set => _dm_offBrightness = Math.Clamp(value, 0f, 0.2f); }
    public bool DM_RgbMode { get => _dm_rgbMode; set => _dm_rgbMode = value; }
    public int DM_ColorMode { get => _dm_colorMode; set => _dm_colorMode = value; }
    public Vector4 DM_ForegroundColor { get => _dm_foregroundColor; set => _dm_foregroundColor = value; }
    public Vector4 DM_BackgroundColor { get => _dm_backgroundColor; set => _dm_backgroundColor = value; }
    public float DM_Brightness { get => _dm_brightness; set => _dm_brightness = Math.Clamp(value, -0.5f, 0.5f); }
    public float DM_Contrast { get => _dm_contrast; set => _dm_contrast = Math.Clamp(value, 0.5f, 2f); }
    public float DM_Gamma { get => _dm_gamma; set => _dm_gamma = Math.Clamp(value, 0.5f, 2.5f); }
    public float DM_Saturation { get => _dm_saturation; set => _dm_saturation = Math.Clamp(value, 0f, 2f); }
    public float DM_EdgeSoftness { get => _dm_edgeSoftness; set => _dm_edgeSoftness = Math.Clamp(value, 0f, 100f); }
    public float DM_ShapeFeather { get => _dm_shapeFeather; set => _dm_shapeFeather = Math.Clamp(value, 0f, 50f); }

    #endregion

    #region Public Properties - Braille Filter

    public int BR_LayoutMode { get => _br_layoutMode; set => _br_layoutMode = value; }
    public float BR_Radius { get => _br_radius; set => _br_radius = Math.Clamp(value, 50f, 500f); }
    public float BR_RectWidth { get => _br_rectWidth; set => _br_rectWidth = Math.Clamp(value, 100f, 800f); }
    public float BR_RectHeight { get => _br_rectHeight; set => _br_rectHeight = Math.Clamp(value, 100f, 600f); }
    public float BR_Threshold { get => _br_threshold; set => _br_threshold = Math.Clamp(value, 0f, 1f); }
    public bool BR_AdaptiveThreshold { get => _br_adaptiveThreshold; set => _br_adaptiveThreshold = value; }
    public float BR_DotSize { get => _br_dotSize; set => _br_dotSize = Math.Clamp(value, 0.3f, 1f); }
    public float BR_DotSpacing { get => _br_dotSpacing; set => _br_dotSpacing = Math.Clamp(value, 0f, 2f); }
    public bool BR_InvertDots { get => _br_invertDots; set => _br_invertDots = value; }
    public float BR_CellWidth { get => _br_cellWidth; set => _br_cellWidth = Math.Clamp(value, 4f, 16f); }
    public float BR_CellHeight { get => _br_cellHeight; set => _br_cellHeight = Math.Clamp(value, 8f, 32f); }
    public Vector4 BR_ForegroundColor { get => _br_foregroundColor; set => _br_foregroundColor = value; }
    public Vector4 BR_BackgroundColor { get => _br_backgroundColor; set => _br_backgroundColor = value; }
    public float BR_Brightness { get => _br_brightness; set => _br_brightness = Math.Clamp(value, -0.5f, 0.5f); }
    public float BR_Contrast { get => _br_contrast; set => _br_contrast = Math.Clamp(value, 0.5f, 2f); }
    public float BR_EdgeSoftness { get => _br_edgeSoftness; set => _br_edgeSoftness = Math.Clamp(value, 0f, 100f); }
    public float BR_ShapeFeather { get => _br_shapeFeather; set => _br_shapeFeather = Math.Clamp(value, 0f, 50f); }

    #endregion

    #region Public Properties - Typewriter Filter

    public int TW_LayoutMode { get => _tw_layoutMode; set => _tw_layoutMode = value; }
    public float TW_Radius { get => _tw_radius; set => _tw_radius = Math.Clamp(value, 50f, 500f); }
    public float TW_RectWidth { get => _tw_rectWidth; set => _tw_rectWidth = Math.Clamp(value, 100f, 800f); }
    public float TW_RectHeight { get => _tw_rectHeight; set => _tw_rectHeight = Math.Clamp(value, 100f, 600f); }
    public float TW_CellWidth { get => _tw_cellWidth; set { _tw_cellWidth = Math.Clamp(value, 4f, 16f); _atlasNeedsUpdate = true; } }
    public float TW_CellHeight { get => _tw_cellHeight; set { _tw_cellHeight = Math.Clamp(value, 8f, 24f); _atlasNeedsUpdate = true; } }
    public float TW_InkVariation { get => _tw_inkVariation; set => _tw_inkVariation = Math.Clamp(value, 0f, 0.5f); }
    public float TW_PositionJitter { get => _tw_positionJitter; set => _tw_positionJitter = Math.Clamp(value, 0f, 2f); }
    public bool TW_RibbonWear { get => _tw_ribbonWear; set => _tw_ribbonWear = value; }
    public bool TW_DoubleStrike { get => _tw_doubleStrike; set => _tw_doubleStrike = value; }
    public float TW_AgeEffect { get => _tw_ageEffect; set => _tw_ageEffect = Math.Clamp(value, 0f, 1f); }
    public Vector4 TW_InkColor { get => _tw_inkColor; set => _tw_inkColor = value; }
    public Vector4 TW_PaperColor { get => _tw_paperColor; set => _tw_paperColor = value; }
    public float TW_Brightness { get => _tw_brightness; set => _tw_brightness = Math.Clamp(value, -0.5f, 0.5f); }
    public float TW_Contrast { get => _tw_contrast; set => _tw_contrast = Math.Clamp(value, 0.5f, 2f); }
    public float TW_EdgeSoftness { get => _tw_edgeSoftness; set => _tw_edgeSoftness = Math.Clamp(value, 0f, 100f); }
    public float TW_ShapeFeather { get => _tw_shapeFeather; set => _tw_shapeFeather = Math.Clamp(value, 0f, 50f); }

    #endregion

    #region Public Properties - Edge ASCII Filter

    public int EA_LayoutMode { get => _ea_layoutMode; set => _ea_layoutMode = Math.Clamp(value, 0, 2); }
    public float EA_Radius { get => _ea_radius; set => _ea_radius = Math.Clamp(value, 50f, 500f); }
    public float EA_RectWidth { get => _ea_rectWidth; set => _ea_rectWidth = Math.Clamp(value, 100f, 800f); }
    public float EA_RectHeight { get => _ea_rectHeight; set => _ea_rectHeight = Math.Clamp(value, 100f, 600f); }
    public float EA_CellWidth { get => _ea_cellWidth; set => _ea_cellWidth = Math.Clamp(value, 4f, 16f); }
    public float EA_CellHeight { get => _ea_cellHeight; set => _ea_cellHeight = Math.Clamp(value, 8f, 24f); }
    public float EA_EdgeThreshold { get => _ea_edgeThreshold; set => _ea_edgeThreshold = Math.Clamp(value, 0.05f, 0.5f); }
    public float EA_LineThickness { get => _ea_lineThickness; set => _ea_lineThickness = Math.Clamp(value, 1f, 3f); }
    public bool EA_ShowCorners { get => _ea_showCorners; set => _ea_showCorners = value; }
    public bool EA_FillBackground { get => _ea_fillBackground; set => _ea_fillBackground = value; }
    public float EA_BackgroundOpacity { get => _ea_backgroundOpacity; set => _ea_backgroundOpacity = Math.Clamp(value, 0f, 0.5f); }
    public float EA_EdgeBrightness { get => _ea_edgeBrightness; set => _ea_edgeBrightness = Math.Clamp(value, 0.5f, 2f); }
    public Vector4 EA_EdgeColor { get => _ea_edgeColor; set => _ea_edgeColor = value; }
    public Vector4 EA_BackgroundColor { get => _ea_backgroundColor; set => _ea_backgroundColor = value; }
    public float EA_Brightness { get => _ea_brightness; set => _ea_brightness = Math.Clamp(value, -0.5f, 0.5f); }
    public float EA_Contrast { get => _ea_contrast; set => _ea_contrast = Math.Clamp(value, 0.5f, 2f); }
    public float EA_EdgeSoftness { get => _ea_edgeSoftness; set => _ea_edgeSoftness = Math.Clamp(value, 0f, 100f); }
    public float EA_ShapeFeather { get => _ea_shapeFeather; set => _ea_shapeFeather = Math.Clamp(value, 0f, 50f); }

    #endregion

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile ASCII Classic shader
        var asciiSource = LoadEmbeddedShader("ASCIIClassic.hlsl");
        _asciiClassicVS = context.CompileShader(asciiSource, "VSMain", ShaderStage.Vertex);
        _asciiClassicPS = context.CompileShader(asciiSource, "PSMain", ShaderStage.Pixel);

        // Load and compile Dot Matrix shader
        var dotMatrixSource = LoadEmbeddedShader("DotMatrix.hlsl");
        _dotMatrixVS = context.CompileShader(dotMatrixSource, "VSMain", ShaderStage.Vertex);
        _dotMatrixPS = context.CompileShader(dotMatrixSource, "PSMain", ShaderStage.Pixel);

        // Load and compile Matrix Rain shader
        var matrixRainSource = LoadEmbeddedShader("MatrixRain.hlsl");
        _matrixRainVS = context.CompileShader(matrixRainSource, "VSMain", ShaderStage.Vertex);
        _matrixRainPS = context.CompileShader(matrixRainSource, "PSMain", ShaderStage.Pixel);

        // Load and compile Braille shader
        var brailleSource = LoadEmbeddedShader("Braille.hlsl");
        _brailleVS = context.CompileShader(brailleSource, "VSMain", ShaderStage.Vertex);
        _braillePS = context.CompileShader(brailleSource, "PSMain", ShaderStage.Pixel);

        // Load and compile Typewriter shader
        var typewriterSource = LoadEmbeddedShader("Typewriter.hlsl");
        _typewriterVS = context.CompileShader(typewriterSource, "VSMain", ShaderStage.Vertex);
        _typewriterPS = context.CompileShader(typewriterSource, "PSMain", ShaderStage.Pixel);

        // Load and compile Edge ASCII shader
        var edgeASCIISource = LoadEmbeddedShader("EdgeASCII.hlsl");
        _edgeASCIIVS = context.CompileShader(edgeASCIISource, "VSMain", ShaderStage.Vertex);
        _edgeASCIIPS = context.CompileShader(edgeASCIISource, "PSMain", ShaderStage.Pixel);

        // Create shared post-effects constant buffer
        _postEffectsBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<PostEffectsParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create filter params buffer (sized for largest filter)
        var maxFilterSize = Math.Max(
            Math.Max(Marshal.SizeOf<ASCIIClassicFilterParams>(), Marshal.SizeOf<DotMatrixFilterParams>()),
            Math.Max(Math.Max(Marshal.SizeOf<MatrixRainFilterParams>(), Marshal.SizeOf<BrailleFilterParams>()),
                Math.Max(Marshal.SizeOf<TypewriterFilterParams>(), Marshal.SizeOf<EdgeASCIIFilterParams>())));
        _filterParamsBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = maxFilterSize,
            Type = BufferType.Constant,
            Dynamic = true
        });

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

        // Shared Post-Effects
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

        // ASCII Classic Filter
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
        if (Configuration.TryGet("fontFamily", out int fontFamily))
            _fontFamily = fontFamily;
        if (Configuration.TryGet("fontWeight", out int fontWeight))
            _fontWeight = fontWeight;
        if (Configuration.TryGet("saturation", out float saturation))
            _saturation = saturation;
        if (Configuration.TryGet("quantizeLevels", out int quantizeLevels))
            _quantizeLevels = quantizeLevels;
        if (Configuration.TryGet("preserveLuminance", out bool preserveLuminance))
            _preserveLuminance = preserveLuminance;
        if (Configuration.TryGet("brightness", out float brightness))
            _brightness = brightness;
        if (Configuration.TryGet("contrast", out float contrast))
            _contrast = contrast;
        if (Configuration.TryGet("gamma", out float gamma))
            _gamma = gamma;
        if (Configuration.TryGet("invert", out bool invert))
            _invert = invert;
        if (Configuration.TryGet("sampleMode", out int sampleMode))
            _sampleMode = sampleMode;
        if (Configuration.TryGet("antialiasing", out int antialiasing))
            _antialiasing = antialiasing;
        if (Configuration.TryGet("charShadow", out bool charShadow))
            _charShadow = charShadow;
        if (Configuration.TryGet("shadowOffset", out Vector2 shadowOffset))
            _shadowOffset = shadowOffset;
        if (Configuration.TryGet("shadowColor", out Vector4 shadowColor))
            _shadowColor = shadowColor;
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

        // Matrix Rain Filter
        if (Configuration.TryGet("mr_layoutMode", out int mrLayoutMode))
            _mr_layoutMode = mrLayoutMode;
        if (Configuration.TryGet("mr_radius", out float mrRadius))
            _mr_radius = mrRadius;
        if (Configuration.TryGet("mr_rectWidth", out float mrRectWidth))
            _mr_rectWidth = mrRectWidth;
        if (Configuration.TryGet("mr_rectHeight", out float mrRectHeight))
            _mr_rectHeight = mrRectHeight;
        if (Configuration.TryGet("mr_fallSpeed", out float mrFallSpeed))
            _mr_fallSpeed = mrFallSpeed;
        if (Configuration.TryGet("mr_trailLength", out float mrTrailLength))
            _mr_trailLength = mrTrailLength;
        if (Configuration.TryGet("mr_charCycleSpeed", out float mrCharCycleSpeed))
            _mr_charCycleSpeed = mrCharCycleSpeed;
        if (Configuration.TryGet("mr_columnDensity", out float mrColumnDensity))
            _mr_columnDensity = mrColumnDensity;
        if (Configuration.TryGet("mr_glowIntensity", out float mrGlowIntensity))
            _mr_glowIntensity = mrGlowIntensity;
        if (Configuration.TryGet("mr_cellWidth", out float mrCellWidth))
            _mr_cellWidth = mrCellWidth;
        if (Configuration.TryGet("mr_cellHeight", out float mrCellHeight))
            _mr_cellHeight = mrCellHeight;
        if (Configuration.TryGet("mr_primaryColor", out Vector4 mrPrimaryColor))
            _mr_primaryColor = mrPrimaryColor;
        if (Configuration.TryGet("mr_glowColor", out Vector4 mrGlowColor))
            _mr_glowColor = mrGlowColor;
        if (Configuration.TryGet("mr_brightness", out float mrBrightness))
            _mr_brightness = mrBrightness;
        if (Configuration.TryGet("mr_contrast", out float mrContrast))
            _mr_contrast = mrContrast;
        if (Configuration.TryGet("mr_backgroundFade", out float mrBackgroundFade))
            _mr_backgroundFade = mrBackgroundFade;
        if (Configuration.TryGet("mr_edgeSoftness", out float mrEdgeSoftness))
            _mr_edgeSoftness = mrEdgeSoftness;
        if (Configuration.TryGet("mr_shapeFeather", out float mrShapeFeather))
            _mr_shapeFeather = mrShapeFeather;

        // Dot Matrix Filter
        if (Configuration.TryGet("dm_layoutMode", out int dmLayoutMode))
            _dm_layoutMode = dmLayoutMode;
        if (Configuration.TryGet("dm_radius", out float dmRadius))
            _dm_radius = dmRadius;
        if (Configuration.TryGet("dm_rectWidth", out float dmRectWidth))
            _dm_rectWidth = dmRectWidth;
        if (Configuration.TryGet("dm_rectHeight", out float dmRectHeight))
            _dm_rectHeight = dmRectHeight;
        if (Configuration.TryGet("dm_cellSize", out float dmCellSize))
            _dm_cellSize = dmCellSize;
        if (Configuration.TryGet("dm_dotSize", out float dmDotSize))
            _dm_dotSize = dmDotSize;
        if (Configuration.TryGet("dm_dotSpacing", out float dmDotSpacing))
            _dm_dotSpacing = dmDotSpacing;
        if (Configuration.TryGet("dm_ledShape", out int dmLedShape))
            _dm_ledShape = dmLedShape;
        if (Configuration.TryGet("dm_offBrightness", out float dmOffBrightness))
            _dm_offBrightness = dmOffBrightness;
        if (Configuration.TryGet("dm_rgbMode", out bool dmRgbMode))
            _dm_rgbMode = dmRgbMode;
        if (Configuration.TryGet("dm_colorMode", out int dmColorMode))
            _dm_colorMode = dmColorMode;
        if (Configuration.TryGet("dm_foregroundColor", out Vector4 dmForegroundColor))
            _dm_foregroundColor = dmForegroundColor;
        if (Configuration.TryGet("dm_backgroundColor", out Vector4 dmBackgroundColor))
            _dm_backgroundColor = dmBackgroundColor;
        if (Configuration.TryGet("dm_brightness", out float dmBrightness))
            _dm_brightness = dmBrightness;
        if (Configuration.TryGet("dm_contrast", out float dmContrast))
            _dm_contrast = dmContrast;
        if (Configuration.TryGet("dm_gamma", out float dmGamma))
            _dm_gamma = dmGamma;
        if (Configuration.TryGet("dm_saturation", out float dmSaturation))
            _dm_saturation = dmSaturation;
        if (Configuration.TryGet("dm_edgeSoftness", out float dmEdgeSoftness))
            _dm_edgeSoftness = dmEdgeSoftness;
        if (Configuration.TryGet("dm_shapeFeather", out float dmShapeFeather))
            _dm_shapeFeather = dmShapeFeather;

        // Braille Filter
        if (Configuration.TryGet("br_layoutMode", out int brLayoutMode))
            _br_layoutMode = brLayoutMode;
        if (Configuration.TryGet("br_radius", out float brRadius))
            _br_radius = brRadius;
        if (Configuration.TryGet("br_rectWidth", out float brRectWidth))
            _br_rectWidth = brRectWidth;
        if (Configuration.TryGet("br_rectHeight", out float brRectHeight))
            _br_rectHeight = brRectHeight;
        if (Configuration.TryGet("br_threshold", out float brThreshold))
            _br_threshold = brThreshold;
        if (Configuration.TryGet("br_adaptiveThreshold", out bool brAdaptiveThreshold))
            _br_adaptiveThreshold = brAdaptiveThreshold;
        if (Configuration.TryGet("br_dotSize", out float brDotSize))
            _br_dotSize = brDotSize;
        if (Configuration.TryGet("br_dotSpacing", out float brDotSpacing))
            _br_dotSpacing = brDotSpacing;
        if (Configuration.TryGet("br_invertDots", out bool brInvertDots))
            _br_invertDots = brInvertDots;
        if (Configuration.TryGet("br_cellWidth", out float brCellWidth))
            _br_cellWidth = brCellWidth;
        if (Configuration.TryGet("br_cellHeight", out float brCellHeight))
            _br_cellHeight = brCellHeight;
        if (Configuration.TryGet("br_foregroundColor", out Vector4 brForegroundColor))
            _br_foregroundColor = brForegroundColor;
        if (Configuration.TryGet("br_backgroundColor", out Vector4 brBackgroundColor))
            _br_backgroundColor = brBackgroundColor;
        if (Configuration.TryGet("br_brightness", out float brBrightness))
            _br_brightness = brBrightness;
        if (Configuration.TryGet("br_contrast", out float brContrast))
            _br_contrast = brContrast;
        if (Configuration.TryGet("br_edgeSoftness", out float brEdgeSoftness))
            _br_edgeSoftness = brEdgeSoftness;
        if (Configuration.TryGet("br_shapeFeather", out float brShapeFeather))
            _br_shapeFeather = brShapeFeather;

        // Typewriter Filter
        if (Configuration.TryGet("tw_layoutMode", out int twLayoutMode))
            _tw_layoutMode = twLayoutMode;
        if (Configuration.TryGet("tw_radius", out float twRadius))
            _tw_radius = twRadius;
        if (Configuration.TryGet("tw_rectWidth", out float twRectWidth))
            _tw_rectWidth = twRectWidth;
        if (Configuration.TryGet("tw_rectHeight", out float twRectHeight))
            _tw_rectHeight = twRectHeight;
        if (Configuration.TryGet("tw_cellWidth", out float twCellWidth))
            _tw_cellWidth = twCellWidth;
        if (Configuration.TryGet("tw_cellHeight", out float twCellHeight))
            _tw_cellHeight = twCellHeight;
        if (Configuration.TryGet("tw_inkVariation", out float twInkVariation))
            _tw_inkVariation = twInkVariation;
        if (Configuration.TryGet("tw_positionJitter", out float twPositionJitter))
            _tw_positionJitter = twPositionJitter;
        if (Configuration.TryGet("tw_ribbonWear", out bool twRibbonWear))
            _tw_ribbonWear = twRibbonWear;
        if (Configuration.TryGet("tw_doubleStrike", out bool twDoubleStrike))
            _tw_doubleStrike = twDoubleStrike;
        if (Configuration.TryGet("tw_ageEffect", out float twAgeEffect))
            _tw_ageEffect = twAgeEffect;
        if (Configuration.TryGet("tw_inkColor", out Vector4 twInkColor))
            _tw_inkColor = twInkColor;
        if (Configuration.TryGet("tw_paperColor", out Vector4 twPaperColor))
            _tw_paperColor = twPaperColor;
        if (Configuration.TryGet("tw_brightness", out float twBrightness))
            _tw_brightness = twBrightness;
        if (Configuration.TryGet("tw_contrast", out float twContrast))
            _tw_contrast = twContrast;
        if (Configuration.TryGet("tw_edgeSoftness", out float twEdgeSoftness))
            _tw_edgeSoftness = twEdgeSoftness;
        if (Configuration.TryGet("tw_shapeFeather", out float twShapeFeather))
            _tw_shapeFeather = twShapeFeather;

        // Edge ASCII Filter
        if (Configuration.TryGet("ea_layoutMode", out int eaLayoutMode))
            _ea_layoutMode = eaLayoutMode;
        if (Configuration.TryGet("ea_radius", out float eaRadius))
            _ea_radius = eaRadius;
        if (Configuration.TryGet("ea_rectWidth", out float eaRectWidth))
            _ea_rectWidth = eaRectWidth;
        if (Configuration.TryGet("ea_rectHeight", out float eaRectHeight))
            _ea_rectHeight = eaRectHeight;
        if (Configuration.TryGet("ea_cellWidth", out float eaCellWidth))
            _ea_cellWidth = eaCellWidth;
        if (Configuration.TryGet("ea_cellHeight", out float eaCellHeight))
            _ea_cellHeight = eaCellHeight;
        if (Configuration.TryGet("ea_edgeThreshold", out float eaEdgeThreshold))
            _ea_edgeThreshold = eaEdgeThreshold;
        if (Configuration.TryGet("ea_lineThickness", out float eaLineThickness))
            _ea_lineThickness = eaLineThickness;
        if (Configuration.TryGet("ea_showCorners", out bool eaShowCorners))
            _ea_showCorners = eaShowCorners;
        if (Configuration.TryGet("ea_fillBackground", out bool eaFillBackground))
            _ea_fillBackground = eaFillBackground;
        if (Configuration.TryGet("ea_backgroundOpacity", out float eaBackgroundOpacity))
            _ea_backgroundOpacity = eaBackgroundOpacity;
        if (Configuration.TryGet("ea_edgeBrightness", out float eaEdgeBrightness))
            _ea_edgeBrightness = eaEdgeBrightness;
        if (Configuration.TryGet("ea_edgeColor", out Vector4 eaEdgeColor))
            _ea_edgeColor = eaEdgeColor;
        if (Configuration.TryGet("ea_backgroundColor", out Vector4 eaBackgroundColor))
            _ea_backgroundColor = eaBackgroundColor;
        if (Configuration.TryGet("ea_brightness", out float eaBrightness))
            _ea_brightness = eaBrightness;
        if (Configuration.TryGet("ea_contrast", out float eaContrast))
            _ea_contrast = eaContrast;
        if (Configuration.TryGet("ea_edgeSoftness", out float eaEdgeSoftness))
            _ea_edgeSoftness = eaEdgeSoftness;
        if (Configuration.TryGet("ea_shapeFeather", out float eaShapeFeather))
            _ea_shapeFeather = eaShapeFeather;

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
        if (_postEffectsBuffer == null || _filterParamsBuffer == null) return;

        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Build and update post-effects buffer (b0) - shared across all filters
        var postEffects = new PostEffectsParams
        {
            ViewportSize = context.ViewportSize,
            Time = _totalTime,
            Scanlines = _scanlines ? 1f : 0f,
            ScanlineIntensity = _scanlineIntensity,
            ScanlineSpacing = _scanlineSpacing,
            CrtCurvature = _crtCurvature ? 1f : 0f,
            CrtAmount = _crtAmount,

            PhosphorGlow = _phosphorGlow ? 1f : 0f,
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
            _pad1 = 0,
            _pad2 = Vector4.Zero,

            _reserved1 = Vector4.Zero,
            _reserved2 = Vector4.Zero
        };

        context.UpdateBuffer(_postEffectsBuffer, postEffects);

        // Render based on filter type
        switch (_filterType)
        {
            case FilterType.MatrixRain:
                RenderMatrixRain(context, screenTexture);
                break;

            case FilterType.DotMatrix:
                RenderDotMatrix(context, screenTexture);
                break;

            case FilterType.Braille:
                RenderBraille(context, screenTexture);
                break;

            case FilterType.Typewriter:
                RenderTypewriter(context, screenTexture);
                break;

            case FilterType.EdgeASCII:
                RenderEdgeASCII(context, screenTexture);
                break;

            case FilterType.ASCIIClassic:
            default:
                RenderASCIIClassic(context, screenTexture);
                break;
        }
    }

    private void RenderASCIIClassic(IRenderContext context, ITexture screenTexture)
    {
        if (_asciiClassicVS == null || _asciiClassicPS == null) return;

        // Update atlas if needed
        if (_atlasNeedsUpdate)
        {
            UpdateCharacterAtlas(context);
            _atlasNeedsUpdate = false;
        }

        var atlasTexture = _atlas.Texture;
        if (atlasTexture == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new ASCIIClassicFilterParams
        {
            MousePosition = _mousePosition,
            LayoutMode = _layoutMode,
            Radius = _radius,
            EdgeSoftness = _edgeSoftness,
            ShapeFeather = _shapeFeather,
            RectWidth = _rectWidth,
            RectHeight = _rectHeight,

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

            Antialiasing = _antialiasing,
            CharShadow = _charShadow ? 1f : 0f,
            ShadowOffset = _shadowOffset,
            ShadowColor = _shadowColor,

            GlowOnBright = _glowOnBright ? 1f : 0f,
            GlowThreshold = _glowThreshold,
            GlowRadius = _glowRadius,
            GridLines = _gridLines ? 1f : 0f,
            GridThickness = _gridThickness,
            InnerGlow = _innerGlow ? 1f : 0f,
            InnerGlowSize = _innerGlowSize,
            _pad1 = 0,
            GridColor = _gridColor,
            InnerGlowColor = _innerGlowColor
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_asciiClassicVS);
        context.SetPixelShader(_asciiClassicPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
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

    private void RenderDotMatrix(IRenderContext context, ITexture screenTexture)
    {
        if (_dotMatrixVS == null || _dotMatrixPS == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new DotMatrixFilterParams
        {
            MousePosition = _mousePosition,
            LayoutMode = _dm_layoutMode,
            Radius = _dm_radius,
            EdgeSoftness = _dm_edgeSoftness,
            ShapeFeather = _dm_shapeFeather,
            RectWidth = _dm_rectWidth,
            RectHeight = _dm_rectHeight,

            DotSize = _dm_dotSize,
            DotSpacing = _dm_dotSpacing,
            CellSize = _dm_cellSize,
            LedShape = _dm_ledShape,
            OffBrightness = _dm_offBrightness,
            RgbMode = _dm_rgbMode ? 1f : 0f,
            ColorMode = _dm_colorMode,
            _pad1 = 0,

            ForegroundColor = _dm_foregroundColor,
            BackgroundColor = _dm_backgroundColor,

            Brightness = _dm_brightness,
            Contrast = _dm_contrast,
            Gamma = _dm_gamma,
            Saturation = _dm_saturation,
            _pad2 = Vector4.Zero
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_dotMatrixVS);
        context.SetPixelShader(_dotMatrixPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Draw fullscreen quad
        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Cleanup
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    private void RenderMatrixRain(IRenderContext context, ITexture screenTexture)
    {
        if (_matrixRainVS == null || _matrixRainPS == null) return;

        // Update atlas if needed (for character rendering)
        if (_atlasNeedsUpdate)
        {
            UpdateCharacterAtlas(context);
            _atlasNeedsUpdate = false;
        }

        var atlasTexture = _atlas.Texture;
        if (atlasTexture == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new MatrixRainFilterParams
        {
            MousePosition = _mousePosition,
            LayoutMode = _mr_layoutMode,
            Radius = _mr_radius,
            EdgeSoftness = _mr_edgeSoftness,
            ShapeFeather = _mr_shapeFeather,
            RectWidth = _mr_rectWidth,
            RectHeight = _mr_rectHeight,

            FallSpeed = _mr_fallSpeed,
            TrailLength = _mr_trailLength,
            CharCycleSpeed = _mr_charCycleSpeed,
            ColumnDensity = _mr_columnDensity,
            GlowIntensity = _mr_glowIntensity,
            CellWidth = _mr_cellWidth,
            CellHeight = _mr_cellHeight,
            CharCount = _atlas.CharacterCount,

            PrimaryColor = _mr_primaryColor,
            GlowColor = _mr_glowColor,

            Brightness = _mr_brightness,
            Contrast = _mr_contrast,
            BackgroundFade = _mr_backgroundFade,
            _pad1 = 0,
            _pad2 = Vector4.Zero
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_matrixRainVS);
        context.SetPixelShader(_matrixRainPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
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

    private void RenderBraille(IRenderContext context, ITexture screenTexture)
    {
        if (_brailleVS == null || _braillePS == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new BrailleFilterParams
        {
            MousePosition = _mousePosition,
            LayoutMode = _br_layoutMode,
            Radius = _br_radius,
            EdgeSoftness = _br_edgeSoftness,
            ShapeFeather = _br_shapeFeather,
            RectWidth = _br_rectWidth,
            RectHeight = _br_rectHeight,

            Threshold = _br_threshold,
            AdaptiveThreshold = _br_adaptiveThreshold ? 1f : 0f,
            DotSize = _br_dotSize,
            DotSpacing = _br_dotSpacing,
            InvertDots = _br_invertDots ? 1f : 0f,
            CellWidth = _br_cellWidth,
            CellHeight = _br_cellHeight,
            _pad1 = 0,

            ForegroundColor = _br_foregroundColor,
            BackgroundColor = _br_backgroundColor,

            Brightness = _br_brightness,
            Contrast = _br_contrast,
            _pad2 = 0,
            _pad3 = 0,
            _pad4 = Vector4.Zero
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_brailleVS);
        context.SetPixelShader(_braillePS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Draw fullscreen quad
        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Cleanup
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    private void RenderTypewriter(IRenderContext context, ITexture screenTexture)
    {
        if (_typewriterVS == null || _typewriterPS == null) return;

        // Update atlas if needed (for character rendering)
        if (_atlasNeedsUpdate)
        {
            UpdateCharacterAtlas(context);
            _atlasNeedsUpdate = false;
        }

        var atlasTexture = _atlas.Texture;
        if (atlasTexture == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new TypewriterFilterParams
        {
            MousePosition = _mousePosition,
            LayoutMode = _tw_layoutMode,
            Radius = _tw_radius,
            EdgeSoftness = _tw_edgeSoftness,
            ShapeFeather = _tw_shapeFeather,
            RectWidth = _tw_rectWidth,
            RectHeight = _tw_rectHeight,

            CellWidth = _tw_cellWidth,
            CellHeight = _tw_cellHeight,
            CharCount = _atlas.CharacterCount,
            _pad1 = 0,

            InkVariation = _tw_inkVariation,
            PositionJitter = _tw_positionJitter,
            RibbonWear = _tw_ribbonWear ? 1f : 0f,
            DoubleStrike = _tw_doubleStrike ? 1f : 0f,
            StrikeOffset = 1f,
            AgeEffect = _tw_ageEffect,
            _pad2 = 0,
            _pad3 = 0,

            InkColor = _tw_inkColor,
            PaperColor = _tw_paperColor,

            Brightness = _tw_brightness,
            Contrast = _tw_contrast,
            _pad4 = 0,
            _pad5 = 0
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_typewriterVS);
        context.SetPixelShader(_typewriterPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
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

    private void RenderEdgeASCII(IRenderContext context, ITexture screenTexture)
    {
        if (_edgeASCIIVS == null || _edgeASCIIPS == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new EdgeASCIIFilterParams
        {
            MousePosition = _mousePosition,
            LayoutMode = _ea_layoutMode,
            Radius = _ea_radius,
            EdgeSoftness = _ea_edgeSoftness,
            ShapeFeather = _ea_shapeFeather,
            RectWidth = _ea_rectWidth,
            RectHeight = _ea_rectHeight,

            CellWidth = _ea_cellWidth,
            CellHeight = _ea_cellHeight,
            CharCount = 10, // Edge ASCII uses 10 characters for edge intensity
            _pad1 = 0,

            EdgeThreshold = _ea_edgeThreshold,
            LineThickness = _ea_lineThickness,
            ShowCorners = _ea_showCorners ? 1f : 0f,
            FillBackground = _ea_fillBackground ? 1f : 0f,
            BackgroundOpacity = _ea_backgroundOpacity,
            EdgeBrightness = _ea_edgeBrightness,
            _pad2 = 0,
            _pad3 = 0,

            EdgeColor = _ea_edgeColor,
            BackgroundColor = _ea_backgroundColor,

            Brightness = _ea_brightness,
            Contrast = _ea_contrast,
            _pad4 = 0,
            _pad5 = 0
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_edgeASCIIVS);
        context.SetPixelShader(_edgeASCIIPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Draw fullscreen quad
        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Cleanup
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    protected override void OnDispose()
    {
        _atlas.Dispose();
        _asciiClassicVS?.Dispose();
        _asciiClassicPS?.Dispose();
        _dotMatrixVS?.Dispose();
        _dotMatrixPS?.Dispose();
        _matrixRainVS?.Dispose();
        _matrixRainPS?.Dispose();
        _brailleVS?.Dispose();
        _braillePS?.Dispose();
        _typewriterVS?.Dispose();
        _typewriterPS?.Dispose();
        _edgeASCIIVS?.Dispose();
        _edgeASCIIPS?.Dispose();
        _postEffectsBuffer?.Dispose();
        _filterParamsBuffer?.Dispose();
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
    /// Shared post-effects constant buffer (128 bytes, register b0).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct PostEffectsParams
    {
        // Core (32 bytes)
        public Vector2 ViewportSize;
        public float Time;
        public float Scanlines;
        public float ScanlineIntensity;
        public float ScanlineSpacing;
        public float CrtCurvature;
        public float CrtAmount;

        // Effects (32 bytes)
        public float PhosphorGlow;
        public float PhosphorIntensity;
        public float Chromatic;
        public float ChromaticOffset;
        public float Vignette;
        public float VignetteIntensity;
        public float VignetteRadius;
        public float Noise;

        // More effects (32 bytes)
        public float NoiseAmount;
        public float Flicker;
        public float FlickerSpeed;
        public float _pad1;
        public Vector4 _pad2;

        // Reserved (32 bytes)
        public Vector4 _reserved1;
        public Vector4 _reserved2;
    }

    /// <summary>
    /// ASCII Classic filter constant buffer (192 bytes, register b1).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 192)]
    private struct ASCIIClassicFilterParams
    {
        // Core (32 bytes)
        public Vector2 MousePosition;
        public float LayoutMode;
        public float Radius;
        public float EdgeSoftness;
        public float ShapeFeather;
        public float RectWidth;
        public float RectHeight;

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

        // Character rendering (32 bytes)
        public float Antialiasing;
        public float CharShadow;
        public Vector2 ShadowOffset;
        public Vector4 ShadowColor;

        // Grid & glow (48 bytes)
        public float GlowOnBright;
        public float GlowThreshold;
        public float GlowRadius;
        public float GridLines;
        public float GridThickness;
        public float InnerGlow;
        public float InnerGlowSize;
        public float _pad1;
        public Vector4 GridColor;
        public Vector4 InnerGlowColor;
    }

    /// <summary>
    /// Dot Matrix filter constant buffer (128 bytes, register b1).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct DotMatrixFilterParams
    {
        // Core (32 bytes)
        public Vector2 MousePosition;
        public float LayoutMode;
        public float Radius;
        public float EdgeSoftness;
        public float ShapeFeather;
        public float RectWidth;
        public float RectHeight;

        // Dot settings (32 bytes)
        public float DotSize;
        public float DotSpacing;
        public float CellSize;
        public float LedShape;
        public float OffBrightness;
        public float RgbMode;
        public float ColorMode;
        public float _pad1;

        // Colors (32 bytes)
        public Vector4 ForegroundColor;
        public Vector4 BackgroundColor;

        // Brightness (32 bytes)
        public float Brightness;
        public float Contrast;
        public float Gamma;
        public float Saturation;
        public Vector4 _pad2;
    }

    /// <summary>
    /// Matrix Rain filter constant buffer (128 bytes, register b1).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct MatrixRainFilterParams
    {
        // Core (32 bytes)
        public Vector2 MousePosition;
        public float LayoutMode;
        public float Radius;
        public float EdgeSoftness;
        public float ShapeFeather;
        public float RectWidth;
        public float RectHeight;

        // Rain settings (32 bytes)
        public float FallSpeed;
        public float TrailLength;
        public float CharCycleSpeed;
        public float ColumnDensity;
        public float GlowIntensity;
        public float CellWidth;
        public float CellHeight;
        public float CharCount;

        // Colors (32 bytes)
        public Vector4 PrimaryColor;
        public Vector4 GlowColor;

        // Brightness (32 bytes)
        public float Brightness;
        public float Contrast;
        public float BackgroundFade;
        public float _pad1;
        public Vector4 _pad2;
    }

    /// <summary>
    /// Braille filter constant buffer (128 bytes, register b1).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct BrailleFilterParams
    {
        // Core (32 bytes)
        public Vector2 MousePosition;
        public float LayoutMode;
        public float Radius;
        public float EdgeSoftness;
        public float ShapeFeather;
        public float RectWidth;
        public float RectHeight;

        // Braille settings (32 bytes)
        public float Threshold;
        public float AdaptiveThreshold;
        public float DotSize;
        public float DotSpacing;
        public float InvertDots;
        public float CellWidth;
        public float CellHeight;
        public float _pad1;

        // Colors (32 bytes)
        public Vector4 ForegroundColor;
        public Vector4 BackgroundColor;

        // Brightness (32 bytes)
        public float Brightness;
        public float Contrast;
        public float _pad2;
        public float _pad3;
        public Vector4 _pad4;
    }

    /// <summary>
    /// Typewriter filter constant buffer (128 bytes, register b1).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct TypewriterFilterParams
    {
        // Core (32 bytes)
        public Vector2 MousePosition;
        public float LayoutMode;
        public float Radius;
        public float EdgeSoftness;
        public float ShapeFeather;
        public float RectWidth;
        public float RectHeight;

        // Cell settings (16 bytes)
        public float CellWidth;
        public float CellHeight;
        public float CharCount;
        public float _pad1;

        // Typewriter settings (32 bytes)
        public float InkVariation;
        public float PositionJitter;
        public float RibbonWear;
        public float DoubleStrike;
        public float StrikeOffset;
        public float AgeEffect;
        public float _pad2;
        public float _pad3;

        // Colors (32 bytes)
        public Vector4 InkColor;
        public Vector4 PaperColor;

        // Brightness (16 bytes)
        public float Brightness;
        public float Contrast;
        public float _pad4;
        public float _pad5;
    }

    /// <summary>
    /// Edge ASCII filter constant buffer (128 bytes, register b1).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct EdgeASCIIFilterParams
    {
        // Core (32 bytes)
        public Vector2 MousePosition;
        public float LayoutMode;
        public float Radius;
        public float EdgeSoftness;
        public float ShapeFeather;
        public float RectWidth;
        public float RectHeight;

        // Cell settings (16 bytes)
        public float CellWidth;
        public float CellHeight;
        public float CharCount;
        public float _pad1;

        // Edge detection (32 bytes)
        public float EdgeThreshold;
        public float LineThickness;
        public float ShowCorners;
        public float FillBackground;
        public float BackgroundOpacity;
        public float EdgeBrightness;
        public float _pad2;
        public float _pad3;

        // Colors (32 bytes)
        public Vector4 EdgeColor;
        public Vector4 BackgroundColor;

        // Brightness (16 bytes)
        public float Brightness;
        public float Contrast;
        public float _pad4;
        public float _pad5;
    }
}
