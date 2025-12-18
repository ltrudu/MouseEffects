using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.EmojiRain.UI;

public partial class EmojiRainSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public EmojiRainSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Emoji settings
        if (_effect.Configuration.TryGet("er_emojiCount", out int count))
        {
            EmojiCountSlider.Value = count;
            EmojiCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("er_fallSpeed", out float fallSpeed))
        {
            FallSpeedSlider.Value = fallSpeed;
            FallSpeedValue.Text = fallSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("er_minSize", out float minSize))
        {
            MinSizeSlider.Value = minSize;
            MinSizeValue.Text = minSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("er_maxSize", out float maxSize))
        {
            MaxSizeSlider.Value = maxSize;
            MaxSizeValue.Text = maxSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("er_rotationAmount", out float rotation))
        {
            RotationAmountSlider.Value = rotation;
            RotationAmountValue.Text = rotation.ToString("F1");
        }

        if (_effect.Configuration.TryGet("er_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F0");
        }

        // Emoji type toggles
        if (_effect.Configuration.TryGet("er_enableHappy", out bool happy))
            EnableHappyCheckBox.IsChecked = happy;

        if (_effect.Configuration.TryGet("er_enableSad", out bool sad))
            EnableSadCheckBox.IsChecked = sad;

        if (_effect.Configuration.TryGet("er_enableWink", out bool wink))
            EnableWinkCheckBox.IsChecked = wink;

        if (_effect.Configuration.TryGet("er_enableHeartEyes", out bool heartEyes))
            EnableHeartEyesCheckBox.IsChecked = heartEyes;

        if (_effect.Configuration.TryGet("er_enableStarEyes", out bool starEyes))
            EnableStarEyesCheckBox.IsChecked = starEyes;

        if (_effect.Configuration.TryGet("er_enableSurprised", out bool surprised))
            EnableSurprisedCheckBox.IsChecked = surprised;
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Emoji settings
        config.Set("er_emojiCount", (int)EmojiCountSlider.Value);
        config.Set("er_fallSpeed", (float)FallSpeedSlider.Value);
        config.Set("er_minSize", (float)MinSizeSlider.Value);
        config.Set("er_maxSize", (float)MaxSizeSlider.Value);
        config.Set("er_rotationAmount", (float)RotationAmountSlider.Value);
        config.Set("er_lifetime", (float)LifetimeSlider.Value);

        // Emoji type toggles
        config.Set("er_enableHappy", EnableHappyCheckBox.IsChecked ?? true);
        config.Set("er_enableSad", EnableSadCheckBox.IsChecked ?? true);
        config.Set("er_enableWink", EnableWinkCheckBox.IsChecked ?? true);
        config.Set("er_enableHeartEyes", EnableHeartEyesCheckBox.IsChecked ?? true);
        config.Set("er_enableStarEyes", EnableStarEyesCheckBox.IsChecked ?? true);
        config.Set("er_enableSurprised", EnableSurprisedCheckBox.IsChecked ?? true);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EmojiCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EmojiCountValue != null)
            EmojiCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void FallSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FallSpeedValue != null)
            FallSpeedValue.Text = e.NewValue.ToString("F0");
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

    private void RotationAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RotationAmountValue != null)
            RotationAmountValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void EmojiTypeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }
}
