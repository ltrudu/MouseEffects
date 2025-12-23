using System.Collections.Generic;
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
    private string _currentStyle = "Classic Burst";
    private string _currentLaunchStyle = "All Together";
    private Vector4 _primaryColor = new(1f, 0.3f, 0.1f, 1f);
    private Vector4 _secondaryColor = new(1f, 0.8f, 0.2f, 1f);
    private Vector4 _rocketPrimaryColor = new(1f, 0.8f, 0.2f, 1f);
    private Vector4 _rocketSecondaryColor = new(1f, 0.4f, 0.1f, 1f);

    private static readonly Dictionary<string, string> StyleDescriptions = new()
    {
        ["Classic Burst"] = "Traditional radial explosion with colorful particles and optional secondary bursts",
        ["Spinner"] = "Rotating mini-fireworks that spin and sparkle as they fall",
        ["Willow"] = "Graceful drooping trails like a weeping willow tree",
        ["Crackling"] = "Popping sparks that flash and crackle randomly",
        ["Chrysanthemum"] = "Dense star pattern where particles leave glowing trails like flower petals",
        ["Random"] = "Each explosion randomly selects from all available styles"
    };

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

        if (_effect.Configuration.TryGet<bool>("enableRandomExplosionSize", out var randomExpSize))
            EnableRandomExplosionSizeCheckBox.IsChecked = randomExpSize;
        if (_effect.Configuration.TryGet<float>("minExplosionSize", out var minExpSize))
        {
            MinExplosionSizeSlider.Value = minExpSize;
            MinExplosionSizeValue.Text = minExpSize.ToString("F1");
        }
        if (_effect.Configuration.TryGet<float>("maxExplosionSize", out var maxExpSize))
        {
            MaxExplosionSizeSlider.Value = maxExpSize;
            MaxExplosionSizeValue.Text = maxExpSize.ToString("F1");
        }
        UpdateRandomExplosionSizePanelVisibility();

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

        // Load firework style
        if (_effect.Configuration.TryGet<string>("fireworkStyle", out var style))
        {
            _currentStyle = style;
            for (int i = 0; i < FireworkStyleComboBox.Items.Count; i++)
            {
                if ((FireworkStyleComboBox.Items[i] as ComboBoxItem)?.Content?.ToString() == style)
                {
                    FireworkStyleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        // Load style-specific parameters
        if (_effect.Configuration.TryGet<float>("spinSpeed", out var spinSpd))
        {
            SpinSpeedSlider.Value = spinSpd;
            SpinSpeedValue.Text = spinSpd.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("spinRadius", out var spinRad))
        {
            SpinRadiusSlider.Value = spinRad;
            SpinRadiusValue.Text = spinRad.ToString("F0");
        }

        if (_effect.Configuration.TryGet<bool>("enableSparkTrails", out var sparkTrails))
            SparkTrailsCheckBox.IsChecked = sparkTrails;

        if (_effect.Configuration.TryGet<float>("droopIntensity", out var droopInt))
        {
            DroopIntensitySlider.Value = droopInt;
            DroopIntensityValue.Text = droopInt.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("branchDensity", out var branchDens))
        {
            BranchDensitySlider.Value = branchDens;
            BranchDensityValue.Text = branchDens.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("flashRate", out var flashRt))
        {
            FlashRateSlider.Value = flashRt;
            FlashRateValue.Text = flashRt.ToString("F0");
        }

        if (_effect.Configuration.TryGet<float>("popIntensity", out var popInt))
        {
            PopIntensitySlider.Value = popInt;
            PopIntensityValue.Text = popInt.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("particleMultiplier", out var partMult))
        {
            ParticleMultiplierSlider.Value = partMult;
            ParticleMultiplierValue.Text = partMult.ToString("F1");
        }

        if (_effect.Configuration.TryGet<int>("sparkDensity", out var sparkDens))
        {
            SparkDensitySlider.Value = sparkDens;
            SparkDensityValue.Text = sparkDens.ToString();
        }

        if (_effect.Configuration.TryGet<float>("trailPersistence", out var trailPers))
        {
            TrailPersistenceSlider.Value = trailPers;
            TrailPersistenceValue.Text = trailPers.ToString("F1");
        }

        if (_effect.Configuration.TryGet<int>("maxSparksPerParticle", out var maxSparks))
        {
            MaxSparksSlider.Value = maxSparks;
            MaxSparksValue.Text = maxSparks.ToString();
        }

        // Display particle count
        if (_effect.Configuration.TryGet<bool>("displayParticleCount", out var displayPart))
            DisplayParticleCountCheckBox.IsChecked = displayPart;
        if (_effect.Configuration.TryGet<bool>("displayStyle", out var displayStyle))
            DisplayStyleCheckBox.IsChecked = displayStyle;

        // Random Wave mode settings
        if (_effect.Configuration.TryGet<bool>("randomWaveMode", out var waveMode))
            RandomWaveModeCheckBox.IsChecked = waveMode;

        if (_effect.Configuration.TryGet<float>("waveDuration", out var waveDur))
        {
            WaveDurationSlider.Value = waveDur;
            WaveDurationValue.Text = waveDur.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("randomWaveDuration", out var randWaveDur))
            RandomWaveDurationCheckBox.IsChecked = randWaveDur;

        if (_effect.Configuration.TryGet<float>("waveDurationMin", out var waveDurMin))
        {
            WaveDurationMinSlider.Value = waveDurMin;
            WaveDurationMinValue.Text = waveDurMin.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("waveDurationMax", out var waveDurMax))
        {
            WaveDurationMaxSlider.Value = waveDurMax;
            WaveDurationMaxValue.Text = waveDurMax.ToString("F1");
        }

        // Automatic mode settings
        if (_effect.Configuration.TryGet<bool>("automaticMode", out var autoMode))
            AutomaticModeCheckBox.IsChecked = autoMode;

        if (_effect.Configuration.TryGet<int>("numberOfLaunchpads", out var numPads))
        {
            NumberOfLaunchpadsSlider.Value = numPads;
            NumberOfLaunchpadsValue.Text = numPads.ToString();
        }

        if (_effect.Configuration.TryGet<string>("launchStyle", out var launchStyle))
        {
            _currentLaunchStyle = launchStyle;
            for (int i = 0; i < LaunchStyleComboBox.Items.Count; i++)
            {
                if ((LaunchStyleComboBox.Items[i] as ComboBoxItem)?.Content?.ToString() == launchStyle)
                {
                    LaunchStyleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        if (_effect.Configuration.TryGet<float>("autoSpawnRate", out var spawnRate))
        {
            AutoSpawnRateSlider.Value = spawnRate;
            AutoSpawnRateValue.Text = spawnRate.ToString("F1");
        }

        if (_effect.Configuration.TryGet<float>("autoSpawnDelay", out var spawnDelay))
        {
            AutoSpawnDelaySlider.Value = spawnDelay;
            AutoSpawnDelayValue.Text = spawnDelay.ToString("F1");
        }

        if (_effect.Configuration.TryGet<bool>("randomLaunchAngle", out var randAngle))
            RandomLaunchAngleCheckBox.IsChecked = randAngle;

        if (_effect.Configuration.TryGet<float>("minLaunchAngle", out var minAngle))
        {
            MinLaunchAngleSlider.Value = minAngle;
            MinLaunchAngleValue.Text = $"{minAngle:F0}°";
        }

        if (_effect.Configuration.TryGet<float>("maxLaunchAngle", out var maxAngle))
        {
            MaxLaunchAngleSlider.Value = maxAngle;
            MaxLaunchAngleValue.Text = $"{maxAngle:F0}°";
        }

        UpdateStylePanelVisibility();
        UpdateWavePanelVisibility();
        UpdateAutomaticPanelVisibility();
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("displayParticleCount", DisplayParticleCountCheckBox.IsChecked == true);
        config.Set("displayStyle", DisplayStyleCheckBox.IsChecked == true);
        config.Set("maxParticles", (int)MaxParticlesSlider.Value);
        config.Set("maxFireworks", (int)MaxFireworksSlider.Value);
        config.Set("particleLifespan", (float)LifespanSlider.Value);
        config.Set("spawnOnLeftClick", LeftClickCheckBox.IsChecked ?? true);
        config.Set("spawnOnRightClick", RightClickCheckBox.IsChecked == true);
        config.Set("minParticlesPerFirework", (int)MinParticlesPerFireworkSlider.Value);
        config.Set("maxParticlesPerFirework", (int)MaxParticlesPerFireworkSlider.Value);
        config.Set("clickExplosionForce", (float)ClickForceSlider.Value);
        config.Set("enableRandomExplosionSize", EnableRandomExplosionSizeCheckBox.IsChecked == true);
        config.Set("minExplosionSize", (float)MinExplosionSizeSlider.Value);
        config.Set("maxExplosionSize", (float)MaxExplosionSizeSlider.Value);
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

        // Style configuration
        config.Set("fireworkStyle", _currentStyle);

        // Style-specific parameters
        config.Set("spinSpeed", (float)SpinSpeedSlider.Value);
        config.Set("spinRadius", (float)SpinRadiusSlider.Value);
        config.Set("enableSparkTrails", SparkTrailsCheckBox.IsChecked == true);
        config.Set("droopIntensity", (float)DroopIntensitySlider.Value);
        config.Set("branchDensity", (float)BranchDensitySlider.Value);
        config.Set("flashRate", (float)FlashRateSlider.Value);
        config.Set("popIntensity", (float)PopIntensitySlider.Value);
        config.Set("particleMultiplier", (float)ParticleMultiplierSlider.Value);
        config.Set("sparkDensity", (int)SparkDensitySlider.Value);
        config.Set("trailPersistence", (float)TrailPersistenceSlider.Value);
        config.Set("maxSparksPerParticle", (int)MaxSparksSlider.Value);

        // Random Wave mode settings
        config.Set("randomWaveMode", RandomWaveModeCheckBox.IsChecked == true);
        config.Set("waveDuration", (float)WaveDurationSlider.Value);
        config.Set("randomWaveDuration", RandomWaveDurationCheckBox.IsChecked == true);
        config.Set("waveDurationMin", (float)WaveDurationMinSlider.Value);
        config.Set("waveDurationMax", (float)WaveDurationMaxSlider.Value);

        // Automatic mode settings
        config.Set("automaticMode", AutomaticModeCheckBox.IsChecked == true);
        config.Set("numberOfLaunchpads", (int)NumberOfLaunchpadsSlider.Value);
        config.Set("launchStyle", _currentLaunchStyle);
        config.Set("autoSpawnRate", (float)AutoSpawnRateSlider.Value);
        config.Set("autoSpawnDelay", (float)AutoSpawnDelaySlider.Value);
        config.Set("randomLaunchAngle", RandomLaunchAngleCheckBox.IsChecked == true);
        config.Set("minLaunchAngle", (float)MinLaunchAngleSlider.Value);
        config.Set("maxLaunchAngle", (float)MaxLaunchAngleSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
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
    private void DisplayParticleCountCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();
    private void DisplayStyleCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void MinParticlesPerFireworkSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const int minGap = 10;
        var minVal = (int)e.NewValue;
        var maxVal = (int)MaxParticlesPerFireworkSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 1)
        {
            var newMax = Math.Min(minVal + minGap, (int)MaxParticlesPerFireworkSlider.Maximum);
            MaxParticlesPerFireworkSlider.Value = newMax;
            MaxParticlesPerFireworkValue.Text = newMax.ToString();
        }

        if (MinParticlesPerFireworkValue != null)
            MinParticlesPerFireworkValue.Text = minVal.ToString();
        UpdateConfiguration();
    }

    private void MaxParticlesPerFireworkSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const int minGap = 10;
        var maxVal = (int)e.NewValue;
        var minVal = (int)MinParticlesPerFireworkSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 1)
        {
            var newMin = Math.Max(maxVal - minGap, (int)MinParticlesPerFireworkSlider.Minimum);
            MinParticlesPerFireworkSlider.Value = newMin;
            MinParticlesPerFireworkValue.Text = newMin.ToString();
        }

        if (MaxParticlesPerFireworkValue != null)
            MaxParticlesPerFireworkValue.Text = maxVal.ToString();
        UpdateConfiguration();
    }

    private void ClickForceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ClickForceValue != null)
            ClickForceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void EnableRandomExplosionSizeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateRandomExplosionSizePanelVisibility();
        UpdateConfiguration();
    }

    private void UpdateRandomExplosionSizePanelVisibility()
    {
        if (RandomExplosionSizePanel != null)
            RandomExplosionSizePanel.Visibility = EnableRandomExplosionSizeCheckBox?.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MinExplosionSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinExplosionSizeValue != null)
            MinExplosionSizeValue.Text = e.NewValue.ToString("F1");
        if (MaxExplosionSizeSlider != null && e.NewValue >= MaxExplosionSizeSlider.Value)
            MaxExplosionSizeSlider.Value = e.NewValue + 0.1;
        UpdateConfiguration();
    }

    private void MaxExplosionSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxExplosionSizeValue != null)
            MaxExplosionSizeValue.Text = e.NewValue.ToString("F1");
        if (MinExplosionSizeSlider != null && e.NewValue <= MinExplosionSizeSlider.Value)
            MinExplosionSizeSlider.Value = e.NewValue - 0.1;
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
        if (_isInitializing) return;

        const double minGap = 1.0;
        var minVal = e.NewValue;
        var maxVal = MaxSizeSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 0.1)
        {
            var newMax = Math.Min(minVal + minGap, MaxSizeSlider.Maximum);
            MaxSizeSlider.Value = newMax;
            MaxSizeValue.Text = newMax.ToString("F1");
        }

        if (MinSizeValue != null)
            MinSizeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 1.0;
        var maxVal = e.NewValue;
        var minVal = MinSizeSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 0.1)
        {
            var newMin = Math.Max(maxVal - minGap, MinSizeSlider.Minimum);
            MinSizeSlider.Value = newMin;
            MinSizeValue.Text = newMin.ToString("F1");
        }

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
        if (_isInitializing) return;

        const double minGap = 0.05; // 5% minimum gap
        var minVal = e.NewValue;
        var maxVal = RocketMaxAltitudeSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 0.01)
        {
            var newMax = Math.Min(minVal + minGap, RocketMaxAltitudeSlider.Maximum);
            RocketMaxAltitudeSlider.Value = newMax;
            RocketMaxAltitudeValue.Text = $"{(int)(newMax * 100)}%";
        }

        if (RocketMinAltitudeValue != null)
            RocketMinAltitudeValue.Text = $"{(int)(e.NewValue * 100)}%";
        UpdateConfiguration();
    }

    private void RocketMaxAltitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 0.05; // 5% minimum gap
        var maxVal = e.NewValue;
        var minVal = RocketMinAltitudeSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 0.01)
        {
            var newMin = Math.Max(maxVal - minGap, RocketMinAltitudeSlider.Minimum);
            RocketMinAltitudeSlider.Value = newMin;
            RocketMinAltitudeValue.Text = $"{(int)(newMin * 100)}%";
        }

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

    private void FireworkStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || FireworkStyleComboBox.SelectedItem == null) return;

        var selectedItem = FireworkStyleComboBox.SelectedItem as ComboBoxItem;
        _currentStyle = selectedItem?.Content?.ToString() ?? "Classic Burst";

        UpdateStylePanelVisibility();
        UpdateStyleDescription();
        UpdateConfiguration();
    }

    private void UpdateStylePanelVisibility()
    {
        SpinnerSettingsPanel.Visibility = _currentStyle == "Spinner" ? Visibility.Visible : Visibility.Collapsed;
        WillowSettingsPanel.Visibility = _currentStyle == "Willow" ? Visibility.Visible : Visibility.Collapsed;
        CracklingSettingsPanel.Visibility = _currentStyle == "Crackling" ? Visibility.Visible : Visibility.Collapsed;
        ChrysanthemumSettingsPanel.Visibility = _currentStyle == "Chrysanthemum" ? Visibility.Visible : Visibility.Collapsed;
        RandomSettingsPanel.Visibility = _currentStyle == "Random" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateStyleDescription()
    {
        if (StyleDescriptionText != null && StyleDescriptions.TryGetValue(_currentStyle, out var desc))
            StyleDescriptionText.Text = desc;
    }

    // Spinner handlers
    private void SpinSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpinSpeedValue != null) SpinSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void SpinRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpinRadiusValue != null) SpinRadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SparkTrailsCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    // Willow handlers
    private void DroopIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DroopIntensityValue != null) DroopIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void BranchDensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BranchDensityValue != null) BranchDensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    // Crackling handlers
    private void FlashRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FlashRateValue != null) FlashRateValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void PopIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PopIntensityValue != null) PopIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ParticleMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ParticleMultiplierValue != null) ParticleMultiplierValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    // Chrysanthemum handlers
    private void SparkDensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SparkDensityValue != null) SparkDensityValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void TrailPersistenceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrailPersistenceValue != null) TrailPersistenceValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MaxSparksSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxSparksValue != null) MaxSparksValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    // Random Wave Mode handlers
    private void RandomWaveModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateWavePanelVisibility();
        UpdateConfiguration();
    }

    private void RandomWaveDurationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateWaveDurationPanelVisibility();
        UpdateConfiguration();
    }

    private void WaveDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WaveDurationValue != null) WaveDurationValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void WaveDurationMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WaveDurationMinValue != null) WaveDurationMinValue.Text = e.NewValue.ToString("F1");

        // Ensure min < max
        if (WaveDurationMaxSlider != null && e.NewValue >= WaveDurationMaxSlider.Value)
            WaveDurationMaxSlider.Value = e.NewValue + 1;

        UpdateConfiguration();
    }

    private void WaveDurationMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WaveDurationMaxValue != null) WaveDurationMaxValue.Text = e.NewValue.ToString("F1");

        // Ensure max > min
        if (WaveDurationMinSlider != null && e.NewValue <= WaveDurationMinSlider.Value)
            WaveDurationMinSlider.Value = e.NewValue - 1;

        UpdateConfiguration();
    }

    private void UpdateWavePanelVisibility()
    {
        bool waveEnabled = RandomWaveModeCheckBox?.IsChecked == true;
        if (WaveSettingsPanel != null)
            WaveSettingsPanel.Visibility = waveEnabled ? Visibility.Visible : Visibility.Collapsed;

        if (waveEnabled)
            UpdateWaveDurationPanelVisibility();
    }

    private void UpdateWaveDurationPanelVisibility()
    {
        bool randomDuration = RandomWaveDurationCheckBox?.IsChecked == true;
        if (FixedDurationPanel != null)
            FixedDurationPanel.Visibility = randomDuration ? Visibility.Collapsed : Visibility.Visible;
        if (RandomDurationPanel != null)
            RandomDurationPanel.Visibility = randomDuration ? Visibility.Visible : Visibility.Collapsed;
    }

    // Automatic Mode handlers
    private void AutomaticModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateAutomaticPanelVisibility();
        UpdateConfiguration();
    }

    private void UpdateAutomaticPanelVisibility()
    {
        bool autoEnabled = AutomaticModeCheckBox?.IsChecked == true;
        if (AutomaticSettingsPanel != null)
            AutomaticSettingsPanel.Visibility = autoEnabled ? Visibility.Visible : Visibility.Collapsed;

        if (autoEnabled)
            UpdateLaunchAnglePanelVisibility();
    }

    private void UpdateLaunchAnglePanelVisibility()
    {
        bool randomAngle = RandomLaunchAngleCheckBox?.IsChecked == true;
        if (LaunchAnglePanel != null)
            LaunchAnglePanel.Visibility = randomAngle ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NumberOfLaunchpadsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (NumberOfLaunchpadsValue != null)
            NumberOfLaunchpadsValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void LaunchStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || LaunchStyleComboBox.SelectedItem == null) return;

        var selectedItem = LaunchStyleComboBox.SelectedItem as ComboBoxItem;
        _currentLaunchStyle = selectedItem?.Content?.ToString() ?? "All Together";
        UpdateConfiguration();
    }

    private void AutoSpawnRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AutoSpawnRateValue != null)
            AutoSpawnRateValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void AutoSpawnDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AutoSpawnDelayValue != null)
            AutoSpawnDelayValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RandomLaunchAngleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateLaunchAnglePanelVisibility();
        UpdateConfiguration();
    }

    private void MinLaunchAngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        if (MinLaunchAngleValue != null)
            MinLaunchAngleValue.Text = $"{e.NewValue:F0}°";

        // Ensure min < max
        if (MaxLaunchAngleSlider != null && e.NewValue >= MaxLaunchAngleSlider.Value)
            MaxLaunchAngleSlider.Value = e.NewValue + 5;

        UpdateConfiguration();
    }

    private void MaxLaunchAngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        if (MaxLaunchAngleValue != null)
            MaxLaunchAngleValue.Text = $"{e.NewValue:F0}°";

        // Ensure max > min
        if (MinLaunchAngleSlider != null && e.NewValue <= MinLaunchAngleSlider.Value)
            MinLaunchAngleSlider.Value = e.NewValue - 5;

        UpdateConfiguration();
    }
}
