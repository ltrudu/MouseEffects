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
- **Dynamic Capture** - Effects can enable/disable capture dynamically based on state
- **Capture FPS Tracking** - Monitor screen capture performance separately from render FPS

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
- **Theme** - Choose Light, Dark, or System theme
- **FPS Counter** - Display performance metrics in settings window
- **FPS Overlay** - On-screen performance display

## UI Theming

MouseEffects features modern UI theming powered by [ModernWPF](https://github.com/Kinnara/ModernWpf):

### Theme Options

| Theme | Description |
|-------|-------------|
| **System** | Follows Windows system theme automatically |
| **Light** | Always use light theme |
| **Dark** | Always use dark theme (default) |

### Features

- **Fluent Design** - Modern Windows 11 styling with smooth animations
- **Automatic Updates** - Theme changes apply immediately without restart
- **Consistent Look** - All windows and plugin settings use the same theme
- **System Integration** - System mode follows Windows light/dark preference

### Performance Monitoring

Two FPS display options are available:

#### Settings Window FPS Counter
Shows performance metrics directly in the settings window:
- **Render FPS**: Current render rate vs. target (e.g., "59.8 / 60 fps")
- **Capture FPS**: Screen capture rate (e.g., "Cap: 54.2 fps")
- Color-coded indicators: green (>95%), yellow (80-95%), red (<80%)

#### On-Screen FPS Overlay
A small overlay window displaying real-time performance:
- Position: Top-left corner of primary monitor
- Shows both render FPS and capture FPS
- Can be toggled independently from settings window counter
- Automatically enables capture FPS tracking when visible

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

### Dynamic Screen Capture Optimization

Effects that use screen capture can implement dynamic capture mode:

- **State-Based Capture**: Capture only runs when effect needs it (e.g., Water Ripple only captures when ripples are active)
- **Automatic Switching**: Effects signal their capture needs via `RequiresContinuousScreenCapture`
- **Major FPS Impact**: Disabling capture when idle can improve FPS from ~50 to ~120+ on some systems

### Shader Optimizations

- **Precomputed Values**: Expensive operations (division) computed on CPU where possible
- **Early Exit**: Shaders skip processing when no active elements exist
- **Partial Buffer Updates**: Only upload changed data to GPU

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
| **Color Processing** | HSL/RGB manipulation, color blindness simulation/correction |
| **Geometric Rendering** | Lines, circles, rectangles with shaders |
| **Texture Sampling** | Use screen content as texture source |

## Color Vision Accessibility (ColorBlindnessNG)

The ColorBlindnessNG plugin provides advanced accessibility features for color vision deficiency (CVD):

### Simulation Mode
Simulates how people with various types of color blindness perceive colors:
- **Machado Algorithm**: RGB-space matrices from Machado et al. (2009) research
- **Strict LMS Algorithm**: Physiologically-accurate LMS colorspace processing
- **8 CVD Types**: Protanopia, Protanomaly, Deuteranopia, Deuteranomaly, Tritanopia, Tritanomaly, Achromatopsia, Achromatomaly

### Correction Mode
Helps colorblind users distinguish colors using LUT-based remapping:
- **Per-Channel Control**: Independent red, green, blue correction with customizable gradients
- **Multiple Presets**: Built-in presets for common CVD types plus custom preset support
- **Application Modes**: Full Channel, Dominant Only, or Threshold-based correction
- **Gradient Interpolation**: Linear RGB, Perceptual LAB, or HSL color space blending

### Advanced Features (v1.0.28+)

#### Simulation-Guided Correction
Uses CVD simulation to detect which pixels are affected before applying correction:
- Only corrects pixels that would actually be impacted by the CVD type
- Configurable sensitivity (0.5-5.0) controls detection aggressiveness
- Leaves unaffected colors unchanged for more natural results

#### Post-Correction Simulation (Re-simulate for Verification)
Applies CVD simulation AFTER correction to verify effectiveness:
- Non-colorblind users can see how corrected colors appear to CVD users
- Helps validate that previously confusing colors are now distinguishable
- Independent algorithm and intensity controls per zone

### Display Modes
- **Fullscreen**: Apply effect to entire screen
- **Split Modes**: Vertical, Horizontal, or Quadrant splits for comparison
- **Circle/Rectangle**: Cursor-following shapes with configurable size and soft edges
- **Comparison Mode**: Duplicate screen in each zone with virtual cursor projection

See [Plugins Reference](Plugins.md#color-blindness-ng) for complete settings documentation

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
