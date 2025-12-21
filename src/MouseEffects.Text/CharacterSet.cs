namespace MouseEffects.Text;

/// <summary>
/// Maps characters to entity types for text rendering.
/// </summary>
public static class CharacterSet
{
    /// <summary>
    /// Convert a character to its entity type for shader rendering.
    /// </summary>
    /// <param name="c">The character to convert.</param>
    /// <returns>The entity type float value for the shader.</returns>
    public static float GetEntityType(char c)
    {
        // Digits
        if (c >= '0' && c <= '9')
            return TextEntityTypes.Digit0 + (c - '0');

        // Uppercase letters
        if (c >= 'A' && c <= 'Z')
            return TextEntityTypes.LetterA + (c - 'A');

        // Lowercase letters (convert to uppercase)
        if (c >= 'a' && c <= 'z')
            return TextEntityTypes.LetterA + (c - 'a');

        // Punctuation
        return c switch
        {
            ':' => TextEntityTypes.Colon,
            '/' => TextEntityTypes.Slash,
            '.' => TextEntityTypes.Dot,
            '-' => TextEntityTypes.Dash,
            ' ' => TextEntityTypes.Space,
            _ => TextEntityTypes.Space // Unknown characters become space
        };
    }

    /// <summary>
    /// Check if a character is renderable (not a space/unknown).
    /// </summary>
    public static bool IsRenderable(char c)
    {
        float entityType = GetEntityType(c);
        return entityType != TextEntityTypes.Space;
    }

    /// <summary>
    /// Get the width multiplier for a character.
    /// Some characters (like colon, dot) are narrower.
    /// </summary>
    public static float GetWidthMultiplier(char c)
    {
        return c switch
        {
            ':' => 0.5f,
            '.' => 0.5f,
            '-' => 0.7f,
            ' ' => 0.6f,
            'M' or 'm' or 'W' or 'w' => 1.2f,
            _ => 1.0f
        };
    }
}
