# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build entire solution
dotnet build

# Build Release
dotnet build -c Release

# Build specific plugin
dotnet build plugins/MouseEffects.Effects.ASCIIZer/MouseEffects.Effects.ASCIIZer.csproj

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

## Plan mode
Use smart names based on the feature user want to implement to create plan .md files, not random names, use a name that means something for the user regarding the required task. 
Also store the plans of one project in a sub folder of its folder (the current folder). Name this subfolder : ".claude_plans" and store all plans related to the current active project here.

## Agents
Use as many agents as you can everytime you can to parallelize plan mode and edit mode.
