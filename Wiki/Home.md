# MouseEffects Wiki

Welcome to the MouseEffects documentation. This wiki provides comprehensive guides for users and developers.

## User Guide

- [Features Overview](Features.md) - Learn about all application features
- [Auto-Updates](Auto-Updates.md) - Automatic update system and configuration
- [Plugins Reference](Plugins.md) - Detailed documentation for each effect plugin

## Developer Guide

- [Architecture](Architecture.md) - Technical architecture and design patterns
- [Building from Source](Building.md) - How to build the application
- [MSIX Packaging](MSIX-Packaging.md) - Creating MSIX distributable packages
- [Velopack Packaging](Velopack-Packaging.md) - Creating portable installer with auto-updates
- [Certificate Management](Certificates.md) - Code signing certificates

## Plugin Development

- [Creating Basic Plugins](Plugin-Development.md) - Build effects without screen capture
- [Screen Capture Plugins](Plugin-ScreenCapture.md) - Build effects that transform screen content

## Quick Links

| Topic | Description |
|-------|-------------|
| [System Requirements](#system-requirements) | Hardware and software requirements |
| [Installation](#installation) | How to install MouseEffects |
| [Configuration](#configuration) | Where settings are stored |

## System Requirements

### Minimum Requirements

- **Operating System**: Windows 10 version 1803 (April 2018 Update) or later
- **Graphics**: DirectX 11 compatible GPU
- **Memory**: 4 GB RAM
- **Storage**: 50 MB available space

### Recommended

- **Operating System**: Windows 11
- **Graphics**: Dedicated GPU with 2+ GB VRAM
- **Memory**: 8 GB RAM

## Installation

### Velopack Installer (Recommended)

1. Download `MouseEffects-win-Setup.exe` from [Releases](https://github.com/ltrudu/MouseEffects/releases)
2. Run the installer - no administrator rights required
3. The app installs to `%LocalAppData%\MouseEffects` and updates automatically

### MSIX Package (Enterprise/Store)

1. Download the latest `.msix` from releases
2. Install the developer certificate (if not using a trusted certificate)
3. Double-click the `.msix` file

### Portable Version

1. Download `MouseEffects-{version}-win-full.nupkg` from releases
2. Rename to `.zip` and extract to any folder
3. Run `MouseEffects.App.exe`

### From Source

1. Clone the repository
2. Build with `dotnet build`
3. Run `MouseEffects.App.exe`

## Configuration

Settings are stored in:

```
%APPDATA%\MouseEffects\
├── settings.json           # Application settings
└── plugins\
    ├── particle-trail.json # Per-plugin settings
    ├── laser-work.json
    └── ...
```

### Application Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `SelectedGpuName` | string | null | GPU to use (null = auto) |
| `TargetFrameRate` | int | 60 | Frame rate limit (30-120) |
| `ShowFpsCounter` | bool | false | Show FPS in settings window |
| `ShowFpsOverlay` | bool | false | Show FPS on screen overlay |
| `UpdateCheckFrequency` | string | OnStartup | When to check: OnStartup, Daily, Weekly, Never |
| `UpdateMode` | string | Notify | How to handle updates: Silent, Notify |
| `IncludePreReleases` | bool | false | Include pre-release versions |
| `LastUpdateCheck` | DateTime | null | Last update check timestamp |

## Getting Help

- Check the [Plugins Reference](Plugins.md) for effect-specific settings
- Review the [Architecture](Architecture.md) for technical details
- Open an issue on GitHub for bugs or feature requests
