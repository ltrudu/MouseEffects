namespace MouseEffects.Effects.Retro;

/// <summary>
/// Available retro scaling filter types.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// Super 2xSaI scaling algorithm for smooth pixel art upscaling.
    /// </summary>
    XSaI = 0,

    /// <summary>
    /// CRT TV simulation with RGB phosphor pattern.
    /// </summary>
    TVFilter = 1,

    /// <summary>
    /// Cel-shading / Cartoon effect with edge detection and color quantization.
    /// </summary>
    ToonFilter = 2

    // Future filters:
    // SuperEagle = 3,
    // HQ2x = 4,
    // Scale2x = 5,
}
