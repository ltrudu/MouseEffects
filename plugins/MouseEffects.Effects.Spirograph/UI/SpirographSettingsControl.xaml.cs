using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Spirograph.UI;

public partial class SpirographSettingsControl : UserControl
{
    private readonly SpirographEffect _effect;
    private bool _isLoading = true;

    public SpirographSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is not SpirographEffect spirographEffect)
            throw new ArgumentException("Effect must be SpirographEffect", nameof(effect));

        _effect = spirographEffect;

        Loaded += (s, e) => LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _isLoading = true;
        try
        {
            // Shape parameters
            InnerRadiusSlider.Value = _effect.InnerRadius;
            OuterRadiusSlider.Value = _effect.OuterRadius;
            PenOffsetSlider.Value = _effect.PenOffset;
            NumPetalsSlider.Value = _effect.NumPetals;

            // Animation
            RotationSpeedSlider.Value = _effect.RotationSpeed;
            TrailFadeSpeedSlider.Value = _effect.TrailFadeSpeed;

            // Appearance
            LineThicknessSlider.Value = _effect.LineThickness;
            GlowIntensitySlider.Value = _effect.GlowIntensity;

            // Colors
            ColorModeCombo.SelectedIndex = _effect.ColorMode;
            ColorCycleSpeedSlider.Value = _effect.ColorCycleSpeed;

            UpdateColorModeUI();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateColorModeUI()
    {
        // Show/hide controls based on color mode
        int colorMode = ColorModeCombo.SelectedIndex;

        // Rainbow mode: show cycle speed
        ColorCycleSpeedLabel.Visibility = colorMode == 0 ? Visibility.Visible : Visibility.Collapsed;
        ColorCycleSpeedPanel.Visibility = colorMode == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Fixed/Gradient mode: show color presets
        ColorPresetLabel.Visibility = colorMode > 0 ? Visibility.Visible : Visibility.Collapsed;
        ColorPresetCombo.Visibility = colorMode > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void InnerRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.InnerRadius = (float)InnerRadiusSlider.Value;
        _effect.Configuration.Set("sp_innerRadius", _effect.InnerRadius);
    }

    private void OuterRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.OuterRadius = (float)OuterRadiusSlider.Value;
        _effect.Configuration.Set("sp_outerRadius", _effect.OuterRadius);
    }

    private void PenOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.PenOffset = (float)PenOffsetSlider.Value;
        _effect.Configuration.Set("sp_penOffset", _effect.PenOffset);
    }

    private void NumPetalsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.NumPetals = (int)NumPetalsSlider.Value;
        _effect.Configuration.Set("sp_numPetals", _effect.NumPetals);
    }

    private void RotationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RotationSpeed = (float)RotationSpeedSlider.Value;
        _effect.Configuration.Set("sp_rotationSpeed", _effect.RotationSpeed);
    }

    private void TrailFadeSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.TrailFadeSpeed = (float)TrailFadeSpeedSlider.Value;
        _effect.Configuration.Set("sp_trailFadeSpeed", _effect.TrailFadeSpeed);
    }

    private void LineThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.LineThickness = (float)LineThicknessSlider.Value;
        _effect.Configuration.Set("sp_lineThickness", _effect.LineThickness);
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("sp_glowIntensity", _effect.GlowIntensity);
    }

    private void ColorModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        _effect.ColorMode = ColorModeCombo.SelectedIndex;
        _effect.Configuration.Set("sp_colorMode", _effect.ColorMode);

        UpdateColorModeUI();
    }

    private void ColorCycleSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ColorCycleSpeed = (float)ColorCycleSpeedSlider.Value;
        _effect.Configuration.Set("sp_colorCycleSpeed", _effect.ColorCycleSpeed);
    }

    private void ColorPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        // Color presets
        (_effect.PrimaryColor, _effect.SecondaryColor, _effect.TertiaryColor) = ColorPresetCombo.SelectedIndex switch
        {
            0 => ( // Pink/Cyan/Green
                new Vector4(1f, 0f, 0.5f, 1f),      // Pink
                new Vector4(0f, 0.5f, 1f, 1f),      // Cyan
                new Vector4(0.5f, 1f, 0f, 1f)       // Green
            ),
            1 => ( // Purple/Orange/Blue
                new Vector4(0.545f, 0f, 1f, 1f),    // Purple
                new Vector4(1f, 0.55f, 0f, 1f),     // Orange
                new Vector4(0f, 0.5f, 1f, 1f)       // Blue
            ),
            2 => ( // Red/Yellow/Blue
                new Vector4(1f, 0f, 0f, 1f),        // Red
                new Vector4(1f, 1f, 0f, 1f),        // Yellow
                new Vector4(0f, 0.4f, 1f, 1f)       // Blue
            ),
            3 => ( // Blue/Green/Purple
                new Vector4(0.13f, 0.59f, 1f, 1f),  // Electric Blue
                new Vector4(0f, 1f, 0.5f, 1f),      // Green
                new Vector4(0.8f, 0f, 1f, 1f)       // Purple
            ),
            _ => ( // Orange/Pink/Yellow
                new Vector4(1f, 0.55f, 0f, 1f),     // Orange
                new Vector4(1f, 0.41f, 0.71f, 1f),  // Pink
                new Vector4(1f, 0.92f, 0.02f, 1f)   // Yellow
            )
        };

        _effect.Configuration.Set("sp_primaryColor", _effect.PrimaryColor);
        _effect.Configuration.Set("sp_secondaryColor", _effect.SecondaryColor);
        _effect.Configuration.Set("sp_tertiaryColor", _effect.TertiaryColor);
    }
}
