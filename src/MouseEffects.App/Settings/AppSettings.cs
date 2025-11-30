using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MouseEffects.App.Services;
using MouseEffects.Core.Diagnostics;

namespace MouseEffects.App.Settings;

/// <summary>
/// Application settings with persistence.
/// Contains only application-level settings (GPU selection).
/// Plugin settings are stored in separate files per plugin via PluginSettings class.
/// </summary>
public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MouseEffects",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// The name of the GPU to use for rendering. Null means auto-select.
    /// </summary>
    public string? SelectedGpuName { get; set; }

    /// <summary>
    /// Target frame rate for rendering (30-120 fps). Default is 60.
    /// </summary>
    public int TargetFrameRate { get; set; } = 60;

    /// <summary>
    /// Whether to show the FPS counter in the settings window.
    /// </summary>
    public bool ShowFpsCounter { get; set; } = false;

    /// <summary>
    /// Whether to show the FPS overlay on screen (top right corner).
    /// Only works when ShowFpsCounter is enabled.
    /// </summary>
    public bool ShowFpsOverlay { get; set; } = false;

    // ═══════════════════════════════════════════════════
    // Update Settings
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// How often to check for updates.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UpdateCheckFrequency UpdateCheckFrequency { get; set; } = UpdateCheckFrequency.OnStartup;

    /// <summary>
    /// How to handle updates when available.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UpdateMode UpdateMode { get; set; } = UpdateMode.Notify;

    /// <summary>
    /// Whether to include pre-release versions in update checks.
    /// </summary>
    public bool IncludePreReleases { get; set; } = false;

    /// <summary>
    /// Last time an update check was performed.
    /// </summary>
    public DateTime? LastUpdateCheck { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    Logger.Log("AppSettings", $"Loaded app settings (GPU: {settings.SelectedGpuName ?? "auto"})");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("AppSettings", $"Error loading settings: {ex.Message}");
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
            Logger.Log("AppSettings", "App settings saved");
        }
        catch (Exception ex)
        {
            Logger.Log("AppSettings", $"Error saving settings: {ex.Message}");
        }
    }
}
