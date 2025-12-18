using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Nebula.UI;

public partial class NebulaSettingsControl : UserControl
{
    private readonly NebulaEffect _effect;
    private bool _isLoading = true;

    public NebulaSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is not NebulaEffect nebulaEffect)
            throw new ArgumentException("Effect must be NebulaEffect", nameof(effect));

        _effect = nebulaEffect;

        Loaded += (s, e) => LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _isLoading = true;
        try
        {
            // Alpha
            AlphaSlider.Value = _effect.Alpha;

            // Cloud properties
            CloudDensitySlider.Value = _effect.CloudDensity;
            LayerCountSlider.Value = _effect.LayerCount;
            EffectRadiusSlider.Value = _effect.EffectRadius;
            NoiseScaleSlider.Value = _effect.NoiseScale;

            // Animation
            SwirlSpeedSlider.Value = _effect.SwirlSpeed;
            CloudSpeedSlider.Value = _effect.CloudSpeed;

            // Visual effects
            GlowIntensitySlider.Value = _effect.GlowIntensity;
            GlowAnimationSpeedSlider.Value = _effect.GlowAnimationSpeed;
            StarDensitySlider.Value = _effect.StarDensity;
            ColorVariationSlider.Value = _effect.ColorVariation;

            // Color palette
            ColorPaletteCombo.SelectedIndex = _effect.ColorPalette;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void AlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.Alpha = (float)AlphaSlider.Value;
        _effect.Configuration.Set("nb_alpha", _effect.Alpha);
    }

    private void CloudDensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.CloudDensity = (float)CloudDensitySlider.Value;
        _effect.Configuration.Set("nb_cloudDensity", _effect.CloudDensity);
    }

    private void LayerCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.LayerCount = (int)LayerCountSlider.Value;
        _effect.Configuration.Set("nb_layerCount", _effect.LayerCount);
    }

    private void EffectRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.EffectRadius = (float)EffectRadiusSlider.Value;
        _effect.Configuration.Set("nb_effectRadius", _effect.EffectRadius);
    }

    private void NoiseScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.NoiseScale = (float)NoiseScaleSlider.Value;
        _effect.Configuration.Set("nb_noiseScale", _effect.NoiseScale);
    }

    private void SwirlSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.SwirlSpeed = (float)SwirlSpeedSlider.Value;
        _effect.Configuration.Set("nb_swirlSpeed", _effect.SwirlSpeed);
    }

    private void CloudSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.CloudSpeed = (float)CloudSpeedSlider.Value;
        _effect.Configuration.Set("nb_cloudSpeed", _effect.CloudSpeed);
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("nb_glowIntensity", _effect.GlowIntensity);
    }

    private void GlowAnimationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowAnimationSpeed = (float)GlowAnimationSpeedSlider.Value;
        _effect.Configuration.Set("nb_glowAnimationSpeed", _effect.GlowAnimationSpeed);
    }

    private void StarDensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.StarDensity = (float)StarDensitySlider.Value;
        _effect.Configuration.Set("nb_starDensity", _effect.StarDensity);
    }

    private void ColorVariationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ColorVariation = (float)ColorVariationSlider.Value;
        _effect.Configuration.Set("nb_colorVariation", _effect.ColorVariation);
    }

    private void ColorPaletteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        _effect.ColorPalette = ColorPaletteCombo.SelectedIndex;
        _effect.Configuration.Set("nb_colorPalette", _effect.ColorPalette);

        // Show custom color panel only when Custom is selected
        CustomColorPanel.Visibility = _effect.ColorPalette == 3 ? Visibility.Visible : Visibility.Collapsed;
    }
}
