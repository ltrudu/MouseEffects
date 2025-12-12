# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build entire solution
dotnet build

# Build Release
dotnet build -c Release

# Build specific plugin
dotnet build plugins/MouseEffects.Effects.Retro/MouseEffects.Effects.Retro.csproj

# Clean and rebuild
dotnet clean && dotnet build

# Run the application
dotnet run --project src/MouseEffects.App/MouseEffects.App.csproj

# Publish self-contained
dotnet publish src/MouseEffects.App/MouseEffects.App.csproj -c Release -r win-x64 --self-contained true -o ./publish

# Create Velopack installer
vpk pack --packId MouseEffects --packVersion 1.0.x --packDir ./publish --mainExe MouseEffects.App.exe --outputDir ./releases
```

## Architecture Overview

MouseEffects is a GPU-accelerated visual effects overlay for Windows using DirectX 11. It follows a plugin-based architecture:

```
MouseEffects.App (Orchestration)
    ├── GameLoop - Frame timing, update/render cycle
    ├── EffectManager - Plugin lifecycle management
    └── OverlayManager - Transparent window handling

Core Services
    ├── MouseEffects.Core - Interfaces (IEffect, IEffectFactory, IRenderContext)
    ├── MouseEffects.DirectX - D3D11 rendering, screen capture via DXGI Desktop Duplication
    ├── MouseEffects.Input - Global mouse hooks (Win32)
    ├── MouseEffects.Overlay - Transparent click-through windows
    └── MouseEffects.Plugins - Plugin discovery and loading

plugins/ - Effect implementations (each is a separate DLL)
```

## Effect Plugin Pattern

Every effect plugin follows this structure:

1. **Factory class** (`IEffectFactory`) - Creates instances, provides metadata and default config
2. **Effect class** (`EffectBase`) - GPU resources, update/render logic
3. **Shaders** (`Shaders/*.hlsl`) - Embedded HLSL, compiled at runtime
4. **Settings UI** (`UI/*.xaml`) - Optional WPF controls

### Key Effect Lifecycle Methods

```csharp
OnInitialize(IRenderContext)    // Create GPU resources (shaders, buffers)
OnConfigurationChanged()        // Apply settings from Configuration
OnUpdate(GameTime, MouseState)  // Update state each frame
OnRender(IRenderContext)        // Draw to screen
OnDispose()                     // Release GPU resources
```

### Screen Capture Effects

Effects that transform screen content must:
- Override `RequiresContinuousScreenCapture => true`
- Access screen via `context.ScreenTexture` (register t0 in shader)
- Handle hybrid GPU scenarios (iGPU captures, dGPU renders)

## Shader Conventions

- **Constant buffers**: b0 for shared/post-effects params, b1 for filter-specific params
- **Textures**: t0 = screen texture, t1 = character atlas or other resources
- **Samplers**: s0 = linear, s1 = point
- **Struct alignment**: 16-byte boundaries, use `[StructLayout(LayoutKind.Sequential, Size = N)]`
- **Embed shaders**: Add `<EmbeddedResource Include="Shaders\*.hlsl" />` to .csproj

### Common Shader Pattern

```hlsl
cbuffer Constants : register(b0) {
    float2 ViewportSize;
    float2 MousePosition;
    float Time;
    // ... 16-byte aligned
}

// Fullscreen triangle (no vertex buffer needed)
VSOutput VS(uint vertexId : SV_VertexID) {
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;
}
```

## Configuration System

Effects use `EffectConfiguration` for settings persistence:

```csharp
// In effect: read settings
Configuration.TryGet("paramName", out float value);

// In UI: update both property AND config for persistence
_effect.PropertyName = value;
_effect.Configuration.Set("paramName", value);
```

Settings persist to `%APPDATA%\MouseEffects\plugins\{effect-id}.json`

## Project Conventions

- **Plugin output**: Builds automatically copy to `src/MouseEffects.App/bin/.../plugins/`
- **Naming**: `MouseEffects.Effects.{EffectName}` for plugin projects
- **Filter prefixes**: Use unique prefixes for filter properties (e.g., `DM_` for DotMatrix, `EA_` for EdgeASCII)
- **Blend modes**: `BlendMode.Opaque`, `.Alpha`, `.Additive`, `.Multiply`

## Common Pitfalls

- **HLSL backslash in comments**: `// back \` causes line continuation - use `// backslash` instead
- **PI redefinition**: Define constants once at file scope with `static const float PI = 3.14159;`
- **Missing textures**: Shader samples from unbound texture = black screen
- **Constant buffer size mismatch**: C# struct size must exactly match HLSL cbuffer
- **No #include in shaders**: HLSL #include is not supported - inline all shared code (e.g., post-effects functions)

## Multi-Filter Plugin Pattern (e.g., Retro, ASCIIZer)

Plugins with multiple filter types follow this structure:

```
Plugin/
├── FilterType.cs              # Enum of available filters
├── {Plugin}Effect.cs          # Main effect with switch for filter rendering
├── {Plugin}Factory.cs         # Default config for all filters
├── Shaders/
│   ├── Filter1.hlsl           # Each filter has its own shader
│   └── Filter2.hlsl
└── UI/
    ├── {Plugin}SettingsControl.xaml  # Main control with filter selector
    └── Filters/
        ├── Filter1Settings.xaml      # Filter-specific settings
        └── Filter2Settings.xaml
```

**Key patterns:**
- Use filter-specific prefixes for config keys (e.g., `xs_`, `tv_`, `toon_`)
- Create separate constant buffer structs per filter (same base layout, filter-specific extras)
- Use largest buffer size when creating shared `_filterParamsBuffer`
- Filter settings UI: each filter gets its own UserControl with `Initialize(effect)` method

## Multi-Effect UI Architecture (e.g., Tesla)

Plugins with multiple distinct effects (not just filter variations) use a dynamic UI pattern with ContentControl as a host for effect-specific settings panels.

### Directory Structure

```
Plugin/
├── {Plugin}Effect.cs              # Main effect with all effect logic
├── {Plugin}Factory.cs             # Default config for all effects
├── Shaders/
│   ├── Effect1Shader.hlsl         # Each effect has its own shader
│   └── Effect2Shader.hlsl
└── UI/
    ├── {Plugin}SettingsControl.xaml    # Main control with effect selector
    ├── {Plugin}SettingsControl.xaml.cs
    └── Effects/
        ├── Effect1Settings.xaml        # Effect-specific settings
        ├── Effect1Settings.xaml.cs
        ├── Effect2Settings.xaml
        └── Effect2Settings.xaml.cs
```

### Key UI Pattern: ContentControl as Dynamic Host

**XAML (Main Settings Control):**
```xaml
<!-- Effect Type Selector -->
<ComboBox x:Name="EffectTypeCombo" SelectionChanged="EffectTypeCombo_Changed">
    <ComboBoxItem Content="Effect 1" IsSelected="True" />
    <ComboBoxItem Content="Effect 2" />
</ComboBox>

<!-- Dynamic Effect Settings Host -->
<ContentControl x:Name="EffectSettingsHost" />
```

**Code-Behind (Lazy Instantiation + Initialize Pattern):**
```csharp
private Effect1Settings? _effect1Settings;
private Effect2Settings? _effect2Settings;

private void LoadEffectSettings()
{
    int effectType = EffectTypeCombo.SelectedIndex;
    switch (effectType)
    {
        case 0: // Effect 1
            if (_effect1Settings == null)
                _effect1Settings = new Effect1Settings();
            EffectSettingsHost.Content = _effect1Settings;
            _effect1Settings.Initialize(_effect);
            break;
        case 1: // Effect 2
            if (_effect2Settings == null)
                _effect2Settings = new Effect2Settings();
            EffectSettingsHost.Content = _effect2Settings;
            _effect2Settings.Initialize(_effect);
            break;
    }
}
```

### Effect Settings Control Pattern

Each effect-specific settings control follows this pattern:

```csharp
public partial class Effect1Settings : UserControl
{
    private PluginEffect? _effect;
    private bool _isLoading;

    public Effect1Settings()
    {
        InitializeComponent();
    }

    public void Initialize(PluginEffect effect)
    {
        _effect = effect;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;
        _isLoading = true;
        try
        {
            // Load values from effect properties
            SomeSlider.Value = _effect.SomeProperty;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void SomeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        // Update both property AND config for persistence
        _effect.SomeProperty = (float)SomeSlider.Value;
        _effect.Configuration.Set("prefix_someProperty", _effect.SomeProperty);
    }
}
```

### Configuration Key Prefixes

Each effect should use a unique prefix for its config keys:
- Effect 1: `e1_` prefix (e.g., `e1_enabled`, `e1_intensity`)
- Effect 2: `e2_` prefix (e.g., `e2_enabled`, `e2_count`)

**Separate trigger flags per effect (not shared enum):**
```csharp
// Instead of: TriggerType _mouseMoveEffect = TriggerType.Effect1;
// Use separate booleans:
bool _effect1MouseMoveEnabled = true;
bool _effect1LeftClickEnabled = true;
bool _effect2MouseMoveEnabled = false;
```

### Benefits of This Pattern

1. **Lazy Loading**: Effect settings controls are created on-demand, reducing initial load time
2. **Memory Efficient**: Only the visible settings panel consumes resources
3. **Clean Separation**: Each effect's UI is isolated in its own file
4. **Easy Extension**: Adding new effects only requires new settings files and a switch case
5. **Persistent Selection**: Selected effect type stored in config (`selectedEffectType`)

## Window Capture Exclusion

To exclude a window from DXGI Desktop Duplication screen capture (prevents feedback loops):

```csharp
[LibraryImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
private static partial bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);

private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

// In window Loaded event:
SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
```

## Plan mode
Use smart names based on the feature user want to implement to create plan .md files, not random names, use a name that means something for the user regarding the required task. 
Also store the plans of one project in a sub folder of its folder (the current folder). Name this subfolder : ".claude_plans" and store all plans related to the current active project here.

## Agents
Use as many agents as you can everytime you can to parallelize plan mode and edit mode.
