using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.NeonGlow.UI;

public partial class NeonGlowSettingsControl : UserControl
{
    private readonly NeonGlowEffect _effect;
    private bool _isLoading = true;

    public NeonGlowSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is not NeonGlowEffect neonEffect)
            throw new ArgumentException("Effect must be NeonGlowEffect", nameof(effect));

        _effect = neonEffect;

        Loaded += (s, e) => LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _isLoading = true;
        try
        {
            // Trail settings
            TrailLengthSlider.Value = _effect.MaxTrailLength;
            TrailSpacingSlider.Value = _effect.TrailSpacing;

            // Line and glow
            LineThicknessSlider.Value = _effect.LineThickness;
            GlowLayersSlider.Value = _effect.GlowLayers;
            GlowIntensitySlider.Value = _effect.GlowIntensity;

            // Fade and smoothing
            FadeSpeedSlider.Value = _effect.FadeSpeed;
            SmoothingSlider.Value = _effect.SmoothingFactor;

            // Color mode
            ColorModeCombo.SelectedIndex = _effect.ColorMode;
            UpdateColorPanelVisibility();

            // Rainbow speed
            RainbowSpeedSlider.Value = _effect.RainbowSpeed;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateColorPanelVisibility()
    {
        int mode = ColorModeCombo.SelectedIndex;

        FixedColorPanel.Visibility = mode == 0 ? Visibility.Visible : Visibility.Collapsed;
        GradientColorPanel.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;
        RainbowSpeedPanel.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TrailLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.MaxTrailLength = (int)TrailLengthSlider.Value;
        _effect.Configuration.Set("ng_maxTrailPoints", _effect.MaxTrailLength);
    }

    private void TrailSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.TrailSpacing = (float)TrailSpacingSlider.Value;
        _effect.Configuration.Set("ng_trailSpacing", _effect.TrailSpacing);
    }

    private void LineThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.LineThickness = (float)LineThicknessSlider.Value;
        _effect.Configuration.Set("ng_lineThickness", _effect.LineThickness);
    }

    private void GlowLayersSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowLayers = (int)GlowLayersSlider.Value;
        _effect.Configuration.Set("ng_glowLayers", _effect.GlowLayers);
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("ng_glowIntensity", _effect.GlowIntensity);
    }

    private void FadeSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FadeSpeed = (float)FadeSpeedSlider.Value;
        _effect.Configuration.Set("ng_fadeSpeed", _effect.FadeSpeed);
    }

    private void SmoothingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.SmoothingFactor = (float)SmoothingSlider.Value;
        _effect.Configuration.Set("ng_smoothingFactor", _effect.SmoothingFactor);
    }

    private void ColorModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        _effect.ColorMode = ColorModeCombo.SelectedIndex;
        _effect.Configuration.Set("ng_colorMode", _effect.ColorMode);
        UpdateColorPanelVisibility();
    }

    private void FixedColorPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        _effect.PrimaryColor = FixedColorPresetCombo.SelectedIndex switch
        {
            0 => new Vector4(1f, 0.08f, 0.58f, 1f),     // Hot Pink
            1 => new Vector4(0f, 1f, 1f, 1f),           // Cyan
            2 => new Vector4(0.54f, 0f, 1f, 1f),        // Purple
            3 => new Vector4(0.24f, 1f, 0f, 1f),        // Neon Green
            4 => new Vector4(1f, 0.55f, 0f, 1f),        // Orange
            _ => new Vector4(0.13f, 0.59f, 1f, 1f)      // Electric Blue
        };

        _effect.Configuration.Set("ng_primaryColor", _effect.PrimaryColor);
    }

    private void GradientPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        (_effect.PrimaryColor, _effect.SecondaryColor) = GradientPresetCombo.SelectedIndex switch
        {
            0 => (new Vector4(1f, 0.08f, 0.58f, 1f), new Vector4(0f, 1f, 1f, 1f)),           // Pink to Cyan
            1 => (new Vector4(0.54f, 0f, 1f, 1f), new Vector4(0.13f, 0.59f, 1f, 1f)),       // Purple to Blue
            2 => (new Vector4(1f, 0.55f, 0f, 1f), new Vector4(1f, 0.08f, 0.58f, 1f)),       // Orange to Pink
            3 => (new Vector4(0.24f, 1f, 0f, 1f), new Vector4(0f, 1f, 1f, 1f)),             // Green to Cyan
            _ => (new Vector4(0.13f, 0.59f, 1f, 1f), new Vector4(0.54f, 0f, 1f, 1f))        // Blue to Purple
        };

        _effect.Configuration.Set("ng_primaryColor", _effect.PrimaryColor);
        _effect.Configuration.Set("ng_secondaryColor", _effect.SecondaryColor);
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("ng_rainbowSpeed", _effect.RainbowSpeed);
    }
}
