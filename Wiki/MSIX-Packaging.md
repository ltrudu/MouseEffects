# MSIX Packaging

This guide explains how to create MSIX packages for MouseEffects distribution.

## Overview

MSIX is the modern Windows application packaging format that provides:

- **Clean Installation** - No registry pollution
- **Easy Updates** - Seamless app updates
- **Sandboxing** - App isolation for security
- **Store Ready** - Compatible with Microsoft Store

## Prerequisites

### Required Tools

| Tool | Purpose | Location |
|------|---------|----------|
| Windows SDK | makeappx.exe, signtool.exe | `C:\Program Files (x86)\Windows Kits\10\bin\` |
| .NET SDK 8.0 | Building the application | [Download](https://dotnet.microsoft.com/) |
| PowerShell 5.1+ | Running build scripts | Built into Windows |

### Certificate

You need a code signing certificate. See [Certificate Management](Certificates.md) for details.

## Quick Start

### Using the Build Script

The easiest way to create an MSIX package:

```powershell
cd MouseEffects
.\packaging\build-msix.ps1 -Platform x64
```

This script:
1. Builds the solution in Release mode
2. Publishes as self-contained
3. Copies manifest and assets
4. Copies plugins
5. Creates MSIX package
6. Signs with your certificate

### Output

```
packaging/output/
└── MouseEffects_x64.msix
```

## Manual Packaging

### Step 1: Build and Publish

```powershell
# Build the solution
dotnet build MouseEffects.sln -c Release -p:Platform=x64

# Publish self-contained
dotnet publish src/MouseEffects.App/MouseEffects.App.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true
```

### Step 2: Prepare Package Contents

```powershell
$publishFolder = "src\MouseEffects.App\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64"

# Copy manifest (rename to AppxManifest.xml)
Copy-Item "src\MouseEffects.App\Package.appxmanifest" `
    -Destination "$publishFolder\AppxManifest.xml"

# Copy logo images
Copy-Item "src\MouseEffects.App\Images\*" `
    -Destination "$publishFolder\Images\" -Recurse

# Copy plugins
Copy-Item "src\MouseEffects.App\bin\Release\net8.0-windows\plugins\*" `
    -Destination "$publishFolder\plugins\" -Recurse
```

### Step 3: Create MSIX Package

```powershell
# Find makeappx.exe
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe"

# Create package
& $makeappx pack `
    /d $publishFolder `
    /p "packaging\output\MouseEffects.msix" `
    /o
```

### Step 4: Sign the Package

```powershell
# Find signtool.exe
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"

# Sign with certificate thumbprint
& $signtool sign `
    /sha1 YOUR_CERTIFICATE_THUMBPRINT `
    /fd SHA256 `
    "packaging\output\MouseEffects.msix"
```

## Package Manifest

The `Package.appxmanifest` defines your app identity:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
         IgnorableNamespaces="uap rescap">

  <Identity Name="MouseEffects"
            Publisher="CN=MouseEffects Dev"
            Version="1.0.0.0"
            ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>MouseEffects</DisplayName>
    <PublisherDisplayName>MouseEffects</PublisherDisplayName>
    <Logo>Images\StoreLogo.png</Logo>
    <Description>Mouse cursor visual effects overlay application</Description>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop"
                        MinVersion="10.0.17763.0"
                        MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Applications>
    <Application Id="App"
                 Executable="MouseEffects.App.exe"
                 EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="MouseEffects"
                          Description="Mouse cursor visual effects overlay"
                          BackgroundColor="transparent"
                          Square150x150Logo="Images\Square150x150Logo.png"
                          Square44x44Logo="Images\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Images\Wide310x150Logo.png"
                         Square310x310Logo="Images\LargeTile.png"
                         ShortName="MouseEffects">
        </uap:DefaultTile>
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
```

### Key Elements

| Element | Description |
|---------|-------------|
| `Identity.Name` | Unique app identifier |
| `Identity.Publisher` | Must match certificate subject |
| `Identity.Version` | App version (Major.Minor.Build.Revision) |
| `Properties.DisplayName` | Name shown in Start menu |
| `TargetDeviceFamily` | Windows version requirements |
| `runFullTrust` | Required for desktop apps |

## Required Assets

### Logo Images

| Image | Size | Usage |
|-------|------|-------|
| `StoreLogo.png` | 50x50 | Store listing |
| `Square44x44Logo.png` | 44x44 | Taskbar, small tiles |
| `Square150x150Logo.png` | 150x150 | Start menu tile |
| `Wide310x150Logo.png` | 310x150 | Wide tile |
| `LargeTile.png` | 310x310 | Large tile |

### Creating Placeholder Images

```powershell
# Generate placeholder logos with Python
python packaging/MouseEffects.Package/create_logos.py
```

## Version Management

### Updating Version

1. Edit `Package.appxmanifest`:
   ```xml
   <Identity Version="1.0.1.0" ... />
   ```

2. Rebuild the package

### Version Format

- **Major.Minor.Build.Revision**
- Each component: 0-65535
- Must increase for updates

## Multi-Platform Builds

### Building for Multiple Architectures

```powershell
# Build x64
.\packaging\build-msix.ps1 -Platform x64

# Build x86
.\packaging\build-msix.ps1 -Platform x86

# Build ARM64
.\packaging\build-msix.ps1 -Platform ARM64
```

### Creating a Bundle

```powershell
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe"

& $makeappx bundle `
    /d "packaging\output" `
    /p "packaging\output\MouseEffects.msixbundle" `
    /o
```

## Distribution Options

### Sideloading

For direct distribution:

1. Export and share your certificate (`.cer` file)
2. Users install the certificate
3. Users install the `.msix` package

### Microsoft Store

For Store distribution:

1. Create a Partner Center account
2. Reserve your app name
3. Update manifest with Store identity
4. Submit the package for certification

### App Installer

Create `.appinstaller` file for auto-updates:

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller Uri="https://example.com/MouseEffects.appinstaller"
              Version="1.0.0.0"
              xmlns="http://schemas.microsoft.com/appx/appinstaller/2018">
  <MainPackage Name="MouseEffects"
               Publisher="CN=MouseEffects Dev"
               Version="1.0.0.0"
               Uri="https://example.com/MouseEffects.msix"
               ProcessorArchitecture="x64"/>
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="0"/>
  </UpdateSettings>
</AppInstaller>
```

## Troubleshooting

### Package Validation Errors

Use Windows App Certification Kit:
```powershell
# Validate package
appcert.exe test -appxpackagepath MouseEffects.msix -reportoutputpath report.xml
```

### Signature Errors

Verify signature:
```powershell
signtool verify /pa MouseEffects.msix
```

Common issues:
- Certificate not trusted → Install to Trusted Root
- Publisher mismatch → Update manifest Identity.Publisher
- Timestamp expired → Use timestamping when signing

### Installation Failures

Check Event Viewer:
1. Windows Logs → Application
2. Filter by Source: "AppXDeployment-Server"

## Build Script Reference

### Parameters

```powershell
.\packaging\build-msix.ps1 `
    -Configuration Release `     # Build configuration
    -Platform x64 `              # Target platform
    -CertThumbprint "ABC123..."  # Certificate thumbprint
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| `MSIX_CERT_THUMBPRINT` | Default certificate thumbprint |
| `MSIX_OUTPUT_DIR` | Custom output directory |

## Next Steps

- [Certificate Management](Certificates.md) - Set up code signing
- [Building from Source](Building.md) - Build the application
