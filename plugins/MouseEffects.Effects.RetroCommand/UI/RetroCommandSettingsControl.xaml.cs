using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.RetroCommand.UI;

public partial class RetroCommandSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private readonly DispatcherTimer _scoreTimer;
    private bool _isInitializing = true;

    private Vector4 _cityColor = new(0f, 0.8f, 1f, 1f);
    private Vector4 _baseColor = new(0f, 1f, 0.5f, 1f);
    private Vector4 _enemyMissileColor = new(1f, 0.2f, 0.2f, 1f);
    private Vector4 _explosionColor = new(1f, 0.8f, 0.2f, 1f);
    private Vector4 _scoreOverlayColor = new(0f, 1f, 0f, 1f);

    public event Action<string>? SettingsChanged;

    public RetroCommandSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;

        // Subscribe to high scores change event
        if (_effect is RetroCommandEffect retroEffect)
        {
            retroEffect.HighScoresChanged += OnHighScoresChanged;
        }

        // Set up score polling timer
        _scoreTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _scoreTimer.Tick += ScoreTimer_Tick;
        _scoreTimer.Start();

        Unloaded += (s, e) =>
        {
            _scoreTimer.Stop();
            if (_effect is RetroCommandEffect re)
            {
                re.HighScoresChanged -= OnHighScoresChanged;
            }
        };
    }

    private void OnHighScoresChanged(string highScoresJson)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var config = new EffectConfiguration();
            CopyCurrentSettingsToConfig(config);
            config.Set("rc_highScoresJson", highScoresJson);
            _effect.Configure(config);
            SettingsChanged?.Invoke(_effect.Metadata.Id);
        });
    }

    private void CopyCurrentSettingsToConfig(EffectConfiguration config)
    {
        // Render style
        config.Set("rc_renderStyle", RenderStyleComboBox.SelectedIndex);

        // Explosion mode
        config.Set("rc_explosionMode", ExplosionModeComboBox.SelectedIndex);
        config.Set("rc_counterMissileSpeed", (float)CounterMissileSpeedSlider.Value);

        // Fire rate mode
        config.Set("rc_fireRateMode", FireRateModeComboBox.SelectedIndex);
        config.Set("rc_maxActiveExplosions", (int)MaxActiveExplosionsSlider.Value);
        config.Set("rc_fireCooldown", (float)FireCooldownSlider.Value);

        // City settings
        config.Set("rc_citySize", (float)CitySizeSlider.Value);
        config.Set("rc_cityColor", _cityColor);

        // Base settings
        config.Set("rc_baseSize", (float)BaseSizeSlider.Value);
        config.Set("rc_baseColor", _baseColor);

        // Enemy missile settings
        config.Set("rc_enemyMissileSpeed", (float)EnemyMissileSpeedSlider.Value);
        config.Set("rc_enemyMissileSpeedIncrease", (float)EnemyMissileSpeedIncreaseSlider.Value);
        config.Set("rc_enemyMissilesPerWave", (int)EnemyMissilesPerWaveSlider.Value);
        config.Set("rc_enemyMissileSize", (float)EnemyMissileSizeSlider.Value);
        config.Set("rc_enemyMissileColor", _enemyMissileColor);

        // Explosion settings
        config.Set("rc_explosionMaxRadius", (float)ExplosionMaxRadiusSlider.Value);
        config.Set("rc_explosionExpandSpeed", (float)ExplosionExpandSpeedSlider.Value);
        config.Set("rc_explosionShrinkSpeed", (float)ExplosionShrinkSpeedSlider.Value);
        config.Set("rc_explosionDuration", (float)ExplosionDurationSlider.Value);
        config.Set("rc_explosionColor", _explosionColor);

        // Visual settings
        config.Set("rc_glowIntensity", (float)GlowSlider.Value);
        config.Set("rc_neonIntensity", (float)NeonSlider.Value);
        config.Set("rc_showTrails", EnableTrailsCheckBox.IsChecked ?? true);

        // Wave settings
        config.Set("rc_wavePauseDuration", (float)WavePauseDurationSlider.Value);

        // Scoring
        if (int.TryParse(ScoreMissileTextBox.Text, out int scoreMissile))
            config.Set("rc_scoreMissile", scoreMissile);
        if (int.TryParse(ScoreWaveBonusTextBox.Text, out int waveBonus))
            config.Set("rc_scoreWaveBonus", waveBonus);
        if (int.TryParse(ScoreCityBonusTextBox.Text, out int cityBonus))
            config.Set("rc_scoreCityBonus", cityBonus);

        // Score overlay
        config.Set("rc_showScoreOverlay", ShowScoreOverlayCheckBox.IsChecked ?? true);
        config.Set("rc_scoreOverlaySize", (float)ScoreOverlaySizeSlider.Value);
        config.Set("rc_scoreOverlaySpacing", (float)ScoreOverlaySpacingSlider.Value);
        config.Set("rc_scoreOverlayMargin", (float)ScoreOverlayMarginSlider.Value);
        config.Set("rc_scoreOverlayBgOpacity", (float)ScoreOverlayBgOpacitySlider.Value);
        config.Set("rc_scoreOverlayColor", _scoreOverlayColor);
        config.Set("rc_scoreOverlayX", (float)ScoreOverlayXSlider.Value);
        config.Set("rc_scoreOverlayY", (float)ScoreOverlayYSlider.Value);

        // Timer duration
        config.Set("rc_timerDuration", (float)TimerDurationSlider.Value);

        // Reset hotkey
        config.Set("rc_enableResetHotkey", EnableResetHotkeyCheckBox.IsChecked ?? false);

        // Preserve existing high scores
        if (_effect is RetroCommandEffect re)
        {
            config.Set("rc_highScoresJson", re.GetHighScoresJson());
        }
    }

    private void ScoreTimer_Tick(object? sender, EventArgs e)
    {
        if (_effect is RetroCommandEffect retroEffect)
        {
            // Update score
            ScoreDisplay.Text = retroEffect.CurrentScore.ToString("N0");

            // Update PPM
            PpmDisplay.Text = ((int)retroEffect.PointsPerMinute).ToString("N0");

            // Update timer countdown
            if (retroEffect.WaitingForFirstHit && retroEffect.IsGameActive)
            {
                TimerDisplay.Text = "READY";
            }
            else
            {
                float remaining = retroEffect.RemainingTime;
                int minutes = (int)remaining / 60;
                int seconds = (int)remaining % 60;
                TimerDisplay.Text = $"{minutes:D2}:{seconds:D2}";
            }

            // Change timer color to red when game over
            TimerDisplay.Foreground = new SolidColorBrush(
                retroEffect.IsGameOver
                    ? System.Windows.Media.Color.FromRgb(255, 80, 80)
                    : System.Windows.Media.Color.FromRgb(0, 204, 255));

            // Update timer duration display
            float duration = retroEffect.TimerDuration;
            int durMinutes = (int)duration / 60;
            int durSeconds = (int)duration % 60;
            TimerDurationDisplay.Text = $"{durMinutes:D2}:{durSeconds:D2}";

            // Update wave display
            WaveDisplay.Text = retroEffect.CurrentWave.ToString();

            // Update cities display
            int citiesRemaining = retroEffect.CitiesRemaining;
            CitiesDisplay.Text = citiesRemaining.ToString();
            CitiesDisplay.Foreground = new SolidColorBrush(
                citiesRemaining <= 2
                    ? System.Windows.Media.Color.FromRgb(255, 80, 80)
                    : System.Windows.Media.Color.FromRgb(102, 255, 102));
        }
    }

    private void LoadConfiguration()
    {
        // Render style
        if (_effect.Configuration.TryGet("rc_renderStyle", out int renderStyle))
            RenderStyleComboBox.SelectedIndex = renderStyle;

        // Explosion mode
        if (_effect.Configuration.TryGet("rc_explosionMode", out int expMode))
            ExplosionModeComboBox.SelectedIndex = expMode;
        if (_effect.Configuration.TryGet("rc_counterMissileSpeed", out float cmSpeed))
        {
            CounterMissileSpeedSlider.Value = cmSpeed;
            CounterMissileSpeedValue.Text = cmSpeed.ToString("F0");
        }
        UpdateExplosionModeVisibility();

        // Fire rate mode
        if (_effect.Configuration.TryGet("rc_fireRateMode", out int frMode))
            FireRateModeComboBox.SelectedIndex = frMode;
        if (_effect.Configuration.TryGet("rc_maxActiveExplosions", out int maxExp))
        {
            MaxActiveExplosionsSlider.Value = maxExp;
            MaxActiveExplosionsValue.Text = maxExp.ToString();
        }
        if (_effect.Configuration.TryGet("rc_fireCooldown", out float cooldown))
        {
            FireCooldownSlider.Value = cooldown;
            FireCooldownValue.Text = cooldown.ToString("F2");
        }
        UpdateFireRateModeVisibility();

        // City settings
        if (_effect.Configuration.TryGet("rc_citySize", out float citySize))
        {
            CitySizeSlider.Value = citySize;
            CitySizeValue.Text = citySize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_cityColor", out Vector4 cityCol))
        {
            _cityColor = cityCol;
            UpdateCityColorPreview();
        }

        // Base settings
        if (_effect.Configuration.TryGet("rc_baseSize", out float baseSize))
        {
            BaseSizeSlider.Value = baseSize;
            BaseSizeValue.Text = baseSize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_baseColor", out Vector4 baseCol))
        {
            _baseColor = baseCol;
            UpdateBaseColorPreview();
        }

        // Enemy missile settings
        if (_effect.Configuration.TryGet("rc_enemyMissileSpeed", out float emSpeed))
        {
            EnemyMissileSpeedSlider.Value = emSpeed;
            EnemyMissileSpeedValue.Text = emSpeed.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_enemyMissileSpeedIncrease", out float emInc))
        {
            EnemyMissileSpeedIncreaseSlider.Value = emInc;
            EnemyMissileSpeedIncreaseValue.Text = emInc.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_enemyMissilesPerWave", out int emWave))
        {
            EnemyMissilesPerWaveSlider.Value = emWave;
            EnemyMissilesPerWaveValue.Text = emWave.ToString();
        }
        if (_effect.Configuration.TryGet("rc_enemyMissileSize", out float emSize))
        {
            EnemyMissileSizeSlider.Value = emSize;
            EnemyMissileSizeValue.Text = emSize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_enemyMissileColor", out Vector4 emCol))
        {
            _enemyMissileColor = emCol;
            UpdateEnemyMissileColorPreview();
        }

        // Explosion settings
        if (_effect.Configuration.TryGet("rc_explosionMaxRadius", out float expRad))
        {
            ExplosionMaxRadiusSlider.Value = expRad;
            ExplosionMaxRadiusValue.Text = expRad.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_explosionExpandSpeed", out float expSpd))
        {
            ExplosionExpandSpeedSlider.Value = expSpd;
            ExplosionExpandSpeedValue.Text = expSpd.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_explosionShrinkSpeed", out float shrSpd))
        {
            ExplosionShrinkSpeedSlider.Value = shrSpd;
            ExplosionShrinkSpeedValue.Text = shrSpd.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_explosionDuration", out float expDur))
        {
            ExplosionDurationSlider.Value = expDur;
            ExplosionDurationValue.Text = expDur.ToString("F2");
        }
        if (_effect.Configuration.TryGet("rc_explosionColor", out Vector4 expCol))
        {
            _explosionColor = expCol;
            UpdateExplosionColorPreview();
        }

        // Visual settings
        if (_effect.Configuration.TryGet("rc_glowIntensity", out float glow))
        {
            GlowSlider.Value = glow;
            GlowValue.Text = glow.ToString("F1");
        }
        if (_effect.Configuration.TryGet("rc_neonIntensity", out float neon))
        {
            NeonSlider.Value = neon;
            NeonValue.Text = neon.ToString("F1");
        }
        if (_effect.Configuration.TryGet("rc_showTrails", out bool trails))
            EnableTrailsCheckBox.IsChecked = trails;

        // Wave settings
        if (_effect.Configuration.TryGet("rc_wavePauseDuration", out float wavePause))
        {
            WavePauseDurationSlider.Value = wavePause;
            WavePauseDurationValue.Text = wavePause.ToString("F1");
        }

        // Scoring
        if (_effect.Configuration.TryGet("rc_scoreMissile", out int scoreMissile))
            ScoreMissileTextBox.Text = scoreMissile.ToString();
        if (_effect.Configuration.TryGet("rc_scoreWaveBonus", out int waveBonus))
            ScoreWaveBonusTextBox.Text = waveBonus.ToString();
        if (_effect.Configuration.TryGet("rc_scoreCityBonus", out int cityBonus))
            ScoreCityBonusTextBox.Text = cityBonus.ToString();

        // Score overlay
        if (_effect.Configuration.TryGet("rc_showScoreOverlay", out bool showOverlay))
            ShowScoreOverlayCheckBox.IsChecked = showOverlay;
        if (_effect.Configuration.TryGet("rc_scoreOverlaySize", out float overlaySize))
        {
            ScoreOverlaySizeSlider.Value = overlaySize;
            ScoreOverlaySizeValue.Text = overlaySize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_scoreOverlaySpacing", out float overlaySpacing))
        {
            ScoreOverlaySpacingSlider.Value = overlaySpacing;
            ScoreOverlaySpacingValue.Text = overlaySpacing.ToString("F2");
        }
        if (_effect.Configuration.TryGet("rc_scoreOverlayMargin", out float overlayMargin))
        {
            ScoreOverlayMarginSlider.Value = overlayMargin;
            ScoreOverlayMarginValue.Text = overlayMargin.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_scoreOverlayBgOpacity", out float bgOpacity))
        {
            ScoreOverlayBgOpacitySlider.Value = bgOpacity;
            ScoreOverlayBgOpacityValue.Text = bgOpacity.ToString("F2");
        }
        if (_effect.Configuration.TryGet("rc_scoreOverlayColor", out Vector4 overlayColor))
        {
            _scoreOverlayColor = overlayColor;
            UpdateScoreOverlayColorPreview();
        }
        if (_effect.Configuration.TryGet("rc_scoreOverlayX", out float overlayX))
        {
            ScoreOverlayXSlider.Value = overlayX;
            ScoreOverlayXValue.Text = overlayX.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rc_scoreOverlayY", out float overlayY))
        {
            ScoreOverlayYSlider.Value = overlayY;
            ScoreOverlayYValue.Text = overlayY.ToString("F0");
        }

        // Timer duration
        if (_effect.Configuration.TryGet("rc_timerDuration", out float timerDur))
        {
            TimerDurationSlider.Value = timerDur;
            int minutes = (int)timerDur / 60;
            int seconds = (int)timerDur % 60;
            TimerDurationValue.Text = $"{minutes}:{seconds:D2}";
        }
        else if (_effect is RetroCommandEffect re)
        {
            float duration = re.TimerDuration;
            TimerDurationSlider.Value = duration;
            int minutes = (int)duration / 60;
            int seconds = (int)duration % 60;
            TimerDurationValue.Text = $"{minutes}:{seconds:D2}";
        }

        // Reset hotkey
        if (_effect.Configuration.TryGet("rc_enableResetHotkey", out bool resetHotkey))
            EnableResetHotkeyCheckBox.IsChecked = resetHotkey;
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        CopyCurrentSettingsToConfig(config);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateExplosionModeVisibility()
    {
        // Show counter-missile speed only when mode is Counter-Missile (1)
        CounterMissilePanel.Visibility = ExplosionModeComboBox.SelectedIndex == 1
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateFireRateModeVisibility()
    {
        int mode = FireRateModeComboBox.SelectedIndex;
        // 0=Unlimited, 1=MaxActive, 2=Cooldown
        MaxActivePanel.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        CooldownPanel.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateConfiguration();
    }

    private void ExplosionModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateExplosionModeVisibility();
        UpdateConfiguration();
    }

    private void FireRateModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateFireRateModeVisibility();
        UpdateConfiguration();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        // Update value displays
        if (CounterMissileSpeedValue != null)
            CounterMissileSpeedValue.Text = CounterMissileSpeedSlider.Value.ToString("F0");
        if (MaxActiveExplosionsValue != null)
            MaxActiveExplosionsValue.Text = ((int)MaxActiveExplosionsSlider.Value).ToString();
        if (FireCooldownValue != null)
            FireCooldownValue.Text = FireCooldownSlider.Value.ToString("F2");

        if (CitySizeValue != null)
            CitySizeValue.Text = CitySizeSlider.Value.ToString("F0");
        if (BaseSizeValue != null)
            BaseSizeValue.Text = BaseSizeSlider.Value.ToString("F0");

        if (EnemyMissileSpeedValue != null)
            EnemyMissileSpeedValue.Text = EnemyMissileSpeedSlider.Value.ToString("F0");
        if (EnemyMissileSpeedIncreaseValue != null)
            EnemyMissileSpeedIncreaseValue.Text = EnemyMissileSpeedIncreaseSlider.Value.ToString("F0");
        if (EnemyMissilesPerWaveValue != null)
            EnemyMissilesPerWaveValue.Text = ((int)EnemyMissilesPerWaveSlider.Value).ToString();
        if (EnemyMissileSizeValue != null)
            EnemyMissileSizeValue.Text = EnemyMissileSizeSlider.Value.ToString("F0");

        if (ExplosionMaxRadiusValue != null)
            ExplosionMaxRadiusValue.Text = ExplosionMaxRadiusSlider.Value.ToString("F0");
        if (ExplosionExpandSpeedValue != null)
            ExplosionExpandSpeedValue.Text = ExplosionExpandSpeedSlider.Value.ToString("F0");
        if (ExplosionShrinkSpeedValue != null)
            ExplosionShrinkSpeedValue.Text = ExplosionShrinkSpeedSlider.Value.ToString("F0");
        if (ExplosionDurationValue != null)
            ExplosionDurationValue.Text = ExplosionDurationSlider.Value.ToString("F2");

        if (GlowValue != null)
            GlowValue.Text = GlowSlider.Value.ToString("F1");
        if (NeonValue != null)
            NeonValue.Text = NeonSlider.Value.ToString("F1");

        if (WavePauseDurationValue != null)
            WavePauseDurationValue.Text = WavePauseDurationSlider.Value.ToString("F1");

        // Score overlay
        if (ScoreOverlaySizeValue != null)
            ScoreOverlaySizeValue.Text = ScoreOverlaySizeSlider.Value.ToString("F0");
        if (ScoreOverlaySpacingValue != null)
            ScoreOverlaySpacingValue.Text = ScoreOverlaySpacingSlider.Value.ToString("F2");
        if (ScoreOverlayMarginValue != null)
            ScoreOverlayMarginValue.Text = ScoreOverlayMarginSlider.Value.ToString("F0");
        if (ScoreOverlayBgOpacityValue != null)
            ScoreOverlayBgOpacityValue.Text = ScoreOverlayBgOpacitySlider.Value.ToString("F2");
        if (ScoreOverlayXValue != null)
            ScoreOverlayXValue.Text = ScoreOverlayXSlider.Value.ToString("F0");
        if (ScoreOverlayYValue != null)
            ScoreOverlayYValue.Text = ScoreOverlayYSlider.Value.ToString("F0");

        UpdateConfiguration();
    }

    private void ScoreTextBox_Changed(object sender, TextChangedEventArgs e) => UpdateConfiguration();

    private void PickColor(ref Vector4 colorRef, Action updatePreview)
    {
        using var colorDialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(colorRef.W * 255),
                (int)(colorRef.X * 255),
                (int)(colorRef.Y * 255),
                (int)(colorRef.Z * 255)),
            FullOpen = true
        };

        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = colorDialog.Color;
            colorRef = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, 1f);
            updatePreview();
            UpdateConfiguration();
        }
    }

    private void CityColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _cityColor, UpdateCityColorPreview);
    }

    private void BaseColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _baseColor, UpdateBaseColorPreview);
    }

    private void EnemyMissileColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _enemyMissileColor, UpdateEnemyMissileColorPreview);
    }

    private void ExplosionColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _explosionColor, UpdateExplosionColorPreview);
    }

    private void ScoreOverlayColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _scoreOverlayColor, UpdateScoreOverlayColorPreview);
    }

    private void UpdateCityColorPreview()
    {
        CityColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_cityColor.W * 255),
            (byte)(_cityColor.X * 255),
            (byte)(_cityColor.Y * 255),
            (byte)(_cityColor.Z * 255)));
    }

    private void UpdateBaseColorPreview()
    {
        BaseColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_baseColor.W * 255),
            (byte)(_baseColor.X * 255),
            (byte)(_baseColor.Y * 255),
            (byte)(_baseColor.Z * 255)));
    }

    private void UpdateEnemyMissileColorPreview()
    {
        EnemyMissileColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_enemyMissileColor.W * 255),
            (byte)(_enemyMissileColor.X * 255),
            (byte)(_enemyMissileColor.Y * 255),
            (byte)(_enemyMissileColor.Z * 255)));
    }

    private void UpdateExplosionColorPreview()
    {
        ExplosionColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_explosionColor.W * 255),
            (byte)(_explosionColor.X * 255),
            (byte)(_explosionColor.Y * 255),
            (byte)(_explosionColor.Z * 255)));
    }

    private void UpdateScoreOverlayColorPreview()
    {
        ScoreOverlayColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_scoreOverlayColor.W * 255),
            (byte)(_scoreOverlayColor.X * 255),
            (byte)(_scoreOverlayColor.Y * 255),
            (byte)(_scoreOverlayColor.Z * 255)));
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect is RetroCommandEffect retroEffect)
        {
            retroEffect.ResetGame();
        }
    }

    private void TimerDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        int totalSeconds = (int)e.NewValue;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (TimerDurationValue != null)
            TimerDurationValue.Text = $"{minutes}:{seconds:D2}";

        UpdateConfiguration();
    }
}
