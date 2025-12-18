using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.DandelionSeeds.UI;

public partial class DandelionSeedsSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public DandelionSeedsSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Seed settings
        if (_effect.Configuration.TryGet("ds_seedCount", out int count))
        {
            SeedCountSlider.Value = count;
            SeedCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("ds_minSize", out float minSize))
        {
            MinSizeSlider.Value = minSize;
            MinSizeValue.Text = minSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ds_maxSize", out float maxSize))
        {
            MaxSizeSlider.Value = maxSize;
            MaxSizeValue.Text = maxSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ds_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ds_spawnRadius", out float radius))
        {
            SpawnRadiusSlider.Value = radius;
            SpawnRadiusValue.Text = radius.ToString("F0");
        }

        // Motion settings
        if (_effect.Configuration.TryGet("ds_floatSpeed", out float floatSpeed))
        {
            FloatSpeedSlider.Value = floatSpeed;
            FloatSpeedValue.Text = floatSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ds_upwardDrift", out float upward))
        {
            UpwardDriftSlider.Value = upward;
            UpwardDriftValue.Text = upward.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ds_windStrength", out float wind))
        {
            WindStrengthSlider.Value = wind;
            WindStrengthValue.Text = wind.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ds_windFrequency", out float freq))
        {
            WindFrequencySlider.Value = freq;
            WindFrequencyValue.Text = freq.ToString("F1");
        }

        if (_effect.Configuration.TryGet("ds_tumbleSpeed", out float tumble))
        {
            TumbleSpeedSlider.Value = tumble;
            TumbleSpeedValue.Text = tumble.ToString("F1");
        }

        // Appearance settings
        if (_effect.Configuration.TryGet("ds_glowIntensity", out float glow))
        {
            GlowIntensitySlider.Value = glow;
            GlowIntensityValue.Text = glow.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Seed settings
        config.Set("ds_seedCount", (int)SeedCountSlider.Value);
        config.Set("ds_minSize", (float)MinSizeSlider.Value);
        config.Set("ds_maxSize", (float)MaxSizeSlider.Value);
        config.Set("ds_lifetime", (float)LifetimeSlider.Value);
        config.Set("ds_spawnRadius", (float)SpawnRadiusSlider.Value);

        // Motion settings
        config.Set("ds_floatSpeed", (float)FloatSpeedSlider.Value);
        config.Set("ds_upwardDrift", (float)UpwardDriftSlider.Value);
        config.Set("ds_windStrength", (float)WindStrengthSlider.Value);
        config.Set("ds_windFrequency", (float)WindFrequencySlider.Value);
        config.Set("ds_tumbleSpeed", (float)TumbleSpeedSlider.Value);

        // Appearance settings
        config.Set("ds_glowIntensity", (float)GlowIntensitySlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void SeedCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SeedCountValue != null)
            SeedCountValue.Text = e.NewValue.ToString("F0");
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

    private void SpawnRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpawnRadiusValue != null)
            SpawnRadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void FloatSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FloatSpeedValue != null)
            FloatSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void UpwardDriftSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (UpwardDriftValue != null)
            UpwardDriftValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WindStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WindStrengthValue != null)
            WindStrengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WindFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WindFrequencyValue != null)
            WindFrequencyValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void TumbleSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TumbleSpeedValue != null)
            TumbleSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }
}
