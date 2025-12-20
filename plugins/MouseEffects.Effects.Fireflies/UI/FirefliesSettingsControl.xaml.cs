using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Fireflies.UI;

public partial class FirefliesSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;

    public event Action<string>? SettingsChanged;

    public FirefliesSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        // General settings
        if (_effect.Configuration.TryGet("ff_fireflyCount", out int count))
        {
            FireflyCountSlider.Value = count;
            FireflyCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("ff_glowSize", out float size))
        {
            GlowSizeSlider.Value = size;
            GlowSizeValue.Text = size.ToString("F0");
        }

        // Pulse settings
        if (_effect.Configuration.TryGet("ff_pulseSpeed", out float pulseSpeed))
        {
            PulseSpeedSlider.Value = pulseSpeed;
            PulseSpeedValue.Text = pulseSpeed.ToString("F1");
        }

        if (_effect.Configuration.TryGet("ff_pulseRandomness", out float pulseRnd))
        {
            PulseRandomnessSlider.Value = pulseRnd;
            PulseRandomnessValue.Text = pulseRnd.ToString("F1");
        }

        if (_effect.Configuration.TryGet("ff_minBrightness", out float minBright))
        {
            MinBrightnessSlider.Value = minBright;
            MinBrightnessValue.Text = minBright.ToString("F2");
        }

        if (_effect.Configuration.TryGet("ff_maxBrightness", out float maxBright))
        {
            MaxBrightnessSlider.Value = maxBright;
            MaxBrightnessValue.Text = maxBright.ToString("F1");
        }

        // Movement settings
        if (_effect.Configuration.TryGet("ff_attractionStrength", out float attraction))
        {
            AttractionStrengthSlider.Value = attraction;
            AttractionStrengthValue.Text = attraction.ToString("F2");
        }

        if (_effect.Configuration.TryGet("ff_wanderStrength", out float wander))
        {
            WanderStrengthSlider.Value = wander;
            WanderStrengthValue.Text = wander.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ff_maxSpeed", out float speed))
        {
            MaxSpeedSlider.Value = speed;
            MaxSpeedValue.Text = speed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ff_wanderChangeRate", out float wanderRate))
        {
            WanderChangeRateSlider.Value = wanderRate;
            WanderChangeRateValue.Text = wanderRate.ToString("F1");
        }

        // Explosion settings
        if (_effect.Configuration.TryGet("ff_explosionEnabled", out bool explosionEnabled))
        {
            ExplosionEnabledCheckBox.IsChecked = explosionEnabled;
            UpdateExplosionVisibility();
        }

        if (_effect.Configuration.TryGet("ff_explosionStrength", out float explosionStrength))
        {
            ExplosionStrengthSlider.Value = explosionStrength;
            ExplosionStrengthValue.Text = explosionStrength.ToString("F0");
        }
    }

    private void UpdateExplosionVisibility()
    {
        if (ExplosionSettingsPanel == null) return;
        ExplosionSettingsPanel.Visibility = ExplosionEnabledCheckBox.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();

        // General settings
        config.Set("ff_fireflyCount", (int)FireflyCountSlider.Value);
        config.Set("ff_glowSize", (float)GlowSizeSlider.Value);

        // Pulse settings
        config.Set("ff_pulseSpeed", (float)PulseSpeedSlider.Value);
        config.Set("ff_pulseRandomness", (float)PulseRandomnessSlider.Value);
        config.Set("ff_minBrightness", (float)MinBrightnessSlider.Value);
        config.Set("ff_maxBrightness", (float)MaxBrightnessSlider.Value);

        // Movement settings
        config.Set("ff_attractionStrength", (float)AttractionStrengthSlider.Value);
        config.Set("ff_wanderStrength", (float)WanderStrengthSlider.Value);
        config.Set("ff_maxSpeed", (float)MaxSpeedSlider.Value);
        config.Set("ff_wanderChangeRate", (float)WanderChangeRateSlider.Value);

        // Explosion settings
        config.Set("ff_explosionEnabled", ExplosionEnabledCheckBox.IsChecked == true);
        config.Set("ff_explosionStrength", (float)ExplosionStrengthSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void FireflyCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FireflyCountValue != null)
            FireflyCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void GlowSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowSizeValue != null)
            GlowSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void PulseSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PulseSpeedValue != null)
            PulseSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void PulseRandomnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PulseRandomnessValue != null)
            PulseRandomnessValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MinBrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinBrightnessValue != null)
            MinBrightnessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void MaxBrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxBrightnessValue != null)
            MaxBrightnessValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void AttractionStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AttractionStrengthValue != null)
            AttractionStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void WanderStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WanderStrengthValue != null)
            WanderStrengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxSpeedValue != null)
            MaxSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WanderChangeRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WanderChangeRateValue != null)
            WanderChangeRateValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ExplosionEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateExplosionVisibility();
        UpdateConfiguration();
    }

    private void ExplosionStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ExplosionStrengthValue != null)
            ExplosionStrengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }
}
