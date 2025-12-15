using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MouseEffects.Effects.SacredGeometries.UI.Effects;

public partial class MandalaSettings : System.Windows.Controls.UserControl
{
    private SacredGeometriesEffect? _effect;
    private bool _isLoading;

    public MandalaSettings()
    {
        InitializeComponent();
    }

    public void Initialize(SacredGeometriesEffect effect)
    {
        _effect = effect;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;
        _isLoading = true;

        try
        {
            // Pattern
            PatternCombo.SelectedIndex = (int)_effect.SelectedPattern;
            RandomPatternCheck.IsChecked = _effect.RandomPatternEnabled;
            ComplexitySlider.Value = _effect.PatternComplexity;
            ComplexityLabel.Text = $"{_effect.PatternComplexity:F1}";

            // Radius
            FixedRadiusSlider.Value = _effect.FixedRadius;
            FixedRadiusLabel.Text = $"{_effect.FixedRadius:F0} px";
            AnimatedRadiusCheck.IsChecked = _effect.AnimatedRadius;
            AnimatedRadiusPanel.Visibility = _effect.AnimatedRadius ? Visibility.Visible : Visibility.Collapsed;
            MinRadiusSlider.Value = _effect.RadiusMin;
            MinRadiusLabel.Text = $"{_effect.RadiusMin:F0} px";
            MaxRadiusSlider.Value = _effect.RadiusMax;
            MaxRadiusLabel.Text = $"{_effect.RadiusMax:F0} px";
            OscSpeedSlider.Value = _effect.RadiusOscSpeed;
            OscSpeedLabel.Text = $"{_effect.RadiusOscSpeed:F1}x";

            // Rotation
            RotSpeedSlider.Value = _effect.RotationSpeed;
            RotSpeedLabel.Text = $"{_effect.RotationSpeed:F0}°/s";
            DirectionCombo.SelectedIndex = _effect.RotationDirection;
            RandomRotSpeedCheck.IsChecked = _effect.RandomRotationSpeed;
            RandomRotSpeedPanel.Visibility = _effect.RandomRotationSpeed ? Visibility.Visible : Visibility.Collapsed;
            RotMinSpeedSlider.Value = _effect.RotationSpeedMin;
            RotMinSpeedLabel.Text = $"{_effect.RotationSpeedMin:F0}°/s";
            RotMaxSpeedSlider.Value = _effect.RotationSpeedMax;
            RotMaxSpeedLabel.Text = $"{_effect.RotationSpeedMax:F0}°/s";

            // Colors
            RainbowModeCheck.IsChecked = _effect.RainbowMode;
            RainbowPanel.Visibility = _effect.RainbowMode ? Visibility.Visible : Visibility.Collapsed;
            FixedColorPanel.Visibility = _effect.RainbowMode ? Visibility.Collapsed : Visibility.Visible;
            RainbowSpeedSlider.Value = _effect.RainbowSpeed;
            RainbowSpeedLabel.Text = $"{_effect.RainbowSpeed:F1}x";
            RandomRainbowSpeedCheck.IsChecked = _effect.RandomRainbowSpeed;
            RandomRainbowSpeedPanel.Visibility = _effect.RandomRainbowSpeed ? Visibility.Visible : Visibility.Collapsed;
            RainbowMinSpeedSlider.Value = _effect.RainbowSpeedMin;
            RainbowMinSpeedLabel.Text = $"{_effect.RainbowSpeedMin:F1}x";
            RainbowMaxSpeedSlider.Value = _effect.RainbowSpeedMax;
            RainbowMaxSpeedLabel.Text = $"{_effect.RainbowSpeedMax:F1}x";
            UpdateColorPreview(PrimaryColorPreview, _effect.PrimaryColor);
            UpdateColorPreview(SecondaryColorPreview, _effect.SecondaryColor);

            // Glow
            GlowIntensitySlider.Value = _effect.GlowIntensity;
            GlowIntensityLabel.Text = $"{_effect.GlowIntensity:F1}";
            LineThicknessSlider.Value = _effect.LineThickness;
            LineThicknessLabel.Text = $"{_effect.LineThickness:F1} px";
            TwinkleIntensitySlider.Value = _effect.TwinkleIntensity;
            TwinkleIntensityLabel.Text = $"{_effect.TwinkleIntensity:F2}";

            // Appearance
            AppearanceModeCombo.SelectedIndex = (int)_effect.AppearanceMode;
            RandomAppearanceModeCheck.IsChecked = _effect.RandomAppearanceMode;
            FadeInSlider.Value = _effect.FadeInDuration;
            FadeInLabel.Text = $"{_effect.FadeInDuration:F2}s";
            FadeOutSlider.Value = _effect.FadeOutDuration;
            FadeOutLabel.Text = $"{_effect.FadeOutDuration:F2}s";
            ScaleInSlider.Value = _effect.ScaleInDuration;
            ScaleInLabel.Text = $"{_effect.ScaleInDuration:F2}s";
            ScaleOutSlider.Value = _effect.ScaleOutDuration;
            ScaleOutLabel.Text = $"{_effect.ScaleOutDuration:F2}s";

            // Spawn
            MaxCountSlider.Value = _effect.MaxMandalaCount;
            MaxCountLabel.Text = _effect.MaxMandalaCount == 1 ? "1 (Follow Cursor)" : $"{_effect.MaxMandalaCount}";

            // Triggers
            MouseMoveEnabledCheck.IsChecked = _effect.MouseMoveEnabled;
            MoveDistancePanel.Visibility = _effect.MouseMoveEnabled ? Visibility.Visible : Visibility.Collapsed;
            MoveDistanceSlider.Value = _effect.MoveDistanceThreshold;
            MoveDistanceLabel.Text = $"{_effect.MoveDistanceThreshold:F0} px";
            LeftClickEnabledCheck.IsChecked = _effect.LeftClickEnabled;
            RightClickEnabledCheck.IsChecked = _effect.RightClickEnabled;

            // Lifetime
            LifetimeSlider.Value = _effect.LifetimeDuration;
            LifetimeLabel.Text = $"{_effect.LifetimeDuration:F1}s";
            WhileActiveModeCheck.IsChecked = _effect.WhileActiveMode;

            // Performance
            MaxActiveSlider.Value = _effect.MaxActiveMandalas;
            MaxActiveLabel.Text = $"{_effect.MaxActiveMandalas}";
            MaxSpawnsSlider.Value = _effect.MaxSpawnsPerSecond;
            MaxSpawnsLabel.Text = $"{_effect.MaxSpawnsPerSecond}/s";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateColorPreview(Border preview, Vector4 color)
    {
        var mediaColor = System.Windows.Media.Color.FromArgb(
            (byte)(color.W * 255),
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255));
        preview.Background = new SolidColorBrush(mediaColor);
    }

    private Vector4 GetColorFromPreview(Border preview)
    {
        if (preview.Background is SolidColorBrush brush)
        {
            var c = brush.Color;
            return new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        }
        return new Vector4(1, 1, 1, 1);
    }

    // ===== PATTERN HANDLERS =====
    private void PatternCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SelectedPattern = (PatternType)PatternCombo.SelectedIndex;
        _effect.Configuration.Set("sg_pat_selected", PatternCombo.SelectedIndex);
    }

    private void RandomPatternCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomPatternEnabled = RandomPatternCheck.IsChecked == true;
        _effect.Configuration.Set("sg_pat_randomEnabled", _effect.RandomPatternEnabled);
    }

    private void ComplexitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PatternComplexity = (float)ComplexitySlider.Value;
        _effect.Configuration.Set("sg_pat_complexity", _effect.PatternComplexity);
        ComplexityLabel.Text = $"{_effect.PatternComplexity:F1}";
    }

    // ===== RADIUS HANDLERS =====
    private void FixedRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FixedRadius = (float)FixedRadiusSlider.Value;
        _effect.Configuration.Set("sg_rad_fixed", _effect.FixedRadius);
        FixedRadiusLabel.Text = $"{_effect.FixedRadius:F0} px";
    }

    private void AnimatedRadiusCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.AnimatedRadius = AnimatedRadiusCheck.IsChecked == true;
        _effect.Configuration.Set("sg_rad_animated", _effect.AnimatedRadius);
        AnimatedRadiusPanel.Visibility = _effect.AnimatedRadius ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MinRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RadiusMin = (float)MinRadiusSlider.Value;
        _effect.Configuration.Set("sg_rad_min", _effect.RadiusMin);
        MinRadiusLabel.Text = $"{_effect.RadiusMin:F0} px";
    }

    private void MaxRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RadiusMax = (float)MaxRadiusSlider.Value;
        _effect.Configuration.Set("sg_rad_max", _effect.RadiusMax);
        MaxRadiusLabel.Text = $"{_effect.RadiusMax:F0} px";
    }

    private void OscSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RadiusOscSpeed = (float)OscSpeedSlider.Value;
        _effect.Configuration.Set("sg_rad_oscSpeed", _effect.RadiusOscSpeed);
        OscSpeedLabel.Text = $"{_effect.RadiusOscSpeed:F1}x";
    }

    // ===== ROTATION HANDLERS =====
    private void RotSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationSpeed = (float)RotSpeedSlider.Value;
        _effect.Configuration.Set("sg_rot_speed", _effect.RotationSpeed);
        RotSpeedLabel.Text = $"{_effect.RotationSpeed:F0}°/s";
    }

    private void DirectionCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationDirection = DirectionCombo.SelectedIndex;
        _effect.Configuration.Set("sg_rot_direction", _effect.RotationDirection);
    }

    private void RandomRotSpeedCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomRotationSpeed = RandomRotSpeedCheck.IsChecked == true;
        _effect.Configuration.Set("sg_rot_randomSpeed", _effect.RandomRotationSpeed);
        RandomRotSpeedPanel.Visibility = _effect.RandomRotationSpeed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RotMinSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationSpeedMin = (float)RotMinSpeedSlider.Value;
        _effect.Configuration.Set("sg_rot_minSpeed", _effect.RotationSpeedMin);
        RotMinSpeedLabel.Text = $"{_effect.RotationSpeedMin:F0}°/s";
    }

    private void RotMaxSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationSpeedMax = (float)RotMaxSpeedSlider.Value;
        _effect.Configuration.Set("sg_rot_maxSpeed", _effect.RotationSpeedMax);
        RotMaxSpeedLabel.Text = $"{_effect.RotationSpeedMax:F0}°/s";
    }

    // ===== COLOR HANDLERS =====
    private void RainbowModeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowMode = RainbowModeCheck.IsChecked == true;
        _effect.Configuration.Set("sg_col_rainbowMode", _effect.RainbowMode);
        RainbowPanel.Visibility = _effect.RainbowMode ? Visibility.Visible : Visibility.Collapsed;
        FixedColorPanel.Visibility = _effect.RainbowMode ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RainbowSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("sg_col_rainbowSpeed", _effect.RainbowSpeed);
        RainbowSpeedLabel.Text = $"{_effect.RainbowSpeed:F1}x";
    }

    private void RandomRainbowSpeedCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomRainbowSpeed = RandomRainbowSpeedCheck.IsChecked == true;
        _effect.Configuration.Set("sg_col_randomRainbowSpeed", _effect.RandomRainbowSpeed);
        RandomRainbowSpeedPanel.Visibility = _effect.RandomRainbowSpeed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RainbowMinSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowSpeedMin = (float)RainbowMinSpeedSlider.Value;
        _effect.Configuration.Set("sg_col_rainbowSpeedMin", _effect.RainbowSpeedMin);
        RainbowMinSpeedLabel.Text = $"{_effect.RainbowSpeedMin:F1}x";
    }

    private void RainbowMaxSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowSpeedMax = (float)RainbowMaxSpeedSlider.Value;
        _effect.Configuration.Set("sg_col_rainbowSpeedMax", _effect.RainbowSpeedMax);
        RainbowMaxSpeedLabel.Text = $"{_effect.RainbowSpeedMax:F1}x";
    }

    private void PrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        var currentColor = GetColorFromPreview(PrimaryColorPreview);
        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(currentColor.W * 255),
                (int)(currentColor.X * 255),
                (int)(currentColor.Y * 255),
                (int)(currentColor.Z * 255))
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                dialog.Color.A / 255f);
            _effect.PrimaryColor = newColor;
            _effect.Configuration.Set("sg_col_primary", newColor);
            UpdateColorPreview(PrimaryColorPreview, newColor);
        }
    }

    private void SecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        var currentColor = GetColorFromPreview(SecondaryColorPreview);
        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(currentColor.W * 255),
                (int)(currentColor.X * 255),
                (int)(currentColor.Y * 255),
                (int)(currentColor.Z * 255))
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                dialog.Color.A / 255f);
            _effect.SecondaryColor = newColor;
            _effect.Configuration.Set("sg_col_secondary", newColor);
            UpdateColorPreview(SecondaryColorPreview, newColor);
        }
    }

    // ===== GLOW HANDLERS =====
    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("sg_glow_intensity", _effect.GlowIntensity);
        GlowIntensityLabel.Text = $"{_effect.GlowIntensity:F1}";
    }

    private void LineThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LineThickness = (float)LineThicknessSlider.Value;
        _effect.Configuration.Set("sg_glow_lineThickness", _effect.LineThickness);
        LineThicknessLabel.Text = $"{_effect.LineThickness:F1} px";
    }

    private void TwinkleIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.TwinkleIntensity = (float)TwinkleIntensitySlider.Value;
        _effect.Configuration.Set("sg_glow_twinkleIntensity", _effect.TwinkleIntensity);
        TwinkleIntensityLabel.Text = $"{_effect.TwinkleIntensity:F2}";
    }

    // ===== APPEARANCE HANDLERS =====
    private void AppearanceModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.AppearanceMode = (AppearanceMode)AppearanceModeCombo.SelectedIndex;
        _effect.Configuration.Set("sg_app_mode", AppearanceModeCombo.SelectedIndex);
    }

    private void RandomAppearanceModeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomAppearanceMode = RandomAppearanceModeCheck.IsChecked == true;
        _effect.Configuration.Set("sg_app_randomMode", _effect.RandomAppearanceMode);
    }

    private void FadeInSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FadeInDuration = (float)FadeInSlider.Value;
        _effect.Configuration.Set("sg_app_fadeInDuration", _effect.FadeInDuration);
        FadeInLabel.Text = $"{_effect.FadeInDuration:F2}s";
    }

    private void FadeOutSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FadeOutDuration = (float)FadeOutSlider.Value;
        _effect.Configuration.Set("sg_app_fadeOutDuration", _effect.FadeOutDuration);
        FadeOutLabel.Text = $"{_effect.FadeOutDuration:F2}s";
    }

    private void ScaleInSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ScaleInDuration = (float)ScaleInSlider.Value;
        _effect.Configuration.Set("sg_app_scaleInDuration", _effect.ScaleInDuration);
        ScaleInLabel.Text = $"{_effect.ScaleInDuration:F2}s";
    }

    private void ScaleOutSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ScaleOutDuration = (float)ScaleOutSlider.Value;
        _effect.Configuration.Set("sg_app_scaleOutDuration", _effect.ScaleOutDuration);
        ScaleOutLabel.Text = $"{_effect.ScaleOutDuration:F2}s";
    }

    // ===== SPAWN HANDLERS =====
    private void MaxCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxMandalaCount = (int)MaxCountSlider.Value;
        _effect.Configuration.Set("sg_spawn_maxCount", _effect.MaxMandalaCount);
        MaxCountLabel.Text = _effect.MaxMandalaCount == 1 ? "1 (Follow Cursor)" : $"{_effect.MaxMandalaCount}";
    }

    // ===== TRIGGER HANDLERS =====
    private void MouseMoveEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MouseMoveEnabled = MouseMoveEnabledCheck.IsChecked == true;
        _effect.Configuration.Set("sg_trig_mouseMoveEnabled", _effect.MouseMoveEnabled);
        MoveDistancePanel.Visibility = _effect.MouseMoveEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MoveDistanceSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MoveDistanceThreshold = (float)MoveDistanceSlider.Value;
        _effect.Configuration.Set("sg_trig_moveDistance", _effect.MoveDistanceThreshold);
        MoveDistanceLabel.Text = $"{_effect.MoveDistanceThreshold:F0} px";
    }

    private void LeftClickEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LeftClickEnabled = LeftClickEnabledCheck.IsChecked == true;
        _effect.Configuration.Set("sg_trig_leftClickEnabled", _effect.LeftClickEnabled);
    }

    private void RightClickEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RightClickEnabled = RightClickEnabledCheck.IsChecked == true;
        _effect.Configuration.Set("sg_trig_rightClickEnabled", _effect.RightClickEnabled);
    }

    // ===== LIFETIME HANDLERS =====
    private void LifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LifetimeDuration = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("sg_life_duration", _effect.LifetimeDuration);
        LifetimeLabel.Text = $"{_effect.LifetimeDuration:F1}s";
    }

    private void WhileActiveModeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.WhileActiveMode = WhileActiveModeCheck.IsChecked == true;
        _effect.Configuration.Set("sg_life_whileActiveMode", _effect.WhileActiveMode);
    }

    // ===== PERFORMANCE HANDLERS =====
    private void MaxActiveSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxActiveMandalas = (int)MaxActiveSlider.Value;
        _effect.Configuration.Set("sg_perf_maxActive", _effect.MaxActiveMandalas);
        MaxActiveLabel.Text = $"{_effect.MaxActiveMandalas}";
    }

    private void MaxSpawnsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxSpawnsPerSecond = (int)MaxSpawnsSlider.Value;
        _effect.Configuration.Set("sg_perf_maxSpawnsPerSecond", _effect.MaxSpawnsPerSecond);
        MaxSpawnsLabel.Text = $"{_effect.MaxSpawnsPerSecond}/s";
    }
}
