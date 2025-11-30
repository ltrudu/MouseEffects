namespace MouseEffects.App.Services;

/// <summary>
/// Update check frequency options.
/// </summary>
public enum UpdateCheckFrequency
{
    OnStartup,
    Daily,
    Weekly,
    Never
}

/// <summary>
/// Update application mode.
/// </summary>
public enum UpdateMode
{
    /// <summary>
    /// Download updates silently in background, apply on next restart.
    /// </summary>
    Silent,

    /// <summary>
    /// Notify user when update is available, let them choose when to update.
    /// </summary>
    Notify
}

/// <summary>
/// Information about an available update.
/// </summary>
public class UpdateInfo
{
    public string CurrentVersion { get; init; } = string.Empty;
    public string NewVersion { get; init; } = string.Empty;
    public bool IsUpdateAvailable { get; init; }
}

/// <summary>
/// Service for checking and applying application updates via GitHub Releases.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Gets the current application version.
    /// </summary>
    string CurrentVersion { get; }

    /// <summary>
    /// Gets whether an update is currently being downloaded.
    /// </summary>
    bool IsDownloading { get; }

    /// <summary>
    /// Gets whether an update has been downloaded and is ready to apply.
    /// </summary>
    bool IsUpdateReady { get; }

    /// <summary>
    /// Check for available updates.
    /// </summary>
    /// <returns>Update information, or null if check failed.</returns>
    Task<UpdateInfo?> CheckForUpdatesAsync();

    /// <summary>
    /// Download the available update.
    /// </summary>
    /// <param name="progress">Progress reporter (0-100).</param>
    Task DownloadUpdateAsync(IProgress<int>? progress = null);

    /// <summary>
    /// Apply the downloaded update and restart the application.
    /// </summary>
    void ApplyUpdateAndRestart();

    /// <summary>
    /// Raised when an update is available.
    /// </summary>
    event Action<UpdateInfo>? UpdateAvailable;

    /// <summary>
    /// Raised when update download progress changes.
    /// </summary>
    event Action<int>? DownloadProgressChanged;

    /// <summary>
    /// Raised when an update is ready to apply.
    /// </summary>
    event Action? UpdateReady;
}
