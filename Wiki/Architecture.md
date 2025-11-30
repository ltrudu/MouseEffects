# Architecture Guide

This document describes the technical architecture of MouseEffects.

## Overview

MouseEffects follows a modular, plugin-based architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    MouseEffects.App                         │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────────┐  │
│  │  GameLoop   │  │EffectManager │  │  OverlayManager   │  │
│  └──────┬──────┘  └──────┬───────┘  └─────────┬─────────┘  │
└─────────┼────────────────┼────────────────────┼────────────┘
          │                │                    │
┌─────────▼────────────────▼────────────────────▼────────────┐
│                      Core Services                          │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────────┐  │
│  │    Input    │  │   DirectX    │  │     Overlay       │  │
│  │   (Hooks)   │  │  (Rendering) │  │    (Window)       │  │
│  └─────────────┘  └──────────────┘  └───────────────────┘  │
└─────────────────────────────────────────────────────────────┘
          │                │
┌─────────▼────────────────▼─────────────────────────────────┐
│                    Plugin System                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  IEffect  ◄──  EffectBase  ◄──  ConcreteEffects    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

### MouseEffects.Core

The foundation library with no Windows-specific dependencies.

**Key Components**:

```csharp
// Effect contract
public interface IEffect : IDisposable
{
    EffectMetadata Metadata { get; }
    bool IsEnabled { get; set; }
    bool IsComplete { get; }
    int RenderOrder { get; }
    EffectConfiguration Configuration { get; }

    void Initialize(IRenderContext context);
    void Configure(EffectConfiguration config);
    void Update(GameTime gameTime, MouseState mouseState);
    void Render(IRenderContext context);
}

// Factory for creating effects
public interface IEffectFactory
{
    EffectMetadata Metadata { get; }
    IEffect CreateEffect();
    EffectConfiguration GetDefaultConfiguration();
    EffectConfigurationSchema GetConfigurationSchema();
    FrameworkElement? CreateSettingsControl(IEffect effect);
}
```

**Configuration System**:

```csharp
// Type-safe configuration storage
public class EffectConfiguration
{
    public void Set<T>(string key, T value);
    public bool TryGet<T>(string key, out T value);
    public T Get<T>(string key, T defaultValue);
}

// Schema for UI generation
public class EffectConfigurationSchema
{
    public IReadOnlyList<ConfigurationParameter> Parameters { get; }
}
```

### MouseEffects.DirectX

DirectX 11 rendering implementation.

**Graphics Device**:

```csharp
public class D3D11GraphicsDevice : IDisposable
{
    public ID3D11Device Device { get; }
    public ID3D11DeviceContext Context { get; }

    // Resource creation helpers
    public ID3D11Buffer CreateBuffer<T>(...);
    public ID3D11Texture2D CreateTexture2D(...);
    public ID3D11ShaderResourceView CreateShaderResourceView(...);
}
```

**Render Context**:

```csharp
public class D3D11RenderContext : IRenderContext
{
    // Screen capture
    public ID3D11ShaderResourceView? ScreenTexture { get; }
    public bool ContinuousCaptureMode { get; set; }

    // Rendering
    public void BeginFrame();
    public void EndFrame();
    public void Clear(Color4 color);

    // State management
    public void SetBlendState(BlendMode mode);
    public void SetShader(ID3D11VertexShader vs, ID3D11PixelShader ps);
}
```

**Screen Capture**:

```csharp
public class ScreenCapture : IDisposable
{
    // DXGI Desktop Duplication
    public bool Initialize(IDXGIAdapter adapter);
    public ID3D11Texture2D? CaptureFrame(int timeoutMs);

    // Hybrid GPU support
    public bool RequiresCrossDeviceCopy { get; }
}
```

### MouseEffects.Input

Global mouse input handling using Win32 hooks.

```csharp
public class GlobalMouseHook : IDisposable
{
    public event EventHandler<MouseEventArgs>? MouseMove;
    public event EventHandler<MouseButtonEventArgs>? MouseDown;
    public event EventHandler<MouseButtonEventArgs>? MouseUp;

    public Point CurrentPosition { get; }
    public MouseButtonState LeftButton { get; }
    public MouseButtonState RightButton { get; }
}
```

### MouseEffects.Overlay

Transparent overlay window management.

```csharp
public class OverlayWindow : IDisposable
{
    public IntPtr Handle { get; }
    public Size Size { get; }

    // Window properties
    public bool IsClickThrough { get; set; }
    public bool IsTopmost { get; set; }
    public bool IsTransparent { get; set; }
}
```

### MouseEffects.Plugins

Plugin discovery and loading.

```csharp
public class PluginLoader
{
    public IEnumerable<IEffectFactory> LoadPlugins(string pluginsPath);

    // Discovers DLLs implementing IEffectFactory
    // Uses reflection to instantiate factories
}
```

### MouseEffects.App

Main application orchestration.

```csharp
public class GameLoop
{
    public int TargetFrameRate { get; set; }

    public event Action<GameTime>? OnUpdate;
    public event Action? OnRender;

    public void Start();
    public void Stop();
}

public class EffectManager
{
    public void RegisterFactory(IEffectFactory factory);
    public IEffect? GetEffect(string id);
    public void EnableEffect(string id);
    public void DisableEffect(string id);
}
```

## Data Flow

### Frame Update Cycle

```
1. GameLoop.Tick()
   │
   ├─► 2. Input Processing
   │      └─ GlobalMouseHook polls current state
   │
   ├─► 3. Screen Capture Check
   │      └─ If any effect requires continuous capture:
   │         └─ Set ContinuousCaptureMode = true
   │
   ├─► 4. Effect Updates (Fixed Timestep)
   │      └─ For each enabled effect:
   │         └─ effect.Update(gameTime, mouseState)
   │
   └─► 5. Rendering (Variable Timestep)
          └─ For each overlay window:
             ├─ context.BeginFrame()
             ├─ Capture screen if needed
             ├─ For each effect (sorted by RenderOrder):
             │  └─ effect.Render(context)
             └─ context.EndFrame()
```

### Effect Lifecycle

```
                    ┌──────────────┐
                    │   Created    │
                    └──────┬───────┘
                           │
                    ┌──────▼───────┐
                    │  Initialize  │◄─── GPU resources allocated
                    └──────┬───────┘
                           │
            ┌──────────────▼──────────────┐
            │         Active Loop          │
            │  ┌───────────────────────┐  │
            │  │  Configure (on change)│  │
            │  ├───────────────────────┤  │
            │  │  Update (every frame) │  │
            │  ├───────────────────────┤  │
            │  │  Render (every frame) │  │
            │  └───────────────────────┘  │
            └──────────────┬──────────────┘
                           │ IsComplete = true
                    ┌──────▼───────┐
                    │   Dispose    │◄─── GPU resources released
                    └──────────────┘
```

## Rendering Pipeline

### Shader Architecture

Each effect typically has:

1. **Vertex Shader** - Transforms vertices, passes data to pixel shader
2. **Pixel Shader** - Computes final pixel color
3. **Constant Buffers** - Per-frame/per-object data
4. **Structured Buffers** - Arrays of effect-specific data

Example shader structure:

```hlsl
// Constant buffer (updated once per frame)
cbuffer FrameConstants : register(b0)
{
    float2 ScreenSize;
    float2 MousePosition;
    float Time;
    float DeltaTime;
}

// Structured buffer (particle data)
StructuredBuffer<Particle> Particles : register(t0);

// Screen texture (for effects that use screen capture)
Texture2D ScreenTexture : register(t1);
SamplerState ScreenSampler : register(s0);
```

### Blend Modes

```csharp
public enum BlendMode
{
    Opaque,      // No blending, fully replaces
    Alpha,       // Standard alpha blending
    Additive,    // Adds to existing color (glow effects)
    Multiply     // Multiplies with existing color
}
```

### GPU Resource Management

Effects manage their own GPU resources:

```csharp
public class ParticleTrailEffect : EffectBase
{
    private ID3D11Buffer? _particleBuffer;
    private ID3D11ShaderResourceView? _particleSRV;
    private ID3D11VertexShader? _vertexShader;
    private ID3D11PixelShader? _pixelShader;

    protected override void OnInitialize(IRenderContext context)
    {
        // Create GPU resources
        var device = ((D3D11RenderContext)context).Device;
        _particleBuffer = CreateStructuredBuffer(device, ...);
        _particleSRV = device.CreateShaderResourceView(_particleBuffer);
        // Compile and create shaders...
    }

    protected override void OnDispose()
    {
        // Release GPU resources
        _particleSRV?.Dispose();
        _particleBuffer?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }
}
```

## Screen Capture System

### DXGI Desktop Duplication

MouseEffects uses the DXGI Desktop Duplication API for efficient screen capture:

```
┌────────────────────────────────────────────────────────┐
│                    DWM Compositor                       │
│     (Composites all windows into final frame)          │
└────────────────────────┬───────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│              DXGI Output Duplication                    │
│  ┌─────────────────────────────────────────────────┐   │
│  │  AcquireNextFrame(timeout)                      │   │
│  │  - Returns desktop texture when frame ready    │   │
│  │  - GPU-accelerated, zero-copy if same adapter  │   │
│  └─────────────────────────────────────────────────┘   │
└────────────────────────┬───────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│                  Screen Texture                         │
│     (Used by effects as shader resource)               │
└────────────────────────────────────────────────────────┘
```

### Capture Modes

**Request Mode** (default):
- 0ms timeout
- Returns immediately with cached frame if no new frame
- Used by effects that don't need every screen update

**Continuous Mode**:
- 16ms timeout (targets 60 FPS)
- Waits for DWM to compose new frame
- Flushes DWM to ensure latest content
- Used by screen transformation effects

### Hybrid GPU Handling

For laptops with integrated + discrete GPUs:

```
┌─────────────────┐          ┌─────────────────┐
│  Intel iGPU     │          │  NVIDIA/AMD     │
│  (Primary)      │          │  (Discrete)     │
│                 │          │                 │
│  ┌───────────┐  │          │  ┌───────────┐  │
│  │  Screen   │  │  Copy    │  │  Render   │  │
│  │  Capture  │──┼─────────►│  │  Context  │  │
│  └───────────┘  │          │  └───────────┘  │
└─────────────────┘          └─────────────────┘
```

When rendering adapter differs from capture adapter:
1. Create staging texture on capture adapter
2. Copy captured frame to staging (CPU-accessible)
3. Read pixels on CPU
4. Write to texture on render adapter

## Settings Persistence

### Storage Locations

```
%APPDATA%\MouseEffects\
├── settings.json           # Application settings
└── plugins\
    ├── particle-trail.json
    ├── laser-work.json
    ├── screen-distortion.json
    ├── color-blindness.json
    ├── radial-dithering.json
    └── tile-vibration.json
```

### Serialization

```csharp
// Application settings
public class AppSettings
{
    public string? SelectedGpuName { get; set; }
    public int TargetFrameRate { get; set; } = 60;
    public bool ShowFpsCounter { get; set; }
    public bool ShowFpsOverlay { get; set; }
}

// Plugin settings
public class PluginSettings
{
    public bool IsEnabled { get; set; }
    public EffectConfiguration Configuration { get; set; }
}
```

### Configuration Serialization

Special handling for complex types:

```csharp
// Vector4 (colors) serialized as object
{
    "startColor": { "X": 1.0, "Y": 0.5, "Z": 0.0, "W": 1.0 }
}

// Arrays serialized as JSON arrays
{
    "curvePoints": [
        { "X": 0, "Y": 0 },
        { "X": 0.5, "Y": 0.5 },
        { "X": 1, "Y": 1 }
    ]
}
```

## Threading Model

MouseEffects uses a single-threaded game loop:

```
Main Thread (UI + Game Loop)
│
├─► Windows Message Pump
│   └─ Processes window messages, input events
│
├─► GameLoop.Tick()
│   ├─ Update phase (logic)
│   └─ Render phase (DirectX calls)
│
└─► Settings Window
    └─ WPF UI on same thread
```

**Thread Safety Considerations**:
- All DirectX calls on main thread
- Mouse hook callbacks marshaled to main thread
- Settings changes applied on next frame

## Design Patterns

### Factory Pattern
- `IEffectFactory` creates effect instances
- Separates plugin discovery from instantiation
- Enables lazy loading of effects

### Component Pattern
- Effects are independent, composable components
- Each effect handles its own state and rendering
- RenderOrder controls composition order

### Configuration Pattern
- `EffectConfiguration` provides type-safe key-value storage
- `EffectConfigurationSchema` enables automatic UI generation
- Decouples settings from effect implementation

### Observer Pattern
- `SettingsChanged` events for persistence
- `ViewportChanged` notifications for resize handling
- `ConfigurationChanged` hooks for effect updates
