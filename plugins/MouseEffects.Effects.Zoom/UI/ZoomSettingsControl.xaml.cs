using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Zoom.UI;

public partial class ZoomSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private readonly ZoomEffect? _zoomEffect;
    private bool _isInitializing = true;
    private bool _isExpanded;
    private Vector4 _borderColor = new(0.2f, 0.6f, 1.0f, 1.0f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public ZoomSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        _zoomEffect = effect as ZoomEffect;

        // Subscribe to hotkey configuration changes
        if (_zoomEffect != null)
        {
            _zoomEffect.ConfigurationChangedByHotkey += OnConfigurationChangedByHotkey;
        }

        LoadConfiguration();
        _isInitializing = false;
    }

    private void OnConfigurationChangedByHotkey()
    {
        // Use Dispatcher to update UI from the render thread
        Dispatcher.BeginInvoke(new Action(() =>
        {
            RefreshFromConfiguration();
            // Notify that settings changed for persistence
            SettingsChanged?.Invoke(_effect.Metadata.Id);
        }));
    }

    private void RefreshFromConfiguration()
    {
        _isInitializing = true;

        if (_effect.Configuration.TryGet("zoomFactor", out float zoom))
        {
            ZoomFactorSlider.Value = zoom;
            ZoomFactorValue.Text = $"{zoom:F1}x";
        }

        if (_effect.Configuration.TryGet("radius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("width", out float width))
        {
            WidthSlider.Value = width;
            WidthValue.Text = width.ToString("F0");
        }

        if (_effect.Configuration.TryGet("height", out float height))
        {
            HeightSlider.Value = height;
            HeightValue.Text = height.ToString("F0");
        }

        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("shapeType", out int shapeType))
        {
            ShapeComboBox.SelectedIndex = shapeType;
            UpdateShapePanelVisibility(shapeType);
        }

        if (_effect.Configuration.TryGet("zoomFactor", out float zoom))
        {
            ZoomFactorSlider.Value = zoom;
            ZoomFactorValue.Text = $"{zoom:F1}x";
        }

        if (_effect.Configuration.TryGet("radius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("width", out float width))
        {
            WidthSlider.Value = width;
            WidthValue.Text = width.ToString("F0");
        }

        if (_effect.Configuration.TryGet("height", out float height))
        {
            HeightSlider.Value = height;
            HeightValue.Text = height.ToString("F0");
        }

        if (_effect.Configuration.TryGet("syncSizes", out bool sync))
        {
            SyncSizesCheckBox.IsChecked = sync;
            UpdateHeightControlsEnabled(!sync);
        }

        if (_effect.Configuration.TryGet("borderWidth", out float borderWidth))
        {
            BorderWidthSlider.Value = borderWidth;
            BorderWidthValue.Text = borderWidth.ToString("F1");
        }

        if (_effect.Configuration.TryGet("borderColor", out Vector4 color))
        {
            _borderColor = color;
            UpdateBorderColorPreview();
        }

        if (_effect.Configuration.TryGet("enableZoomHotkey", out bool zoomHotkey))
        {
            ZoomHotkeyCheckBox.IsChecked = zoomHotkey;
        }

        if (_effect.Configuration.TryGet("enableSizeHotkey", out bool sizeHotkey))
        {
            SizeHotkeyCheckBox.IsChecked = sizeHotkey;
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("shapeType", ShapeComboBox.SelectedIndex);
        config.Set("zoomFactor", (float)ZoomFactorSlider.Value);
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("width", (float)WidthSlider.Value);
        config.Set("height", (float)HeightSlider.Value);
        config.Set("syncSizes", SyncSizesCheckBox.IsChecked ?? false);
        config.Set("borderWidth", (float)BorderWidthSlider.Value);
        config.Set("borderColor", _borderColor);
        config.Set("enableZoomHotkey", ZoomHotkeyCheckBox.IsChecked ?? false);
        config.Set("enableSizeHotkey", SizeHotkeyCheckBox.IsChecked ?? false);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateShapePanelVisibility(int shapeType)
    {
        CircleSettingsPanel.Visibility = shapeType == 0 ? Visibility.Visible : Visibility.Collapsed;
        RectangleSettingsPanel.Visibility = shapeType == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateHeightControlsEnabled(bool enabled)
    {
        HeightLabel.Opacity = enabled ? 1.0 : 0.5;
        HeightSlider.IsEnabled = enabled;
        HeightValue.Opacity = enabled ? 0.7 : 0.35;
    }

    private void UpdateBorderColorPreview()
    {
        BorderColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_borderColor.W * 255),
            (byte)(_borderColor.X * 255),
            (byte)(_borderColor.Y * 255),
            (byte)(_borderColor.Z * 255)));
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void ShapeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int shapeType = ShapeComboBox.SelectedIndex;
        UpdateShapePanelVisibility(shapeType);
        UpdateConfiguration();
    }

    private void ZoomFactorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomFactorValue != null)
            ZoomFactorValue.Text = $"{e.NewValue:F1}x";
        UpdateConfiguration();
    }

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WidthValue != null)
            WidthValue.Text = e.NewValue.ToString("F0");

        // If sync is enabled, update height to match width
        if (SyncSizesCheckBox?.IsChecked == true && !_isInitializing)
        {
            _isInitializing = true;
            HeightSlider.Value = e.NewValue;
            HeightValue.Text = e.NewValue.ToString("F0");
            _isInitializing = false;
        }

        UpdateConfiguration();
    }

    private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HeightValue != null)
            HeightValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SyncSizesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        bool isSync = SyncSizesCheckBox.IsChecked ?? false;
        UpdateHeightControlsEnabled(!isSync);

        // When enabling sync, set height to match width
        if (isSync)
        {
            _isInitializing = true;
            HeightSlider.Value = WidthSlider.Value;
            HeightValue.Text = WidthSlider.Value.ToString("F0");
            _isInitializing = false;
        }

        UpdateConfiguration();
    }

    private void BorderWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BorderWidthValue != null)
            BorderWidthValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void BorderColorPickerButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_borderColor.W * 255),
            (int)(_borderColor.X * 255),
            (int)(_borderColor.Y * 255),
            (int)(_borderColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _borderColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);

            UpdateBorderColorPreview();
            UpdateConfiguration();
        }
    }

    private void ZoomHotkeyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SizeHotkeyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
