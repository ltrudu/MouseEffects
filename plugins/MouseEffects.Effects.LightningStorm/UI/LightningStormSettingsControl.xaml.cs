using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.LightningStorm.UI;

public partial class LightningStormSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;
    private Vector4 _customColor = new(0.4f, 0.6f, 1f, 1f);

    public LightningStormSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        if (_effect.Configuration.TryGet<bool>("ls_onClickTrigger", out var onClick))
            OnClickCheckBox.IsChecked = onClick;

        if (_effect.Configuration.TryGet<bool>("ls_onMoveTrigger", out var onMove))
            OnMoveCheckBox.IsChecked = onMove;

        if (_effect.Configuration.TryGet<float>("ls_moveDistance", out var moveDist))
        {
            MoveDistanceSlider.Value = moveDist;
            MoveDistanceValue.Text = moveDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet<bool>("ls_randomTiming", out var randTime))
            RandomTimingCheckBox.IsChecked = randTime;

        if (_effect.Configuration.TryGet<float>("ls_minStrikeInterval", out var minInterval))
        {
            MinIntervalSlider.Value = minInterval;
            MinIntervalValue.Text = minInterval.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("ls_maxStrikeInterval", out var maxInterval))
        {
            MaxIntervalSlider.Value = maxInterval;
            MaxIntervalValue.Text = maxInterval.ToString("F1");
        }

        if (_effect.Configuration.TryGet<int>("ls_minBoltCount", out var minBolts))
        {
            MinBoltCountSlider.Value = minBolts;
            MinBoltCountValue.Text = minBolts.ToString();
        }

        if (_effect.Configuration.TryGet<int>("ls_maxBoltCount", out var maxBolts))
        {
            MaxBoltCountSlider.Value = maxBolts;
            MaxBoltCountValue.Text = maxBolts.ToString();
        }

        if (_effect.Configuration.TryGet<float>("ls_boltThickness", out var thickness))
        {
            ThicknessSlider.Value = thickness;
            ThicknessValue.Text = thickness.ToString("F1");
        }

        if (_effect.Configuration.TryGet<int>("ls_branchCount", out var branches))
        {
            BranchCountSlider.Value = branches;
            BranchCountValue.Text = branches.ToString();
        }

        if (_effect.Configuration.TryGet<float>("ls_branchProbability", out var branchProb))
        {
            BranchProbSlider.Value = branchProb;
            BranchProbValue.Text = branchProb.ToString("F2");
        }

        if (_effect.Configuration.TryGet<bool>("ls_strikeFromCursor", out var fromCursor))
            StrikeFromCursorCheckBox.IsChecked = fromCursor;

        if (_effect.Configuration.TryGet<bool>("ls_chainLightning", out var chain))
            ChainLightningCheckBox.IsChecked = chain;

        if (_effect.Configuration.TryGet<float>("ls_minStrikeDistance", out var minDist))
        {
            MinDistanceSlider.Value = minDist;
            MinDistanceValue.Text = minDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("ls_maxStrikeDistance", out var maxDist))
        {
            MaxDistanceSlider.Value = maxDist;
            MaxDistanceValue.Text = maxDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("ls_boltLifetime", out var lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F2");
        }

        if (_effect.Configuration.TryGet<float>("ls_flickerSpeed", out var flicker))
        {
            FlickerSpeedSlider.Value = flicker;
            FlickerSpeedValue.Text = flicker.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("ls_flashIntensity", out var flash))
        {
            FlashSlider.Value = flash;
            FlashValue.Text = flash.ToString("F2");
        }

        if (_effect.Configuration.TryGet<float>("ls_glowIntensity", out var glow))
        {
            GlowSlider.Value = glow;
            GlowValue.Text = glow.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("ls_persistenceEffect", out var persist))
            PersistenceCheckBox.IsChecked = persist;

        if (_effect.Configuration.TryGet<float>("ls_persistenceFade", out var persistFade))
        {
            PersistenceFadeSlider.Value = persistFade;
            PersistenceFadeValue.Text = persistFade.ToString("F1");
        }

        if (_effect.Configuration.TryGet<int>("ls_colorMode", out var colorMode))
            ColorModeCombo.SelectedIndex = colorMode;

        if (_effect.Configuration.TryGet<Vector4>("ls_customColor", out var customCol))
        {
            _customColor = customCol;
            UpdateCustomColorPreview();
        }

        if (_effect.Configuration.TryGet<bool>("ls_rainbowEnabled", out var rainbow))
            RainbowCheckBox.IsChecked = rainbow;

        if (_effect.Configuration.TryGet<float>("ls_rainbowSpeed", out var rainbowSpd))
        {
            RainbowSpeedSlider.Value = rainbowSpd;
            RainbowSpeedValue.Text = rainbowSpd.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("ls_fullStormColor", out var fullStorm))
            FullStormColorCheckBox.IsChecked = fullStorm;

        // Update visibility based on loaded settings
        UpdateCustomColorPanelVisibility();
        UpdateRainbowOptionsVisibility();

        if (_effect.Configuration.TryGet<bool>("ls_enableSparks", out var sparks))
            EnableSparksCheckBox.IsChecked = sparks;

        if (_effect.Configuration.TryGet<int>("ls_sparkCount", out var sparkCnt))
        {
            SparkCountSlider.Value = sparkCnt;
            SparkCountValue.Text = sparkCnt.ToString();
        }

        if (_effect.Configuration.TryGet<float>("ls_sparkLifetime", out var sparkLife))
        {
            SparkLifetimeSlider.Value = sparkLife;
            SparkLifetimeValue.Text = sparkLife.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("ls_sparkSpeed", out var sparkSpd))
        {
            SparkSpeedSlider.Value = sparkSpd;
            SparkSpeedValue.Text = sparkSpd.ToString("F0");
        }
    }

    private void UpdateCustomColorPreview()
    {
        CustomColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromRgb(
                (byte)(_customColor.X * 255),
                (byte)(_customColor.Y * 255),
                (byte)(_customColor.Z * 255)));
    }

    private void SavePropertyAndConfig<T>(string key, T value, Action? additionalAction = null)
    {
        if (_isLoading) return;

        _effect.Configuration.Set(key, value);

        // Update effect property using reflection
        var property = _effect.GetType().GetProperty(KeyToPropertyName(key));
        if (property != null && property.CanWrite)
        {
            property.SetValue(_effect, value);
        }

        additionalAction?.Invoke();
    }

    private string KeyToPropertyName(string configKey)
    {
        // Remove ls_ prefix and capitalize first letter of each word
        var parts = configKey.Replace("ls_", "").Split('_');
        return string.Join("", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
    }

    // Event handlers
    private void OnClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SavePropertyAndConfig("ls_onClickTrigger", OnClickCheckBox.IsChecked == true);
    }

    private void OnMoveCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SavePropertyAndConfig("ls_onMoveTrigger", OnMoveCheckBox.IsChecked == true);
    }

    private void MoveDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)MoveDistanceSlider.Value;
        MoveDistanceValue.Text = value.ToString("F0");
        SavePropertyAndConfig("ls_moveDistance", value);
    }

    private void RandomTimingCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SavePropertyAndConfig("ls_randomTiming", RandomTimingCheckBox.IsChecked == true);
    }

    private void MinIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)MinIntervalSlider.Value;
        MinIntervalValue.Text = value.ToString("F1");
        SavePropertyAndConfig("ls_minStrikeInterval", value);
    }

    private void MaxIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)MaxIntervalSlider.Value;
        MaxIntervalValue.Text = value.ToString("F1");
        SavePropertyAndConfig("ls_maxStrikeInterval", value);
    }

    private void MinBoltCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (int)MinBoltCountSlider.Value;
        MinBoltCountValue.Text = value.ToString();
        SavePropertyAndConfig("ls_minBoltCount", value);
    }

    private void MaxBoltCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (int)MaxBoltCountSlider.Value;
        MaxBoltCountValue.Text = value.ToString();
        SavePropertyAndConfig("ls_maxBoltCount", value);
    }

    private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)ThicknessSlider.Value;
        ThicknessValue.Text = value.ToString("F1");
        SavePropertyAndConfig("ls_boltThickness", value);
    }

    private void BranchCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (int)BranchCountSlider.Value;
        BranchCountValue.Text = value.ToString();
        SavePropertyAndConfig("ls_branchCount", value);
    }

    private void BranchProbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)BranchProbSlider.Value;
        BranchProbValue.Text = value.ToString("F2");
        SavePropertyAndConfig("ls_branchProbability", value);
    }

    private void StrikeFromCursorCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SavePropertyAndConfig("ls_strikeFromCursor", StrikeFromCursorCheckBox.IsChecked == true);
    }

    private void ChainLightningCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SavePropertyAndConfig("ls_chainLightning", ChainLightningCheckBox.IsChecked == true);
    }

    private void MinDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)MinDistanceSlider.Value;
        MinDistanceValue.Text = value.ToString("F0");
        SavePropertyAndConfig("ls_minStrikeDistance", value);
    }

    private void MaxDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)MaxDistanceSlider.Value;
        MaxDistanceValue.Text = value.ToString("F0");
        SavePropertyAndConfig("ls_maxStrikeDistance", value);
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)LifetimeSlider.Value;
        LifetimeValue.Text = value.ToString("F2");
        SavePropertyAndConfig("ls_boltLifetime", value);
    }

    private void FlickerSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)FlickerSpeedSlider.Value;
        FlickerSpeedValue.Text = value.ToString("F0");
        SavePropertyAndConfig("ls_flickerSpeed", value);
    }

    private void FlashSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)FlashSlider.Value;
        FlashValue.Text = value.ToString("F2");
        SavePropertyAndConfig("ls_flashIntensity", value);
    }

    private void GlowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)GlowSlider.Value;
        GlowValue.Text = value.ToString("F1");
        SavePropertyAndConfig("ls_glowIntensity", value);
    }

    private void PersistenceCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SavePropertyAndConfig("ls_persistenceEffect", PersistenceCheckBox.IsChecked == true);
    }

    private void PersistenceFadeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)PersistenceFadeSlider.Value;
        PersistenceFadeValue.Text = value.ToString("F1");
        SavePropertyAndConfig("ls_persistenceFade", value);
    }

    private void ColorModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        SavePropertyAndConfig("ls_colorMode", ColorModeCombo.SelectedIndex);
        UpdateCustomColorPanelVisibility();
    }

    private void UpdateCustomColorPanelVisibility()
    {
        // Show custom color options when "Custom" (index 3) is selected
        CustomColorPanel.Visibility = ColorModeCombo.SelectedIndex == 3
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateRainbowOptionsVisibility()
    {
        // Show rainbow options when rainbow checkbox is checked
        RainbowOptionsPanel.Visibility = RainbowCheckBox.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RainbowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        SavePropertyAndConfig("ls_rainbowEnabled", RainbowCheckBox.IsChecked == true);
        UpdateRainbowOptionsVisibility();
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)RainbowSpeedSlider.Value;
        RainbowSpeedValue.Text = value.ToString("F1");
        SavePropertyAndConfig("ls_rainbowSpeed", value);
    }

    private void FullStormColorCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        SavePropertyAndConfig("ls_fullStormColor", FullStormColorCheckBox.IsChecked == true);
    }

    private void CustomColorButton_Click(object sender, RoutedEventArgs e)
    {
        using var colorDialog = new ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(_customColor.X * 255),
                (int)(_customColor.Y * 255),
                (int)(_customColor.Z * 255))
        };

        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            _customColor = new Vector4(
                colorDialog.Color.R / 255f,
                colorDialog.Color.G / 255f,
                colorDialog.Color.B / 255f,
                1f);

            UpdateCustomColorPreview();
            SavePropertyAndConfig("ls_customColor", _customColor);
        }
    }

    private void EnableSparksCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SavePropertyAndConfig("ls_enableSparks", EnableSparksCheckBox.IsChecked == true);
    }

    private void SparkCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (int)SparkCountSlider.Value;
        SparkCountValue.Text = value.ToString();
        SavePropertyAndConfig("ls_sparkCount", value);
    }

    private void SparkLifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)SparkLifetimeSlider.Value;
        SparkLifetimeValue.Text = value.ToString("F1");
        SavePropertyAndConfig("ls_sparkLifetime", value);
    }

    private void SparkSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        var value = (float)SparkSpeedSlider.Value;
        SparkSpeedValue.Text = value.ToString("F0");
        SavePropertyAndConfig("ls_sparkSpeed", value);
    }
}
