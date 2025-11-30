# Features Overview

MouseEffects provides a rich set of features for creating stunning visual effects around your mouse cursor.

## Core Features

### GPU-Accelerated Rendering

MouseEffects uses DirectX 11 for hardware-accelerated rendering:

- **Efficient Shader Pipeline** - Custom HLSL shaders for each effect
- **GPU Instancing** - Render thousands of particles in a single draw call
- **Structured Buffers** - Efficient GPU memory usage for effect data
- **Real-time Performance** - Maintains 60+ FPS even with multiple effects

### Transparent Overlay

The application renders on a transparent, always-on-top window:

- **Click-Through** - Mouse events pass through to underlying applications
- **Full Screen Coverage** - Effects visible across all monitors
- **No Focus Stealing** - Runs silently without interrupting your workflow

### Plugin Architecture

Effects are implemented as plugins for maximum flexibility:

- **Hot Loading** - Plugins discovered automatically at startup
- **Independent Configuration** - Each plugin has its own settings
- **Custom UI** - Plugins can provide WPF-based settings controls
- **Easy Development** - Simple interface to implement new effects

### Screen Capture Integration

Some effects can capture and transform screen content:

- **DXGI Desktop Duplication** - Efficient hardware-accelerated capture
- **Hybrid GPU Support** - Works with laptops using integrated + discrete GPUs
- **On-Demand Capture** - Only captures when effects need it
- **Continuous Mode** - Real-time screen transformation for filter effects

## System Tray Integration

MouseEffects runs as a system tray application:

- **Minimal Footprint** - No visible window when running
- **Quick Access** - Right-click tray icon for settings
- **Status Indicator** - Icon shows application state

### Tray Menu Options

| Option | Description |
|--------|-------------|
| **Settings** | Open the settings window |
| **Enable/Disable** | Toggle all effects on/off |
| **Exit** | Close the application |

## Automatic Updates

MouseEffects includes built-in auto-update functionality via [Velopack](https://github.com/velopack/velopack):

- **GitHub Integration** - Updates distributed via GitHub Releases
- **Delta Updates** - Only download changed files for faster updates
- **No Admin Required** - Updates install to user profile
- **Configurable Modes** - Silent background or user-notified updates
- **Check Frequency** - On startup, daily, weekly, or manual only

### Update Modes

| Mode | Description |
|------|-------------|
| **Silent** | Downloads and applies automatically on restart |
| **Notify** | Shows notification, user chooses when to update |

See [Auto-Updates](Auto-Updates.md) for detailed configuration.

## Settings Window

The settings window provides complete control over all effects:

### Global Settings

- **GPU Selection** - Choose which graphics card to use
- **Frame Rate** - Adjust target FPS (30-120)
- **FPS Counter** - Display performance metrics

### Effect Settings

Each effect has its own configuration panel:

- **Enable/Disable** - Toggle individual effects
- **Collapsible Panels** - Organized settings groups
- **Real-time Preview** - Changes apply immediately
- **Persistent Storage** - Settings saved automatically

## Performance Features

### Fixed Timestep Game Loop

- **Consistent Physics** - Effects behave the same regardless of frame rate
- **Smooth Animation** - No stuttering or jitter
- **Decoupled Update/Render** - Logic runs at fixed rate, rendering at variable rate

### Resource Management

- **Lazy Initialization** - Resources created only when needed
- **Automatic Cleanup** - GPU resources properly disposed
- **Memory Efficient** - Structured buffers minimize memory usage

### Multi-GPU Support

MouseEffects handles complex GPU configurations:

- **Automatic Detection** - Finds the best GPU for rendering
- **Hybrid Graphics** - Works with Intel + NVIDIA/AMD setups
- **Cross-Adapter Copy** - Handles screen capture across GPU boundaries

## Input Handling

### Global Mouse Hook

- **System-Wide Tracking** - Captures mouse position anywhere on screen
- **Button State** - Tracks left, right, middle, and extra buttons
- **Click Events** - Detects click and release for effect triggers

### Per-Frame Input State

- **Position Tracking** - Current and previous positions
- **Delta Calculation** - Movement since last frame
- **Event Separation** - Clicks vs. held state

## Effect Capabilities

Effects in MouseEffects can:

| Capability | Description |
|------------|-------------|
| **Particle Systems** | Spawn and manage thousands of particles |
| **Screen Transformation** | Modify screen content in real-time |
| **Physics Simulation** | Gravity, velocity, collision detection |
| **Color Processing** | HSL/RGB manipulation, color blindness simulation |
| **Geometric Rendering** | Lines, circles, rectangles with shaders |
| **Texture Sampling** | Use screen content as texture source |

## Extensibility

### Custom Effects

Developers can create new effects by:

1. Implementing `IEffect` and `IEffectFactory` interfaces
2. Writing HLSL shaders for GPU rendering
3. Optionally providing WPF settings controls
4. Compiling as a DLL in the plugins folder

### Configuration Schema

Effects define their settings with a schema:

- **Automatic UI Generation** - Settings controls created from schema
- **Type Safety** - Float, int, bool, color, choice parameters
- **Validation** - Min/max ranges enforced
- **Grouping** - Organize settings into logical sections

See [Plugin Development](Plugin-Development.md) for detailed guides.
