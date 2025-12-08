using System.Numerics;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Zone mode: what type of processing to apply.
/// </summary>
public enum ZoneMode
{
    Original = 0,    // No processing, show original screen
    Simulation = 1,  // CVD simulation
    Correction = 2   // Color correction (LUT-based or Daltonization)
}

/// <summary>
/// Correction algorithm to use when Mode is Correction.
/// </summary>
public enum CorrectionAlgorithm
{
    /// <summary>
    /// LUT-based color remapping (per-channel false coloring).
    /// </summary>
    LUTBased = 0,

    /// <summary>
    /// Daltonization algorithm (error redistribution to visible channels).
    /// Based on scientific CVD simulation to calculate lost colors and redistribute them.
    /// </summary>
    Daltonization = 1,

    /// <summary>
    /// Hue Rotation in HSL color space.
    /// Rotates problematic hue ranges to more distinguishable positions on the color wheel.
    /// </summary>
    HueRotation = 2,

    /// <summary>
    /// CIELAB perceptual color space remapping.
    /// Transfers color information between perceptual axes (a*=red-green, b*=blue-yellow).
    /// </summary>
    CIELABRemapping = 3
}

/// <summary>
/// CVD type for correction algorithms (simplified list for user selection).
/// </summary>
public enum CVDCorrectionType
{
    Protanopia = 0,    // Red-blind (severe)
    Protanomaly = 1,   // Red-weak (mild)
    Deuteranopia = 2,  // Green-blind (severe)
    Deuteranomaly = 3, // Green-weak (mild)
    Tritanopia = 4,    // Blue-blind (severe)
    Tritanomaly = 5    // Blue-weak (mild)
}

/// <summary>
/// Settings for a single screen zone.
/// Each zone can independently be set to Original, Simulation, or Correction mode.
/// </summary>
public class ZoneSettings
{
    /// <summary>
    /// Zone processing mode.
    /// </summary>
    public ZoneMode Mode { get; set; } = ZoneMode.Original;

    // ============ Simulation Settings ============

    /// <summary>
    /// Simulation algorithm (0=Machado, 1=Strict/LMS).
    /// </summary>
    public SimulationAlgorithm SimulationAlgorithm { get; set; } = SimulationAlgorithm.Machado;

    /// <summary>
    /// Simulation filter type (0=None, 1-6=Machado types, 7-12=Strict types, 13-14=Achro).
    /// </summary>
    public int SimulationFilterType { get; set; } = 0;

    // ============ Correction Settings ============

    /// <summary>
    /// Which correction algorithm to use.
    /// </summary>
    public CorrectionAlgorithm CorrectionAlgorithm { get; set; } = CorrectionAlgorithm.LUTBased;

    // ============ Daltonization Settings ============

    /// <summary>
    /// CVD type to correct for when using Daltonization.
    /// Uses the same filter type values as simulation (1-6=Machado, 7-12=Strict).
    /// </summary>
    public int DaltonizationCVDType { get; set; } = 3; // Deuteranopia by default

    /// <summary>
    /// Strength of the Daltonization correction (0.0-1.0).
    /// </summary>
    public float DaltonizationStrength { get; set; } = 1.0f;

    // ============ Hue Rotation Settings ============

    /// <summary>
    /// CVD type to correct for (determines auto-configuration of hue ranges).
    /// </summary>
    public CVDCorrectionType HueRotationCVDType { get; set; } = CVDCorrectionType.Deuteranopia;

    /// <summary>
    /// Overall strength of the hue rotation effect (0.0-1.0).
    /// </summary>
    public float HueRotationStrength { get; set; } = 1.0f;

    /// <summary>
    /// Enable advanced mode for manual control of hue rotation parameters.
    /// </summary>
    public bool HueRotationAdvancedMode { get; set; } = false;

    /// <summary>
    /// Start of the source hue range to rotate (0-360 degrees).
    /// Only used when AdvancedMode is enabled.
    /// </summary>
    public float HueRotationSourceStart { get; set; } = 0f;

    /// <summary>
    /// End of the source hue range to rotate (0-360 degrees).
    /// Only used when AdvancedMode is enabled.
    /// </summary>
    public float HueRotationSourceEnd { get; set; } = 120f;

    /// <summary>
    /// Amount to shift the hue (-180 to +180 degrees).
    /// Only used when AdvancedMode is enabled.
    /// </summary>
    public float HueRotationShift { get; set; } = 60f;

    /// <summary>
    /// Softness of the hue range boundaries (0.0=hard, 1.0=very soft).
    /// Only used when AdvancedMode is enabled.
    /// </summary>
    public float HueRotationFalloff { get; set; } = 0.3f;

    // ============ CIELAB Remapping Settings ============

    /// <summary>
    /// CVD type to correct for (determines auto-configuration of CIELAB remapping).
    /// </summary>
    public CVDCorrectionType CIELABCVDType { get; set; } = CVDCorrectionType.Deuteranopia;

    /// <summary>
    /// Overall strength of the CIELAB remapping effect (0.0-1.0).
    /// </summary>
    public float CIELABStrength { get; set; } = 1.0f;

    /// <summary>
    /// Enable advanced mode for manual control of CIELAB parameters.
    /// </summary>
    public bool CIELABAdvancedMode { get; set; } = false;

    /// <summary>
    /// Transfer factor from a* (red-green) to b* (blue-yellow) axis.
    /// Positive values shift red-green info to blue-yellow. Range: -1.0 to 1.0.
    /// </summary>
    public float CIELABAtoB { get; set; } = 0.5f;

    /// <summary>
    /// Transfer factor from b* (blue-yellow) to a* (red-green) axis.
    /// Positive values shift blue-yellow info to red-green. Range: -1.0 to 1.0.
    /// </summary>
    public float CIELABBtoA { get; set; } = 0.0f;

    /// <summary>
    /// Enhancement multiplier for a* (red-green) axis. Range: 0.0 to 2.0.
    /// Values > 1.0 increase red-green contrast.
    /// </summary>
    public float CIELABAEnhance { get; set; } = 1.0f;

    /// <summary>
    /// Enhancement multiplier for b* (blue-yellow) axis. Range: 0.0 to 2.0.
    /// Values > 1.0 increase blue-yellow contrast.
    /// </summary>
    public float CIELABBEnhance { get; set; } = 1.0f;

    /// <summary>
    /// How much to encode color differences into lightness. Range: 0.0 to 1.0.
    /// Higher values make color differences visible as brightness differences.
    /// </summary>
    public float CIELABLEnhance { get; set; } = 0.0f;

    // ============ LUT Correction Settings ============

    /// <summary>
    /// LUT application mode.
    /// </summary>
    public ApplicationMode ApplicationMode { get; set; } = ApplicationMode.FullChannel;

    /// <summary>
    /// Gradient interpolation type for LUT generation.
    /// </summary>
    public GradientType GradientType { get; set; } = GradientType.LinearRGB;

    /// <summary>
    /// Threshold for threshold application mode.
    /// </summary>
    public float Threshold { get; set; } = 0.3f;

    /// <summary>
    /// Global intensity for this zone.
    /// </summary>
    public float Intensity { get; set; } = 1.0f;

    // ============ Simulation-Guided Correction Settings ============

    /// <summary>
    /// When enabled, uses CVD simulation to detect which pixels are affected
    /// and applies LUT correction only to those pixels (proportionally to error magnitude).
    /// </summary>
    public bool SimulationGuidedEnabled { get; set; } = false;

    /// <summary>
    /// Algorithm to use for simulation-guided detection.
    /// </summary>
    public SimulationAlgorithm SimulationGuidedAlgorithm { get; set; } = SimulationAlgorithm.Machado;

    /// <summary>
    /// CVD filter type for simulation-guided detection (1-6=Machado, 7-12=Strict, 13-14=Achro).
    /// This determines which type of color blindness to detect/correct for.
    /// </summary>
    public int SimulationGuidedFilterType { get; set; } = 3; // Deuteranopia by default

    /// <summary>
    /// Sensitivity multiplier for simulation-guided detection.
    /// Lower values (0.5) = conservative, only strongly affected pixels get corrected.
    /// Higher values (5.0) = aggressive, more pixels are detected as needing correction.
    /// Default is 2.0 for balanced detection.
    /// </summary>
    public float SimulationGuidedSensitivity { get; set; } = 2.0f;

    // ============ Post-Correction Simulation Settings ============

    /// <summary>
    /// When enabled, applies CVD simulation AFTER correction.
    /// This allows non-colorblind users to verify how corrected colors appear to CVD users.
    /// </summary>
    public bool PostCorrectionSimEnabled { get; set; } = false;

    /// <summary>
    /// Algorithm to use for post-correction simulation.
    /// </summary>
    public SimulationAlgorithm PostCorrectionSimAlgorithm { get; set; } = SimulationAlgorithm.Machado;

    /// <summary>
    /// CVD filter type for post-correction simulation (1-6=Machado, 7-12=Strict, 13-14=Achro).
    /// </summary>
    public int PostCorrectionSimFilterType { get; set; } = 3; // Deuteranopia by default

    /// <summary>
    /// Intensity of post-correction simulation blend (0.0-1.0).
    /// 0.0 = no simulation applied, 1.0 = full simulation.
    /// </summary>
    public float PostCorrectionSimIntensity { get; set; } = 1.0f;

    // ============ Per-Channel LUT Settings ============

    /// <summary>
    /// Red channel settings.
    /// </summary>
    public ChannelLUTSettings RedChannel { get; } = new()
    {
        StartColor = new Vector3(1, 0, 0),
        EndColor = new Vector3(0, 1, 1)
    };

    /// <summary>
    /// Green channel settings.
    /// </summary>
    public ChannelLUTSettings GreenChannel { get; } = new()
    {
        StartColor = new Vector3(0, 1, 0),
        EndColor = new Vector3(0, 1, 1)
    };

    /// <summary>
    /// Blue channel settings.
    /// </summary>
    public ChannelLUTSettings BlueChannel { get; } = new()
    {
        StartColor = new Vector3(0, 0, 1),
        EndColor = new Vector3(1, 1, 0)
    };

    /// <summary>
    /// Flag to indicate LUTs need regeneration.
    /// </summary>
    public bool LutsNeedUpdate { get; set; } = true;

    /// <summary>
    /// Creates a deep copy of this zone settings.
    /// </summary>
    public ZoneSettings Clone()
    {
        var clone = new ZoneSettings
        {
            Mode = Mode,
            SimulationAlgorithm = SimulationAlgorithm,
            SimulationFilterType = SimulationFilterType,
            CorrectionAlgorithm = CorrectionAlgorithm,
            DaltonizationCVDType = DaltonizationCVDType,
            DaltonizationStrength = DaltonizationStrength,
            ApplicationMode = ApplicationMode,
            GradientType = GradientType,
            Threshold = Threshold,
            Intensity = Intensity,
            SimulationGuidedEnabled = SimulationGuidedEnabled,
            SimulationGuidedAlgorithm = SimulationGuidedAlgorithm,
            SimulationGuidedFilterType = SimulationGuidedFilterType,
            SimulationGuidedSensitivity = SimulationGuidedSensitivity,
            PostCorrectionSimEnabled = PostCorrectionSimEnabled,
            PostCorrectionSimAlgorithm = PostCorrectionSimAlgorithm,
            PostCorrectionSimFilterType = PostCorrectionSimFilterType,
            PostCorrectionSimIntensity = PostCorrectionSimIntensity,
            LutsNeedUpdate = LutsNeedUpdate,
            // Hue Rotation settings
            HueRotationCVDType = HueRotationCVDType,
            HueRotationStrength = HueRotationStrength,
            HueRotationAdvancedMode = HueRotationAdvancedMode,
            HueRotationSourceStart = HueRotationSourceStart,
            HueRotationSourceEnd = HueRotationSourceEnd,
            HueRotationShift = HueRotationShift,
            HueRotationFalloff = HueRotationFalloff,
            // CIELAB Remapping settings
            CIELABCVDType = CIELABCVDType,
            CIELABStrength = CIELABStrength,
            CIELABAdvancedMode = CIELABAdvancedMode,
            CIELABAtoB = CIELABAtoB,
            CIELABBtoA = CIELABBtoA,
            CIELABAEnhance = CIELABAEnhance,
            CIELABBEnhance = CIELABBEnhance,
            CIELABLEnhance = CIELABLEnhance
        };

        // Copy channel settings (including per-channel blend modes)
        clone.RedChannel.Enabled = RedChannel.Enabled;
        clone.RedChannel.Strength = RedChannel.Strength;
        clone.RedChannel.StartColor = RedChannel.StartColor;
        clone.RedChannel.EndColor = RedChannel.EndColor;
        clone.RedChannel.WhiteProtection = RedChannel.WhiteProtection;
        clone.RedChannel.DominanceThreshold = RedChannel.DominanceThreshold;
        clone.RedChannel.BlendMode = RedChannel.BlendMode;

        clone.GreenChannel.Enabled = GreenChannel.Enabled;
        clone.GreenChannel.Strength = GreenChannel.Strength;
        clone.GreenChannel.StartColor = GreenChannel.StartColor;
        clone.GreenChannel.EndColor = GreenChannel.EndColor;
        clone.GreenChannel.WhiteProtection = GreenChannel.WhiteProtection;
        clone.GreenChannel.DominanceThreshold = GreenChannel.DominanceThreshold;
        clone.GreenChannel.BlendMode = GreenChannel.BlendMode;

        clone.BlueChannel.Enabled = BlueChannel.Enabled;
        clone.BlueChannel.Strength = BlueChannel.Strength;
        clone.BlueChannel.StartColor = BlueChannel.StartColor;
        clone.BlueChannel.EndColor = BlueChannel.EndColor;
        clone.BlueChannel.WhiteProtection = BlueChannel.WhiteProtection;
        clone.BlueChannel.DominanceThreshold = BlueChannel.DominanceThreshold;
        clone.BlueChannel.BlendMode = BlueChannel.BlendMode;

        return clone;
    }

    /// <summary>
    /// Applies a correction preset to this zone's correction settings.
    /// </summary>
    public void ApplyPreset(CorrectionPreset preset)
    {
        RedChannel.Enabled = preset.RedEnabled;
        RedChannel.Strength = preset.RedStrength;
        RedChannel.StartColor = preset.RedStartColor;
        RedChannel.EndColor = preset.RedEndColor;
        RedChannel.WhiteProtection = preset.RedWhiteProtection;
        RedChannel.DominanceThreshold = preset.RedDominanceThreshold;
        RedChannel.BlendMode = preset.RedBlendMode;

        GreenChannel.Enabled = preset.GreenEnabled;
        GreenChannel.Strength = preset.GreenStrength;
        GreenChannel.StartColor = preset.GreenStartColor;
        GreenChannel.EndColor = preset.GreenEndColor;
        GreenChannel.WhiteProtection = preset.GreenWhiteProtection;
        GreenChannel.DominanceThreshold = preset.GreenDominanceThreshold;
        GreenChannel.BlendMode = preset.GreenBlendMode;

        BlueChannel.Enabled = preset.BlueEnabled;
        BlueChannel.Strength = preset.BlueStrength;
        BlueChannel.StartColor = preset.BlueStartColor;
        BlueChannel.EndColor = preset.BlueEndColor;
        BlueChannel.WhiteProtection = preset.BlueWhiteProtection;
        BlueChannel.DominanceThreshold = preset.BlueDominanceThreshold;
        BlueChannel.BlendMode = preset.BlueBlendMode;

        GradientType = preset.RecommendedGradientType;
        ApplicationMode = preset.RecommendedApplicationMode;
        Intensity = preset.DefaultIntensity;

        LutsNeedUpdate = true;
    }
}
