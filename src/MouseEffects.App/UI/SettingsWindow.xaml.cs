using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MouseEffects.App.Services;
using MouseEffects.App.Settings;
using MouseEffects.App.UI.Controls;
using MouseEffects.Core.Diagnostics;
using MouseEffects.Core.Effects;
using MouseEffects.DirectX.Graphics;
using MouseEffects.Plugins;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MouseEffects.App.UI;

/// <summary>
/// Settings window for configuring effects.
/// Dynamically loads plugin-provided settings controls.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly EffectManager _effectManager;
    private readonly PluginLoader? _pluginLoader;
    private readonly DispatcherTimer _fpsTimer;
    private EffectSelectorDropdown? _effectSelector;
    private FrameworkElement? _currentSettingsControl;
    private bool _isInitializing = true;
    private List<GpuInfo> _availableGpus = [];
    private UpdateInfo? _pendingUpdate;

    public SettingsWindow(EffectManager effectManager)
    {
        _effectManager = effectManager;
        _pluginLoader = Program.PluginLoader;

        // Initialize FPS update timer
        _fpsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250) // Update 4 times per second
        };
        _fpsTimer.Tick += FpsTimer_Tick;

        InitializeComponent();
        LoadThemeSetting();
        LoadGpuList();
        LoadFrameRateSetting();
        LoadFpsCounterSetting();
        LoadHdrSettings();
        LoadHotkeySettings();
        LoadPluginSettings();
        LoadUpdateSettings();
        _isInitializing = false;
    }

    private void LoadThemeSetting()
    {
        var settings = Program.Settings;
        ThemeSelector.SelectedIndex = settings.Theme switch
        {
            AppTheme.System => 0,
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };
    }

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        var settings = Program.Settings;
        var selectedItem = ThemeSelector.SelectedItem as ComboBoxItem;
        var tag = selectedItem?.Tag?.ToString();

        settings.Theme = tag switch
        {
            "System" => AppTheme.System,
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Dark
        };

        settings.Save();

        // Apply theme immediately
        if (System.Windows.Application.Current is App app)
        {
            app.ApplyTheme(settings.Theme);
        }
    }

    private void LoadFrameRateSetting()
    {
        var settings = Program.Settings;
        FrameRateSlider.Value = settings.TargetFrameRate;
        FrameRateValue.Text = $"{settings.TargetFrameRate} fps";
    }

    private void LoadFpsCounterSetting()
    {
        var settings = Program.Settings;
        ShowFpsCheckBox.IsChecked = settings.ShowFpsCounter;
        ShowFpsOverlayCheckBox.IsChecked = settings.ShowFpsOverlay;
        UpdateFpsCounterVisibility(settings.ShowFpsCounter);
    }

    private void UpdateFpsCounterVisibility(bool show)
    {
        FpsCounterText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        CaptureFpsText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            _fpsTimer.Start();
        }
        else
        {
            _fpsTimer.Stop();
        }
        // Use centralized tracking - don't disable if overlay still needs it
        Program.SetSettingsWindowNeedsCaptureFps(show);
    }

    private void FpsTimer_Tick(object? sender, EventArgs e)
    {
        var gameLoop = Program.GameLoop;
        if (gameLoop != null)
        {
            var currentFps = gameLoop.CurrentFps;
            var targetFps = gameLoop.TargetFrameRate;
            var captureFps = gameLoop.CaptureFps;
            FpsCounterText.Text = $"{currentFps:F1} / {targetFps} fps";
            CaptureFpsText.Text = $"Cap: {captureFps:F1} fps";

            // Color code: green if close to target, yellow if slightly below, red if way below
            var ratio = currentFps / targetFps;
            if (ratio >= 0.95)
                FpsCounterText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A6E3A1")); // Green
            else if (ratio >= 0.8)
                FpsCounterText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9E2AF")); // Yellow
            else
                FpsCounterText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F38BA8")); // Red
        }
    }

    private void ShowFpsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var show = ShowFpsCheckBox.IsChecked == true;
        UpdateFpsCounterVisibility(show);

        var settings = Program.Settings;
        settings.ShowFpsCounter = show;
        settings.Save();
    }

    private void ShowFpsOverlayCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var show = ShowFpsOverlayCheckBox.IsChecked == true;
        Program.SetFpsOverlayVisible(show);

        var settings = Program.Settings;
        settings.ShowFpsOverlay = show;
        settings.Save();
    }

    private void LoadHdrSettings()
    {
        var settings = Program.Settings;
        var overlayManager = Program.OverlayManager;

        // Check if HDR is supported on this device
        bool hdrSupported = overlayManager?.IsHdrSupported ?? false;

        // Hide the entire HDR card if not supported
        HdrSettingsCard.Visibility = hdrSupported ? Visibility.Visible : Visibility.Collapsed;

        if (!hdrSupported)
            return;

        EnableHdrCheckBox.IsChecked = settings.EnableHdr;
        HdrBrightnessSlider.Value = settings.HdrPeakBrightness;
        HdrBrightnessValue.Text = $"{settings.HdrPeakBrightness:F1}x";

        // Show brightness panel only when HDR is enabled
        HdrBrightnessPanel.Visibility = settings.EnableHdr ? Visibility.Visible : Visibility.Collapsed;

        // Update status text
        UpdateHdrStatus();
    }

    private void UpdateHdrStatus()
    {
        var overlayManager = Program.OverlayManager;
        if (overlayManager != null)
        {
            bool supported = overlayManager.IsHdrSupported;
            bool enabled = overlayManager.IsHdrEnabled;

            if (!supported)
            {
                HdrStatusText.Text = "HDR not supported on this display or Windows HDR is disabled";
            }
            else if (enabled)
            {
                HdrStatusText.Text = $"HDR is active (peak brightness: {overlayManager.HdrPeakBrightness:F1}x)";
            }
            else
            {
                HdrStatusText.Text = "HDR is supported and available";
            }
        }
        else
        {
            HdrStatusText.Text = "";
        }
    }

    private void EnableHdrCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var enabled = EnableHdrCheckBox.IsChecked == true;

        var settings = Program.Settings;
        settings.EnableHdr = enabled;
        settings.Save();

        // Show/hide brightness slider
        HdrBrightnessPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        // Update status text to indicate restart needed
        HdrStatusText.Text = "Restart MouseEffects to apply HDR changes";
    }

    private void HdrBrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || HdrBrightnessValue == null) return;

        var value = (float)HdrBrightnessSlider.Value;
        HdrBrightnessValue.Text = $"{value:F1}x";

        var settings = Program.Settings;
        settings.HdrPeakBrightness = value;
        settings.Save();

        // Update status text to indicate restart needed
        HdrStatusText.Text = "Restart MouseEffects to apply HDR changes";
    }

    private void LoadHotkeySettings()
    {
        var settings = Program.Settings;
        ToggleHotkeyCheckBox.IsChecked = settings.EnableToggleHotkey;
        SettingsHotkeyCheckBox.IsChecked = settings.EnableSettingsHotkey;
        ScreenCaptureHotkeyCheckBox.IsChecked = settings.EnableScreenCaptureHotkey;
    }

    private void ToggleHotkeyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var enabled = ToggleHotkeyCheckBox.IsChecked == true;

        var settings = Program.Settings;
        settings.EnableToggleHotkey = enabled;
        settings.Save();

        // Update hotkey registration
        Program.UpdateToggleHotkey(enabled);
    }

    private void SettingsHotkeyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var enabled = SettingsHotkeyCheckBox.IsChecked == true;

        var settings = Program.Settings;
        settings.EnableSettingsHotkey = enabled;
        settings.Save();

        // Update hotkey registration
        Program.UpdateSettingsHotkey(enabled);
    }

    private void ScreenCaptureHotkeyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var enabled = ScreenCaptureHotkeyCheckBox.IsChecked == true;

        var settings = Program.Settings;
        settings.EnableScreenCaptureHotkey = enabled;
        settings.Save();

        // Update hotkey registration
        Program.UpdateScreenCaptureHotkey(enabled);
    }

    private void LoadGpuList()
    {
        _availableGpus = D3D11GraphicsDevice.GetAvailableGpus();

        GpuSelector.Items.Clear();
        GpuSelector.Items.Add("Auto (prefer display-connected)");

        foreach (var gpu in _availableGpus)
        {
            GpuSelector.Items.Add(gpu.ToString());
        }

        // Select current setting
        var settings = Program.Settings;
        if (string.IsNullOrEmpty(settings.SelectedGpuName))
        {
            GpuSelector.SelectedIndex = 0;
        }
        else
        {
            var index = _availableGpus.FindIndex(g => g.Name.Contains(settings.SelectedGpuName, StringComparison.OrdinalIgnoreCase));
            GpuSelector.SelectedIndex = index >= 0 ? index + 1 : 0;
        }

        // Show current GPU
        var currentGpu = Program.OverlayManager?.CurrentGpuName ?? "Unknown";
        CurrentGpuText.Text = $"Currently using: {currentGpu}";
    }

    private void LoadPluginSettings()
    {
        if (_pluginLoader == null) return;

        // Create and populate the effect selector dropdown
        _effectSelector = new EffectSelectorDropdown();
        _effectSelector.PopulateEffects(_pluginLoader.Factories);
        _effectSelector.EffectSelected += OnEffectSelected;

        // Set the initially selected effect
        var activeEffectId = Program.GetActiveEffectId();
        _effectSelector.SelectedEffectId = activeEffectId;

        // Add to the host
        EffectSelectorHost.Content = _effectSelector;

        // Show the current effect's settings (or hint message)
        UpdateEffectSettingsUI(activeEffectId);
    }

    /// <summary>
    /// Handle effect selection from the dropdown.
    /// </summary>
    private void OnEffectSelected(string? effectId)
    {
        if (_isInitializing) return;

        Logger.Log("SettingsWindow", $"Effect selected: {effectId ?? "None"}");

        // Set the active effect (handles previous effect disposal and config loading)
        Program.SetActiveEffect(effectId);

        // Update the settings UI
        UpdateEffectSettingsUI(effectId);

        // Sync tray menu
        Program.SyncTrayWithEffects();
    }

    /// <summary>
    /// Update the effect settings panel based on selection.
    /// </summary>
    private void UpdateEffectSettingsUI(string? effectId)
    {
        _currentSettingsControl = null;

        if (string.IsNullOrEmpty(effectId))
        {
            // No effect selected - show hint message
            EffectSettingsCard.Visibility = Visibility.Visible;
            EffectSettingsTitle.Text = "Effect Settings";
            EffectSettingsHint.Visibility = Visibility.Visible;
            EffectSettingsHost.Content = null;
            return;
        }

        var effect = _effectManager.ActiveEffect;
        if (effect == null)
        {
            // Effect not loaded yet
            EffectSettingsCard.Visibility = Visibility.Collapsed;
            return;
        }

        var factory = _effectManager.GetFactory(effectId);
        if (factory == null)
        {
            EffectSettingsCard.Visibility = Visibility.Collapsed;
            return;
        }

        // Show effect settings card with effect name
        EffectSettingsCard.Visibility = Visibility.Visible;
        EffectSettingsTitle.Text = $"{factory.Metadata.Name} Settings";
        EffectSettingsHint.Visibility = Visibility.Collapsed;

        // Create and show the settings control from the plugin
        var settingsControl = factory.CreateSettingsControl(effect);
        if (settingsControl is FrameworkElement frameworkElement)
        {
            EffectSettingsHost.Content = frameworkElement;
            _currentSettingsControl = frameworkElement;
            WireUpSettingsChanged(settingsControl, effectId);
        }
        else
        {
            // No settings control available
            EffectSettingsHint.Text = "No settings available for this effect";
            EffectSettingsHint.Visibility = Visibility.Visible;
            EffectSettingsHost.Content = null;
        }
    }

    /// <summary>
    /// Find a child element by name in the visual tree.
    /// </summary>
    private static T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
    {
        if (parent == null) return null;

        int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild && typedChild.Name == childName)
            {
                return typedChild;
            }

            var foundChild = FindChild<T>(child, childName);
            if (foundChild != null)
            {
                return foundChild;
            }
        }
        return null;
    }

    /// <summary>
    /// Refresh the UI when the active effect changes from the system tray.
    /// </summary>
    public void RefreshEffectEnabledState(string effectId, bool isEnabled)
    {
        // Update the dropdown selection
        if (_effectSelector != null)
        {
            _effectSelector.SelectedEffectId = isEnabled ? effectId : null;
        }

        // Update the settings panel
        UpdateEffectSettingsUI(isEnabled ? effectId : null);
    }

    /// <summary>
    /// Wire up the SettingsChanged event if the control has one.
    /// Uses reflection to support any plugin control with this event pattern.
    /// </summary>
    private void WireUpSettingsChanged(object settingsControl, string effectId)
    {
        var eventInfo = settingsControl.GetType().GetEvent("SettingsChanged");
        if (eventInfo == null) return;

        // Create a delegate that matches Action<string>
        var handler = new Action<string>(OnPluginSettingsChanged);
        try
        {
            eventInfo.AddEventHandler(settingsControl, handler);
        }
        catch
        {
            // If event wiring fails, settings won't auto-save but app continues working
        }
    }

    private void OnPluginSettingsChanged(string effectId)
    {
        // Save settings for the changed effect
        Program.SavePluginSettings(effectId);

        // Sync tray menu
        Program.SyncTrayWithEffects();
    }

    private void GpuSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        var settings = Program.Settings;

        if (GpuSelector.SelectedIndex == 0)
        {
            settings.SelectedGpuName = null;
        }
        else
        {
            var gpuIndex = GpuSelector.SelectedIndex - 1;
            if (gpuIndex >= 0 && gpuIndex < _availableGpus.Count)
            {
                settings.SelectedGpuName = _availableGpus[gpuIndex].Name;
            }
        }

        settings.Save();
        System.Windows.MessageBox.Show("GPU setting saved. Please restart the application for changes to take effect.",
            "Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void FrameRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        if (FrameRateValue == null) return;

        var fps = (int)e.NewValue;
        FrameRateValue.Text = $"{fps} fps";

        // Update settings and game loop
        var settings = Program.Settings;
        settings.TargetFrameRate = fps;
        settings.Save();

        // Apply immediately to running game loop
        var gameLoop = Program.GameLoop;
        if (gameLoop != null)
        {
            gameLoop.TargetFrameRate = fps;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    // ═══════════════════════════════════════════════════
    // Update Settings
    // ═══════════════════════════════════════════════════

    private void LoadUpdateSettings()
    {
        var settings = Program.Settings;
        var updateService = Program.UpdateService;

        // Display current version
        VersionText.Text = updateService?.CurrentVersion ?? "Unknown";

        // Select current frequency
        var frequencyIndex = settings.UpdateCheckFrequency switch
        {
            UpdateCheckFrequency.OnStartup => 0,
            UpdateCheckFrequency.Daily => 1,
            UpdateCheckFrequency.Weekly => 2,
            UpdateCheckFrequency.Never => 3,
            _ => 0
        };
        UpdateFrequencySelector.SelectedIndex = frequencyIndex;

        // Select current mode
        var modeIndex = settings.UpdateMode switch
        {
            UpdateMode.Notify => 0,
            UpdateMode.Silent => 1,
            _ => 0
        };
        UpdateModeSelector.SelectedIndex = modeIndex;

        // Wire up update service events if available
        if (updateService != null)
        {
            updateService.UpdateAvailable += OnUpdateAvailable;
            updateService.DownloadProgressChanged += OnDownloadProgressChanged;
            updateService.UpdateReady += OnUpdateReady;
        }
    }

    private void UpdateFrequencySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        var settings = Program.Settings;
        var selectedItem = UpdateFrequencySelector.SelectedItem as ComboBoxItem;
        var tag = selectedItem?.Tag?.ToString();

        settings.UpdateCheckFrequency = tag switch
        {
            "OnStartup" => UpdateCheckFrequency.OnStartup,
            "Daily" => UpdateCheckFrequency.Daily,
            "Weekly" => UpdateCheckFrequency.Weekly,
            "Never" => UpdateCheckFrequency.Never,
            _ => UpdateCheckFrequency.OnStartup
        };

        settings.Save();
    }

    private void UpdateModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        var settings = Program.Settings;
        var selectedItem = UpdateModeSelector.SelectedItem as ComboBoxItem;
        var tag = selectedItem?.Tag?.ToString();

        settings.UpdateMode = tag switch
        {
            "Notify" => UpdateMode.Notify,
            "Silent" => UpdateMode.Silent,
            _ => UpdateMode.Notify
        };

        settings.Save();
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        var updateService = Program.UpdateService;
        if (updateService == null)
        {
            UpdateStatusText.Text = "Update service not available";
            return;
        }

        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "Checking for updates...";

        try
        {
            var updateInfo = await updateService.CheckForUpdatesAsync();

            if (updateInfo == null)
            {
                UpdateStatusText.Text = "Failed to check for updates";
            }
            else if (updateInfo.IsUpdateAvailable)
            {
                _pendingUpdate = updateInfo;
                UpdateStatusText.Text = $"Version {updateInfo.NewVersion} available!";
                ApplyUpdateButton.Visibility = Visibility.Visible;
            }
            else
            {
                UpdateStatusText.Text = "You're up to date!";
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    private async void ApplyUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        var updateService = Program.UpdateService;
        if (updateService == null) return;

        ApplyUpdateButton.IsEnabled = false;
        ApplyUpdateButton.Content = "Downloading...";
        UpdateProgressBar.Visibility = Visibility.Visible;

        try
        {
            await updateService.DownloadUpdateAsync(new Progress<int>(p =>
            {
                Dispatcher.Invoke(() => UpdateProgressBar.Value = p);
            }));

            ApplyUpdateButton.Content = "Restarting...";
            updateService.ApplyUpdateAndRestart();
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Download failed: {ex.Message}";
            ApplyUpdateButton.Content = "Retry Download";
            ApplyUpdateButton.IsEnabled = true;
            UpdateProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void OnUpdateAvailable(UpdateInfo info)
    {
        Dispatcher.Invoke(() =>
        {
            _pendingUpdate = info;
            UpdateStatusText.Text = $"Version {info.NewVersion} available!";
            ApplyUpdateButton.Visibility = Visibility.Visible;
        });
    }

    private void OnDownloadProgressChanged(int progress)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateProgressBar.Value = progress;
        });
    }

    private void OnUpdateReady()
    {
        Dispatcher.Invoke(() =>
        {
            ApplyUpdateButton.Content = "Install Update & Restart";
            ApplyUpdateButton.IsEnabled = true;
            UpdateProgressBar.Visibility = Visibility.Collapsed;
        });
    }

    // ═══════════════════════════════════════════════════
    // Manage Settings (Save/Reload/Export/Import)
    // ═══════════════════════════════════════════════════

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MouseEffects");

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveSettingsButton.IsEnabled = false;
            BackupStatusText.Text = "Saving settings...";

            // Save app settings
            Program.Settings.Save();

            // Save active effect's settings if any
            var activeEffect = _effectManager.ActiveEffect;
            if (activeEffect != null)
            {
                Program.SavePluginSettings(activeEffect.Metadata.Id);
            }

            BackupStatusText.Text = $"Settings saved successfully at {DateTime.Now:HH:mm:ss}";
            Logger.Log("SettingsWindow", "All settings saved");
        }
        catch (Exception ex)
        {
            BackupStatusText.Text = $"Save failed: {ex.Message}";
            Logger.Log("SettingsWindow", $"Save failed: {ex.Message}");
        }
        finally
        {
            SaveSettingsButton.IsEnabled = true;
        }
    }

    private void ReloadSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReloadSettingsButton.IsEnabled = false;
            BackupStatusText.Text = "Reloading settings...";

            _isInitializing = true;

            // Reload app settings from disk
            var settings = AppSettings.Load();

            // Apply app settings to UI
            ThemeSelector.SelectedIndex = settings.Theme switch
            {
                AppTheme.System => 0,
                AppTheme.Light => 1,
                AppTheme.Dark => 2,
                _ => 0
            };

            FrameRateSlider.Value = settings.TargetFrameRate;
            FrameRateValue.Text = $"{settings.TargetFrameRate} fps";

            ShowFpsCheckBox.IsChecked = settings.ShowFpsCounter;
            ShowFpsOverlayCheckBox.IsChecked = settings.ShowFpsOverlay;
            UpdateFpsCounterVisibility(settings.ShowFpsCounter);

            ToggleHotkeyCheckBox.IsChecked = settings.EnableToggleHotkey;
            SettingsHotkeyCheckBox.IsChecked = settings.EnableSettingsHotkey;
            ScreenCaptureHotkeyCheckBox.IsChecked = settings.EnableScreenCaptureHotkey;

            // Apply settings to game loop
            var gameLoop = Program.GameLoop;
            if (gameLoop != null)
            {
                gameLoop.TargetFrameRate = settings.TargetFrameRate;
            }

            // Reload plugin settings from disk and apply to active effect
            var activeEffect = _effectManager.ActiveEffect;
            if (activeEffect != null)
            {
                var pluginSettings = PluginSettings.Load(activeEffect.Metadata.Id);
                pluginSettings.ApplyToEffect(activeEffect);
            }

            // Refresh plugin UI controls
            LoadPluginSettings();

            // Sync tray menu with reloaded states
            Program.SyncTrayWithEffects();

            _isInitializing = false;

            BackupStatusText.Text = $"Settings reloaded successfully at {DateTime.Now:HH:mm:ss}";
            Logger.Log("SettingsWindow", "All settings reloaded from disk");
        }
        catch (Exception ex)
        {
            BackupStatusText.Text = $"Reload failed: {ex.Message}";
            Logger.Log("SettingsWindow", $"Reload failed: {ex.Message}");
            _isInitializing = false;
        }
        finally
        {
            ReloadSettingsButton.IsEnabled = true;
        }
    }

    private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Export MouseEffects Settings",
                Filter = "MouseEffects Settings (*.me)|*.me",
                DefaultExt = ".me",
                FileName = $"MouseEffects_Settings_{DateTime.Now:yyyy-MM-dd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportSettingsButton.IsEnabled = false;
                BackupStatusText.Text = "Exporting settings...";

                // Delete existing file if it exists (SaveFileDialog already prompted for overwrite)
                if (File.Exists(saveDialog.FileName))
                {
                    File.Delete(saveDialog.FileName);
                }

                // Create the zip file with .me extension
                if (Directory.Exists(SettingsFolder))
                {
                    ZipFile.CreateFromDirectory(SettingsFolder, saveDialog.FileName, CompressionLevel.Optimal, false);
                    BackupStatusText.Text = $"Settings exported successfully to {Path.GetFileName(saveDialog.FileName)}";
                    Logger.Log("SettingsWindow", $"Settings exported to: {saveDialog.FileName}");
                }
                else
                {
                    BackupStatusText.Text = "No settings folder found to export.";
                }

                ExportSettingsButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            BackupStatusText.Text = $"Export failed: {ex.Message}";
            Logger.Log("SettingsWindow", $"Export failed: {ex.Message}");
            ExportSettingsButton.IsEnabled = true;
        }
    }

    private void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Import MouseEffects Settings",
                Filter = "MouseEffects Settings (*.me)|*.me",
                DefaultExt = ".me"
            };

            if (openDialog.ShowDialog() == true)
            {
                // Validate the archive first
                using (var archive = ZipFile.OpenRead(openDialog.FileName))
                {
                    var hasSettingsJson = archive.Entries.Any(entry =>
                        entry.FullName.Equals("settings.json", StringComparison.OrdinalIgnoreCase));
                    var hasPluginsFolder = archive.Entries.Any(entry =>
                        entry.FullName.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase) ||
                        entry.FullName.StartsWith("plugins\\", StringComparison.OrdinalIgnoreCase));

                    if (!hasSettingsJson && !hasPluginsFolder)
                    {
                        BackupStatusText.Text = "Invalid settings file: missing settings.json or plugins folder.";
                        return;
                    }
                }

                // Restart app with the .me file as argument - let command-line handler do the import
                // This avoids file locking issues since the app will close first
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    BackupStatusText.Text = "Restarting to import settings...";
                    Logger.Log("SettingsWindow", $"Restarting to import: {openDialog.FileName}");

                    System.Diagnostics.Process.Start(exePath, $"\"{openDialog.FileName}\"");
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    BackupStatusText.Text = "Could not determine application path.";
                }
            }
        }
        catch (Exception ex)
        {
            BackupStatusText.Text = $"Import failed: {ex.Message}";
            Logger.Log("SettingsWindow", $"Import failed: {ex.Message}");
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        // Minimize to tray instead of taskbar
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
            HideToTray();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide instead of close to preserve window state
        e.Cancel = true;
        HideToTray();
    }

    private void HideToTray()
    {
        _fpsTimer.Stop();

        // Remove topmost from settings window when hidden
        Topmost = false;

        // Resume overlay topmost enforcement
        Program.ResumeTopmostEnforcement();

        Hide();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        // Suspend overlay topmost enforcement so settings window can stay on top
        Program.SuspendTopmostEnforcement();

        // Make settings window topmost so it appears above the overlay
        Topmost = true;

        // Restart FPS timer when window becomes visible (if enabled)
        if (Program.Settings.ShowFpsCounter)
        {
            _fpsTimer.Start();
        }
    }
}
