using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.InkBlot.UI;

public partial class InkBlotSettingsControl : System.Windows.Controls.UserControl
{
    private InkBlotEffect? _effect;
    private bool _isLoading = true;

    public InkBlotSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect as InkBlotEffect;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        try
        {
            // Physics
            DropRadiusSlider.Value = _effect.DropRadius;
            GravitySlider.Value = _effect.Gravity;
            SurfaceTensionSlider.Value = _effect.SurfaceTension;
            ViscositySlider.Value = _effect.Viscosity;

            // Appearance
            ColorModeCombo.SelectedIndex = _effect.ColorMode;
            RainbowSpeedSlider.Value = _effect.RainbowSpeed;
            UpdateRainbowSpeedVisibility();
            ThresholdSlider.Value = _effect.MetaballThreshold;
            EdgeSoftnessSlider.Value = _effect.EdgeSoftness;
            OpacitySlider.Value = _effect.Opacity;
            GlowSlider.Value = _effect.GlowIntensity;
            AnimateGlowCheck.IsChecked = _effect.AnimateGlow;
            GlowMinSlider.Value = _effect.GlowMin;
            GlowMaxSlider.Value = _effect.GlowMax;
            GlowAnimSpeedSlider.Value = _effect.GlowAnimSpeed;
            UpdateGlowAnimVisibility();
            InnerDarkeningSlider.Value = _effect.InnerDarkening;
            LifetimeSlider.Value = _effect.Lifetime;

            // Spawning
            SpawnOnClickCheck.IsChecked = _effect.SpawnOnClick;
            SpawnOnMoveCheck.IsChecked = _effect.SpawnOnMove;
            MoveDistanceSlider.Value = _effect.MoveDistance;
            SpawnSpreadSlider.Value = _effect.SpawnSpread;
            DropsPerSpawnSlider.Value = _effect.DropsPerSpawn;
            MaxDropsPerSecondSlider.Value = _effect.MaxDropsPerSecond;
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Physics handlers
    private void DropRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.DropRadius = (float)DropRadiusSlider.Value;
        _effect.Configuration.Set("dropRadius", _effect.DropRadius);
    }

    private void GravitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Gravity = (float)GravitySlider.Value;
        _effect.Configuration.Set("gravity", _effect.Gravity);
    }

    private void SurfaceTensionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SurfaceTension = (float)SurfaceTensionSlider.Value;
        _effect.Configuration.Set("surfaceTension", _effect.SurfaceTension);
    }

    private void ViscositySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Viscosity = (float)ViscositySlider.Value;
        _effect.Configuration.Set("viscosity", _effect.Viscosity);
    }

    private void UpdateRainbowSpeedVisibility()
    {
        RainbowSpeedPanel.Visibility = ColorModeCombo.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    // Appearance handlers
    private void ColorModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorMode = ColorModeCombo.SelectedIndex;
        _effect.Configuration.Set("colorMode", _effect.ColorMode);
        UpdateRainbowSpeedVisibility();
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("rainbowSpeed", _effect.RainbowSpeed);
    }

    private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MetaballThreshold = (float)ThresholdSlider.Value;
        _effect.Configuration.Set("metaballThreshold", _effect.MetaballThreshold);
    }

    private void EdgeSoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EdgeSoftness = (float)EdgeSoftnessSlider.Value;
        _effect.Configuration.Set("edgeSoftness", _effect.EdgeSoftness);
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Opacity = (float)OpacitySlider.Value;
        _effect.Configuration.Set("opacity", _effect.Opacity);
    }

    private void GlowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowSlider.Value;
        _effect.Configuration.Set("glowIntensity", _effect.GlowIntensity);
    }

    private void UpdateGlowAnimVisibility()
    {
        GlowAnimPanel.Visibility = AnimateGlowCheck.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AnimateGlowCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.AnimateGlow = AnimateGlowCheck.IsChecked == true;
        _effect.Configuration.Set("animateGlow", _effect.AnimateGlow);
        UpdateGlowAnimVisibility();
    }

    private void GlowMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowMin = (float)GlowMinSlider.Value;
        _effect.Configuration.Set("glowMin", _effect.GlowMin);
    }

    private void GlowMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowMax = (float)GlowMaxSlider.Value;
        _effect.Configuration.Set("glowMax", _effect.GlowMax);
    }

    private void GlowAnimSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowAnimSpeed = (float)GlowAnimSpeedSlider.Value;
        _effect.Configuration.Set("glowAnimSpeed", _effect.GlowAnimSpeed);
    }

    private void InnerDarkeningSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.InnerDarkening = (float)InnerDarkeningSlider.Value;
        _effect.Configuration.Set("innerDarkening", _effect.InnerDarkening);
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Lifetime = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("lifetime", _effect.Lifetime);
    }

    // Spawn handlers
    private void SpawnOnClickCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpawnOnClick = SpawnOnClickCheck.IsChecked == true;
        _effect.Configuration.Set("spawnOnClick", _effect.SpawnOnClick);
    }

    private void SpawnOnMoveCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpawnOnMove = SpawnOnMoveCheck.IsChecked == true;
        _effect.Configuration.Set("spawnOnMove", _effect.SpawnOnMove);
    }

    private void MoveDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MoveDistance = (float)MoveDistanceSlider.Value;
        _effect.Configuration.Set("moveDistance", _effect.MoveDistance);
    }

    private void SpawnSpreadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpawnSpread = (float)SpawnSpreadSlider.Value;
        _effect.Configuration.Set("spawnSpread", _effect.SpawnSpread);
    }

    private void DropsPerSpawnSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.DropsPerSpawn = (int)DropsPerSpawnSlider.Value;
        _effect.Configuration.Set("dropsPerSpawn", _effect.DropsPerSpawn);
    }

    private void MaxDropsPerSecondSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxDropsPerSecond = (int)MaxDropsPerSecondSlider.Value;
        _effect.Configuration.Set("maxDropsPerSecond", _effect.MaxDropsPerSecond);
    }

    private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Pass mouse wheel events to parent ScrollViewer
        if (!e.Handled)
        {
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            };
            var parent = ((FrameworkElement)sender).Parent as UIElement;
            parent?.RaiseEvent(eventArg);
        }
    }
}
