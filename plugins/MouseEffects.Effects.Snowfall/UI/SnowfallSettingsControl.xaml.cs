using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Snowfall.UI;

public partial class SnowfallSettingsControl : UserControl
{
    private readonly SnowfallEffect? _effect;
    private bool _isLoading = true;

    public SnowfallSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect as SnowfallEffect;

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
            // Spawn settings
            SnowflakeCountSlider.Value = _effect.SnowflakeCount;
            SpawnRadiusSlider.Value = _effect.SpawnRadius;
            LifetimeSlider.Value = _effect.SnowflakeLifetime;

            // Motion settings
            FallSpeedSlider.Value = _effect.FallSpeed;
            WindStrengthSlider.Value = _effect.WindStrength;
            WindFrequencySlider.Value = _effect.WindFrequency;
            RotationSpeedSlider.Value = _effect.RotationSpeed;

            // Appearance settings
            MinSizeSlider.Value = _effect.MinSize;
            MaxSizeSlider.Value = _effect.MaxSize;
            GlowIntensitySlider.Value = _effect.GlowIntensity;
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Spawn Settings
    private void SnowflakeCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        int value = (int)SnowflakeCountSlider.Value;
        _effect.SnowflakeCount = value;
        _effect.Configuration.Set("sf_snowflakeCount", value);
        SnowflakeCountValue.Text = value.ToString();
    }

    private void SpawnRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SpawnRadiusSlider.Value;
        _effect.SpawnRadius = value;
        _effect.Configuration.Set("sf_spawnRadius", value);
        SpawnRadiusValue.Text = value.ToString("F0");
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)LifetimeSlider.Value;
        _effect.SnowflakeLifetime = value;
        _effect.Configuration.Set("sf_lifetime", value);
        LifetimeValue.Text = value.ToString("F1");
    }

    // Motion Settings
    private void FallSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)FallSpeedSlider.Value;
        _effect.FallSpeed = value;
        _effect.Configuration.Set("sf_fallSpeed", value);
        FallSpeedValue.Text = value.ToString("F0");
    }

    private void WindStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindStrengthSlider.Value;
        _effect.WindStrength = value;
        _effect.Configuration.Set("sf_windStrength", value);
        WindStrengthValue.Text = value.ToString("F0");
    }

    private void WindFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindFrequencySlider.Value;
        _effect.WindFrequency = value;
        _effect.Configuration.Set("sf_windFrequency", value);
        WindFrequencyValue.Text = value.ToString("F1");
    }

    private void RotationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)RotationSpeedSlider.Value;
        _effect.RotationSpeed = value;
        _effect.Configuration.Set("sf_rotationSpeed", value);
        RotationSpeedValue.Text = value.ToString("F1");
    }

    // Appearance Settings
    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MinSizeSlider.Value;
        _effect.MinSize = value;
        _effect.Configuration.Set("sf_minSize", value);
        MinSizeValue.Text = value.ToString("F0");
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MaxSizeSlider.Value;
        _effect.MaxSize = value;
        _effect.Configuration.Set("sf_maxSize", value);
        MaxSizeValue.Text = value.ToString("F0");
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)GlowIntensitySlider.Value;
        _effect.GlowIntensity = value;
        _effect.Configuration.Set("sf_glowIntensity", value);
        GlowIntensityValue.Text = value.ToString("F1");
    }
}
