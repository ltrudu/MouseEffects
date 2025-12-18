using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Shockwave.UI;

public partial class ShockwaveSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;
    private bool _isExpanded;
    private Vector4 _customColor = new(0.0f, 0.5f, 1.0f, 1.0f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public ShockwaveSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        // Load current values from effect configuration
        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("sw_maxShockwaves", out int maxShockwaves))
        {
            MaxShockwavesSlider.Value = maxShockwaves;
            MaxShockwavesValue.Text = maxShockwaves.ToString();
        }

        if (_effect.Configuration.TryGet("sw_ringLifespan", out float lifespan))
        {
            LifespanSlider.Value = lifespan;
            LifespanValue.Text = lifespan.ToString("F1");
        }

        if (_effect.Configuration.TryGet("sw_expansionSpeed", out float speed))
        {
            ExpansionSpeedSlider.Value = speed;
            ExpansionSpeedValue.Text = speed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("sw_maxRadius", out float maxRadius))
        {
            MaxRadiusSlider.Value = maxRadius;
            MaxRadiusValue.Text = maxRadius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("sw_ringThickness", out float thickness))
        {
            ThicknessSlider.Value = thickness;
            ThicknessValue.Text = thickness.ToString("F0");
        }

        if (_effect.Configuration.TryGet("sw_glowIntensity", out float glowIntensity))
        {
            GlowIntensitySlider.Value = glowIntensity;
            GlowIntensityValue.Text = glowIntensity.ToString("F1");
        }

        if (_effect.Configuration.TryGet("sw_enableDistortion", out bool enableDistortion))
        {
            DistortionCheckBox.IsChecked = enableDistortion;
        }

        if (_effect.Configuration.TryGet("sw_distortionStrength", out float distortionStrength))
        {
            DistortionStrengthSlider.Value = distortionStrength;
            DistortionStrengthValue.Text = distortionStrength.ToString("F0");
        }

        if (_effect.Configuration.TryGet("sw_hdrBrightness", out float hdrBrightness))
        {
            HdrBrightnessSlider.Value = hdrBrightness;
            HdrBrightnessValue.Text = hdrBrightness.ToString("F1");
        }

        if (_effect.Configuration.TryGet("sw_spawnOnLeftClick", out bool leftClick))
        {
            LeftClickCheckBox.IsChecked = leftClick;
        }

        if (_effect.Configuration.TryGet("sw_spawnOnRightClick", out bool rightClick))
        {
            RightClickCheckBox.IsChecked = rightClick;
        }

        if (_effect.Configuration.TryGet("sw_spawnOnMove", out bool spawnOnMove))
        {
            SpawnOnMoveCheckBox.IsChecked = spawnOnMove;
        }

        if (_effect.Configuration.TryGet("sw_moveSpawnDistance", out float moveSpawnDist))
        {
            MoveSpawnDistanceSlider.Value = moveSpawnDist;
            MoveSpawnDistanceValue.Text = moveSpawnDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet("sw_moveRingLifespan", out float moveLifespan))
        {
            MoveLifespanSlider.Value = moveLifespan;
            MoveLifespanValue.Text = moveLifespan.ToString("F1");
        }

        if (_effect.Configuration.TryGet("sw_moveExpansionSpeed", out float moveSpeed))
        {
            MoveExpansionSpeedSlider.Value = moveSpeed;
            MoveExpansionSpeedValue.Text = moveSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("sw_colorPreset", out int colorPreset))
        {
            ColorPresetCombo.SelectedIndex = colorPreset;
            UpdateCustomColorVisibility();
        }

        if (_effect.Configuration.TryGet("sw_customColor", out Vector4 customColor))
        {
            _customColor = customColor;
            UpdateCustomColorPreview();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();
        config.Set("sw_maxShockwaves", (int)MaxShockwavesSlider.Value);
        config.Set("sw_ringLifespan", (float)LifespanSlider.Value);
        config.Set("sw_expansionSpeed", (float)ExpansionSpeedSlider.Value);
        config.Set("sw_maxRadius", (float)MaxRadiusSlider.Value);
        config.Set("sw_ringThickness", (float)ThicknessSlider.Value);
        config.Set("sw_glowIntensity", (float)GlowIntensitySlider.Value);
        config.Set("sw_enableDistortion", DistortionCheckBox.IsChecked ?? true);
        config.Set("sw_distortionStrength", (float)DistortionStrengthSlider.Value);
        config.Set("sw_hdrBrightness", (float)HdrBrightnessSlider.Value);
        config.Set("sw_spawnOnLeftClick", LeftClickCheckBox.IsChecked ?? true);
        config.Set("sw_spawnOnRightClick", RightClickCheckBox.IsChecked ?? false);
        config.Set("sw_spawnOnMove", SpawnOnMoveCheckBox.IsChecked ?? false);
        config.Set("sw_moveSpawnDistance", (float)MoveSpawnDistanceSlider.Value);
        config.Set("sw_moveRingLifespan", (float)MoveLifespanSlider.Value);
        config.Set("sw_moveExpansionSpeed", (float)MoveExpansionSpeedSlider.Value);
        config.Set("sw_colorPreset", ColorPresetCombo.SelectedIndex);
        config.Set("sw_customColor", _customColor);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateCustomColorVisibility()
    {
        // Show custom color picker only when "Custom" is selected (index 3)
        CustomColorPanel.Visibility = ColorPresetCombo.SelectedIndex == 3
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateCustomColorPreview()
    {
        CustomColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromRgb(
                (byte)(_customColor.X * 255),
                (byte)(_customColor.Y * 255),
                (byte)(_customColor.Z * 255)));
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "\u25B2" : "\u25BC";
    }

    private void MaxShockwavesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxShockwavesValue != null)
            MaxShockwavesValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void LifespanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifespanValue != null)
            LifespanValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ExpansionSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ExpansionSpeedValue != null)
            ExpansionSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxRadiusValue != null)
            MaxRadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThicknessValue != null)
            ThicknessValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void DistortionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void DistortionStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DistortionStrengthValue != null)
            DistortionStrengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void HdrBrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HdrBrightnessValue != null)
            HdrBrightnessValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void LeftClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RightClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SpawnOnMoveCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void MoveSpawnDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveSpawnDistanceValue != null)
            MoveSpawnDistanceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MoveLifespanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveLifespanValue != null)
            MoveLifespanValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MoveExpansionSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveExpansionSpeedValue != null)
            MoveExpansionSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ColorPresetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateCustomColorVisibility();
        UpdateConfiguration();
    }

    private void CustomColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Use Windows Forms ColorDialog
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                255, // Full opacity for ring color
                (int)(_customColor.X * 255),
                (int)(_customColor.Y * 255),
                (int)(_customColor.Z * 255)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _customColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f); // Always full opacity
            UpdateCustomColorPreview();
            UpdateConfiguration();
        }
    }
}
