using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.FlowerBloom.UI;

public partial class FlowerBloomSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;
    private bool _isExpanded;

    public event Action<string>? SettingsChanged;

    public FlowerBloomSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        // Trigger settings
        if (_effect.Configuration.TryGet("fb_leftClickEnabled", out bool leftEnabled))
            LeftClickCheckBox.IsChecked = leftEnabled;

        if (_effect.Configuration.TryGet("fb_rightClickEnabled", out bool rightEnabled))
            RightClickCheckBox.IsChecked = rightEnabled;

        if (_effect.Configuration.TryGet("fb_continuousSpawn", out bool continuous))
            ContinuousSpawnCheckBox.IsChecked = continuous;

        if (_effect.Configuration.TryGet("fb_spawnRate", out float spawnRate))
        {
            SpawnRateSlider.Value = spawnRate;
            SpawnRateValue.Text = spawnRate.ToString("F1");
        }

        // Flower settings
        if (_effect.Configuration.TryGet("fb_flowerType", out int flowerType))
            FlowerTypeCombo.SelectedIndex = flowerType;

        if (_effect.Configuration.TryGet("fb_colorPalette", out int colorPalette))
            ColorPaletteCombo.SelectedIndex = colorPalette;

        if (_effect.Configuration.TryGet("fb_petalCount", out int petalCount))
        {
            PetalCountSlider.Value = petalCount;
            PetalCountValue.Text = petalCount.ToString();
        }

        if (_effect.Configuration.TryGet("fb_flowerSize", out float flowerSize))
        {
            FlowerSizeSlider.Value = flowerSize;
            FlowerSizeValue.Text = flowerSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("fb_showStem", out bool showStem))
            ShowStemCheckBox.IsChecked = showStem;

        if (_effect.Configuration.TryGet("fb_sizeVariation", out bool sizeVariation))
            SizeVariationCheckBox.IsChecked = sizeVariation;

        if (_effect.Configuration.TryGet("fb_sizeVariationAmount", out float sizeVariationAmount))
        {
            SizeVariationSlider.Value = sizeVariationAmount;
            SizeVariationValue.Text = sizeVariationAmount.ToString("F2");
        }

        // Animation settings
        if (_effect.Configuration.TryGet("fb_bloomDuration", out float bloomDuration))
        {
            BloomDurationSlider.Value = bloomDuration;
            BloomDurationValue.Text = bloomDuration.ToString("F1");
        }

        if (_effect.Configuration.TryGet("fb_flowerLifetime", out float flowerLifetime))
        {
            FlowerLifetimeSlider.Value = flowerLifetime;
            FlowerLifetimeValue.Text = flowerLifetime.ToString("F1");
        }

        if (_effect.Configuration.TryGet("fb_fadeOutDuration", out float fadeOutDuration))
        {
            FadeOutDurationSlider.Value = fadeOutDuration;
            FadeOutDurationValue.Text = fadeOutDuration.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Trigger settings
        config.Set("fb_leftClickEnabled", LeftClickCheckBox.IsChecked ?? true);
        config.Set("fb_rightClickEnabled", RightClickCheckBox.IsChecked ?? true);
        config.Set("fb_continuousSpawn", ContinuousSpawnCheckBox.IsChecked ?? false);
        config.Set("fb_spawnRate", (float)SpawnRateSlider.Value);

        // Flower settings
        config.Set("fb_flowerType", FlowerTypeCombo.SelectedIndex);
        config.Set("fb_colorPalette", ColorPaletteCombo.SelectedIndex);
        config.Set("fb_petalCount", (int)PetalCountSlider.Value);
        config.Set("fb_flowerSize", (float)FlowerSizeSlider.Value);
        config.Set("fb_showStem", ShowStemCheckBox.IsChecked ?? true);
        config.Set("fb_sizeVariation", SizeVariationCheckBox.IsChecked ?? true);
        config.Set("fb_sizeVariationAmount", (float)SizeVariationSlider.Value);

        // Animation settings
        config.Set("fb_bloomDuration", (float)BloomDurationSlider.Value);
        config.Set("fb_flowerLifetime", (float)FlowerLifetimeSlider.Value);
        config.Set("fb_fadeOutDuration", (float)FadeOutDurationSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void LeftClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RightClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ContinuousSpawnCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SpawnRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpawnRateValue != null)
            SpawnRateValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FlowerTypeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ColorPaletteCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void PetalCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PetalCountValue != null)
            PetalCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void FlowerSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FlowerSizeValue != null)
            FlowerSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ShowStemCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SizeVariationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SizeVariationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SizeVariationValue != null)
            SizeVariationValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void BloomDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BloomDurationValue != null)
            BloomDurationValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FlowerLifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FlowerLifetimeValue != null)
            FlowerLifetimeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FadeOutDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeOutDurationValue != null)
            FadeOutDurationValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
