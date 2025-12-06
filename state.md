# MouseEffects Application State

## Project Overview

MouseEffects is a GPU-accelerated visual effects overlay for Windows that creates cursor effects in real-time using DirectX 11. It features a plugin architecture allowing custom effects to be added without modifying the core application. The overlay is transparent and click-through, rendering effects on top of all other windows.

**Key Technologies:**
- .NET 8.0 with WPF
- DirectX 11 for GPU-accelerated rendering
- HLSL shaders for visual effects
- ModernWPF for Fluent Design UI
- Plugin discovery via reflection

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        MouseEffects.App                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ MainWindow  │  │EffectManager│  │    PluginSettings       │  │
│  │   (WPF UI)  │  │             │  │ (JSON serialization)    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│MouseEffects   │    │MouseEffects   │    │MouseEffects   │
│   .Overlay    │    │   .DirectX    │    │   .Input      │
│               │    │               │    │               │
│ Transparent   │    │ DX11 Context  │    │ Global Mouse  │
│ Click-through │    │ Shader Mgmt   │    │    Hook       │
│   Window      │    │ Buffer Mgmt   │    │               │
└───────────────┘    └───────────────┘    └───────────────┘
        │                     │                     │
        └─────────────────────┼─────────────────────┘
                              ▼
                    ┌───────────────┐
                    │MouseEffects   │
                    │    .Core      │
                    │               │
                    │  Interfaces   │
                    │  Base Classes │
                    │  Data Types   │
                    └───────────────┘
                              ▲
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   Plugin 1    │    │   Plugin 2    │    │   Plugin N    │
│ (ParticleTrail│    │  (Invaders)   │    │    (...)      │
└───────────────┘    └───────────────┘    └───────────────┘
```

## Project Structure

```
MouseEffects/
├── src/
│   ├── MouseEffects.Core/           # Core interfaces and base classes
│   │   ├── Effects/
│   │   │   ├── IEffect.cs           # Main effect interface
│   │   │   ├── EffectBase.cs        # Abstract base implementation
│   │   │   ├── IEffectFactory.cs    # Factory interface for plugins
│   │   │   ├── EffectConfiguration.cs # Key-value config with type conversion
│   │   │   └── IHotkeyProvider.cs   # Interface for plugin hotkeys
│   │   ├── Input/
│   │   │   └── MouseState.cs        # Mouse position, velocity, buttons
│   │   ├── Rendering/
│   │   │   ├── IRenderContext.cs    # GPU rendering abstraction
│   │   │   └── BlendMode.cs         # Alpha, Additive, etc.
│   │   ├── Time/
│   │   │   └── GameTime.cs          # Delta time, total time
│   │   └── Diagnostics/
│   │       └── Logger.cs            # Centralized logging
│   │
│   ├── MouseEffects.DirectX/        # DirectX 11 implementation
│   │   ├── DX11RenderContext.cs     # IRenderContext implementation
│   │   ├── ShaderCompiler.cs        # HLSL compilation
│   │   └── BufferManager.cs         # GPU buffer management
│   │
│   ├── MouseEffects.Input/          # Input handling
│   │   ├── GlobalMouseHook.cs       # Low-level mouse hook
│   │   └── MouseTracker.cs          # Position/velocity tracking
│   │
│   ├── MouseEffects.Overlay/        # Overlay window
│   │   └── OverlayWindow.cs         # Transparent, topmost, click-through
│   │
│   ├── MouseEffects.Plugins/        # Plugin system
│   │   └── PluginLoader.cs          # Assembly scanning, factory discovery
│   │
│   └── MouseEffects.App/            # Main WPF application
│       ├── App.xaml                 # Application entry, theming
│       ├── MainWindow.xaml          # Settings UI
│       ├── EffectManager.cs         # Effect lifecycle management
│       ├── GameLoop.cs              # Update/Render loop (60 FPS)
│       ├── Program.cs               # Entry point, hotkey polling
│       └── Settings/
│           ├── AppSettings.cs       # Global app settings
│           └── PluginSettings.cs    # Per-plugin JSON storage
│
├── plugins/                         # Effect plugins (auto-discovered)
│   ├── MouseEffects.Effects.ParticleTrail/
│   ├── MouseEffects.Effects.LaserWork/
│   ├── MouseEffects.Effects.ScreenDistortion/
│   ├── MouseEffects.Effects.ColorBlindness/
│   ├── MouseEffects.Effects.ColorBlindnessNG/  # NEW: Next-gen CVD simulation & correction
│   ├── MouseEffects.Effects.RadialDithering/
│   ├── MouseEffects.Effects.TileVibration/
│   ├── MouseEffects.Effects.WaterRipple/
│   ├── MouseEffects.Effects.Zoom/
│   ├── MouseEffects.Effects.Invaders/
│   └── MouseEffects.Effects.Firework/
│
├── packaging/                       # MSIX packaging for Windows Store
│   └── MouseEffects.Package/
│
└── Wiki/                            # Documentation
    ├── Plugins.md
    ├── Features.md
    ├── Plugin-Development.md
    └── Plugin-ScreenCapture.md
```

## Core Interfaces

### IEffect (MouseEffects.Core/Effects/IEffect.cs)

The main interface that all effects must implement:

```csharp
public interface IEffect : IDisposable
{
    Guid InstanceId { get; }
    EffectMetadata Metadata { get; }
    EffectConfiguration Configuration { get; }
    int RenderOrder { get; }
    bool IsComplete { get; }
    bool IsEnabled { get; set; }
    bool IsInitialized { get; }
    bool RequiresContinuousScreenCapture { get; }  // For screen-reading effects

    void Initialize(IRenderContext context);
    void Configure(EffectConfiguration config);
    void Update(GameTime gameTime, MouseState mouseState);
    void Render(IRenderContext context);
    void OnViewportSizeChanged(int width, int height);
}
```

### EffectBase (MouseEffects.Core/Effects/EffectBase.cs)

Abstract base class that handles common boilerplate:

```csharp
public abstract class EffectBase : IEffect
{
    protected IRenderContext? Context { get; private set; }
    protected int _viewportWidth, _viewportHeight;

    // Override these in your effect:
    protected abstract void OnInitialize(IRenderContext context);
    protected abstract void OnUpdate(GameTime gameTime, MouseState mouseState);
    protected abstract void OnRender(IRenderContext context);
    protected virtual void OnConfigurationChanged() { }
    protected virtual void OnViewportSizeChanged(int w, int h) { }
    protected virtual void OnDispose() { }
}
```

### IEffectFactory (MouseEffects.Core/Effects/IEffectFactory.cs)

Factory interface for plugin discovery:

```csharp
public interface IEffectFactory
{
    EffectMetadata Metadata { get; }
    IEffect Create();
    EffectConfiguration GetDefaultConfiguration();
    EffectConfigurationSchema GetConfigurationSchema();
    object? CreateSettingsControl(IEffect effect);  // WPF UserControl
}
```

### EffectConfiguration (MouseEffects.Core/Effects/EffectConfiguration.cs)

Type-safe configuration storage with automatic numeric type conversion:

```csharp
public class EffectConfiguration
{
    public T Get<T>(string key, T defaultValue = default!);
    public void Set<T>(string key, T value);
    public bool TryGet<T>(string key, out T value);  // Handles int→float conversion
    public IReadOnlyDictionary<string, object> GetAll();
    public EffectConfiguration Clone();
}
```

**Important**: The `TryGet<T>` method handles numeric type conversions automatically. JSON may deserialize `30` as `int`, but `TryGet<float>` will still work via `Convert.ToSingle()`.

### IHotkeyProvider (MouseEffects.Core/Effects/IHotkeyProvider.cs)

Interface for plugins that want to register global hotkeys:

```csharp
public interface IHotkeyProvider
{
    IEnumerable<HotkeyDefinition> GetHotkeys();
}

public record HotkeyDefinition
{
    public string Id { get; init; }
    public string DisplayName { get; init; }
    public HotkeyModifiers Modifiers { get; init; }  // Ctrl, Shift, Alt (flags)
    public HotkeyKey Key { get; init; }              // A-Z, 0-9, F1-F12, etc.
    public bool IsEnabled { get; init; }
    public Action Callback { get; init; }
}
```

### MouseState (MouseEffects.Core/Input/MouseState.cs)

Mouse input data passed to effects each frame:

```csharp
public readonly struct MouseState
{
    public Vector2 Position { get; }
    public Vector2 PreviousPosition { get; }
    public Vector2 Velocity { get; }
    public int ScrollDelta { get; }
    public MouseButtons ButtonsDown { get; }     // Currently held
    public MouseButtons ButtonsPressed { get; }  // Just pressed this frame
    public MouseButtons ButtonsReleased { get; } // Just released this frame
}

[Flags]
public enum MouseButtons { None, Left, Right, Middle, X1, X2 }
```

### IRenderContext (MouseEffects.Core/Rendering/IRenderContext.cs)

GPU rendering abstraction (implemented by DX11RenderContext):

```csharp
public interface IRenderContext
{
    Vector2 ViewportSize { get; }
    IShaderResourceView? ScreenTexture { get; }  // For screen capture effects

    IShader CompileShader(ShaderType type, string source, string entryPoint);
    IBuffer CreateBuffer<T>(BufferType type, T[] data);
    IBuffer CreateBuffer(BufferType type, int sizeBytes);
    ISamplerState CreateSamplerState(SamplerDescription desc);

    void UpdateBuffer<T>(IBuffer buffer, ReadOnlySpan<T> data);
    void SetVertexShader(IShader shader);
    void SetPixelShader(IShader shader);
    void SetConstantBuffer(ShaderStage stage, int slot, IBuffer buffer);
    void SetShaderResource(ShaderStage stage, int slot, IBuffer buffer);
    void SetBlendState(BlendMode mode);
    void SetPrimitiveTopology(PrimitiveTopology topology);

    void Draw(int vertexCount, int startVertex);
    void DrawInstanced(int vertexCount, int instanceCount, int startVertex, int startInstance);
}
```

## Plugin Development Guide

### Project File (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <Platforms>x64</Platforms>
    <OutputPath>..\..\src\MouseEffects.App\bin\$(Platform)\$(Configuration)\$(TargetFramework)\plugins\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MouseEffects.Core\MouseEffects.Core.csproj" />
    <EmbeddedResource Include="Shaders\*.hlsl" />
  </ItemGroup>
</Project>
```

### Effect Class Structure

```csharp
public sealed class MyEffect : EffectBase
{
    // 1. Metadata
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "my-effect",
        Name = "My Effect",
        Description = "Does something cool",
        Author = "You",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    // 2. GPU Resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _constantBuffer;

    // 3. Cached Configuration
    private float _intensity = 1.0f;
    private Vector4 _color = new(1, 1, 1, 1);

    public override EffectMetadata Metadata => _metadata;

    // 4. Initialize GPU resources
    protected override void OnInitialize(IRenderContext context)
    {
        var shaderCode = LoadEmbeddedShader("MyShader.hlsl");
        _vertexShader = context.CompileShader(ShaderType.Vertex, shaderCode, "VSMain");
        _pixelShader = context.CompileShader(ShaderType.Pixel, shaderCode, "PSMain");
        _constantBuffer = context.CreateBuffer(BufferType.Constant, 64);
    }

    // 5. Read configuration
    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("intensity", out float i)) _intensity = i;
        if (Configuration.TryGet("color", out Vector4 c)) _color = c;
    }

    // 6. Update state
    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        // Update animations, physics, etc.
    }

    // 7. Render
    protected override void OnRender(IRenderContext context)
    {
        // Update constant buffer, set shaders, draw
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.DrawInstanced(6, instanceCount, 0, 0);
    }

    // 8. Cleanup
    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(MyEffect).Assembly;
        using var stream = assembly.GetManifestResourceStream($"Namespace.Shaders.{name}");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}
```

### Constant Buffer Alignment

DirectX requires constant buffers to be 16-byte aligned:

```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]  // Must be multiple of 16
private struct ShaderParams
{
    public Vector2 MousePos;      // 8 bytes
    public Vector2 ViewportSize;  // 8 bytes (total: 16)
    public Vector4 Color;         // 16 bytes (total: 32)
    public float Intensity;       // 4 bytes
    public float Time;            // 4 bytes
    public float Padding1;        // 4 bytes
    public float Padding2;        // 4 bytes (total: 48)
    public Vector4 ExtraData;     // 16 bytes (total: 64)
}
```

### Settings Control Pattern

```csharp
public partial class MySettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;

    public event Action<string>? SettingsChanged;

    public MySettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        if (_effect.Configuration.TryGet("intensity", out float i))
            IntensitySlider.Value = i;
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("intensity", (float)IntensitySlider.Value);
        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void Slider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        => UpdateConfiguration();
}
```

## Plugins Reference

### 1. ParticleTrail

**ID**: `particle-trail`
**Category**: Visual
**Description**: Colorful particle trails following the cursor

**Features:**
- Multiple particle colors with rainbow mode
- Adjustable particle count, size, and lifespan
- Velocity-based emission rate
- Glow and bloom effects

### 2. LaserWork

**ID**: `laser-work`
**Category**: Visual
**Description**: Glowing laser beams from cursor

**Features:**
- Multiple beam modes (straight, spread, spiral)
- Customizable colors and glow intensity
- Beam length and width controls
- Click-activated or continuous modes

### 3. ScreenDistortion

**ID**: `screen-distortion`
**Category**: Screen Capture
**Description**: Real-time screen warping effects

**Features:**
- Requires `RequiresContinuousScreenCapture = true`
- Multiple distortion modes (ripple, swirl, bulge)
- Adjustable strength and radius
- Smooth falloff at edges

### 4. ColorBlindness

**ID**: `color-blindness`
**Category**: Accessibility
**Description**: Color vision simulation and correction

**Features:**
- Filter types: None, Protanopia, Deuteranopia, Tritanopia, Grayscale, Inverted
- Shape modes: Circle, Rectangle, Fullscreen
- **Dual filter mode**: Different filters inside/outside shape
- Adjustable shape size and border
- Shader applies color transformation matrices

**Configuration:**
- `filterType` (int): Inside filter or fullscreen filter
- `outsideFilterType` (int): Outside filter (shape modes only)
- `shapeMode` (int): 0=Circle, 1=Rectangle, 2=Fullscreen
- `circleRadius`, `rectangleWidth`, `rectangleHeight`

### 4b. ColorBlindnessNG (NEW)

**ID**: `color-blindness-ng`
**Category**: Accessibility
**Description**: Next-generation CVD simulation and correction with LUT-based color remapping

**Key Differences from ColorBlindness:**
- Separates Simulation and Correction into distinct operating modes
- Uses verified Machado (2009) matrices for simulation
- Implements LUT-based color remapping for correction (instead of matrix-based)
- Per-channel color gradient control for fine-tuned correction

**Operating Modes:**

1. **Simulation Mode** - Shows what CVD people see
   - Algorithm: Machado (RGB-space) or Strict (LMS-space)
   - 9 CVD types: None, Protanopia, Protanomaly, Deuteranopia, Deuteranomaly, Tritanopia, Tritanomaly, Achromatopsia, Achromatomaly

2. **Correction Mode** - Helps CVD users see colors better
   - LUT-based color remapping with 256-entry gradients per channel
   - 9 presets: Custom, Deuteranopia, Protanopia, Tritanopia, Deuteranomaly, Protanomaly, Tritanomaly, Red-Green (Both), High Contrast
   - Per-channel controls: Enable, Strength (0-1), Start Color, End Color
   - Application modes: Full Channel, Dominant Only, Threshold
   - Gradient interpolation: Linear RGB, Perceptual LAB, HSL

**Configuration:**
- `mode` (int): 0=Simulation, 1=Correction
- `simulationAlgorithm` (int): 0=Machado, 1=Strict
- `simulationFilterType` (int): CVD type for simulation
- `applicationMode` (int): 0=Full Channel, 1=Dominant Only, 2=Threshold
- `gradientType` (int): 0=Linear RGB, 1=Perceptual LAB, 2=HSL
- `threshold` (float): For threshold mode (0-1)
- `redEnabled`, `greenEnabled`, `blueEnabled` (bool): Channel enables
- `redStrength`, `greenStrength`, `blueStrength` (float): Channel strengths
- `redStartColor`, `redEndColor`, etc. (string): Hex colors like "#FF0000"
- `intensity` (float): Global intensity (0-1)

**Machado (2009) Matrices:**
```hlsl
// Deuteranopia (100% M-cone loss)
float3x3(0.625, 0.375, 0.000,
         0.700, 0.300, 0.000,
         0.000, 0.300, 0.700)

// Protanopia (100% L-cone loss)
float3x3(0.567, 0.433, 0.000,
         0.558, 0.442, 0.000,
         0.000, 0.242, 0.758)

// Tritanopia (100% S-cone loss)
float3x3(0.950, 0.050, 0.000,
         0.000, 0.433, 0.567,
         0.000, 0.475, 0.525)
```

**Files:**
- `ColorBlindnessNGEffect.cs` - Main effect with LUT texture management
- `ColorBlindnessNGFactory.cs` - Plugin factory
- `LUTGenerator.cs` - Generates gradient LUT textures (Linear RGB, LAB, HSL)
- `CorrectionPresets.cs` - 9 preset definitions
- `Shaders/ColorBlindnessNG.hlsl` - Combined simulation + correction shader
- `UI/ColorBlindnessNGSettingsControl.xaml(.cs)` - Dynamic WPF settings UI

### 5. RadialDithering

**ID**: `radial-dithering`
**Category**: Visual
**Description**: Retro dithering effect around cursor

**Features:**
- Multiple dithering patterns (Bayer, noise, ordered)
- Adjustable dither size and intensity
- Color palette reduction
- Radial falloff from cursor

### 6. TileVibration

**ID**: `tile-vibration`
**Category**: Visual
**Description**: Screen divided into vibrating tiles

**Features:**
- Grid-based tile system
- Proximity-based vibration intensity
- Adjustable tile size and gap
- Color tinting options

### 7. WaterRipple

**ID**: `water-ripple`
**Category**: Screen Capture
**Description**: Realistic water ripple effects

**Features:**
- Physics-based wave propagation
- Click to create ripples
- Adjustable wave speed and damping
- Refraction-based distortion

### 8. Zoom

**ID**: `zoom`
**Category**: Utility
**Description**: Magnifying lens effect at cursor

**Features:**
- Circle or rectangle shape
- Zoom factor: 1.1x to 5.0x
- Adjustable size (radius 20-500px, width/height 40-800px)
- Optional border with customizable color
- Hotkeys:
  - Shift+Ctrl+Wheel: Adjust zoom factor
  - Shift+Alt+Wheel: Adjust size
- `ConfigurationChangedByHotkey` event for UI sync

**Configuration:**
- `zoomFactor` (float): 1.1 to 5.0
- `shapeType` (int): 0=Circle, 1=Rectangle
- `circleRadius`, `rectangleWidth`, `rectangleHeight`
- `borderWidth`, `borderColor`
- `enableWheelZoom`, `enableWheelSize`

### 9. Space Invaders

**ID**: `invaders`
**Category**: Interactive
**Description**: Defend against neon space invaders with rockets from your cursor

**Features:**
- Classic arcade gameplay with modern neon visuals
- Three invader types (Squid, Crab, Octopus) with different point values
- Rockets launched on click or mouse movement
- Timer-based gameplay (default 90 seconds)
- Real-time score, PPM (points per minute), and countdown display

**Game Over Mechanics:**
- "TOUCHED" - Mouse cursor touches an invader
- "INVADED" - Invader reaches bottom of screen
- Animated "GAME OVER" text with pulsing glow and wave animation
- Reason displayed below in orange

**High Scores System (NEW):**
- Stores top 5 scores as Points Per Minute (PPM) with dates
- Displayed when game ends successfully (timer runs out)
- New high score: Rainbow cycling colors with breathing animation
- Old scores: Neon blue with subtle pulsing
- Default scores: 2000, 1500, 1000, 500, 200 PPM (dated 04/12/2025)
- Stored in `highScoresJson` config key (not in settings UI)

**Hotkey Support:**
- Implements `IHotkeyProvider`
- Ctrl+Shift+I: Reset game (when enabled)
- Toggle in settings: "Reset Hotkey (Ctrl+Shift+I)"

**Configuration:**
- Rocket: speed, size, rainbow mode, spawn triggers
- Invaders: spawn rate, speed range, sizes, colors, descent speed
- Explosions: particle count, force, lifespan, glow
- Scoring: points per invader type, overlay position/size/color
- Timer: `timerDuration` (30-300 seconds)
- High scores: `highScoresJson` (JSON array, internal use)

### 10. Firework

**ID**: `firework`
**Category**: Visual
**Description**: Click-triggered firework explosions

**Features:**
- Particle-based firework physics
- Multiple explosion patterns
- Customizable colors and trail effects
- Gravity and wind simulation

## Configuration Storage

Settings are stored per-plugin in JSON format:

**Location**: `%APPDATA%\MouseEffects\plugins\{plugin-id}.json`

**Format:**
```json
{
  "IsEnabled": true,
  "Configuration": {
    "intensity": 1.5,
    "color": { "X": 1.0, "Y": 0.5, "Z": 0.0, "W": 1.0 },
    "timerDuration": 30
  }
}
```

**Type Handling:**
- Numbers without decimals are deserialized as `int`
- `EffectConfiguration.TryGet<float>` handles int→float conversion automatically
- Vector4 (colors) serialized as `{ "X", "Y", "Z", "W" }` objects

## UI Theming

Uses [ModernWPF](https://github.com/Kinnara/ModernWpf) for Fluent Design:

**Theme Options:**
- System (follows Windows setting)
- Light
- Dark (default)

**Implementation:**
```csharp
// AppSettings.cs
public AppTheme Theme { get; set; } = AppTheme.Dark;

// App.xaml.cs
public static void ApplyTheme(AppTheme theme)
{
    ThemeManager.Current.ApplicationTheme = theme switch
    {
        AppTheme.Light => ApplicationTheme.Light,
        AppTheme.Dark => ApplicationTheme.Dark,
        _ => null  // System
    };
}
```

## Build Commands

```bash
# Build single plugin
dotnet build "plugins\MouseEffects.Effects.Invaders\MouseEffects.Effects.Invaders.csproj" -c Debug -p:Platform=x64

# Build entire solution
dotnet build -c Release -p:Platform=x64

# Add new plugin to solution
dotnet sln add "plugins\NewPlugin\NewPlugin.csproj" --solution-folder "plugins"

# Clean and rebuild
dotnet clean && dotnet build -c Release -p:Platform=x64
```

**IMPORTANT**: Always specify `-p:Platform=x64` for Release builds. Without it, plugins may build to `bin\AnyCPU\` and won't be discovered.

## Recent Bug Fixes

### Numeric Type Conversion in Configuration (2025-12-05)

**Problem**: JSON deserializes whole numbers (e.g., `30`) as `int`, but `TryGet<float>` expected exact type match. This caused saved float settings to be silently ignored.

**Symptoms:**
- Timer duration set to 30s but effect used default 90s
- Any float setting saved as whole number wouldn't load

**Fix**: Added automatic numeric type conversion in `EffectConfiguration.TryGet<T>`:
```csharp
if (typeof(T) == typeof(float) && obj is IConvertible)
{
    value = (T)(object)Convert.ToSingle(obj);
    return true;
}
```

**File**: `MouseEffects.Core/Effects/EffectConfiguration.cs`

## Current Branch

`master`

## Git Status

Modified files pending commit. Recent commits:
- fced7b8 Fixed float error in settings
- df01858 Updated Invaders scoring layout
- 3179c5b Added hotkey to reset invader game, game over mechanics
- 604315a Changed default settings
- 836c2aa Added more colors to invaders and default setup

## Documentation

- `README.md` - Project overview, effects list, installation
- `Wiki/Plugins.md` - Detailed plugin reference with all settings
- `Wiki/Features.md` - Features overview including theming
- `Wiki/Plugin-Development.md` - How to create plugins
- `Wiki/Plugin-ScreenCapture.md` - Screen capture plugin guide

---

# Session State: 2025-12-06

## Current Session Summary

### Completed Tasks

1. **Added Correction/Simulation Mode Toggle to ColorBlindness Plugin**
   - Users can now choose between "Correction" (helps colorblind users) and "Simulation" (shows what they see)
   - Default mode is Correction
   - Each zone can have independent mode settings

2. **Fixed CVD Simulation and Correction Algorithms**
   - Replaced broken LMS-based simulation (had incorrect coefficients that produced negative values for neutral white)
   - Implemented Machado et al. (2009) research matrices for accurate CVD simulation
   - Matrices work directly in linear RGB space - scientifically validated and well-tested
   - Added matrices for all 6 CVD types:
     - Protanopia (100% L-cone deficiency)
     - Protanomaly (50% L-cone weakness)
     - Deuteranopia (100% M-cone deficiency)
     - Deuteranomaly (50% M-cone weakness)
     - Tritanopia (100% S-cone deficiency)
     - Tritanomaly (50% S-cone weakness)

3. **Updated Daltonization (Correction) Algorithm**
   - Now uses accurate Machado simulation to calculate color error
   - Properly redistributes lost color information:
     - Protan/Deutan: shifts red-green error to blue channel
     - Tritan: shifts blue error to red/green channels

### Files Modified This Session

1. **ColorBlindness.hlsl** (`plugins/MouseEffects.Effects.ColorBlindness/Shaders/`)
   - Added `SimulationMode` to constant buffer for each zone
   - Added Machado (2009) simulation matrices as static constants
   - Added `SimulateCVD_RGB()` function using Machado matrices
   - Updated `ApplyLMSCorrection()` to use new simulation for error calculation
   - Updated `ApplyLMSSimulation()` to use Machado matrices
   - Updated `ApplyZoneCorrection()` to check simulation mode flag

2. **ColorBlindnessEffect.cs** (`plugins/MouseEffects.Effects.ColorBlindness/`)
   - Added `SimulationMode` property to `ZoneSettings` class
   - Updated constant buffer struct with `SimulationMode` for each zone
   - Updated `OnRender()` to pass simulation mode values
   - Added configuration loading for `simulationMode`

3. **ColorBlindnessSettingsControl.xaml** (`plugins/MouseEffects.Effects.ColorBlindness/UI/`)
   - Added Mode ComboBox to all 4 zones with options:
     - "Correction (help colorblind users)" - default
     - "Simulation (show what they see)"

4. **ColorBlindnessSettingsControl.xaml.cs** (`plugins/MouseEffects.Effects.ColorBlindness/UI/`)
   - Updated `LoadZoneSettings()` to include `modeCombo` parameter
   - Updated `SaveZoneSettings()` to include `modeCombo` parameter
   - Added event handlers: `Zone0ModeCombo_SelectionChanged` through `Zone3ModeCombo_SelectionChanged`

### Previous Session Work (From Earlier Context)

- Implemented zone-based architecture for ColorBlindness plugin
- Fixed Alt+Shift+C hotkey using `IHotkeyProvider` interface for global hotkeys
- Added virtual cursor indicator in comparison mode
- Added PayPal donate button to README.md
- Documented Space Invaders plugin in README.md and Wiki/Plugins.md

### Build Status

- **Last Build**: Successful (0 warnings, 0 errors)
- **Build Time**: ~4 seconds

### Testing Notes

To verify the fix:
1. Open MouseEffects and enable ColorBlindness plugin
2. Set layout to Quadrants for comparison view
3. Test with color wheel image (`Linear_RGB_color_wheel.png` in project root)
4. **Correction mode + Protanopia**: Should add blue tints to distinguish red from green
5. **Simulation mode + Protanopia**: Red and green should appear similar (as protanope sees them)

### Technical Reference: Machado (2009) Simulation Matrices

**Protanopia (100%):**
```
[0.152286,  1.052583, -0.204868]
[0.114503,  0.786281,  0.099216]
[-0.003882, -0.048116,  1.051998]
```

**Deuteranopia (100%):**
```
[0.367322,  0.860646, -0.227968]
[0.280085,  0.672501,  0.047413]
[-0.011820,  0.042940,  0.968881]
```

**Tritanopia (100%):**
```
[1.255528, -0.076749, -0.178779]
[-0.078411,  0.930809,  0.147602]
[0.004733,  0.691367,  0.303900]
```

4. **Added Machado and Strict LMS Filter Modes**
   - Users can now choose between two simulation/correction algorithms:
     - **Machado**: RGB-space matrices from Machado et al. (2009) - fast, widely used
     - **Strict**: Proper LMS colorspace simulation - more physiologically accurate
   - Both modes available for all 6 CVD types (Protanopia, Protanomaly, Deuteranopia, Deuteranomaly, Tritanopia, Tritanomaly)
   - Filter dropdown now shows 17 options:
     - None
     - 6 Machado filters (Protanopia through Tritanomaly)
     - 6 Strict filters (Protanopia through Tritanomaly)
     - Achromatopsia, Achromatomaly, Grayscale, Inverted Grayscale

### Files Modified This Session (Continued)

5. **ColorBlindness.hlsl** - Added Machado vs Strict architecture:
   - Added `Machado_*` matrices (from godotshaders.com)
   - Added `Strict_*_LMS` matrices (Brettel/Viénot confusion lines)
   - Created `SimulateMachado()` function for RGB-space simulation
   - Created `SimulateStrict()` function for LMS-space simulation
   - Updated `ApplyLMSCorrection()` to handle filter types 1-16
   - Updated `ApplyLMSSimulation()` to handle filter types 1-16

6. **ColorBlindnessSettingsControl.xaml** - Updated all 4 zone filter ComboBoxes:
   - Added "(Machado)" suffix to types 1-6
   - Added "(Strict)" suffix to types 7-12
   - Total 17 filter options per zone

### Filter Type Mapping

| Index | Filter Name |
|-------|-------------|
| 0 | None |
| 1 | Protanopia (Machado) |
| 2 | Protanomaly (Machado) |
| 3 | Deuteranopia (Machado) |
| 4 | Deuteranomaly (Machado) |
| 5 | Tritanopia (Machado) |
| 6 | Tritanomaly (Machado) |
| 7 | Protanopia (Strict) |
| 8 | Protanomaly (Strict) |
| 9 | Deuteranopia (Strict) |
| 10 | Deuteranomaly (Strict) |
| 11 | Tritanopia (Strict) |
| 12 | Tritanomaly (Strict) |
| 13 | Achromatopsia |
| 14 | Achromatomaly |
| 15 | Grayscale |
| 16 | Inverted Grayscale |

### Technical Reference: Strict LMS Matrices

Based on Brettel, Viénot & Mollon (1997) confusion lines:

**Protanopia (L-cone deficient):**
- L' = 2.02344*M - 2.52581*S

**Deuteranopia (M-cone deficient):**
- M' = 0.49421*L + 1.24827*S

**Tritanopia (S-cone deficient):**
- S' = -0.01224*L + 0.07203*M

### Build Status

- **Last Build**: Successful (0 warnings, 0 errors)
- **Build Time**: ~7 seconds

5. **Fixed Strict LMS Simulation Matrices**
   - **Deuteranopia**: Fixed coefficients from `M' = 0.49*L + 1.25*S` (wrong, sum=1.74) to `M' = 0.95*L + 0.05*S` (correct, sum=1.0)
   - **Tritanopia**: Fixed coefficients from `S' = -0.87*L + 1.87*M` (amplified blue!) to `S' = -0.4*L + 0.8*M` (Viénot 1999)
   - **Protanopia**: Fixed to use ixora.io coefficients `L' = 1.05*M - 0.05*S`

6. **Fixed Correction Algorithm**
   - Problem: Red colors got negative error, causing correction to add green/remove blue → orange tint
   - Solution: Use `max(0.0, error)` to only correct actual color losses, not colors that shifted in simulation
   - Result: Reds stay red, greens shift to cyan (for deuteranopia correction)

### Key Principle: White Point Preservation
- Simulation matrix coefficients must sum to ~1.0 to preserve white
- Previous coefficients summed to 1.74 (deuteranopia) and produced wrong results
- Fixed coefficients: 0.95 + 0.05 = 1.0 ✓

### Build Status

- **Last Build**: Successful (0 warnings, 0 errors)

### Next Steps (If Continuing)

1. Consider committing changes with descriptive message
2. Update Wiki documentation for the new filter options

---

# Session State: 2025-12-06 (ColorBlindnessNG Implementation)

## Completed Implementation

### ColorBlindnessNG Plugin - COMPLETE

Created a new "ColorBlindnessNG" (Next Generation) plugin that separates **Simulation** and **Correction** into two distinct approaches:

**Plugin Structure:**
```
plugins/MouseEffects.Effects.ColorBlindnessNG/
├── MouseEffects.Effects.ColorBlindnessNG.csproj
├── ColorBlindnessNGEffect.cs      # Main effect with LUT management
├── ColorBlindnessNGFactory.cs     # Plugin factory for discovery
├── LUTGenerator.cs                # LUT texture generation (RGB, LAB, HSL)
├── CorrectionPresets.cs           # 9 preset definitions
├── Shaders/
│   └── ColorBlindnessNG.hlsl      # Combined simulation + correction shader
└── UI/
    ├── ColorBlindnessNGSettingsControl.xaml
    └── ColorBlindnessNGSettingsControl.xaml.cs
```

### Files Created

1. **MouseEffects.Effects.ColorBlindnessNG.csproj**
   - Project file with embedded shader resources
   - References Core and DirectX projects
   - Output to plugins folder

2. **ColorBlindnessNGFactory.cs**
   - Plugin factory for discovery
   - Default configuration with mode=0 (Simulation), intensity=1.0

3. **ColorBlindnessNGEffect.cs**
   - Main effect class extending EffectBase
   - Creates and manages 256x1 RGBA float LUT textures for R, G, B channels
   - Handles mode switching between Simulation and Correction
   - Updates LUT textures when configuration changes

4. **LUTGenerator.cs**
   - Static class generating gradient LUT textures
   - Three interpolation types:
     - `LerpLinearRGB()` - Simple linear RGB interpolation
     - `LerpLAB()` - Perceptually uniform LAB colorspace interpolation
     - `LerpHSL()` - HSL interpolation with hue wrap-around handling
   - Full color space conversion: RGB↔XYZ↔LAB, RGB↔HSL
   - sRGB gamma correction (linearize/encode)

5. **CorrectionPresets.cs**
   - `CorrectionPreset` record with per-channel settings
   - 9 presets: Custom, Deuteranopia, Protanopia, Tritanopia, Deuteranomaly, Protanomaly, Tritanomaly, RedGreenBoth, HighContrast
   - Each preset specifies: channel enables, strengths, start/end colors, recommended gradient type and application mode

6. **ColorBlindnessNG.hlsl**
   - **Simulation Mode**: Verified Machado (2009) matrices + Strict LMS mode
   - **Correction Mode**: LUT-based per-channel color remapping
   - Three application modes: Full Channel, Dominant Only, Threshold
   - Proper sRGB linearization for accurate color math

7. **ColorBlindnessNGSettingsControl.xaml/xaml.cs**
   - Dynamic UI switching between Simulation and Correction panels
   - Simulation: Algorithm radio (Machado/Strict), CVD type dropdown
   - Correction: Preset dropdown, application mode, gradient type, per-channel controls with color pickers
   - Windows Forms ColorDialog for color picking

### Verified Machado (2009) Matrices

Source: "A Physiologically-based Model for Simulation of Color Vision Deficiency"
IEEE Transactions on Visualization and Computer Graphics, Vol. 15, No. 6, 2009

```hlsl
// Protanopia (100% L-cone loss)
static const float3x3 Machado_Protanopia = float3x3(
    0.567, 0.433, 0.000,
    0.558, 0.442, 0.000,
    0.000, 0.242, 0.758
);

// Deuteranopia (100% M-cone loss)
static const float3x3 Machado_Deuteranopia = float3x3(
    0.625, 0.375, 0.000,
    0.700, 0.300, 0.000,
    0.000, 0.300, 0.700
);

// Tritanopia (100% S-cone loss)
static const float3x3 Machado_Tritanopia = float3x3(
    0.950, 0.050, 0.000,
    0.000, 0.433, 0.567,
    0.000, 0.475, 0.525
);
```

### Build Status

- **ColorBlindnessNG Project**: Builds successfully
- **Solution Build**: File lock errors because MouseEffects.App was running (not a code issue)
- Project added to solution under `plugins` folder

### Bug Fix Applied

- **Ambiguous UserControl reference**: Fixed by adding `using UserControl = System.Windows.Controls.UserControl;` to distinguish WPF from WinForms UserControl

### Testing Required

1. Close MouseEffects.App if running
2. Build full solution: `dotnet build -c Debug -p:Platform=x64`
3. Test Simulation mode with Ishihara test images
4. Test Correction mode presets with color wheel images
5. Verify LUT gradient interpolation (LAB should be smoother than Linear RGB)

### LUT-Based Correction Architecture

```
Input Screen Color (R, G, B)
         │
         ▼
┌─────────────────────────────────────────┐
│  For each channel with LUT enabled:     │
│                                         │
│  R channel → Sample R_LUT[R] → R'       │
│  G channel → Sample G_LUT[G] → G'       │
│  B channel → Sample B_LUT[B] → B'       │
│                                         │
│  Channels without LUT pass through      │
└─────────────────────────────────────────┘
         │
         ▼
Output Corrected Color (R', G', B')
```

### Application Modes

| Mode | Description |
|------|-------------|
| **Full Channel** | Any pixel with R>0 gets the red LUT applied proportionally |
| **Dominant Only** | Only remap when channel is dominant (R > G and R > B) |
| **Threshold** | Apply LUT only when channel exceeds configurable threshold |

### Gradient Interpolation Types

| Type | Description |
|------|-------------|
| **Linear RGB** | Simple linear interpolation - fast, may have muddy midtones |
| **Perceptual LAB** | Interpolate in LAB color space - perceptually uniform gradient |
| **HSL** | Interpolate through hue - more vibrant gradient |
