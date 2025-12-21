using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Invaders.UI;

public partial class InvadersSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private readonly DispatcherTimer _scoreTimer;
    private bool _isInitializing = true;

    private Vector4 _rocketColor = new(0f, 1f, 0.5f, 1f);
    private Vector4 _invaderSmallColor = new(1f, 0.2f, 0.8f, 1f);
    private Vector4 _invaderMediumColor = new(0.2f, 0.8f, 1f, 1f);
    private Vector4 _invaderBigColor = new(0.2f, 1f, 0.4f, 1f);
    private Vector4 _scoreOverlayColor = new(0f, 1f, 0f, 1f);

    public event Action<string>? SettingsChanged;

    public InvadersSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;

        // Subscribe to high scores change event
        if (_effect is InvadersEffect invadersEffect)
        {
            invadersEffect.HighScoresChanged += OnHighScoresChanged;
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
            if (_effect is InvadersEffect ie)
            {
                ie.HighScoresChanged -= OnHighScoresChanged;
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
        config.Set("renderStyle", RenderStyleComboBox.SelectedIndex);

        // Rocket settings
        config.Set("spawnOnLeftClick", LeftClickCheckBox.IsChecked ?? true);
        config.Set("spawnOnRightClick", RightClickCheckBox.IsChecked ?? false);
        config.Set("spawnOnMove", SpawnOnMoveCheckBox.IsChecked ?? false);
        config.Set("moveSpawnDistance", (float)MoveSpawnDistanceSlider.Value);
        config.Set("rocketSpeed", (float)RocketSpeedSlider.Value);
        config.Set("rocketSize", (float)RocketSizeSlider.Value);
        config.Set("rocketRainbowMode", RocketRainbowCheckBox.IsChecked ?? true);
        config.Set("rocketColor", _rocketColor);

        // Invader settings
        config.Set("invaderSpawnRate", (float)SpawnRateSlider.Value);
        config.Set("invaderMinSpeed", (float)InvaderMinSpeedSlider.Value);
        config.Set("invaderMaxSpeed", (float)InvaderMaxSpeedSlider.Value);
        config.Set("invaderBigSize", (float)BigSizeSlider.Value);
        config.Set("invaderMediumSizePercent", (float)MediumSizeSlider.Value);
        config.Set("invaderSmallSizePercent", (float)SmallSizeSlider.Value);
        config.Set("maxActiveInvaders", (int)MaxInvadersSlider.Value);
        config.Set("invaderDescentSpeed", (float)DescentSpeedSlider.Value);
        config.Set("invaderSmallColor", _invaderSmallColor);
        config.Set("invaderMediumColor", _invaderMediumColor);
        config.Set("invaderBigColor", _invaderBigColor);

        // Explosion settings
        config.Set("explosionParticleCount", (int)ExplosionParticlesSlider.Value);
        config.Set("explosionForce", (float)ExplosionForceSlider.Value);
        config.Set("explosionLifespan", (float)ExplosionLifespanSlider.Value);
        config.Set("explosionParticleSize", (float)ExplosionSizeSlider.Value);

        // Visual settings
        config.Set("glowIntensity", (float)GlowSlider.Value);
        config.Set("neonIntensity", (float)NeonSlider.Value);
        config.Set("enableTrails", EnableTrailsCheckBox.IsChecked ?? true);
        config.Set("animSpeed", (float)AnimSpeedSlider.Value);

        // Scoring
        if (int.TryParse(ScoreSmallTextBox.Text, out int scoreS))
            config.Set("scoreSmall", scoreS);
        if (int.TryParse(ScoreMediumTextBox.Text, out int scoreM))
            config.Set("scoreMedium", scoreM);
        if (int.TryParse(ScoreBigTextBox.Text, out int scoreB))
            config.Set("scoreBig", scoreB);

        // Score overlay
        config.Set("showScoreOverlay", ShowScoreOverlayCheckBox.IsChecked ?? true);
        config.Set("scoreOverlaySize", (float)ScoreOverlaySizeSlider.Value);
        config.Set("scoreOverlaySpacing", (float)ScoreOverlaySpacingSlider.Value);
        config.Set("scoreOverlayMargin", (float)ScoreOverlayMarginSlider.Value);
        config.Set("scoreOverlayBgOpacity", (float)ScoreOverlayBgOpacitySlider.Value);
        config.Set("scoreOverlayColor", _scoreOverlayColor);
        config.Set("scoreOverlayX", (float)ScoreOverlayXSlider.Value);
        config.Set("scoreOverlayY", (float)ScoreOverlayYSlider.Value);

        // Timer duration
        config.Set("timerDuration", (float)TimerDurationSlider.Value);

        // Reset hotkey
        config.Set("enableResetHotkey", EnableResetHotkeyCheckBox.IsChecked ?? false);

        // Preserve existing high scores
        if (_effect is InvadersEffect ie)
        {
            config.Set("highScoresJson", ie.GetHighScoresJson());
        }
    }

    private void ScoreTimer_Tick(object? sender, EventArgs e)
    {
        if (_effect is InvadersEffect invadersEffect)
        {
            // Update score
            ScoreDisplay.Text = invadersEffect.CurrentScore.ToString("N0");

            // Update PPM
            PpmDisplay.Text = ((int)invadersEffect.PointsPerMinute).ToString("N0");

            // Update timer countdown (show "READY" when waiting for first hit)
            if (invadersEffect.WaitingForFirstHit && invadersEffect.IsGameActive)
            {
                TimerDisplay.Text = "READY";
            }
            else
            {
                float remaining = invadersEffect.RemainingTime;
                int minutes = (int)remaining / 60;
                int seconds = (int)remaining % 60;
                TimerDisplay.Text = $"{minutes:D2}:{seconds:D2}";
            }

            // Change timer color to red when game over
            TimerDisplay.Foreground = new SolidColorBrush(
                invadersEffect.IsGameOver
                    ? System.Windows.Media.Color.FromRgb(255, 80, 80)
                    : System.Windows.Media.Color.FromRgb(0, 204, 255));

            // Update timer duration display
            float duration = invadersEffect.TimerDuration;
            int durMinutes = (int)duration / 60;
            int durSeconds = (int)duration % 60;
            TimerDurationDisplay.Text = $"{durMinutes:D2}:{durSeconds:D2}";
        }
    }

    private void LoadConfiguration()
    {
        // Render style
        if (_effect.Configuration.TryGet("renderStyle", out int renderStyle))
            RenderStyleComboBox.SelectedIndex = renderStyle;

        // Rocket settings
        if (_effect.Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
            LeftClickCheckBox.IsChecked = leftClick;
        if (_effect.Configuration.TryGet("spawnOnRightClick", out bool rightClick))
            RightClickCheckBox.IsChecked = rightClick;
        if (_effect.Configuration.TryGet("spawnOnMove", out bool spawnMove))
            SpawnOnMoveCheckBox.IsChecked = spawnMove;
        if (_effect.Configuration.TryGet("moveSpawnDistance", out float moveDist))
        {
            MoveSpawnDistanceSlider.Value = moveDist;
            MoveSpawnDistanceValue.Text = moveDist.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rocketSpeed", out float rocketSpd))
        {
            RocketSpeedSlider.Value = rocketSpd;
            RocketSpeedValue.Text = rocketSpd.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rocketSize", out float rocketSz))
        {
            RocketSizeSlider.Value = rocketSz;
            RocketSizeValue.Text = rocketSz.ToString("F0");
        }
        if (_effect.Configuration.TryGet("rocketRainbowMode", out bool rocketRainbow))
            RocketRainbowCheckBox.IsChecked = rocketRainbow;
        if (_effect.Configuration.TryGet("rocketColor", out Vector4 rocketCol))
        {
            _rocketColor = rocketCol;
            UpdateRocketColorPreview();
        }

        // Invader settings
        if (_effect.Configuration.TryGet("invaderSpawnRate", out float spawnRate))
        {
            SpawnRateSlider.Value = spawnRate;
            SpawnRateValue.Text = spawnRate.ToString("F1");
        }
        if (_effect.Configuration.TryGet("invaderMinSpeed", out float minSpd))
        {
            InvaderMinSpeedSlider.Value = minSpd;
            InvaderMinSpeedValue.Text = minSpd.ToString("F0");
        }
        if (_effect.Configuration.TryGet("invaderMaxSpeed", out float maxSpd))
        {
            InvaderMaxSpeedSlider.Value = maxSpd;
            InvaderMaxSpeedValue.Text = maxSpd.ToString("F0");
        }
        if (_effect.Configuration.TryGet("invaderBigSize", out float bigSize))
        {
            BigSizeSlider.Value = bigSize;
            BigSizeValue.Text = bigSize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("invaderMediumSizePercent", out float medPct))
        {
            MediumSizeSlider.Value = medPct;
            MediumSizeValue.Text = $"{(int)(medPct * 100)}%";
        }
        if (_effect.Configuration.TryGet("invaderSmallSizePercent", out float smallPct))
        {
            SmallSizeSlider.Value = smallPct;
            SmallSizeValue.Text = $"{(int)(smallPct * 100)}%";
        }
        if (_effect.Configuration.TryGet("maxActiveInvaders", out int maxInv))
        {
            MaxInvadersSlider.Value = maxInv;
            MaxInvadersValue.Text = maxInv.ToString();
        }
        if (_effect.Configuration.TryGet("invaderDescentSpeed", out float descent))
        {
            DescentSpeedSlider.Value = descent;
            DescentSpeedValue.Text = descent.ToString("F0");
        }
        if (_effect.Configuration.TryGet("invaderSmallColor", out Vector4 smallCol))
        {
            _invaderSmallColor = smallCol;
            UpdateSmallColorPreview();
        }
        if (_effect.Configuration.TryGet("invaderMediumColor", out Vector4 medCol))
        {
            _invaderMediumColor = medCol;
            UpdateMediumColorPreview();
        }
        if (_effect.Configuration.TryGet("invaderBigColor", out Vector4 bigCol))
        {
            _invaderBigColor = bigCol;
            UpdateBigColorPreview();
        }

        // Explosion settings
        if (_effect.Configuration.TryGet("explosionParticleCount", out int expCount))
        {
            ExplosionParticlesSlider.Value = expCount;
            ExplosionParticlesValue.Text = expCount.ToString();
        }
        if (_effect.Configuration.TryGet("explosionForce", out float expForce))
        {
            ExplosionForceSlider.Value = expForce;
            ExplosionForceValue.Text = expForce.ToString("F0");
        }
        if (_effect.Configuration.TryGet("explosionLifespan", out float expLife))
        {
            ExplosionLifespanSlider.Value = expLife;
            ExplosionLifespanValue.Text = expLife.ToString("F1");
        }
        if (_effect.Configuration.TryGet("explosionParticleSize", out float expSize))
        {
            ExplosionSizeSlider.Value = expSize;
            ExplosionSizeValue.Text = expSize.ToString("F0");
        }

        // Visual settings
        if (_effect.Configuration.TryGet("glowIntensity", out float glow))
        {
            GlowSlider.Value = glow;
            GlowValue.Text = glow.ToString("F1");
        }
        if (_effect.Configuration.TryGet("neonIntensity", out float neon))
        {
            NeonSlider.Value = neon;
            NeonValue.Text = neon.ToString("F1");
        }
        if (_effect.Configuration.TryGet("enableTrails", out bool trails))
            EnableTrailsCheckBox.IsChecked = trails;
        if (_effect.Configuration.TryGet("animSpeed", out float anim))
        {
            AnimSpeedSlider.Value = anim;
            AnimSpeedValue.Text = anim.ToString("F1");
        }

        // Scoring
        if (_effect.Configuration.TryGet("scoreSmall", out int scoreS))
            ScoreSmallTextBox.Text = scoreS.ToString();
        if (_effect.Configuration.TryGet("scoreMedium", out int scoreM))
            ScoreMediumTextBox.Text = scoreM.ToString();
        if (_effect.Configuration.TryGet("scoreBig", out int scoreB))
            ScoreBigTextBox.Text = scoreB.ToString();

        // Score overlay
        if (_effect.Configuration.TryGet("showScoreOverlay", out bool showOverlay))
            ShowScoreOverlayCheckBox.IsChecked = showOverlay;
        if (_effect.Configuration.TryGet("scoreOverlaySize", out float overlaySize))
        {
            ScoreOverlaySizeSlider.Value = overlaySize;
            ScoreOverlaySizeValue.Text = overlaySize.ToString("F0");
        }
        if (_effect.Configuration.TryGet("scoreOverlaySpacing", out float overlaySpacing))
        {
            ScoreOverlaySpacingSlider.Value = overlaySpacing;
            ScoreOverlaySpacingValue.Text = overlaySpacing.ToString("F2");
        }
        if (_effect.Configuration.TryGet("scoreOverlayMargin", out float overlayMargin))
        {
            ScoreOverlayMarginSlider.Value = overlayMargin;
            ScoreOverlayMarginValue.Text = overlayMargin.ToString("F0");
        }
        if (_effect.Configuration.TryGet("scoreOverlayBgOpacity", out float bgOpacity))
        {
            ScoreOverlayBgOpacitySlider.Value = bgOpacity;
            ScoreOverlayBgOpacityValue.Text = bgOpacity.ToString("F2");
        }
        if (_effect.Configuration.TryGet("scoreOverlayColor", out Vector4 overlayColor))
        {
            _scoreOverlayColor = overlayColor;
            UpdateScoreOverlayColorPreview();
        }
        if (_effect.Configuration.TryGet("scoreOverlayX", out float overlayX))
        {
            ScoreOverlayXSlider.Value = overlayX;
            ScoreOverlayXValue.Text = overlayX.ToString("F0");
        }
        if (_effect.Configuration.TryGet("scoreOverlayY", out float overlayY))
        {
            ScoreOverlayYSlider.Value = overlayY;
            ScoreOverlayYValue.Text = overlayY.ToString("F0");
        }

        // Timer duration - fallback to effect's current value if not in config
        if (_effect.Configuration.TryGet("timerDuration", out float timerDur))
        {
            TimerDurationSlider.Value = timerDur;
            int minutes = (int)timerDur / 60;
            int seconds = (int)timerDur % 60;
            TimerDurationValue.Text = $"{minutes}:{seconds:D2}";
        }
        else if (_effect is InvadersEffect ie)
        {
            float duration = ie.TimerDuration;
            TimerDurationSlider.Value = duration;
            int minutes = (int)duration / 60;
            int seconds = (int)duration % 60;
            TimerDurationValue.Text = $"{minutes}:{seconds:D2}";
        }

        // Reset hotkey
        if (_effect.Configuration.TryGet("enableResetHotkey", out bool resetHotkey))
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

    private void RenderStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateConfiguration();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        // Update value displays
        if (MoveSpawnDistanceValue != null)
            MoveSpawnDistanceValue.Text = MoveSpawnDistanceSlider.Value.ToString("F0");
        if (RocketSpeedValue != null)
            RocketSpeedValue.Text = RocketSpeedSlider.Value.ToString("F0");
        if (RocketSizeValue != null)
            RocketSizeValue.Text = RocketSizeSlider.Value.ToString("F0");
        if (SpawnRateValue != null)
            SpawnRateValue.Text = SpawnRateSlider.Value.ToString("F1");
        if (BigSizeValue != null)
            BigSizeValue.Text = BigSizeSlider.Value.ToString("F0");
        if (MediumSizeValue != null)
            MediumSizeValue.Text = $"{(int)(MediumSizeSlider.Value * 100)}%";
        if (SmallSizeValue != null)
            SmallSizeValue.Text = $"{(int)(SmallSizeSlider.Value * 100)}%";
        if (MaxInvadersValue != null)
            MaxInvadersValue.Text = ((int)MaxInvadersSlider.Value).ToString();
        if (DescentSpeedValue != null)
            DescentSpeedValue.Text = DescentSpeedSlider.Value.ToString("F0");
        if (ExplosionParticlesValue != null)
            ExplosionParticlesValue.Text = ((int)ExplosionParticlesSlider.Value).ToString();
        if (ExplosionForceValue != null)
            ExplosionForceValue.Text = ExplosionForceSlider.Value.ToString("F0");
        if (ExplosionLifespanValue != null)
            ExplosionLifespanValue.Text = ExplosionLifespanSlider.Value.ToString("F1");
        if (ExplosionSizeValue != null)
            ExplosionSizeValue.Text = ExplosionSizeSlider.Value.ToString("F0");
        if (GlowValue != null)
            GlowValue.Text = GlowSlider.Value.ToString("F1");
        if (NeonValue != null)
            NeonValue.Text = NeonSlider.Value.ToString("F1");
        if (AnimSpeedValue != null)
            AnimSpeedValue.Text = AnimSpeedSlider.Value.ToString("F1");

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

    private void InvaderMinSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 20;
        var minVal = e.NewValue;
        var maxVal = InvaderMaxSpeedSlider.Value;

        if (minVal >= maxVal - minGap + 1)
        {
            var newMax = Math.Min(minVal + minGap, InvaderMaxSpeedSlider.Maximum);
            InvaderMaxSpeedSlider.Value = newMax;
            InvaderMaxSpeedValue.Text = newMax.ToString("F0");
        }

        if (InvaderMinSpeedValue != null)
            InvaderMinSpeedValue.Text = minVal.ToString("F0");
        UpdateConfiguration();
    }

    private void InvaderMaxSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 20;
        var maxVal = e.NewValue;
        var minVal = InvaderMinSpeedSlider.Value;

        if (maxVal <= minVal + minGap - 1)
        {
            var newMin = Math.Max(maxVal - minGap, InvaderMinSpeedSlider.Minimum);
            InvaderMinSpeedSlider.Value = newMin;
            InvaderMinSpeedValue.Text = newMin.ToString("F0");
        }

        if (InvaderMaxSpeedValue != null)
            InvaderMaxSpeedValue.Text = maxVal.ToString("F0");
        UpdateConfiguration();
    }

    private void ScoreTextBox_Changed(object sender, TextChangedEventArgs e) => UpdateConfiguration();

    private void RocketColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(ref _rocketColor, UpdateRocketColorPreview);
    }

    private void SmallColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _invaderSmallColor, UpdateSmallColorPreview);
    }

    private void MediumColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _invaderMediumColor, UpdateMediumColorPreview);
    }

    private void BigColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _invaderBigColor, UpdateBigColorPreview);
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

    private void UpdateRocketColorPreview()
    {
        RocketColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_rocketColor.W * 255),
            (byte)(_rocketColor.X * 255),
            (byte)(_rocketColor.Y * 255),
            (byte)(_rocketColor.Z * 255)));
    }

    private void UpdateSmallColorPreview()
    {
        SmallColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_invaderSmallColor.W * 255),
            (byte)(_invaderSmallColor.X * 255),
            (byte)(_invaderSmallColor.Y * 255),
            (byte)(_invaderSmallColor.Z * 255)));
    }

    private void UpdateMediumColorPreview()
    {
        MediumColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_invaderMediumColor.W * 255),
            (byte)(_invaderMediumColor.X * 255),
            (byte)(_invaderMediumColor.Y * 255),
            (byte)(_invaderMediumColor.Z * 255)));
    }

    private void UpdateBigColorPreview()
    {
        BigColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_invaderBigColor.W * 255),
            (byte)(_invaderBigColor.X * 255),
            (byte)(_invaderBigColor.Y * 255),
            (byte)(_invaderBigColor.Z * 255)));
    }

    private void ScoreOverlayColorPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PickColor(ref _scoreOverlayColor, UpdateScoreOverlayColorPreview);
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
        if (_effect is InvadersEffect invadersEffect)
        {
            invadersEffect.ResetGame();
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
