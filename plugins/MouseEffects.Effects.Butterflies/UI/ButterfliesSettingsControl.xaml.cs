using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Butterflies.UI;

public partial class ButterfliesSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;

    public event Action<string>? SettingsChanged;

    public ButterfliesSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        // Butterfly settings
        if (_effect.Configuration.TryGet("bf_butterflyCount", out int count))
        {
            ButterflyCountSlider.Value = count;
            ButterflyCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("bf_minSize", out float minSize))
        {
            MinSizeSlider.Value = minSize;
            MinSizeValue.Text = minSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("bf_maxSize", out float maxSize))
        {
            MaxSizeSlider.Value = maxSize;
            MaxSizeValue.Text = maxSize.ToString("F0");
        }

        // Animation settings
        if (_effect.Configuration.TryGet("bf_wingFlapSpeed", out float flapSpeed))
        {
            WingFlapSpeedSlider.Value = flapSpeed;
            WingFlapSpeedValue.Text = flapSpeed.ToString("F1");
        }

        // Flight behavior
        if (_effect.Configuration.TryGet("bf_followDistance", out float followDist))
        {
            FollowDistanceSlider.Value = followDist;
            FollowDistanceValue.Text = followDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet("bf_followStrength", out float followStr))
        {
            FollowStrengthSlider.Value = followStr;
            FollowStrengthValue.Text = followStr.ToString("F2");
        }

        if (_effect.Configuration.TryGet("bf_wanderStrength", out float wanderStr))
        {
            WanderStrengthSlider.Value = wanderStr;
            WanderStrengthValue.Text = wanderStr.ToString("F0");
        }

        // Visual settings
        if (_effect.Configuration.TryGet("bf_glowIntensity", out float glow))
        {
            GlowIntensitySlider.Value = glow;
            GlowIntensityValue.Text = glow.ToString("F1");
        }

        if (_effect.Configuration.TryGet("bf_colorMode", out int colorMode))
        {
            ColorModeComboBox.SelectedIndex = colorMode;
        }

        if (_effect.Configuration.TryGet("bf_rainbowSpeed", out float rainbowSpeed))
        {
            RainbowSpeedSlider.Value = rainbowSpeed;
            RainbowSpeedValue.Text = rainbowSpeed.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();

        // Butterfly settings
        config.Set("bf_butterflyCount", (int)ButterflyCountSlider.Value);
        config.Set("bf_minSize", (float)MinSizeSlider.Value);
        config.Set("bf_maxSize", (float)MaxSizeSlider.Value);

        // Animation settings
        config.Set("bf_wingFlapSpeed", (float)WingFlapSpeedSlider.Value);

        // Flight behavior
        config.Set("bf_followDistance", (float)FollowDistanceSlider.Value);
        config.Set("bf_followStrength", (float)FollowStrengthSlider.Value);
        config.Set("bf_wanderStrength", (float)WanderStrengthSlider.Value);

        // Visual settings
        config.Set("bf_glowIntensity", (float)GlowIntensitySlider.Value);
        config.Set("bf_colorMode", ColorModeComboBox.SelectedIndex);
        config.Set("bf_rainbowSpeed", (float)RainbowSpeedSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void ButterflyCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ButterflyCountValue != null)
            ButterflyCountValue.Text = e.NewValue.ToString("F0");
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

    private void WingFlapSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WingFlapSpeedValue != null)
            WingFlapSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FollowDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FollowDistanceValue != null)
            FollowDistanceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void FollowStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FollowStrengthValue != null)
            FollowStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void WanderStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WanderStrengthValue != null)
            WanderStrengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ColorModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RainbowSpeedValue != null)
            RainbowSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }
}
