using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.MagneticField.UI;

public partial class MagneticFieldSettingsControl : UserControl
{
    private MagneticFieldEffect? _effect;
    private bool _isLoading = true;

    public MagneticFieldSettingsControl(IEffect effect)
    {
        InitializeComponent();
        DataContext = effect;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MagneticFieldEffect magneticFieldEffect)
        {
            _effect = magneticFieldEffect;
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

        LineCountSlider.Value = _effect.LineCount;
        FieldStrengthSlider.Value = _effect.FieldStrength;
        FieldCurvatureSlider.Value = _effect.FieldCurvature;
        EffectRadiusSlider.Value = _effect.EffectRadius;
        AnimationSpeedSlider.Value = _effect.AnimationSpeed;
        FlowScaleSlider.Value = _effect.FlowScale;
        FlowSpeedSlider.Value = _effect.FlowSpeed;
        LineThicknessSlider.Value = _effect.LineThickness;
        GlowIntensitySlider.Value = _effect.GlowIntensity;
        DualPoleModeCheckBox.IsChecked = _effect.DualPoleMode;
        PoleSeparationSlider.Value = _effect.PoleSeparation;
        ColorModeCombo.SelectedIndex = _effect.ColorMode;
    }

    private void LineCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LineCount = (int)LineCountSlider.Value;
        _effect.Configuration.Set("mf_lineCount", _effect.LineCount);
    }

    private void FieldStrengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FieldStrength = (float)FieldStrengthSlider.Value;
        _effect.Configuration.Set("mf_fieldStrength", _effect.FieldStrength);
    }

    private void FieldCurvatureSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FieldCurvature = (float)FieldCurvatureSlider.Value;
        _effect.Configuration.Set("mf_fieldCurvature", _effect.FieldCurvature);
    }

    private void EffectRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EffectRadius = (float)EffectRadiusSlider.Value;
        _effect.Configuration.Set("mf_effectRadius", _effect.EffectRadius);
    }

    private void AnimationSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.AnimationSpeed = (float)AnimationSpeedSlider.Value;
        _effect.Configuration.Set("mf_animationSpeed", _effect.AnimationSpeed);
    }

    private void FlowScaleSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FlowScale = (float)FlowScaleSlider.Value;
        _effect.Configuration.Set("mf_flowScale", _effect.FlowScale);
    }

    private void FlowSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FlowSpeed = (float)FlowSpeedSlider.Value;
        _effect.Configuration.Set("mf_flowSpeed", _effect.FlowSpeed);
    }

    private void LineThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LineThickness = (float)LineThicknessSlider.Value;
        _effect.Configuration.Set("mf_lineThickness", _effect.LineThickness);
    }

    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("mf_glowIntensity", _effect.GlowIntensity);
    }

    private void DualPoleModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.DualPoleMode = DualPoleModeCheckBox.IsChecked == true;
        _effect.Configuration.Set("mf_dualPoleMode", _effect.DualPoleMode);
    }

    private void PoleSeparationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PoleSeparation = (float)PoleSeparationSlider.Value;
        _effect.Configuration.Set("mf_poleSeparation", _effect.PoleSeparation);
    }

    private void ColorModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorMode = ColorModeCombo.SelectedIndex;
        _effect.Configuration.Set("mf_colorMode", _effect.ColorMode);
    }
}
