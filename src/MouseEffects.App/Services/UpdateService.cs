using MouseEffects.App.Settings;
using MouseEffects.Core.Diagnostics;
using Velopack;
using Velopack.Sources;

namespace MouseEffects.App.Services;

/// <summary>
/// Service for checking and applying application updates via GitHub Releases using Velopack.
/// </summary>
public class UpdateService : IUpdateService
{
    private const string GitHubRepoUrl = "https://github.com/ltrudu/MouseEffects";

    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;
    private bool _isDownloading;
    private bool _isUpdateReady;

    public event Action<UpdateInfo>? UpdateAvailable;
    public event Action<int>? DownloadProgressChanged;
    public event Action? UpdateReady;

    public string CurrentVersion => _updateManager.CurrentVersion?.ToString() ?? "0.0.0";
    public bool IsDownloading => _isDownloading;
    public bool IsUpdateReady => _isUpdateReady;

    public UpdateService()
    {
        var source = new GithubSource(GitHubRepoUrl, null, false);
        _updateManager = new UpdateManager(source);
        Logger.Log("UpdateService", $"Initialized with GitHub source: {GitHubRepoUrl}");
        Logger.Log("UpdateService", $"Current version: {CurrentVersion}");
        Logger.Log("UpdateService", $"Is installed: {_updateManager.IsInstalled}");
        Logger.Log("UpdateService", $"App ID: {_updateManager.AppId ?? "null"}");
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            Logger.Log("UpdateService", "Checking for updates...");

            if (!_updateManager.IsInstalled)
            {
                Logger.Log("UpdateService", "WARNING: App is not installed via Velopack. Updates only work when installed with the Setup.exe installer.");
                return new UpdateInfo
                {
                    CurrentVersion = CurrentVersion,
                    NewVersion = CurrentVersion,
                    IsUpdateAvailable = false,
                    Message = "Updates require installation via Setup.exe"
                };
            }

            Logger.Log("UpdateService", $"Fetching updates from GitHub...");
            var newVersion = await _updateManager.CheckForUpdatesAsync();

            if (newVersion != null)
            {
                var targetVersion = newVersion.TargetFullRelease.Version.ToString();
                Logger.Log("UpdateService", $"GitHub returned version: {targetVersion}");
                Logger.Log("UpdateService", $"Release count: {newVersion.DeltasToTarget?.Count ?? 0} deltas");

                _pendingUpdate = new UpdateInfo
                {
                    CurrentVersion = CurrentVersion,
                    NewVersion = targetVersion,
                    IsUpdateAvailable = true
                };

                Logger.Log("UpdateService", $"Update available: {_pendingUpdate.CurrentVersion} -> {_pendingUpdate.NewVersion}");
                UpdateAvailable?.Invoke(_pendingUpdate);
                return _pendingUpdate;
            }

            Logger.Log("UpdateService", "CheckForUpdatesAsync returned null - no updates available or already up to date");
            return new UpdateInfo
            {
                CurrentVersion = CurrentVersion,
                NewVersion = CurrentVersion,
                IsUpdateAvailable = false
            };
        }
        catch (Exception ex)
        {
            Logger.Error("UpdateService", ex);
            return null;
        }
    }

    public async Task DownloadUpdateAsync(IProgress<int>? progress = null)
    {
        if (_pendingUpdate == null || !_pendingUpdate.IsUpdateAvailable)
        {
            Logger.Log("UpdateService", "No pending update to download");
            return;
        }

        try
        {
            _isDownloading = true;
            Logger.Log("UpdateService", $"Downloading update to {_pendingUpdate.NewVersion}...");

            var newVersion = await _updateManager.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                Logger.Log("UpdateService", "Update no longer available");
                return;
            }

            await _updateManager.DownloadUpdatesAsync(newVersion, p =>
            {
                progress?.Report(p);
                DownloadProgressChanged?.Invoke(p);
            });

            _isDownloading = false;
            _isUpdateReady = true;
            Logger.Log("UpdateService", "Update downloaded successfully");
            UpdateReady?.Invoke();
        }
        catch (Exception ex)
        {
            _isDownloading = false;
            Logger.Error("UpdateService", ex);
            throw;
        }
    }

    public void ApplyUpdateAndRestart()
    {
        if (!_isUpdateReady)
        {
            Logger.Log("UpdateService", "No update ready to apply");
            return;
        }

        try
        {
            Logger.Log("UpdateService", "Applying update and restarting...");
            _updateManager.ApplyUpdatesAndRestart(null);
        }
        catch (Exception ex)
        {
            Logger.Error("UpdateService", ex);
            throw;
        }
    }

    /// <summary>
    /// Check for updates based on settings and apply according to update mode.
    /// Call this from application startup.
    /// </summary>
    public async Task CheckAndApplyUpdatesAsync(AppSettings settings)
    {
        if (settings.UpdateCheckFrequency == UpdateCheckFrequency.Never)
        {
            Logger.Log("UpdateService", "Update checking disabled in settings");
            return;
        }

        // Check if we should check based on frequency
        if (!ShouldCheckForUpdates(settings))
        {
            Logger.Log("UpdateService", "Skipping update check (not due yet)");
            return;
        }

        var updateInfo = await CheckForUpdatesAsync();
        if (updateInfo == null || !updateInfo.IsUpdateAvailable)
        {
            return;
        }

        // Update last check time
        settings.LastUpdateCheck = DateTime.UtcNow;
        settings.Save();

        if (settings.UpdateMode == UpdateMode.Silent)
        {
            // Silent mode: download in background, will apply on next restart
            Logger.Log("UpdateService", "Silent mode: downloading update in background");
            await DownloadUpdateAsync();
            // Don't restart - user will get update on next manual restart
        }
        // Notify mode: UpdateAvailable event was already raised, UI will handle it
    }

    private bool ShouldCheckForUpdates(AppSettings settings)
    {
        if (settings.UpdateCheckFrequency == UpdateCheckFrequency.OnStartup)
        {
            return true;
        }

        var lastCheck = settings.LastUpdateCheck;
        if (lastCheck == null)
        {
            return true;
        }

        var timeSinceLastCheck = DateTime.UtcNow - lastCheck.Value;

        return settings.UpdateCheckFrequency switch
        {
            UpdateCheckFrequency.Daily => timeSinceLastCheck.TotalDays >= 1,
            UpdateCheckFrequency.Weekly => timeSinceLastCheck.TotalDays >= 7,
            _ => true
        };
    }
}
