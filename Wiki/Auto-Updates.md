# Auto-Updates

MouseEffects includes built-in automatic update functionality powered by [Velopack](https://github.com/velopack/velopack). This allows the application to check for updates, download them in the background, and apply them seamlessly.

## How It Works

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  MouseEffects   │────▶│  GitHub Releases │────▶│  Your App       │
│  (Check)        │     │  (Host Updates)  │     │  (Auto-Update)  │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

1. **On Startup**: The app checks GitHub Releases for newer versions
2. **Detection**: Compares current version with latest release
3. **Download**: Downloads update packages (full or delta)
4. **Apply**: Installs update on next restart

## Update Modes

MouseEffects supports two update modes, configurable in Settings:

### Silent Mode

- Updates download automatically in the background
- No notifications or interruptions
- Updates apply on next restart
- Best for users who want hands-off updates

### Notify Mode (Default)

- Shows notification when update is available
- User can choose when to download and install
- Progress displayed during download
- Best for users who want control over updates

## Configuration

### Settings Window

Access update settings via the system tray icon → Settings → Updates section:

| Option | Description |
|--------|-------------|
| **Check for updates** | How often to check: On startup, Daily, Weekly, Never |
| **When update available** | Silent (background) or Notify (ask user) |
| **Check Now** | Manually check for updates immediately |

### Settings File

Update settings are stored in `%AppData%\MouseEffects\settings.json`:

```json
{
  "UpdateCheckFrequency": "OnStartup",
  "UpdateMode": "Notify",
  "IncludePreReleases": false,
  "LastUpdateCheck": "2024-01-15T10:30:00Z"
}
```

## Update Check Frequency

| Setting | Behavior |
|---------|----------|
| **OnStartup** | Check every time the app starts |
| **Daily** | Check once per day |
| **Weekly** | Check once per week |
| **Never** | Disable automatic checks (manual only) |

## Manual Updates

To manually check for updates:

1. Open Settings (right-click tray icon → Settings)
2. Scroll to the Updates section
3. Click **Check Now**
4. If an update is available, click **Download & Install Update**

## Update Flow

### Automatic (Silent Mode)

```
App Start → Check GitHub → Download in Background → Ready on Restart
```

### Manual (Notify Mode)

```
App Start → Check GitHub → Notification → User Clicks → Download → Restart
```

## Delta Updates

Velopack supports delta updates, which means:

- Only changed files are downloaded
- Significantly smaller download sizes
- Faster update process
- Automatic fallback to full update if delta fails

## Troubleshooting

### Updates Not Working

1. **Check network connection** - The app needs internet access to check GitHub
2. **Verify frequency setting** - Make sure "Never" isn't selected
3. **Check logs** - Look in `%AppData%\MouseEffects\debug.log` for errors

### Update Check Fails

Common causes:
- GitHub API rate limiting (temporary, try again later)
- Firewall blocking GitHub access
- No releases published yet

### Update Won't Apply

1. Close MouseEffects completely
2. Restart the application
3. The update should apply automatically

### Rollback

If an update causes issues:
1. The previous version is retained
2. Uninstall and reinstall the previous version from [Releases](https://github.com/ltrudu/MouseEffects/releases)

## For Developers

### Update Service Architecture

The update system consists of:

| Component | File | Purpose |
|-----------|------|---------|
| `IUpdateService` | Services/IUpdateService.cs | Interface definition |
| `UpdateService` | Services/UpdateService.cs | Velopack integration |
| Update UI | UI/SettingsWindow.xaml | Settings controls |

### GitHub Releases Integration

Updates are published to GitHub Releases with these artifacts:

| File | Purpose |
|------|---------|
| `MouseEffects-win-Setup.exe` | Installer for new users |
| `MouseEffects-{version}-win-full.nupkg` | Full update package |
| `MouseEffects-{version}-win-delta.nupkg` | Delta update package |
| `RELEASES` | Velopack manifest file |

### Publishing Updates

To publish a new version:

1. Update version in `MouseEffects.App.csproj`
2. Commit and tag: `git tag v1.0.4`
3. Push tag: `git push origin v1.0.4`
4. GitHub Actions automatically builds and publishes

See [Velopack Packaging](Velopack-Packaging.md) for detailed instructions.

## Security

- Updates are downloaded over HTTPS
- All releases are published through GitHub's trusted infrastructure
- No administrator rights required for updates
- Updates install to user profile only

## Comparison with MSIX Updates

| Feature | Velopack | MSIX |
|---------|----------|------|
| Auto-updates | Built-in | Via Microsoft Store |
| Admin required | No | No |
| Delta updates | Yes | Yes |
| Certificate required | No | Yes |
| Offline install | Yes | Yes |
| Enterprise deployment | Manual | Group Policy |
