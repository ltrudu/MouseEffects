using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ColorBlindness;

/// <summary>
/// Color blindness simulation effect with RGB curve adjustment.
/// Supports circular, rectangular, and fullscreen application modes.
/// </summary>
public sealed class ColorBlindnessEffect : EffectBase
{
    public const int CurveLutSize = 256;

    private const float DefaultRadius = 300.0f;
    private const float DefaultRectWidth = 400.0f;
    private const float DefaultRectHeight = 300.0f;
    private const int DefaultShapeMode = 0; // Circle
    private const int DefaultFilterType = 1; // Deuteranopia
    private const float DefaultIntensity = 1.0f;
    private const float DefaultColorBoost = 1.0f;
    private const float DefaultEdgeSoftness = 0.2f;
    private const bool DefaultEnableCurves = false;
    private const float DefaultCurveStrength = 1.0f;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "color-blindness",
        Name = "Color Blindness",
        Description = "Simulates color blindness conditions with RGB curve adjustment. Apply to circular, rectangular, or fullscreen areas.",
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
    private ITexture? _curveLut;

    // Effect parameters
    private float _radius = DefaultRadius;
    private float _rectWidth = DefaultRectWidth;
    private float _rectHeight = DefaultRectHeight;
    private int _shapeMode = DefaultShapeMode;
    private int _filterType = DefaultFilterType;
    private float _intensity = DefaultIntensity;
    private float _colorBoost = DefaultColorBoost;
    private float _edgeSoftness = DefaultEdgeSoftness;
    private bool _enableCurves = DefaultEnableCurves;
    private float _curveStrength = DefaultCurveStrength;
    private Vector2 _mousePosition;

    // Curve data - array of control points for R, G, B, and Master curves
    private CurveData _redCurve = CurveData.CreateLinear();
    private CurveData _greenCurve = CurveData.CreateLinear();
    private CurveData _blueCurve = CurveData.CreateLinear();
    private CurveData _masterCurve = CurveData.CreateLinear();
    private bool _curvesNeedUpdate = true;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// This effect requires continuous screen capture to show live desktop content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

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
        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;

        if (Configuration.TryGet("rectWidth", out float rectWidth))
            _rectWidth = rectWidth;

        if (Configuration.TryGet("rectHeight", out float rectHeight))
            _rectHeight = rectHeight;

        if (Configuration.TryGet("shapeMode", out int shapeMode))
            _shapeMode = shapeMode;

        if (Configuration.TryGet("filterType", out int filterType))
            _filterType = filterType;

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
            Radius = _radius,
            RectWidth = _rectWidth,
            RectHeight = _rectHeight,
            ShapeMode = _shapeMode,
            FilterType = _filterType,
            Intensity = _intensity,
            ColorBoost = _colorBoost,
            EdgeSoftness = _edgeSoftness,
            EnableCurves = _enableCurves ? 1.0f : 0.0f,
            CurveStrength = _curveStrength
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
        // This prevents stale content from showing through during window dragging
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

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct ColorBlindnessParams
    {
        // Must match HLSL cbuffer layout exactly!
        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float Radius;               // 4 bytes, offset 16
        public float RectWidth;            // 4 bytes, offset 20
        public float RectHeight;           // 4 bytes, offset 24
        public float ShapeMode;            // 4 bytes, offset 28
        public float FilterType;           // 4 bytes, offset 32
        public float Intensity;            // 4 bytes, offset 36
        public float ColorBoost;           // 4 bytes, offset 40
        public float EdgeSoftness;         // 4 bytes, offset 44
        public float EnableCurves;         // 4 bytes, offset 48
        public float CurveStrength;        // 4 bytes, offset 52
        private float _padding1;           // 4 bytes, offset 56
        private float _padding2;           // 4 bytes, offset 60
        public Vector4 Padding;            // 16 bytes, offset 64
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
