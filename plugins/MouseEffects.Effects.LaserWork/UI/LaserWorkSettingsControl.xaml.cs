using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;
using Color = System.Windows.Media.Color;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.LaserWork.UI;

public partial class LaserWorkSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private Vector4 _laserColor = new(1f, 0.2f, 0.2f, 1f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public LaserWorkSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        // Emission rate
        if (_effect.Configuration.TryGet("lasersPerSecond", out float lps))
        {
            LasersPerSecondSlider.Value = lps;
            LasersPerSecondValue.Text = lps.ToString("F0");
        }

        // Direction settings
        if (_effect.Configuration.TryGet("shootForward", out bool shootForward))
            ShootForwardCheckBox.IsChecked = shootForward;
        if (_effect.Configuration.TryGet("shootBackward", out bool shootBackward))
            ShootBackwardCheckBox.IsChecked = shootBackward;
        if (_effect.Configuration.TryGet("shootLeft", out bool shootLeft))
            ShootLeftCheckBox.IsChecked = shootLeft;
        if (_effect.Configuration.TryGet("shootRight", out bool shootRight))
            ShootRightCheckBox.IsChecked = shootRight;

        // Size settings
        if (_effect.Configuration.TryGet("minLaserLength", out float minLength))
        {
            MinLengthSlider.Value = minLength;
            MinLengthValue.Text = minLength.ToString("F0");
        }
        if (_effect.Configuration.TryGet("maxLaserLength", out float maxLength))
        {
            MaxLengthSlider.Value = maxLength;
            MaxLengthValue.Text = maxLength.ToString("F0");
        }
        if (_effect.Configuration.TryGet("minLaserWidth", out float minWidth))
        {
            MinWidthSlider.Value = minWidth;
            MinWidthValue.Text = minWidth.ToString("F1");
        }
        if (_effect.Configuration.TryGet("maxLaserWidth", out float maxWidth))
        {
            MaxWidthSlider.Value = maxWidth;
            MaxWidthValue.Text = maxWidth.ToString("F1");
        }
        if (_effect.Configuration.TryGet("autoShrink", out bool autoShrink))
            AutoShrinkCheckBox.IsChecked = autoShrink;

        // Physics settings
        if (_effect.Configuration.TryGet("laserSpeed", out float speed))
        {
            SpeedSlider.Value = speed;
            SpeedValue.Text = speed.ToString("F0");
        }
        if (_effect.Configuration.TryGet("laserLifespan", out float lifespan))
        {
            LifespanSlider.Value = lifespan;
            LifespanValue.Text = lifespan.ToString("F1");
        }

        // Alpha settings
        if (_effect.Configuration.TryGet("minAlpha", out float minAlpha))
        {
            MinAlphaSlider.Value = minAlpha;
            MinAlphaValue.Text = minAlpha.ToString("F2");
        }
        if (_effect.Configuration.TryGet("maxAlpha", out float maxAlpha))
        {
            MaxAlphaSlider.Value = maxAlpha;
            MaxAlphaValue.Text = maxAlpha.ToString("F2");
        }

        // Glow settings
        if (_effect.Configuration.TryGet("glowIntensity", out float glow))
        {
            GlowIntensitySlider.Value = glow;
            GlowIntensityValue.Text = glow.ToString("F2");
        }

        // Color settings
        if (_effect.Configuration.TryGet("laserColor", out Vector4 color))
        {
            _laserColor = color;
            UpdateColorPreview();
        }

        // Rainbow settings
        if (_effect.Configuration.TryGet("rainbowMode", out bool rainbow))
        {
            RainbowModeCheckBox.IsChecked = rainbow;
            UpdateRainbowUI(rainbow);
        }
        if (_effect.Configuration.TryGet("rainbowSpeed", out float rainbowSpeed))
        {
            RainbowSpeedSlider.Value = rainbowSpeed;
            RainbowSpeedValue.Text = rainbowSpeed.ToString("F1");
        }

        // Collision explosion settings
        if (_effect.Configuration.TryGet("enableCollisionExplosion", out bool enableExplosion))
        {
            EnableCollisionExplosionCheckBox.IsChecked = enableExplosion;
            UpdateExplosionUI(enableExplosion);
        }
        if (_effect.Configuration.TryGet("explosionLaserCount", out float explosionCount))
        {
            ExplosionCountSlider.Value = explosionCount;
            ExplosionCountValue.Text = ((int)explosionCount).ToString();
        }
        if (_effect.Configuration.TryGet("explosionLifespanMultiplier", out float explosionLifespan))
        {
            ExplosionLifespanSlider.Value = explosionLifespan;
            ExplosionLifespanValue.Text = explosionLifespan.ToString("F2");
        }
        if (_effect.Configuration.TryGet("explosionLasersCanCollide", out bool collideAlways))
        {
            CollideAlwaysCheckBox.IsChecked = collideAlways;
            UpdateCollideAlwaysUI(collideAlways);
        }
        if (_effect.Configuration.TryGet("maxCollisionCount", out float maxCollisions))
        {
            MaxCollisionSlider.Value = maxCollisions;
            MaxCollisionValue.Text = ((int)maxCollisions).ToString();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();

        // Emission rate
        config.Set("lasersPerSecond", (float)LasersPerSecondSlider.Value);

        // Direction settings
        config.Set("shootForward", ShootForwardCheckBox.IsChecked ?? true);
        config.Set("shootBackward", ShootBackwardCheckBox.IsChecked ?? true);
        config.Set("shootLeft", ShootLeftCheckBox.IsChecked ?? true);
        config.Set("shootRight", ShootRightCheckBox.IsChecked ?? true);

        // Size settings
        config.Set("minLaserLength", (float)MinLengthSlider.Value);
        config.Set("maxLaserLength", (float)MaxLengthSlider.Value);
        config.Set("minLaserWidth", (float)MinWidthSlider.Value);
        config.Set("maxLaserWidth", (float)MaxWidthSlider.Value);
        config.Set("autoShrink", AutoShrinkCheckBox.IsChecked ?? false);

        // Physics settings
        config.Set("laserSpeed", (float)SpeedSlider.Value);
        config.Set("laserLifespan", (float)LifespanSlider.Value);

        // Alpha settings
        config.Set("minAlpha", (float)MinAlphaSlider.Value);
        config.Set("maxAlpha", (float)MaxAlphaSlider.Value);

        // Glow settings
        config.Set("glowIntensity", (float)GlowIntensitySlider.Value);

        // Color settings
        config.Set("laserColor", _laserColor);

        // Rainbow settings
        config.Set("rainbowMode", RainbowModeCheckBox.IsChecked ?? false);
        config.Set("rainbowSpeed", (float)RainbowSpeedSlider.Value);

        // Collision explosion settings
        config.Set("enableCollisionExplosion", EnableCollisionExplosionCheckBox.IsChecked ?? false);
        config.Set("explosionLaserCount", (float)ExplosionCountSlider.Value);
        config.Set("explosionLifespanMultiplier", (float)ExplosionLifespanSlider.Value);
        config.Set("explosionLasersCanCollide", CollideAlwaysCheckBox.IsChecked ?? false);
        config.Set("maxCollisionCount", (float)MaxCollisionSlider.Value);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateColorPreview()
    {
        ColorPreview.Background = new SolidColorBrush(Color.FromArgb(
            (byte)(_laserColor.W * 255),
            (byte)(_laserColor.X * 255),
            (byte)(_laserColor.Y * 255),
            (byte)(_laserColor.Z * 255)));
    }

    private void UpdateRainbowUI(bool rainbowEnabled)
    {
        ColorSettingsPanel.Visibility = rainbowEnabled ? Visibility.Collapsed : Visibility.Visible;
        RainbowSettingsPanel.Visibility = rainbowEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateExplosionUI(bool explosionEnabled)
    {
        ExplosionSettingsPanel.Visibility = explosionEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateCollideAlwaysUI(bool collideAlwaysEnabled)
    {
        MaxCollisionPanel.Visibility = collideAlwaysEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DirectionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void LasersPerSecondSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LasersPerSecondValue != null) LasersPerSecondValue.Text = LasersPerSecondSlider.Value.ToString("F0");
        UpdateConfiguration();
    }

    private void MinLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 10;
        var minVal = e.NewValue;
        var maxVal = MaxLengthSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 1)
        {
            var newMax = Math.Min(minVal + minGap, MaxLengthSlider.Maximum);
            MaxLengthSlider.Value = newMax;
            MaxLengthValue.Text = newMax.ToString("F0");
        }

        if (MinLengthValue != null) MinLengthValue.Text = minVal.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 10;
        var maxVal = e.NewValue;
        var minVal = MinLengthSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 1)
        {
            var newMin = Math.Max(maxVal - minGap, MinLengthSlider.Minimum);
            MinLengthSlider.Value = newMin;
            MinLengthValue.Text = newMin.ToString("F0");
        }

        if (MaxLengthValue != null) MaxLengthValue.Text = maxVal.ToString("F0");
        UpdateConfiguration();
    }

    private void MinWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 1;
        var minVal = e.NewValue;
        var maxVal = MaxWidthSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 0.1)
        {
            var newMax = Math.Min(minVal + minGap, MaxWidthSlider.Maximum);
            MaxWidthSlider.Value = newMax;
            MaxWidthValue.Text = newMax.ToString("F1");
        }

        if (MinWidthValue != null) MinWidthValue.Text = minVal.ToString("F1");
        UpdateConfiguration();
    }

    private void MaxWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 1;
        var maxVal = e.NewValue;
        var minVal = MinWidthSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 0.1)
        {
            var newMin = Math.Max(maxVal - minGap, MinWidthSlider.Minimum);
            MinWidthSlider.Value = newMin;
            MinWidthValue.Text = newMin.ToString("F1");
        }

        if (MaxWidthValue != null) MaxWidthValue.Text = maxVal.ToString("F1");
        UpdateConfiguration();
    }

    private void AutoShrinkCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void PhysicsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpeedValue != null) SpeedValue.Text = SpeedSlider.Value.ToString("F0");
        if (LifespanValue != null) LifespanValue.Text = LifespanSlider.Value.ToString("F1");
        UpdateConfiguration();
    }

    private void MinAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 0.1;
        var minVal = e.NewValue;
        var maxVal = MaxAlphaSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 0.01)
        {
            var newMax = Math.Min(minVal + minGap, MaxAlphaSlider.Maximum);
            MaxAlphaSlider.Value = newMax;
            MaxAlphaValue.Text = newMax.ToString("F2");
        }

        if (MinAlphaValue != null) MinAlphaValue.Text = minVal.ToString("F2");
        UpdateConfiguration();
    }

    private void MaxAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 0.1;
        var maxVal = e.NewValue;
        var minVal = MinAlphaSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 0.01)
        {
            var newMin = Math.Max(maxVal - minGap, MinAlphaSlider.Minimum);
            MinAlphaSlider.Value = newMin;
            MinAlphaValue.Text = newMin.ToString("F2");
        }

        if (MaxAlphaValue != null) MaxAlphaValue.Text = maxVal.ToString("F2");
        UpdateConfiguration();
    }

    private void GlowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null) GlowIntensityValue.Text = GlowIntensitySlider.Value.ToString("F2");
        UpdateConfiguration();
    }

    private void RainbowModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        bool rainbowEnabled = RainbowModeCheckBox.IsChecked ?? false;
        UpdateRainbowUI(rainbowEnabled);
        UpdateConfiguration();
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RainbowSpeedValue != null) RainbowSpeedValue.Text = RainbowSpeedSlider.Value.ToString("F1");
        UpdateConfiguration();
    }

    private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
    {
        // Use Windows Forms color dialog
        using var colorDialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(_laserColor.W * 255),
                (int)(_laserColor.X * 255),
                (int)(_laserColor.Y * 255),
                (int)(_laserColor.Z * 255)),
            FullOpen = true
        };

        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = colorDialog.Color;
            _laserColor = new Vector4(
                c.R / 255f,
                c.G / 255f,
                c.B / 255f,
                1f);
            UpdateColorPreview();
            UpdateConfiguration();
        }
    }

    private void CollisionExplosionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        bool explosionEnabled = EnableCollisionExplosionCheckBox.IsChecked ?? false;
        UpdateExplosionUI(explosionEnabled);
        UpdateConfiguration();
    }

    private void ExplosionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ExplosionCountValue != null) ExplosionCountValue.Text = ((int)ExplosionCountSlider.Value).ToString();
        if (ExplosionLifespanValue != null) ExplosionLifespanValue.Text = ExplosionLifespanSlider.Value.ToString("F2");
        if (MaxCollisionValue != null) MaxCollisionValue.Text = ((int)MaxCollisionSlider.Value).ToString();
        UpdateConfiguration();
    }

    private void CollideAlwaysCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        bool collideAlwaysEnabled = CollideAlwaysCheckBox.IsChecked ?? false;
        UpdateCollideAlwaysUI(collideAlwaysEnabled);
        UpdateConfiguration();
    }
}
