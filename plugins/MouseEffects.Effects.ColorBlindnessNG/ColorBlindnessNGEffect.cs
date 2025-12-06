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
/// Split screen mode for comparing original vs corrected.
/// </summary>
public enum SplitMode
{
    Fullscreen = 0,
    SplitVertical = 1,
    SplitHorizontal = 2,
    Quadrants = 3
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
    public float WhiteProtection { get; set; } = 0.0f;
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
        _ => 1
    };

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
        // No per-frame updates needed
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
            ViewportSize = viewportSize,
            SplitModeValue = (float)_splitMode,
            SplitPosition = _splitPosition,
            SplitPositionV = _splitPositionV,
            ComparisonMode = _comparisonMode ? 1.0f : 0.0f
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
                Padding1 = 0,
                Padding2 = 0
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
    /// Size: 64 bytes (16 floats)
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
        public float Padding1;             // Padding
        public float Padding2;             // Padding
    }

    /// <summary>
    /// Full constant buffer for shader.
    /// Size: 16 (global) + 4 * 64 (zones) = 272 bytes, padded to 288 for 16-byte alignment
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 288)]
    private struct ColorBlindnessNGParams
    {
        // Global parameters (16 bytes)
        public Vector2 ViewportSize;       // 8 bytes
        public float SplitModeValue;       // 4 bytes
        public float SplitPosition;        // 4 bytes

        public float SplitPositionV;       // 4 bytes
        public float ComparisonMode;       // 4 bytes
        public float GlobalPadding1;       // 4 bytes
        public float GlobalPadding2;       // 4 bytes

        // Per-zone parameters (64 bytes each × 4 = 256 bytes)
        public ZoneParams Zone0;
        public ZoneParams Zone1;
        public ZoneParams Zone2;
        public ZoneParams Zone3;
    }

    #endregion
}
