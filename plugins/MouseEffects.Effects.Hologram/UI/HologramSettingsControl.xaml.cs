using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Hologram.UI;

public partial class HologramSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public HologramSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        if (_effect.Configuration.TryGet("radius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("centerRadius", out float centerRadius))
        {
            CenterRadiusSlider.Value = centerRadius;
            CenterRadiusValue.Text = centerRadius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("dotsEnabled", out bool dotsEnabled))
        {
            DotsEnabledCheckBox.IsChecked = dotsEnabled;
        }

        if (_effect.Configuration.TryGet("scanLineDensity", out float density))
        {
            ScanLineDensitySlider.Value = density;
            ScanLineDensityValue.Text = density.ToString("F0");
        }

        if (_effect.Configuration.TryGet("scanLineSpeed", out float speed))
        {
            ScanLineSpeedSlider.Value = speed;
            ScanLineSpeedValue.Text = speed.ToString("F1");
        }

        if (_effect.Configuration.TryGet("flickerIntensity", out float flicker))
        {
            FlickerIntensitySlider.Value = flicker;
            FlickerIntensityValue.Text = flicker.ToString("F2");
        }

        if (_effect.Configuration.TryGet("colorTint", out int tint))
        {
            ColorTintCombo.SelectedIndex = tint;
        }

        if (_effect.Configuration.TryGet("edgeGlowStrength", out float glow))
        {
            EdgeGlowSlider.Value = glow;
            EdgeGlowValue.Text = glow.ToString("F1");
        }

        if (_effect.Configuration.TryGet("noiseAmount", out float noise))
        {
            NoiseSlider.Value = noise;
            NoiseValue.Text = noise.ToString("F2");
        }

        if (_effect.Configuration.TryGet("chromaticAberration", out float aberration))
        {
            ChromaticAberrationSlider.Value = aberration;
            ChromaticAberrationValue.Text = aberration.ToString("F3");
        }

        if (_effect.Configuration.TryGet("tintStrength", out float tintStr))
        {
            TintStrengthSlider.Value = tintStr;
            TintStrengthValue.Text = tintStr.ToString("F2");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("centerRadius", (float)CenterRadiusSlider.Value);
        config.Set("dotsEnabled", DotsEnabledCheckBox.IsChecked == true);
        config.Set("scanLineDensity", (float)ScanLineDensitySlider.Value);
        config.Set("scanLineSpeed", (float)ScanLineSpeedSlider.Value);
        config.Set("flickerIntensity", (float)FlickerIntensitySlider.Value);
        config.Set("colorTint", ColorTintCombo.SelectedIndex);
        config.Set("edgeGlowStrength", (float)EdgeGlowSlider.Value);
        config.Set("noiseAmount", (float)NoiseSlider.Value);
        config.Set("chromaticAberration", (float)ChromaticAberrationSlider.Value);
        config.Set("tintStrength", (float)TintStrengthSlider.Value);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void CenterRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CenterRadiusValue != null)
            CenterRadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void DotsEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ScanLineDensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScanLineDensityValue != null)
            ScanLineDensityValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ScanLineSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScanLineSpeedValue != null)
            ScanLineSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FlickerIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FlickerIntensityValue != null)
            FlickerIntensityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ColorTintCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void EdgeGlowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeGlowValue != null)
            EdgeGlowValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void NoiseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (NoiseValue != null)
            NoiseValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ChromaticAberrationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ChromaticAberrationValue != null)
            ChromaticAberrationValue.Text = e.NewValue.ToString("F3");
        UpdateConfiguration();
    }

    private void TintStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TintStrengthValue != null)
            TintStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }
}
