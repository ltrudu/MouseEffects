using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Smoke.UI;

public partial class SmokeSettingsControl : UserControl
{
    private readonly SmokeEffect _effect;
    private bool _isLoading = true;

    public SmokeSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is not SmokeEffect smokeEffect)
            throw new ArgumentException("Effect must be SmokeEffect", nameof(effect));

        _effect = smokeEffect;

        Loaded += (s, e) => LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _isLoading = true;
        try
        {
            // Particle settings
            ParticleCountSlider.Value = _effect.ParticleCount;
            ParticleSizeSlider.Value = _effect.ParticleSize;
            LifetimeSlider.Value = _effect.ParticleLifetime;
            SpawnRateSlider.Value = _effect.SpawnRate;

            // Motion settings
            RiseSpeedSlider.Value = _effect.RiseSpeed;
            ExpansionRateSlider.Value = _effect.ExpansionRate;
            TurbulenceSlider.Value = _effect.TurbulenceStrength;

            // Visual settings
            OpacitySlider.Value = _effect.Opacity;
            SoftnessSlider.Value = _effect.Softness;

            // Color mode
            ColorModeCombo.SelectedIndex = _effect.ColorMode;
            UpdateColorPanelVisibility();

            // Trigger settings
            MouseMoveCheckBox.IsChecked = _effect.MouseMoveEnabled;
            MoveDistanceSlider.Value = _effect.MoveDistanceThreshold;
            UpdateMouseMovePanelVisibility();

            LeftClickCheckBox.IsChecked = _effect.LeftClickEnabled;
            LeftClickCountSlider.Value = _effect.LeftClickBurstCount;
            UpdateLeftClickPanelVisibility();

            RightClickCheckBox.IsChecked = _effect.RightClickEnabled;
            RightClickCountSlider.Value = _effect.RightClickBurstCount;
            UpdateRightClickPanelVisibility();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateColorPanelVisibility()
    {
        ColoredSmokePanel.Visibility = ColorModeCombo.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateMouseMovePanelVisibility()
    {
        MouseMovePanel.Visibility = MouseMoveCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateLeftClickPanelVisibility()
    {
        LeftClickPanel.Visibility = LeftClickCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRightClickPanelVisibility()
    {
        RightClickPanel.Visibility = RightClickCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ParticleCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ParticleCount = (int)ParticleCountSlider.Value;
        _effect.Configuration.Set("sm_particleCount", _effect.ParticleCount);
    }

    private void ParticleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ParticleSize = (float)ParticleSizeSlider.Value;
        _effect.Configuration.Set("sm_particleSize", _effect.ParticleSize);
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ParticleLifetime = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("sm_particleLifetime", _effect.ParticleLifetime);
    }

    private void SpawnRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.SpawnRate = (float)SpawnRateSlider.Value;
        _effect.Configuration.Set("sm_spawnRate", _effect.SpawnRate);
    }

    private void RiseSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RiseSpeed = (float)RiseSpeedSlider.Value;
        _effect.Configuration.Set("sm_riseSpeed", _effect.RiseSpeed);
    }

    private void ExpansionRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ExpansionRate = (float)ExpansionRateSlider.Value;
        _effect.Configuration.Set("sm_expansionRate", _effect.ExpansionRate);
    }

    private void TurbulenceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.TurbulenceStrength = (float)TurbulenceSlider.Value;
        _effect.Configuration.Set("sm_turbulenceStrength", _effect.TurbulenceStrength);
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.Opacity = (float)OpacitySlider.Value;
        _effect.Configuration.Set("sm_opacity", _effect.Opacity);
    }

    private void SoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.Softness = (float)SoftnessSlider.Value;
        _effect.Configuration.Set("sm_softness", _effect.Softness);
    }

    private void ColorModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        _effect.ColorMode = ColorModeCombo.SelectedIndex;
        _effect.Configuration.Set("sm_colorMode", _effect.ColorMode);
        UpdateColorPanelVisibility();
    }

    private void ColorPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        _effect.SmokeColor = ColorPresetCombo.SelectedIndex switch
        {
            0 => new Vector4(0.5f, 0.7f, 1f, 1f),       // Blue
            1 => new Vector4(0.7f, 0.5f, 1f, 1f),       // Purple
            2 => new Vector4(0.5f, 1f, 0.6f, 1f),       // Green
            3 => new Vector4(1f, 0.6f, 0.3f, 1f),       // Orange
            4 => new Vector4(1f, 0.5f, 0.8f, 1f),       // Pink
            _ => new Vector4(0.3f, 1f, 1f, 1f)          // Cyan
        };

        _effect.Configuration.Set("sm_smokeColor", _effect.SmokeColor);
    }

    private void MouseMoveCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.MouseMoveEnabled = MouseMoveCheckBox.IsChecked == true;
        _effect.Configuration.Set("sm_mouseMoveEnabled", _effect.MouseMoveEnabled);
        UpdateMouseMovePanelVisibility();
    }

    private void MoveDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.MoveDistanceThreshold = (float)MoveDistanceSlider.Value;
        _effect.Configuration.Set("sm_moveDistanceThreshold", _effect.MoveDistanceThreshold);
    }

    private void LeftClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.LeftClickEnabled = LeftClickCheckBox.IsChecked == true;
        _effect.Configuration.Set("sm_leftClickEnabled", _effect.LeftClickEnabled);
        UpdateLeftClickPanelVisibility();
    }

    private void LeftClickCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.LeftClickBurstCount = (int)LeftClickCountSlider.Value;
        _effect.Configuration.Set("sm_leftClickBurstCount", _effect.LeftClickBurstCount);
    }

    private void RightClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.RightClickEnabled = RightClickCheckBox.IsChecked == true;
        _effect.Configuration.Set("sm_rightClickEnabled", _effect.RightClickEnabled);
        UpdateRightClickPanelVisibility();
    }

    private void RightClickCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RightClickBurstCount = (int)RightClickCountSlider.Value;
        _effect.Configuration.Set("sm_rightClickBurstCount", _effect.RightClickBurstCount);
    }
}
