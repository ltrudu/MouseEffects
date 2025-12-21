namespace MouseEffects.Text;

/// <summary>
/// Standardized entity type constants for text rendering.
/// These map directly to the shader's entity type dispatch.
/// </summary>
public static class TextEntityTypes
{
    // Digits 0-9 (entity types 0-9)
    public const float Digit0 = 0f;
    public const float Digit1 = 1f;
    public const float Digit2 = 2f;
    public const float Digit3 = 3f;
    public const float Digit4 = 4f;
    public const float Digit5 = 5f;
    public const float Digit6 = 6f;
    public const float Digit7 = 7f;
    public const float Digit8 = 8f;
    public const float Digit9 = 9f;

    // Punctuation (entity types 10-19)
    public const float Colon = 10f;
    public const float Slash = 11f;
    public const float Dot = 12f;
    public const float Dash = 13f;

    // Letters A-Z (entity types 20-45)
    public const float LetterA = 20f;
    public const float LetterB = 21f;
    public const float LetterC = 22f;
    public const float LetterD = 23f;
    public const float LetterE = 24f;
    public const float LetterF = 25f;
    public const float LetterG = 26f;
    public const float LetterH = 27f;
    public const float LetterI = 28f;
    public const float LetterJ = 29f;
    public const float LetterK = 30f;
    public const float LetterL = 31f;
    public const float LetterM = 32f;
    public const float LetterN = 33f;
    public const float LetterO = 34f;
    public const float LetterP = 35f;
    public const float LetterQ = 36f;
    public const float LetterR = 37f;
    public const float LetterS = 38f;
    public const float LetterT = 39f;
    public const float LetterU = 40f;
    public const float LetterV = 41f;
    public const float LetterW = 42f;
    public const float LetterX = 43f;
    public const float LetterY = 44f;
    public const float LetterZ = 45f;

    // Special types
    public const float Space = 50f;
    public const float Background = 60f;

    /// <summary>
    /// Get the entity type for a digit (0-9).
    /// </summary>
    public static float GetDigitType(int digit) => Digit0 + digit;

    /// <summary>
    /// Get the entity type for a letter (A-Z or a-z).
    /// </summary>
    public static float GetLetterType(char letter)
    {
        char upper = char.ToUpperInvariant(letter);
        if (upper >= 'A' && upper <= 'Z')
            return LetterA + (upper - 'A');
        return Space; // Unknown letter becomes space
    }
}
