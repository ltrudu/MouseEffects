using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Operating mode for the ColorBlindnessNG effect.
/// </summary>
public enum OperatingMode
{
    Simulation = 0,
    Correction = 1
}

/// <summary>
/// Simulation algorithm type.
/// </summary>
public enum SimulationAlgorithm
{
    Machado = 0,
    Strict = 1
}

/// <summary>
/// LUT application mode for correction.
/// </summary>
public enum ApplicationMode
{
    FullChannel = 0,
    DominantOnly = 1,
    Threshold = 2
}

/// <summary>
/// Gradient interpolation type for LUT generation.
/// </summary>
public enum GradientType
{
    LinearRGB = 0,
    PerceptualLAB = 1,
    HSL = 2
}

/// <summary>
/// Settings for a single channel LUT.
/// </summary>
public class ChannelLUTSettings
{
    public bool Enabled { get; set; }
    public float Strength { get; set; } = 1.0f;
    public Vector3 StartColor { get; set; } = new(1, 0, 0); // RGB 0-1
    public Vector3 EndColor { get; set; } = new(0, 1, 1);   // RGB 0-1
}

/// <summary>
/// Next-generation color blindness simulation and correction effect.
/// Separates scientific simulation from practical LUT-based correction.
/// </summary>
public sealed class ColorBlindnessNGEffect : EffectBase
{
    public const int LutSize = 256;

    private const float DefaultIntensity = 1.0f;
    private const float DefaultThreshold = 0.3f;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "color-blindness-ng",
        Name = "Color Blindness NG",
        Description = "Next-generation CVD simulation and correction with LUT-based color remapping.",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Accessibility
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;
    private ISamplerState? _pointSampler;
    private ITexture? _redLut;
    private ITexture? _greenLut;
    private ITexture? _blueLut;

    // Effect parameters
    private OperatingMode _mode = OperatingMode.Simulation;
    private SimulationAlgorithm _simulationAlgorithm = SimulationAlgorithm.Machado;
    private int _simulationFilterType = 0;
    private ApplicationMode _applicationMode = ApplicationMode.FullChannel;
    private GradientType _gradientType = GradientType.LinearRGB;
    private float _threshold = DefaultThreshold;
    private float _intensity = DefaultIntensity;

    // Channel LUT settings
    private readonly ChannelLUTSettings _redChannel = new() { StartColor = new Vector3(1, 0, 0), EndColor = new Vector3(0, 1, 1) };
    private readonly ChannelLUTSettings _greenChannel = new() { StartColor = new Vector3(0, 1, 0), EndColor = new Vector3(0, 1, 1) };
    private readonly ChannelLUTSettings _blueChannel = new() { StartColor = new Vector3(0, 0, 1), EndColor = new Vector3(1, 1, 0) };

    private bool _lutsNeedUpdate = true;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// This effect requires continuous screen capture to show live desktop content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    /// <summary>
    /// Gets or sets the operating mode.
    /// </summary>
    public OperatingMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    /// <summary>
    /// Gets or sets the simulation algorithm.
    /// </summary>
    public SimulationAlgorithm SimulationAlgorithm
    {
        get => _simulationAlgorithm;
        set => _simulationAlgorithm = value;
    }

    /// <summary>
    /// Gets or sets the simulation filter type.
    /// </summary>
    public int SimulationFilterType
    {
        get => _simulationFilterType;
        set => _simulationFilterType = value;
    }

    /// <summary>
    /// Gets or sets the application mode for correction.
    /// </summary>
    public ApplicationMode ApplicationMode
    {
        get => _applicationMode;
        set => _applicationMode = value;
    }

    /// <summary>
    /// Gets or sets the gradient type for LUT generation.
    /// </summary>
    public GradientType GradientType
    {
        get => _gradientType;
        set
        {
            _gradientType = value;
            _lutsNeedUpdate = true;
        }
    }

    /// <summary>
    /// Gets the red channel LUT settings.
    /// </summary>
    public ChannelLUTSettings RedChannel => _redChannel;

    /// <summary>
    /// Gets the green channel LUT settings.
    /// </summary>
    public ChannelLUTSettings GreenChannel => _greenChannel;

    /// <summary>
    /// Gets the blue channel LUT settings.
    /// </summary>
    public ChannelLUTSettings BlueChannel => _blueChannel;

    /// <summary>
    /// Marks LUTs for regeneration on next render.
    /// </summary>
    public void InvalidateLUTs()
    {
        _lutsNeedUpdate = true;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ColorBlindnessNG.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<ColorBlindnessNGParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create samplers
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
        _pointSampler = context.CreateSamplerState(SamplerDescription.PointClamp);

        // Create initial LUT textures
        CreateLUTs(context);
    }

    private void CreateLUTs(IRenderContext context)
    {
        var texDesc = new TextureDescription
        {
            Width = LutSize,
            Height = 1,
            Format = TextureFormat.R32G32B32A32_Float,
            ShaderResource = true
        };

        _redLut = context.CreateTexture(texDesc, LUTGenerator.GenerateLUT(_redChannel.StartColor, _redChannel.EndColor, _gradientType));
        _greenLut = context.CreateTexture(texDesc, LUTGenerator.GenerateLUT(_greenChannel.StartColor, _greenChannel.EndColor, _gradientType));
        _blueLut = context.CreateTexture(texDesc, LUTGenerator.GenerateLUT(_blueChannel.StartColor, _blueChannel.EndColor, _gradientType));

        _lutsNeedUpdate = false;
    }

    private void UpdateLUTs(IRenderContext context)
    {
        if (!_lutsNeedUpdate) return;

        var texDesc = new TextureDescription
        {
            Width = LutSize,
            Height = 1,
            Format = TextureFormat.R32G32B32A32_Float,
            ShaderResource = true
        };

        _redLut?.Dispose();
        _greenLut?.Dispose();
        _blueLut?.Dispose();

        _redLut = context.CreateTexture(texDesc, LUTGenerator.GenerateLUT(_redChannel.StartColor, _redChannel.EndColor, _gradientType));
        _greenLut = context.CreateTexture(texDesc, LUTGenerator.GenerateLUT(_greenChannel.StartColor, _greenChannel.EndColor, _gradientType));
        _blueLut = context.CreateTexture(texDesc, LUTGenerator.GenerateLUT(_blueChannel.StartColor, _blueChannel.EndColor, _gradientType));

        _lutsNeedUpdate = false;
    }

    protected override void OnConfigurationChanged()
    {
        // Mode
        if (Configuration.TryGet("mode", out int mode))
            _mode = (OperatingMode)mode;

        // Simulation settings
        if (Configuration.TryGet("simulationAlgorithm", out int algorithm))
            _simulationAlgorithm = (SimulationAlgorithm)algorithm;

        if (Configuration.TryGet("simulationFilterType", out int filterType))
            _simulationFilterType = filterType;

        // Correction settings
        if (Configuration.TryGet("applicationMode", out int appMode))
            _applicationMode = (ApplicationMode)appMode;

        if (Configuration.TryGet("gradientType", out int gradType))
        {
            var newGradType = (GradientType)gradType;
            if (newGradType != _gradientType)
            {
                _gradientType = newGradType;
                _lutsNeedUpdate = true;
            }
        }

        if (Configuration.TryGet("threshold", out float threshold))
            _threshold = threshold;

        // Red channel
        if (Configuration.TryGet("redEnabled", out bool redEnabled))
            _redChannel.Enabled = redEnabled;
        if (Configuration.TryGet("redStrength", out float redStrength))
            _redChannel.Strength = redStrength;
        if (Configuration.TryGet("redStartColor", out string? redStart) && redStart != null)
        {
            var newColor = ParseHexColor(redStart);
            if (newColor != _redChannel.StartColor)
            {
                _redChannel.StartColor = newColor;
                _lutsNeedUpdate = true;
            }
        }
        if (Configuration.TryGet("redEndColor", out string? redEnd) && redEnd != null)
        {
            var newColor = ParseHexColor(redEnd);
            if (newColor != _redChannel.EndColor)
            {
                _redChannel.EndColor = newColor;
                _lutsNeedUpdate = true;
            }
        }

        // Green channel
        if (Configuration.TryGet("greenEnabled", out bool greenEnabled))
            _greenChannel.Enabled = greenEnabled;
        if (Configuration.TryGet("greenStrength", out float greenStrength))
            _greenChannel.Strength = greenStrength;
        if (Configuration.TryGet("greenStartColor", out string? greenStart) && greenStart != null)
        {
            var newColor = ParseHexColor(greenStart);
            if (newColor != _greenChannel.StartColor)
            {
                _greenChannel.StartColor = newColor;
                _lutsNeedUpdate = true;
            }
        }
        if (Configuration.TryGet("greenEndColor", out string? greenEnd) && greenEnd != null)
        {
            var newColor = ParseHexColor(greenEnd);
            if (newColor != _greenChannel.EndColor)
            {
                _greenChannel.EndColor = newColor;
                _lutsNeedUpdate = true;
            }
        }

        // Blue channel
        if (Configuration.TryGet("blueEnabled", out bool blueEnabled))
            _blueChannel.Enabled = blueEnabled;
        if (Configuration.TryGet("blueStrength", out float blueStrength))
            _blueChannel.Strength = blueStrength;
        if (Configuration.TryGet("blueStartColor", out string? blueStart) && blueStart != null)
        {
            var newColor = ParseHexColor(blueStart);
            if (newColor != _blueChannel.StartColor)
            {
                _blueChannel.StartColor = newColor;
                _lutsNeedUpdate = true;
            }
        }
        if (Configuration.TryGet("blueEndColor", out string? blueEnd) && blueEnd != null)
        {
            var newColor = ParseHexColor(blueEnd);
            if (newColor != _blueChannel.EndColor)
            {
                _blueChannel.EndColor = newColor;
                _lutsNeedUpdate = true;
            }
        }

        // Global
        if (Configuration.TryGet("intensity", out float intensity))
            _intensity = intensity;
    }

    private static Vector3 ParseHexColor(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex[1..];

        if (hex.Length != 6)
            return new Vector3(1, 1, 1);

        try
        {
            int r = Convert.ToInt32(hex[..2], 16);
            int g = Convert.ToInt32(hex[2..4], 16);
            int b = Convert.ToInt32(hex[4..6], 16);
            return new Vector3(r / 255f, g / 255f, b / 255f);
        }
        catch
        {
            return new Vector3(1, 1, 1);
        }
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        // No per-frame updates needed
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Get the screen texture from context
        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Update LUTs if needed
        UpdateLUTs(context);

        // Calculate effective filter type based on algorithm
        int effectiveFilterType = _simulationFilterType;
        if (_simulationAlgorithm == SimulationAlgorithm.Strict && _simulationFilterType > 0 && _simulationFilterType <= 6)
        {
            effectiveFilterType = _simulationFilterType + 6; // Offset to strict filters (7-12)
        }

        // Update parameters
        var cbParams = new ColorBlindnessNGParams
        {
            ViewportSize = context.ViewportSize,
            Mode = (float)_mode,
            SimulationFilterType = effectiveFilterType,
            ApplicationMode = (float)_applicationMode,
            Threshold = _threshold,
            Intensity = _intensity,
            RedEnabled = _redChannel.Enabled ? 1.0f : 0.0f,
            RedStrength = _redChannel.Strength,
            GreenEnabled = _greenChannel.Enabled ? 1.0f : 0.0f,
            GreenStrength = _greenChannel.Strength,
            BlueEnabled = _blueChannel.Enabled ? 1.0f : 0.0f,
            BlueStrength = _blueChannel.Strength,
            Padding = 0
        };

        context.UpdateBuffer(_paramsBuffer!, cbParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetShaderResource(ShaderStage.Pixel, 1, _redLut!);
        context.SetShaderResource(ShaderStage.Pixel, 2, _greenLut!);
        context.SetShaderResource(ShaderStage.Pixel, 3, _blueLut!);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);
        context.SetSampler(ShaderStage.Pixel, 1, _pointSampler!);

        // Use opaque blend
        context.SetBlendState(BlendMode.Opaque);

        // Draw fullscreen quad
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind resources
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
        context.SetShaderResource(ShaderStage.Pixel, 1, (ITexture?)null);
        context.SetShaderResource(ShaderStage.Pixel, 2, (ITexture?)null);
        context.SetShaderResource(ShaderStage.Pixel, 3, (ITexture?)null);
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
        _redLut?.Dispose();
        _greenLut?.Dispose();
        _blueLut?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ColorBlindnessNGEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.ColorBlindnessNG.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct ColorBlindnessNGParams
    {
        public Vector2 ViewportSize;       // 8 bytes
        public float Mode;                 // 4 bytes - 0=Simulation, 1=Correction
        public float SimulationFilterType; // 4 bytes
        public float ApplicationMode;      // 4 bytes - 0=Full, 1=Dominant, 2=Threshold
        public float Threshold;            // 4 bytes
        public float Intensity;            // 4 bytes
        public float RedEnabled;           // 4 bytes
        public float RedStrength;          // 4 bytes
        public float GreenEnabled;         // 4 bytes
        public float GreenStrength;        // 4 bytes
        public float BlueEnabled;          // 4 bytes
        public float BlueStrength;         // 4 bytes
        public float Padding;              // 4 bytes (pad to 64 bytes)
    }

    #endregion
}
