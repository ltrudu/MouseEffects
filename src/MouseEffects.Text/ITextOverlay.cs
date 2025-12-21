using System.Numerics;
using MouseEffects.Core.Rendering;
using MouseEffects.Text.Layout;
using MouseEffects.Text.Style;

namespace MouseEffects.Text;

/// <summary>
/// Interface for the centralized text overlay rendering system.
/// Provides a high-level API for effects to render text on top of their content.
/// </summary>
public interface ITextOverlay : IDisposable
{
    /// <summary>
    /// Begin a new text frame. Must be called before adding any text.
    /// Clears any text from the previous frame.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Add a text string at a position.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="position">Top-left position in screen coordinates.</param>
    /// <param name="style">Visual style for the text.</param>
    void AddText(string text, Vector2 position, TextStyle style);

    /// <summary>
    /// Add text with explicit horizontal alignment.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="anchorX">X position - meaning depends on alignment (left edge, center, or right edge).</param>
    /// <param name="centerY">Y center position of the text.</param>
    /// <param name="style">Visual style for the text.</param>
    /// <param name="alignment">Horizontal alignment relative to anchorX.</param>
    void AddTextAligned(string text, float anchorX, float centerY, TextStyle style, TextAlignment alignment);

    /// <summary>
    /// Add a text string centered at a position.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="centerPosition">Center position in screen coordinates.</param>
    /// <param name="style">Visual style for the text.</param>
    void AddTextCentered(string text, Vector2 centerPosition, TextStyle style);

    /// <summary>
    /// Add a number display at a position.
    /// </summary>
    /// <param name="value">The number to display.</param>
    /// <param name="position">Top-left position in screen coordinates.</param>
    /// <param name="style">Visual style for the number.</param>
    /// <param name="minDigits">Minimum digits (pads with leading zeros).</param>
    void AddNumber(int value, Vector2 position, TextStyle style, int minDigits = 0);

    /// <summary>
    /// Add a timer display (MM:SS format) at a position.
    /// </summary>
    /// <param name="remainingSeconds">Time remaining in seconds.</param>
    /// <param name="position">Top-left position in screen coordinates.</param>
    /// <param name="style">Visual style for the timer.</param>
    /// <param name="showMilliseconds">If true, shows SS.MS format instead.</param>
    void AddTimer(float remainingSeconds, Vector2 position, TextStyle style, bool showMilliseconds = false);

    /// <summary>
    /// Add a background panel (renders behind all text).
    /// </summary>
    /// <param name="center">Center position of the panel.</param>
    /// <param name="size">Width and height of the panel.</param>
    /// <param name="color">Panel color (RGBA, alpha for transparency).</param>
    /// <param name="cornerRadius">Rounded corner radius (0-1).</param>
    void AddBackground(Vector2 center, Vector2 size, Vector4 color, float cornerRadius = 0.1f);

    /// <summary>
    /// Create a fluent text builder for complex layouts.
    /// </summary>
    /// <returns>A new text builder instance.</returns>
    ITextBuilder CreateBuilder();

    /// <summary>
    /// End the text frame. Call after adding all text for this frame.
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Render all queued text. Called after the effect has finished rendering.
    /// </summary>
    /// <param name="context">The render context.</param>
    void Render(IRenderContext context);

    /// <summary>
    /// Maximum number of text entities available.
    /// </summary>
    int MaxEntities { get; }

    /// <summary>
    /// Number of entities used in the current frame.
    /// </summary>
    int UsedEntities { get; }

    /// <summary>
    /// Whether the overlay has been initialized and is ready to use.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Current time for animations (set by BeginFrame or externally).
    /// </summary>
    float Time { get; set; }
}
