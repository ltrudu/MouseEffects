using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.BlackHole.UI;

public partial class BlackHoleSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private bool _isExpanded;
    private Vector4 _diskColor = new(1.0f, 0.6f, 0.2f, 1.0f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public BlackHoleSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("radius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("distortionStrength", out float strength))
        {
            DistortionStrengthSlider.Value = strength;
            DistortionStrengthValue.Text = strength.ToString("F1");
        }

        if (_effect.Configuration.TryGet("eventHorizonSize", out float eventHorizon))
        {
            EventHorizonSlider.Value = eventHorizon;
            EventHorizonValue.Text = eventHorizon.ToString("F2");
        }

        if (_effect.Configuration.TryGet("accretionDiskEnabled", out bool diskEnabled))
        {
            AccretionDiskCheckBox.IsChecked = diskEnabled;
            UpdateAccretionDiskPanelVisibility(diskEnabled);
        }

        if (_effect.Configuration.TryGet("accretionDiskColor", out Vector4 color))
        {
            _diskColor = color;
            UpdateDiskColorPreview();
        }

        if (_effect.Configuration.TryGet("rotationSpeed", out float rotation))
        {
            RotationSpeedSlider.Value = rotation;
            RotationSpeedValue.Text = rotation.ToString("F1");
        }

        if (_effect.Configuration.TryGet("glowIntensity", out float glow))
        {
            GlowIntensitySlider.Value = glow;
            GlowIntensityValue.Text = glow.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("distortionStrength", (float)DistortionStrengthSlider.Value);
        config.Set("eventHorizonSize", (float)EventHorizonSlider.Value);
        config.Set("accretionDiskEnabled", AccretionDiskCheckBox.IsChecked ?? true);
        config.Set("accretionDiskColor", _diskColor);
        config.Set("rotationSpeed", (float)RotationSpeedSlider.Value);
        config.Set("glowIntensity", (float)GlowIntensitySlider.Value);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateAccretionDiskPanelVisibility(bool enabled)
    {
        AccretionDiskPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateDiskColorPreview()
    {
        DiskColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            255,
            (byte)(_diskColor.X * 255),
            (byte)(_diskColor.Y * 255),
            (byte)(_diskColor.Z * 255)));
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void DistortionStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DistortionStrengthValue != null)
            DistortionStrengthValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void EventHorizonSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EventHorizonValue != null)
            EventHorizonValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void AccretionDiskCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        bool enabled = AccretionDiskCheckBox.IsChecked ?? true;
        UpdateAccretionDiskPanelVisibility(enabled);
        UpdateConfiguration();
    }

    private void DiskColorPickerButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            255,
            (int)(_diskColor.X * 255),
            (int)(_diskColor.Y * 255),
            (int)(_diskColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _diskColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);

            UpdateDiskColorPreview();
            UpdateConfiguration();
        }
    }

    private void RotationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RotationSpeedValue != null)
            RotationSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
