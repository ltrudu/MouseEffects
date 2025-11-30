# Velopack Packaging

This guide explains how to create distributable packages using Velopack, the modern installer and auto-update framework used by MouseEffects.

## Overview

Velopack provides:

- **No-admin installer** - Installs to user profile
- **Automatic updates** - Via GitHub Releases
- **Delta updates** - Only download changed files
- **No certificate required** - Unlike MSIX

## Prerequisites

### Install Velopack CLI

```bash
dotnet tool install -g vpk
```

### Verify Installation

```bash
vpk --version
```

## Building Packages Locally

### Step 1: Publish the Application

```bash
dotnet publish src/MouseEffects.App/MouseEffects.App.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output ./publish/win-x64
```

### Step 2: Create Velopack Package

```bash
vpk pack `
    --packId MouseEffects `
    --packVersion 1.0.3 `
    --packDir ./publish/win-x64 `
    --mainExe MouseEffects.App.exe `
    --outputDir ./releases
```

### Step 3: Package Contents

After running `vpk pack`, you'll find:

```
releases/
├── MouseEffects-win-Setup.exe      # Installer for new users
├── MouseEffects-1.0.3-win-full.nupkg   # Full update package
├── MouseEffects-1.0.3-win-delta.nupkg  # Delta update (if previous version exists)
└── RELEASES                        # Manifest file for updates
```

## GitHub Actions (Automated)

MouseEffects uses GitHub Actions for automated releases. When you push a version tag, the workflow automatically builds and publishes packages.

### Trigger a Release

```bash
# Update version in csproj first
git add .
git commit -m "chore: bump version to 1.0.4"
git tag v1.0.4
git push origin main --tags
```

### Workflow File

The workflow is defined in `.github/workflows/release.yml`:

```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Install Velopack CLI
        run: dotnet tool install -g vpk

      - name: Publish Application
        run: dotnet publish src/MouseEffects.App -c Release -r win-x64 --self-contained

      - name: Create Velopack Release
        run: |
          vpk pack --packId MouseEffects --packVersion ${{ github.ref_name }} \
            --packDir ./publish/win-x64 --mainExe MouseEffects.App.exe \
            --outputDir ./releases

      - name: Upload to GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: releases/*
```

### Manual Trigger

You can also trigger a release manually:

1. Go to Actions tab in GitHub
2. Select "Build and Release" workflow
3. Click "Run workflow"
4. Enter the version number

## Version Management

### Version in csproj

Update the version in `src/MouseEffects.App/MouseEffects.App.csproj`:

```xml
<PropertyGroup>
    <Version>1.0.4</Version>
    <AssemblyVersion>1.0.4.0</AssemblyVersion>
    <FileVersion>1.0.4.0</FileVersion>
</PropertyGroup>
```

### Semantic Versioning

Follow semantic versioning (MAJOR.MINOR.PATCH):

| Change Type | Version Bump | Example |
|-------------|--------------|---------|
| Bug fix | PATCH | 1.0.3 → 1.0.4 |
| New feature | MINOR | 1.0.4 → 1.1.0 |
| Breaking change | MAJOR | 1.1.0 → 2.0.0 |

### Pre-release Versions

For beta/preview releases:

```bash
git tag v1.1.0-beta.1
git push origin v1.1.0-beta.1
```

Pre-releases are marked as such on GitHub and won't auto-update users (unless they enable pre-releases).

## Package Options

### Common `vpk pack` Options

| Option | Description |
|--------|-------------|
| `--packId` | Application identifier |
| `--packVersion` | Version number |
| `--packDir` | Directory containing published app |
| `--mainExe` | Main executable name |
| `--outputDir` | Where to place output files |
| `--packTitle` | Display name in installer |
| `--icon` | Path to .ico file for installer |
| `--splashImage` | Splash screen during install |

### Example with All Options

```bash
vpk pack `
    --packId MouseEffects `
    --packVersion 1.0.4 `
    --packDir ./publish/win-x64 `
    --mainExe MouseEffects.App.exe `
    --outputDir ./releases `
    --packTitle "MouseEffects" `
    --icon ./src/MouseEffects.App/Images/app.ico `
    --splashImage ./assets/splash.png
```

## Delta Updates

Velopack automatically creates delta updates when you have a previous release:

1. Download previous `RELEASES` file to your `--outputDir`
2. Run `vpk pack` with same `--outputDir`
3. Delta packages are created automatically

```bash
# Download previous RELEASES
curl -L https://github.com/ltrudu/MouseEffects/releases/latest/download/RELEASES -o ./releases/RELEASES

# Create new package (will include delta)
vpk pack --packId MouseEffects --packVersion 1.0.4 ...
```

## Plugins Folder

The `plugins/` folder is automatically included because:

1. Plugins are built as project references
2. They output to `bin/Release/net8.0-windows/plugins/`
3. `dotnet publish` includes the entire output directory
4. Velopack packages everything in the publish directory

Verify plugins are included:

```bash
# After publishing, check contents
ls ./publish/win-x64/plugins/
```

Expected output:
```
MouseEffects.Effects.ParticleTrail.dll
MouseEffects.Effects.LaserWork.dll
MouseEffects.Effects.ScreenDistortion.dll
MouseEffects.Effects.ColorBlindness.dll
MouseEffects.Effects.RadialDithering.dll
MouseEffects.Effects.TileVibration.dll
```

## Testing Updates Locally

### Create Test Release

1. Build version 1.0.0:
   ```bash
   dotnet publish ... -p:Version=1.0.0
   vpk pack --packVersion 1.0.0 ...
   ```

2. Install it using Setup.exe

3. Build version 1.0.1:
   ```bash
   dotnet publish ... -p:Version=1.0.1
   vpk pack --packVersion 1.0.1 ...
   ```

4. Host files locally (e.g., with Python):
   ```bash
   cd releases
   python -m http.server 8080
   ```

5. Modify `UpdateService.cs` temporarily to use local URL:
   ```csharp
   var source = new SimpleWebSource("http://localhost:8080");
   ```

6. Run installed app and test update flow

## Troubleshooting

### Package Creation Fails

**Missing mainExe**:
```
Error: Could not find main executable
```
Solution: Verify `--mainExe` matches actual exe name

**Invalid version**:
```
Error: Version must be a valid semantic version
```
Solution: Use format like `1.0.3` not `v1.0.3`

### Updates Not Detected

1. Check `RELEASES` file is uploaded to GitHub Release
2. Verify version in RELEASES matches expected
3. Check app's current version matches what you expect

### Delta Creation Fails

- Ensure previous `RELEASES` file exists in output directory
- Delta is optional - full package always works

## Comparison: Velopack vs MSIX

| Feature | Velopack | MSIX |
|---------|----------|------|
| Certificate | Not required | Required |
| Admin rights | Not needed | Not needed |
| Auto-updates | Built-in | Store only |
| Delta updates | Yes | Yes |
| Hosting | GitHub/Any | Store/MSIX |
| Enterprise | GitHub | Group Policy |
| Sandbox | No | Yes |
| Store | No | Yes |

## Next Steps

- [Auto-Updates](Auto-Updates.md) - User guide for updates
- [Building from Source](Building.md) - Development setup
- [MSIX Packaging](MSIX-Packaging.md) - Alternative packaging
