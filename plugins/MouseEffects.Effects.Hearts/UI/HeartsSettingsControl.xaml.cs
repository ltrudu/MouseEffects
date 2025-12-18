using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Hearts.UI;

public partial class HeartsSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public HeartsSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Heart settings
        if (_effect.Configuration.TryGet("h_heartCount", out int count))
        {
            HeartCountSlider.Value = count;
            HeartCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("h_minSize", out float minSize))
        {
            MinSizeSlider.Value = minSize;
            MinSizeValue.Text = minSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("h_maxSize", out float maxSize))
        {
            MaxSizeSlider.Value = maxSize;
            MaxSizeValue.Text = maxSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("h_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F0");
        }

        // Motion settings
        if (_effect.Configuration.TryGet("h_floatSpeed", out float floatSpeed))
        {
            FloatSpeedSlider.Value = floatSpeed;
            FloatSpeedValue.Text = floatSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("h_wobbleAmount", out float wobble))
        {
            WobbleAmountSlider.Value = wobble;
            WobbleAmountValue.Text = wobble.ToString("F0");
        }

        if (_effect.Configuration.TryGet("h_wobbleFrequency", out float freq))
        {
            WobbleFrequencySlider.Value = freq;
            WobbleFrequencyValue.Text = freq.ToString("F1");
        }

        if (_effect.Configuration.TryGet("h_rotationAmount", out float rotation))
        {
            RotationAmountSlider.Value = rotation;
            RotationAmountValue.Text = rotation.ToString("F1");
        }

        // Visual effects
        if (_effect.Configuration.TryGet("h_glowIntensity", out float glow))
        {
            GlowIntensitySlider.Value = glow;
            GlowIntensityValue.Text = glow.ToString("F1");
        }

        if (_effect.Configuration.TryGet("h_sparkleIntensity", out float sparkle))
        {
            SparkleIntensitySlider.Value = sparkle;
            SparkleIntensityValue.Text = sparkle.ToString("F1");
        }

        // Color mode
        if (_effect.Configuration.TryGet("h_colorMode", out int colorMode))
        {
            ColorModeComboBox.SelectedIndex = colorMode;
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Heart settings
        config.Set("h_heartCount", (int)HeartCountSlider.Value);
        config.Set("h_minSize", (float)MinSizeSlider.Value);
        config.Set("h_maxSize", (float)MaxSizeSlider.Value);
        config.Set("h_lifetime", (float)LifetimeSlider.Value);

        // Motion settings
        config.Set("h_floatSpeed", (float)FloatSpeedSlider.Value);
        config.Set("h_wobbleAmount", (float)WobbleAmountSlider.Value);
        config.Set("h_wobbleFrequency", (float)WobbleFrequencySlider.Value);
        config.Set("h_rotationAmount", (float)RotationAmountSlider.Value);

        // Visual effects
        config.Set("h_glowIntensity", (float)GlowIntensitySlider.Value);
        config.Set("h_sparkleIntensity", (float)SparkleIntensitySlider.Value);

        // Color mode
        config.Set("h_colorMode", ColorModeComboBox.SelectedIndex);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void HeartCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HeartCountValue != null)
            HeartCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinSizeValue != null)
            MinSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxSizeValue != null)
            MaxSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void FloatSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FloatSpeedValue != null)
            FloatSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WobbleAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WobbleAmountValue != null)
            WobbleAmountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WobbleFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WobbleFrequencyValue != null)
            WobbleFrequencyValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RotationAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RotationAmountValue != null)
            RotationAmountValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void SparkleIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SparkleIntensityValue != null)
            SparkleIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ColorModeComboBox_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }
}
