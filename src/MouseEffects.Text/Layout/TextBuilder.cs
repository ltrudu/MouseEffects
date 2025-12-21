using System.Numerics;
using MouseEffects.Text.Style;

namespace MouseEffects.Text.Layout;

/// <summary>
/// Fluent builder for creating complex text layouts.
/// </summary>
public sealed class TextBuilder : ITextBuilder
{
    private readonly TextOverlay _overlay;
    private Vector2 _panelPosition;
    private float _currentY;
    private float _lineSpacing = 2.0f;
    private float _panelWidth;

    // Background settings
    private bool _hasBackground;
    private Vector4 _backgroundColor;
    private float _backgroundPadding;

    // Track content bounds for auto-sizing background
    private float _contentMinX = float.MaxValue;
    private float _contentMaxX = float.MinValue;
    private float _contentMinY = float.MaxValue;
    private float _contentMaxY = float.MinValue;

    // Last added element for applying animations
    private int _lastElementStartIndex;
    private int _lastElementEndIndex;

    internal TextBuilder(TextOverlay overlay)
    {
        _overlay = overlay;
        _lastElementStartIndex = _overlay.UsedEntities;
        _lastElementEndIndex = _overlay.UsedEntities;
    }

    public ITextBuilder Panel(Vector2 position)
    {
        _panelPosition = position;
        _currentY = position.Y;
        _panelWidth = 0f;

        // Reset bounds tracking
        _contentMinX = float.MaxValue;
        _contentMaxX = float.MinValue;
        _contentMinY = float.MaxValue;
        _contentMaxY = float.MinValue;

        return this;
    }

    public ITextBuilder WithBackground(Vector4 color, float opacity = 0.7f, float padding = 10f)
    {
        _hasBackground = true;
        _backgroundColor = new Vector4(color.X, color.Y, color.Z, color.W * opacity);
        _backgroundPadding = padding;
        return this;
    }

    public ITextBuilder Line(string label, string value, TextStyle labelStyle, TextStyle valueStyle, float minWidth = 300f)
    {
        _lastElementStartIndex = _overlay.UsedEntities;

        float lineHeight = Math.Max(labelStyle.Size, valueStyle.Size);
        float y = _currentY + lineHeight / 2; // Center Y for this line

        // Calculate actual widths needed
        float labelWidth = _overlay.CalculateTextWidth(label, labelStyle);
        float valueWidth = _overlay.CalculateTextWidth(value, valueStyle);

        // Minimum gap between label and value (based on larger style size)
        float gap = Math.Max(labelStyle.Size, valueStyle.Size) * 0.5f;

        // Calculate required width to fit both without overlap
        float requiredWidth = labelWidth + gap + valueWidth;
        float actualWidth = Math.Max(minWidth, requiredWidth);

        // Define column boundaries
        float leftColumnStart = _panelPosition.X;
        float rightColumnEnd = _panelPosition.X + actualWidth;

        // Render label: LEFT aligned in left column
        _overlay.AddTextAligned(label, leftColumnStart, y, labelStyle, TextAlignment.Left);

        // Render value: RIGHT aligned in right column
        _overlay.AddTextAligned(value, rightColumnEnd, y, valueStyle, TextAlignment.Right);

        // Update tracking
        UpdateBounds(leftColumnStart, _currentY, rightColumnEnd, _currentY + lineHeight);
        _panelWidth = Math.Max(_panelWidth, actualWidth);
        _currentY += lineHeight * _lineSpacing;

        _lastElementEndIndex = _overlay.UsedEntities;
        return this;
    }

    public ITextBuilder Line(string label, int value, TextStyle labelStyle, TextStyle valueStyle, int minDigits = 0, float minWidth = 300f)
    {
        string valueStr = minDigits > 0 ? value.ToString($"D{minDigits}") : value.ToString();
        return Line(label, valueStr, labelStyle, valueStyle, minWidth);
    }

    public ITextBuilder TimerLine(string label, float remainingSeconds, TextStyle labelStyle, TextStyle valueStyle, float minWidth = 300f)
    {
        remainingSeconds = Math.Max(0f, remainingSeconds);
        int totalSeconds = (int)remainingSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        string timerStr = $"{minutes:D2}:{seconds:D2}";

        return Line(label, timerStr, labelStyle, valueStyle, minWidth);
    }

    public ITextBuilder CenteredText(string text, TextStyle style)
    {
        _lastElementStartIndex = _overlay.UsedEntities;

        float textWidth = _overlay.CalculateTextWidth(text, style);

        // If no width established yet, use text width
        if (_panelWidth == 0)
        {
            _panelWidth = textWidth + _backgroundPadding * 2;
        }

        float centerX = _panelPosition.X + _panelWidth / 2;
        float y = _currentY + style.Size / 2;

        _overlay.AddTextAligned(text, centerX, y, style, TextAlignment.Center);

        // Update tracking
        float x = centerX - textWidth / 2;
        UpdateBounds(x, _currentY, x + textWidth, _currentY + style.Size);
        _currentY += style.Size * _lineSpacing;

        _lastElementEndIndex = _overlay.UsedEntities;
        return this;
    }

    public ITextBuilder Text(string text, TextStyle style)
    {
        _lastElementStartIndex = _overlay.UsedEntities;

        float x = _panelPosition.X;
        float y = _currentY + style.Size / 2;

        _overlay.AddTextAligned(text, x, y, style, TextAlignment.Left);

        float textWidth = _overlay.CalculateTextWidth(text, style);
        UpdateBounds(x, _currentY, x + textWidth, _currentY + style.Size);
        _panelWidth = Math.Max(_panelWidth, textWidth);
        _currentY += style.Size * _lineSpacing;

        _lastElementEndIndex = _overlay.UsedEntities;
        return this;
    }

    public ITextBuilder Spacing(float pixels)
    {
        _currentY += pixels;
        return this;
    }

    public ITextBuilder LineSpacing(float multiplier)
    {
        _lineSpacing = multiplier;
        return this;
    }

    public ITextBuilder WithAnimation(TextAnimation animation)
    {
        // This would require modifying already-added entities
        // For now, this is a placeholder - animations should be set in TextStyle
        return this;
    }

    public void Build()
    {
        if (_hasBackground && _contentMinX != float.MaxValue)
        {
            // Calculate background size from content bounds
            float padding = _backgroundPadding;
            float bgWidth = (_contentMaxX - _contentMinX) + padding * 2;
            float bgHeight = (_contentMaxY - _contentMinY) + padding * 2;
            float bgCenterX = (_contentMinX + _contentMaxX) / 2;
            float bgCenterY = (_contentMinY + _contentMaxY) / 2;

            // Insert background at index 0 so it renders first (behind all text)
            _overlay.InsertBackgroundAtFront(
                new Vector2(bgCenterX, bgCenterY),
                new Vector2(bgWidth, bgHeight),
                _backgroundColor
            );
        }
    }

    private void UpdateBounds(float minX, float minY, float maxX, float maxY)
    {
        _contentMinX = Math.Min(_contentMinX, minX);
        _contentMaxX = Math.Max(_contentMaxX, maxX);
        _contentMinY = Math.Min(_contentMinY, minY);
        _contentMaxY = Math.Max(_contentMaxY, maxY);
    }
}
