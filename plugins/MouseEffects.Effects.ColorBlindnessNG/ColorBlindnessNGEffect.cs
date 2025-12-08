using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ColorBlindnessNG;

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
/// Blend mode for LUT correction - determines how LUT colors are blended with original.
/// </summary>
public enum LutBlendMode
{
    /// <summary>
    /// Original formula: blend amount depends on channel intensity.
    /// Best for pure/bright colors, weak for dark colors.
    /// Formula: lerp(result, lerp(result, lut, channel), strength)
    /// </summary>
    ChannelWeighted = 0,

    /// <summary>
    /// Direct replacement controlled only by strength.
    /// Works equally for all color intensities.
    /// Formula: lerp(result, lut, strength)
    /// </summary>
    Direct = 1,

    /// <summary>
    /// Blend based on channel's relative dominance (channel/max).
    /// Good for mixed colors where channel isn't dominant.
    /// Formula: lerp(result, lut, (channel/maxChannel) * strength)
    /// </summary>
    Proportional = 2,

    /// <summary>
    /// Adds the color shift from start to LUT color.
    /// Preserves luminosity better.
    /// Formula: result + (lut - startColor) * channel * strength
    /// </summary>
    Additive = 3,

    /// <summary>
    /// Screen blend mode - brightens colors.
    /// Formula: 1 - (1-result) * (1 - lut * channel * strength)
    /// </summary>
    Screen = 4
}

/// <summary>
/// Split screen mode for comparing original vs corrected.
/// </summary>
public enum SplitMode
{
    Fullscreen = 0,
    SplitVertical = 1,
    SplitHorizontal = 2,
    Quadrants = 3,
    Circle = 4,
    Rectangle = 5
}

/// <summary>
/// Settings for a single channel LUT.
/// </summary>
public class ChannelLUTSettings
{
    public bool Enabled { get; set; }
    public float Strength { get; set; } = 1.0f;
    public Vector3 StartColor { get; set; } = new(1, 0, 0);
    public Vector3 EndColor { get; set; } = new(0, 1, 1);
    public float WhiteProtection { get; set; } = 0.01f;
    /// <summary>
    /// Dominance threshold: minimum percentage of total color (R+G+B) this channel must have
    /// to apply LUT correction. 0.0 = disabled, 0.33 = equal distribution, 0.5+ = dominant.
    /// Use to exclude colors like yellow (R+G) when only correcting pure reds.
    /// </summary>
    public float DominanceThreshold { get; set; } = 0.0f;
    /// <summary>
    /// Blend mode for this channel's LUT correction.
    /// Determines how LUT colors are blended with the original pixel.
    /// </summary>
    public LutBlendMode BlendMode { get; set; } = LutBlendMode.ChannelWeighted;
}

/// <summary>
/// Next-generation color blindness simulation and correction effect.
/// Supports per-zone configuration for split-screen modes.
/// </summary>
public sealed class ColorBlindnessNGEffect : EffectBase
{
    public const int LutSize = 256;
    public const int MaxZones = 4;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "color-blindness-ng",
        Name = "Color Blindness NG",
        Description = "Next-generation CVD simulation and correction with per-zone LUT-based color remapping.",
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

    // Per-zone LUT textures (up to 4 zones × 3 channels = 12 textures)
    private readonly ITexture?[,] _zoneLuts = new ITexture?[MaxZones, 3]; // [zone, channel]

    // Split screen parameters
    private SplitMode _splitMode = SplitMode.Fullscreen;
    private float _splitPosition = 0.5f;
    private float _splitPositionV = 0.5f;
    private bool _comparisonMode = false;

    // Shape mode parameters (Circle and Rectangle)
    private float _radius = 200f;
    private float _rectWidth = 300f;
    private float _rectHeight = 200f;
    private float _edgeSoftness = 0.2f;

    // Mouse position for virtual cursor in comparison mode
    private Vector2 _mousePosition;

    // Per-zone settings (4 zones max)
    private readonly ZoneSettings[] _zones = new ZoneSettings[MaxZones];

    public override EffectMetadata Metadata => _metadata;

    public override bool RequiresContinuousScreenCapture => true;

    /// <summary>
    /// Gets or sets the split screen mode.
    /// </summary>
    public SplitMode SplitMode
    {
        get => _splitMode;
        set => _splitMode = value;
    }

    /// <summary>
    /// Gets or sets the horizontal split position (0-1).
    /// </summary>
    public float SplitPosition
    {
        get => _splitPosition;
        set => _splitPosition = Math.Clamp(value, 0.1f, 0.9f);
    }

    /// <summary>
    /// Gets or sets the vertical split position (0-1).
    /// </summary>
    public float SplitPositionV
    {
        get => _splitPositionV;
        set => _splitPositionV = Math.Clamp(value, 0.1f, 0.9f);
    }

    /// <summary>
    /// Gets or sets comparison mode. When enabled, zone 0 shows original.
    /// </summary>
    public bool ComparisonMode
    {
        get => _comparisonMode;
        set => _comparisonMode = value;
    }

    /// <summary>
    /// Gets or sets the radius for Circle mode (in pixels).
    /// </summary>
    public float Radius
    {
        get => _radius;
        set => _radius = Math.Clamp(value, 10f, 1000f);
    }

    /// <summary>
    /// Gets or sets the width for Rectangle mode (in pixels).
    /// </summary>
    public float RectWidth
    {
        get => _rectWidth;
        set => _rectWidth = Math.Clamp(value, 10f, 2000f);
    }

    /// <summary>
    /// Gets or sets the height for Rectangle mode (in pixels).
    /// </summary>
    public float RectHeight
    {
        get => _rectHeight;
        set => _rectHeight = Math.Clamp(value, 10f, 2000f);
    }

    /// <summary>
    /// Gets or sets the edge softness for shape modes (0 = hard, 1 = maximum soft).
    /// </summary>
    public float EdgeSoftness
    {
        get => _edgeSoftness;
        set => _edgeSoftness = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets the settings for a specific zone (0-3).
    /// </summary>
    public ZoneSettings GetZone(int index) => _zones[Math.Clamp(index, 0, MaxZones - 1)];

    /// <summary>
    /// Gets the number of active zones based on split mode.
    /// </summary>
    public int ActiveZoneCount => _splitMode switch
    {
        SplitMode.Fullscreen => 1,
        SplitMode.SplitVertical => 2,
        SplitMode.SplitHorizontal => 2,
        SplitMode.Quadrants => 4,
        SplitMode.Circle => 2,
        SplitMode.Rectangle => 2,
        _ => 1
    };

    /// <summary>
    /// Gets whether the current mode is a shape mode (Circle or Rectangle).
    /// </summary>
    public bool IsShapeMode => _splitMode == SplitMode.Circle || _splitMode == SplitMode.Rectangle;

    public ColorBlindnessNGEffect()
    {
        // Initialize all zones with default settings
        for (int i = 0; i < MaxZones; i++)
        {
            _zones[i] = new ZoneSettings();
        }

        // Zone 0 defaults to simulation with Deuteranopia
        _zones[0].Mode = ZoneMode.Simulation;
        _zones[0].SimulationFilterType = 3; // Deuteranopia

        // Zone 1 defaults to correction
        _zones[1].Mode = ZoneMode.Correction;
        _zones[1].RedChannel.Enabled = true;

        // Zones 2-3 default to original
        _zones[2].Mode = ZoneMode.Original;
        _zones[3].Mode = ZoneMode.Original;
    }

    /// <summary>
    /// Marks LUTs for a specific zone for regeneration.
    /// </summary>
    public void InvalidateZoneLUTs(int zoneIndex)
    {
        if (zoneIndex >= 0 && zoneIndex < MaxZones)
            _zones[zoneIndex].LutsNeedUpdate = true;
    }

    /// <summary>
    /// Marks all zone LUTs for regeneration.
    /// </summary>
    public void InvalidateAllLUTs()
    {
        for (int i = 0; i < MaxZones; i++)
            _zones[i].LutsNeedUpdate = true;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        var shaderSource = LoadEmbeddedShader("ColorBlindnessNG.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<ColorBlindnessNGParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
        _pointSampler = context.CreateSamplerState(SamplerDescription.PointClamp);

        // Create initial LUTs for all zones
        CreateAllLUTs(context);
    }

    private void CreateAllLUTs(IRenderContext context)
    {
        var texDesc = new TextureDescription
        {
            Width = LutSize,
            Height = 1,
            Format = TextureFormat.R32G32B32A32_Float,
            ShaderResource = true
        };

        for (int zone = 0; zone < MaxZones; zone++)
        {
            var z = _zones[zone];
            _zoneLuts[zone, 0] = context.CreateTexture(texDesc,
                LUTGenerator.GenerateLUT(z.RedChannel.StartColor, z.RedChannel.EndColor, z.GradientType));
            _zoneLuts[zone, 1] = context.CreateTexture(texDesc,
                LUTGenerator.GenerateLUT(z.GreenChannel.StartColor, z.GreenChannel.EndColor, z.GradientType));
            _zoneLuts[zone, 2] = context.CreateTexture(texDesc,
                LUTGenerator.GenerateLUT(z.BlueChannel.StartColor, z.BlueChannel.EndColor, z.GradientType));
            z.LutsNeedUpdate = false;
        }
    }

    private void UpdateZoneLUTs(IRenderContext context, int zoneIndex)
    {
        var z = _zones[zoneIndex];
        if (!z.LutsNeedUpdate) return;

        var texDesc = new TextureDescription
        {
            Width = LutSize,
            Height = 1,
            Format = TextureFormat.R32G32B32A32_Float,
            ShaderResource = true
        };

        // Dispose old textures
        _zoneLuts[zoneIndex, 0]?.Dispose();
        _zoneLuts[zoneIndex, 1]?.Dispose();
        _zoneLuts[zoneIndex, 2]?.Dispose();

        // Create new textures
        _zoneLuts[zoneIndex, 0] = context.CreateTexture(texDesc,
            LUTGenerator.GenerateLUT(z.RedChannel.StartColor, z.RedChannel.EndColor, z.GradientType));
        _zoneLuts[zoneIndex, 1] = context.CreateTexture(texDesc,
            LUTGenerator.GenerateLUT(z.GreenChannel.StartColor, z.GreenChannel.EndColor, z.GradientType));
        _zoneLuts[zoneIndex, 2] = context.CreateTexture(texDesc,
            LUTGenerator.GenerateLUT(z.BlueChannel.StartColor, z.BlueChannel.EndColor, z.GradientType));

        z.LutsNeedUpdate = false;
    }

    protected override void OnConfigurationChanged()
    {
        // Split screen settings
        if (Configuration.TryGet("splitMode", out int splitMode))
            _splitMode = (SplitMode)splitMode;
        if (Configuration.TryGet("splitPosition", out float splitPos))
            _splitPosition = splitPos;
        if (Configuration.TryGet("splitPositionV", out float splitPosV))
            _splitPositionV = splitPosV;
        if (Configuration.TryGet("comparisonMode", out bool compMode))
            _comparisonMode = compMode;

        // Shape mode settings
        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;
        if (Configuration.TryGet("rectWidth", out float rectWidth))
            _rectWidth = rectWidth;
        if (Configuration.TryGet("rectHeight", out float rectHeight))
            _rectHeight = rectHeight;
        if (Configuration.TryGet("edgeSoftness", out float edgeSoftness))
            _edgeSoftness = edgeSoftness;

        // Load per-zone settings
        for (int i = 0; i < MaxZones; i++)
        {
            LoadZoneConfiguration(i);
        }
    }

    private void LoadZoneConfiguration(int zoneIndex)
    {
        var z = _zones[zoneIndex];
        var prefix = $"zone{zoneIndex}_";

        if (Configuration.TryGet($"{prefix}mode", out int mode))
            z.Mode = (ZoneMode)mode;

        // Simulation settings
        if (Configuration.TryGet($"{prefix}simAlgorithm", out int algorithm))
            z.SimulationAlgorithm = (SimulationAlgorithm)algorithm;
        if (Configuration.TryGet($"{prefix}simFilterType", out int filterType))
            z.SimulationFilterType = filterType;

        // Correction settings
        if (Configuration.TryGet($"{prefix}correctionAlgorithm", out int corrAlgo))
            z.CorrectionAlgorithm = (CorrectionAlgorithm)corrAlgo;
        if (Configuration.TryGet($"{prefix}daltonizationCVDType", out int daltonCVDType))
            z.DaltonizationCVDType = daltonCVDType;
        if (Configuration.TryGet($"{prefix}daltonizationStrength", out float daltonStrength))
            z.DaltonizationStrength = daltonStrength;
        if (Configuration.TryGet($"{prefix}appMode", out int appMode))
            z.ApplicationMode = (ApplicationMode)appMode;
        if (Configuration.TryGet($"{prefix}gradientType", out int gradType))
        {
            var newGradType = (GradientType)gradType;
            if (newGradType != z.GradientType)
            {
                z.GradientType = newGradType;
                z.LutsNeedUpdate = true;
            }
        }
        if (Configuration.TryGet($"{prefix}threshold", out float threshold))
            z.Threshold = threshold;
        if (Configuration.TryGet($"{prefix}intensity", out float intensity))
            z.Intensity = intensity;

        // Simulation-guided correction settings
        if (Configuration.TryGet($"{prefix}simGuidedEnabled", out bool simGuidedEnabled))
            z.SimulationGuidedEnabled = simGuidedEnabled;
        if (Configuration.TryGet($"{prefix}simGuidedAlgorithm", out int simGuidedAlgorithm))
            z.SimulationGuidedAlgorithm = (SimulationAlgorithm)simGuidedAlgorithm;
        if (Configuration.TryGet($"{prefix}simGuidedFilterType", out int simGuidedFilterType))
            z.SimulationGuidedFilterType = simGuidedFilterType;
        if (Configuration.TryGet($"{prefix}simGuidedSensitivity", out float simGuidedSensitivity))
            z.SimulationGuidedSensitivity = simGuidedSensitivity;

        // Post-correction simulation settings
        if (Configuration.TryGet($"{prefix}postSimEnabled", out bool postSimEnabled))
            z.PostCorrectionSimEnabled = postSimEnabled;
        if (Configuration.TryGet($"{prefix}postSimAlgorithm", out int postSimAlgorithm))
            z.PostCorrectionSimAlgorithm = (SimulationAlgorithm)postSimAlgorithm;
        if (Configuration.TryGet($"{prefix}postSimFilterType", out int postSimFilterType))
            z.PostCorrectionSimFilterType = postSimFilterType;
        if (Configuration.TryGet($"{prefix}postSimIntensity", out float postSimIntensity))
            z.PostCorrectionSimIntensity = postSimIntensity;

        // Hue Rotation settings
        if (Configuration.TryGet($"{prefix}hueRotCVDType", out int hueRotCVDType))
            z.HueRotationCVDType = (CVDCorrectionType)hueRotCVDType;
        if (Configuration.TryGet($"{prefix}hueRotStrength", out float hueRotStrength))
            z.HueRotationStrength = hueRotStrength;
        if (Configuration.TryGet($"{prefix}hueRotAdvanced", out bool hueRotAdvanced))
            z.HueRotationAdvancedMode = hueRotAdvanced;
        if (Configuration.TryGet($"{prefix}hueRotSourceStart", out float hueRotSourceStart))
            z.HueRotationSourceStart = hueRotSourceStart;
        if (Configuration.TryGet($"{prefix}hueRotSourceEnd", out float hueRotSourceEnd))
            z.HueRotationSourceEnd = hueRotSourceEnd;
        if (Configuration.TryGet($"{prefix}hueRotShift", out float hueRotShift))
            z.HueRotationShift = hueRotShift;
        if (Configuration.TryGet($"{prefix}hueRotFalloff", out float hueRotFalloff))
            z.HueRotationFalloff = hueRotFalloff;

        // CIELAB Remapping settings
        if (Configuration.TryGet($"{prefix}cielabCVDType", out int cielabCVDType))
            z.CIELABCVDType = (CVDCorrectionType)cielabCVDType;
        if (Configuration.TryGet($"{prefix}cielabStrength", out float cielabStrength))
            z.CIELABStrength = cielabStrength;
        if (Configuration.TryGet($"{prefix}cielabAdvanced", out bool cielabAdvanced))
            z.CIELABAdvancedMode = cielabAdvanced;
        if (Configuration.TryGet($"{prefix}cielabAtoB", out float cielabAtoB))
            z.CIELABAtoB = cielabAtoB;
        if (Configuration.TryGet($"{prefix}cielabBtoA", out float cielabBtoA))
            z.CIELABBtoA = cielabBtoA;
        if (Configuration.TryGet($"{prefix}cielabAEnhance", out float cielabAEnhance))
            z.CIELABAEnhance = cielabAEnhance;
        if (Configuration.TryGet($"{prefix}cielabBEnhance", out float cielabBEnhance))
            z.CIELABBEnhance = cielabBEnhance;
        if (Configuration.TryGet($"{prefix}cielabLEnhance", out float cielabLEnhance))
            z.CIELABLEnhance = cielabLEnhance;

        // Red channel
        bool needsUpdate = z.LutsNeedUpdate;
        LoadChannelConfiguration(z.RedChannel, $"{prefix}red", ref needsUpdate);

        // Green channel
        LoadChannelConfiguration(z.GreenChannel, $"{prefix}green", ref needsUpdate);

        // Blue channel
        LoadChannelConfiguration(z.BlueChannel, $"{prefix}blue", ref needsUpdate);
        z.LutsNeedUpdate = needsUpdate;
    }

    private void LoadChannelConfiguration(ChannelLUTSettings channel, string prefix, ref bool needsUpdate)
    {
        if (Configuration.TryGet($"{prefix}Enabled", out bool enabled))
            channel.Enabled = enabled;
        if (Configuration.TryGet($"{prefix}Strength", out float strength))
            channel.Strength = strength;
        if (Configuration.TryGet($"{prefix}WhiteProtection", out float whiteProt))
            channel.WhiteProtection = whiteProt;
        if (Configuration.TryGet($"{prefix}DominanceThreshold", out float dominanceThreshold))
            channel.DominanceThreshold = dominanceThreshold;
        if (Configuration.TryGet($"{prefix}BlendMode", out int blendMode))
            channel.BlendMode = (LutBlendMode)blendMode;
        if (Configuration.TryGet($"{prefix}StartColor", out string? startColor) && startColor != null)
        {
            var newColor = ParseHexColor(startColor);
            if (!ColorsEqual(channel.StartColor, newColor))
            {
                channel.StartColor = newColor;
                needsUpdate = true;
            }
        }
        if (Configuration.TryGet($"{prefix}EndColor", out string? endColor) && endColor != null)
        {
            var newColor = ParseHexColor(endColor);
            if (!ColorsEqual(channel.EndColor, newColor))
            {
                channel.EndColor = newColor;
                needsUpdate = true;
            }
        }
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

    private static bool ColorsEqual(Vector3 a, Vector3 b)
    {
        const float tolerance = 0.001f;
        return Math.Abs(a.X - b.X) < tolerance &&
               Math.Abs(a.Y - b.Y) < tolerance &&
               Math.Abs(a.Z - b.Z) < tolerance;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _mousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Update LUTs for all zones if needed
        for (int i = 0; i < MaxZones; i++)
        {
            UpdateZoneLUTs(context, i);
        }

        // Build constant buffer with all zone parameters
        var cbParams = BuildConstantBuffer(context.ViewportSize);
        context.UpdateBuffer(_paramsBuffer!, cbParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);

        // Set LUT textures for all zones (slots 1-12)
        // Zone 0: slots 1, 2, 3 (R, G, B)
        // Zone 1: slots 4, 5, 6
        // Zone 2: slots 7, 8, 9
        // Zone 3: slots 10, 11, 12
        for (int zone = 0; zone < MaxZones; zone++)
        {
            int baseSlot = 1 + zone * 3;
            context.SetShaderResource(ShaderStage.Pixel, baseSlot, _zoneLuts[zone, 0]!);
            context.SetShaderResource(ShaderStage.Pixel, baseSlot + 1, _zoneLuts[zone, 1]!);
            context.SetShaderResource(ShaderStage.Pixel, baseSlot + 2, _zoneLuts[zone, 2]!);
        }

        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);
        context.SetSampler(ShaderStage.Pixel, 1, _pointSampler!);

        context.SetBlendState(BlendMode.Opaque);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind resources
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
        for (int slot = 1; slot <= 12; slot++)
        {
            context.SetShaderResource(ShaderStage.Pixel, slot, (ITexture?)null);
        }
    }

    private ColorBlindnessNGParams BuildConstantBuffer(Vector2 viewportSize)
    {
        var cbParams = new ColorBlindnessNGParams
        {
            MousePosition = _mousePosition,
            ViewportSize = viewportSize,
            SplitModeValue = (float)_splitMode,
            SplitPosition = _splitPosition,
            SplitPositionV = _splitPositionV,
            ComparisonMode = _comparisonMode ? 1.0f : 0.0f,
            Radius = _radius,
            RectWidth = _rectWidth,
            RectHeight = _rectHeight,
            EdgeSoftness = _edgeSoftness
        };

        // Fill zone parameters
        for (int i = 0; i < MaxZones; i++)
        {
            var z = _zones[i];

            // Calculate effective filter type for simulation
            int effectiveFilterType = z.SimulationFilterType;
            if (z.SimulationAlgorithm == SimulationAlgorithm.Strict &&
                z.SimulationFilterType > 0 && z.SimulationFilterType <= 6)
            {
                effectiveFilterType = z.SimulationFilterType + 6;
            }

            // Calculate effective filter type for simulation-guided correction
            int effectiveSimGuidedFilterType = z.SimulationGuidedFilterType;
            if (z.SimulationGuidedAlgorithm == SimulationAlgorithm.Strict &&
                z.SimulationGuidedFilterType > 0 && z.SimulationGuidedFilterType <= 6)
            {
                effectiveSimGuidedFilterType = z.SimulationGuidedFilterType + 6;
            }

            // Calculate effective filter type for post-correction simulation
            int effectivePostSimFilterType = z.PostCorrectionSimFilterType;
            if (z.PostCorrectionSimAlgorithm == SimulationAlgorithm.Strict &&
                z.PostCorrectionSimFilterType > 0 && z.PostCorrectionSimFilterType <= 6)
            {
                effectivePostSimFilterType = z.PostCorrectionSimFilterType + 6;
            }

            // Calculate effective filter type for Daltonization
            int effectiveDaltonizationCVDType = z.DaltonizationCVDType;
            // Note: DaltonizationCVDType follows same encoding as simulation filter types

            // Get effective Hue Rotation parameters (auto-configure from CVD type if not in advanced mode)
            float hueSourceStart = z.HueRotationSourceStart;
            float hueSourceEnd = z.HueRotationSourceEnd;
            float hueShift = z.HueRotationShift;
            float hueFalloff = z.HueRotationFalloff;

            if (!z.HueRotationAdvancedMode)
            {
                // Auto-configure based on CVD type
                // Different hue ranges and shifts for each type based on confusion lines
                switch (z.HueRotationCVDType)
                {
                    case CVDCorrectionType.Protanopia:
                        // Red-blind (severe): Red appears darker, focus on red-green confusion
                        // Rotate reds toward orange/yellow for better distinction
                        hueSourceStart = 340f; hueSourceEnd = 60f; hueShift = 45f; hueFalloff = 0.35f;
                        break;
                    case CVDCorrectionType.Protanomaly:
                        // Red-weak (mild): Smaller rotation needed
                        hueSourceStart = 350f; hueSourceEnd = 40f; hueShift = 25f; hueFalloff = 0.3f;
                        break;
                    case CVDCorrectionType.Deuteranopia:
                        // Green-blind (severe): Green confusion with red/brown
                        // Rotate greens toward cyan/blue for distinction
                        hueSourceStart = 60f; hueSourceEnd = 160f; hueShift = 50f; hueFalloff = 0.35f;
                        break;
                    case CVDCorrectionType.Deuteranomaly:
                        // Green-weak (mild): Smaller rotation
                        hueSourceStart = 80f; hueSourceEnd = 140f; hueShift = 30f; hueFalloff = 0.3f;
                        break;
                    case CVDCorrectionType.Tritanopia:
                        // Blue-blind (severe): Blue-yellow confusion
                        // Rotate blues toward magenta, yellows toward red
                        hueSourceStart = 180f; hueSourceEnd = 280f; hueShift = -55f; hueFalloff = 0.35f;
                        break;
                    case CVDCorrectionType.Tritanomaly:
                        // Blue-weak (mild): Smaller rotation
                        hueSourceStart = 200f; hueSourceEnd = 260f; hueShift = -35f; hueFalloff = 0.3f;
                        break;
                }
            }

            // Get effective CIELAB parameters (auto-configure from CVD type if not in advanced mode)
            float labAtoB = z.CIELABAtoB;
            float labBtoA = z.CIELABBtoA;
            float labAEnhance = z.CIELABAEnhance;
            float labBEnhance = z.CIELABBEnhance;
            float labLEnhance = z.CIELABLEnhance;

            if (!z.CIELABAdvancedMode)
            {
                // Auto-configure based on CVD type
                // Severity affects transfer amount: -opia (severe) = stronger, -anomaly (mild) = weaker
                switch (z.CIELABCVDType)
                {
                    case CVDCorrectionType.Protanopia:
                        // Red-blind (severe): Strong transfer a*→b*, slight red enhancement via L*
                        labAtoB = 0.6f; labBtoA = 0f; labAEnhance = 0.9f; labBEnhance = 1.3f; labLEnhance = 0.15f;
                        break;
                    case CVDCorrectionType.Protanomaly:
                        // Red-weak (mild): Moderate transfer, preserve more original color
                        labAtoB = 0.35f; labBtoA = 0f; labAEnhance = 1f; labBEnhance = 1.15f; labLEnhance = 0.08f;
                        break;
                    case CVDCorrectionType.Deuteranopia:
                        // Green-blind (severe): Strong transfer a*→b*
                        labAtoB = 0.55f; labBtoA = 0f; labAEnhance = 1f; labBEnhance = 1.25f; labLEnhance = 0.12f;
                        break;
                    case CVDCorrectionType.Deuteranomaly:
                        // Green-weak (mild): Moderate transfer
                        labAtoB = 0.3f; labBtoA = 0f; labAEnhance = 1f; labBEnhance = 1.1f; labLEnhance = 0.05f;
                        break;
                    case CVDCorrectionType.Tritanopia:
                        // Blue-blind (severe): Strong transfer b*→a*
                        labAtoB = 0f; labBtoA = 0.55f; labAEnhance = 1.25f; labBEnhance = 0.9f; labLEnhance = 0.12f;
                        break;
                    case CVDCorrectionType.Tritanomaly:
                        // Blue-weak (mild): Moderate transfer b*→a*
                        labAtoB = 0f; labBtoA = 0.3f; labAEnhance = 1.1f; labBEnhance = 1f; labLEnhance = 0.05f;
                        break;
                }
            }

            var zoneParams = new ZoneParams
            {
                Mode = (float)z.Mode,
                SimulationFilterType = effectiveFilterType,
                ApplicationMode = (float)z.ApplicationMode,
                Threshold = z.Threshold,
                Intensity = z.Intensity,
                RedEnabled = z.RedChannel.Enabled ? 1.0f : 0.0f,
                RedStrength = z.RedChannel.Strength,
                RedWhiteProtection = z.RedChannel.WhiteProtection,
                GreenEnabled = z.GreenChannel.Enabled ? 1.0f : 0.0f,
                GreenStrength = z.GreenChannel.Strength,
                GreenWhiteProtection = z.GreenChannel.WhiteProtection,
                BlueEnabled = z.BlueChannel.Enabled ? 1.0f : 0.0f,
                BlueStrength = z.BlueChannel.Strength,
                BlueWhiteProtection = z.BlueChannel.WhiteProtection,
                SimulationGuidedEnabled = z.SimulationGuidedEnabled ? 1.0f : 0.0f,
                SimulationGuidedFilterType = effectiveSimGuidedFilterType,
                SimulationGuidedSensitivity = z.SimulationGuidedSensitivity,
                PostCorrectionSimEnabled = z.PostCorrectionSimEnabled ? 1.0f : 0.0f,
                PostCorrectionSimFilterType = effectivePostSimFilterType,
                PostCorrectionSimIntensity = z.PostCorrectionSimIntensity,
                RedDominanceThreshold = z.RedChannel.DominanceThreshold,
                GreenDominanceThreshold = z.GreenChannel.DominanceThreshold,
                BlueDominanceThreshold = z.BlueChannel.DominanceThreshold,
                RedBlendMode = (float)z.RedChannel.BlendMode,
                GreenBlendMode = (float)z.GreenChannel.BlendMode,
                BlueBlendMode = (float)z.BlueChannel.BlendMode,
                CorrectionAlgorithm = (float)z.CorrectionAlgorithm,
                DaltonizationCVDType = effectiveDaltonizationCVDType,
                DaltonizationStrength = z.DaltonizationStrength,
                // Hue Rotation parameters
                HueRotationStrength = z.HueRotationStrength,
                HueRotationSourceStart = hueSourceStart,
                HueRotationSourceEnd = hueSourceEnd,
                HueRotationShift = hueShift,
                HueRotationFalloff = hueFalloff,
                // CIELAB Remapping parameters
                CIELABStrength = z.CIELABStrength,
                CIELABAtoB = labAtoB,
                CIELABBtoA = labBtoA,
                CIELABAEnhance = labAEnhance,
                CIELABBEnhance = labBEnhance,
                CIELABLEnhance = labLEnhance
            };

            switch (i)
            {
                case 0: cbParams.Zone0 = zoneParams; break;
                case 1: cbParams.Zone1 = zoneParams; break;
                case 2: cbParams.Zone2 = zoneParams; break;
                case 3: cbParams.Zone3 = zoneParams; break;
            }
        }

        return cbParams;
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

        // Dispose all zone LUTs
        for (int zone = 0; zone < MaxZones; zone++)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                _zoneLuts[zone, channel]?.Dispose();
            }
        }
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

    /// <summary>
    /// Per-zone parameters packed for shader.
    /// Size: 160 bytes (40 floats)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct ZoneParams
    {
        public float Mode;                 // 0=Original, 1=Simulation, 2=Correction
        public float SimulationFilterType; // Filter type for simulation
        public float ApplicationMode;      // 0=Full, 1=Dominant, 2=Threshold
        public float Threshold;            // Threshold for threshold mode

        public float Intensity;            // Global intensity for zone
        public float RedEnabled;           // Red channel enabled
        public float RedStrength;          // Red channel strength
        public float RedWhiteProtection;   // Red white protection

        public float GreenEnabled;         // Green channel enabled
        public float GreenStrength;        // Green channel strength
        public float GreenWhiteProtection; // Green white protection
        public float BlueEnabled;          // Blue channel enabled

        public float BlueStrength;         // Blue channel strength
        public float BlueWhiteProtection;  // Blue white protection
        public float SimulationGuidedEnabled;    // 1.0 = use simulation to detect affected pixels
        public float SimulationGuidedFilterType; // CVD type for detection

        public float SimulationGuidedSensitivity; // Sensitivity multiplier for detection
        public float PostCorrectionSimEnabled;    // 1.0 = apply CVD simulation AFTER correction
        public float PostCorrectionSimFilterType; // CVD type for post-correction simulation
        public float PostCorrectionSimIntensity;  // Intensity of post-correction simulation

        public float RedDominanceThreshold;   // Min % of R/(R+G+B) to apply red LUT
        public float GreenDominanceThreshold; // Min % of G/(R+G+B) to apply green LUT
        public float BlueDominanceThreshold;  // Min % of B/(R+G+B) to apply blue LUT
        public float RedBlendMode;            // Per-channel blend mode for red

        public float GreenBlendMode;          // Per-channel blend mode for green
        public float BlueBlendMode;           // Per-channel blend mode for blue
        public float CorrectionAlgorithm;     // 0=LUT-based, 1=Daltonization, 2=HueRotation, 3=CIELAB
        public float DaltonizationCVDType;    // CVD type for Daltonization (1-6=Machado, 7-12=Strict)

        public float DaltonizationStrength;   // Strength of Daltonization correction (0-1)
        public float HueRotationStrength;     // Hue rotation overall strength (0-1)
        public float HueRotationSourceStart;  // Start of source hue range (0-360 degrees)
        public float HueRotationSourceEnd;    // End of source hue range (0-360 degrees)

        public float HueRotationShift;        // Amount to shift hue (-180 to +180 degrees)
        public float HueRotationFalloff;      // Softness of hue range boundaries (0-1)
        public float CIELABStrength;          // CIELAB remapping overall strength (0-1)
        public float CIELABAtoB;              // Transfer factor from a* to b* axis (-1 to 1)

        public float CIELABBtoA;              // Transfer factor from b* to a* axis (-1 to 1)
        public float CIELABAEnhance;          // Enhancement multiplier for a* axis (0-2)
        public float CIELABBEnhance;          // Enhancement multiplier for b* axis (0-2)
        public float CIELABLEnhance;          // Encode color diff into lightness (0-1)
    }

    /// <summary>
    /// Full constant buffer for shader.
    /// Size: 48 (global) + 4 * 160 (zones) = 688 bytes
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 688)]
    private struct ColorBlindnessNGParams
    {
        // Global parameters (48 bytes)
        public Vector2 MousePosition;      // 8 bytes
        public Vector2 ViewportSize;       // 8 bytes
        public float SplitModeValue;       // 4 bytes
        public float SplitPosition;        // 4 bytes
        public float SplitPositionV;       // 4 bytes
        public float ComparisonMode;       // 4 bytes
        public float Radius;               // 4 bytes - Circle mode radius
        public float RectWidth;            // 4 bytes - Rectangle mode width
        public float RectHeight;           // 4 bytes - Rectangle mode height
        public float EdgeSoftness;         // 4 bytes - Edge blending softness (0=hard, 1=soft)

        // Per-zone parameters (64 bytes each × 4 = 256 bytes)
        public ZoneParams Zone0;
        public ZoneParams Zone1;
        public ZoneParams Zone2;
        public ZoneParams Zone3;
    }

    #endregion
}
