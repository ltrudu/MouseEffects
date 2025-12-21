# MouseEffects.Text - Centralized Text Rendering System

A GPU-accelerated text overlay system for MouseEffects plugins.

## Overview

MouseEffects.Text provides a centralized text rendering system that plugins can use to display text overlays such as scores, timers, game over screens, and high score tables. The system uses a separate shader that renders on top of effect output.

## Quick Start

### 1. Add Project Reference

```xml
<ProjectReference Include="..\MouseEffects.Text\MouseEffects.Text.csproj" />
```

### 2. Create TextOverlay Instance

```csharp
using MouseEffects.Text;
using MouseEffects.Text.Style;

public class MyEffect : EffectBase
{
    private TextOverlay? _textOverlay;

    protected override void OnInitialize(IRenderContext context)
    {
        // Create and initialize text overlay
        _textOverlay = new TextOverlay();
        _textOverlay.Initialize(context);
    }
}
```

### 3. Render Text

```csharp
protected override void OnRender(IRenderContext context)
{
    // 1. Render your effect first
    RenderGameEntities(context);

    // 2. Begin text frame
    _textOverlay!.BeginFrame();
    _textOverlay.Time = (float)_gameTime.TotalSeconds;

    // 3. Add text elements
    _textOverlay.AddText("SCORE", new Vector2(50, 50), TextStyle.Label);
    _textOverlay.AddNumber(_score, new Vector2(150, 50), TextStyle.Score);

    // 4. End frame and render
    _textOverlay.EndFrame();
    _textOverlay.Render(context);
}
```

### 4. Dispose

```csharp
protected override void OnDispose()
{
    _textOverlay?.Dispose();
}
```

## API Reference

### TextStyle Presets

| Preset | Size | Color | Use Case |
|--------|------|-------|----------|
| `TextStyle.Default` | 32px | White | General text |
| `TextStyle.Title` | 64px | White, high glow | Titles, headers |
| `TextStyle.Label` | 24px | Gray | Labels, descriptions |
| `TextStyle.Score` | 48px | Green | Score display |
| `TextStyle.Timer` | 32px | Cyan | Countdown timers |
| `TextStyle.Warning` | 48px | Orange | Warning messages |
| `TextStyle.GameOver` | 80px | Red, pulsing | Game over text |
| `TextStyle.HighScoreTitle` | 48px | Gold | High score headers |
| `TextStyle.HighScoreEntry` | 32px | White | High score entries |
| `TextStyle.NewHighScore` | 36px | Rainbow | New high score highlight |

### Custom Styles

```csharp
// Create custom style
var myStyle = new TextStyle
{
    Color = new Vector4(1f, 0.5f, 0f, 1f), // Orange
    Size = 40f,
    GlowIntensity = 1.5f,
    Spacing = 0.8f
};

// Or modify a preset
var customScore = TextStyle.Score.WithColor(new Vector4(1f, 1f, 0f, 1f));
var dimmedLabel = TextStyle.Label.Dimmed(0.5f);
```

### Animations

```csharp
// Apply animations to styles
var pulsingText = TextStyle.GameOver.WithAnimation(TextAnimation.Pulse());
var waveText = TextStyle.Title.WithAnimation(TextAnimation.Wave(speed: 3f, intensity: 8f));
var rainbowText = TextStyle.NewHighScore.WithAnimation(TextAnimation.Rainbow());
var breathingText = TextStyle.Warning.WithAnimation(TextAnimation.Breathing());
var shakingText = TextStyle.GameOver.WithAnimation(TextAnimation.Shake(intensity: 3f));
```

### TextBuilder Fluent API

For complex layouts, use the TextBuilder:

```csharp
_textOverlay.CreateBuilder()
    .Panel(new Vector2(50f, 50f))
    .WithBackground(new Vector4(0.05f, 0.05f, 0.1f, 1f), opacity: 0.7f, padding: 15f)
    .Line("SCORE", _score.ToString(), TextStyle.Label, TextStyle.Score)
    .Line("LIVES", _lives.ToString(), TextStyle.Label, TextStyle.Score)
    .TimerLine("TIME", _remainingTime, TextStyle.Label, TextStyle.Timer)
    .Spacing(10f)
    .CenteredText("LEVEL 1", TextStyle.Title)
    .Build();
```

### Direct Methods

```csharp
// Simple text (left-aligned from position)
_textOverlay.AddText("HELLO", position, style);

// Centered text
_textOverlay.AddTextCentered("GAME OVER", screenCenter, TextStyle.GameOver);

// Numbers with optional leading zeros
_textOverlay.AddNumber(42, position, TextStyle.Score);
_textOverlay.AddNumber(5, position, TextStyle.Score, minDigits: 3); // "005"

// Timer (MM:SS or SS.ms format)
_textOverlay.AddTimer(125.5f, position, TextStyle.Timer); // "02:05"
_textOverlay.AddTimer(9.75f, position, TextStyle.Timer, showMilliseconds: true); // "09.75"

// Background panel
_textOverlay.AddBackground(center, size, backgroundColor);
```

## Complete Example

```csharp
public class GameEffect : EffectBase
{
    private TextOverlay? _textOverlay;
    private int _score = 0;
    private int _lives = 3;
    private float _gameTime = 0f;
    private bool _isGameOver = false;

    protected override void OnInitialize(IRenderContext context)
    {
        _textOverlay = new TextOverlay();
        _textOverlay.Initialize(context);
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _gameTime += (float)gameTime.ElapsedTime.TotalSeconds;
        // Game logic...
    }

    protected override void OnRender(IRenderContext context)
    {
        // Render game entities first
        RenderGame(context);

        // Render text overlay on top
        var text = _textOverlay!;
        text.BeginFrame();
        text.Time = _gameTime;

        // Score panel in top-left
        text.CreateBuilder()
            .Panel(new Vector2(50f, 50f))
            .WithBackground(new Vector4(0f, 0f, 0f, 1f), 0.6f, 12f)
            .Line("SCORE", _score.ToString(), TextStyle.Label, TextStyle.Score, minWidth: 200f)
            .Line("LIVES", _lives.ToString(), TextStyle.Label, TextStyle.Score, minWidth: 200f)
            .Build();

        // Game over overlay
        if (_isGameOver)
        {
            var center = context.ViewportSize / 2f;
            text.AddTextCentered("GAME OVER", center,
                TextStyle.GameOver.WithAnimation(TextAnimation.Pulse()));
            text.AddTextCentered("PRESS SPACE TO RESTART",
                center + new Vector2(0, 100), TextStyle.Label);
        }

        text.EndFrame();
        text.Render(context);
    }

    protected override void OnDispose()
    {
        _textOverlay?.Dispose();
    }
}
```

## Supported Characters

- **Digits**: 0-9
- **Letters**: A-Z (uppercase only)
- **Punctuation**: `:` (colon), `/` (slash), `.` (dot), `-` (dash)
- **Space**: Advances position without rendering

## Performance Notes

- Maximum 2000 entities per frame (configurable in constructor)
- GPU instanced rendering - all characters drawn in a single draw call
- Shader-based glow and animations - no CPU overhead
- Call `BeginFrame()` once per frame to reset entity count
