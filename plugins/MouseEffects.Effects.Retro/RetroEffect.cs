using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Retro;

/// <summary>
/// Retro effect that applies pixel art scaling filters (xSaI, etc.) to the screen.
/// Uses split constant buffers: b0 for shared post-effects, b1 for filter-specific params.
/// </summary>
public sealed class RetroEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "retro",
        Name = "Retro",
        Description = "Retro scaling filters for pixel art style effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    // GPU resources - shared
    private IBuffer? _postEffectsBuffer;    // b0 - shared post-effects
    private IBuffer? _filterParamsBuffer;   // b1 - filter-specific params
    private ISamplerState? _linearSampler;
    private ISamplerState? _pointSampler;

    // XSaI shader
    private IShader? _xsaiVS;
    private IShader? _xsaiPS;

    // TV Filter shader
    private IShader? _tvFilterVS;
    private IShader? _tvFilterPS;

    // Toon Filter shader
    private IShader? _toonFilterVS;
    private IShader? _toonFilterPS;

    // Animation time and mouse position
    private float _totalTime;
    private Vector2 _mousePosition;

    // Global settings
    private FilterType _filterType = FilterType.XSaI;

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

    #region Layout Properties

    private int _layoutMode;
    private float _radius = 200f;
    private float _rectWidth = 400f;
    private float _rectHeight = 300f;
    private float _edgeSoftness = 20f;

    #endregion

    #region XSaI Filter Properties

    private int _xs_mode;
    private float _xs_pixelSize = 4f;
    private int _xs_scaleFactor = 4;
    private float _xs_strength = 1f;

    #endregion

    #region TV Filter Properties

    private float _tv_phosphorWidth = 0.7f;
    private float _tv_phosphorHeight = 0.85f;
    private float _tv_phosphorGap = 0.05f;
    private float _tv_brightness = 2.0f;

    #endregion

    #region Toon Filter Properties

    private float _toon_edgeThreshold = 0.1f;
    private float _toon_edgeWidth = 1.5f;
    private float _toon_colorLevels = 6f;
    private float _toon_saturation = 1.2f;

    #endregion

    #region Public Properties - Global

    public override EffectMetadata Metadata => _metadata;

    public override bool RequiresContinuousScreenCapture => true;

    public FilterType FilterType { get => _filterType; set => _filterType = value; }

    #endregion

    #region Public Properties - Post-Effects

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

    #region Public Properties - Layout

    public int LayoutMode { get => _layoutMode; set => _layoutMode = Math.Clamp(value, 0, 2); }
    public float Radius { get => _radius; set => _radius = Math.Clamp(value, 50f, 500f); }
    public float RectWidth { get => _rectWidth; set => _rectWidth = Math.Clamp(value, 100f, 800f); }
    public float RectHeight { get => _rectHeight; set => _rectHeight = Math.Clamp(value, 100f, 600f); }
    public float EdgeSoftness { get => _edgeSoftness; set => _edgeSoftness = Math.Clamp(value, 0f, 100f); }

    #endregion

    #region Public Properties - XSaI

    public int XS_Mode { get => _xs_mode; set => _xs_mode = Math.Clamp(value, 0, 2); }
    public float XS_PixelSize { get => _xs_pixelSize; set => _xs_pixelSize = Math.Clamp(value, 2f, 16f); }
    public int XS_ScaleFactor { get => _xs_scaleFactor; set => _xs_scaleFactor = Math.Clamp(value, 2, 16); }
    public float XS_Strength { get => _xs_strength; set => _xs_strength = Math.Clamp(value, 0f, 1f); }

    #endregion

    #region Public Properties - TV Filter

    public float TV_PhosphorWidth { get => _tv_phosphorWidth; set => _tv_phosphorWidth = Math.Clamp(value, 0.3f, 0.9f); }
    public float TV_PhosphorHeight { get => _tv_phosphorHeight; set => _tv_phosphorHeight = Math.Clamp(value, 0.5f, 1f); }
    public float TV_PhosphorGap { get => _tv_phosphorGap; set => _tv_phosphorGap = Math.Clamp(value, 0f, 0.2f); }
    public float TV_Brightness { get => _tv_brightness; set => _tv_brightness = Math.Clamp(value, 1f, 3f); }

    #endregion

    #region Public Properties - Toon Filter

    public float Toon_EdgeThreshold { get => _toon_edgeThreshold; set => _toon_edgeThreshold = Math.Clamp(value, 0.01f, 1.0f); }
    public float Toon_EdgeWidth { get => _toon_edgeWidth; set => _toon_edgeWidth = Math.Clamp(value, 1f, 5f); }
    public float Toon_ColorLevels { get => _toon_colorLevels; set => _toon_colorLevels = Math.Clamp(value, 2f, 16f); }
    public float Toon_Saturation { get => _toon_saturation; set => _toon_saturation = Math.Clamp(value, 0.5f, 2f); }

    #endregion

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile XSaI shader
        var xsaiSource = LoadEmbeddedShader("XSaI.hlsl");
        _xsaiVS = context.CompileShader(xsaiSource, "VSMain", ShaderStage.Vertex);
        _xsaiPS = context.CompileShader(xsaiSource, "PSMain", ShaderStage.Pixel);

        // Load and compile TV Filter shader
        var tvSource = LoadEmbeddedShader("TVFilter.hlsl");
        _tvFilterVS = context.CompileShader(tvSource, "VSMain", ShaderStage.Vertex);
        _tvFilterPS = context.CompileShader(tvSource, "PSMain", ShaderStage.Pixel);

        // Load and compile Toon Filter shader
        var toonSource = LoadEmbeddedShader("ToonFilter.hlsl");
        _toonFilterVS = context.CompileShader(toonSource, "VSMain", ShaderStage.Vertex);
        _toonFilterPS = context.CompileShader(toonSource, "PSMain", ShaderStage.Pixel);

        // Create shared post-effects constant buffer
        _postEffectsBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<PostEffectsParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create filter params buffer (use largest struct size - TVFilterParams is 80 bytes)
        _filterParamsBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<TVFilterParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create samplers
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
        _pointSampler = context.CreateSamplerState(SamplerDescription.PointClamp);
    }

    private string LoadEmbeddedShader(string fileName)
    {
        var assembly = typeof(RetroEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Retro.Shaders.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    protected override void OnConfigurationChanged()
    {
        // Global
        if (Configuration.TryGet("filterType", out int filterType))
            _filterType = (FilterType)filterType;

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

        // Layout
        if (Configuration.TryGet("layoutMode", out int layoutMode))
            _layoutMode = layoutMode;
        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;
        if (Configuration.TryGet("rectWidth", out float rectWidth))
            _rectWidth = rectWidth;
        if (Configuration.TryGet("rectHeight", out float rectHeight))
            _rectHeight = rectHeight;
        if (Configuration.TryGet("edgeSoftness", out float edgeSoftness))
            _edgeSoftness = edgeSoftness;

        // XSaI Filter
        if (Configuration.TryGet("xs_mode", out int xsMode))
            _xs_mode = xsMode;
        if (Configuration.TryGet("xs_pixelSize", out float xsPixelSize))
            _xs_pixelSize = xsPixelSize;
        if (Configuration.TryGet("xs_scaleFactor", out int xsScaleFactor))
            _xs_scaleFactor = xsScaleFactor;
        if (Configuration.TryGet("xs_strength", out float xsStrength))
            _xs_strength = xsStrength;

        // TV Filter
        if (Configuration.TryGet("tv_phosphorWidth", out float tvPhosphorWidth))
            _tv_phosphorWidth = tvPhosphorWidth;
        if (Configuration.TryGet("tv_phosphorHeight", out float tvPhosphorHeight))
            _tv_phosphorHeight = tvPhosphorHeight;
        if (Configuration.TryGet("tv_phosphorGap", out float tvPhosphorGap))
            _tv_phosphorGap = tvPhosphorGap;
        if (Configuration.TryGet("tv_brightness", out float tvBrightness))
            _tv_brightness = tvBrightness;

        // Toon Filter
        if (Configuration.TryGet("toon_edgeThreshold", out float toonEdgeThreshold))
            _toon_edgeThreshold = toonEdgeThreshold;
        if (Configuration.TryGet("toon_edgeWidth", out float toonEdgeWidth))
            _toon_edgeWidth = toonEdgeWidth;
        if (Configuration.TryGet("toon_colorLevels", out float toonColorLevels))
            _toon_colorLevels = toonColorLevels;
        if (Configuration.TryGet("toon_saturation", out float toonSaturation))
            _toon_saturation = toonSaturation;
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
            case FilterType.ToonFilter:
                RenderToonFilter(context, screenTexture);
                break;
            case FilterType.TVFilter:
                RenderTVFilter(context, screenTexture);
                break;
            case FilterType.XSaI:
            default:
                RenderXSaI(context, screenTexture);
                break;
        }
    }

    private void RenderXSaI(IRenderContext context, ITexture screenTexture)
    {
        if (_xsaiVS == null || _xsaiPS == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new XSaIFilterParams
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _mousePosition,
            TexelSize = new Vector2(1f / context.ViewportSize.X, 1f / context.ViewportSize.Y),
            LayoutMode = _layoutMode,
            Radius = _radius,

            RectWidth = _rectWidth,
            RectHeight = _rectHeight,
            EdgeSoftness = _edgeSoftness,
            Mode = _xs_mode,
            PixelSize = _xs_pixelSize,
            ScaleFactor = _xs_scaleFactor,
            Strength = _xs_strength,
            Time = _totalTime
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_xsaiVS);
        context.SetPixelShader(_xsaiPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _pointSampler!);
        context.SetSampler(ShaderStage.Pixel, 1, _linearSampler!);

        // Draw fullscreen quad
        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Cleanup
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    private void RenderTVFilter(IRenderContext context, ITexture screenTexture)
    {
        if (_tvFilterVS == null || _tvFilterPS == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new TVFilterParams
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _mousePosition,
            TexelSize = new Vector2(1f / context.ViewportSize.X, 1f / context.ViewportSize.Y),
            LayoutMode = _layoutMode,
            Radius = _radius,

            RectWidth = _rectWidth,
            RectHeight = _rectHeight,
            EdgeSoftness = _edgeSoftness,
            Mode = _xs_mode,
            PixelSize = _xs_pixelSize,
            ScaleFactor = _xs_scaleFactor,
            Strength = _xs_strength,
            Time = _totalTime,

            PhosphorWidth = _tv_phosphorWidth,
            PhosphorHeight = _tv_phosphorHeight,
            PhosphorGap = _tv_phosphorGap,
            Brightness = _tv_brightness
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_tvFilterVS);
        context.SetPixelShader(_tvFilterPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _pointSampler!);
        context.SetSampler(ShaderStage.Pixel, 1, _linearSampler!);

        // Draw fullscreen quad
        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Cleanup
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    private void RenderToonFilter(IRenderContext context, ITexture screenTexture)
    {
        if (_toonFilterVS == null || _toonFilterPS == null) return;

        // Build and update filter params buffer (b1)
        var filterParams = new ToonFilterParams
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _mousePosition,
            TexelSize = new Vector2(1f / context.ViewportSize.X, 1f / context.ViewportSize.Y),
            LayoutMode = _layoutMode,
            Radius = _radius,

            RectWidth = _rectWidth,
            RectHeight = _rectHeight,
            EdgeSoftness = _edgeSoftness,
            Mode = _xs_mode,
            PixelSize = _xs_pixelSize,
            ScaleFactor = _xs_scaleFactor,
            Strength = _xs_strength,
            Time = _totalTime,

            EdgeThreshold = _toon_edgeThreshold,
            EdgeWidth = _toon_edgeWidth,
            ColorLevels = _toon_colorLevels,
            Saturation = _toon_saturation
        };

        context.UpdateBuffer(_filterParamsBuffer!, filterParams);

        // Set shaders
        context.SetVertexShader(_toonFilterVS);
        context.SetPixelShader(_toonFilterPS);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _postEffectsBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 1, _filterParamsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _pointSampler!);
        context.SetSampler(ShaderStage.Pixel, 1, _linearSampler!);

        // Draw fullscreen quad
        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Cleanup
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    protected override void OnDispose()
    {
        _xsaiVS?.Dispose();
        _xsaiPS?.Dispose();
        _tvFilterVS?.Dispose();
        _tvFilterPS?.Dispose();
        _toonFilterVS?.Dispose();
        _toonFilterPS?.Dispose();
        _postEffectsBuffer?.Dispose();
        _filterParamsBuffer?.Dispose();
        _linearSampler?.Dispose();
        _pointSampler?.Dispose();
    }

    #region Constant Buffer Structures

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

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct XSaIFilterParams
    {
        // Core layout (32 bytes)
        public Vector2 ViewportSize;
        public Vector2 MousePosition;
        public Vector2 TexelSize;
        public float LayoutMode;
        public float Radius;

        // Layout continued (32 bytes)
        public float RectWidth;
        public float RectHeight;
        public float EdgeSoftness;
        public float Mode;
        public float PixelSize;
        public float ScaleFactor;
        public float Strength;
        public float Time;
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct TVFilterParams
    {
        // Core layout (32 bytes)
        public Vector2 ViewportSize;
        public Vector2 MousePosition;
        public Vector2 TexelSize;
        public float LayoutMode;
        public float Radius;

        // Layout continued (32 bytes)
        public float RectWidth;
        public float RectHeight;
        public float EdgeSoftness;
        public float Mode;
        public float PixelSize;
        public float ScaleFactor;
        public float Strength;
        public float Time;

        // TV-specific (16 bytes)
        public float PhosphorWidth;
        public float PhosphorHeight;
        public float PhosphorGap;
        public float Brightness;
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct ToonFilterParams
    {
        // Core layout (32 bytes)
        public Vector2 ViewportSize;
        public Vector2 MousePosition;
        public Vector2 TexelSize;
        public float LayoutMode;
        public float Radius;

        // Layout continued (32 bytes)
        public float RectWidth;
        public float RectHeight;
        public float EdgeSoftness;
        public float Mode;
        public float PixelSize;
        public float ScaleFactor;
        public float Strength;
        public float Time;

        // Toon-specific (16 bytes)
        public float EdgeThreshold;
        public float EdgeWidth;
        public float ColorLevels;
        public float Saturation;
    }

    #endregion
}
