using System.Numerics;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Zone mode: what type of processing to apply.
/// </summary>
public enum ZoneMode
{
    Original = 0,    // No processing, show original screen
    Simulation = 1,  // CVD simulation
    Correction = 2   // LUT-based correction
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
            ApplicationMode = ApplicationMode,
            GradientType = GradientType,
            Threshold = Threshold,
            Intensity = Intensity,
            SimulationGuidedEnabled = SimulationGuidedEnabled,
            SimulationGuidedAlgorithm = SimulationGuidedAlgorithm,
            SimulationGuidedFilterType = SimulationGuidedFilterType,
            LutsNeedUpdate = LutsNeedUpdate
        };

        // Copy channel settings
        clone.RedChannel.Enabled = RedChannel.Enabled;
        clone.RedChannel.Strength = RedChannel.Strength;
        clone.RedChannel.StartColor = RedChannel.StartColor;
        clone.RedChannel.EndColor = RedChannel.EndColor;
        clone.RedChannel.WhiteProtection = RedChannel.WhiteProtection;

        clone.GreenChannel.Enabled = GreenChannel.Enabled;
        clone.GreenChannel.Strength = GreenChannel.Strength;
        clone.GreenChannel.StartColor = GreenChannel.StartColor;
        clone.GreenChannel.EndColor = GreenChannel.EndColor;
        clone.GreenChannel.WhiteProtection = GreenChannel.WhiteProtection;

        clone.BlueChannel.Enabled = BlueChannel.Enabled;
        clone.BlueChannel.Strength = BlueChannel.Strength;
        clone.BlueChannel.StartColor = BlueChannel.StartColor;
        clone.BlueChannel.EndColor = BlueChannel.EndColor;
        clone.BlueChannel.WhiteProtection = BlueChannel.WhiteProtection;

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

        GreenChannel.Enabled = preset.GreenEnabled;
        GreenChannel.Strength = preset.GreenStrength;
        GreenChannel.StartColor = preset.GreenStartColor;
        GreenChannel.EndColor = preset.GreenEndColor;
        GreenChannel.WhiteProtection = preset.GreenWhiteProtection;

        BlueChannel.Enabled = preset.BlueEnabled;
        BlueChannel.Strength = preset.BlueStrength;
        BlueChannel.StartColor = preset.BlueStartColor;
        BlueChannel.EndColor = preset.BlueEndColor;
        BlueChannel.WhiteProtection = preset.BlueWhiteProtection;

        GradientType = preset.RecommendedGradientType;
        ApplicationMode = preset.RecommendedApplicationMode;
        Intensity = preset.DefaultIntensity;

        LutsNeedUpdate = true;
    }
}
