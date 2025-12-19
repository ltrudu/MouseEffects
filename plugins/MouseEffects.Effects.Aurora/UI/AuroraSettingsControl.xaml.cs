using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Aurora.UI;

public partial class AuroraSettingsControl : UserControl
{
    private readonly AuroraEffect _effect;
    private bool _isLoading = true;

    public AuroraSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is not AuroraEffect auroraEffect)
            throw new ArgumentException("Effect must be AuroraEffect", nameof(effect));

        _effect = auroraEffect;

        Loaded += (s, e) => LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _isLoading = true;
        try
        {
            // Aurora appearance
            HeightSlider.Value = _effect.Height;
            HorizontalSpreadSlider.Value = _effect.HorizontalSpread;
            EdgeFalloffSlider.Value = _effect.EdgeFalloff;
            NumLayersSlider.Value = _effect.NumLayers;
            ColorIntensitySlider.Value = _effect.ColorIntensity;
            GlowStrengthSlider.Value = _effect.GlowStrength;
            AlphaSlider.Value = _effect.Alpha;

            // Rainbow
            RainbowSpeedSlider.Value = _effect.RainbowSpeed;
            if (_effect.RainbowMode)
            {
                ColorPresetCombo.SelectedIndex = 5; // Rainbow preset
                RainbowSpeedLabel.Visibility = Visibility.Visible;
                RainbowSpeedGrid.Visibility = Visibility.Visible;
            }

            // Animation
            WaveSpeedSlider.Value = _effect.WaveSpeed;
            WaveFrequencySlider.Value = _effect.WaveFrequency;
            NoiseScaleSlider.Value = _effect.NoiseScale;
            NoiseStrengthSlider.Value = _effect.NoiseStrength;
            VerticalFlowSlider.Value = _effect.VerticalFlow;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.Height = (float)HeightSlider.Value;
        _effect.Configuration.Set("au_height", _effect.Height);
    }

    private void HorizontalSpreadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.HorizontalSpread = (float)HorizontalSpreadSlider.Value;
        _effect.Configuration.Set("au_horizontalSpread", _effect.HorizontalSpread);
    }

    private void EdgeFalloffSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.EdgeFalloff = (float)EdgeFalloffSlider.Value;
        _effect.Configuration.Set("au_edgeFalloff", _effect.EdgeFalloff);
    }

    private void NumLayersSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.NumLayers = (int)NumLayersSlider.Value;
        _effect.Configuration.Set("au_numLayers", _effect.NumLayers);
    }

    private void ColorIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ColorIntensity = (float)ColorIntensitySlider.Value;
        _effect.Configuration.Set("au_colorIntensity", _effect.ColorIntensity);
    }

    private void GlowStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowStrength = (float)GlowStrengthSlider.Value;
        _effect.Configuration.Set("au_glowStrength", _effect.GlowStrength);
    }

    private void AlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.Alpha = (float)AlphaSlider.Value;
        _effect.Configuration.Set("au_alpha", _effect.Alpha);
    }

    private void WaveSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.WaveSpeed = (float)WaveSpeedSlider.Value;
        _effect.Configuration.Set("au_waveSpeed", _effect.WaveSpeed);
    }

    private void WaveFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.WaveFrequency = (float)WaveFrequencySlider.Value;
        _effect.Configuration.Set("au_waveFrequency", _effect.WaveFrequency);
    }

    private void NoiseScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.NoiseScale = (float)NoiseScaleSlider.Value;
        _effect.Configuration.Set("au_noiseScale", _effect.NoiseScale);
    }

    private void NoiseStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.NoiseStrength = (float)NoiseStrengthSlider.Value;
        _effect.Configuration.Set("au_noiseStrength", _effect.NoiseStrength);
    }

    private void VerticalFlowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.VerticalFlow = (float)VerticalFlowSlider.Value;
        _effect.Configuration.Set("au_verticalFlow", _effect.VerticalFlow);
    }

    private void ColorPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        // Aurora color presets
        (_effect.PrimaryColor, _effect.SecondaryColor, _effect.TertiaryColor, _effect.AccentColor) = ColorPresetCombo.SelectedIndex switch
        {
            0 => ( // Classic Aurora (Green/Cyan/Purple/Pink)
                new Vector4(0f, 1f, 0.5f, 1f),      // Green
                new Vector4(0f, 1f, 1f, 1f),        // Cyan
                new Vector4(0.545f, 0f, 1f, 1f),    // Purple
                new Vector4(1f, 0.078f, 0.576f, 1f) // Pink
            ),
            1 => ( // Pink Aurora (Pink/Purple/Cyan/White)
                new Vector4(1f, 0.078f, 0.576f, 1f), // Pink
                new Vector4(0.545f, 0f, 1f, 1f),     // Purple
                new Vector4(0f, 1f, 1f, 1f),         // Cyan
                new Vector4(1f, 1f, 1f, 1f)          // White
            ),
            2 => ( // Blue Aurora (Blue/Cyan/White/Light Blue)
                new Vector4(0.13f, 0.59f, 1f, 1f),   // Electric Blue
                new Vector4(0f, 1f, 1f, 1f),         // Cyan
                new Vector4(1f, 1f, 1f, 1f),         // White
                new Vector4(0.53f, 0.81f, 0.98f, 1f) // Light Blue
            ),
            3 => ( // Red Aurora (Red/Orange/Yellow/Pink)
                new Vector4(1f, 0f, 0f, 1f),         // Red
                new Vector4(1f, 0.55f, 0f, 1f),      // Orange
                new Vector4(1f, 1f, 0f, 1f),         // Yellow
                new Vector4(1f, 0.078f, 0.576f, 1f)  // Pink
            ),
            5 => ( // Rainbow - colors don't matter, shader uses HSV
                new Vector4(1f, 0f, 0f, 1f),
                new Vector4(1f, 1f, 0f, 1f),
                new Vector4(0f, 1f, 0f, 1f),
                new Vector4(0f, 0f, 1f, 1f)
            ),
            _ => ( // Purple Aurora (Purple/Pink/Blue/Magenta)
                new Vector4(0.545f, 0f, 1f, 1f),     // Purple
                new Vector4(1f, 0.078f, 0.576f, 1f), // Pink
                new Vector4(0.13f, 0.59f, 1f, 1f),   // Electric Blue
                new Vector4(1f, 0f, 1f, 1f)          // Magenta
            )
        };

        // Handle rainbow mode
        bool isRainbow = ColorPresetCombo.SelectedIndex == 5;
        _effect.RainbowMode = isRainbow;
        _effect.Configuration.Set("au_rainbowMode", isRainbow);

        // Show/hide rainbow speed slider
        RainbowSpeedLabel.Visibility = isRainbow ? Visibility.Visible : Visibility.Collapsed;
        RainbowSpeedGrid.Visibility = isRainbow ? Visibility.Visible : Visibility.Collapsed;

        _effect.Configuration.Set("au_primaryColor", _effect.PrimaryColor);
        _effect.Configuration.Set("au_secondaryColor", _effect.SecondaryColor);
        _effect.Configuration.Set("au_tertiaryColor", _effect.TertiaryColor);
        _effect.Configuration.Set("au_accentColor", _effect.AccentColor);
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("au_rainbowSpeed", _effect.RainbowSpeed);
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
