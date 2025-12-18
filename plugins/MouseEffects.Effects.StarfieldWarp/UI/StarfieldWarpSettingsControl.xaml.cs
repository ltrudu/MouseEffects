using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.StarfieldWarp.UI;

public partial class StarfieldWarpSettingsControl : UserControl
{
    private StarfieldWarpEffect? _effect;
    private bool _isLoading = true;
    private bool _isExpanded;

    public StarfieldWarpSettingsControl(IEffect effect)
    {
        InitializeComponent();
        DataContext = effect;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StarfieldWarpEffect starfieldEffect)
        {
            _effect = starfieldEffect;
            _isLoading = true;

            try
            {
                EnabledCheckBox.IsChecked = _effect.IsEnabled;
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

        StarCountSlider.Value = _effect.StarCount;
        WarpSpeedSlider.Value = _effect.WarpSpeed;
        StreakLengthSlider.Value = _effect.StreakLength;
        EffectRadiusSlider.Value = _effect.EffectRadius;
        StarBrightnessSlider.Value = _effect.StarBrightness;
        ColorTintCheckBox.IsChecked = _effect.ColorTintEnabled;
        TunnelEffectCheckBox.IsChecked = _effect.TunnelEffect;
        TunnelDarknessSlider.Value = _effect.TunnelDarkness;
        StarSizeSlider.Value = _effect.StarSize;
        DepthLayersSlider.Value = _effect.DepthLayers;
        PulseEffectCheckBox.IsChecked = _effect.PulseEffect;
        PulseSpeedSlider.Value = _effect.PulseSpeed;
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked == true;
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "\u25B6" : "\u25BC";
    }

    private void StarCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.StarCount = (int)StarCountSlider.Value;
        _effect.Configuration.Set("sw_starCount", _effect.StarCount);
    }

    private void WarpSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.WarpSpeed = (float)WarpSpeedSlider.Value;
        _effect.Configuration.Set("sw_warpSpeed", _effect.WarpSpeed);
    }

    private void StreakLengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.StreakLength = (float)StreakLengthSlider.Value;
        _effect.Configuration.Set("sw_streakLength", _effect.StreakLength);
    }

    private void EffectRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EffectRadius = (float)EffectRadiusSlider.Value;
        _effect.Configuration.Set("sw_effectRadius", _effect.EffectRadius);
    }

    private void StarBrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.StarBrightness = (float)StarBrightnessSlider.Value;
        _effect.Configuration.Set("sw_starBrightness", _effect.StarBrightness);
    }

    private void ColorTintCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorTintEnabled = ColorTintCheckBox.IsChecked == true;
        _effect.Configuration.Set("sw_colorTintEnabled", _effect.ColorTintEnabled);
    }

    private void TunnelEffectCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.TunnelEffect = TunnelEffectCheckBox.IsChecked == true;
        _effect.Configuration.Set("sw_tunnelEffect", _effect.TunnelEffect);
    }

    private void TunnelDarknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.TunnelDarkness = (float)TunnelDarknessSlider.Value;
        _effect.Configuration.Set("sw_tunnelDarkness", _effect.TunnelDarkness);
    }

    private void StarSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.StarSize = (float)StarSizeSlider.Value;
        _effect.Configuration.Set("sw_starSize", _effect.StarSize);
    }

    private void DepthLayersSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.DepthLayers = (int)DepthLayersSlider.Value;
        _effect.Configuration.Set("sw_depthLayers", _effect.DepthLayers);
    }

    private void PulseEffectCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PulseEffect = PulseEffectCheckBox.IsChecked == true;
        _effect.Configuration.Set("sw_pulseEffect", _effect.PulseEffect);
    }

    private void PulseSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PulseSpeed = (float)PulseSpeedSlider.Value;
        _effect.Configuration.Set("sw_pulseSpeed", _effect.PulseSpeed);
    }
}
