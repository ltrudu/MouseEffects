# GitHub Actions Release Workflow Guide

A comprehensive beginner-friendly guide to automating your release process with GitHub Actions.

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Prerequisites](#2-prerequisites)
3. [Understanding GitHub Actions](#3-understanding-github-actions)
4. [The Release Workflow Explained](#4-the-release-workflow-explained)
5. [Step-by-Step Setup Guide](#5-step-by-step-setup-guide)
6. [Git Publish Workflow Tool (Python GUI)](#6-git-publish-workflow-tool-python-gui)
7. [Usage Guide](#7-usage-guide)
8. [Troubleshooting](#8-troubleshooting)
9. [Customization Tips](#9-customization-tips)

---

## 1. Introduction

### What is CI/CD?

**CI/CD** stands for **Continuous Integration / Continuous Deployment**:

- **Continuous Integration (CI)**: Automatically building and testing your code every time you push changes
- **Continuous Deployment (CD)**: Automatically deploying/releasing your application after successful builds

### What is GitHub Actions?

GitHub Actions is GitHub's built-in automation platform that lets you:

- **Automate workflows** directly in your repository
- **Build, test, and deploy** your code automatically
- **Trigger actions** based on events (push, pull request, tags, schedules, etc.)
- **Use pre-built actions** from the marketplace or create your own

### Why Use Automated Releases?

| Manual Releases | Automated Releases |
|-----------------|-------------------|
| Error-prone (forget steps) | Consistent every time |
| Time-consuming | Fast and efficient |
| Hard to reproduce | Reproducible builds |
| Version mismatches | Automatic versioning |
| Requires developer time | Set it and forget it |

### How Our Workflow Works

```
┌─────────────────────────────────────────────────────────────┐
│                    RELEASE WORKFLOW                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   1. Developer creates a tag (e.g., v1.0.38)                │
│                    ↓                                         │
│   2. Push tag to GitHub                                      │
│                    ↓                                         │
│   3. GitHub Actions workflow is triggered                    │
│                    ↓                                         │
│   4. Code is checked out on a fresh VM                       │
│                    ↓                                         │
│   5. Dependencies are restored                               │
│                    ↓                                         │
│   6. Application is built and published                      │
│                    ↓                                         │
│   7. Installer is created (Velopack)                         │
│                    ↓                                         │
│   8. GitHub Release is created with artifacts                │
│                    ↓                                         │
│   9. Users can download the new version!                     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Prerequisites

Before you begin, make sure you have:

- **GitHub Account** with a repository
- **Git** installed on your computer
- **Basic Git knowledge**: clone, commit, push, pull, tags
- **Python 3.x** (for the GUI tool - optional but recommended)
- **Your project's build tools** (e.g., .NET SDK, Node.js, etc.)

### Git Tags Primer

Git tags are like bookmarks for specific commits. They're commonly used for releases:

```bash
# Create a tag
git tag v1.0.0

# Create an annotated tag with message
git tag -a v1.0.0 -m "Release version 1.0.0"

# Push a specific tag to remote
git push origin v1.0.0

# Push all tags
git push --tags

# List all tags
git tag -l

# Delete a local tag
git tag -d v1.0.0

# Delete a remote tag
git push origin --delete v1.0.0
```

---

## 3. Understanding GitHub Actions

### Key Concepts

#### Workflow
A configurable automated process defined in a YAML file. Workflows live in `.github/workflows/` directory.

#### Job
A set of steps that execute on the same runner (virtual machine). Jobs can run in parallel or sequentially.

#### Step
An individual task within a job. Steps can run commands or use actions.

#### Action
A reusable unit of code. Can be from GitHub Marketplace or custom-made.

#### Runner
A server that runs your workflows. GitHub provides hosted runners (Ubuntu, Windows, macOS).

#### Event/Trigger
What starts a workflow (push, pull request, tag, schedule, manual, etc.).

### YAML Syntax Basics

```yaml
# Comments start with #

# Key-value pairs
name: My Workflow
version: 1.0

# Lists (arrays)
steps:
  - item1
  - item2
  - item3

# Nested structure
job:
  name: Build
  runs-on: ubuntu-latest
  steps:
    - name: Checkout
      uses: actions/checkout@v4

# Multi-line strings
run: |
  echo "Line 1"
  echo "Line 2"
  echo "Line 3"

# Environment variables
env:
  MY_VAR: "value"

# Using variables
run: echo ${{ env.MY_VAR }}
```

### Common Triggers

```yaml
on:
  # On every push to main branch
  push:
    branches: [main]

  # On pull requests to main
  pull_request:
    branches: [main]

  # On tag push (for releases)
  push:
    tags:
      - 'v*'  # Matches v1.0.0, v2.1.3, etc.

  # Manual trigger from GitHub UI
  workflow_dispatch:
    inputs:
      version:
        description: 'Version number'
        required: true

  # Scheduled (cron syntax)
  schedule:
    - cron: '0 0 * * *'  # Daily at midnight
```

---

## 4. The Release Workflow Explained

Here's our complete release workflow with detailed comments:

```yaml
# ============================================================
# WORKFLOW: Build and Release
# ============================================================
# This workflow automatically builds and releases the application
# when a version tag (v*) is pushed to the repository.
# ============================================================

name: Build and Release

# ============================================================
# TRIGGERS
# ============================================================
on:
  # Trigger 1: When a tag starting with 'v' is pushed
  # Examples: v1.0.0, v2.1.3, v1.0.0-beta
  push:
    tags:
      - 'v*'

  # Trigger 2: Manual trigger from GitHub Actions UI
  # Useful for testing or creating releases without tags
  workflow_dispatch:
    inputs:
      version:
        description: 'Version number (e.g., 1.0.3)'
        required: true
        type: string

# ============================================================
# ENVIRONMENT VARIABLES
# ============================================================
# These are available to all jobs and steps
env:
  DOTNET_VERSION: '8.0.x'
  PROJECT_PATH: 'src/MouseEffects.App/MouseEffects.App.csproj'
  TARGET_FRAMEWORK: 'net8.0-windows10.0.19041.0'

# ============================================================
# PERMISSIONS
# ============================================================
# Required to create releases and upload assets
permissions:
  contents: write

# ============================================================
# JOBS
# ============================================================
jobs:
  build:
    # Run on Windows (required for .NET Windows apps)
    runs-on: windows-latest

    steps:
      # ----------------------------------------------------------
      # STEP 1: Checkout Code
      # ----------------------------------------------------------
      # Downloads your repository code to the runner
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history for versioning

      # ----------------------------------------------------------
      # STEP 2: Setup .NET SDK
      # ----------------------------------------------------------
      # Installs the specified .NET SDK version
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      # ----------------------------------------------------------
      # STEP 3: Determine Version
      # ----------------------------------------------------------
      # Extracts version from tag (v1.0.0 -> 1.0.0)
      # or uses manual input
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
          echo "Version: $version"

      # ----------------------------------------------------------
      # STEP 4: Restore Dependencies
      # ----------------------------------------------------------
      # Downloads NuGet packages
      - name: Restore dependencies
        run: dotnet restore

      # ----------------------------------------------------------
      # STEP 5: Build Solution
      # ----------------------------------------------------------
      # Compiles the code in Release mode
      - name: Build solution (x64)
        run: dotnet build --configuration Release --no-restore -p:Platform=x64

      # ----------------------------------------------------------
      # STEP 6: Publish Application
      # ----------------------------------------------------------
      # Creates a self-contained deployment
      - name: Publish application (x64)
        run: |
          dotnet publish ${{ env.PROJECT_PATH }} `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output ./publish/win-x64 `
            -p:PublishSingleFile=false `
            -p:Version=${{ steps.version.outputs.VERSION }}

      # ----------------------------------------------------------
      # STEP 7: Copy Plugins
      # ----------------------------------------------------------
      # Copies plugin DLLs to the publish folder
      - name: Copy plugins (x64)
        shell: pwsh
        run: |
          $pluginsSource = "./src/MouseEffects.App/bin/x64/Release/${{ env.TARGET_FRAMEWORK }}/plugins"
          $pluginsDest = "./publish/win-x64/plugins"
          if (Test-Path $pluginsSource) {
            New-Item -ItemType Directory -Force -Path $pluginsDest | Out-Null
            Copy-Item -Path "$pluginsSource\*" -Destination $pluginsDest -Recurse -Force
            Write-Host "x64 plugins copied"
          }

      # ----------------------------------------------------------
      # STEP 8: Install Velopack CLI
      # ----------------------------------------------------------
      # Velopack creates professional installers with auto-update
      - name: Install Velopack CLI
        run: dotnet tool install -g vpk

      # ----------------------------------------------------------
      # STEP 9: Create Installer Package
      # ----------------------------------------------------------
      # Packages the app into an installer
      - name: Create Velopack release (x64)
        shell: pwsh
        run: |
          vpk pack `
            --packId MouseEffects `
            --packVersion ${{ steps.version.outputs.VERSION }} `
            --packDir ./publish/win-x64 `
            --mainExe MouseEffects.App.exe `
            --outputDir ./releases/x64 `
            --packTitle "MouseEffects" `
            --icon ./src/MouseEffects.App/MouseEffects.ico

      # ----------------------------------------------------------
      # STEP 10: Rename Artifacts
      # ----------------------------------------------------------
      # Renames files to include architecture info
      - name: Rename artifacts
        shell: pwsh
        run: |
          Move-Item -Path "./releases/x64/MouseEffects-win-Setup.exe" `
                    -Destination "./releases/MouseEffects-x64-Setup.exe" -Force

          Get-ChildItem -Path "./releases/x64/*.nupkg" | ForEach-Object {
            $newName = $_.Name -replace "-full\.nupkg$", "-x64-full.nupkg"
            Move-Item -Path $_.FullName -Destination "./releases/$newName" -Force
          }

      # ----------------------------------------------------------
      # STEP 11: Upload Artifacts
      # ----------------------------------------------------------
      # Saves build outputs for later download
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: MouseEffects-${{ steps.version.outputs.VERSION }}
          path: |
            ./releases/MouseEffects-x64-Setup.exe
            ./releases/*.nupkg
            ./releases/RELEASES-*
          retention-days: 30

      # ----------------------------------------------------------
      # STEP 12: Create GitHub Release (Tag Trigger)
      # ----------------------------------------------------------
      # Creates a release with download links
      - name: Create GitHub Release
        if: startsWith(github.ref, 'refs/tags/v')
        uses: softprops/action-gh-release@v2
        with:
          name: MouseEffects v${{ steps.version.outputs.VERSION }}
          draft: false
          prerelease: ${{ contains(github.ref_name, '-') }}
          generate_release_notes: true
          files: |
            ./releases/MouseEffects-x64-Setup.exe
            ./releases/*-x64-full.nupkg
            ./releases/RELEASES-x64
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      # ----------------------------------------------------------
      # STEP 13: Create GitHub Release (Manual Trigger)
      # ----------------------------------------------------------
      # For manual workflow runs, creates a draft release
      - name: Create GitHub Release (manual trigger)
        if: github.event_name == 'workflow_dispatch'
        uses: softprops/action-gh-release@v2
        with:
          tag_name: v${{ steps.version.outputs.VERSION }}
          name: MouseEffects v${{ steps.version.outputs.VERSION }}
          draft: true
          prerelease: false
          generate_release_notes: true
          files: |
            ./releases/MouseEffects-x64-Setup.exe
            ./releases/*-x64-full.nupkg
            ./releases/RELEASES-x64
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

---

## 5. Step-by-Step Setup Guide

### Step 1: Create the Workflow Directory

In your repository, create the following directory structure:

```
your-repo/
├── .github/
│   └── workflows/
│       └── release.yml
├── src/
│   └── ... your code ...
└── README.md
```

### Step 2: Create the Workflow File

Create `.github/workflows/release.yml` with the content from Section 4 above.

### Step 3: Configure Repository Permissions

1. Go to your repository on GitHub
2. Click **Settings** > **Actions** > **General**
3. Scroll to **Workflow permissions**
4. Select **Read and write permissions**
5. Check **Allow GitHub Actions to create and approve pull requests**
6. Click **Save**

### Step 4: Push the Workflow

```bash
git add .github/workflows/release.yml
git commit -m "Add release workflow"
git push
```

### Step 5: Test with Manual Trigger

1. Go to **Actions** tab in your repository
2. Click on **Build and Release** workflow
3. Click **Run workflow**
4. Enter a version number (e.g., `1.0.0-test`)
5. Click **Run workflow**
6. Watch the progress!

### Step 6: Create Your First Release

```bash
# Make sure you're on the right branch
git checkout main
git pull

# Create and push a tag
git tag v1.0.0
git push origin v1.0.0
```

The workflow will automatically trigger and create a release!

---

## 6. Git Publish Workflow Tool (Python GUI)

To make releasing even easier, we created a Python GUI tool that:

- Fetches the latest version tag
- Auto-increments the version number
- Creates and pushes tags with one click
- Can delete remote tags (for re-triggering workflows)

### The Complete Python Code

Save this as `git-publish-workflow.py`:

```python
#!/usr/bin/env python3
"""
Git Publish Workflow GUI

Opens a GUI to create and push a version tag to trigger GitHub Actions workflow.
Executes: git tag v[major].[minor].[patch] && git push origin v[major].[minor].[patch]
"""

import tkinter as tk
from tkinter import ttk, messagebox, scrolledtext
import subprocess
import threading
import os


class GitPublishWorkflowApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Git Publish Workflow")
        self.root.geometry("600x500")
        self.root.resizable(True, True)

        # Get current working directory
        self.cwd = os.getcwd()

        # Configure grid weights for resizing
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(2, weight=1)

        self._create_widgets()
        self._fetch_latest_tag()

    def _create_widgets(self):
        # Working directory display
        dir_frame = ttk.LabelFrame(self.root, text="Working Directory", padding=10)
        dir_frame.grid(row=0, column=0, padx=10, pady=5, sticky="ew")
        dir_frame.columnconfigure(0, weight=1)

        self.dir_label = ttk.Label(dir_frame, text=self.cwd, wraplength=550)
        self.dir_label.grid(row=0, column=0, sticky="w")

        # Version input frame
        version_frame = ttk.LabelFrame(self.root, text="Version Number", padding=10)
        version_frame.grid(row=1, column=0, padx=10, pady=5, sticky="ew")

        # Version entry fields
        ttk.Label(version_frame, text="Major:").grid(row=0, column=0, padx=5, pady=5)
        self.major_var = tk.StringVar(value="1")
        self.major_entry = ttk.Entry(version_frame, textvariable=self.major_var, width=8, justify="center")
        self.major_entry.grid(row=0, column=1, padx=5, pady=5)

        ttk.Label(version_frame, text=".").grid(row=0, column=2)

        ttk.Label(version_frame, text="Minor:").grid(row=0, column=3, padx=5, pady=5)
        self.minor_var = tk.StringVar(value="0")
        self.minor_entry = ttk.Entry(version_frame, textvariable=self.minor_var, width=8, justify="center")
        self.minor_entry.grid(row=0, column=4, padx=5, pady=5)

        ttk.Label(version_frame, text=".").grid(row=0, column=5)

        ttk.Label(version_frame, text="Patch:").grid(row=0, column=6, padx=5, pady=5)
        self.patch_var = tk.StringVar(value="0")
        self.patch_entry = ttk.Entry(version_frame, textvariable=self.patch_var, width=8, justify="center")
        self.patch_entry.grid(row=0, column=7, padx=5, pady=5)

        # Version preview
        self.version_preview_var = tk.StringVar(value="v1.0.0")
        ttk.Label(version_frame, text="Tag:").grid(row=0, column=8, padx=(20, 5), pady=5)
        self.version_preview = ttk.Label(version_frame, textvariable=self.version_preview_var,
                                         font=("Consolas", 12, "bold"), foreground="blue")
        self.version_preview.grid(row=0, column=9, padx=5, pady=5)

        # Bind entry changes to update preview
        self.major_var.trace_add("write", self._update_preview)
        self.minor_var.trace_add("write", self._update_preview)
        self.patch_var.trace_add("write", self._update_preview)

        # Latest tag info
        self.latest_tag_var = tk.StringVar(value="Fetching latest tag...")
        ttk.Label(version_frame, textvariable=self.latest_tag_var, foreground="gray").grid(
            row=1, column=0, columnspan=10, sticky="w", pady=(5, 0))

        # Buttons frame
        button_frame = ttk.Frame(version_frame)
        button_frame.grid(row=2, column=0, columnspan=10, pady=10)

        self.publish_btn = ttk.Button(button_frame, text="Publish Tag", command=self._publish_tag)
        self.publish_btn.pack(side="left", padx=5)

        self.delete_btn = ttk.Button(button_frame, text="Delete Remote Tag", command=self._delete_remote_tag)
        self.delete_btn.pack(side="left", padx=5)

        self.refresh_btn = ttk.Button(button_frame, text="Refresh Tags", command=self._fetch_latest_tag)
        self.refresh_btn.pack(side="left", padx=5)

        # Output frame
        output_frame = ttk.LabelFrame(self.root, text="Git Output", padding=10)
        output_frame.grid(row=2, column=0, padx=10, pady=5, sticky="nsew")
        output_frame.columnconfigure(0, weight=1)
        output_frame.rowconfigure(0, weight=1)

        self.output_text = scrolledtext.ScrolledText(output_frame, wrap=tk.WORD,
                                                      font=("Consolas", 10), height=15)
        self.output_text.grid(row=0, column=0, sticky="nsew")

        # Configure text tags for coloring
        self.output_text.tag_configure("error", foreground="red")
        self.output_text.tag_configure("success", foreground="green")
        self.output_text.tag_configure("info", foreground="blue")
        self.output_text.tag_configure("command", foreground="purple", font=("Consolas", 10, "bold"))

        # Clear button
        clear_btn = ttk.Button(output_frame, text="Clear Output", command=self._clear_output)
        clear_btn.grid(row=1, column=0, pady=(5, 0))

    def _update_preview(self, *args):
        """Update the version preview label."""
        try:
            major = self.major_var.get() or "0"
            minor = self.minor_var.get() or "0"
            patch = self.patch_var.get() or "0"
            self.version_preview_var.set(f"v{major}.{minor}.{patch}")
        except:
            pass

    def _get_version(self):
        """Get the version string from entry fields."""
        major = self.major_var.get().strip()
        minor = self.minor_var.get().strip()
        patch = self.patch_var.get().strip()

        if not major or not minor or not patch:
            raise ValueError("All version fields must be filled")

        try:
            int(major)
            int(minor)
            int(patch)
        except ValueError:
            raise ValueError("Version numbers must be integers")

        return f"v{major}.{minor}.{patch}"

    def _log(self, message, tag=None):
        """Log a message to the output text widget."""
        self.output_text.insert(tk.END, message + "\n", tag)
        self.output_text.see(tk.END)
        self.root.update_idletasks()

    def _clear_output(self):
        """Clear the output text widget."""
        self.output_text.delete(1.0, tk.END)

    def _run_git_command(self, command, description):
        """Run a git command and return success status."""
        self._log(f"\n> {' '.join(command)}", "command")
        self._log(f"  ({description})", "info")

        try:
            result = subprocess.run(
                command,
                capture_output=True,
                text=True,
                cwd=self.cwd
            )

            if result.stdout:
                self._log(result.stdout.strip())

            if result.stderr:
                if result.returncode == 0:
                    self._log(result.stderr.strip())
                else:
                    self._log(result.stderr.strip(), "error")

            if result.returncode == 0:
                self._log(f"  [OK] {description} completed successfully", "success")
                return True
            else:
                self._log(f"  [FAILED] {description} failed with code {result.returncode}", "error")
                return False

        except Exception as e:
            self._log(f"  [ERROR] {str(e)}", "error")
            return False

    def _fetch_latest_tag(self):
        """Fetch and display the latest version tag."""
        def fetch():
            try:
                result = subprocess.run(
                    ["git", "tag", "-l", "v*", "--sort=-v:refname"],
                    capture_output=True,
                    text=True,
                    cwd=self.cwd
                )

                if result.returncode == 0 and result.stdout.strip():
                    tags = result.stdout.strip().split("\n")
                    latest = tags[0] if tags else "No version tags found"
                    self.root.after(0, lambda: self.latest_tag_var.set(f"Latest tag: {latest}"))

                    # Parse and increment patch version for suggestion
                    if latest.startswith("v"):
                        parts = latest[1:].split(".")
                        if len(parts) >= 3:
                            try:
                                self.root.after(0, lambda: self.major_var.set(parts[0]))
                                self.root.after(0, lambda: self.minor_var.set(parts[1]))
                                patch_num = parts[2].split("-")[0]
                                new_patch = str(int(patch_num) + 1)
                                self.root.after(0, lambda: self.patch_var.set(new_patch))
                            except:
                                pass
                else:
                    self.root.after(0, lambda: self.latest_tag_var.set("No version tags found"))

            except Exception as e:
                self.root.after(0, lambda: self.latest_tag_var.set(f"Error: {str(e)}"))

        threading.Thread(target=fetch, daemon=True).start()

    def _publish_tag(self):
        """Create and push the version tag."""
        try:
            version = self._get_version()
        except ValueError as e:
            messagebox.showerror("Invalid Version", str(e))
            return

        if not messagebox.askyesno("Confirm Publish",
                                   f"This will create and push tag: {version}\n\n"
                                   f"This will trigger the GitHub Actions workflow.\n\n"
                                   f"Continue?"):
            return

        self.publish_btn.state(["disabled"])
        self.delete_btn.state(["disabled"])

        def publish():
            self._log(f"\n{'='*50}", "info")
            self._log(f"Publishing tag: {version}", "info")
            self._log(f"{'='*50}", "info")

            # Create the tag
            success = self._run_git_command(
                ["git", "tag", version],
                f"Creating local tag {version}"
            )

            if not success:
                self._log("\nTag creation failed. The tag may already exist locally.", "error")
                self.root.after(0, lambda: self.publish_btn.state(["!disabled"]))
                self.root.after(0, lambda: self.delete_btn.state(["!disabled"]))
                return

            # Push the tag
            success = self._run_git_command(
                ["git", "push", "origin", version],
                f"Pushing tag {version} to origin"
            )

            if success:
                self._log(f"\n{'='*50}", "success")
                self._log(f"Tag {version} published successfully!", "success")
                self._log("GitHub Actions workflow should now be triggered.", "success")
                self._log(f"{'='*50}", "success")
            else:
                self._log("\nPush failed. Check your network connection and credentials.", "error")

            self.root.after(0, lambda: self.publish_btn.state(["!disabled"]))
            self.root.after(0, lambda: self.delete_btn.state(["!disabled"]))
            self.root.after(0, self._fetch_latest_tag)

        threading.Thread(target=publish, daemon=True).start()

    def _delete_remote_tag(self):
        """Delete the remote tag (useful for re-triggering workflow)."""
        try:
            version = self._get_version()
        except ValueError as e:
            messagebox.showerror("Invalid Version", str(e))
            return

        if not messagebox.askyesno("Confirm Delete",
                                   f"This will delete the remote tag: {version}\n\n"
                                   f"You can then publish again to re-trigger the workflow.\n\n"
                                   f"Continue?"):
            return

        self.publish_btn.state(["disabled"])
        self.delete_btn.state(["disabled"])

        def delete():
            self._log(f"\n{'='*50}", "info")
            self._log(f"Deleting remote tag: {version}", "info")
            self._log(f"{'='*50}", "info")

            success = self._run_git_command(
                ["git", "push", "origin", "--delete", version],
                f"Deleting remote tag {version}"
            )

            if success:
                self._run_git_command(
                    ["git", "tag", "-d", version],
                    f"Deleting local tag {version}"
                )

                self._log(f"\n{'='*50}", "success")
                self._log(f"Tag {version} deleted. You can now publish again.", "success")
                self._log(f"{'='*50}", "success")
            else:
                self._log("\nDelete failed. The tag may not exist on remote.", "error")

            self.root.after(0, lambda: self.publish_btn.state(["!disabled"]))
            self.root.after(0, lambda: self.delete_btn.state(["!disabled"]))
            self.root.after(0, self._fetch_latest_tag)

        threading.Thread(target=delete, daemon=True).start()


def main():
    root = tk.Tk()
    app = GitPublishWorkflowApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
```

### How to Use the Tool

1. **Save the script** to your preferred location
2. **Navigate to your project folder** in terminal/command prompt
3. **Run the script**: `python git-publish-workflow.py`
4. The GUI will:
   - Show your current directory
   - Fetch and display the latest tag
   - Auto-suggest the next version (patch increment)
5. **Click "Publish Tag"** to create and push the tag
6. Watch the output for success/error messages

### Creating Your Own Version

To adapt this tool for your needs:

1. **Change the window title**: Modify `self.root.title("Git Publish Workflow")`
2. **Add custom buttons**: Add more `ttk.Button` widgets in `_create_widgets`
3. **Add pre-publish checks**: Add validation in `_publish_tag` before creating the tag
4. **Integrate with your CI**: Modify `_run_git_command` to run additional commands

---

## 7. Usage Guide

### Triggering a Release via Tag

**Option 1: Command Line**
```bash
git tag v1.0.38
git push origin v1.0.38
```

**Option 2: Using the Python GUI**
1. Run `python git-publish-workflow.py`
2. Adjust version numbers
3. Click "Publish Tag"

**Option 3: GitHub UI (Manual Trigger)**
1. Go to Actions tab
2. Select "Build and Release"
3. Click "Run workflow"
4. Enter version number
5. Click "Run workflow"

### Monitoring Workflow Progress

1. Go to your repository's **Actions** tab
2. Click on the running workflow
3. Expand each step to see logs
4. Green checkmark = success, Red X = failure

### Downloading Artifacts

**From a Release:**
1. Go to **Releases** page
2. Find your release
3. Download assets under "Assets"

**From Workflow Run (before release):**
1. Go to **Actions** tab
2. Click on completed workflow
3. Scroll to "Artifacts"
4. Download the artifact ZIP

---

## 8. Troubleshooting

### Common Issues and Solutions

#### "Tag already exists"
```bash
# Delete local tag
git tag -d v1.0.38

# Delete remote tag
git push origin --delete v1.0.38

# Now create again
git tag v1.0.38
git push origin v1.0.38
```

#### "Permission denied" when creating release
1. Go to Settings > Actions > General
2. Enable "Read and write permissions"
3. Re-run the workflow

#### Build fails with "dotnet not found"
- Ensure `setup-dotnet` step is before build steps
- Check the `dotnet-version` matches your project

#### Artifacts not found in release
- Check the `path` in `upload-artifact` step
- Verify files exist with `ls` or `dir` step
- Check file paths in `action-gh-release`

#### Workflow not triggering on tag push
- Ensure tag matches pattern (e.g., `v*`)
- Check workflow file syntax with YAML validator
- Verify workflow is on the default branch

### Debugging Tips

1. **Add debug output:**
   ```yaml
   - name: Debug info
     run: |
       echo "Current directory: $(pwd)"
       echo "Files:"
       ls -la
   ```

2. **Enable debug logging:**
   - Go to Settings > Secrets > Actions
   - Add secret `ACTIONS_STEP_DEBUG` = `true`

3. **Test locally with act:**
   ```bash
   # Install act
   # Run workflow locally
   act push --tag v1.0.0
   ```

---

## 9. Customization Tips

### For Different Languages/Frameworks

**Node.js:**
```yaml
- uses: actions/setup-node@v4
  with:
    node-version: '20'
- run: npm ci
- run: npm run build
```

**Python:**
```yaml
- uses: actions/setup-python@v5
  with:
    python-version: '3.11'
- run: pip install -r requirements.txt
- run: python setup.py build
```

**Rust:**
```yaml
- uses: actions-rs/toolchain@v1
  with:
    toolchain: stable
- run: cargo build --release
```

### Adding Tests Before Release

```yaml
- name: Run tests
  run: dotnet test --configuration Release --no-build

# Only continue if tests pass
- name: Publish
  if: success()
  run: dotnet publish ...
```

### Multi-Platform Builds

```yaml
jobs:
  build:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      # ... build steps
```

### Notifications

**Discord:**
```yaml
- name: Discord notification
  uses: sarisia/actions-status-discord@v1
  if: always()
  with:
    webhook: ${{ secrets.DISCORD_WEBHOOK }}
```

**Slack:**
```yaml
- name: Slack notification
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
  env:
    SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK }}
```

---

## Summary

You now have everything you need to:

1. **Understand** how GitHub Actions works
2. **Set up** an automated release workflow
3. **Use** the Python GUI tool for easy releases
4. **Customize** the workflow for your needs
5. **Troubleshoot** common issues

Happy releasing!

---

*Generated for MouseEffects project - https://github.com/ltrudu/MouseEffects*
