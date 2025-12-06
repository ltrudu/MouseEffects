using System.Drawing;
using System.Windows.Forms;
using MouseEffects.Core.Effects;

namespace MouseEffects.App.UI;

/// <summary>
/// Manages the system tray icon and context menu.
/// </summary>
public sealed class SystemTrayManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _effectsMenu;
    private readonly Dictionary<string, ToolStripMenuItem> _effectMenuItems = new();
    private bool _disposed;
    private bool _isUpdatingMenuItems;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;
    public event Action<bool>? EnabledChanged;
    public event Action<string, bool>? EffectToggled;

    public SystemTrayManager()
    {
        _contextMenu = new ContextMenuStrip();

        // Effects submenu (will be populated dynamically)
        _effectsMenu = new ToolStripMenuItem("Effects");

        // Main menu items
        _enabledItem = new ToolStripMenuItem("Enabled")
        {
            CheckOnClick = true,
            Checked = true
        };
        _enabledItem.CheckedChanged += (s, e) => EnabledChanged?.Invoke(_enabledItem.Checked);

        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (s, e) => SettingsRequested?.Invoke();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitRequested?.Invoke();

        _contextMenu.Items.Add(_enabledItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_effectsMenu);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(settingsItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "MouseEffects - Alt+Shift+M to toggle",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => SettingsRequested?.Invoke();
    }

    public bool IsEnabled
    {
        get => _enabledItem.Checked;
        set => _enabledItem.Checked = value;
    }

    /// <summary>
    /// Populate the effects submenu with the available effects.
    /// </summary>
    /// <param name="effects">List of effect metadata and their current enabled states.</param>
    public void PopulateEffectsMenu(IEnumerable<(EffectMetadata metadata, bool isEnabled)> effects)
    {
        _isUpdatingMenuItems = true;
        try
        {
            _effectsMenu.DropDownItems.Clear();
            _effectMenuItems.Clear();

            foreach (var (metadata, isEnabled) in effects)
            {
                var menuItem = new ToolStripMenuItem(metadata.Name)
                {
                    CheckOnClick = true,
                    Checked = isEnabled,
                    Tag = metadata.Id
                };

                menuItem.CheckedChanged += OnEffectMenuItemCheckedChanged;
                _effectsMenu.DropDownItems.Add(menuItem);
                _effectMenuItems[metadata.Id] = menuItem;
            }

            if (_effectsMenu.DropDownItems.Count == 0)
            {
                var noEffectsItem = new ToolStripMenuItem("(No effects loaded)")
                {
                    Enabled = false
                };
                _effectsMenu.DropDownItems.Add(noEffectsItem);
            }
        }
        finally
        {
            _isUpdatingMenuItems = false;
        }
    }

    private void OnEffectMenuItemCheckedChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingMenuItems) return;

        if (sender is ToolStripMenuItem menuItem && menuItem.Tag is string effectId)
        {
            EffectToggled?.Invoke(effectId, menuItem.Checked);
        }
    }

    /// <summary>
    /// Update the checked state of a specific effect menu item.
    /// </summary>
    public void SetEffectEnabled(string effectId, bool enabled)
    {
        if (_effectMenuItems.TryGetValue(effectId, out var menuItem))
        {
            _isUpdatingMenuItems = true;
            try
            {
                menuItem.Checked = enabled;
            }
            finally
            {
                _isUpdatingMenuItems = false;
            }
        }
    }

    /// <summary>
    /// Synchronize all effect menu items with the actual effect states.
    /// </summary>
    public void SyncEffectStates(IEnumerable<(string effectId, bool isEnabled)> states)
    {
        _isUpdatingMenuItems = true;
        try
        {
            foreach (var (effectId, isEnabled) in states)
            {
                if (_effectMenuItems.TryGetValue(effectId, out var menuItem))
                {
                    menuItem.Checked = isEnabled;
                }
            }
        }
        finally
        {
            _isUpdatingMenuItems = false;
        }
    }

    public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    private static Icon CreateDefaultIcon()
    {
        // Create a simple colored icon programmatically
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);

        // Draw a gradient circle
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 16, 16),
            Color.FromArgb(100, 150, 255),
            Color.FromArgb(255, 100, 200),
            45f);
        g.FillEllipse(brush, 1, 1, 14, 14);

        // Add highlight
        using var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
        g.FillEllipse(highlightBrush, 3, 2, 6, 4);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
