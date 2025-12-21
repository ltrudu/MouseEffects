using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Retropede.UI;

public partial class RetropedeSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private readonly DispatcherTimer _scoreTimer;
    private bool _isInitializing = true;

    private Vector4 _retropedeHeadColor = new(1f, 0.2f, 0.4f, 1f);
    private Vector4 _retropedeBodyColor = new(1f, 0.4f, 0.6f, 1f);
    private Vector4 _mushroomColor = new(0.4f, 1f, 0.4f, 1f);
    private Vector4 _spiderColor = new(1f, 1f, 0.2f, 1f);
    private Vector4 _ddtBombColor = new(1f, 0f, 1f, 1f);
    private Vector4 _ddtGasColor = new(0f, 1f, 0f, 1f);

    public event Action<string>? SettingsChanged;

    public RetropedeSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;

        // Subscribe to high scores change event
        if (_effect is RetropedeEffect retropedeEffect)
        {
            retropedeEffect.HighScoresChanged += OnHighScoresChanged;
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
            if (_effect is RetropedeEffect me)
            {
                me.HighScoresChanged -= OnHighScoresChanged;
            }
        };
    }

    private void OnHighScoresChanged(string highScoresJson)
    {
        // Save high scores to configuration
        Dispatcher.BeginInvoke(() =>
        {
            var config = new EffectConfiguration();

            // Copy all current settings
            CopyCurrentSettingsToConfig(config);

            // Update high scores
            config.Set("highScoresJson", highScoresJson);

            _effect.Configure(config);
            SettingsChanged?.Invoke(_effect.Metadata.Id);
        });
    }

    private void CopyCurrentSettingsToConfig(EffectConfiguration config)
    {
        // Render style
        config.Set("mp_renderStyle", RenderStyleCombo.SelectedIndex);

        // Cannon settings
        config.Set("mp_spawnOnLeftClick", FireOnLeftClickCheckBox.IsChecked ?? true);
        config.Set("mp_spawnOnMove", FireOnMoveCheckBox.IsChecked ?? false);
        config.Set("mp_moveFireThreshold", (float)MoveFireThresholdSlider.Value);
        config.Set("mp_laserSpeed", (float)LaserSpeedSlider.Value);
        config.Set("mp_cannonSize", (float)CannonSizeSlider.Value);
        config.Set("mp_playerZoneHeight", (float)PlayerZoneHeightSlider.Value);

        // Retropede settings
        config.Set("mp_baseSpeed", (float)BaseSpeedSlider.Value);
        config.Set("mp_startingSegments", (int)StartingSegmentsSlider.Value);
        config.Set("mp_segmentSize", (float)SegmentSizeSlider.Value);
        config.Set("mp_retropedeHeadColor", _retropedeHeadColor);
        config.Set("mp_retropedeBodyColor", _retropedeBodyColor);

        // Mushroom settings
        config.Set("mp_mushroomSize", (float)MushroomSizeSlider.Value);
        config.Set("mp_mushroomHealth", (int)MushroomHealthSlider.Value);
        config.Set("mp_initialMushroomCount", (int)InitialMushroomCountSlider.Value);
        config.Set("mp_mushroomColor", _mushroomColor);

        // Spider settings
        config.Set("mp_spiderEnabled", SpiderEnabledCheckBox.IsChecked ?? true);
        config.Set("mp_spiderSpawnRate", (float)SpiderSpawnRateSlider.Value);
        config.Set("mp_spiderSpeed", (float)SpiderSpeedSlider.Value);
        config.Set("mp_spiderColor", _spiderColor);

        // DDT settings
        config.Set("mp_ddtEnabled", DdtEnabledCheckBox.IsChecked ?? true);
        config.Set("mp_ddtMaxOnField", (int)DdtMaxOnFieldSlider.Value);
        config.Set("mp_ddtExplosionRadius", (float)DdtExplosionRadiusSlider.Value);
        config.Set("mp_ddtExplosionDuration", (float)DdtExplosionDurationSlider.Value);
        config.Set("mp_ddtBombColor", _ddtBombColor);
        config.Set("mp_ddtGasColor", _ddtGasColor);

        // Visual settings
        config.Set("mp_glowIntensity", (float)GlowSlider.Value);
        config.Set("mp_neonIntensity", (float)NeonSlider.Value);
        config.Set("mp_retroScanlines", (float)ScanlinesSlider.Value);
        config.Set("mp_animSpeed", (float)AnimSpeedSlider.Value);

        // Scoring
        if (int.TryParse(ScoreHeadTextBox.Text, out int scoreHead))
            config.Set("mp_scoreHead", scoreHead);
        if (int.TryParse(ScoreBodyTextBox.Text, out int scoreBody))
            config.Set("mp_scoreBody", scoreBody);
        if (int.TryParse(ScoreMushroomTextBox.Text, out int scoreMushroom))
            config.Set("mp_scoreMushroom", scoreMushroom);
        if (int.TryParse(ScoreSpiderTextBox.Text, out int scoreSpider))
            config.Set("mp_scoreSpider", scoreSpider);

        // Timer duration
        config.Set("mp_timerDuration", (float)TimerDurationSlider.Value);

        // Reset hotkey
        config.Set("mp_enableResetHotkey", EnableResetHotkeyCheckBox.IsChecked ?? false);

        // Preserve existing high scores
        if (_effect is RetropedeEffect me)
        {
            config.Set("highScoresJson", me.GetHighScoresJson());
        }
    }

    private void ScoreTimer_Tick(object? sender, EventArgs e)
    {
        if (_effect is RetropedeEffect retropedeEffect)
        {
            // Update score
            ScoreDisplay.Text = retropedeEffect.CurrentScore.ToString("N0");

            // Update PPM
            PpmDisplay.Text = ((int)retropedeEffect.PointsPerMinute).ToString("N0");

            // Update timer countdown (show "READY" when waiting for first hit)
            if (retropedeEffect.WaitingForFirstHit && retropedeEffect.IsGameActive)
            {
                TimerDisplay.Text = "READY";
            }
            else
            {
                float remaining = retropedeEffect.RemainingTime;
                int minutes = (int)remaining / 60;
                int seconds = (int)remaining % 60;
                TimerDisplay.Text = $"{minutes:D2}:{seconds:D2}";
            }

            // Change timer color to red when game over
            TimerDisplay.Foreground = new SolidColorBrush(
                retropedeEffect.IsGameOver
                    ? System.Windows.Media.Color.FromRgb(255, 80, 80)
                    : System.Windows.Media.Color.FromRgb(0, 204, 255));

            // Update wave display
            WaveDisplay.Text = retropedeEffect.CurrentWave.ToString();
        }
    }

    private void LoadConfiguration()
    {
        // Render style
        if (_effect.Configuration.TryGet("mp_renderStyle", out int renderStyle))
            RenderStyleCombo.SelectedIndex = renderStyle;

        // Cannon settings
        if (_effect.Configuration.TryGet("mp_spawnOnLeftClick", out bool leftClick))
            FireOnLeftClickCheckBox.IsChecked = leftClick;
        if (_effect.Configuration.TryGet("mp_spawnOnMove", out bool spawnMove))
            FireOnMoveCheckBox.IsChecked = spawnMove;
        if (_effect.Configuration.TryGet("mp_moveFireThreshold", out float moveThreshold))
        {
            MoveFireThresholdSlider.Value = moveThreshold;
            MoveFireThresholdValue.Text = moveThreshold.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_laserSpeed", out float laserSpeed))
        {
            LaserSpeedSlider.Value = laserSpeed;
            LaserSpeedValue.Text = laserSpeed.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_cannonSize", out float cannonSize))
        {
            CannonSizeSlider.Value = cannonSize;
            CannonSizeValue.Text = cannonSize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_playerZoneHeight", out float playerZone))
        {
            PlayerZoneHeightSlider.Value = playerZone;
            PlayerZoneHeightValue.Text = playerZone.ToString("F0");
        }

        // Retropede settings
        if (_effect.Configuration.TryGet("mp_baseSpeed", out float baseSpeed))
        {
            BaseSpeedSlider.Value = baseSpeed;
            BaseSpeedValue.Text = baseSpeed.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_startingSegments", out int segments))
        {
            StartingSegmentsSlider.Value = segments;
            StartingSegmentsValue.Text = segments.ToString();
        }
        if (_effect.Configuration.TryGet("mp_segmentSize", out float segmentSize))
        {
            SegmentSizeSlider.Value = segmentSize;
            SegmentSizeValue.Text = segmentSize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_retropedeHeadColor", out Vector4 headColor))
        {
            _retropedeHeadColor = headColor;
            UpdateRetropedeHeadColorPreview();
        }
        if (_effect.Configuration.TryGet("mp_retropedeBodyColor", out Vector4 bodyColor))
        {
            _retropedeBodyColor = bodyColor;
            UpdateRetropedeBodyColorPreview();
        }

        // Mushroom settings
        if (_effect.Configuration.TryGet("mp_mushroomSize", out float mushroomSize))
        {
            MushroomSizeSlider.Value = mushroomSize;
            MushroomSizeValue.Text = mushroomSize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_mushroomHealth", out int mushroomHealth))
        {
            MushroomHealthSlider.Value = mushroomHealth;
            MushroomHealthValue.Text = mushroomHealth.ToString();
        }
        if (_effect.Configuration.TryGet("mp_initialMushroomCount", out int mushroomCount))
        {
            InitialMushroomCountSlider.Value = mushroomCount;
            InitialMushroomCountValue.Text = mushroomCount.ToString();
        }
        if (_effect.Configuration.TryGet("mp_mushroomColor", out Vector4 mushroomColor))
        {
            _mushroomColor = mushroomColor;
            UpdateMushroomColorPreview();
        }

        // Spider settings
        if (_effect.Configuration.TryGet("mp_spiderEnabled", out bool spiderEnabled))
            SpiderEnabledCheckBox.IsChecked = spiderEnabled;
        if (_effect.Configuration.TryGet("mp_spiderSpawnRate", out float spiderSpawnRate))
        {
            SpiderSpawnRateSlider.Value = spiderSpawnRate;
            SpiderSpawnRateValue.Text = spiderSpawnRate.ToString("F1");
        }
        if (_effect.Configuration.TryGet("mp_spiderSpeed", out float spiderSpeed))
        {
            SpiderSpeedSlider.Value = spiderSpeed;
            SpiderSpeedValue.Text = spiderSpeed.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_spiderColor", out Vector4 spiderColor))
        {
            _spiderColor = spiderColor;
            UpdateSpiderColorPreview();
        }

        // DDT settings
        if (_effect.Configuration.TryGet("mp_ddtEnabled", out bool ddtEnabled))
            DdtEnabledCheckBox.IsChecked = ddtEnabled;
        if (_effect.Configuration.TryGet("mp_ddtMaxOnField", out int ddtMax))
        {
            DdtMaxOnFieldSlider.Value = ddtMax;
            DdtMaxOnFieldValue.Text = ddtMax.ToString();
        }
        if (_effect.Configuration.TryGet("mp_ddtExplosionRadius", out float ddtRadius))
        {
            DdtExplosionRadiusSlider.Value = ddtRadius;
            DdtExplosionRadiusValue.Text = ddtRadius.ToString("F0");
        }
        if (_effect.Configuration.TryGet("mp_ddtExplosionDuration", out float ddtDuration))
        {
            DdtExplosionDurationSlider.Value = ddtDuration;
            DdtExplosionDurationValue.Text = ddtDuration.ToString("F1");
        }
        if (_effect.Configuration.TryGet("mp_ddtBombColor", out Vector4 ddtBombColor))
        {
            _ddtBombColor = ddtBombColor;
            UpdateDdtBombColorPreview();
        }
        if (_effect.Configuration.TryGet("mp_ddtGasColor", out Vector4 ddtGasColor))
        {
            _ddtGasColor = ddtGasColor;
            UpdateDdtGasColorPreview();
        }

        // Visual settings
        if (_effect.Configuration.TryGet("mp_glowIntensity", out float glow))
        {
            GlowSlider.Value = glow;
            GlowValue.Text = glow.ToString("F1");
        }
        if (_effect.Configuration.TryGet("mp_neonIntensity", out float neon))
        {
            NeonSlider.Value = neon;
            NeonValue.Text = neon.ToString("F1");
        }
        if (_effect.Configuration.TryGet("mp_retroScanlines", out float scanlines))
        {
            ScanlinesSlider.Value = scanlines;
            ScanlinesValue.Text = scanlines.ToString("F1");
        }
        if (_effect.Configuration.TryGet("mp_animSpeed", out float anim))
        {
            AnimSpeedSlider.Value = anim;
            AnimSpeedValue.Text = anim.ToString("F1");
        }

        // Scoring
        if (_effect.Configuration.TryGet("mp_scoreHead", out int scoreHead))
            ScoreHeadTextBox.Text = scoreHead.ToString();
        if (_effect.Configuration.TryGet("mp_scoreBody", out int scoreBody))
            ScoreBodyTextBox.Text = scoreBody.ToString();
        if (_effect.Configuration.TryGet("mp_scoreMushroom", out int scoreMushroom))
            ScoreMushroomTextBox.Text = scoreMushroom.ToString();
        if (_effect.Configuration.TryGet("mp_scoreSpider", out int scoreSpider))
            ScoreSpiderTextBox.Text = scoreSpider.ToString();

        // Timer duration - fallback to effect's current value if not in config
        if (_effect.Configuration.TryGet("mp_timerDuration", out float timerDur))
        {
            TimerDurationSlider.Value = timerDur;
            int minutes = (int)timerDur / 60;
            int seconds = (int)timerDur % 60;
            TimerDurationValue.Text = $"{minutes}:{seconds:D2}";
        }
        else if (_effect is RetropedeEffect me)
        {
            float duration = me.TimerDuration;
            TimerDurationSlider.Value = duration;
            int minutes = (int)duration / 60;
            int seconds = (int)duration % 60;
            TimerDurationValue.Text = $"{minutes}:{seconds:D2}";
        }

        // Reset hotkey
        if (_effect.Configuration.TryGet("mp_enableResetHotkey", out bool resetHotkey))
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

    private void CheckBox_Changed(object sender, RoutedEventArgs e) => UpdateConfiguration();

    private void RenderStyleCombo_Changed(object sender, SelectionChangedEventArgs e) => UpdateConfiguration();

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        // Update value displays
        if (MoveFireThresholdValue != null)
            MoveFireThresholdValue.Text = MoveFireThresholdSlider.Value.ToString("F0");
        if (LaserSpeedValue != null)
            LaserSpeedValue.Text = LaserSpeedSlider.Value.ToString("F0");
        if (CannonSizeValue != null)
            CannonSizeValue.Text = CannonSizeSlider.Value.ToString("F0");
        if (PlayerZoneHeightValue != null)
            PlayerZoneHeightValue.Text = PlayerZoneHeightSlider.Value.ToString("F0");

        // Retropede
        if (BaseSpeedValue != null)
            BaseSpeedValue.Text = BaseSpeedSlider.Value.ToString("F0");
        if (StartingSegmentsValue != null)
            StartingSegmentsValue.Text = ((int)StartingSegmentsSlider.Value).ToString();
        if (SegmentSizeValue != null)
            SegmentSizeValue.Text = SegmentSizeSlider.Value.ToString("F0");

        // Mushroom
        if (MushroomSizeValue != null)
            MushroomSizeValue.Text = MushroomSizeSlider.Value.ToString("F0");
        if (MushroomHealthValue != null)
            MushroomHealthValue.Text = ((int)MushroomHealthSlider.Value).ToString();
        if (InitialMushroomCountValue != null)
            InitialMushroomCountValue.Text = ((int)InitialMushroomCountSlider.Value).ToString();

        // Spider
        if (SpiderSpawnRateValue != null)
            SpiderSpawnRateValue.Text = SpiderSpawnRateSlider.Value.ToString("F1");
        if (SpiderSpeedValue != null)
            SpiderSpeedValue.Text = SpiderSpeedSlider.Value.ToString("F0");

        // DDT
        if (DdtMaxOnFieldValue != null)
            DdtMaxOnFieldValue.Text = ((int)DdtMaxOnFieldSlider.Value).ToString();
        if (DdtExplosionRadiusValue != null)
            DdtExplosionRadiusValue.Text = DdtExplosionRadiusSlider.Value.ToString("F0");
        if (DdtExplosionDurationValue != null)
            DdtExplosionDurationValue.Text = DdtExplosionDurationSlider.Value.ToString("F1");

        // Visual
        if (GlowValue != null)
            GlowValue.Text = GlowSlider.Value.ToString("F1");
        if (NeonValue != null)
            NeonValue.Text = NeonSlider.Value.ToString("F1");
        if (ScanlinesValue != null)
            ScanlinesValue.Text = ScanlinesSlider.Value.ToString("F1");
        if (AnimSpeedValue != null)
            AnimSpeedValue.Text = AnimSpeedSlider.Value.ToString("F1");

        UpdateConfiguration();
    }

    private void ScoreTextBox_Changed(object sender, TextChangedEventArgs e) => UpdateConfiguration();

    private void RetropedeHeadColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _retropedeHeadColor, UpdateRetropedeHeadColorPreview);
    }

    private void RetropedeBodyColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _retropedeBodyColor, UpdateRetropedeBodyColorPreview);
    }

    private void MushroomColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _mushroomColor, UpdateMushroomColorPreview);
    }

    private void SpiderColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _spiderColor, UpdateSpiderColorPreview);
    }

    private void DdtBombColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _ddtBombColor, UpdateDdtBombColorPreview);
    }

    private void DdtGasColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _ddtGasColor, UpdateDdtGasColorPreview);
    }

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

    private void UpdateRetropedeHeadColorPreview()
    {
        RetropedeHeadColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_retropedeHeadColor.W * 255),
            (byte)(_retropedeHeadColor.X * 255),
            (byte)(_retropedeHeadColor.Y * 255),
            (byte)(_retropedeHeadColor.Z * 255)));
    }

    private void UpdateRetropedeBodyColorPreview()
    {
        RetropedeBodyColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_retropedeBodyColor.W * 255),
            (byte)(_retropedeBodyColor.X * 255),
            (byte)(_retropedeBodyColor.Y * 255),
            (byte)(_retropedeBodyColor.Z * 255)));
    }

    private void UpdateMushroomColorPreview()
    {
        MushroomColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_mushroomColor.W * 255),
            (byte)(_mushroomColor.X * 255),
            (byte)(_mushroomColor.Y * 255),
            (byte)(_mushroomColor.Z * 255)));
    }

    private void UpdateSpiderColorPreview()
    {
        SpiderColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_spiderColor.W * 255),
            (byte)(_spiderColor.X * 255),
            (byte)(_spiderColor.Y * 255),
            (byte)(_spiderColor.Z * 255)));
    }

    private void UpdateDdtBombColorPreview()
    {
        DdtBombColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_ddtBombColor.W * 255),
            (byte)(_ddtBombColor.X * 255),
            (byte)(_ddtBombColor.Y * 255),
            (byte)(_ddtBombColor.Z * 255)));
    }

    private void UpdateDdtGasColorPreview()
    {
        DdtGasColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_ddtGasColor.W * 255),
            (byte)(_ddtGasColor.X * 255),
            (byte)(_ddtGasColor.Y * 255),
            (byte)(_ddtGasColor.Z * 255)));
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect is RetropedeEffect retropedeEffect)
        {
            retropedeEffect.ResetGame();
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
