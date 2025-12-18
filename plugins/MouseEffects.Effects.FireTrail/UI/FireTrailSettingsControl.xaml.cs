using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.FireTrail.UI;

/// <summary>
/// Interaction logic for FireTrailSettingsControl.xaml
/// </summary>
public partial class FireTrailSettingsControl : System.Windows.Controls.UserControl
{
    private readonly FireTrailEffect? _effect;
    private bool _isLoading = true;

    public FireTrailSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is FireTrailEffect fireTrailEffect)
        {
            _effect = fireTrailEffect;
            LoadConfiguration();
        }

        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        _isLoading = true;
        try
        {
            // General
            EnabledCheckBox.IsChecked = _effect.Enabled;
            IntensitySlider.Value = _effect.Intensity;
            IntensityValue.Text = _effect.Intensity.ToString("F1");
            LifetimeSlider.Value = _effect.ParticleLifetime;
            LifetimeValue.Text = $"{_effect.ParticleLifetime:F1}s";

            // Flame Appearance
            FireStyleCombo.SelectedIndex = _effect.FireStyle;
            HeightSlider.Value = _effect.FlameHeight;
            HeightValue.Text = _effect.FlameHeight.ToString("F0");
            WidthSlider.Value = _effect.FlameWidth;
            WidthValue.Text = _effect.FlameWidth.ToString("F0");
            TurbulenceSlider.Value = _effect.TurbulenceAmount;
            TurbulenceValue.Text = _effect.TurbulenceAmount.ToString("F2");
            FlickerSpeedSlider.Value = _effect.FlickerSpeed;
            FlickerSpeedValue.Text = _effect.FlickerSpeed.ToString("F0");

            // Color & Glow
            GlowSlider.Value = _effect.GlowIntensity;
            GlowValue.Text = _effect.GlowIntensity.ToString("F1");
            SaturationSlider.Value = _effect.ColorSaturation;
            SaturationValue.Text = _effect.ColorSaturation.ToString("F1");

            // Particle Types
            SmokeSlider.Value = _effect.SmokeAmount;
            SmokeValue.Text = _effect.SmokeAmount.ToString("F2");
            EmberSlider.Value = _effect.EmberAmount;
            EmberValue.Text = _effect.EmberAmount.ToString("F2");

            // HDR
            HdrEnabledCheckBox.IsChecked = _effect.HdrEnabled;
            HdrBrightnessSlider.Value = _effect.HdrBrightness;
            HdrBrightnessValue.Text = _effect.HdrBrightness.ToString("F1");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentPanel.Visibility == Visibility.Collapsed)
        {
            ContentPanel.Visibility = Visibility.Visible;
            FoldButton.Content = "\u25B2"; // Up arrow
        }
        else
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            FoldButton.Content = "\u25BC"; // Down arrow
        }
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Enabled = EnabledCheckBox.IsChecked ?? false;
        _effect.Configuration.Set("ft_enabled", _effect.Enabled);
    }

    private void IntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Intensity = (float)IntensitySlider.Value;
        IntensityValue.Text = _effect.Intensity.ToString("F1");
        _effect.Configuration.Set("ft_intensity", _effect.Intensity);
    }

    private void LifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ParticleLifetime = (float)LifetimeSlider.Value;
        LifetimeValue.Text = $"{_effect.ParticleLifetime:F1}s";
        _effect.Configuration.Set("ft_particleLifetime", _effect.ParticleLifetime);
    }

    private void FireStyleCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FireStyle = FireStyleCombo.SelectedIndex;
        _effect.Configuration.Set("ft_fireStyle", _effect.FireStyle);
    }

    private void HeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FlameHeight = (float)HeightSlider.Value;
        HeightValue.Text = _effect.FlameHeight.ToString("F0");
        _effect.Configuration.Set("ft_flameHeight", _effect.FlameHeight);
    }

    private void WidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FlameWidth = (float)WidthSlider.Value;
        WidthValue.Text = _effect.FlameWidth.ToString("F0");
        _effect.Configuration.Set("ft_flameWidth", _effect.FlameWidth);
    }

    private void TurbulenceSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.TurbulenceAmount = (float)TurbulenceSlider.Value;
        TurbulenceValue.Text = _effect.TurbulenceAmount.ToString("F2");
        _effect.Configuration.Set("ft_turbulenceAmount", _effect.TurbulenceAmount);
    }

    private void FlickerSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FlickerSpeed = (float)FlickerSpeedSlider.Value;
        FlickerSpeedValue.Text = _effect.FlickerSpeed.ToString("F0");
        _effect.Configuration.Set("ft_flickerSpeed", _effect.FlickerSpeed);
    }

    private void GlowSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowSlider.Value;
        GlowValue.Text = _effect.GlowIntensity.ToString("F1");
        _effect.Configuration.Set("ft_glowIntensity", _effect.GlowIntensity);
    }

    private void SaturationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorSaturation = (float)SaturationSlider.Value;
        SaturationValue.Text = _effect.ColorSaturation.ToString("F1");
        _effect.Configuration.Set("ft_colorSaturation", _effect.ColorSaturation);
    }

    private void SmokeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SmokeAmount = (float)SmokeSlider.Value;
        SmokeValue.Text = _effect.SmokeAmount.ToString("F2");
        _effect.Configuration.Set("ft_smokeAmount", _effect.SmokeAmount);
    }

    private void EmberSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EmberAmount = (float)EmberSlider.Value;
        EmberValue.Text = _effect.EmberAmount.ToString("F2");
        _effect.Configuration.Set("ft_emberAmount", _effect.EmberAmount);
    }

    private void HdrEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.HdrEnabled = HdrEnabledCheckBox.IsChecked ?? false;
        _effect.Configuration.Set("ft_hdrEnabled", _effect.HdrEnabled);
    }

    private void HdrBrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.HdrBrightness = (float)HdrBrightnessSlider.Value;
        HdrBrightnessValue.Text = _effect.HdrBrightness.ToString("F1");
        _effect.Configuration.Set("ft_hdrBrightness", _effect.HdrBrightness);
    }
}
