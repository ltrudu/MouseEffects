# Workflow File Reference

This document provides a detailed line-by-line explanation of the `.github/workflows/release.yml` file.

## Complete File with Annotations

```yaml
# ============================================================================
# WORKFLOW NAME
# ============================================================================
# This name appears in the GitHub Actions UI
name: Build and Release

# ============================================================================
# TRIGGERS (on:)
# ============================================================================
# Defines WHEN this workflow runs
on:
  # TRIGGER 1: Tag Push
  # Runs automatically when a tag matching the pattern is pushed
  push:
    tags:
      - 'v*'    # Pattern: any tag starting with 'v'
                # Examples: v1.0.0, v2.1.3-beta, v1.0.0-rc1
                # Non-matches: release-1.0, 1.0.0 (no 'v' prefix)

  # TRIGGER 2: Manual Dispatch
  # Allows running the workflow manually from GitHub UI
  workflow_dispatch:
    inputs:
      version:
        description: 'Version number (e.g., 1.0.3)'
        required: true       # User must provide this value
        type: string         # Free-form text input

# ============================================================================
# ENVIRONMENT VARIABLES (env:)
# ============================================================================
# Global variables available to all jobs and steps
# Referenced as ${{ env.VARIABLE_NAME }}
env:
  DOTNET_VERSION: '8.0.x'    # .NET SDK version to install
                              # '8.0.x' means latest 8.0.* version
  PROJECT_PATH: 'src/MouseEffects.App/MouseEffects.App.csproj'
                              # Path to the main project file

# ============================================================================
# JOBS
# ============================================================================
# A workflow contains one or more jobs
# Jobs run in parallel by default (unless dependencies are specified)
jobs:
  # --------------------------------------------------------------------------
  # JOB: build
  # --------------------------------------------------------------------------
  build:
    # RUNNER: Virtual machine to run on
    # Options: ubuntu-latest, windows-latest, macos-latest
    # Windows required for .NET Windows Forms/WPF apps
    runs-on: windows-latest

    # STEPS: Sequential tasks within this job
    steps:
      # ======================================================================
      # STEP 1: Checkout code
      # ======================================================================
      # Downloads your repository to the runner
      - name: Checkout code
        uses: actions/checkout@v4      # Official GitHub action
        with:
          fetch-depth: 0               # 0 = full history with all tags
                                       # Needed for version detection
                                       # Default (1) only gets latest commit

      # ======================================================================
      # STEP 2: Setup .NET SDK
      # ======================================================================
      # Installs .NET SDK on the runner
      - name: Setup .NET
        uses: actions/setup-dotnet@v4  # Official .NET setup action
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
                                       # References the env variable above
                                       # Resolves to '8.0.x'

      # ======================================================================
      # STEP 3: Determine version number
      # ======================================================================
      # Extracts version from tag or manual input
      - name: Determine version
        id: version                    # ID allows referencing outputs later
                                       # as: steps.version.outputs.VERSION
        shell: pwsh                    # Use PowerShell (pwsh = PowerShell Core)
        run: |
          # Check if this was a manual trigger with version input
          if ("${{ github.event.inputs.version }}" -ne "") {
            # Manual trigger: use the provided version
            $version = "${{ github.event.inputs.version }}"
          } else {
            # Tag trigger: extract version from tag name
            # github.ref_name = 'v1.0.4' -> TrimStart('v') -> '1.0.4'
            $version = "${{ github.ref_name }}".TrimStart('v')
          }
          # Write to GITHUB_OUTPUT file (how steps share data)
          echo "VERSION=$version" >> $env:GITHUB_OUTPUT
          # Also print for logging
          echo "Version: $version"

      # ======================================================================
      # STEP 4: Restore NuGet packages
      # ======================================================================
      # Downloads all NuGet dependencies
      - name: Restore dependencies
        run: dotnet restore             # Reads .csproj files and downloads packages

      # ======================================================================
      # STEP 5: Build the solution
      # ======================================================================
      # Compiles all projects
      - name: Build solution
        run: dotnet build --configuration Release --no-restore
        # --configuration Release    : Build in Release mode (optimized)
        # --no-restore              : Skip restore (already done above)

      # ======================================================================
      # STEP 6: Publish application
      # ======================================================================
      # Creates deployment-ready output
      - name: Publish application (x64)
        run: |
          dotnet publish ${{ env.PROJECT_PATH }} `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output ./publish/win-x64 `
            -p:PublishSingleFile=false `
            -p:Version=${{ steps.version.outputs.VERSION }}
        # Parameter breakdown:
        # ${{ env.PROJECT_PATH }}      : The project to publish
        # --configuration Release      : Use Release configuration
        # --runtime win-x64           : Target Windows 64-bit
        #                               Options: win-x64, win-x86, win-arm64
        # --self-contained true       : Include .NET runtime in output
        #                               Users don't need .NET installed
        # --output ./publish/win-x64  : Where to put the output files
        # -p:PublishSingleFile=false  : Keep as separate DLLs (Velopack needs this)
        # -p:Version=...              : Set assembly version from our variable

      # ======================================================================
      # STEP 7: Install Velopack CLI
      # ======================================================================
      # Installs the vpk command-line tool
      - name: Install Velopack CLI
        run: dotnet tool install -g vpk
        # -g : Install globally (available in PATH)
        # vpk : Velopack CLI package name

      # ======================================================================
      # STEP 8: Create Velopack package
      # ======================================================================
      # Packages the application for distribution
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
        # Parameter breakdown:
        # --packId MouseEffects        : Unique identifier for the app
        # --packVersion ...            : Version number (e.g., 1.0.4)
        # --packDir ./publish/win-x64  : Directory containing published files
        # --mainExe MouseEffects.App.exe : Main executable name
        # --outputDir ./releases       : Where to create installer files
        # --packTitle "MouseEffects"   : Display name in installer
        # --icon ...                   : Icon for installer and shortcuts
        #
        # Creates:
        #   - MouseEffects-win-Setup.exe (installer)
        #   - MouseEffects-{ver}-win-full.nupkg (update package)
        #   - RELEASES (metadata file)

      # ======================================================================
      # STEP 9: List artifacts (debugging)
      # ======================================================================
      # Shows what files were created (helpful for debugging)
      - name: List release artifacts
        shell: pwsh
        run: |
          echo "Release artifacts:"
          Get-ChildItem -Path ./releases -Recurse | Format-Table Name, Length
        # Outputs file names and sizes to the log

      # ======================================================================
      # STEP 10: Upload build artifacts
      # ======================================================================
      # Stores files for later download (even if release fails)
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: MouseEffects-${{ steps.version.outputs.VERSION }}
                                       # Artifact name in GitHub UI
          path: ./releases/*           # Files to upload (glob pattern)
          retention-days: 30           # Keep for 30 days then auto-delete

      # ======================================================================
      # STEP 11: Create GitHub Release (tag trigger)
      # ======================================================================
      # Creates a public release on GitHub
      - name: Create GitHub Release
        if: startsWith(github.ref, 'refs/tags/v')
                                       # ONLY run if triggered by a tag
                                       # Prevents running on manual dispatch
        uses: softprops/action-gh-release@v2
                                       # Popular third-party release action
        with:
          name: MouseEffects v${{ steps.version.outputs.VERSION }}
                                       # Release title shown on GitHub
          draft: false                 # false = immediately visible
                                       # true = hidden until manually published
          prerelease: ${{ contains(github.ref_name, '-') }}
                                       # Auto-detect pre-release:
                                       # 'v1.0.0' -> false (stable)
                                       # 'v1.0.0-beta' -> true (pre-release)
          generate_release_notes: true # Auto-generate from commits since last release
          files: |                     # Files to attach to release
            ./releases/MouseEffects-win-Setup.exe
            ./releases/MouseEffects-${{ steps.version.outputs.VERSION }}-win-full.nupkg
            ./releases/RELEASES
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
                                       # Built-in token for GitHub API access
                                       # Automatically provided, properly scoped

      # ======================================================================
      # STEP 12: Create GitHub Release (manual trigger)
      # ======================================================================
      # Creates a DRAFT release for manual triggers
      - name: Create GitHub Release (manual trigger)
        if: github.event_name == 'workflow_dispatch'
                                       # ONLY run if manually triggered
        uses: softprops/action-gh-release@v2
        with:
          tag_name: v${{ steps.version.outputs.VERSION }}
                                       # Creates a new tag (manual doesn't have one)
          name: MouseEffects v${{ steps.version.outputs.VERSION }}
          draft: true                  # DRAFT: requires manual publish
                                       # Allows review before making public
          prerelease: false
          generate_release_notes: true
          files: |
            ./releases/MouseEffects-win-Setup.exe
            ./releases/MouseEffects-${{ steps.version.outputs.VERSION }}-win-full.nupkg
            ./releases/RELEASES
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

## Key Concepts Explained

### GitHub Context Variables

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `github.ref` | Full ref that triggered workflow | `refs/tags/v1.0.4` |
| `github.ref_name` | Short ref name | `v1.0.4` |
| `github.event_name` | Event that triggered workflow | `push` or `workflow_dispatch` |
| `github.event.inputs.*` | Manual input values | User-provided version |
| `secrets.GITHUB_TOKEN` | Auto-generated API token | (hidden) |

### Expression Syntax

```yaml
# Variable substitution
${{ env.VARIABLE }}           # Environment variable
${{ steps.id.outputs.NAME }}  # Step output
${{ secrets.SECRET_NAME }}    # Repository secret
${{ github.* }}               # GitHub context

# Conditionals
if: startsWith(github.ref, 'refs/tags/')  # String prefix check
if: contains(github.ref_name, '-')        # String contains check
if: github.event_name == 'push'           # Equality check
if: success()                             # Previous steps succeeded
if: failure()                             # Previous steps failed
if: always()                              # Always run
```

### Output Flow

```
[Step 3: Determine version]
    │
    ├─ Reads: github.ref_name or github.event.inputs.version
    │
    └─ Writes: steps.version.outputs.VERSION = "1.0.4"
               │
               ▼
[Step 6: Publish] ─────► -p:Version=1.0.4
               │
               ▼
[Step 8: Velopack] ────► --packVersion 1.0.4
               │
               ▼
[Step 11: Release] ───► name: MouseEffects v1.0.4
                        files: MouseEffects-1.0.4-win-full.nupkg
```

### File Paths in Workflow

```
Repository Root/
├── .github/
│   └── workflows/
│       └── release.yml          # This workflow file
├── src/
│   └── MouseEffects.App/
│       ├── MouseEffects.App.csproj
│       └── Images/
│           └── StoreLogo.png    # Icon for installer
│
└── (Created during workflow):
    ├── publish/
    │   └── win-x64/             # dotnet publish output
    │       ├── MouseEffects.App.exe
    │       ├── MouseEffects.App.dll
    │       └── ...
    └── releases/                # vpk pack output
        ├── MouseEffects-win-Setup.exe
        ├── MouseEffects-1.0.4-win-full.nupkg
        └── RELEASES
```

## Modifying the Workflow

### Adding a New Step

```yaml
steps:
  # ... existing steps ...

  - name: My new step
    run: echo "Hello from new step"

  # ... rest of steps ...
```

### Adding a Secret

1. Go to **Settings → Secrets and variables → Actions**
2. Click **New repository secret**
3. Use in workflow: `${{ secrets.MY_SECRET }}`

### Adding Another Platform

```yaml
- name: Publish application (x86)
  run: |
    dotnet publish ${{ env.PROJECT_PATH }} `
      --configuration Release `
      --runtime win-x86 `
      --self-contained true `
      --output ./publish/win-x86 `
      -p:Version=${{ steps.version.outputs.VERSION }}

- name: Create Velopack release (x86)
  run: |
    vpk pack `
      --packId MouseEffects `
      --packVersion ${{ steps.version.outputs.VERSION }} `
      --packDir ./publish/win-x86 `
      --mainExe MouseEffects.App.exe `
      --outputDir ./releases-x86 `
      --packTitle "MouseEffects (32-bit)"
```

### Conditional Steps

```yaml
# Only run on specific branches
- name: Deploy to staging
  if: github.ref == 'refs/heads/develop'
  run: ./deploy-staging.sh

# Only run if previous step succeeded
- name: Notify success
  if: success()
  run: echo "Build succeeded!"

# Run even if previous steps failed
- name: Cleanup
  if: always()
  run: ./cleanup.sh
```

## Debugging Tips

### View All Available Context

Add this step to see all available variables:

```yaml
- name: Debug context
  run: |
    echo "github.ref: ${{ github.ref }}"
    echo "github.ref_name: ${{ github.ref_name }}"
    echo "github.event_name: ${{ github.event_name }}"
    echo "github.sha: ${{ github.sha }}"
    echo "runner.os: ${{ runner.os }}"
```

### Force Workflow Re-run

If a workflow fails:
1. Go to the failed run
2. Click "Re-run all jobs" or "Re-run failed jobs"

### Test Changes Safely

1. Create a test branch
2. Modify workflow to trigger on that branch:
   ```yaml
   on:
     push:
       branches: [test-workflow]
   ```
3. Push to test branch
4. Verify changes work
5. Merge to main and restore original triggers

## See Also

- [GitHub Actions CI/CD](GitHub-Actions.md) - Overview and usage guide
- [Velopack Packaging](Velopack-Packaging.md) - Local Velopack usage
- [Building from Source](Building.md) - Local build instructions
