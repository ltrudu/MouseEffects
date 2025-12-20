using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.PixelExplosion.UI;

public partial class PixelExplosionSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public PixelExplosionSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Trigger settings
        if (_effect.Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
            SpawnOnLeftClickCheckBox.IsChecked = leftClick;

        if (_effect.Configuration.TryGet("spawnOnRightClick", out bool rightClick))
            SpawnOnRightClickCheckBox.IsChecked = rightClick;

        if (_effect.Configuration.TryGet("spawnOnMouseMove", out bool mouseMove))
        {
            SpawnOnMouseMoveCheckBox.IsChecked = mouseMove;
            UpdateMouseThresholdVisibility(mouseMove);
        }

        if (_effect.Configuration.TryGet("mouseThreshold", out float threshold))
        {
            MouseThresholdSlider.Value = threshold;
            MouseThresholdValue.Text = threshold.ToString("F0");
        }

        // Explosion settings
        if (_effect.Configuration.TryGet("pixelCountMin", out int minCount))
        {
            PixelCountMinSlider.Value = minCount;
            PixelCountMinValue.Text = minCount.ToString();
        }

        if (_effect.Configuration.TryGet("pixelCountMax", out int maxCount))
        {
            PixelCountMaxSlider.Value = maxCount;
            PixelCountMaxValue.Text = maxCount.ToString();
        }

        if (_effect.Configuration.TryGet("explosionForce", out float force))
        {
            ExplosionForceSlider.Value = force;
            ExplosionForceValue.Text = force.ToString("F0");
        }

        // Pixel settings
        if (_effect.Configuration.TryGet("pixelSizeMin", out float minSize))
        {
            PixelSizeMinSlider.Value = minSize;
            PixelSizeMinValue.Text = minSize.ToString("F1");
        }

        if (_effect.Configuration.TryGet("pixelSizeMax", out float maxSize))
        {
            PixelSizeMaxSlider.Value = maxSize;
            PixelSizeMaxValue.Text = maxSize.ToString("F1");
        }

        if (_effect.Configuration.TryGet("lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F1");
        }

        // Physics settings
        if (_effect.Configuration.TryGet("gravity", out float gravity))
        {
            GravitySlider.Value = gravity;
            GravityValue.Text = gravity.ToString("F0");
        }

        // Color palette
        if (_effect.Configuration.TryGet("colorPalette", out int palette))
        {
            ColorPaletteCombo.SelectedIndex = palette;
            UpdateRainbowSpeedVisibility(palette);
        }

        // Rainbow speed
        if (_effect.Configuration.TryGet("rainbowSpeed", out float rainbowSpeed))
        {
            RainbowSpeedSlider.Value = rainbowSpeed;
            RainbowSpeedValue.Text = rainbowSpeed.ToString("F1");
        }

        // Performance settings
        if (_effect.Configuration.TryGet("maxPixels", out int maxPix))
        {
            MaxPixelsSlider.Value = maxPix;
            MaxPixelsValue.Text = maxPix.ToString();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Trigger settings
        config.Set("spawnOnLeftClick", SpawnOnLeftClickCheckBox.IsChecked ?? true);
        config.Set("spawnOnRightClick", SpawnOnRightClickCheckBox.IsChecked ?? false);
        config.Set("spawnOnMouseMove", SpawnOnMouseMoveCheckBox.IsChecked ?? false);
        config.Set("mouseThreshold", (float)MouseThresholdSlider.Value);

        // Explosion settings
        config.Set("pixelCountMin", (int)PixelCountMinSlider.Value);
        config.Set("pixelCountMax", (int)PixelCountMaxSlider.Value);
        config.Set("explosionForce", (float)ExplosionForceSlider.Value);

        // Pixel settings
        config.Set("pixelSizeMin", (float)PixelSizeMinSlider.Value);
        config.Set("pixelSizeMax", (float)PixelSizeMaxSlider.Value);
        config.Set("lifetime", (float)LifetimeSlider.Value);

        // Physics settings
        config.Set("gravity", (float)GravitySlider.Value);

        // Color palette
        config.Set("colorPalette", ColorPaletteCombo.SelectedIndex);

        // Rainbow speed
        config.Set("rainbowSpeed", (float)RainbowSpeedSlider.Value);

        // Performance settings
        config.Set("maxPixels", (int)MaxPixelsSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void SpawnOnLeftClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SpawnOnRightClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SpawnOnMouseMoveCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateMouseThresholdVisibility(SpawnOnMouseMoveCheckBox.IsChecked ?? false);
        UpdateConfiguration();
    }

    private void UpdateMouseThresholdVisibility(bool visible)
    {
        if (MouseThresholdPanel != null)
        {
            MouseThresholdPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void MouseThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MouseThresholdValue != null)
            MouseThresholdValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void PixelCountMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PixelCountMinValue != null)
            PixelCountMinValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void PixelCountMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PixelCountMaxValue != null)
            PixelCountMaxValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ExplosionForceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ExplosionForceValue != null)
            ExplosionForceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void PixelSizeMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PixelSizeMinValue != null)
            PixelSizeMinValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void PixelSizeMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PixelSizeMaxValue != null)
            PixelSizeMaxValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GravitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GravityValue != null)
            GravityValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ColorPaletteCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateRainbowSpeedVisibility(ColorPaletteCombo.SelectedIndex);
        UpdateConfiguration();
    }

    private void UpdateRainbowSpeedVisibility(int paletteIndex)
    {
        if (RainbowSpeedPanel != null)
        {
            // Index 5 = Animated Rainbow
            RainbowSpeedPanel.Visibility = paletteIndex == 5 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RainbowSpeedValue != null)
            RainbowSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MaxPixelsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxPixelsValue != null)
            MaxPixelsValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }
}
