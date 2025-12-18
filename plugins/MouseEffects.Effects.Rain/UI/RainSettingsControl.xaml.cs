using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Rain.UI;

public partial class RainSettingsControl : UserControl
{
    private readonly RainEffect? _effect;
    private bool _isLoading = true;

    public RainSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect as RainEffect;

        if (_effect != null)
        {
            LoadConfiguration();
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        _isLoading = true;
        try
        {
            // Rain settings
            RainIntensitySlider.Value = _effect.RainIntensity;
            FallSpeedSlider.Value = _effect.FallSpeed;
            WindAngleSlider.Value = _effect.WindAngle;
            LifetimeSlider.Value = _effect.RaindropLifetime;

            // Appearance settings
            MinLengthSlider.Value = _effect.MinLength;
            MaxLengthSlider.Value = _effect.MaxLength;
            MinSizeSlider.Value = _effect.MinSize;
            MaxSizeSlider.Value = _effect.MaxSize;

            // Splash settings
            SplashEnabledCheckBox.IsChecked = _effect.SplashEnabled;
            SplashSizeSlider.Value = _effect.SplashSize;

            // Area settings
            FullScreenCheckBox.IsChecked = _effect.FullScreenMode;
            SpawnRadiusSlider.Value = _effect.SpawnRadius;

            // Enable/disable state
            EnabledCheckBox.IsChecked = _effect.IsEnabled;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentPanel.Visibility == Visibility.Visible)
        {
            ContentPanel.Visibility = Visibility.Collapsed;
            FoldButton.Content = "▼";
        }
        else
        {
            ContentPanel.Visibility = Visibility.Visible;
            FoldButton.Content = "▲";
        }
    }

    // Rain Settings
    private void RainIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        int value = (int)RainIntensitySlider.Value;
        _effect.RainIntensity = value;
        _effect.Configuration.Set("rain_intensity", value);
        RainIntensityValue.Text = value.ToString();
    }

    private void FallSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)FallSpeedSlider.Value;
        _effect.FallSpeed = value;
        _effect.Configuration.Set("rain_fallSpeed", value);
        FallSpeedValue.Text = value.ToString("F0");
    }

    private void WindAngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindAngleSlider.Value;
        _effect.WindAngle = value;
        _effect.Configuration.Set("rain_windAngle", value);
        WindAngleValue.Text = value.ToString("F0");
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)LifetimeSlider.Value;
        _effect.RaindropLifetime = value;
        _effect.Configuration.Set("rain_lifetime", value);
        LifetimeValue.Text = value.ToString("F1");
    }

    // Appearance Settings
    private void MinLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MinLengthSlider.Value;
        _effect.MinLength = value;
        _effect.Configuration.Set("rain_minLength", value);
        MinLengthValue.Text = value.ToString("F0");
    }

    private void MaxLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MaxLengthSlider.Value;
        _effect.MaxLength = value;
        _effect.Configuration.Set("rain_maxLength", value);
        MaxLengthValue.Text = value.ToString("F0");
    }

    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MinSizeSlider.Value;
        _effect.MinSize = value;
        _effect.Configuration.Set("rain_minSize", value);
        MinSizeValue.Text = value.ToString("F1");
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MaxSizeSlider.Value;
        _effect.MaxSize = value;
        _effect.Configuration.Set("rain_maxSize", value);
        MaxSizeValue.Text = value.ToString("F1");
    }

    // Splash Settings
    private void SplashEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        bool value = SplashEnabledCheckBox.IsChecked ?? true;
        _effect.SplashEnabled = value;
        _effect.Configuration.Set("rain_splashEnabled", value);
    }

    private void SplashSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SplashSizeSlider.Value;
        _effect.SplashSize = value;
        _effect.Configuration.Set("rain_splashSize", value);
        SplashSizeValue.Text = value.ToString("F0");
    }

    // Area Settings
    private void FullScreenCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        bool value = FullScreenCheckBox.IsChecked ?? false;
        _effect.FullScreenMode = value;
        _effect.Configuration.Set("rain_fullScreen", value);
    }

    private void SpawnRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SpawnRadiusSlider.Value;
        _effect.SpawnRadius = value;
        _effect.Configuration.Set("rain_spawnRadius", value);
        SpawnRadiusValue.Text = value.ToString("F0");
    }
}
