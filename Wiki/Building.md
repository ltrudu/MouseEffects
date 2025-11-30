# Building from Source

This guide explains how to build MouseEffects from source code.

## Prerequisites

### Required Software

| Software | Version | Download |
|----------|---------|----------|
| Visual Studio 2022 | 17.8+ | [Download](https://visualstudio.microsoft.com/) |
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download) |
| Windows SDK | 10.0.19041.0+ | Included with VS |

### Visual Studio Workloads

Install these workloads via Visual Studio Installer:

- **.NET Desktop Development**
- **Desktop Development with C++** (for Windows SDK)

### Optional Tools

- **Git** - For cloning the repository
- **PowerShell 7+** - For build scripts

## Getting the Source

### Clone the Repository

```bash
git clone https://github.com/yourusername/MouseEffects.git
cd MouseEffects
```

### Repository Structure

```
MouseEffects/
├── src/                      # Core application projects
│   ├── MouseEffects.Core/
│   ├── MouseEffects.DirectX/
│   ├── MouseEffects.Input/
│   ├── MouseEffects.Overlay/
│   ├── MouseEffects.Plugins/
│   └── MouseEffects.App/
├── plugins/                  # Effect plugins
│   ├── MouseEffects.Effects.ParticleTrail/
│   ├── MouseEffects.Effects.LaserWork/
│   └── ...
├── packaging/                # MSIX packaging
└── wiki/                     # Documentation
```

## Building with Visual Studio

### Open Solution

1. Open `MouseEffects.sln` in Visual Studio 2022
2. Wait for NuGet packages to restore automatically

### Build Configuration

| Configuration | Use Case |
|--------------|----------|
| Debug | Development and debugging |
| Release | Production builds |

### Build Steps

1. Select configuration (Debug/Release)
2. Select platform (x64 recommended)
3. Build → Build Solution (Ctrl+Shift+B)

### Output Location

After building, find outputs at:

```
src/MouseEffects.App/bin/{Configuration}/net8.0-windows/
├── MouseEffects.App.exe
├── MouseEffects.*.dll
└── plugins/
    ├── MouseEffects.Effects.ParticleTrail.dll
    ├── MouseEffects.Effects.LaserWork.dll
    └── ...
```

## Building with Command Line

### Using .NET CLI

```bash
# Restore packages
dotnet restore

# Build Debug
dotnet build

# Build Release
dotnet build -c Release

# Build specific platform
dotnet build -c Release -p:Platform=x64
```

### Using MSBuild

```bash
# Build entire solution
msbuild MouseEffects.sln /p:Configuration=Release /p:Platform=x64

# Build specific project
msbuild src/MouseEffects.App/MouseEffects.App.csproj /p:Configuration=Release
```

## Running the Application

### From Visual Studio

1. Set `MouseEffects.App` as startup project
2. Press F5 (Debug) or Ctrl+F5 (Run without debugging)

### From Command Line

```bash
# Navigate to output directory
cd src/MouseEffects.App/bin/Release/net8.0-windows

# Run the application
./MouseEffects.App.exe
```

## Building Plugins

Plugins are built automatically with the solution. To build a single plugin:

```bash
dotnet build plugins/MouseEffects.Effects.ParticleTrail/MouseEffects.Effects.ParticleTrail.csproj
```

### Plugin Output

Plugins are automatically copied to the app's plugins folder during build.

## Build Troubleshooting

### Common Issues

#### NuGet Restore Fails

```bash
# Clear NuGet cache and restore
dotnet nuget locals all --clear
dotnet restore
```

#### DirectX Shader Compilation Errors

Ensure Windows SDK is installed:
1. Open Visual Studio Installer
2. Modify your VS installation
3. Check "Windows 10 SDK" under Individual Components

#### Missing .NET 8 Runtime

Install the .NET 8 SDK:
```bash
winget install Microsoft.DotNet.SDK.8
```

#### Platform Target Mismatch

Ensure all projects target the same platform:
```bash
dotnet build -p:Platform=x64
```

### Clean Build

If experiencing strange issues, perform a clean build:

```bash
# Clean all outputs
dotnet clean

# Remove bin/obj directories
Get-ChildItem -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force

# Rebuild
dotnet build
```

## Development Workflow

### Recommended Workflow

1. **Create feature branch**
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Make changes and build**
   ```bash
   dotnet build
   ```

3. **Test changes**
   - Run application
   - Verify effects work correctly
   - Check for memory leaks

4. **Commit and push**
   ```bash
   git add .
   git commit -m "feat: add new feature"
   git push origin feature/my-new-feature
   ```

### Debugging Tips

#### Enable DirectX Debug Layer

For detailed graphics debugging, enable the DirectX debug layer:

1. Install "Graphics Tools" Windows optional feature
2. Set environment variable: `DXGI_DEBUG=1`
3. Check Visual Studio Output window for D3D messages

#### Shader Debugging

Use Visual Studio Graphics Debugger:
1. Debug → Graphics → Start Graphics Debugging
2. Capture a frame
3. Inspect shader execution step by step

## Publishing

### Self-Contained Publish

Create a self-contained deployment (includes .NET runtime):

```bash
dotnet publish src/MouseEffects.App/MouseEffects.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o ./publish
```

### Framework-Dependent Publish

Smaller deployment (requires .NET 8 runtime installed):

```bash
dotnet publish src/MouseEffects.App/MouseEffects.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o ./publish
```

### Output Contents

```
publish/
├── MouseEffects.App.exe
├── MouseEffects.App.dll
├── MouseEffects.*.dll
├── plugins/
│   └── *.dll
└── (runtime files if self-contained)
```

## Creating Installers

### Velopack Installer (Recommended)

Create a portable installer with auto-updates:

```bash
# Install Velopack CLI
dotnet tool install -g vpk

# Publish self-contained
dotnet publish src/MouseEffects.App/MouseEffects.App.csproj `
    -c Release -r win-x64 --self-contained true -o ./publish

# Create installer
vpk pack --packId MouseEffects --packVersion 1.0.3 `
    --packDir ./publish --mainExe MouseEffects.App.exe `
    --outputDir ./releases
```

Output:
- `MouseEffects-win-Setup.exe` - User installer (no admin)
- `MouseEffects-1.0.3-win-full.nupkg` - Update package
- `RELEASES` - Update manifest

See [Velopack Packaging](Velopack-Packaging.md) for detailed instructions.

### MSIX Package

For enterprise or Microsoft Store distribution:

```bash
# Use the packaging script
./packaging/build-msix.ps1
```

See [MSIX Packaging](MSIX-Packaging.md) for detailed instructions.

## Next Steps

After building successfully:

- [Create Velopack Installer](Velopack-Packaging.md) for GitHub distribution
- [Create MSIX Package](MSIX-Packaging.md) for Store/Enterprise distribution
- [Develop Custom Plugins](Plugin-Development.md)
- [Set Up Code Signing](Certificates.md)
