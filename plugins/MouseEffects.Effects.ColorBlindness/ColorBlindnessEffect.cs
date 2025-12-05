using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ColorBlindness;

/// <summary>
/// Layout modes for the color blindness correction effect.
/// </summary>
public enum LayoutMode
{
    Fullscreen = 0,
    Circle = 1,
    Rectangle = 2,
    SplitVertical = 3,
    SplitHorizontal = 4,
    Quadrants = 5
}

/// <summary>
/// Settings for a single zone in the effect.
/// </summary>
public class ZoneSettings
{
    public int CorrectionMode { get; set; } = 0; // 0=LMS, 1=RGB
    public int LMSFilterType { get; set; } = 0;  // 0-10
    public Vector4 MatrixRow0 { get; set; } = new(1.0f, 0.0f, 0.0f, 0.0f);
    public Vector4 MatrixRow1 { get; set; } = new(0.0f, 1.0f, 0.0f, 0.0f);
    public Vector4 MatrixRow2 { get; set; } = new(0.0f, 0.0f, 1.0f, 0.0f);

    public ZoneSettings Clone()
    {
        return new ZoneSettings
        {
            CorrectionMode = CorrectionMode,
            LMSFilterType = LMSFilterType,
            MatrixRow0 = MatrixRow0,
            MatrixRow1 = MatrixRow1,
            MatrixRow2 = MatrixRow2
        };
    }
}

/// <summary>
/// Color blindness correction effect using Daltonization algorithm.
/// Corrects colors for people with color vision deficiency.
/// Supports multiple layout modes with up to 4 zones.
/// </summary>
public sealed class ColorBlindnessEffect : EffectBase, IHotkeyProvider
{
    public const int CurveLutSize = 256;
    public const int MaxZones = 4;

    private const float DefaultRadius = 300.0f;
    private const float DefaultRectWidth = 400.0f;
    private const float DefaultRectHeight = 300.0f;
    private const float DefaultSplitPosition = 0.5f;
    private const float DefaultIntensity = 1.0f;
    private const float DefaultColorBoost = 1.0f;
    private const float DefaultEdgeSoftness = 0.2f;
    private const bool DefaultEnableCurves = false;
    private const float DefaultCurveStrength = 1.0f;
    private const bool DefaultComparisonMode = false;
    private const bool DefaultEnableComparisonHotkey = true;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "color-blindness",
        Name = "Color Blindness Correction",
        Description = "Corrects colors for color vision deficiency using Daltonization. Supports multiple layout modes with up to 4 zones.",
        Author = "MouseEffects",
        Version = new Version(2, 0, 0),
        Category = EffectCategory.Accessibility
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;
    private ISamplerState? _pointSampler;
    private ITexture? _curveLut;

    // Effect parameters
    private LayoutMode _layoutMode = LayoutMode.Fullscreen;
    private float _radius = DefaultRadius;
    private float _rectWidth = DefaultRectWidth;
    private float _rectHeight = DefaultRectHeight;
    private float _splitPosition = DefaultSplitPosition;
    private float _splitPositionV = DefaultSplitPosition;
    private float _intensity = DefaultIntensity;
    private float _colorBoost = DefaultColorBoost;
    private float _edgeSoftness = DefaultEdgeSoftness;
    private bool _enableCurves = DefaultEnableCurves;
    private float _curveStrength = DefaultCurveStrength;
    private bool _comparisonMode = DefaultComparisonMode;
    private bool _enableComparisonHotkey = DefaultEnableComparisonHotkey;
    private Vector2 _mousePosition;

    // Zone settings (4 zones)
    private readonly ZoneSettings[] _zones = new ZoneSettings[MaxZones];

    // Curve data - array of control points for R, G, B, and Master curves
    private CurveData _redCurve = CurveData.CreateLinear();
    private CurveData _greenCurve = CurveData.CreateLinear();
    private CurveData _blueCurve = CurveData.CreateLinear();
    private CurveData _masterCurve = CurveData.CreateLinear();
    private bool _curvesNeedUpdate = true;

    public ColorBlindnessEffect()
    {
        // Initialize all zones with default settings
        for (int i = 0; i < MaxZones; i++)
        {
            _zones[i] = new ZoneSettings();
        }
    }

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// This effect requires continuous screen capture to show live desktop content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    /// <summary>
    /// Gets the zone settings array.
    /// </summary>
    public ZoneSettings[] Zones => _zones;

    /// <summary>
    /// Gets or sets the layout mode.
    /// </summary>
    public LayoutMode LayoutMode
    {
        get => _layoutMode;
        set => _layoutMode = value;
    }

    /// <summary>
    /// Gets or sets whether comparison mode is enabled.
    /// </summary>
    public bool ComparisonMode
    {
        get => _comparisonMode;
        set => _comparisonMode = value;
    }

    /// <summary>
    /// Gets or sets whether the comparison mode hotkey (Alt+Shift+C) is enabled.
    /// </summary>
    public bool EnableComparisonHotkey
    {
        get => _enableComparisonHotkey;
        set => _enableComparisonHotkey = value;
    }

    /// <summary>
    /// Event raised when comparison mode is toggled via hotkey.
    /// UI should subscribe to update checkbox state.
    /// </summary>
    public event Action<bool>? ComparisonModeChanged;

    /// <summary>
    /// Implements IHotkeyProvider. Returns the hotkey definitions for this effect.
    /// </summary>
    public IEnumerable<HotkeyDefinition> GetHotkeys()
    {
        yield return new HotkeyDefinition
        {
            Id = "toggle-comparison",
            DisplayName = "Toggle Comparison Mode",
            Modifiers = HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            Key = HotkeyKey.C,
            IsEnabled = _enableComparisonHotkey && IsEnabled && _layoutMode >= LayoutMode.SplitVertical,
            Callback = ToggleComparisonMode
        };
    }

    /// <summary>
    /// Toggles comparison mode on/off via hotkey.
    /// </summary>
    public void ToggleComparisonMode()
    {
        // Only toggle if in a layout that supports comparison mode
        if (_layoutMode >= LayoutMode.SplitVertical)
        {
            _comparisonMode = !_comparisonMode;
            ComparisonModeChanged?.Invoke(_comparisonMode);
        }
    }

    /// <summary>
    /// Gets or sets the red channel curve data.
    /// </summary>
    public CurveData RedCurve
    {
        get => _redCurve;
        set
        {
            _redCurve = value;
            _curvesNeedUpdate = true;
        }
    }

    /// <summary>
    /// Gets or sets the green channel curve data.
    /// </summary>
    public CurveData GreenCurve
    {
        get => _greenCurve;
        set
        {
            _greenCurve = value;
            _curvesNeedUpdate = true;
        }
    }

    /// <summary>
    /// Gets or sets the blue channel curve data.
    /// </summary>
    public CurveData BlueCurve
    {
        get => _blueCurve;
        set
        {
            _blueCurve = value;
            _curvesNeedUpdate = true;
        }
    }

    /// <summary>
    /// Gets or sets the master (RGB) curve data.
    /// </summary>
    public CurveData MasterCurve
    {
        get => _masterCurve;
        set
        {
            _masterCurve = value;
            _curvesNeedUpdate = true;
        }
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ColorBlindness.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<ColorBlindnessParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create samplers
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
        _pointSampler = context.CreateSamplerState(SamplerDescription.PointClamp);

        // Create curve LUT texture
        CreateCurveLut(context);
    }

    private void CreateCurveLut(IRenderContext context)
    {
        // Generate initial linear LUT
        var lutData = GenerateCurveLutData();

        var texDesc = new TextureDescription
        {
            Width = CurveLutSize,
            Height = 1,
            Format = TextureFormat.R32G32B32A32_Float,
            ShaderResource = true
        };

        _curveLut = context.CreateTexture(texDesc, lutData);
    }

    private byte[] GenerateCurveLutData()
    {
        var data = new float[CurveLutSize * 4]; // RGBA

        for (int i = 0; i < CurveLutSize; i++)
        {
            float t = i / (float)(CurveLutSize - 1);

            // Evaluate each curve
            data[i * 4 + 0] = _redCurve.Evaluate(t);     // R
            data[i * 4 + 1] = _greenCurve.Evaluate(t);   // G
            data[i * 4 + 2] = _blueCurve.Evaluate(t);    // B
            data[i * 4 + 3] = _masterCurve.Evaluate(t);  // Master (stored in A)
        }

        // Convert to bytes
        var bytes = new byte[data.Length * sizeof(float)];
        Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private void UpdateCurveLut(IRenderContext context)
    {
        if (!_curvesNeedUpdate) return;

        // Dispose old texture and recreate with new data
        _curveLut?.Dispose();

        var lutData = GenerateCurveLutData();
        var texDesc = new TextureDescription
        {
            Width = CurveLutSize,
            Height = 1,
            Format = TextureFormat.R32G32B32A32_Float,
            ShaderResource = true
        };

        _curveLut = context.CreateTexture(texDesc, lutData);
        _curvesNeedUpdate = false;
    }

    protected override void OnConfigurationChanged()
    {
        // Layout mode
        if (Configuration.TryGet("layoutMode", out int layoutMode))
            _layoutMode = (LayoutMode)layoutMode;

        // General settings
        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;

        if (Configuration.TryGet("rectWidth", out float rectWidth))
            _rectWidth = rectWidth;

        if (Configuration.TryGet("rectHeight", out float rectHeight))
            _rectHeight = rectHeight;

        if (Configuration.TryGet("splitPosition", out float splitPosition))
            _splitPosition = splitPosition;

        if (Configuration.TryGet("splitPositionV", out float splitPositionV))
            _splitPositionV = splitPositionV;

        if (Configuration.TryGet("intensity", out float intensity))
            _intensity = intensity;

        if (Configuration.TryGet("colorBoost", out float colorBoost))
            _colorBoost = colorBoost;

        if (Configuration.TryGet("edgeSoftness", out float edgeSoftness))
            _edgeSoftness = edgeSoftness;

        if (Configuration.TryGet("enableCurves", out bool enableCurves))
            _enableCurves = enableCurves;

        if (Configuration.TryGet("curveStrength", out float curveStrength))
            _curveStrength = curveStrength;

        if (Configuration.TryGet("comparisonMode", out bool comparisonMode))
            _comparisonMode = comparisonMode;

        if (Configuration.TryGet("enableComparisonHotkey", out bool enableComparisonHotkey))
            _enableComparisonHotkey = enableComparisonHotkey;

        // Load zone settings
        for (int z = 0; z < MaxZones; z++)
        {
            string prefix = $"zone{z}_";

            if (Configuration.TryGet($"{prefix}correctionMode", out int correctionMode))
                _zones[z].CorrectionMode = correctionMode;

            if (Configuration.TryGet($"{prefix}lmsFilterType", out int lmsFilterType))
                _zones[z].LMSFilterType = lmsFilterType;

            // Load matrix values
            var row0 = _zones[z].MatrixRow0;
            var row1 = _zones[z].MatrixRow1;
            var row2 = _zones[z].MatrixRow2;

            if (Configuration.TryGet($"{prefix}matrixR0", out float r0)) row0.X = r0;
            if (Configuration.TryGet($"{prefix}matrixR1", out float r1)) row0.Y = r1;
            if (Configuration.TryGet($"{prefix}matrixR2", out float r2)) row0.Z = r2;
            if (Configuration.TryGet($"{prefix}matrixG0", out float g0)) row1.X = g0;
            if (Configuration.TryGet($"{prefix}matrixG1", out float g1)) row1.Y = g1;
            if (Configuration.TryGet($"{prefix}matrixG2", out float g2)) row1.Z = g2;
            if (Configuration.TryGet($"{prefix}matrixB0", out float b0)) row2.X = b0;
            if (Configuration.TryGet($"{prefix}matrixB1", out float b1)) row2.Y = b1;
            if (Configuration.TryGet($"{prefix}matrixB2", out float b2)) row2.Z = b2;

            _zones[z].MatrixRow0 = row0;
            _zones[z].MatrixRow1 = row1;
            _zones[z].MatrixRow2 = row2;
        }

        // Load curve data if present
        if (Configuration.TryGet("redCurve", out string? redCurveJson) && redCurveJson != null)
            _redCurve = CurveData.FromJson(redCurveJson);

        if (Configuration.TryGet("greenCurve", out string? greenCurveJson) && greenCurveJson != null)
            _greenCurve = CurveData.FromJson(greenCurveJson);

        if (Configuration.TryGet("blueCurve", out string? blueCurveJson) && blueCurveJson != null)
            _blueCurve = CurveData.FromJson(blueCurveJson);

        if (Configuration.TryGet("masterCurve", out string? masterCurveJson) && masterCurveJson != null)
            _masterCurve = CurveData.FromJson(masterCurveJson);

        _curvesNeedUpdate = true;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _mousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Get the screen texture from context
        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Update curve LUT if needed
        UpdateCurveLut(context);

        // Update parameters
        var cbParams = new ColorBlindnessParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            LayoutMode = (float)_layoutMode,
            Radius = _radius,
            RectWidth = _rectWidth,
            RectHeight = _rectHeight,
            SplitPosition = _splitPosition,
            SplitPositionV = _splitPositionV,
            EdgeSoftness = _edgeSoftness,
            Intensity = _intensity,
            ColorBoost = _colorBoost,
            EnableCurves = _enableCurves ? 1.0f : 0.0f,
            CurveStrength = _curveStrength,
            ComparisonMode = _comparisonMode ? 1.0f : 0.0f,

            // Zone 0
            Zone0_CorrectionMode = _zones[0].CorrectionMode,
            Zone0_LMSFilterType = _zones[0].LMSFilterType,
            Zone0_Pad1 = 0,
            Zone0_Pad2 = 0,
            Zone0_MatrixRow0 = _zones[0].MatrixRow0,
            Zone0_MatrixRow1 = _zones[0].MatrixRow1,
            Zone0_MatrixRow2 = _zones[0].MatrixRow2,

            // Zone 1
            Zone1_CorrectionMode = _zones[1].CorrectionMode,
            Zone1_LMSFilterType = _zones[1].LMSFilterType,
            Zone1_Pad1 = 0,
            Zone1_Pad2 = 0,
            Zone1_MatrixRow0 = _zones[1].MatrixRow0,
            Zone1_MatrixRow1 = _zones[1].MatrixRow1,
            Zone1_MatrixRow2 = _zones[1].MatrixRow2,

            // Zone 2
            Zone2_CorrectionMode = _zones[2].CorrectionMode,
            Zone2_LMSFilterType = _zones[2].LMSFilterType,
            Zone2_Pad1 = 0,
            Zone2_Pad2 = 0,
            Zone2_MatrixRow0 = _zones[2].MatrixRow0,
            Zone2_MatrixRow1 = _zones[2].MatrixRow1,
            Zone2_MatrixRow2 = _zones[2].MatrixRow2,

            // Zone 3
            Zone3_CorrectionMode = _zones[3].CorrectionMode,
            Zone3_LMSFilterType = _zones[3].LMSFilterType,
            Zone3_Pad1 = 0,
            Zone3_Pad2 = 0,
            Zone3_MatrixRow0 = _zones[3].MatrixRow0,
            Zone3_MatrixRow1 = _zones[3].MatrixRow1,
            Zone3_MatrixRow2 = _zones[3].MatrixRow2
        };

        context.UpdateBuffer(_paramsBuffer!, cbParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetShaderResource(ShaderStage.Pixel, 1, _curveLut!);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);
        context.SetSampler(ShaderStage.Pixel, 1, _pointSampler!);

        // Use opaque blend - shader renders the full screen with either effect or passthrough content
        context.SetBlendState(BlendMode.Opaque);

        // Draw fullscreen quad
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind resources
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
        context.SetShaderResource(ShaderStage.Pixel, 1, (ITexture?)null);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        // No texture recreation needed
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _paramsBuffer?.Dispose();
        _linearSampler?.Dispose();
        _pointSampler?.Dispose();
        _curveLut?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ColorBlindnessEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.ColorBlindness.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Marks the curve LUT for update on next render.
    /// Call this after modifying curve data directly.
    /// </summary>
    public void InvalidateCurves()
    {
        _curvesNeedUpdate = true;
    }

    /// <summary>
    /// Gets the number of active zones for the current layout mode.
    /// </summary>
    public int GetActiveZoneCount()
    {
        return _layoutMode switch
        {
            LayoutMode.Fullscreen => 1,
            LayoutMode.Circle => 2,
            LayoutMode.Rectangle => 2,
            LayoutMode.SplitVertical => 2,
            LayoutMode.SplitHorizontal => 2,
            LayoutMode.Quadrants => 4,
            _ => 1
        };
    }

    /// <summary>
    /// Gets the display name for a zone based on current layout mode.
    /// </summary>
    public string GetZoneName(int zoneIndex)
    {
        return _layoutMode switch
        {
            LayoutMode.Fullscreen => "Screen",
            LayoutMode.Circle => zoneIndex == 0 ? "Inside Circle" : "Outside Circle",
            LayoutMode.Rectangle => zoneIndex == 0 ? "Inside Rectangle" : "Outside Rectangle",
            LayoutMode.SplitVertical => zoneIndex == 0 ? "Left Side" : "Right Side",
            LayoutMode.SplitHorizontal => zoneIndex == 0 ? "Top Half" : "Bottom Half",
            LayoutMode.Quadrants => zoneIndex switch
            {
                0 => "Top-Left",
                1 => "Top-Right",
                2 => "Bottom-Left",
                3 => "Bottom-Right",
                _ => $"Zone {zoneIndex}"
            },
            _ => $"Zone {zoneIndex}"
        };
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 320)]
    private struct ColorBlindnessParams
    {
        // General settings (64 bytes) - offset 0
        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float LayoutMode;           // 4 bytes, offset 16
        public float Radius;               // 4 bytes, offset 20
        public float RectWidth;            // 4 bytes, offset 24
        public float RectHeight;           // 4 bytes, offset 28
        public float SplitPosition;        // 4 bytes, offset 32
        public float SplitPositionV;       // 4 bytes, offset 36
        public float EdgeSoftness;         // 4 bytes, offset 40
        public float Intensity;            // 4 bytes, offset 44
        public float ColorBoost;           // 4 bytes, offset 48
        public float EnableCurves;         // 4 bytes, offset 52
        public float CurveStrength;        // 4 bytes, offset 56
        public float ComparisonMode;       // 4 bytes, offset 60

        // Zone 0 (64 bytes) - offset 64
        public float Zone0_CorrectionMode; // 4 bytes, offset 64
        public float Zone0_LMSFilterType;  // 4 bytes, offset 68
        public float Zone0_Pad1;           // 4 bytes, offset 72
        public float Zone0_Pad2;           // 4 bytes, offset 76
        public Vector4 Zone0_MatrixRow0;   // 16 bytes, offset 80
        public Vector4 Zone0_MatrixRow1;   // 16 bytes, offset 96
        public Vector4 Zone0_MatrixRow2;   // 16 bytes, offset 112

        // Zone 1 (64 bytes) - offset 128
        public float Zone1_CorrectionMode; // 4 bytes, offset 128
        public float Zone1_LMSFilterType;  // 4 bytes, offset 132
        public float Zone1_Pad1;           // 4 bytes, offset 136
        public float Zone1_Pad2;           // 4 bytes, offset 140
        public Vector4 Zone1_MatrixRow0;   // 16 bytes, offset 144
        public Vector4 Zone1_MatrixRow1;   // 16 bytes, offset 160
        public Vector4 Zone1_MatrixRow2;   // 16 bytes, offset 176

        // Zone 2 (64 bytes) - offset 192
        public float Zone2_CorrectionMode; // 4 bytes, offset 192
        public float Zone2_LMSFilterType;  // 4 bytes, offset 196
        public float Zone2_Pad1;           // 4 bytes, offset 200
        public float Zone2_Pad2;           // 4 bytes, offset 204
        public Vector4 Zone2_MatrixRow0;   // 16 bytes, offset 208
        public Vector4 Zone2_MatrixRow1;   // 16 bytes, offset 224
        public Vector4 Zone2_MatrixRow2;   // 16 bytes, offset 240

        // Zone 3 (64 bytes) - offset 256
        public float Zone3_CorrectionMode; // 4 bytes, offset 256
        public float Zone3_LMSFilterType;  // 4 bytes, offset 260
        public float Zone3_Pad1;           // 4 bytes, offset 264
        public float Zone3_Pad2;           // 4 bytes, offset 268
        public Vector4 Zone3_MatrixRow0;   // 16 bytes, offset 272
        public Vector4 Zone3_MatrixRow1;   // 16 bytes, offset 288
        public Vector4 Zone3_MatrixRow2;   // 16 bytes, offset 304
    }

    #endregion
}

/// <summary>
/// Represents a curve with control points for color adjustment.
/// </summary>
public class CurveData
{
    public List<Vector2> ControlPoints { get; set; } = new();

    /// <summary>
    /// Creates a linear curve (no adjustment).
    /// </summary>
    public static CurveData CreateLinear()
    {
        return new CurveData
        {
            ControlPoints = new List<Vector2>
            {
                new(0.0f, 0.0f),
                new(1.0f, 1.0f)
            }
        };
    }

    /// <summary>
    /// Evaluates the curve at the given position using Catmull-Rom spline interpolation.
    /// </summary>
    public float Evaluate(float t)
    {
        if (ControlPoints.Count == 0) return t;
        if (ControlPoints.Count == 1) return ControlPoints[0].Y;

        t = Math.Clamp(t, 0f, 1f);

        // Find the segment containing t
        var sortedPoints = ControlPoints.OrderBy(p => p.X).ToList();

        // Handle edge cases
        if (t <= sortedPoints[0].X) return sortedPoints[0].Y;
        if (t >= sortedPoints[^1].X) return sortedPoints[^1].Y;

        // Find segment
        int segmentIndex = 0;
        for (int i = 0; i < sortedPoints.Count - 1; i++)
        {
            if (t >= sortedPoints[i].X && t <= sortedPoints[i + 1].X)
            {
                segmentIndex = i;
                break;
            }
        }

        // Get control points for Catmull-Rom (need 4 points)
        Vector2 p0 = segmentIndex > 0 ? sortedPoints[segmentIndex - 1] : sortedPoints[segmentIndex];
        Vector2 p1 = sortedPoints[segmentIndex];
        Vector2 p2 = sortedPoints[segmentIndex + 1];
        Vector2 p3 = segmentIndex + 2 < sortedPoints.Count ? sortedPoints[segmentIndex + 2] : sortedPoints[segmentIndex + 1];

        // Calculate local t within segment
        float segmentT = (t - p1.X) / (p2.X - p1.X);

        // Catmull-Rom interpolation
        float result = CatmullRom(p0.Y, p1.Y, p2.Y, p3.Y, segmentT);

        return Math.Clamp(result, 0f, 1f);
    }

    private static float CatmullRom(float p0, float p1, float p2, float p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2.0f * p1) +
            (-p0 + p2) * t +
            (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
            (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
        );
    }

    /// <summary>
    /// Serializes the curve to JSON.
    /// </summary>
    public string ToJson()
    {
        var points = ControlPoints.Select(p => $"{p.X:F4},{p.Y:F4}");
        return string.Join(";", points);
    }

    /// <summary>
    /// Deserializes a curve from JSON.
    /// </summary>
    public static CurveData FromJson(string json)
    {
        var curve = new CurveData();

        if (string.IsNullOrEmpty(json))
            return CreateLinear();

        try
        {
            var points = json.Split(';');
            foreach (var point in points)
            {
                var coords = point.Split(',');
                if (coords.Length == 2 &&
                    float.TryParse(coords[0], out float x) &&
                    float.TryParse(coords[1], out float y))
                {
                    curve.ControlPoints.Add(new Vector2(x, y));
                }
            }

            if (curve.ControlPoints.Count == 0)
                return CreateLinear();
        }
        catch
        {
            return CreateLinear();
        }

        return curve;
    }
}
