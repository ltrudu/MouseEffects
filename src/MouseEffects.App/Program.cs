using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using MouseEffects.App.Services;
using MouseEffects.App.Settings;
using MouseEffects.App.UI;
using MouseEffects.Core.Diagnostics;
using MouseEffects.Core.Effects;
using MouseEffects.Core.UI;
using MouseEffects.Input;
using MouseEffects.Plugins;
using Velopack;

namespace MouseEffects.App;

static partial class Program
{
    private const string MutexName = "MouseEffects_SingleInstance_Mutex";
    private static Mutex? _singleInstanceMutex;

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
    private static readonly HashSet<string> _pressedHotkeys = new();

    [STAThread]
    static void Main(string[] args)
    {
        // Velopack MUST be first - handles update apply/restart
        VelopackApp.Build().Run();

        // Initialize logger
        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "debug.log");
        Logger.Initialize(logPath);
        Logger.Log("Application starting...");

        // Check for command-line import of .me settings file
        var isImporting = args.Length > 0 && File.Exists(args[0]) && args[0].EndsWith(".me", StringComparison.OrdinalIgnoreCase);

        if (isImporting)
        {
            // Kill any existing instance before importing
            KillExistingInstances();
            HandleSettingsImport(args[0]);
            return; // App will restart after import
        }

        // Single instance check - exit if another instance is already running
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Log("Another instance is already running. Exiting.");
            _singleInstanceMutex?.Dispose();
            return;
        }

        // Load settings early so we can apply theme
        _settings = AppSettings.Load();
        Log($"Loaded settings - Theme: {_settings.Theme}, GPU: {_settings.SelectedGpuName ?? "auto"}");

        // Register .me file association (per-user, no admin required)
        FileAssociationHelper.RegisterFileAssociation();

        // Create WPF Application with ModernWpf theming
        var wpfApp = new App();
        wpfApp.ApplyTheme(_settings.Theme);

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
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
        }
    }

    /// <summary>
    /// Kill any existing MouseEffects instances.
    /// </summary>
    private static void KillExistingInstances()
    {
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var processName = currentProcess.ProcessName;

        foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName))
        {
            if (process.Id != currentProcess.Id)
            {
                try
                {
                    Log($"Killing existing instance (PID: {process.Id})");
                    process.Kill();
                    process.WaitForExit(5000); // Wait up to 5 seconds
                }
                catch (Exception ex)
                {
                    Log($"Failed to kill process {process.Id}: {ex.Message}");
                }
            }
        }

        // Give a moment for file handles to be released
        Thread.Sleep(500);
    }

    /// <summary>
    /// Handle importing a .me settings file from command-line (double-click).
    /// </summary>
    private static void HandleSettingsImport(string filePath)
    {
        try
        {
            Log($"Importing settings from: {filePath}");

            var settingsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MouseEffects");

            // Validate the archive
            using (var archive = ZipFile.OpenRead(filePath))
            {
                var hasSettingsJson = archive.Entries.Any(entry =>
                    entry.FullName.Equals("settings.json", StringComparison.OrdinalIgnoreCase));
                var hasPluginsFolder = archive.Entries.Any(entry =>
                    entry.FullName.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.StartsWith("plugins\\", StringComparison.OrdinalIgnoreCase));

                if (!hasSettingsJson && !hasPluginsFolder)
                {
                    MessageBox.Show(
                        "Invalid settings file: missing settings.json or plugins folder.",
                        "Import Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            // Confirm with user
            var result = MessageBox.Show(
                $"Import settings from:\n{Path.GetFileName(filePath)}\n\n" +
                "This will replace all your current settings.\n\n" +
                "Do you want to continue?",
                "Import MouseEffects Settings",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            // Delete existing settings with retry logic
            if (Directory.Exists(settingsFolder))
            {
                DeleteDirectoryWithRetry(settingsFolder, maxRetries: 5, delayMs: 500);
            }

            // Create settings folder and extract
            Directory.CreateDirectory(settingsFolder);
            ZipFile.ExtractToDirectory(filePath, settingsFolder, overwriteFiles: true);

            Log("Settings imported successfully");

            // Show success and restart
            MessageBox.Show(
                "Settings imported successfully!\n\nMouseEffects will now start with the imported settings.",
                "Import Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Restart the application
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.Diagnostics.Process.Start(exePath);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("SettingsImport", ex);
            MessageBox.Show(
                $"Failed to import settings:\n{ex.Message}",
                "Import Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Delete a directory with retry logic for locked files.
    /// </summary>
    private static void DeleteDirectoryWithRetry(string path, int maxRetries, int delayMs)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // First try to delete all files
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch
                    {
                        // Will retry on next iteration
                    }
                }

                // Then delete directories from deepest to shallowest
                var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                    .OrderByDescending(d => d.Length)
                    .ToList();

                foreach (var dir in dirs)
                {
                    try
                    {
                        Directory.Delete(dir, false);
                    }
                    catch
                    {
                        // Will retry on next iteration
                    }
                }

                // Finally delete the root
                Directory.Delete(path, true);
                return; // Success
            }
            catch (Exception ex)
            {
                Log($"Delete attempt {i + 1}/{maxRetries} failed: {ex.Message}");
                if (i < maxRetries - 1)
                {
                    Thread.Sleep(delayMs);
                }
                else
                {
                    throw; // Final attempt failed
                }
            }
        }
    }

    public static AppSettings Settings => _settings;
    public static OverlayManager? OverlayManager => _overlayManager;
    public static PluginLoader? PluginLoader => _pluginLoader;
    public static EffectManager? EffectManager => _effectManager;
    public static GameLoop? GameLoop => _gameLoop;
    public static UpdateService? UpdateService => _updateService;

    // Track which consumers need capture FPS tracking
    private static bool _settingsWindowNeedsCaptureFps;
    private static bool _fpsOverlayNeedsCaptureFps;

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

    /// <summary>
    /// Update capture FPS tracking state from the settings window.
    /// </summary>
    public static void SetSettingsWindowNeedsCaptureFps(bool needs)
    {
        _settingsWindowNeedsCaptureFps = needs;
        UpdateCaptureFpsTracking();
    }

    /// <summary>
    /// Update capture FPS tracking state from the FPS overlay.
    /// </summary>
    public static void SetFpsOverlayNeedsCaptureFps(bool needs)
    {
        _fpsOverlayNeedsCaptureFps = needs;
        UpdateCaptureFpsTracking();
    }

    /// <summary>
    /// Enable capture FPS tracking if any consumer needs it.
    /// </summary>
    private static void UpdateCaptureFpsTracking()
    {
        var needsTracking = _settingsWindowNeedsCaptureFps || _fpsOverlayNeedsCaptureFps;
        _gameLoop?.SetTrackCaptureFps(needsTracking);
    }

    /// <summary>
    /// Temporarily suspend overlay topmost state to allow modal dialogs to appear.
    /// Call ResumeOverlayTopmost() when the dialog is closed.
    /// </summary>
    public static void SuspendOverlayTopmost()
    {
        _overlayManager?.SuspendTopmost();
    }

    /// <summary>
    /// Resume overlay topmost state after modal dialog is closed.
    /// </summary>
    public static void ResumeOverlayTopmost()
    {
        _overlayManager?.ResumeTopmost();
    }

    /// <summary>
    /// Suspend topmost enforcement to allow dialogs to appear above the overlay.
    /// The overlay keeps its topmost state, but periodic enforcement is paused.
    /// </summary>
    public static void SuspendTopmostEnforcement()
    {
        _overlayManager?.SuspendTopmostEnforcement();
    }

    /// <summary>
    /// Resume topmost enforcement after dialogs are closed.
    /// </summary>
    public static void ResumeTopmostEnforcement()
    {
        _overlayManager?.ResumeTopmostEnforcement();
    }

    private static void Initialize()
    {
        try
        {
            // Settings already loaded in Main() for early theme application
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

            // Initialize global dialog helper for plugins
            DialogHelper.Initialize(SuspendTopmostEnforcement, ResumeTopmostEnforcement);
            Log("DialogHelper initialized");

            // Create effect manager with shared render context
            Log("Creating EffectManager...");
            var primaryOverlay = _overlayManager.Overlays[0];
            _effectManager = new EffectManager(primaryOverlay.RenderContext);

            // Load plugins from plugins directory (async for parallel loading)
            var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
            Log($"Loading plugins from: {pluginsPath}");
            _pluginLoader = new PluginLoader(pluginsPath);

            // Subscribe to progress events for logging
            _pluginLoader.PluginLoaded += (name, current, total) =>
                Log($"  Loaded plugin {current}/{total}: {name}");

            // Load plugins asynchronously (parallel assembly loading)
            _pluginLoader.LoadPluginsAsync().GetAwaiter().GetResult();

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
            _trayManager.MenuOpened += OnTrayMenuOpened;
            _trayManager.MenuClosed += OnTrayMenuClosed;

            // Populate the effects menu dynamically from loaded plugins
            PopulateTrayEffectsMenu();

            _trayManager.ShowBalloon("MouseEffects", "Running in background. Press Alt+Shift+M to toggle.");

            // Register hotkey (Alt+Shift+M to toggle) if enabled
            if (_settings.EnableToggleHotkey)
            {
                var hotkeyResult = RegisterHotKey(nint.Zero, HOTKEY_ID, MOD_ALT | MOD_SHIFT, (uint)'M');
                Log($"Hotkey registration (Alt+Shift+M): {(hotkeyResult ? "SUCCESS" : "FAILED")}");
            }

            // Register screen capture hotkey (Alt+Shift+S) if enabled
            if (_settings.EnableScreenCaptureHotkey)
            {
                var screenshotHotkeyResult = RegisterHotKey(nint.Zero, HOTKEY_SCREENSHOT_ID, MOD_ALT | MOD_SHIFT, (uint)'S');
                Log($"Screenshot hotkey registration: {(screenshotHotkeyResult ? "SUCCESS" : "FAILED")}");
            }

            // Register settings window hotkey (Alt+Shift+L) if enabled
            if (_settings.EnableSettingsHotkey)
            {
                var settingsHotkeyResult = RegisterHotKey(nint.Zero, HOTKEY_SETTINGS_ID, MOD_ALT | MOD_SHIFT, (uint)'L');
                var settingsHotkeyError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                Log($"Settings hotkey registration (Alt+Shift+L): {(settingsHotkeyResult ? "SUCCESS" : $"FAILED (error: {settingsHotkeyError})")}");
            }

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
        UnregisterHotKey(nint.Zero, HOTKEY_SCREENSHOT_ID);
        UnregisterHotKey(nint.Zero, HOTKEY_SETTINGS_ID);

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

    /// <summary>
    /// Handle effect enabled from settings window.
    /// Enforces single-plugin mode: only one effect can be enabled at a time.
    /// Returns list of effect IDs that were disabled.
    /// </summary>
    public static List<string> HandleEffectEnabledFromSettings(string effectId)
    {
        if (_effectManager == null) return [];

        // Effect was just enabled - disable all others
        var disabledEffects = new List<string>();

        foreach (var effect in _effectManager.Effects)
        {
            if (effect.Metadata.Id != effectId && effect.IsEnabled)
            {
                effect.IsEnabled = false;
                disabledEffects.Add(effect.Metadata.Id);
                SavePluginSettings(effect.Metadata.Id);
            }
        }

        // Sync tray menu
        SyncTrayWithEffects();

        return disabledEffects;
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

                if (msg.message == WM_HOTKEY)
                {
                    if (msg.wParam == HOTKEY_ID)
                    {
                        ToggleEffects();
                    }
                    else if (msg.wParam == HOTKEY_SCREENSHOT_ID)
                    {
                        CaptureScreenToClipboard();
                    }
                    else if (msg.wParam == HOTKEY_SETTINGS_ID)
                    {
                        ToggleSettingsWindow();
                    }
                }

                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }

            // Run one iteration of game loop
            _gameLoop?.Tick();

            // Check plugin hotkeys
            CheckPluginHotkeys();

            // Small sleep to prevent 100% CPU usage
            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// Check for plugin-defined hotkeys and execute their callbacks.
    /// </summary>
    private static void CheckPluginHotkeys()
    {
        if (_effectManager == null) return;

        foreach (var effect in _effectManager.Effects)
        {
            if (effect is not IHotkeyProvider hotkeyProvider) continue;

            foreach (var hotkey in hotkeyProvider.GetHotkeys())
            {
                if (!hotkey.IsEnabled) continue;

                string hotkeyId = $"{effect.Metadata.Id}:{hotkey.Id}";
                bool isPressed = IsHotkeyPressed(hotkey);

                if (isPressed && !_pressedHotkeys.Contains(hotkeyId))
                {
                    // Hotkey just pressed - invoke callback
                    _pressedHotkeys.Add(hotkeyId);
                    try
                    {
                        hotkey.Callback?.Invoke();
                        Log($"Hotkey triggered: {hotkeyId}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PluginHotkey", ex);
                    }
                }
                else if (!isPressed && _pressedHotkeys.Contains(hotkeyId))
                {
                    // Hotkey released
                    _pressedHotkeys.Remove(hotkeyId);
                }
            }
        }
    }

    /// <summary>
    /// Check if a hotkey combination is currently pressed.
    /// </summary>
    private static bool IsHotkeyPressed(HotkeyDefinition hotkey)
    {
        // Check modifiers
        bool ctrlRequired = hotkey.Modifiers.HasFlag(HotkeyModifiers.Ctrl);
        bool shiftRequired = hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift);
        bool altRequired = hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt);

        bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
        bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
        bool altPressed = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;

        if (ctrlRequired != ctrlPressed) return false;
        if (shiftRequired != shiftPressed) return false;
        if (altRequired != altPressed) return false;

        // Check main key
        int vk = (int)hotkey.Key;
        return (GetAsyncKeyState(vk) & 0x8000) != 0;
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
        if (_effectManager != null)
        {
            _effectManager.IsGloballyPaused = !enabled;
        }
    }

    private static void OnTrayMenuOpened()
    {
        SuspendTopmostEnforcement();
    }

    private static void OnTrayMenuClosed()
    {
        ResumeTopmostEnforcement();
    }

    private static void OnEffectToggled(string effectId, bool enabled)
    {
        if (_effectManager == null) return;

        List<string> changedEffects;

        if (enabled)
        {
            // Enforce single-plugin mode: enable only this effect, disable all others
            changedEffects = _effectManager.EnableExclusively(effectId);
        }
        else
        {
            // Just disable this specific effect
            var effect = _effectManager.Effects.FirstOrDefault(e => e.Metadata.Id == effectId);
            if (effect != null && effect.IsEnabled)
            {
                effect.IsEnabled = false;
                changedEffects = [effectId];
            }
            else
            {
                changedEffects = [];
            }
        }

        // Save settings and update UI for all changed effects
        foreach (var changedId in changedEffects)
        {
            SavePluginSettings(changedId);

            var changedEffect = _effectManager.Effects.FirstOrDefault(e => e.Metadata.Id == changedId);
            if (changedEffect != null)
            {
                _settingsWindow?.RefreshEffectEnabledState(changedId, changedEffect.IsEnabled);
            }
        }

        // Sync the tray menu to reflect all effect states
        SyncTrayWithEffects();
    }

    private static void ToggleEffects()
    {
        _effectsEnabled = !_effectsEnabled;
        if (_effectManager != null)
        {
            _effectManager.IsGloballyPaused = !_effectsEnabled;
        }

        if (_trayManager != null)
        {
            _trayManager.IsEnabled = _effectsEnabled;
            _trayManager.ShowBalloon("MouseEffects",
                _effectsEnabled ? "Effects enabled" : "Effects disabled");
        }
    }

    private static void ToggleSettingsWindow()
    {
        if (_effectManager == null) return;

        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(_effectManager);
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }
        else if (_settingsWindow.IsVisible)
        {
            _settingsWindow.Hide();
        }
        else
        {
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }
    }

    /// <summary>
    /// Capture the entire screen (including overlay) to clipboard.
    /// Blends overlay content with screen capture since overlay is excluded from normal capture.
    /// </summary>
    private static void CaptureScreenToClipboard()
    {
        try
        {
            // Get virtual screen bounds (all monitors)
            int screenLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int screenTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int screenWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            // Create bitmap and capture screen background
            using var bitmap = new System.Drawing.Bitmap(screenWidth, screenHeight);
            using var graphics = System.Drawing.Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(screenLeft, screenTop, 0, 0, bitmap.Size);

            // Blend overlay content on top of screen capture
            if (_overlayManager != null)
            {
                foreach (var overlay in _overlayManager.Overlays)
                {
                    try
                    {
                        // Capture the DirectX content of this overlay
                        using var overlayBitmap = overlay.CaptureFrame();
                        if (overlayBitmap != null)
                        {
                            // Calculate position relative to virtual screen origin
                            int destX = overlay.Bounds.X - screenLeft;
                            int destY = overlay.Bounds.Y - screenTop;

                            // Draw overlay content with alpha blending onto screen capture
                            graphics.DrawImage(overlayBitmap, destX, destY, overlayBitmap.Width, overlayBitmap.Height);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to capture overlay: {ex.Message}");
                    }
                }
            }

            // Copy composited result to clipboard
            System.Windows.Forms.Clipboard.SetImage(bitmap);

            Log($"Screen captured to clipboard ({screenWidth}x{screenHeight})");
            _trayManager?.ShowBalloon("MouseEffects", "Screen captured to clipboard!");
        }
        catch (Exception ex)
        {
            Log($"Screen capture failed: {ex.Message}");
            _trayManager?.ShowBalloon("MouseEffects", "Screen capture failed!");
        }
    }

    /// <summary>
    /// Update the screen capture hotkey registration based on settings.
    /// </summary>
    public static void UpdateScreenCaptureHotkey(bool enabled)
    {
        if (enabled)
        {
            var result = RegisterHotKey(nint.Zero, HOTKEY_SCREENSHOT_ID, MOD_ALT | MOD_SHIFT, (uint)'S');
            Log($"Screenshot hotkey registration: {(result ? "SUCCESS" : "FAILED")}");
        }
        else
        {
            UnregisterHotKey(nint.Zero, HOTKEY_SCREENSHOT_ID);
            Log("Screenshot hotkey unregistered");
        }
    }

    /// <summary>
    /// Update the toggle effects hotkey registration based on settings.
    /// </summary>
    public static void UpdateToggleHotkey(bool enabled)
    {
        if (enabled)
        {
            var result = RegisterHotKey(nint.Zero, HOTKEY_ID, MOD_ALT | MOD_SHIFT, (uint)'M');
            Log($"Toggle hotkey registration: {(result ? "SUCCESS" : "FAILED")}");
        }
        else
        {
            UnregisterHotKey(nint.Zero, HOTKEY_ID);
            Log("Toggle hotkey unregistered");
        }
    }

    /// <summary>
    /// Update the settings window hotkey registration based on settings.
    /// </summary>
    public static void UpdateSettingsHotkey(bool enabled)
    {
        if (enabled)
        {
            var result = RegisterHotKey(nint.Zero, HOTKEY_SETTINGS_ID, MOD_ALT | MOD_SHIFT, (uint)'L');
            Log($"Settings hotkey registration: {(result ? "SUCCESS" : "FAILED")}");
        }
        else
        {
            UnregisterHotKey(nint.Zero, HOTKEY_SETTINGS_ID);
            Log("Settings hotkey unregistered");
        }
    }

    #region Native Methods

    private const int HOTKEY_ID = 1;
    private const int HOTKEY_SCREENSHOT_ID = 2;
    private const int HOTKEY_SETTINGS_ID = 3;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint PM_REMOVE = 0x0001;
    private const uint WM_QUIT = 0x0012;
    private const uint WM_HOTKEY = 0x0312;

    // Virtual key codes for GetAsyncKeyState
    private const int VK_CONTROL = 0x11;
    private const int VK_SHIFT = 0x10;
    private const int VK_MENU = 0x12; // Alt key

    // Screen metrics for multi-monitor support
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

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

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PeekMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TranslateMessage(ref MSG lpMsg);

    [LibraryImport("user32.dll")]
    private static partial nint DispatchMessageW(ref MSG lpMsg);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(nint hWnd, int id);

    [LibraryImport("user32.dll")]
    private static partial void PostQuitMessage(int nExitCode);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int nIndex);

    [LibraryImport("user32.dll")]
    private static partial short GetAsyncKeyState(int vKey);

    #endregion
}
