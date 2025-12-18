using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Portal.UI;

public partial class PortalSettingsControl : System.Windows.Controls.UserControl
{
    private PortalEffect? _effect;
    private bool _isLoading = true;

    public PortalSettingsControl(IEffect effect)
    {
        InitializeComponent();
        DataContext = effect;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PortalEffect portalEffect)
        {
            _effect = portalEffect;
            _isLoading = true;

            try
            {
                LoadConfiguration();
            }
            finally
            {
                _isLoading = false;
            }
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        RadiusSlider.Value = _effect.PortalRadius;
        SpiralArmsSlider.Value = _effect.SpiralArms;
        SpiralTightnessSlider.Value = _effect.SpiralTightness;
        RotationSpeedSlider.Value = _effect.RotationSpeed;
        GlowIntensitySlider.Value = _effect.GlowIntensity;
        DepthStrengthSlider.Value = _effect.DepthStrength;
        InnerDarknessSlider.Value = _effect.InnerDarkness;
        DistortionStrengthSlider.Value = _effect.DistortionStrength;
        RimParticlesCheckBox.IsChecked = _effect.RimParticlesEnabled;
        ParticleSpeedSlider.Value = _effect.ParticleSpeed;
        ColorThemeCombo.SelectedIndex = _effect.ColorTheme;
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PortalRadius = (float)RadiusSlider.Value;
        _effect.Configuration.Set("portalRadius", _effect.PortalRadius);
    }

    private void SpiralArmsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpiralArms = (int)SpiralArmsSlider.Value;
        _effect.Configuration.Set("spiralArms", _effect.SpiralArms);
    }

    private void SpiralTightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpiralTightness = (float)SpiralTightnessSlider.Value;
        _effect.Configuration.Set("spiralTightness", _effect.SpiralTightness);
    }

    private void RotationSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationSpeed = (float)RotationSpeedSlider.Value;
        _effect.Configuration.Set("rotationSpeed", _effect.RotationSpeed);
    }

    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("glowIntensity", _effect.GlowIntensity);
    }

    private void DepthStrengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.DepthStrength = (float)DepthStrengthSlider.Value;
        _effect.Configuration.Set("depthStrength", _effect.DepthStrength);
    }

    private void InnerDarknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.InnerDarkness = (float)InnerDarknessSlider.Value;
        _effect.Configuration.Set("innerDarkness", _effect.InnerDarkness);
    }

    private void DistortionStrengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.DistortionStrength = (float)DistortionStrengthSlider.Value;
        _effect.Configuration.Set("distortionStrength", _effect.DistortionStrength);
    }

    private void RimParticlesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RimParticlesEnabled = RimParticlesCheckBox.IsChecked == true;
        _effect.Configuration.Set("rimParticlesEnabled", _effect.RimParticlesEnabled);
    }

    private void ParticleSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ParticleSpeed = (float)ParticleSpeedSlider.Value;
        _effect.Configuration.Set("particleSpeed", _effect.ParticleSpeed);
    }

    private void ColorThemeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorTheme = ColorThemeCombo.SelectedIndex;
        _effect.Configuration.Set("colorTheme", _effect.ColorTheme);
    }
}
