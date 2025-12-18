using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.CherryBlossoms.UI;

public partial class CherryBlossomsSettingsControl : UserControl
{
    private readonly CherryBlossomsEffect? _effect;
    private bool _isLoading = true;

    public CherryBlossomsSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect as CherryBlossomsEffect;

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
            PetalCountSlider.Value = _effect.PetalCount;
            SpawnRadiusSlider.Value = _effect.SpawnRadius;
            LifetimeSlider.Value = _effect.PetalLifetime;

            // Motion settings
            FallSpeedSlider.Value = _effect.FallSpeed;
            SwayAmountSlider.Value = _effect.SwayAmount;
            SwayFrequencySlider.Value = _effect.SwayFrequency;
            SpinSpeedSlider.Value = _effect.SpinSpeed;

            // Appearance settings
            MinSizeSlider.Value = _effect.MinSize;
            MaxSizeSlider.Value = _effect.MaxSize;
            GlowIntensitySlider.Value = _effect.GlowIntensity;

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

    // Spawn Settings
    private void PetalCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        int value = (int)PetalCountSlider.Value;
        _effect.PetalCount = value;
        _effect.Configuration.Set("cb_petalCount", value);
        PetalCountValue.Text = value.ToString();
    }

    private void SpawnRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SpawnRadiusSlider.Value;
        _effect.SpawnRadius = value;
        _effect.Configuration.Set("cb_spawnRadius", value);
        SpawnRadiusValue.Text = value.ToString("F0");
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)LifetimeSlider.Value;
        _effect.PetalLifetime = value;
        _effect.Configuration.Set("cb_lifetime", value);
        LifetimeValue.Text = value.ToString("F0");
    }

    // Motion Settings
    private void FallSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)FallSpeedSlider.Value;
        _effect.FallSpeed = value;
        _effect.Configuration.Set("cb_fallSpeed", value);
        FallSpeedValue.Text = value.ToString("F0");
    }

    private void SwayAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SwayAmountSlider.Value;
        _effect.SwayAmount = value;
        _effect.Configuration.Set("cb_swayAmount", value);
        SwayAmountValue.Text = value.ToString("F0");
    }

    private void SwayFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SwayFrequencySlider.Value;
        _effect.SwayFrequency = value;
        _effect.Configuration.Set("cb_swayFrequency", value);
        SwayFrequencyValue.Text = value.ToString("F1");
    }

    private void SpinSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SpinSpeedSlider.Value;
        _effect.SpinSpeed = value;
        _effect.Configuration.Set("cb_spinSpeed", value);
        SpinSpeedValue.Text = value.ToString("F1");
    }

    // Appearance Settings
    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MinSizeSlider.Value;
        _effect.MinSize = value;
        _effect.Configuration.Set("cb_minSize", value);
        MinSizeValue.Text = value.ToString("F0");
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MaxSizeSlider.Value;
        _effect.MaxSize = value;
        _effect.Configuration.Set("cb_maxSize", value);
        MaxSizeValue.Text = value.ToString("F0");
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)GlowIntensitySlider.Value;
        _effect.GlowIntensity = value;
        _effect.Configuration.Set("cb_glowIntensity", value);
        GlowIntensityValue.Text = value.ToString("F1");
    }
}
