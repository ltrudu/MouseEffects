# MouseEffects Application State

## Project Overview

MouseEffects is a GPU-accelerated visual effects overlay for Windows that creates cursor effects in real-time using DirectX 11. It uses a plugin architecture for effects.

## Project Structure

```
MouseEffects/
├── src/
│   ├── MouseEffects.Core/           # Interfaces: IEffect, IEffectFactory, EffectBase
│   ├── MouseEffects.DirectX/        # DirectX 11 rendering implementation
│   ├── MouseEffects.Input/          # Global mouse hook, MouseState
│   ├── MouseEffects.Overlay/        # Transparent overlay window
│   ├── MouseEffects.Plugins/        # Plugin discovery and loading
│   └── MouseEffects.App/            # Main WPF application, settings UI
├── plugins/                         # Effect plugins (auto-discovered)
│   ├── MouseEffects.Effects.ParticleTrail/
│   ├── MouseEffects.Effects.LaserWork/
│   ├── MouseEffects.Effects.ScreenDistortion/
│   ├── MouseEffects.Effects.ColorBlindness/
│   ├── MouseEffects.Effects.RadialDithering/
│   ├── MouseEffects.Effects.TileVibration/
│   ├── MouseEffects.Effects.WaterRipple/
│   └── MouseEffects.Effects.Zoom/   # NEW - Magnifying lens effect
├── packaging/                       # MSIX packaging
└── Wiki/                            # Documentation
```

## Core Interfaces

### IEffect (MouseEffects.Core/Effects/IEffect.cs)
- `InstanceId`, `Metadata`, `Configuration`, `RenderOrder`, `IsComplete`, `IsEnabled`
- `RequiresContinuousScreenCapture` - for screen capture effects
- `Initialize(IRenderContext)`, `Configure(EffectConfiguration)`, `Update(GameTime, MouseState)`, `Render(IRenderContext)`

### EffectBase (MouseEffects.Core/Effects/EffectBase.cs)
- Abstract base class implementing IEffect
- Override: `OnInitialize`, `OnUpdate`, `OnRender`, `OnConfigurationChanged`, `OnViewportSizeChanged`, `OnDispose`

### IEffectFactory
- `Metadata`, `Create()`, `GetDefaultConfiguration()`, `GetConfigurationSchema()`, `CreateSettingsControl(IEffect)`

### EffectConfiguration
- Key-value dictionary: `Set<T>(key, value)`, `TryGet<T>(key, out value)`
- Schema parameters: `FloatParameter`, `IntParameter`, `BoolParameter`, `ColorParameter`, `ChoiceParameter`

### MouseState (MouseEffects.Core/Input/MouseState.cs)
- `Position`, `PreviousPosition`, `Velocity`, `ScrollDelta`
- `ButtonsDown`, `ButtonsPressed`, `ButtonsReleased` (MouseButtons flags)

### IRenderContext
- `ViewportSize`, `ScreenTexture` (for screen capture effects)
- `CompileShader`, `CreateBuffer`, `CreateSamplerState`
- `SetVertexShader`, `SetPixelShader`, `SetConstantBuffer`, `SetShaderResource`, `SetBlendState`
- `Draw`, `DrawInstanced`

## Plugin Development Pattern

### Project File (.csproj)
```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
<UseWPF>true</UseWPF>
<UseWindowsForms>true</UseWindowsForms>
<Platforms>x64</Platforms>
<OutputPath>..\..\src\MouseEffects.App\bin\$(Platform)\$(Configuration)\$(TargetFramework)\plugins\</OutputPath>
<EmbeddedResource Include="Shaders\*.hlsl" />
```

### Effect Class Structure
1. Define `EffectMetadata` with Id, Name, Description, Author, Version, Category
2. Declare GPU resources (shaders, buffers, samplers)
3. Declare cached configuration values
4. Implement `OnInitialize`: compile shaders, create buffers
5. Implement `OnConfigurationChanged`: read config values
6. Implement `OnUpdate`: update state from mouse/time
7. Implement `OnRender`: set shaders, update buffers, draw
8. Implement `OnDispose`: cleanup GPU resources

### Shader Loading
```csharp
private static string LoadEmbeddedShader(string name)
{
    var assembly = typeof(YourEffect).Assembly;
    var resourceName = $"Namespace.Shaders.{name}";
    using var stream = assembly.GetManifestResourceStream(resourceName);
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}
```

### Constant Buffer Alignment
- Must be multiple of 16 bytes
- Vector4 requires 16-byte alignment
- Use `[StructLayout(LayoutKind.Sequential, Size = N)]`

### Settings Control Pattern
- WPF UserControl with event `Action<string>? SettingsChanged`
- Use `_isInitializing` flag to prevent feedback loops
- Load config in constructor, update effect via `_effect.Configure(config)`

## Zoom Effect Implementation (Latest Addition)

### Features
- Circle or rectangle shape selection
- Zoom factor: 1.1x to 5.0x (step 0.1)
- Circle: radius 20-500 px
- Rectangle: width/height 40-800 px, sync option for square
- Border: width 0-10 px, customizable color
- Hotkeys (optional):
  - Shift+Ctrl+Wheel: zoom factor +/- 0.1
  - Shift+Alt+Wheel: size +/- 5%

### Key Implementation Details
- Uses `GetAsyncKeyState` from user32.dll for modifier key detection
- `ConfigurationChangedByHotkey` event notifies UI of hotkey changes
- UI uses `Dispatcher.BeginInvoke` to refresh from render thread
- Shader uses signed distance function for rounded rectangle edges

### Files
- `ZoomEffect.cs` - Main effect class
- `ZoomEffectFactory.cs` - Factory with config schema
- `Shaders/ZoomShader.hlsl` - HLSL shader
- `UI/ZoomSettingsControl.xaml` - WPF settings UI
- `UI/ZoomSettingsControl.xaml.cs` - Code-behind

## Build Commands

```bash
# Build single plugin
dotnet build "plugins\MouseEffects.Effects.Zoom\MouseEffects.Effects.Zoom.csproj" -c Debug

# Build entire solution
dotnet build -c Debug

# Add project to solution
dotnet sln add "plugins\NewPlugin\NewPlugin.csproj" --solution-folder "plugins"
```

## Configuration Storage
Settings stored in: `%APPDATA%\MouseEffects\plugins\{plugin-id}.json`

## Current Branch
ZOOM_EFFECT

## Documentation
- README.md - Project overview, effects list, installation
- Wiki/Plugins.md - Detailed plugin reference with all settings
- Wiki/Plugin-Development.md - How to create plugins
- Wiki/Plugin-ScreenCapture.md - Screen capture plugin guide
