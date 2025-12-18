using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.FallingLeaves.UI;

public partial class FallingLeavesSettingsControl : UserControl
{
    private readonly FallingLeavesEffect? _effect;
    private bool _isLoading = true;

    public FallingLeavesSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect as FallingLeavesEffect;

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
            LeafCountSlider.Value = _effect.LeafCount;
            SpawnRadiusSlider.Value = _effect.SpawnRadius;
            LifetimeSlider.Value = _effect.LeafLifetime;

            // Motion settings
            FallSpeedSlider.Value = _effect.FallSpeed;
            WindStrengthSlider.Value = _effect.WindStrength;
            WindFrequencySlider.Value = _effect.WindFrequency;
            TumbleSpeedSlider.Value = _effect.TumbleSpeed;
            SwayAmountSlider.Value = _effect.SwayAmount;

            // Appearance settings
            MinSizeSlider.Value = _effect.MinSize;
            MaxSizeSlider.Value = _effect.MaxSize;
            ColorVarietySlider.Value = _effect.ColorVariety;

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
    private void LeafCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        int value = (int)LeafCountSlider.Value;
        _effect.LeafCount = value;
        _effect.Configuration.Set("fl_leafCount", value);
        LeafCountValue.Text = value.ToString();
    }

    private void SpawnRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SpawnRadiusSlider.Value;
        _effect.SpawnRadius = value;
        _effect.Configuration.Set("fl_spawnRadius", value);
        SpawnRadiusValue.Text = value.ToString("F0");
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)LifetimeSlider.Value;
        _effect.LeafLifetime = value;
        _effect.Configuration.Set("fl_lifetime", value);
        LifetimeValue.Text = value.ToString("F0");
    }

    // Motion Settings
    private void FallSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)FallSpeedSlider.Value;
        _effect.FallSpeed = value;
        _effect.Configuration.Set("fl_fallSpeed", value);
        FallSpeedValue.Text = value.ToString("F0");
    }

    private void WindStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindStrengthSlider.Value;
        _effect.WindStrength = value;
        _effect.Configuration.Set("fl_windStrength", value);
        WindStrengthValue.Text = value.ToString("F0");
    }

    private void WindFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindFrequencySlider.Value;
        _effect.WindFrequency = value;
        _effect.Configuration.Set("fl_windFrequency", value);
        WindFrequencyValue.Text = value.ToString("F1");
    }

    private void TumbleSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)TumbleSpeedSlider.Value;
        _effect.TumbleSpeed = value;
        _effect.Configuration.Set("fl_tumbleSpeed", value);
        TumbleSpeedValue.Text = value.ToString("F1");
    }

    private void SwayAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SwayAmountSlider.Value;
        _effect.SwayAmount = value;
        _effect.Configuration.Set("fl_swayAmount", value);
        SwayAmountValue.Text = value.ToString("F0");
    }

    // Appearance Settings
    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MinSizeSlider.Value;
        _effect.MinSize = value;
        _effect.Configuration.Set("fl_minSize", value);
        MinSizeValue.Text = value.ToString("F0");
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MaxSizeSlider.Value;
        _effect.MaxSize = value;
        _effect.Configuration.Set("fl_maxSize", value);
        MaxSizeValue.Text = value.ToString("F0");
    }

    private void ColorVarietySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)ColorVarietySlider.Value;
        _effect.ColorVariety = value;
        _effect.Configuration.Set("fl_colorVariety", value);
        ColorVarietyValue.Text = value.ToString("F1");
    }
}
