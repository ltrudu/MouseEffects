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

| Effect | Description |
|--------|-------------|
| **Particle Trail** | Colorful particles that follow your cursor with physics simulation |
| **Laser Work** | Directional lasers shooting from cursor with collision detection |
| **Screen Distortion** | Real-time lens/ripple distortion effect |
| **Color Blindness** | CVD simulation & correction with 17 filter types (Machado/Strict algorithms), zone layouts, and comparison view |
| **Color Blindness NG** | Next-gen CVD plugin with per-zone configuration, LUT-based correction, custom presets, Circle/Rectangle cursor-following modes, Simulation-Guided Correction, and Post-Correction Verification |
| **Radial Dithering** | Bayer-pattern dithering effect around cursor |
| **Tile Vibration** | Vibrating tiles that capture and display screen content |
| **Water Ripple** | Expanding water ripples on click with realistic wave interference |
| **Zoom** | Magnifying lens effect with circle or rectangle shape and hotkey support |
| **Firework** | Stunning firework explosions with rockets, trails, and secondary explosions |
| **Space Invaders** | Defend against neon invaders with rockets - includes scoring and leaderboard |

## Screenshots

*Coming soon*

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

### From MSIX Package (Enterprise/Store)

For enterprise deployment or Microsoft Store distribution:

1. Download the latest `.msix` package from [Releases](../../releases)
2. If prompted about untrusted publisher, install the development certificate first:
   - Download `MouseEffects-Dev.cer`
   - Double-click and select **Install Certificate**
   - Choose **Local Machine** → **Trusted Root Certification Authorities**
3. Double-click the `.msix` file to install

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
├── plugins/                      # Built-in effect plugins
│   ├── MouseEffects.Effects.ParticleTrail/
│   ├── MouseEffects.Effects.LaserWork/
│   ├── MouseEffects.Effects.ScreenDistortion/
│   ├── MouseEffects.Effects.ColorBlindness/
│   ├── MouseEffects.Effects.ColorBlindnessNG/
│   ├── MouseEffects.Effects.RadialDithering/
│   ├── MouseEffects.Effects.TileVibration/
│   ├── MouseEffects.Effects.WaterRipple/
│   ├── MouseEffects.Effects.Zoom/
│   ├── MouseEffects.Effects.Firework/
│   └── MouseEffects.Effects.Invaders/
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
