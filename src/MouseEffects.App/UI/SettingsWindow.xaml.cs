using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MouseEffects.App.Services;
using MouseEffects.App.Settings;
using MouseEffects.Core.Effects;
using MouseEffects.DirectX.Graphics;
using MouseEffects.Plugins;

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
    private readonly Dictionary<string, FrameworkElement> _effectSettingsControls = new();
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
        LoadGpuList();
        LoadFrameRateSetting();
        LoadFpsCounterSetting();
        LoadPluginSettings();
        LoadUpdateSettings();
        _isInitializing = false;
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
        if (show)
        {
            _fpsTimer.Start();
        }
        else
        {
            _fpsTimer.Stop();
        }
    }

    private void FpsTimer_Tick(object? sender, EventArgs e)
    {
        var gameLoop = Program.GameLoop;
        if (gameLoop != null)
        {
            var currentFps = gameLoop.CurrentFps;
            var targetFps = gameLoop.TargetFrameRate;
            FpsCounterText.Text = $"{currentFps:F1} / {targetFps} fps";

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

        PluginSettingsContainer.Children.Clear();
        _effectSettingsControls.Clear();

        foreach (var factory in _pluginLoader.Factories)
        {
            // Find the corresponding effect instance
            var effect = _effectManager.Effects.FirstOrDefault(e => e.Metadata.Id == factory.Metadata.Id);
            if (effect == null) continue;

            // Create the card container
            var card = CreateEffectCard(factory, effect);
            PluginSettingsContainer.Children.Add(card);
        }
    }

    private Border CreateEffectCard(IEffectFactory factory, IEffect effect)
    {
        var border = new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#313244")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 5, 0, 5)
        };

        var mainStack = new StackPanel();

        // Try to get plugin-provided settings control
        // (description is now handled inside each plugin's settings control)
        var settingsControl = factory.CreateSettingsControl(effect);
        if (settingsControl is FrameworkElement frameworkElement)
        {
            mainStack.Children.Add(frameworkElement);

            // Wire up settings change notifications for persistence
            WireUpSettingsChanged(settingsControl, effect.Metadata.Id);

            // Store reference to the settings control for external updates
            _effectSettingsControls[effect.Metadata.Id] = frameworkElement;
        }

        border.Child = mainStack;
        return border;
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
    /// Update the enabled checkbox for a specific effect.
    /// Called when an effect is toggled from the system tray.
    /// </summary>
    public void RefreshEffectEnabledState(string effectId, bool isEnabled)
    {
        if (_effectSettingsControls.TryGetValue(effectId, out var control))
        {
            // Find the checkbox in the visual tree (it's built by now)
            var checkBox = FindChild<System.Windows.Controls.CheckBox>(control, "EnabledCheckBox");
            if (checkBox != null)
            {
                checkBox.IsChecked = isEnabled;
            }
        }
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
        // Save settings and sync tray
        Program.SavePluginSettings(effectId);
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

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide instead of close to preserve window state
        e.Cancel = true;
        _fpsTimer.Stop();
        Hide();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        // Restart FPS timer when window becomes visible (if enabled)
        if (Program.Settings.ShowFpsCounter)
        {
            _fpsTimer.Start();
        }
    }
}
