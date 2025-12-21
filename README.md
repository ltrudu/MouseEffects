# MouseEffects

A GPU-accelerated visual effects overlay for Windows that creates stunning cursor effects in real-time.

![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![DirectX 11](https://img.shields.io/badge/DirectX-11-green)
![License](https://img.shields.io/badge/license-MIT-brightgreen)

## Overview

MouseEffects is a modular, plugin-based application that renders beautiful visual effects following your mouse cursor. Built with DirectX 11 for maximum performance, it runs as a transparent overlay on top of all your windows.

### Key Features

- **GPU-Accelerated Rendering** - Smooth 60+ FPS effects using DirectX 11
- **Plugin Architecture** - Easily extensible with custom effects
- **Screen Capture Integration** - Effects can interact with screen content
- **Multi-GPU Support** - Works with hybrid graphics (Intel + NVIDIA/AMD)
- **Low Resource Usage** - Optimized rendering pipeline
- **System Tray Integration** - Runs quietly in the background
- **Auto-Updates** - Automatic updates via GitHub Releases (Velopack)
- **Modern UI Theming** - Light, Dark, or System theme with ModernWPF

## Built-in Effects

MouseEffects includes **46 stunning visual effects** organized into categories:

### 🌟 Particle Effects

| Effect | Description |
|--------|-------------|
| **Bubbles** | Floating soap bubbles with rainbow iridescence and optional screen refraction |
| **Cherry Blossoms** | Beautiful sakura petals floating gently with realistic tumbling motion |
| **Confetti** | Colorful confetti particles bursting from clicks or trailing the cursor |
| **Dandelion Seeds** | Delicate dandelion seeds floating on the breeze with wispy filaments |
| **Falling Leaves** | Autumn leaves drifting down with natural tumbling and color variations |
| **Fireflies** | Magical glowing fireflies that pulse and drift around the cursor |
| **Firework** | Stunning firework explosions with rockets, trails, and secondary bursts |
| **Hearts** | Floating heart particles perfect for romantic or Valentine themes |
| **Particle Trail** | Colorful particles following your cursor with physics simulation |
| **Pixie Dust** | Sparkling magical dust particles with glitter trail effects |
| **Smoke** | Realistic smoke wisps rising and dissipating from the cursor |
| **Snowfall** | Gentle snowflakes drifting down with wind effects |

### 🔥 Fire & Energy

| Effect | Description |
|--------|-------------|
| **Aurora** | Northern lights effect with flowing colorful ribbons |
| **Fire Trail** | Realistic fire and flames trailing behind the cursor with smoke and embers |
| **Laser Work** | Directional lasers shooting from cursor with collision explosions |
| **Lightning Storm** | Electric lightning bolts crackling around the cursor |
| **Neon Glow** | Vibrant neon glow effect following cursor movement |
| **Shockwave** | Expanding shockwave rings emanating from clicks |
| **Tesla** | Electric arcs and tesla coil effects around the cursor |

### 🌌 Space & Cosmic

| Effect | Description |
|--------|-------------|
| **Black Hole** | Gravitational distortion effect pulling in nearby particles |
| **Gravity Well** | Particles orbiting and being attracted to the cursor |
| **Nebula** | Cosmic nebula clouds with swirling colors and stars |
| **Portal** | Swirling interdimensional portal effect at cursor position |
| **Starfield Warp** | Hyperspace starfield warping toward or away from cursor |

### 🎨 Visual Filters (Screen Capture)

| Effect | Description |
|--------|-------------|
| **ASCIIZer** | Renders screen as ASCII art with 6 modes: Classic, Matrix Rain, Dot Matrix, Typewriter, Braille, and Edge ASCII. Includes CRT effects |
| **Color Blindness** | CVD simulation & correction with 17 filter types using Machado/Strict algorithms |
| **Color Blindness NG** | Next-gen CVD plugin with LUT correction, custom presets, and interactive controls |
| **Glitch** | Digital glitch and distortion effects on screen content |
| **Hologram** | Holographic display effect with scan lines and chromatic aberration |
| **Kaleidoscope** | Kaleidoscopic mirror effect centered on cursor |
| **Radial Dithering** | Bayer-pattern dithering effect radiating from cursor |
| **Retro** | Retro gaming filters: CRT scanlines, LCD grid, VHS, Gameboy, and more |
| **Screen Distortion** | Real-time lens, ripple, and wave distortion effects |
| **Tile Vibration** | Vibrating tiles that capture and display screen content |
| **Water Ripple** | Expanding water ripples on click with realistic wave physics |
| **Zoom** | Magnifying lens effect with circle or rectangle shape |

### ✨ Artistic & Geometric

| Effect | Description |
|--------|-------------|
| **Circuit** | Electronic circuit board patterns growing from cursor |
| **Crystal Growth** | Crystalline structures growing and branching outward |
| **DNA Helix** | Rotating DNA double helix structure following the cursor |
| **Flower Bloom** | Flowers blooming and petals unfurling at cursor position |
| **Ink Blot** | Ink splatter and watercolor bleeding effects |
| **Paint Splatter** | Colorful paint splashes and drips from cursor movement |
| **Pixel Explosion** | Retro pixel-style explosions bursting from clicks |
| **Procedural Sigil** | Magical arcane sigil with procedural geometry, runes, counter-rotating rings, and glowing energy |
| **Runes** | Ancient mystical runes appearing and fading around cursor |
| **Sacred Geometries** | Sacred geometry patterns: Flower of Life, Metatron's Cube, Sri Yantra |
| **Spirograph** | Mathematical spirograph patterns drawn by cursor movement |
| **Spotlight** | Dramatic spotlight effect illuminating area around cursor |

### 🎮 Interactive & Games

| Effect | Description |
|--------|-------------|
| **Firework** | Stunning firework explosions with rockets, trails, and secondary bursts |
| **Retro Command** | Missile Command-style defense game - protect cities from incoming missiles with counter-missiles |
| **Retropede** | Classic arcade Retropede - shoot the segmented retropede, avoid the spider, with DDT bombs |
| **Space Invaders** | Defend against neon invaders with rockets - includes scoring and leaderboard | 

## Screenshots

<img width="225" height="325" alt="image" src="https://github.com/user-attachments/assets/08df3105-a2f9-4c02-9584-9ef014c08d6d" />

#

<img width="480" height="270" alt="image" src="https://github.com/user-attachments/assets/c8131cd1-99a5-4346-b538-af999316dcfc" />


## Installation

### Velopack Installer (Recommended)

The easiest way to install MouseEffects with automatic updates:

1. Download `MouseEffects-win-Setup.exe` from [Releases](../../releases)
2. Run the installer - **no administrator rights required**
3. The app installs to your user profile and updates automatically

**Features:**
- No admin rights needed
- Automatic background updates
- Delta updates (only downloads changes)
- Silent or notify update modes

### Portable Version

For a no-install portable version:
 
1. Download `MouseEffects-{version}-win-full.nupkg` from [Releases](../../releases)
2. Rename to `.zip` and extract to any folder
3. Run `MouseEffects.App.exe`

### From Source

See the [Building from Source](Wiki/Building.md) guide.

## Quick Start

1. Launch **MouseEffects** from the Start menu
2. The app starts minimized to the system tray
3. Right-click the tray icon to access settings
4. Enable/disable effects and adjust their parameters
5. Effects render immediately on your screen

## System Requirements

- **OS**: Windows 10 (1803+) or Windows 11
- **Graphics**: DirectX 11 compatible GPU
- **Runtime**: .NET 8.0 (included in MSIX package)

## Documentation

Comprehensive documentation is available in the [Wiki](Wiki/Home.md):

- [Features Overview](Wiki/Features.md)
- [Auto-Updates](Wiki/Auto-Updates.md)
- [Plugin Reference](Wiki/Plugins.md)
- [Architecture Guide](Wiki/Architecture.md)
- [Building from Source](Wiki/Building.md)
- [MSIX Packaging](Wiki/MSIX-Packaging.md)
- [Velopack Packaging](Wiki/Velopack-Packaging.md)
- [Certificate Management](Wiki/Certificates.md)
- [Creating Custom Plugins](Wiki/Plugin-Development.md)
- [Screen Capture Plugins](Wiki/Plugin-ScreenCapture.md)
- [ColorBlindnessNG User Guide](Wiki/ColorBlindnessNG-Guide.md) (English)
- [ColorBlindnessNG User Guide](Wiki/ColorBlindnessNG-Guide_French.md) (French)

## Project Structure

```
MouseEffects/
├── .github/
│   └── workflows/                # GitHub Actions CI/CD
│       └── release.yml           # Automated release workflow
├── src/                          # Core application
│   ├── MouseEffects.Core/        # Interfaces and base classes
│   ├── MouseEffects.DirectX/     # DirectX 11 rendering
│   ├── MouseEffects.Input/       # Mouse input handling
│   ├── MouseEffects.Overlay/     # Overlay window management
│   ├── MouseEffects.Plugins/     # Plugin loading system
│   └── MouseEffects.App/         # Main application
├── plugins/                      # 46 built-in effect plugins
│   ├── MouseEffects.Effects.ASCIIZer/
│   ├── MouseEffects.Effects.Aurora/
│   ├── MouseEffects.Effects.BlackHole/
│   ├── MouseEffects.Effects.Bubbles/
│   ├── MouseEffects.Effects.CherryBlossoms/
│   ├── MouseEffects.Effects.Circuit/
│   ├── MouseEffects.Effects.ColorBlindness/
│   ├── MouseEffects.Effects.ColorBlindnessNG/
│   ├── MouseEffects.Effects.Confetti/
│   ├── MouseEffects.Effects.CrystalGrowth/
│   ├── MouseEffects.Effects.DandelionSeeds/
│   ├── MouseEffects.Effects.DNAHelix/
│   ├── MouseEffects.Effects.FallingLeaves/
│   ├── MouseEffects.Effects.Fireflies/
│   ├── MouseEffects.Effects.FireTrail/
│   ├── MouseEffects.Effects.Firework/
│   ├── MouseEffects.Effects.Glitch/
│   ├── MouseEffects.Effects.GravityWell/
│   ├── MouseEffects.Effects.Hearts/
│   ├── MouseEffects.Effects.Hologram/
│   ├── MouseEffects.Effects.InkBlot/
│   ├── MouseEffects.Effects.Invaders/
│   ├── MouseEffects.Effects.Kaleidoscope/
│   ├── MouseEffects.Effects.LaserWork/
│   ├── MouseEffects.Effects.LightningStorm/
│   ├── MouseEffects.Effects.Retropede/
│   ├── MouseEffects.Effects.Nebula/
│   ├── MouseEffects.Effects.ParticleTrail/
│   ├── MouseEffects.Effects.PixelExplosion/
│   ├── MouseEffects.Effects.PixieDust/
│   ├── MouseEffects.Effects.Portal/
│   ├── MouseEffects.Effects.ProceduralSigil/
│   ├── MouseEffects.Effects.RadialDithering/
│   ├── MouseEffects.Effects.Retro/
│   ├── MouseEffects.Effects.RetroCommand/
│   ├── MouseEffects.Effects.SacredGeometries/
│   ├── MouseEffects.Effects.Shockwave/
│   ├── MouseEffects.Effects.Smoke/
│   ├── MouseEffects.Effects.Snowfall/
│   ├── MouseEffects.Effects.Spirograph/
│   ├── MouseEffects.Effects.Spotlight/
│   ├── MouseEffects.Effects.StarfieldWarp/
│   ├── MouseEffects.Effects.Tesla/
│   ├── MouseEffects.Effects.TileVibration/
│   ├── MouseEffects.Effects.WaterRipple/
│   └── MouseEffects.Effects.Zoom/
├── packaging/                    # MSIX packaging files
└── Wiki/                         # Documentation
```

## Contributing

Contributions are welcome! Please read the [Architecture Guide](wiki/Architecture.md) to understand the codebase structure before submitting pull requests.

### Creating Plugins

MouseEffects has a powerful plugin system. See the plugin development guides:

- [Basic Plugin Development](wiki/Plugin-Development.md) - Create effects without screen capture
- [Screen Capture Plugins](wiki/Plugin-ScreenCapture.md) - Create effects that transform screen content

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) - .NET bindings for DirectX
- [SharpGen.Runtime](https://github.com/SharpGenTools/SharpGenTools) - COM interop generation
- [Velopack](https://github.com/velopack/velopack) - Modern installer and auto-update framework
- [ModernWpf](https://github.com/Kinnara/ModernWpf) - Fluent Design theme for WPF

## Support the Project

If you find MouseEffects useful, consider supporting its development:

<p align="center">
  <a href="https://www.paypal.com/ncp/payment/TU8EH7BZEPCPN">
    <img src="https://img.shields.io/badge/Donate-PayPal-blue.svg?logo=paypal" alt="Donate with PayPal">
  </a>
</p>

Your support helps keep this project alive and enables new features!

---

<p align="center">❤️ Made with love and <a href="https://claude.ai">Claude.ai</a> ❤️</p>
