# How to Build the MouseEffects Installer

## Summary

This guide explains how to create a standard Windows MSI installer for MouseEffects using Visual Studio Setup Project. The installer will deploy the application to Program Files, create Start Menu shortcuts, and optionally configure the application to launch at Windows startup via a registry key. Visual Studio Setup Projects generate standard MSI files that follow Windows Installer conventions, providing users with a familiar installation experience including Add/Remove Programs integration, repair functionality, and clean uninstallation. This method requires Visual Studio 2019 or later with the "Microsoft Visual Studio Installer Projects" extension installed. The resulting MSI can be distributed to end users and installed with standard Windows installer behavior, including silent installation support via command line parameters.

---

## Prerequisites

- **Visual Studio 2019 or later** (Community, Professional, or Enterprise)
- **Microsoft Visual Studio Installer Projects** extension
- **MouseEffects solution** builds successfully

---

## Step 1: Install the Installer Projects Extension

1. Open **Visual Studio**
2. Go to **Extensions → Manage Extensions**
3. Select **Online** in the left panel
4. Search for **"Microsoft Visual Studio Installer Projects"**
5. Click **Download**
6. Close Visual Studio to complete the installation
7. Follow the VSIX installer prompts
8. Restart Visual Studio

---

## Step 2: Create the Setup Project

1. Open the **MouseEffects.sln** solution in Visual Studio
2. In **Solution Explorer**, right-click the **Solution 'MouseEffects'**
3. Select **Add → New Project...**
4. In the search box, type **"Setup Project"**
5. Select **Setup Project** (not Setup Wizard)
6. Configure the project:
   - **Name:** `MouseEffects.Setup`
   - **Location:** `src\` folder (alongside other projects)
7. Click **Create**

---

## Step 3: Configure Project Properties

1. In Solution Explorer, click on **MouseEffects.Setup** project
2. In the **Properties** window (press F4 if not visible), set:

| Property | Value |
|----------|-------|
| ProductName | MouseEffects |
| Manufacturer | MouseEffects |
| Version | 1.0.0 |
| TargetPlatform | x64 |
| InstallAllUsers | True |

---

## Step 4: Add Application Files

### Add Primary Output

1. Right-click **MouseEffects.Setup** project
2. Select **Add → Project Output...**
3. In the dialog:
   - **Project:** Select `MouseEffects.App`
   - **Output type:** Select `Primary Output`
4. Click **OK**

### Add Content Files (if any)

1. Right-click **MouseEffects.Setup** project
2. Select **Add → Project Output...**
3. Select `Content Files` if your project has any
4. Click **OK**

### Add Plugin DLLs

1. Right-click **Application Folder** in the File System view
2. Select **Add → File...**
3. Navigate to `src\MouseEffects.App\bin\Release\net8.0-windows\plugins\`
4. Select all plugin DLLs:
   - `MouseEffects.Effects.ParticleTrail.dll`
   - `MouseEffects.Effects.LaserWork.dll`
   - `MouseEffects.Effects.ScreenDistortion.dll`
5. Click **Open**

### Create Plugins Subfolder

1. Right-click **Application Folder**
2. Select **Add → Folder**
3. Name it **plugins**
4. Drag the plugin DLLs into this folder

---

## Step 5: Set Installation Directory

1. In Solution Explorer, right-click **MouseEffects.Setup**
2. Select **View → File System**
3. Click on **Application Folder** in the left panel
4. In the **Properties** window, set:

| Property | Value |
|----------|-------|
| DefaultLocation | `[ProgramFiles64Folder]\MouseEffects` |

---

## Step 6: Create Start Menu Shortcut

1. In the **File System** view, right-click **User's Programs Menu**
2. Select **Add → Folder**
3. Name the folder **MouseEffects**
4. Right-click the new **MouseEffects** folder
5. Select **Create New Shortcut**
6. In the dialog, navigate to **Application Folder**
7. Select **Primary output from MouseEffects.App**
8. Click **OK**
9. Rename the shortcut to **MouseEffects**

### Optional: Add Desktop Shortcut

1. Right-click **User's Desktop**
2. Select **Create New Shortcut**
3. Select **Primary output from MouseEffects.App**
4. Rename to **MouseEffects**

---

## Step 7: Add Startup Registry Key (Launch at Windows Startup)

1. In Solution Explorer, right-click **MouseEffects.Setup**
2. Select **View → Registry**
3. In the Registry view, expand **HKEY_CURRENT_USER**
4. Right-click **Software** → **Add → Key** → Name: **Microsoft**
5. Right-click **Microsoft** → **Add → Key** → Name: **Windows**
6. Right-click **Windows** → **Add → Key** → Name: **CurrentVersion**
7. Right-click **CurrentVersion** → **Add → Key** → Name: **Run**
8. Right-click **Run** → **New → String Value**
9. Set the **Name** to: `MouseEffects`
10. Set the **Value** to: `[TARGETDIR]MouseEffects.App.exe`

> **Note:** This will make the application start automatically when Windows starts. If you want this to be optional, you'll need to create a custom installer dialog or handle it within your application settings.

---

## Step 8: Configure Build Dependencies

1. In Solution Explorer, right-click the **Solution**
2. Select **Project Dependencies...**
3. In the **Projects** dropdown, select **MouseEffects.Setup**
4. Check the box for **MouseEffects.App**
5. Click **OK**

This ensures the application is built before the installer.

---

## Step 9: Build the Installer

### Build in Visual Studio

1. Set the build configuration to **Release**
2. Right-click **MouseEffects.Setup** project
3. Select **Build**
4. Wait for the build to complete

### Output Location

The MSI installer will be created at:
```
src\MouseEffects.Setup\Release\MouseEffects.Setup.msi
```

---

## Step 10: Test the Installer

### Install

1. Navigate to the Release folder
2. Double-click **MouseEffects.Setup.msi**
3. Follow the installation wizard
4. Verify the application is installed in `C:\Program Files\MouseEffects`
5. Check the Start Menu for the shortcut
6. Verify the startup registry key exists (if configured)

### Uninstall

1. Open **Settings → Apps → Installed Apps**
2. Find **MouseEffects**
3. Click **Uninstall**
4. Verify all files and registry keys are removed

---

## Silent Installation (Command Line)

For automated deployments, you can install silently:

```batch
:: Standard silent install
msiexec /i MouseEffects.Setup.msi /quiet

:: Silent install with logging
msiexec /i MouseEffects.Setup.msi /quiet /log install.log

:: Silent install to custom location
msiexec /i MouseEffects.Setup.msi /quiet TARGETDIR="D:\CustomPath\MouseEffects"

:: Silent uninstall
msiexec /x MouseEffects.Setup.msi /quiet
```

---

## Troubleshooting

### "Setup Project" template not found

- Ensure the **Microsoft Visual Studio Installer Projects** extension is installed
- Restart Visual Studio after installing the extension

### Build fails with missing dependencies

- Build the main solution first: **Build → Build Solution**
- Then build the Setup project

### MSI won't install on other machines

- Ensure the target machine has **.NET 8.0 Runtime** installed, OR
- Publish the app as self-contained (see below)

### Creating a Self-Contained Installer

If you want the installer to work without requiring .NET to be pre-installed:

1. First publish the app as self-contained:
```batch
dotnet publish src\MouseEffects.App\MouseEffects.App.csproj -c Release -r win-x64 --self-contained true -o publish
```

2. In the Setup Project, instead of adding Project Output, add files directly from the `publish` folder

---

## Version Updates

When releasing a new version:

1. Update the **Version** property in the Setup project properties
2. Update the **ProductCode** (right-click project → Properties → click the "..." button next to ProductCode to generate new GUID)
3. Keep the **UpgradeCode** the same (this allows upgrades to replace old versions)
4. Rebuild the installer

---

## Additional Resources

- [Microsoft Docs: Visual Studio Installer Projects](https://docs.microsoft.com/en-us/visualstudio/deployment/installer-projects-net-core)
- [MSI Command Line Options](https://docs.microsoft.com/en-us/windows/win32/msi/command-line-options)
