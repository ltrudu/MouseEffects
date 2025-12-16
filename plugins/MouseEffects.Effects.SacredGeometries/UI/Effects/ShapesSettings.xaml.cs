using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MouseEffects.Effects.SacredGeometries.UI.Effects;

public partial class ShapesSettings : System.Windows.Controls.UserControl
{
    private SacredGeometriesEffect? _effect;
    private bool _isLoading;

    public ShapesSettings()
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
            // Shape
            ShapeCombo.SelectedIndex = (int)_effect.SelectedShape;
            RandomShapeCheck.IsChecked = _effect.RandomShapeEnabled;
            CycleShapesCheck.IsChecked = _effect.CycleShapesEnabled;
            CyclePanel.Visibility = _effect.CycleShapesEnabled ? Visibility.Visible : Visibility.Collapsed;
            CycleSpeedSlider.Value = _effect.CycleSpeed;
            CycleSpeedLabel.Text = $"{_effect.CycleSpeed:F1}s";

            // Size
            RadiusSlider.Value = _effect.ShapeRadius;
            RadiusLabel.Text = $"{_effect.ShapeRadius:F0} px";
            PulseRadiusCheck.IsChecked = _effect.PulseRadiusEnabled;
            PulsePanel.Visibility = _effect.PulseRadiusEnabled ? Visibility.Visible : Visibility.Collapsed;
            PulseAmountSlider.Value = _effect.PulseAmount;
            PulseAmountLabel.Text = $"{_effect.PulseAmount:F2}";
            PulseSpeedSlider.Value = _effect.PulseSpeed;
            PulseSpeedLabel.Text = $"{_effect.PulseSpeed:F1}x";

            // Rotation
            RotSpeedSlider.Value = _effect.ShapeRotationSpeed;
            RotSpeedLabel.Text = $"{_effect.ShapeRotationSpeed:F0} deg/s";
            DirectionCombo.SelectedIndex = _effect.ShapeRotationDirection;

            // Colors
            RainbowModeCheck.IsChecked = _effect.ShapeRainbowMode;
            RainbowPanel.Visibility = _effect.ShapeRainbowMode ? Visibility.Visible : Visibility.Collapsed;
            FixedColorPanel.Visibility = _effect.ShapeRainbowMode ? Visibility.Collapsed : Visibility.Visible;
            RainbowSpeedSlider.Value = _effect.ShapeRainbowSpeed;
            RainbowSpeedLabel.Text = $"{_effect.ShapeRainbowSpeed:F1}x";
            IndependentRainbowCheck.IsChecked = _effect.ShapeIndependentRainbow;
            UpdateColorPreview(PrimaryColorPreview, _effect.ShapePrimaryColor);
            UpdateColorPreview(SecondaryColorPreview, _effect.ShapeSecondaryColor);

            // Glow
            GlowIntensitySlider.Value = _effect.ShapeGlowIntensity;
            GlowIntensityLabel.Text = $"{_effect.ShapeGlowIntensity:F1}";
            LineThicknessSlider.Value = _effect.ShapeLineThickness;
            LineThicknessLabel.Text = $"{_effect.ShapeLineThickness:F3}";
            TwinkleIntensitySlider.Value = _effect.ShapeTwinkleIntensity;
            TwinkleIntensityLabel.Text = $"{_effect.ShapeTwinkleIntensity:F2}";

            // Animation
            AnimSpeedSlider.Value = _effect.ShapeAnimationSpeed;
            AnimSpeedLabel.Text = $"{_effect.ShapeAnimationSpeed:F1}x";

            // Morph
            MorphEnabledCheck.IsChecked = _effect.ShapeMorphEnabled;
            MorphPanel.Visibility = _effect.ShapeMorphEnabled ? Visibility.Visible : Visibility.Collapsed;
            MorphBetweenShapesCheck.IsChecked = _effect.ShapeMorphBetweenShapes;
            MorphSpeedSlider.Value = _effect.ShapeMorphSpeed;
            MorphSpeedLabel.Text = $"{_effect.ShapeMorphSpeed:F1}x";
            MorphIntensitySlider.Value = _effect.ShapeMorphIntensity;
            MorphIntensityLabel.Text = $"{_effect.ShapeMorphIntensity:F2}";

            // Appearance
            AppearanceModeCombo.SelectedIndex = (int)_effect.ShapeAppearanceMode;

            // Spawn
            MaxCountSlider.Value = _effect.MaxShapeCount;
            MaxCountLabel.Text = _effect.MaxShapeCount == 1 ? "1 (Follow Cursor)" : $"{_effect.MaxShapeCount}";

            // Triggers
            MouseMoveEnabledCheck.IsChecked = _effect.ShapeMouseMoveEnabled;
            MoveDistancePanel.Visibility = _effect.ShapeMouseMoveEnabled ? Visibility.Visible : Visibility.Collapsed;
            MoveDistanceSlider.Value = _effect.ShapeMoveDistanceThreshold;
            MoveDistanceLabel.Text = $"{_effect.ShapeMoveDistanceThreshold:F0} px";
            LeftClickEnabledCheck.IsChecked = _effect.ShapeLeftClickEnabled;
            RightClickEnabledCheck.IsChecked = _effect.ShapeRightClickEnabled;

            // Lifetime
            LifetimeSlider.Value = _effect.ShapeLifetimeDuration;
            LifetimeLabel.Text = $"{_effect.ShapeLifetimeDuration:F1}s";

            // Performance
            MaxActiveSlider.Value = _effect.MaxActiveShapes;
            MaxActiveLabel.Text = $"{_effect.MaxActiveShapes}";
            MaxSpawnsSlider.Value = _effect.ShapeMaxSpawnsPerSecond;
            MaxSpawnsLabel.Text = $"{_effect.ShapeMaxSpawnsPerSecond}/s";
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

    // ===== SHAPE HANDLERS =====
    private void ShapeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SelectedShape = (ShapeType)ShapeCombo.SelectedIndex;
        _effect.Configuration.Set("sh_shape_selected", ShapeCombo.SelectedIndex);
    }

    private void RandomShapeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomShapeEnabled = RandomShapeCheck.IsChecked == true;
        _effect.Configuration.Set("sh_shape_randomEnabled", _effect.RandomShapeEnabled);
    }

    private void CycleShapesCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.CycleShapesEnabled = CycleShapesCheck.IsChecked == true;
        _effect.Configuration.Set("sh_shape_cycleEnabled", _effect.CycleShapesEnabled);
        CyclePanel.Visibility = _effect.CycleShapesEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CycleSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.CycleSpeed = (float)CycleSpeedSlider.Value;
        _effect.Configuration.Set("sh_shape_cycleSpeed", _effect.CycleSpeed);
        CycleSpeedLabel.Text = $"{_effect.CycleSpeed:F1}s";
    }

    // ===== SIZE HANDLERS =====
    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeRadius = (float)RadiusSlider.Value;
        _effect.Configuration.Set("sh_rad_fixed", _effect.ShapeRadius);
        RadiusLabel.Text = $"{_effect.ShapeRadius:F0} px";
    }

    private void PulseRadiusCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PulseRadiusEnabled = PulseRadiusCheck.IsChecked == true;
        _effect.Configuration.Set("sh_rad_pulseEnabled", _effect.PulseRadiusEnabled);
        PulsePanel.Visibility = _effect.PulseRadiusEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PulseAmountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PulseAmount = (float)PulseAmountSlider.Value;
        _effect.Configuration.Set("sh_rad_pulseAmount", _effect.PulseAmount);
        PulseAmountLabel.Text = $"{_effect.PulseAmount:F2}";
    }

    private void PulseSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.PulseSpeed = (float)PulseSpeedSlider.Value;
        _effect.Configuration.Set("sh_rad_pulseSpeed", _effect.PulseSpeed);
        PulseSpeedLabel.Text = $"{_effect.PulseSpeed:F1}x";
    }

    // ===== ROTATION HANDLERS =====
    private void RotSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeRotationSpeed = (float)RotSpeedSlider.Value;
        _effect.Configuration.Set("sh_rot_speed", _effect.ShapeRotationSpeed);
        RotSpeedLabel.Text = $"{_effect.ShapeRotationSpeed:F0} deg/s";
    }

    private void DirectionCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeRotationDirection = DirectionCombo.SelectedIndex;
        _effect.Configuration.Set("sh_rot_direction", _effect.ShapeRotationDirection);
    }

    // ===== COLOR HANDLERS =====
    private void RainbowModeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeRainbowMode = RainbowModeCheck.IsChecked == true;
        _effect.Configuration.Set("sh_col_rainbowMode", _effect.ShapeRainbowMode);
        RainbowPanel.Visibility = _effect.ShapeRainbowMode ? Visibility.Visible : Visibility.Collapsed;
        FixedColorPanel.Visibility = _effect.ShapeRainbowMode ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RainbowSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeRainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("sh_col_rainbowSpeed", _effect.ShapeRainbowSpeed);
        RainbowSpeedLabel.Text = $"{_effect.ShapeRainbowSpeed:F1}x";
    }

    private void IndependentRainbowCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeIndependentRainbow = IndependentRainbowCheck.IsChecked == true;
        _effect.Configuration.Set("sh_col_independentRainbow", _effect.ShapeIndependentRainbow);
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
            _effect.ShapePrimaryColor = newColor;
            _effect.Configuration.Set("sh_col_primary", newColor);
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
            _effect.ShapeSecondaryColor = newColor;
            _effect.Configuration.Set("sh_col_secondary", newColor);
            UpdateColorPreview(SecondaryColorPreview, newColor);
        }
    }

    // ===== GLOW HANDLERS =====
    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeGlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("sh_glow_intensity", _effect.ShapeGlowIntensity);
        GlowIntensityLabel.Text = $"{_effect.ShapeGlowIntensity:F1}";
    }

    private void LineThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeLineThickness = (float)LineThicknessSlider.Value;
        _effect.Configuration.Set("sh_glow_lineThickness", _effect.ShapeLineThickness);
        LineThicknessLabel.Text = $"{_effect.ShapeLineThickness:F3}";
    }

    private void TwinkleIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeTwinkleIntensity = (float)TwinkleIntensitySlider.Value;
        _effect.Configuration.Set("sh_glow_twinkleIntensity", _effect.ShapeTwinkleIntensity);
        TwinkleIntensityLabel.Text = $"{_effect.ShapeTwinkleIntensity:F2}";
    }

    // ===== ANIMATION HANDLERS =====
    private void AnimSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeAnimationSpeed = (float)AnimSpeedSlider.Value;
        _effect.Configuration.Set("sh_anim_speed", _effect.ShapeAnimationSpeed);
        AnimSpeedLabel.Text = $"{_effect.ShapeAnimationSpeed:F1}x";
    }

    // ===== APPEARANCE HANDLERS =====
    private void AppearanceModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeAppearanceMode = (AppearanceMode)AppearanceModeCombo.SelectedIndex;
        _effect.Configuration.Set("sh_app_mode", AppearanceModeCombo.SelectedIndex);
    }

    // ===== SPAWN HANDLERS =====
    private void MaxCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxShapeCount = (int)MaxCountSlider.Value;
        _effect.Configuration.Set("sh_spawn_maxCount", _effect.MaxShapeCount);
        MaxCountLabel.Text = _effect.MaxShapeCount == 1 ? "1 (Follow Cursor)" : $"{_effect.MaxShapeCount}";
    }

    // ===== TRIGGER HANDLERS =====
    private void MouseMoveEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeMouseMoveEnabled = MouseMoveEnabledCheck.IsChecked == true;
        _effect.Configuration.Set("sh_trig_mouseMoveEnabled", _effect.ShapeMouseMoveEnabled);
        MoveDistancePanel.Visibility = _effect.ShapeMouseMoveEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MoveDistanceSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeMoveDistanceThreshold = (float)MoveDistanceSlider.Value;
        _effect.Configuration.Set("sh_trig_moveDistance", _effect.ShapeMoveDistanceThreshold);
        MoveDistanceLabel.Text = $"{_effect.ShapeMoveDistanceThreshold:F0} px";
    }

    private void LeftClickEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeLeftClickEnabled = LeftClickEnabledCheck.IsChecked == true;
        _effect.Configuration.Set("sh_trig_leftClickEnabled", _effect.ShapeLeftClickEnabled);
    }

    private void RightClickEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeRightClickEnabled = RightClickEnabledCheck.IsChecked == true;
        _effect.Configuration.Set("sh_trig_rightClickEnabled", _effect.ShapeRightClickEnabled);
    }

    // ===== LIFETIME HANDLERS =====
    private void LifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeLifetimeDuration = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("sh_life_duration", _effect.ShapeLifetimeDuration);
        LifetimeLabel.Text = $"{_effect.ShapeLifetimeDuration:F1}s";
    }

    // ===== PERFORMANCE HANDLERS =====
    private void MaxActiveSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxActiveShapes = (int)MaxActiveSlider.Value;
        _effect.Configuration.Set("sh_perf_maxActive", _effect.MaxActiveShapes);
        MaxActiveLabel.Text = $"{_effect.MaxActiveShapes}";
    }

    private void MaxSpawnsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeMaxSpawnsPerSecond = (int)MaxSpawnsSlider.Value;
        _effect.Configuration.Set("sh_perf_maxSpawnsPerSecond", _effect.ShapeMaxSpawnsPerSecond);
        MaxSpawnsLabel.Text = $"{_effect.ShapeMaxSpawnsPerSecond}/s";
    }

    // ===== MORPH HANDLERS =====
    private void MorphEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeMorphEnabled = MorphEnabledCheck.IsChecked == true;
        _effect.Configuration.Set("sh_morph_enabled", _effect.ShapeMorphEnabled);
        MorphPanel.Visibility = _effect.ShapeMorphEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MorphBetweenShapesCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeMorphBetweenShapes = MorphBetweenShapesCheck.IsChecked == true;
        _effect.Configuration.Set("sh_morph_betweenShapes", _effect.ShapeMorphBetweenShapes);
    }

    private void MorphSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeMorphSpeed = (float)MorphSpeedSlider.Value;
        _effect.Configuration.Set("sh_morph_speed", _effect.ShapeMorphSpeed);
        MorphSpeedLabel.Text = $"{_effect.ShapeMorphSpeed:F1}x";
    }

    private void MorphIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ShapeMorphIntensity = (float)MorphIntensitySlider.Value;
        _effect.Configuration.Set("sh_morph_intensity", _effect.ShapeMorphIntensity);
        MorphIntensityLabel.Text = $"{_effect.ShapeMorphIntensity:F2}";
    }
}
