using System.Numerics;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Defines a correction preset for a specific CVD type.
/// </summary>
public record CorrectionPreset
{
    public required string Name { get; init; }
    public required string Description { get; init; }

    // Red channel settings
    public bool RedEnabled { get; init; }
    public float RedStrength { get; init; } = 1.0f;
    public Vector3 RedStartColor { get; init; } = new(1, 0, 0);
    public Vector3 RedEndColor { get; init; } = new(0, 1, 1);
    public float RedWhiteProtection { get; init; } = 0.01f;

    // Green channel settings
    public bool GreenEnabled { get; init; }
    public float GreenStrength { get; init; } = 1.0f;
    public Vector3 GreenStartColor { get; init; } = new(0, 1, 0);
    public Vector3 GreenEndColor { get; init; } = new(0, 1, 1);
    public float GreenWhiteProtection { get; init; } = 0.01f;

    // Blue channel settings
    public bool BlueEnabled { get; init; }
    public float BlueStrength { get; init; } = 1.0f;
    public Vector3 BlueStartColor { get; init; } = new(0, 0, 1);
    public Vector3 BlueEndColor { get; init; } = new(1, 1, 0);
    public float BlueWhiteProtection { get; init; } = 0.01f;

    // Default intensity for this preset
    public float DefaultIntensity { get; init; } = 1.0f;

    // Recommended gradient type
    public GradientType RecommendedGradientType { get; init; } = GradientType.LinearRGB;

    // Recommended application mode
    public ApplicationMode RecommendedApplicationMode { get; init; } = ApplicationMode.FullChannel;
}

/// <summary>
/// Pre-defined correction presets for all CVD types.
/// </summary>
public static class CorrectionPresets
{
    /// <summary>
    /// Custom preset (no changes, user defines everything).
    /// </summary>
    public static CorrectionPreset Custom { get; } = new()
    {
        Name = "Custom",
        Description = "User-defined settings",
        RedEnabled = false,
        GreenEnabled = false,
        BlueEnabled = false
    };

    /// <summary>
    /// Passthrough preset - Colors remain unchanged.
    /// </summary>
    public static CorrectionPreset Passthrough { get; } = new()
    {
        Name = "Passthrough",
        Description = "No color correction applied. Colors remain unchanged.",
        RedEnabled = false,
        GreenEnabled = false,
        BlueEnabled = false,
        DefaultIntensity = 0.0f
    };

    /// <summary>
    /// Deuteranopia - Complete green cone deficiency.
    /// Shifts greens to cyan/blue for better visibility.
    /// </summary>
    public static CorrectionPreset Deuteranopia { get; } = new()
    {
        Name = "Deuteranopia",
        Description = "Green-blind (M-cone deficiency). Shifts greens to cyan.",
        RedEnabled = false,
        GreenEnabled = true,
        GreenStrength = 1.0f,
        GreenStartColor = new Vector3(0, 1, 0),     // Pure green
        GreenEndColor = new Vector3(0, 0.8f, 1),    // Cyan-blue
        BlueEnabled = false,
        DefaultIntensity = 1.0f,
        RecommendedGradientType = GradientType.HSL,
        RecommendedApplicationMode = ApplicationMode.DominantOnly
    };

    /// <summary>
    /// Protanopia - Complete red cone deficiency.
    /// Shifts reds to cyan/blue for better visibility.
    /// </summary>
    public static CorrectionPreset Protanopia { get; } = new()
    {
        Name = "Protanopia",
        Description = "Red-blind (L-cone deficiency). Shifts reds to cyan.",
        RedEnabled = true,
        RedStrength = 1.0f,
        RedStartColor = new Vector3(1, 0, 0),       // Pure red
        RedEndColor = new Vector3(0, 0.8f, 1),      // Cyan-blue
        GreenEnabled = false,
        BlueEnabled = false,
        DefaultIntensity = 1.0f,
        RecommendedGradientType = GradientType.HSL,
        RecommendedApplicationMode = ApplicationMode.DominantOnly
    };

    /// <summary>
    /// Tritanopia - Complete blue cone deficiency.
    /// Shifts blues to yellow/orange for better visibility.
    /// </summary>
    public static CorrectionPreset Tritanopia { get; } = new()
    {
        Name = "Tritanopia",
        Description = "Blue-blind (S-cone deficiency). Shifts blues to yellow.",
        RedEnabled = false,
        GreenEnabled = false,
        BlueEnabled = true,
        BlueStrength = 1.0f,
        BlueStartColor = new Vector3(0, 0, 1),      // Pure blue
        BlueEndColor = new Vector3(1, 0.9f, 0),     // Yellow-orange
        DefaultIntensity = 1.0f,
        RecommendedGradientType = GradientType.HSL,
        RecommendedApplicationMode = ApplicationMode.DominantOnly
    };

    /// <summary>
    /// Deuteranomaly - Partial green weakness.
    /// Mild shift of greens towards cyan.
    /// </summary>
    public static CorrectionPreset Deuteranomaly { get; } = new()
    {
        Name = "Deuteranomaly",
        Description = "Green-weak (partial M-cone deficiency). Mild green to teal shift.",
        RedEnabled = false,
        GreenEnabled = true,
        GreenStrength = 0.5f,
        GreenStartColor = new Vector3(0, 1, 0),     // Pure green
        GreenEndColor = new Vector3(0, 0.7f, 0.6f), // Teal
        BlueEnabled = false,
        DefaultIntensity = 0.5f,
        RecommendedGradientType = GradientType.PerceptualLAB,
        RecommendedApplicationMode = ApplicationMode.DominantOnly
    };

    /// <summary>
    /// Protanomaly - Partial red weakness.
    /// Mild shift of reds towards teal.
    /// </summary>
    public static CorrectionPreset Protanomaly { get; } = new()
    {
        Name = "Protanomaly",
        Description = "Red-weak (partial L-cone deficiency). Mild red to teal shift.",
        RedEnabled = true,
        RedStrength = 0.5f,
        RedStartColor = new Vector3(1, 0, 0),       // Pure red
        RedEndColor = new Vector3(0, 0.7f, 0.6f),   // Teal
        GreenEnabled = false,
        BlueEnabled = false,
        DefaultIntensity = 0.5f,
        RecommendedGradientType = GradientType.PerceptualLAB,
        RecommendedApplicationMode = ApplicationMode.DominantOnly
    };

    /// <summary>
    /// Tritanomaly - Partial blue weakness.
    /// Mild shift of blues towards yellow.
    /// </summary>
    public static CorrectionPreset Tritanomaly { get; } = new()
    {
        Name = "Tritanomaly",
        Description = "Blue-weak (partial S-cone deficiency). Mild blue to yellow shift.",
        RedEnabled = false,
        GreenEnabled = false,
        BlueEnabled = true,
        BlueStrength = 0.5f,
        BlueStartColor = new Vector3(0, 0, 1),      // Pure blue
        BlueEndColor = new Vector3(1, 0.9f, 0),     // Yellow
        DefaultIntensity = 0.5f,
        RecommendedGradientType = GradientType.PerceptualLAB,
        RecommendedApplicationMode = ApplicationMode.DominantOnly
    };

    /// <summary>
    /// Red-Green (Both) - Combined red and green deficiency.
    /// Shifts both to blue spectrum.
    /// </summary>
    public static CorrectionPreset RedGreenBoth { get; } = new()
    {
        Name = "Red-Green (Both)",
        Description = "Combined red-green weakness. Shifts both to blue spectrum.",
        RedEnabled = true,
        RedStrength = 0.7f,
        RedStartColor = new Vector3(1, 0, 0),       // Pure red
        RedEndColor = new Vector3(0.3f, 0.3f, 1),   // Blue
        GreenEnabled = true,
        GreenStrength = 0.7f,
        GreenStartColor = new Vector3(0, 1, 0),     // Pure green
        GreenEndColor = new Vector3(0.3f, 0.3f, 1), // Blue
        BlueEnabled = false,
        DefaultIntensity = 0.7f,
        RecommendedGradientType = GradientType.HSL,
        RecommendedApplicationMode = ApplicationMode.DominantOnly
    };

    /// <summary>
    /// High Contrast - Enhances all color differences.
    /// </summary>
    public static CorrectionPreset HighContrast { get; } = new()
    {
        Name = "High Contrast",
        Description = "Enhances color separation for general accessibility.",
        RedEnabled = true,
        RedStrength = 0.3f,
        RedStartColor = new Vector3(1, 0, 0),
        RedEndColor = new Vector3(1, 0.2f, 0.2f),
        GreenEnabled = true,
        GreenStrength = 0.3f,
        GreenStartColor = new Vector3(0, 1, 0),
        GreenEndColor = new Vector3(0.2f, 1, 0.2f),
        BlueEnabled = true,
        BlueStrength = 0.3f,
        BlueStartColor = new Vector3(0, 0, 1),
        BlueEndColor = new Vector3(0.2f, 0.2f, 1),
        DefaultIntensity = 0.5f,
        RecommendedGradientType = GradientType.LinearRGB,
        RecommendedApplicationMode = ApplicationMode.FullChannel
    };

    /// <summary>
    /// Gets all available presets.
    /// </summary>
    public static IReadOnlyList<CorrectionPreset> All { get; } = new[]
    {
        Custom,
        Passthrough,
        Deuteranopia,
        Protanopia,
        Tritanopia,
        Deuteranomaly,
        Protanomaly,
        Tritanomaly,
        RedGreenBoth,
        HighContrast
    };

    /// <summary>
    /// Gets a preset by index.
    /// </summary>
    public static CorrectionPreset GetByIndex(int index)
    {
        if (index < 0 || index >= All.Count)
            return Custom;
        return All[index];
    }

    /// <summary>
    /// Gets a preset by name.
    /// </summary>
    public static CorrectionPreset? GetByName(string name)
    {
        return All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
