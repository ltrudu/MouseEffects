using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Glitch.UI;

public partial class GlitchSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public GlitchSettingsControl(IEffect effect)
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

        if (_effect.Configuration.TryGet("intensity", out float intensity))
        {
            IntensitySlider.Value = intensity;
            IntensityValue.Text = intensity.ToString("F1");
        }

        if (_effect.Configuration.TryGet("glitchFrequency", out float frequency))
        {
            GlitchFrequencySlider.Value = frequency;
            GlitchFrequencyValue.Text = frequency.ToString("F1");
        }

        if (_effect.Configuration.TryGet("rgbSplitAmount", out float rgbSplit))
        {
            RgbSplitSlider.Value = rgbSplit;
            RgbSplitValue.Text = rgbSplit.ToString("F3");
        }

        if (_effect.Configuration.TryGet("scanLineFrequency", out float scanLine))
        {
            ScanLineSlider.Value = scanLine;
            ScanLineValue.Text = scanLine.ToString("F0");
        }

        if (_effect.Configuration.TryGet("blockSize", out float blockSize))
        {
            BlockSizeSlider.Value = blockSize;
            BlockSizeValue.Text = blockSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("noiseAmount", out float noise))
        {
            NoiseSlider.Value = noise;
            NoiseValue.Text = noise.ToString("F2");
        }

        if (_effect.Configuration.TryGet("movingBackgroundEnabled", out bool movingBg))
            MovingBackgroundCheckBox.IsChecked = movingBg;

        if (_effect.Configuration.TryGet("checkeredViewEnabled", out bool checkered))
            CheckeredViewCheckBox.IsChecked = checkered;

        if (_effect.Configuration.TryGet("distortionEnabled", out bool distortion))
            DistortionCheckBox.IsChecked = distortion;

        if (_effect.Configuration.TryGet("rgbSplitEnabled", out bool rgbSplitEnabled))
            RgbSplitCheckBox.IsChecked = rgbSplitEnabled;
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("intensity", (float)IntensitySlider.Value);
        config.Set("glitchFrequency", (float)GlitchFrequencySlider.Value);
        config.Set("rgbSplitAmount", (float)RgbSplitSlider.Value);
        config.Set("scanLineFrequency", (float)ScanLineSlider.Value);
        config.Set("blockSize", (float)BlockSizeSlider.Value);
        config.Set("noiseAmount", (float)NoiseSlider.Value);
        config.Set("movingBackgroundEnabled", MovingBackgroundCheckBox.IsChecked == true);
        config.Set("checkeredViewEnabled", CheckeredViewCheckBox.IsChecked == true);
        config.Set("distortionEnabled", DistortionCheckBox.IsChecked == true);
        config.Set("rgbSplitEnabled", RgbSplitCheckBox.IsChecked == true);

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

    private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntensityValue != null)
            IntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GlitchFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlitchFrequencyValue != null)
            GlitchFrequencyValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RgbSplitSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RgbSplitValue != null)
            RgbSplitValue.Text = e.NewValue.ToString("F3");
        UpdateConfiguration();
    }

    private void ScanLineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScanLineValue != null)
            ScanLineValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void BlockSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BlockSizeValue != null)
            BlockSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void NoiseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (NoiseValue != null)
            NoiseValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void MovingBackgroundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void CheckeredViewCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void DistortionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RgbSplitCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }
}
