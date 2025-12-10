namespace MouseEffects.Effects.ASCIIZer;

/// <summary>
/// Available ASCII-style filter types.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// Traditional ASCII art using brightness-to-character mapping.
    /// </summary>
    ASCIIClassic = 0,

    /// <summary>
    /// Falling green characters effect (Matrix-style).
    /// </summary>
    MatrixRain = 1,

    /// <summary>
    /// LED/dot matrix display simulation.
    /// </summary>
    DotMatrix = 2,

    /// <summary>
    /// Old typewriter with ink variations.
    /// </summary>
    Typewriter = 3,

    /// <summary>
    /// Unicode Braille pattern rendering for high resolution.
    /// </summary>
    Braille = 4,

    /// <summary>
    /// ASCII art based on edge detection.
    /// </summary>
    EdgeASCII = 5
}
