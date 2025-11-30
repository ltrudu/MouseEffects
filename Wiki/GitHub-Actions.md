# GitHub Actions CI/CD

This guide explains how the automated build and release system works for MouseEffects using GitHub Actions.

## Table of Contents

- [Overview](#overview)
- [How GitHub Actions Works](#how-github-actions-works)
- [Workflow File Structure](#workflow-file-structure)
- [Triggers](#triggers)
- [Workflow Steps Explained](#workflow-steps-explained)
- [Creating a Release](#creating-a-release)
- [Manual Workflow Dispatch](#manual-workflow-dispatch)
- [Understanding the Output](#understanding-the-output)
- [Troubleshooting](#troubleshooting)
- [Customizing the Workflow](#customizing-the-workflow)
- [Best Practices](#best-practices)

## Overview

GitHub Actions is a CI/CD (Continuous Integration/Continuous Deployment) platform built into GitHub. It automates tasks like building, testing, and releasing software whenever certain events occur in your repository.

For MouseEffects, the workflow:
1. Triggers when you push a version tag (e.g., `v1.0.4`)
2. Builds the application for Windows x64
3. Creates a Velopack installer package
4. Publishes a GitHub Release with downloadable files

## How GitHub Actions Works

### Key Concepts

| Concept | Description |
|---------|-------------|
| **Workflow** | An automated process defined in a YAML file in `.github/workflows/` |
| **Event** | Something that triggers a workflow (push, tag, manual, etc.) |
| **Job** | A set of steps that run on the same runner |
| **Step** | An individual task within a job |
| **Runner** | A virtual machine that executes your workflow |
| **Action** | A reusable unit of code (e.g., `actions/checkout@v4`) |

### Workflow Location

Workflows are stored in:
```
.github/
└── workflows/
    └── release.yml    # Our build and release workflow
```

GitHub automatically detects and runs workflows from this directory.

## Workflow File Structure

The workflow file (`.github/workflows/release.yml`) has this structure:

```yaml
name: Build and Release          # Display name in GitHub UI

on:                              # TRIGGERS - When to run
  push:
    tags:
      - 'v*'                     # Run on tags starting with 'v'
  workflow_dispatch:             # Allow manual triggering

env:                             # ENVIRONMENT VARIABLES
  DOTNET_VERSION: '8.0.x'
  PROJECT_PATH: 'src/MouseEffects.App/MouseEffects.App.csproj'

jobs:                            # JOBS - What to do
  build:
    runs-on: windows-latest      # Use Windows runner
    steps:
      - name: Step name
        uses: action@version     # Use a pre-built action
        # OR
        run: command             # Run a shell command
```

## Triggers

### Tag Push Trigger (Primary)

The workflow runs automatically when you push a tag starting with `v`:

```yaml
on:
  push:
    tags:
      - 'v*'    # Matches: v1.0.0, v2.1.3, v1.0.0-beta, etc.
```

**How to trigger:**
```bash
# Create a tag
git tag v1.0.4

# Push the tag to GitHub
git push origin v1.0.4
```

**Important:** The workflow file must exist on GitHub BEFORE you push the tag. If you push the tag first, the workflow won't trigger.

### Manual Dispatch Trigger

You can also trigger the workflow manually from the GitHub UI:

```yaml
workflow_dispatch:
  inputs:
    version:
      description: 'Version number (e.g., 1.0.3)'
      required: true
      type: string
```

**How to use:**
1. Go to **Actions** tab on GitHub
2. Select "Build and Release" workflow
3. Click "Run workflow"
4. Enter the version number
5. Click "Run workflow"

## Workflow Steps Explained

### Step 1: Checkout Code

```yaml
- name: Checkout code
  uses: actions/checkout@v4
  with:
    fetch-depth: 0    # Full history needed for versioning
```

Downloads your repository code to the runner. `fetch-depth: 0` fetches all history and tags.

### Step 2: Setup .NET

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}
```

Installs the specified .NET SDK version on the runner.

### Step 3: Determine Version

```yaml
- name: Determine version
  id: version
  shell: pwsh
  run: |
    if ("${{ github.event.inputs.version }}" -ne "") {
      $version = "${{ github.event.inputs.version }}"
    } else {
      $version = "${{ github.ref_name }}".TrimStart('v')
    }
    echo "VERSION=$version" >> $env:GITHUB_OUTPUT
```

Extracts the version number from:
- The manual input (if workflow was triggered manually)
- The tag name (removes the `v` prefix: `v1.0.4` → `1.0.4`)

The `id: version` allows other steps to reference this output as `${{ steps.version.outputs.VERSION }}`.

### Step 4: Restore Dependencies

```yaml
- name: Restore dependencies
  run: dotnet restore
```

Downloads NuGet packages required by the solution.

### Step 5: Build Solution

```yaml
- name: Build solution
  run: dotnet build --configuration Release --no-restore
```

Compiles all projects in Release configuration.

### Step 6: Publish Application

```yaml
- name: Publish application (x64)
  run: |
    dotnet publish ${{ env.PROJECT_PATH }} `
      --configuration Release `
      --runtime win-x64 `
      --self-contained true `
      --output ./publish/win-x64 `
      -p:PublishSingleFile=false `
      -p:Version=${{ steps.version.outputs.VERSION }}
```

Creates a publishable output with:
- `--runtime win-x64`: Target Windows 64-bit
- `--self-contained true`: Include .NET runtime (no .NET install needed)
- `-p:Version=...`: Sets the assembly version

### Step 7: Install Velopack CLI

```yaml
- name: Install Velopack CLI
  run: dotnet tool install -g vpk
```

Installs the Velopack command-line tool for creating installers.

### Step 8: Create Velopack Package

```yaml
- name: Create Velopack release
  shell: pwsh
  run: |
    vpk pack `
      --packId MouseEffects `
      --packVersion ${{ steps.version.outputs.VERSION }} `
      --packDir ./publish/win-x64 `
      --mainExe MouseEffects.App.exe `
      --outputDir ./releases `
      --packTitle "MouseEffects" `
      --icon ./src/MouseEffects.App/Images/StoreLogo.png
```

Creates:
- `MouseEffects-win-Setup.exe` - Installer executable
- `MouseEffects-{version}-win-full.nupkg` - Full update package
- `RELEASES` - Metadata file for auto-updates

### Step 9: Upload Artifacts

```yaml
- name: Upload build artifacts
  uses: actions/upload-artifact@v4
  with:
    name: MouseEffects-${{ steps.version.outputs.VERSION }}
    path: ./releases/*
    retention-days: 30
```

Stores build outputs in GitHub for 30 days. Useful for debugging or downloading without a release.

### Step 10: Create GitHub Release

```yaml
- name: Create GitHub Release
  if: startsWith(github.ref, 'refs/tags/v')
  uses: softprops/action-gh-release@v2
  with:
    name: MouseEffects v${{ steps.version.outputs.VERSION }}
    draft: false
    prerelease: ${{ contains(github.ref_name, '-') }}
    generate_release_notes: true
    files: |
      ./releases/MouseEffects-win-Setup.exe
      ./releases/MouseEffects-${{ steps.version.outputs.VERSION }}-win-full.nupkg
      ./releases/RELEASES
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

Creates a public release with:
- Auto-generated release notes from commits
- Pre-release flag if tag contains `-` (e.g., `v1.0.4-beta`)
- Attached installer and update files

## Creating a Release

### Step-by-Step Process

1. **Update version in code** (optional but recommended):
   ```xml
   <!-- In MouseEffects.App.csproj -->
   <Version>1.0.4</Version>
   ```

2. **Commit your changes**:
   ```bash
   git add .
   git commit -m "Release v1.0.4 - description of changes"
   ```

3. **Push commits to GitHub**:
   ```bash
   git push origin master
   ```

4. **Create and push a tag**:
   ```bash
   git tag v1.0.4
   git push origin v1.0.4
   ```

5. **Monitor the workflow**:
   - Go to https://github.com/ltrudu/MouseEffects/actions
   - Click on the running workflow to see live logs
   - Wait for completion (typically 2-5 minutes)

6. **Verify the release**:
   - Go to https://github.com/ltrudu/MouseEffects/releases
   - Download and test the installer

### Version Tag Formats

| Tag | Release Type | Example |
|-----|--------------|---------|
| `v1.0.0` | Stable release | Production-ready |
| `v1.0.0-alpha` | Alpha | Early testing |
| `v1.0.0-beta` | Beta | Feature complete, testing |
| `v1.0.0-rc1` | Release candidate | Final testing |

Tags with `-` in the name are automatically marked as pre-releases.

## Manual Workflow Dispatch

For testing or creating releases without tags:

1. Go to **Actions** → **Build and Release**
2. Click **Run workflow** dropdown
3. Enter version (e.g., `1.0.4`)
4. Click **Run workflow**

This creates a **draft release** that you can review before publishing.

## Understanding the Output

### Workflow Run Page

When viewing a workflow run, you'll see:

```
Build and Release
├── build (windows-latest)
│   ├── ✓ Set up job
│   ├── ✓ Checkout code
│   ├── ✓ Setup .NET
│   ├── ✓ Determine version
│   ├── ✓ Restore dependencies
│   ├── ✓ Build solution
│   ├── ✓ Publish application (x64)
│   ├── ✓ Install Velopack CLI
│   ├── ✓ Create Velopack release
│   ├── ✓ List release artifacts
│   ├── ✓ Upload build artifacts
│   ├── ✓ Create GitHub Release
│   └── ✓ Complete job
```

Click any step to see its detailed logs.

### Artifacts

Build artifacts are available for 30 days:
- Click on the workflow run
- Scroll to "Artifacts" section
- Download the zip file

### Release Assets

Published releases contain:

| File | Purpose |
|------|---------|
| `MouseEffects-win-Setup.exe` | User installer |
| `MouseEffects-{ver}-win-full.nupkg` | Full update package |
| `RELEASES` | Update metadata |

## Troubleshooting

### Workflow Doesn't Run

**Problem:** Pushed a tag but no workflow started.

**Solutions:**
1. Ensure workflow file exists on GitHub before pushing tag
2. Delete and re-push the tag:
   ```bash
   git push origin --delete v1.0.4
   git push origin v1.0.4
   ```
3. Check Actions are enabled: **Settings → Actions → General**

### Workflow Fails at Checkout

**Problem:** "Permission denied" or checkout errors.

**Solution:** Ensure `fetch-depth: 0` is set for full history.

### Build Fails

**Problem:** Compilation errors.

**Solutions:**
1. Test locally first: `dotnet build --configuration Release`
2. Check the error logs in the workflow run
3. Ensure all projects build on Windows

### Velopack Fails

**Problem:** `vpk pack` command fails.

**Solutions:**
1. Check the publish output exists
2. Verify the main executable name
3. Check icon file path is correct

### Release Not Created

**Problem:** Workflow succeeds but no release appears.

**Check:**
1. The `if:` condition: `startsWith(github.ref, 'refs/tags/v')`
2. GITHUB_TOKEN permissions
3. Files exist in the releases directory

### OAuth/Permission Errors

**Problem:** Cannot push workflow file.

**Solution:** Your GitHub token needs `workflow` scope:
1. Create new Personal Access Token with `workflow` scope
2. Update your credentials
3. Push again

## Customizing the Workflow

### Adding More Platforms

To build for x86 and ARM64:

```yaml
- name: Publish application (x86)
  run: |
    dotnet publish ${{ env.PROJECT_PATH }} `
      --configuration Release `
      --runtime win-x86 `
      --self-contained true `
      --output ./publish/win-x86

- name: Publish application (ARM64)
  run: |
    dotnet publish ${{ env.PROJECT_PATH }} `
      --configuration Release `
      --runtime win-arm64 `
      --self-contained true `
      --output ./publish/win-arm64
```

### Adding Tests

```yaml
- name: Run tests
  run: dotnet test --configuration Release --no-build
```

### Adding Code Signing

```yaml
- name: Sign executable
  run: |
    # Using Azure SignTool or similar
    signtool sign /f certificate.pfx /p ${{ secrets.CERT_PASSWORD }} ./publish/win-x64/MouseEffects.App.exe
```

### Parallel Jobs

For faster builds, use matrix strategy:

```yaml
jobs:
  build:
    strategy:
      matrix:
        runtime: [win-x64, win-x86, win-arm64]
    runs-on: windows-latest
    steps:
      - name: Publish
        run: dotnet publish --runtime ${{ matrix.runtime }}
```

### Notifications

Add Slack/Discord notifications:

```yaml
- name: Notify on success
  if: success()
  uses: 8398a7/action-slack@v3
  with:
    status: success
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

## Best Practices

### Version Control

1. **Always test locally before tagging**:
   ```bash
   dotnet build --configuration Release
   dotnet publish --configuration Release --runtime win-x64
   ```

2. **Use semantic versioning**: `MAJOR.MINOR.PATCH`
   - MAJOR: Breaking changes
   - MINOR: New features (backwards compatible)
   - PATCH: Bug fixes

3. **Write meaningful commit messages** - they become release notes

### Security

1. **Never commit secrets** - use GitHub Secrets
2. **Use `GITHUB_TOKEN`** - automatically provided, properly scoped
3. **Review third-party actions** before using

### Workflow Maintenance

1. **Pin action versions**: `uses: actions/checkout@v4` (not `@latest`)
2. **Test workflow changes** with manual dispatch first
3. **Keep workflows simple** - split complex workflows into multiple files

## Quick Reference

### Common Commands

```bash
# Create and push a release tag
git tag v1.0.4
git push origin v1.0.4

# Delete a tag (local and remote)
git tag -d v1.0.4
git push origin --delete v1.0.4

# List all tags
git tag -l

# View workflow runs (with gh CLI)
gh run list
gh run view <run-id>
```

### Useful Links

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Workflow Syntax Reference](https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions)
- [Available Runners](https://docs.github.com/en/actions/using-github-hosted-runners/about-github-hosted-runners)
- [Velopack Documentation](https://velopack.io/docs)

## See Also

- [Velopack Packaging](Velopack-Packaging.md) - Local Velopack package creation
- [Auto-Updates](Auto-Updates.md) - How auto-updates work
- [Building from Source](Building.md) - Local build instructions
