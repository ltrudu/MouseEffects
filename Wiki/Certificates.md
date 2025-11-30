# Certificate Management

This guide explains how to create and manage code signing certificates for MouseEffects.

## Overview

MSIX packages must be signed with a valid certificate. The certificate's publisher name must match the `Publisher` attribute in the package manifest.

## Certificate Types

| Type | Use Case | Trust Level |
|------|----------|-------------|
| **Self-Signed** | Development, internal testing | Manual trust installation |
| **Enterprise** | Corporate deployment | Trusted via Group Policy |
| **EV Code Signing** | Public distribution | Trusted by Windows |
| **Microsoft Store** | Store apps | Signed by Microsoft |

## Self-Signed Certificate (Development)

### Creating the Certificate

Run PowerShell as Administrator:

```powershell
# Create self-signed certificate
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject "CN=MouseEffects Dev" `
    -KeyUsage DigitalSignature `
    -FriendlyName "MouseEffects Development" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @(
        "2.5.29.37={text}1.3.6.1.5.5.7.3.3",  # Code Signing
        "2.5.29.19={text}"                     # Basic Constraints
    )

# Display thumbprint
Write-Host "Certificate Thumbprint: $($cert.Thumbprint)"
```

### Saving the Thumbprint

Add the thumbprint to your project:

1. Copy the thumbprint output
2. Add to `MouseEffects.App.csproj`:
   ```xml
   <PackageCertificateThumbprint>YOUR_THUMBPRINT</PackageCertificateThumbprint>
   ```

Or update `packaging\build-msix.ps1` default parameter.

### Exporting for Distribution

Export the public certificate for users to install:

```powershell
# Export public certificate (.cer)
$thumbprint = "YOUR_THUMBPRINT"
$cert = Get-ChildItem -Path "Cert:\CurrentUser\My\$thumbprint"
Export-Certificate -Cert $cert -FilePath "MouseEffects-Dev.cer"
```

Or use the provided script:
```powershell
.\packaging\export-cert.ps1
```

### Installing on User Machines

Users must install your certificate before they can install the MSIX:

#### Method 1: GUI Installation

1. Double-click `MouseEffects-Dev.cer`
2. Click **Install Certificate**
3. Select **Local Machine**
4. Select **Place all certificates in the following store**
5. Click **Browse** → Select **Trusted Root Certification Authorities**
6. Click **Next** → **Finish**

#### Method 2: PowerShell (Admin)

```powershell
# Import certificate to Trusted Root
Import-Certificate `
    -FilePath "MouseEffects-Dev.cer" `
    -CertStoreLocation "Cert:\LocalMachine\Root"
```

#### Method 3: Command Line (Admin)

```cmd
certutil -addstore Root MouseEffects-Dev.cer
```

## Certificate Locations

Windows stores certificates in different locations:

| Store | Path | Purpose |
|-------|------|---------|
| Current User - Personal | `Cert:\CurrentUser\My` | Your signing certificates |
| Local Machine - Root | `Cert:\LocalMachine\Root` | Trusted root CAs |
| Local Machine - TrustedPublisher | `Cert:\LocalMachine\TrustedPublisher` | Trusted code signers |

### Viewing Certificates

```powershell
# List your signing certificates
Get-ChildItem -Path "Cert:\CurrentUser\My" -CodeSigningCert

# List trusted roots
Get-ChildItem -Path "Cert:\LocalMachine\Root"
```

Or use the Certificate Manager GUI:
```cmd
certmgr.msc
```

## Signing Packages

### Using signtool.exe

```powershell
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"

# Sign with thumbprint
& $signtool sign `
    /sha1 YOUR_THUMBPRINT `
    /fd SHA256 `
    /td SHA256 `
    /tr http://timestamp.digicert.com `
    MouseEffects.msix
```

### Signing Options

| Option | Description |
|--------|-------------|
| `/sha1` | Certificate thumbprint |
| `/fd SHA256` | File digest algorithm |
| `/td SHA256` | Timestamp digest algorithm |
| `/tr` | Timestamp server URL |

### Timestamp Servers

Always use a timestamp server for production:

| Provider | URL |
|----------|-----|
| DigiCert | `http://timestamp.digicert.com` |
| Sectigo | `http://timestamp.sectigo.com` |
| GlobalSign | `http://timestamp.globalsign.com/tsa/r6advanced1` |

Timestamping ensures signatures remain valid after certificate expires.

## Verifying Signatures

### Check Package Signature

```powershell
signtool verify /pa /v MouseEffects.msix
```

### Check Certificate Details

```powershell
$cert = Get-ChildItem -Path "Cert:\CurrentUser\My\YOUR_THUMBPRINT"
$cert | Format-List *
```

## Production Certificates

### EV Code Signing Certificate

For public distribution, purchase an EV code signing certificate:

| Provider | Approximate Cost |
|----------|------------------|
| DigiCert | $400-500/year |
| Sectigo | $300-400/year |
| GlobalSign | $350-450/year |

**Benefits**:
- Immediate Windows SmartScreen reputation
- No user prompts about unknown publisher
- Required for kernel-mode drivers

### Azure Key Vault

For team environments, store certificates in Azure Key Vault:

```powershell
# Install Azure SignTool
dotnet tool install -g AzureSignTool

# Sign with Key Vault
AzureSignTool sign `
    --azure-key-vault-url "https://yourkeyvault.vault.azure.net" `
    --azure-key-vault-client-id "your-client-id" `
    --azure-key-vault-client-secret "your-secret" `
    --azure-key-vault-certificate "your-cert-name" `
    --timestamp-rfc3161 "http://timestamp.digicert.com" `
    MouseEffects.msix
```

## Matching Publisher Names

The certificate subject must match the manifest publisher:

### Certificate Subject
```
CN=MouseEffects Dev
```

### Manifest Publisher
```xml
<Identity Publisher="CN=MouseEffects Dev" ... />
```

### Finding Certificate Subject

```powershell
$cert = Get-ChildItem -Path "Cert:\CurrentUser\My\YOUR_THUMBPRINT"
$cert.Subject
```

## Troubleshooting

### "A certificate chain could not be built"

The certificate or its issuer isn't trusted:

```powershell
# Check certificate chain
certutil -verify -urlfetch MouseEffects-Dev.cer
```

**Solution**: Install certificate to Trusted Root store.

### "The signature is invalid"

Package was modified after signing, or certificate doesn't match:

1. Verify publisher matches certificate subject
2. Re-sign the package
3. Check for file corruption

### "Publisher identity doesn't match"

Manifest publisher doesn't match certificate:

```powershell
# Get certificate subject
(Get-ChildItem "Cert:\CurrentUser\My\$thumbprint").Subject

# Update manifest to match
```

### Certificate Expired

For development, create a new certificate:

```powershell
# New certificate valid for 5 years
New-SelfSignedCertificate ... -NotAfter (Get-Date).AddYears(5)
```

## Security Best Practices

1. **Never share private keys** - Only distribute `.cer` files (public key)
2. **Use strong key sizes** - 2048-bit RSA minimum
3. **Protect with passwords** - Export `.pfx` files with passwords
4. **Use timestamping** - Signatures remain valid after certificate expires
5. **Store securely** - Use Azure Key Vault or HSM for production
6. **Rotate regularly** - Create new certificates before expiration

## Quick Reference

### Create Certificate
```powershell
New-SelfSignedCertificate -Type Custom -Subject "CN=Your Name" `
    -KeyUsage DigitalSignature -FriendlyName "Your App" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3","2.5.29.19={text}")
```

### Export Public Key
```powershell
Export-Certificate -Cert (Get-Item "Cert:\CurrentUser\My\THUMBPRINT") -FilePath "cert.cer"
```

### Install Certificate (Admin)
```powershell
Import-Certificate -FilePath "cert.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```

### Sign Package
```powershell
signtool sign /sha1 THUMBPRINT /fd SHA256 package.msix
```

## Next Steps

- [MSIX Packaging](MSIX-Packaging.md) - Create distributable packages
- [Building from Source](Building.md) - Build the application
