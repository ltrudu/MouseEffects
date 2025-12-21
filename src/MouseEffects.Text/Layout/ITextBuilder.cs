using System.Numerics;
using MouseEffects.Text.Style;

namespace MouseEffects.Text.Layout;

/// <summary>
/// Fluent interface for building complex text layouts.
/// </summary>
public interface ITextBuilder
{
    /// <summary>
    /// Start a new panel at a position.
    /// </summary>
    /// <param name="position">Top-left position of the panel.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder Panel(Vector2 position);

    /// <summary>
    /// Add a background to the current panel.
    /// The background size will be calculated automatically based on content.
    /// </summary>
    /// <param name="color">Background color.</param>
    /// <param name="opacity">Background opacity (0-1).</param>
    /// <param name="padding">Padding around content in pixels.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder WithBackground(Vector4 color, float opacity = 0.7f, float padding = 10f);

    /// <summary>
    /// Add a label-value line (e.g., "SCORE     12345").
    /// </summary>
    /// <param name="label">The label text (left-aligned).</param>
    /// <param name="value">The value text (right-aligned).</param>
    /// <param name="labelStyle">Style for the label.</param>
    /// <param name="valueStyle">Style for the value.</param>
    /// <param name="minWidth">Minimum line width for alignment.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder Line(string label, string value, TextStyle labelStyle, TextStyle valueStyle, float minWidth = 300f);

    /// <summary>
    /// Add a label with numeric value line.
    /// </summary>
    /// <param name="label">The label text.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="labelStyle">Style for the label.</param>
    /// <param name="valueStyle">Style for the value.</param>
    /// <param name="minDigits">Minimum digits (pads with leading zeros).</param>
    /// <param name="minWidth">Minimum line width for alignment.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder Line(string label, int value, TextStyle labelStyle, TextStyle valueStyle, int minDigits = 0, float minWidth = 300f);

    /// <summary>
    /// Add a timer line (MM:SS format).
    /// </summary>
    /// <param name="label">The label text.</param>
    /// <param name="remainingSeconds">Time remaining in seconds.</param>
    /// <param name="labelStyle">Style for the label.</param>
    /// <param name="valueStyle">Style for the value.</param>
    /// <param name="minWidth">Minimum line width for alignment.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder TimerLine(string label, float remainingSeconds, TextStyle labelStyle, TextStyle valueStyle, float minWidth = 300f);

    /// <summary>
    /// Add a centered text line (spans full width).
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="style">Style for the text.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder CenteredText(string text, TextStyle style);

    /// <summary>
    /// Add a simple text line (left-aligned).
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="style">Style for the text.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder Text(string text, TextStyle style);

    /// <summary>
    /// Add vertical spacing.
    /// </summary>
    /// <param name="pixels">Space in pixels.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder Spacing(float pixels);

    /// <summary>
    /// Set the line spacing multiplier for subsequent lines.
    /// </summary>
    /// <param name="multiplier">Line height multiplier (1.0 = no extra space).</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder LineSpacing(float multiplier);

    /// <summary>
    /// Apply animation to the last added element.
    /// </summary>
    /// <param name="animation">The animation to apply.</param>
    /// <returns>This builder for chaining.</returns>
    ITextBuilder WithAnimation(TextAnimation animation);

    /// <summary>
    /// Build and add the layout to the overlay.
    /// </summary>
    void Build();
}
