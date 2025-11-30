using System.IO;
using System.Runtime.InteropServices;
using MouseEffects.App.Services;
using MouseEffects.App.Settings;
using MouseEffects.App.UI;
using MouseEffects.Core.Diagnostics;
using MouseEffects.Core.Effects;
using MouseEffects.Input;
using MouseEffects.Plugins;
using Velopack;

namespace MouseEffects.App;

static class Program
{
    private static GameLoop? _gameLoop;
    private static OverlayManager? _overlayManager;
    private static EffectManager? _effectManager;
    private static PluginLoader? _pluginLoader;
    private static GlobalMouseHook? _mouseInput;
    private static SystemTrayManager? _trayManager;
    private static SettingsWindow? _settingsWindow;
    private static FpsOverlayWindow? _fpsOverlay;
    private static AppSettings _settings = new();
    private static bool _effectsEnabled = true;
    private static UpdateService? _updateService;

    [STAThread]
    static void Main()
    {
        // Velopack MUST be first - handles update apply/restart
        VelopackApp.Build().Run();

        // Initialize logger
        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "debug.log");
        Logger.Initialize(logPath);
        Logger.Log("Application starting...");

        // Required for WPF
        System.Windows.Application wpfApp = new();

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            Initialize();
            RunMessageLoop();
        }
        catch (Exception ex)
        {
            Logger.Error("Main", ex);
            MessageBox.Show($"Error: {ex.Message}", "MouseEffects Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Shutdown();
        }
    }

    public static AppSettings Settings => _settings;
    public static OverlayManager? OverlayManager => _overlayManager;
    public static PluginLoader? PluginLoader => _pluginLoader;
    public static EffectManager? EffectManager => _effectManager;
    public static GameLoop? GameLoop => _gameLoop;
    public static UpdateService? UpdateService => _updateService;

    /// <summary>
    /// Show or hide the FPS overlay on screen.
    /// </summary>
    public static void SetFpsOverlayVisible(bool visible)
    {
        if (visible)
        {
            if (_fpsOverlay == null)
            {
                _fpsOverlay = new FpsOverlayWindow();
            }
            _fpsOverlay.Show();
        }
        else
        {
            _fpsOverlay?.Hide();
        }
    }

    private static void Initialize()
    {
        try
        {
            // Load settings
            Log("Loading settings...");
            _settings = AppSettings.Load();
            Log($"Preferred GPU: {_settings.SelectedGpuName ?? "auto"}");

            Log("Creating OverlayManager...");
            _overlayManager = new OverlayManager(_settings.SelectedGpuName);

            Log("Initializing overlays...");
            _overlayManager.Initialize();

            if (_overlayManager.Overlays.Count == 0)
            {
                throw new InvalidOperationException("Failed to create overlay windows");
            }
            Log($"Created {_overlayManager.Overlays.Count} overlay(s)");

            // Create effect manager with shared render context
            Log("Creating EffectManager...");
            var primaryOverlay = _overlayManager.Overlays[0];
            _effectManager = new EffectManager(primaryOverlay.RenderContext);

            // Load plugins from plugins directory
            var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
            Log($"Loading plugins from: {pluginsPath}");
            _pluginLoader = new PluginLoader(pluginsPath);
            _pluginLoader.LoadPlugins();

            // Register discovered effect factories
            Log($"Registering {_pluginLoader.Factories.Count} effect factories...");
            foreach (var factory in _pluginLoader.Factories)
            {
                _effectManager.RegisterFactory(factory);
                Log($"  Registered: {factory.Metadata.Name}");
            }

            // Create effect instances for all registered factories
            Log("Creating effect instances...");
            foreach (var factory in _pluginLoader.Factories)
            {
                _effectManager.CreateEffect(factory.Metadata.Id);
                Log($"  Created: {factory.Metadata.Id}");
            }

            // Apply saved plugin settings and enabled states
            Log("Applying saved plugin settings...");
            ApplySavedPluginSettings();

            // Create mouse input provider
            Log("Creating mouse input...");
            _mouseInput = new GlobalMouseHook();

            // Create game loop
            Log("Creating game loop...");
            _gameLoop = new GameLoop(_overlayManager, _effectManager, _mouseInput);
            _gameLoop.TargetFrameRate = _settings.TargetFrameRate;
            Log($"Target frame rate: {_gameLoop.TargetFrameRate} fps");
            _gameLoop.Start();

            // Create system tray
            Log("Creating system tray...");
            _trayManager = new SystemTrayManager();
            _trayManager.SettingsRequested += OnSettingsRequested;
            _trayManager.ExitRequested += OnExitRequested;
            _trayManager.EnabledChanged += OnEnabledChanged;
            _trayManager.EffectToggled += OnEffectToggled;

            // Populate the effects menu dynamically from loaded plugins
            PopulateTrayEffectsMenu();

            _trayManager.ShowBalloon("MouseEffects", "Running in background. Press Ctrl+Shift+M to toggle.");

            // Register hotkey (Ctrl+Shift+M to toggle)
            var hotkeyResult = RegisterHotKey(nint.Zero, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)'M');
            Log($"Hotkey registration: {(hotkeyResult ? "SUCCESS" : "FAILED")}");

            // Initialize FPS overlay if enabled
            if (_settings.ShowFpsOverlay)
            {
                SetFpsOverlayVisible(true);
            }

            // Initialize update service and check for updates in background
            InitializeUpdateService();

            Log("Initialization complete!");
            Log($"Effects count: {_effectManager.Effects.Count}");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex}");
            throw;
        }
    }

    private static void Log(string message) => Logger.Log("Program", message);

    private static void InitializeUpdateService()
    {
        try
        {
            Log("Initializing update service...");
            _updateService = new UpdateService();

            // Wire up update events
            _updateService.UpdateAvailable += OnUpdateAvailable;
            _updateService.UpdateReady += OnUpdateReady;

            // Start background update check
            _ = Task.Run(async () =>
            {
                try
                {
                    await _updateService.CheckAndApplyUpdatesAsync(_settings);
                }
                catch (Exception ex)
                {
                    Logger.Error("UpdateService", ex);
                }
            });

            Log($"Update service initialized (version: {_updateService.CurrentVersion})");
        }
        catch (Exception ex)
        {
            Log($"Failed to initialize update service: {ex.Message}");
            // Don't fail startup if update service fails
        }
    }

    private static void OnUpdateAvailable(Services.UpdateInfo info)
    {
        if (_settings.UpdateMode == UpdateMode.Notify)
        {
            _trayManager?.ShowBalloon("Update Available",
                $"MouseEffects {info.NewVersion} is available. Click to update.");
        }
    }

    private static void OnUpdateReady()
    {
        if (_settings.UpdateMode == UpdateMode.Notify)
        {
            _trayManager?.ShowBalloon("Update Ready",
                "Update downloaded. Restart to apply.");
        }
    }

    private static void Shutdown()
    {
        UnregisterHotKey(nint.Zero, HOTKEY_ID);

        // Save all plugin settings before shutdown (each to its own file)
        Log("Saving plugin settings on shutdown...");
        SaveAllPluginSettings();

        _fpsOverlay?.Close();
        _settingsWindow?.Close();
        _trayManager?.Dispose();
        _gameLoop?.Dispose();
        _effectManager?.Dispose();
        _mouseInput?.Dispose();
        _overlayManager?.Dispose();
    }

    /// <summary>
    /// Apply saved plugin enabled states and configurations to loaded effects.
    /// Each plugin has its own settings file.
    /// </summary>
    private static void ApplySavedPluginSettings()
    {
        if (_effectManager == null) return;

        foreach (var effect in _effectManager.Effects)
        {
            var effectId = effect.Metadata.Id;

            // Load plugin-specific settings file
            var pluginSettings = PluginSettings.Load(effectId);
            pluginSettings.ApplyToEffect(effect);

            Log($"  {effectId}: enabled={effect.IsEnabled}, {pluginSettings.Configuration.Count} config values");
        }
    }

    /// <summary>
    /// Populate the system tray effects menu with loaded plugins.
    /// </summary>
    private static void PopulateTrayEffectsMenu()
    {
        if (_trayManager == null || _effectManager == null) return;

        var effectsInfo = _effectManager.Effects
            .Select(e => (e.Metadata, e.IsEnabled))
            .ToList();

        _trayManager.PopulateEffectsMenu(effectsInfo);
        Log($"Populated tray menu with {effectsInfo.Count} effects");
    }

    /// <summary>
    /// Synchronize tray menu checkboxes with current effect states.
    /// </summary>
    public static void SyncTrayWithEffects()
    {
        if (_trayManager == null || _effectManager == null) return;

        var states = _effectManager.Effects
            .Select(e => (e.Metadata.Id, e.IsEnabled))
            .ToList();

        _trayManager.SyncEffectStates(states);
    }

    /// <summary>
    /// Save all plugin settings to their individual files.
    /// </summary>
    private static void SaveAllPluginSettings()
    {
        if (_effectManager == null) return;

        foreach (var effect in _effectManager.Effects)
        {
            var effectId = effect.Metadata.Id;
            var pluginSettings = new PluginSettings();
            pluginSettings.SaveFromEffect(effect);
            pluginSettings.Save(effectId);
        }
    }

    /// <summary>
    /// Save a specific plugin's settings to its own file.
    /// </summary>
    public static void SavePluginSettings(string effectId)
    {
        if (_effectManager == null) return;

        var effect = _effectManager.Effects.FirstOrDefault(e => e.Metadata.Id == effectId);
        if (effect != null)
        {
            var pluginSettings = new PluginSettings();
            pluginSettings.SaveFromEffect(effect);
            pluginSettings.Save(effectId);
            Log($"Saved settings for {effectId}");
        }
    }

    private static void RunMessageLoop()
    {
        var msg = new MSG();

        while (true)
        {
            // Process all pending Windows messages
            while (PeekMessageW(out msg, nint.Zero, 0, 0, PM_REMOVE))
            {
                if (msg.message == WM_QUIT)
                {
                    return;
                }

                if (msg.message == WM_HOTKEY && msg.wParam == HOTKEY_ID)
                {
                    ToggleEffects();
                }

                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }

            // Run one iteration of game loop
            _gameLoop?.Tick();

            // Small sleep to prevent 100% CPU usage
            Thread.Sleep(1);
        }
    }

    private static void OnSettingsRequested()
    {
        if (_effectManager == null) return;

        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(_effectManager);
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private static void OnExitRequested()
    {
        PostQuitMessage(0);
    }

    private static void OnEnabledChanged(bool enabled)
    {
        _effectsEnabled = enabled;
        _effectManager?.SetAllEnabled(enabled);
    }

    private static void OnEffectToggled(string effectId, bool enabled)
    {
        if (_effectManager == null) return;

        var effect = _effectManager.Effects.FirstOrDefault(e => e.Metadata.Id == effectId);
        if (effect != null)
        {
            effect.IsEnabled = enabled;

            // Save the plugin settings immediately to its own file
            SavePluginSettings(effectId);

            // Update the settings window checkbox if it's open
            _settingsWindow?.RefreshEffectEnabledState(effectId, enabled);
        }
    }

    private static void ToggleEffects()
    {
        _effectsEnabled = !_effectsEnabled;
        _effectManager?.SetAllEnabled(_effectsEnabled);

        if (_trayManager != null)
        {
            _trayManager.IsEnabled = _effectsEnabled;
            _trayManager.ShowBalloon("MouseEffects",
                _effectsEnabled ? "Effects enabled" : "Effects disabled");
        }
    }

    #region Native Methods

    private const int HOTKEY_ID = 1;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint PM_REMOVE = 0x0001;
    private const uint WM_QUIT = 0x0012;
    private const uint WM_HOTKEY = 0x0312;

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref MSG lpMsg);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    #endregion
}
