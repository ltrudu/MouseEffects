using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Serializable preset class for custom user presets.
/// Uses hex color strings instead of Vector3 for JSON compatibility.
/// </summary>
public class CustomPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCustom { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Red channel settings
    public bool RedEnabled { get; set; }
    public float RedStrength { get; set; } = 1.0f;
    public string RedStartColor { get; set; } = "#FF0000";
    public string RedEndColor { get; set; } = "#00FFFF";
    public float RedWhiteProtection { get; set; } = 0.0f;
    public float RedDominanceThreshold { get; set; } = 0.0f;
    public int RedBlendMode { get; set; } = 0; // LutBlendMode as int for JSON

    // Green channel settings
    public bool GreenEnabled { get; set; }
    public float GreenStrength { get; set; } = 1.0f;
    public string GreenStartColor { get; set; } = "#00FF00";
    public string GreenEndColor { get; set; } = "#00FFFF";
    public float GreenWhiteProtection { get; set; } = 0.0f;
    public float GreenDominanceThreshold { get; set; } = 0.0f;
    public int GreenBlendMode { get; set; } = 0; // LutBlendMode as int for JSON

    // Blue channel settings
    public bool BlueEnabled { get; set; }
    public float BlueStrength { get; set; } = 1.0f;
    public string BlueStartColor { get; set; } = "#0000FF";
    public string BlueEndColor { get; set; } = "#FFFF00";
    public float BlueWhiteProtection { get; set; } = 0.0f;
    public float BlueDominanceThreshold { get; set; } = 0.0f;
    public int BlueBlendMode { get; set; } = 0; // LutBlendMode as int for JSON

    // Global settings
    public float DefaultIntensity { get; set; } = 1.0f;
    public int RecommendedGradientType { get; set; } = 0;
    public int RecommendedApplicationMode { get; set; } = 0;
    public float Threshold { get; set; } = 0.3f;

    // Simulation-Guided Correction settings
    public bool SimulationGuidedEnabled { get; set; } = false;
    public int SimulationGuidedAlgorithm { get; set; } = 0;
    public int SimulationGuidedFilterType { get; set; } = 3;
    public float SimulationGuidedSensitivity { get; set; } = 2.0f;

    // Post-Correction Simulation settings
    public bool PostCorrectionSimEnabled { get; set; } = false;
    public int PostCorrectionSimAlgorithm { get; set; } = 0;
    public int PostCorrectionSimFilterType { get; set; } = 3;
    public float PostCorrectionSimIntensity { get; set; } = 1.0f;

    /// <summary>
    /// Creates a CustomPreset from a CorrectionPreset (built-in preset).
    /// </summary>
    public static CustomPreset FromCorrectionPreset(CorrectionPreset preset)
    {
        return new CustomPreset
        {
            Name = preset.Name,
            Description = preset.Description,
            IsCustom = false,
            CreatedDate = DateTime.UtcNow,

            RedEnabled = preset.RedEnabled,
            RedStrength = preset.RedStrength,
            RedStartColor = ToHexColor(preset.RedStartColor),
            RedEndColor = ToHexColor(preset.RedEndColor),
            RedWhiteProtection = preset.RedWhiteProtection,
            RedDominanceThreshold = preset.RedDominanceThreshold,
            RedBlendMode = (int)preset.RedBlendMode,

            GreenEnabled = preset.GreenEnabled,
            GreenStrength = preset.GreenStrength,
            GreenStartColor = ToHexColor(preset.GreenStartColor),
            GreenEndColor = ToHexColor(preset.GreenEndColor),
            GreenWhiteProtection = preset.GreenWhiteProtection,
            GreenDominanceThreshold = preset.GreenDominanceThreshold,
            GreenBlendMode = (int)preset.GreenBlendMode,

            BlueEnabled = preset.BlueEnabled,
            BlueStrength = preset.BlueStrength,
            BlueStartColor = ToHexColor(preset.BlueStartColor),
            BlueEndColor = ToHexColor(preset.BlueEndColor),
            BlueWhiteProtection = preset.BlueWhiteProtection,
            BlueDominanceThreshold = preset.BlueDominanceThreshold,
            BlueBlendMode = (int)preset.BlueBlendMode,

            DefaultIntensity = preset.DefaultIntensity,
            RecommendedGradientType = (int)preset.RecommendedGradientType,
            RecommendedApplicationMode = (int)preset.RecommendedApplicationMode,
            Threshold = preset.Threshold,

            SimulationGuidedEnabled = preset.SimulationGuidedEnabled,
            SimulationGuidedAlgorithm = preset.SimulationGuidedAlgorithm,
            SimulationGuidedFilterType = preset.SimulationGuidedFilterType,
            SimulationGuidedSensitivity = preset.SimulationGuidedSensitivity,

            PostCorrectionSimEnabled = preset.PostCorrectionSimEnabled,
            PostCorrectionSimAlgorithm = preset.PostCorrectionSimAlgorithm,
            PostCorrectionSimFilterType = preset.PostCorrectionSimFilterType,
            PostCorrectionSimIntensity = preset.PostCorrectionSimIntensity
        };
    }

    /// <summary>
    /// Converts this CustomPreset to a CorrectionPreset for use by the effect.
    /// </summary>
    public CorrectionPreset ToCorrectionPreset()
    {
        return new CorrectionPreset
        {
            Name = Name,
            Description = Description,

            RedEnabled = RedEnabled,
            RedStrength = RedStrength,
            RedStartColor = ParseHexColor(RedStartColor),
            RedEndColor = ParseHexColor(RedEndColor),
            RedWhiteProtection = RedWhiteProtection,
            RedDominanceThreshold = RedDominanceThreshold,
            RedBlendMode = (LutBlendMode)RedBlendMode,

            GreenEnabled = GreenEnabled,
            GreenStrength = GreenStrength,
            GreenStartColor = ParseHexColor(GreenStartColor),
            GreenEndColor = ParseHexColor(GreenEndColor),
            GreenWhiteProtection = GreenWhiteProtection,
            GreenDominanceThreshold = GreenDominanceThreshold,
            GreenBlendMode = (LutBlendMode)GreenBlendMode,

            BlueEnabled = BlueEnabled,
            BlueStrength = BlueStrength,
            BlueStartColor = ParseHexColor(BlueStartColor),
            BlueEndColor = ParseHexColor(BlueEndColor),
            BlueWhiteProtection = BlueWhiteProtection,
            BlueDominanceThreshold = BlueDominanceThreshold,
            BlueBlendMode = (LutBlendMode)BlueBlendMode,

            DefaultIntensity = DefaultIntensity,
            RecommendedGradientType = (GradientType)RecommendedGradientType,
            RecommendedApplicationMode = (ApplicationMode)RecommendedApplicationMode,
            Threshold = Threshold,

            SimulationGuidedEnabled = SimulationGuidedEnabled,
            SimulationGuidedAlgorithm = SimulationGuidedAlgorithm,
            SimulationGuidedFilterType = SimulationGuidedFilterType,
            SimulationGuidedSensitivity = SimulationGuidedSensitivity,

            PostCorrectionSimEnabled = PostCorrectionSimEnabled,
            PostCorrectionSimAlgorithm = PostCorrectionSimAlgorithm,
            PostCorrectionSimFilterType = PostCorrectionSimFilterType,
            PostCorrectionSimIntensity = PostCorrectionSimIntensity
        };
    }

    /// <summary>
    /// Converts a Vector3 color (0-1 range) to hex string.
    /// </summary>
    public static string ToHexColor(Vector3 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Parses a hex color string to Vector3 (0-1 range).
    /// </summary>
    public static Vector3 ParseHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return new Vector3(1, 1, 1);

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

    /// <summary>
    /// Generates a safe filename from the preset name.
    /// </summary>
    public string GetSafeFileName()
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(Name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return safeName.Trim();
    }
}
