using System.Numerics;

namespace MouseEffects.Text.Style;

/// <summary>
/// Defines the visual style for text rendering.
/// </summary>
public class TextStyle
{
    /// <summary>Text color (RGBA).</summary>
    public Vector4 Color { get; init; } = new(1f, 1f, 1f, 1f);

    /// <summary>Character size in pixels.</summary>
    public float Size { get; init; } = 32f;

    /// <summary>Character spacing multiplier (1.0 = no extra space).</summary>
    public float Spacing { get; init; } = 1.3f;

    /// <summary>Glow intensity multiplier.</summary>
    public float GlowIntensity { get; init; } = 1.0f;

    /// <summary>Optional animation effect.</summary>
    public TextAnimation? Animation { get; init; }

    // ========== Preset Styles ==========

    /// <summary>Default white text.</summary>
    public static TextStyle Default => new();

    /// <summary>Large title text with high glow.</summary>
    public static TextStyle Title => new()
    {
        Size = 64f,
        GlowIntensity = 1.5f
    };

    /// <summary>Dimmed gray label text.</summary>
    public static TextStyle Label => new()
    {
        Size = 24f,
        Color = new Vector4(0.7f, 0.7f, 0.7f, 1f)
    };

    /// <summary>Green score display.</summary>
    public static TextStyle Score => new()
    {
        Size = 48f,
        Color = new Vector4(0f, 1f, 0f, 1f),
        GlowIntensity = 1.2f
    };

    /// <summary>Cyan timer display.</summary>
    public static TextStyle Timer => new()
    {
        Size = 32f,
        Color = new Vector4(0f, 0.8f, 1f, 1f)
    };

    /// <summary>Yellow warning text.</summary>
    public static TextStyle Warning => new()
    {
        Size = 40f,
        Color = new Vector4(1f, 1f, 0f, 1f),
        GlowIntensity = 1.3f
    };

    /// <summary>Red game over text with pulsing animation.</summary>
    public static TextStyle GameOver => new()
    {
        Size = 80f,
        Color = new Vector4(1f, 0.2f, 0.1f, 1f),
        GlowIntensity = 1.5f,
        Animation = TextAnimation.Pulse(3f, 0.4f)
    };

    /// <summary>Neon cyan for high scores title.</summary>
    public static TextStyle HighScoreTitle => new()
    {
        Size = 48f,
        Color = new Vector4(0f, 1f, 1f, 1f),
        GlowIntensity = 1.4f,
        Animation = TextAnimation.Pulse(2f, 0.2f)
    };

    /// <summary>Neon blue for old high score entries.</summary>
    public static TextStyle HighScoreEntry => new()
    {
        Size = 32f,
        Color = new Vector4(0.3f, 0.5f, 1f, 1f),
        GlowIntensity = 1.1f
    };

    /// <summary>Rainbow animated for new high score.</summary>
    public static TextStyle NewHighScore => new()
    {
        Size = 36f,
        Color = new Vector4(1f, 1f, 1f, 1f),
        GlowIntensity = 1.5f,
        Animation = TextAnimation.Rainbow(0.7f)
    };

    // ========== Builder Methods ==========

    /// <summary>Create a copy with a different color.</summary>
    public TextStyle WithColor(Vector4 color) => new()
    {
        Color = color,
        Size = Size,
        Spacing = Spacing,
        GlowIntensity = GlowIntensity,
        Animation = Animation
    };

    /// <summary>Create a copy with a different color (RGB, alpha=1).</summary>
    public TextStyle WithColor(float r, float g, float b) =>
        WithColor(new Vector4(r, g, b, 1f));

    /// <summary>Create a copy with a different size.</summary>
    public TextStyle WithSize(float size) => new()
    {
        Color = Color,
        Size = size,
        Spacing = Spacing,
        GlowIntensity = GlowIntensity,
        Animation = Animation
    };

    /// <summary>Create a copy with a size multiplier.</summary>
    public TextStyle WithSizeMultiplier(float multiplier) =>
        WithSize(Size * multiplier);

    /// <summary>Create a copy with different spacing.</summary>
    public TextStyle WithSpacing(float spacing) => new()
    {
        Color = Color,
        Size = Size,
        Spacing = spacing,
        GlowIntensity = GlowIntensity,
        Animation = Animation
    };

    /// <summary>Create a copy with different glow intensity.</summary>
    public TextStyle WithGlow(float intensity) => new()
    {
        Color = Color,
        Size = Size,
        Spacing = Spacing,
        GlowIntensity = intensity,
        Animation = Animation
    };

    /// <summary>Create a copy with an animation.</summary>
    public TextStyle WithAnimation(TextAnimation animation) => new()
    {
        Color = Color,
        Size = Size,
        Spacing = Spacing,
        GlowIntensity = GlowIntensity,
        Animation = animation
    };

    /// <summary>Create a dimmed version of this style.</summary>
    public TextStyle Dimmed(float factor = 0.7f) => new()
    {
        Color = new Vector4(Color.X, Color.Y, Color.Z, Color.W * factor),
        Size = Size,
        Spacing = Spacing,
        GlowIntensity = GlowIntensity * factor,
        Animation = Animation
    };
}
