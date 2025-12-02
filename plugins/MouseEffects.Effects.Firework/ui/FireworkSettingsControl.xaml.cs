using System.Drawing;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Firework.UI;

public partial class FireworkSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private bool _isExpanded;
    private Vector4 _primaryColor = new(1f, 0.3f, 0.1f, 1f);
    private Vector4 _secondaryColor = new(1f, 0.8f, 0.2f, 1f);
    private Vector4 _rocketPrimaryColor = new(1f, 0.8f, 0.2f, 1f);
    private Vector4 _rocketSecondaryColor = new(1f, 0.4f, 0.1f, 1f);

    public event Action<string>? SettingsChanged;

    public FireworkSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet<int>("maxParticles", out var maxPart))
        {
            MaxParticlesSlider.Value = maxPart;
            MaxParticlesValue.Text = maxPart.ToString();
        }

        if (_effect.Configuration.TryGet<int>("maxFireworks", out var maxFw))
        {
            MaxFireworksSlider.Value = maxFw;
            MaxFireworksValue.Text = maxFw.ToString();
        }

        if (_effect.Configuration.TryGet<float>("particleLifespan", out var lifespan))
        {
            LifespanSlider.Value = lifespan;
            LifespanValue.Text = lifespan.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("spawnOnLeftClick", out var leftClick))
            LeftClickCheckBox.IsChecked = leftClick;

        if (_effect.Configuration.TryGet<bool>("spawnOnRightClick", out var rightClick))
            RightClickCheckBox.IsChecked = rightClick;

        if (_effect.Configuration.TryGet<int>("minParticlesPerFirework", out var minPartFw))
        {
            MinParticlesPerFireworkSlider.Value = minPartFw;
            MinParticlesPerFireworkValue.Text = minPartFw.ToString();
        }

        if (_effect.Configuration.TryGet<int>("maxParticlesPerFirework", out var maxPartFw))
        {
            MaxParticlesPerFireworkSlider.Value = maxPartFw;
            MaxParticlesPerFireworkValue.Text = maxPartFw.ToString();
        }

        if (_effect.Configuration.TryGet<float>("clickExplosionForce", out var clickForce))
        {
            ClickForceSlider.Value = clickForce;
            ClickForceValue.Text = clickForce.ToString("F0");
        }

        if (_effect.Configuration.TryGet<bool>("spawnOnMove", out var spawnMove))
            SpawnOnMoveCheckBox.IsChecked = spawnMove;

        if (_effect.Configuration.TryGet<float>("moveSpawnDistance", out var moveDist))
        {
            MoveSpawnDistanceSlider.Value = moveDist;
            MoveSpawnDistanceValue.Text = moveDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("moveExplosionForce", out var moveForce))
        {
            MoveForceSlider.Value = moveForce;
            MoveForceValue.Text = moveForce.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("minParticleSize", out var minSize))
        {
            MinSizeSlider.Value = minSize;
            MinSizeValue.Text = minSize.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("maxParticleSize", out var maxSize))
        {
            MaxSizeSlider.Value = maxSize;
            MaxSizeValue.Text = maxSize.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("glowIntensity", out var glow))
        {
            GlowSlider.Value = glow;
            GlowValue.Text = glow.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("enableTrails", out var trails))
            EnableTrailsCheckBox.IsChecked = trails;

        if (_effect.Configuration.TryGet<float>("trailLength", out var trailLen))
        {
            TrailLengthSlider.Value = trailLen;
            TrailLengthValue.Text = trailLen.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("gravity", out var gravity))
        {
            GravitySlider.Value = gravity;
            GravityValue.Text = gravity.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("drag", out var drag))
        {
            DragSlider.Value = drag;
            DragValue.Text = drag.ToString("F2");
        }

        if (_effect.Configuration.TryGet<float>("spreadAngle", out var spread))
        {
            SpreadAngleSlider.Value = spread;
            SpreadAngleValue.Text = spread.ToString("F0");
        }

        if (_effect.Configuration.TryGet<bool>("rainbowMode", out var rainbow))
            RainbowModeCheckBox.IsChecked = rainbow;

        if (_effect.Configuration.TryGet<float>("rainbowSpeed", out var rainbowSpeed))
        {
            RainbowSpeedSlider.Value = rainbowSpeed;
            RainbowSpeedValue.Text = rainbowSpeed.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("useRandomColors", out var randomColors))
            RandomColorsCheckBox.IsChecked = randomColors;

        if (_effect.Configuration.TryGet<Vector4>("primaryColor", out var primary))
        {
            _primaryColor = primary;
            UpdatePrimaryColorPreview();
        }

        if (_effect.Configuration.TryGet<Vector4>("secondaryColor", out var secondary))
        {
            _secondaryColor = secondary;
            UpdateSecondaryColorPreview();
        }

        if (_effect.Configuration.TryGet<bool>("enableSecondaryExplosion", out var secondaryExp))
            EnableSecondaryCheckBox.IsChecked = secondaryExp;

        if (_effect.Configuration.TryGet<float>("secondaryExplosionDelay", out var secondaryDelay))
        {
            SecondaryDelaySlider.Value = secondaryDelay;
            SecondaryDelayValue.Text = secondaryDelay.ToString("F1");
        }

        if (_effect.Configuration.TryGet<int>("secondaryParticleCount", out var secondaryCount))
        {
            SecondaryCountSlider.Value = secondaryCount;
            SecondaryCountValue.Text = secondaryCount.ToString();
        }

        if (_effect.Configuration.TryGet<float>("secondaryExplosionForce", out var secondaryForce))
        {
            SecondaryForceSlider.Value = secondaryForce;
            SecondaryForceValue.Text = secondaryForce.ToString("F0");
        }

        if (_effect.Configuration.TryGet<bool>("enableRocketMode", out var rocketMode))
            EnableRocketModeCheckBox.IsChecked = rocketMode;

        if (_effect.Configuration.TryGet<float>("rocketSpeed", out var rocketSpeed))
        {
            RocketSpeedSlider.Value = rocketSpeed;
            RocketSpeedValue.Text = rocketSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("rocketMinAltitude", out var minAlt))
        {
            RocketMinAltitudeSlider.Value = minAlt;
            RocketMinAltitudeValue.Text = $"{(int)(minAlt * 100)}%";
        }

        if (_effect.Configuration.TryGet<float>("rocketMaxAltitude", out var maxAlt))
        {
            RocketMaxAltitudeSlider.Value = maxAlt;
            RocketMaxAltitudeValue.Text = $"{(int)(maxAlt * 100)}%";
        }

        if (_effect.Configuration.TryGet<float>("rocketMaxFuseTime", out var maxFuse))
        {
            RocketMaxFuseTimeSlider.Value = maxFuse;
            RocketMaxFuseTimeValue.Text = maxFuse.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("rocketSize", out var rocketSize))
        {
            RocketSizeSlider.Value = rocketSize;
            RocketSizeValue.Text = rocketSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet<bool>("rocketRainbowMode", out var rocketRainbow))
            RocketRainbowModeCheckBox.IsChecked = rocketRainbow;

        if (_effect.Configuration.TryGet<float>("rocketRainbowSpeed", out var rocketRainbowSpeed))
        {
            RocketRainbowSpeedSlider.Value = rocketRainbowSpeed;
            RocketRainbowSpeedValue.Text = rocketRainbowSpeed.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("rocketUseRandomColors", out var rocketRandom))
            RocketRandomColorsCheckBox.IsChecked = rocketRandom;

        if (_effect.Configuration.TryGet<Vector4>("rocketPrimaryColor", out var rocketPrimary))
        {
            _rocketPrimaryColor = rocketPrimary;
            UpdateRocketPrimaryColorPreview();
        }

        if (_effect.Configuration.TryGet<Vector4>("rocketSecondaryColor", out var rocketSecondary))
        {
            _rocketSecondaryColor = rocketSecondary;
            UpdateRocketSecondaryColorPreview();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("maxParticles", (int)MaxParticlesSlider.Value);
        config.Set("maxFireworks", (int)MaxFireworksSlider.Value);
        config.Set("particleLifespan", (float)LifespanSlider.Value);
        config.Set("spawnOnLeftClick", LeftClickCheckBox.IsChecked ?? true);
        config.Set("spawnOnRightClick", RightClickCheckBox.IsChecked == true);
        config.Set("minParticlesPerFirework", (int)MinParticlesPerFireworkSlider.Value);
        config.Set("maxParticlesPerFirework", (int)MaxParticlesPerFireworkSlider.Value);
        config.Set("clickExplosionForce", (float)ClickForceSlider.Value);
        config.Set("spawnOnMove", SpawnOnMoveCheckBox.IsChecked == true);
        config.Set("moveSpawnDistance", (float)MoveSpawnDistanceSlider.Value);
        config.Set("moveExplosionForce", (float)MoveForceSlider.Value);
        config.Set("minParticleSize", (float)MinSizeSlider.Value);
        config.Set("maxParticleSize", (float)MaxSizeSlider.Value);
        config.Set("glowIntensity", (float)GlowSlider.Value);
        config.Set("enableTrails", EnableTrailsCheckBox.IsChecked ?? true);
        config.Set("trailLength", (float)TrailLengthSlider.Value);
        config.Set("gravity", (float)GravitySlider.Value);
        config.Set("drag", (float)DragSlider.Value);
        config.Set("spreadAngle", (float)SpreadAngleSlider.Value);
        config.Set("rainbowMode", RainbowModeCheckBox.IsChecked ?? true);
        config.Set("rainbowSpeed", (float)RainbowSpeedSlider.Value);
        config.Set("useRandomColors", RandomColorsCheckBox.IsChecked ?? true);
        config.Set("primaryColor", _primaryColor);
        config.Set("secondaryColor", _secondaryColor);
        config.Set("enableSecondaryExplosion", EnableSecondaryCheckBox.IsChecked ?? true);
        config.Set("secondaryExplosionDelay", (float)SecondaryDelaySlider.Value);
        config.Set("secondaryParticleCount", (int)SecondaryCountSlider.Value);
        config.Set("secondaryExplosionForce", (float)SecondaryForceSlider.Value);
        config.Set("enableRocketMode", EnableRocketModeCheckBox.IsChecked == true);
        config.Set("rocketSpeed", (float)RocketSpeedSlider.Value);
        config.Set("rocketMinAltitude", (float)RocketMinAltitudeSlider.Value);
        config.Set("rocketMaxAltitude", (float)RocketMaxAltitudeSlider.Value);
        config.Set("rocketMaxFuseTime", (float)RocketMaxFuseTimeSlider.Value);
        config.Set("rocketSize", (float)RocketSizeSlider.Value);
        config.Set("rocketRainbowMode", RocketRainbowModeCheckBox.IsChecked ?? true);
        config.Set("rocketRainbowSpeed", (float)RocketRainbowSpeedSlider.Value);
        config.Set("rocketUseRandomColors", RocketRandomColorsCheckBox.IsChecked ?? true);
        config.Set("rocketPrimaryColor", _rocketPrimaryColor);
        config.Set("rocketSecondaryColor", _rocketSecondaryColor);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }

    private void MaxParticlesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxParticlesValue != null)
            MaxParticlesValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void MaxFireworksSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxFireworksValue != null)
            MaxFireworksValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void LifespanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifespanValue != null)
            LifespanValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void LeftClickCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();
    private void RightClickCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void MinParticlesPerFireworkSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinParticlesPerFireworkValue != null)
            MinParticlesPerFireworkValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void MaxParticlesPerFireworkSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxParticlesPerFireworkValue != null)
            MaxParticlesPerFireworkValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void ClickForceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ClickForceValue != null)
            ClickForceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SpawnOnMoveCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void MoveSpawnDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveSpawnDistanceValue != null)
            MoveSpawnDistanceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MoveForceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveForceValue != null)
            MoveForceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinSizeValue != null)
            MinSizeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxSizeValue != null)
            MaxSizeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GlowSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowValue != null)
            GlowValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void EnableTrailsCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void TrailLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrailLengthValue != null)
            TrailLengthValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GravitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GravityValue != null)
            GravityValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void DragSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DragValue != null)
            DragValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void SpreadAngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpreadAngleValue != null)
            SpreadAngleValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RainbowModeCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RainbowSpeedValue != null)
            RainbowSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RandomColorsCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void PrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(255, (int)(_primaryColor.X * 255f), (int)(_primaryColor.Y * 255f), (int)(_primaryColor.Z * 255f)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _primaryColor = new Vector4(dialog.Color.R / 255f, dialog.Color.G / 255f, dialog.Color.B / 255f, 1f);
            UpdatePrimaryColorPreview();
            UpdateConfiguration();
        }
    }

    private void SecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(255, (int)(_secondaryColor.X * 255f), (int)(_secondaryColor.Y * 255f), (int)(_secondaryColor.Z * 255f)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _secondaryColor = new Vector4(dialog.Color.R / 255f, dialog.Color.G / 255f, dialog.Color.B / 255f, 1f);
            UpdateSecondaryColorPreview();
            UpdateConfiguration();
        }
    }

    private void UpdatePrimaryColorPreview()
    {
        PrimaryColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(255, (byte)(_primaryColor.X * 255f), (byte)(_primaryColor.Y * 255f), (byte)(_primaryColor.Z * 255f)));
    }

    private void UpdateSecondaryColorPreview()
    {
        SecondaryColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(255, (byte)(_secondaryColor.X * 255f), (byte)(_secondaryColor.Y * 255f), (byte)(_secondaryColor.Z * 255f)));
    }

    private void EnableSecondaryCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void SecondaryDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SecondaryDelayValue != null)
            SecondaryDelayValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void SecondaryCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SecondaryCountValue != null)
            SecondaryCountValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void SecondaryForceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SecondaryForceValue != null)
            SecondaryForceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void EnableRocketModeCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void RocketSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RocketSpeedValue != null)
            RocketSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RocketMinAltitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RocketMinAltitudeValue != null)
            RocketMinAltitudeValue.Text = $"{(int)(e.NewValue * 100)}%";
        UpdateConfiguration();
    }

    private void RocketMaxAltitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RocketMaxAltitudeValue != null)
            RocketMaxAltitudeValue.Text = $"{(int)(e.NewValue * 100)}%";
        UpdateConfiguration();
    }

    private void RocketMaxFuseTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RocketMaxFuseTimeValue != null)
            RocketMaxFuseTimeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RocketSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RocketSizeValue != null)
            RocketSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RocketRainbowModeCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void RocketRainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RocketRainbowSpeedValue != null)
            RocketRainbowSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RocketRandomColorsCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void RocketPrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(255, (int)(_rocketPrimaryColor.X * 255f), (int)(_rocketPrimaryColor.Y * 255f), (int)(_rocketPrimaryColor.Z * 255f)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _rocketPrimaryColor = new Vector4(dialog.Color.R / 255f, dialog.Color.G / 255f, dialog.Color.B / 255f, 1f);
            UpdateRocketPrimaryColorPreview();
            UpdateConfiguration();
        }
    }

    private void RocketSecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(255, (int)(_rocketSecondaryColor.X * 255f), (int)(_rocketSecondaryColor.Y * 255f), (int)(_rocketSecondaryColor.Z * 255f)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _rocketSecondaryColor = new Vector4(dialog.Color.R / 255f, dialog.Color.G / 255f, dialog.Color.B / 255f, 1f);
            UpdateRocketSecondaryColorPreview();
            UpdateConfiguration();
        }
    }

    private void UpdateRocketPrimaryColorPreview()
    {
        RocketPrimaryColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(255, (byte)(_rocketPrimaryColor.X * 255f), (byte)(_rocketPrimaryColor.Y * 255f), (byte)(_rocketPrimaryColor.Z * 255f)));
    }

    private void UpdateRocketSecondaryColorPreview()
    {
        RocketSecondaryColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(255, (byte)(_rocketSecondaryColor.X * 255f), (byte)(_rocketSecondaryColor.Y * 255f), (byte)(_rocketSecondaryColor.Z * 255f)));
    }
}
