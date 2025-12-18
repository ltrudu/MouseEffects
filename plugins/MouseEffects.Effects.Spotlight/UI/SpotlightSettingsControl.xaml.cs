using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Spotlight.UI;

public partial class SpotlightSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;
    private bool _isExpanded;

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public SpotlightSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("spotlightRadius", out float radius))
        {
            SpotlightRadiusSlider.Value = radius;
            SpotlightRadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("edgeSoftness", out float softness))
        {
            EdgeSoftnessSlider.Value = softness;
            EdgeSoftnessValue.Text = softness.ToString("F0");
        }

        if (_effect.Configuration.TryGet("darknessLevel", out float darkness))
        {
            DarknessLevelSlider.Value = darkness;
            DarknessLevelValue.Text = darkness.ToString("F2");
        }

        if (_effect.Configuration.TryGet("brightnessBoost", out float brightness))
        {
            BrightnessBoostSlider.Value = brightness;
            BrightnessBoostValue.Text = brightness.ToString("F1");
        }

        if (_effect.Configuration.TryGet("colorTemperature", out int temperature))
        {
            ColorTemperatureCombo.SelectedIndex = temperature;
        }

        if (_effect.Configuration.TryGet("dustParticlesEnabled", out bool dustEnabled))
        {
            DustParticlesCheckBox.IsChecked = dustEnabled;
            UpdateDustParticlesPanelVisibility(dustEnabled);
        }

        if (_effect.Configuration.TryGet("dustDensity", out float density))
        {
            DustDensitySlider.Value = density;
            DustDensityValue.Text = density.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        _effect.Configuration.Set("spotlightRadius", (float)SpotlightRadiusSlider.Value);
        _effect.Configuration.Set("edgeSoftness", (float)EdgeSoftnessSlider.Value);
        _effect.Configuration.Set("darknessLevel", (float)DarknessLevelSlider.Value);
        _effect.Configuration.Set("brightnessBoost", (float)BrightnessBoostSlider.Value);
        _effect.Configuration.Set("colorTemperature", ColorTemperatureCombo.SelectedIndex);
        _effect.Configuration.Set("dustParticlesEnabled", DustParticlesCheckBox.IsChecked ?? true);
        _effect.Configuration.Set("dustDensity", (float)DustDensitySlider.Value);

        _effect.Configure(_effect.Configuration);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateDustParticlesPanelVisibility(bool enabled)
    {
        DustParticlesPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void SpotlightRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpotlightRadiusValue != null)
            SpotlightRadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void EdgeSoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeSoftnessValue != null)
            EdgeSoftnessValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void DarknessLevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DarknessLevelValue != null)
            DarknessLevelValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void BrightnessBoostSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessBoostValue != null)
            BrightnessBoostValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ColorTemperatureCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void DustParticlesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = DustParticlesCheckBox.IsChecked ?? true;
        UpdateDustParticlesPanelVisibility(enabled);
        UpdateConfiguration();
    }

    private void DustDensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DustDensityValue != null)
            DustDensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
