using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.ProceduralSigil.UI;

public partial class ProceduralSigilSettingsControl : UserControl
{
    private readonly ProceduralSigilEffect _effect;
    private bool _isLoading = true; // Start true to prevent events during init

    public ProceduralSigilSettingsControl(IEffect effect)
    {
        _effect = (ProceduralSigilEffect)effect;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;

        // Position
        PositionModeCombo.SelectedIndex = (int)_effect.Position;
        SigilRadiusSlider.Value = _effect.SigilRadius;
        FadeDurationSlider.Value = _effect.FadeDuration;
        UpdateFadeDurationVisibility();

        // Appearance
        ColorPresetCombo.SelectedIndex = (int)_effect.Preset;
        GlowIntensitySlider.Value = _effect.GlowIntensity;
        LineThicknessSlider.Value = _effect.LineThickness;

        // Layers
        LayerCenterCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Center) != 0;
        LayerInnerCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Inner) != 0;
        LayerMiddleCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Middle) != 0;
        LayerRunesCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Runes) != 0;
        LayerGlowCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Glow) != 0;

        // Animation
        RotationSpeedSlider.Value = _effect.RotationSpeed;
        CounterRotateCheckBox.IsChecked = _effect.CounterRotateLayers;
        PulseCheckBox.IsChecked = (_effect.Animations & ProceduralSigilEffect.SigilAnimations.Pulse) != 0;
        PulseSpeedSlider.Value = _effect.PulseSpeed;
        MorphCheckBox.IsChecked = (_effect.Animations & ProceduralSigilEffect.SigilAnimations.Morph) != 0;
        RuneScrollSpeedSlider.Value = _effect.RuneScrollSpeed;

        _isLoading = false;
    }

    private void UpdateFadeDurationVisibility()
    {
        var mode = (ProceduralSigilEffect.PositionMode)PositionModeCombo.SelectedIndex;
        bool showFade = mode == ProceduralSigilEffect.PositionMode.ClickToSummon ||
                       mode == ProceduralSigilEffect.PositionMode.ClickAtCursor;
        FadeDurationLabel.Visibility = showFade ? Visibility.Visible : Visibility.Collapsed;
        FadeDurationGrid.Visibility = showFade ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PositionModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var mode = (ProceduralSigilEffect.PositionMode)PositionModeCombo.SelectedIndex;
        _effect.Position = mode;
        _effect.Configuration.Set("positionMode", (int)mode);
        UpdateFadeDurationVisibility();
    }

    private void SigilRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.SigilRadius = (float)e.NewValue;
        _effect.Configuration.Set("sigilRadius", (float)e.NewValue);
    }

    private void FadeDurationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FadeDuration = (float)e.NewValue;
        _effect.Configuration.Set("fadeDuration", (float)e.NewValue);
    }

    private void ColorPresetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var preset = (ProceduralSigilEffect.ColorPreset)ColorPresetCombo.SelectedIndex;
        _effect.Preset = preset;
        _effect.Configuration.Set("colorPreset", (int)preset);
    }

    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowIntensity = (float)e.NewValue;
        _effect.Configuration.Set("glowIntensity", (float)e.NewValue);
    }

    private void LineThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.LineThickness = (float)e.NewValue;
        _effect.Configuration.Set("lineThickness", (float)e.NewValue);
    }

    private void LayerCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        var layers = ProceduralSigilEffect.SigilLayers.None;
        if (LayerCenterCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Center;
        if (LayerInnerCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Inner;
        if (LayerMiddleCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Middle;
        if (LayerRunesCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Runes;
        if (LayerGlowCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Glow;

        _effect.Layers = layers;
        _effect.Configuration.Set("layerFlags", (uint)layers);
    }

    private void RotationSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RotationSpeed = (float)e.NewValue;
        _effect.Configuration.Set("rotationSpeed", (float)e.NewValue);
    }

    private void CounterRotateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.CounterRotateLayers = CounterRotateCheckBox.IsChecked == true;
        _effect.Configuration.Set("counterRotateLayers", _effect.CounterRotateLayers);
    }

    private void AnimationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        var animations = ProceduralSigilEffect.SigilAnimations.Rotate; // Always enabled
        if (PulseCheckBox.IsChecked == true) animations |= ProceduralSigilEffect.SigilAnimations.Pulse;
        if (MorphCheckBox.IsChecked == true) animations |= ProceduralSigilEffect.SigilAnimations.Morph;

        _effect.Animations = animations;
        _effect.Configuration.Set("animationFlags", (uint)animations);
    }

    private void PulseSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.PulseSpeed = (float)e.NewValue;
        _effect.Configuration.Set("pulseSpeed", (float)e.NewValue);
    }

    private void RuneScrollSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RuneScrollSpeed = (float)e.NewValue;
        _effect.Configuration.Set("runeScrollSpeed", (float)e.NewValue);
    }
}
