using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Tesla.UI.Effects;

public partial class LightningBoltSettings : System.Windows.Controls.UserControl
{
    private TeslaEffect? _effect;
    private bool _isLoading;

    public LightningBoltSettings()
    {
        InitializeComponent();
    }

    public void Initialize(TeslaEffect effect)
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
            // Trigger settings
            MouseMoveEnabledCheckBox.IsChecked = _effect.LbMouseMoveEnabled;
            LeftClickEnabledCheckBox.IsChecked = _effect.LbLeftClickEnabled;
            RightClickEnabledCheckBox.IsChecked = _effect.LbRightClickEnabled;

            // Move trigger
            DistanceThresholdSlider.Value = _effect.MoveDistanceThreshold;
            DistanceThresholdValue.Text = $"{_effect.MoveDistanceThreshold:F0} px";
            RandomDistanceCheckBox.IsChecked = _effect.RandomDistanceEnabled;
            MoveDirectionModeCombo.SelectedIndex = (int)_effect.MoveDirectionMode;

            // Click trigger
            ClickDirectionModeCombo.SelectedIndex = (int)_effect.ClickDirectionMode;
            SpreadAngleSlider.Value = _effect.SpreadAngle;
            SpreadAngleValue.Text = $"{_effect.SpreadAngle:F0} deg";

            // Core settings
            CoreEnabledCheckBox.IsChecked = _effect.CoreEnabled;
            CoreRadiusSlider.Value = _effect.CoreRadius;
            CoreRadiusValue.Text = $"{_effect.CoreRadius:F0} px";
            UpdateColorPreview(CoreColorPreview, _effect.CoreColor);

            // Bolt count
            RandomBoltCountCheckBox.IsChecked = _effect.RandomBoltCount;
            MinBoltCountSlider.Value = _effect.MinBoltCount;
            MinBoltCountValue.Text = _effect.MinBoltCount.ToString();
            MaxBoltCountSlider.Value = _effect.MaxBoltCount;
            MaxBoltCountValue.Text = _effect.MaxBoltCount.ToString();
            FixedBoltCountSlider.Value = _effect.FixedBoltCount;
            FixedBoltCountValue.Text = _effect.FixedBoltCount.ToString();
            UpdateBoltCountVisibility(_effect.RandomBoltCount);

            // Bolt appearance
            MinBoltLengthSlider.Value = _effect.MinBoltLength;
            MinBoltLengthValue.Text = $"{_effect.MinBoltLength:F0} px";
            MaxBoltLengthSlider.Value = _effect.MaxBoltLength;
            MaxBoltLengthValue.Text = $"{_effect.MaxBoltLength:F0} px";
            BoltThicknessSlider.Value = _effect.BoltThickness;
            BoltThicknessValue.Text = $"{_effect.BoltThickness:F1}";
            BranchProbabilitySlider.Value = _effect.BranchProbability;
            BranchProbabilityValue.Text = $"{_effect.BranchProbability * 100:F0}%";

            // Colors
            UpdateColorPreview(GlowColorPreview, _effect.GlowColor);
            RandomColorVariationCheckBox.IsChecked = _effect.RandomColorVariation;
            RainbowModeCheckBox.IsChecked = _effect.RainbowMode;
            RainbowSpeedSlider.Value = _effect.RainbowSpeed;
            RainbowSpeedValue.Text = $"{_effect.RainbowSpeed:F1}";
            RainbowSpeedPanel.Visibility = _effect.RainbowMode ? Visibility.Visible : Visibility.Collapsed;

            // Timing
            BoltLifetimeSlider.Value = _effect.BoltLifetime;
            BoltLifetimeValue.Text = $"{_effect.BoltLifetime:F2} s";
            FlickerSpeedSlider.Value = _effect.FlickerSpeed;
            FlickerSpeedValue.Text = $"{_effect.FlickerSpeed:F0}";
            FadeDurationSlider.Value = _effect.FadeDuration;
            FadeDurationValue.Text = $"{_effect.FadeDuration:F2} s";

            // Glow
            GlowIntensitySlider.Value = _effect.GlowIntensity;
            GlowIntensityValue.Text = $"{_effect.GlowIntensity:F1}";

            // Performance
            MaxActiveBoltsSlider.Value = _effect.MaxActiveBolts;
            MaxActiveBoltsValue.Text = _effect.MaxActiveBolts.ToString();
            MaxBoltsPerSecondSlider.Value = _effect.MaxBoltsPerSecond;
            MaxBoltsPerSecondValue.Text = _effect.MaxBoltsPerSecond.ToString();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateColorPreview(Border preview, Vector4 color)
    {
        preview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255)));
    }

    private void UpdateBoltCountVisibility(bool isRandom)
    {
        RandomBoltCountPanel.Visibility = isRandom ? Visibility.Visible : Visibility.Collapsed;
        FixedBoltCountPanel.Visibility = isRandom ? Visibility.Collapsed : Visibility.Visible;
    }

    // ===== Trigger Settings =====
    private void MouseMoveEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LbMouseMoveEnabled = MouseMoveEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("lb_mouseMoveEnabled", _effect.LbMouseMoveEnabled);
    }

    private void LeftClickEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LbLeftClickEnabled = LeftClickEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("lb_leftClickEnabled", _effect.LbLeftClickEnabled);
    }

    private void RightClickEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LbRightClickEnabled = RightClickEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("lb_rightClickEnabled", _effect.LbRightClickEnabled);
    }

    // ===== Move Trigger =====
    private void DistanceThresholdSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MoveDistanceThreshold = (float)DistanceThresholdSlider.Value;
        _effect.Configuration.Set("mt_distanceThreshold", _effect.MoveDistanceThreshold);
        DistanceThresholdValue.Text = $"{_effect.MoveDistanceThreshold:F0} px";
    }

    private void RandomDistanceCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomDistanceEnabled = RandomDistanceCheckBox.IsChecked == true;
        _effect.Configuration.Set("mt_randomDistanceEnabled", _effect.RandomDistanceEnabled);
    }

    private void MoveDirectionModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MoveDirectionMode = (DirectionMode)MoveDirectionModeCombo.SelectedIndex;
        _effect.Configuration.Set("mt_directionMode", (int)_effect.MoveDirectionMode);
    }

    // ===== Click Trigger =====
    private void ClickDirectionModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ClickDirectionMode = (DirectionMode)ClickDirectionModeCombo.SelectedIndex;
        _effect.Configuration.Set("ct_directionMode", (int)_effect.ClickDirectionMode);
    }

    private void SpreadAngleSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpreadAngle = (float)SpreadAngleSlider.Value;
        _effect.Configuration.Set("ct_spreadAngle", _effect.SpreadAngle);
        SpreadAngleValue.Text = $"{_effect.SpreadAngle:F0} deg";
    }

    // ===== Core Settings =====
    private void CoreEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.CoreEnabled = CoreEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("core_enabled", _effect.CoreEnabled);
    }

    private void CoreRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.CoreRadius = (float)CoreRadiusSlider.Value;
        _effect.Configuration.Set("core_radius", _effect.CoreRadius);
        CoreRadiusValue.Text = $"{_effect.CoreRadius:F0} px";
    }

    private void CoreColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_effect.CoreColor.X * 255),
            (int)(_effect.CoreColor.Y * 255),
            (int)(_effect.CoreColor.Z * 255));

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _effect.CoreColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1f);
            _effect.Configuration.Set("core_color", _effect.CoreColor);
            UpdateColorPreview(CoreColorPreview, _effect.CoreColor);
        }
    }

    // ===== Bolt Count =====
    private void RandomBoltCountCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomBoltCount = RandomBoltCountCheckBox.IsChecked == true;
        _effect.Configuration.Set("bc_randomCount", _effect.RandomBoltCount);
        UpdateBoltCountVisibility(_effect.RandomBoltCount);
    }

    private void MinBoltCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MinBoltCount = (int)MinBoltCountSlider.Value;
        _effect.Configuration.Set("bc_minCount", _effect.MinBoltCount);
        MinBoltCountValue.Text = _effect.MinBoltCount.ToString();
    }

    private void MaxBoltCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxBoltCount = (int)MaxBoltCountSlider.Value;
        _effect.Configuration.Set("bc_maxCount", _effect.MaxBoltCount);
        MaxBoltCountValue.Text = _effect.MaxBoltCount.ToString();
    }

    private void FixedBoltCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FixedBoltCount = (int)FixedBoltCountSlider.Value;
        _effect.Configuration.Set("bc_fixedCount", _effect.FixedBoltCount);
        FixedBoltCountValue.Text = _effect.FixedBoltCount.ToString();
    }

    // ===== Bolt Appearance =====
    private void MinBoltLengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MinBoltLength = (float)MinBoltLengthSlider.Value;
        _effect.Configuration.Set("ba_minLength", _effect.MinBoltLength);
        MinBoltLengthValue.Text = $"{_effect.MinBoltLength:F0} px";
    }

    private void MaxBoltLengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxBoltLength = (float)MaxBoltLengthSlider.Value;
        _effect.Configuration.Set("ba_maxLength", _effect.MaxBoltLength);
        MaxBoltLengthValue.Text = $"{_effect.MaxBoltLength:F0} px";
    }

    private void BoltThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.BoltThickness = (float)BoltThicknessSlider.Value;
        _effect.Configuration.Set("ba_thickness", _effect.BoltThickness);
        BoltThicknessValue.Text = $"{_effect.BoltThickness:F1}";
    }

    private void BranchProbabilitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.BranchProbability = (float)BranchProbabilitySlider.Value;
        _effect.Configuration.Set("ba_branchProbability", _effect.BranchProbability);
        BranchProbabilityValue.Text = $"{_effect.BranchProbability * 100:F0}%";
    }

    // ===== Colors =====
    private void GlowColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_effect.GlowColor.X * 255),
            (int)(_effect.GlowColor.Y * 255),
            (int)(_effect.GlowColor.Z * 255));

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _effect.GlowColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1f);
            _effect.Configuration.Set("col_glow", _effect.GlowColor);
            UpdateColorPreview(GlowColorPreview, _effect.GlowColor);
        }
    }

    private void RandomColorVariationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomColorVariation = RandomColorVariationCheckBox.IsChecked == true;
        _effect.Configuration.Set("col_randomVariation", _effect.RandomColorVariation);
    }

    private void RainbowModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowMode = RainbowModeCheckBox.IsChecked == true;
        _effect.Configuration.Set("col_rainbowMode", _effect.RainbowMode);
        RainbowSpeedPanel.Visibility = _effect.RainbowMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RainbowSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("col_rainbowSpeed", _effect.RainbowSpeed);
        RainbowSpeedValue.Text = $"{_effect.RainbowSpeed:F1}";
    }

    // ===== Timing =====
    private void BoltLifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.BoltLifetime = (float)BoltLifetimeSlider.Value;
        _effect.Configuration.Set("time_boltLifetime", _effect.BoltLifetime);
        BoltLifetimeValue.Text = $"{_effect.BoltLifetime:F2} s";
    }

    private void FlickerSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FlickerSpeed = (float)FlickerSpeedSlider.Value;
        _effect.Configuration.Set("time_flickerSpeed", _effect.FlickerSpeed);
        FlickerSpeedValue.Text = $"{_effect.FlickerSpeed:F0}";
    }

    private void FadeDurationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.FadeDuration = (float)FadeDurationSlider.Value;
        _effect.Configuration.Set("time_fadeDuration", _effect.FadeDuration);
        FadeDurationValue.Text = $"{_effect.FadeDuration:F2} s";
    }

    // ===== Glow =====
    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("glow_intensity", _effect.GlowIntensity);
        GlowIntensityValue.Text = $"{_effect.GlowIntensity:F1}";
    }

    // ===== Performance =====
    private void MaxActiveBoltsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxActiveBolts = (int)MaxActiveBoltsSlider.Value;
        _effect.Configuration.Set("perf_maxActiveBolts", _effect.MaxActiveBolts);
        MaxActiveBoltsValue.Text = _effect.MaxActiveBolts.ToString();
    }

    private void MaxBoltsPerSecondSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxBoltsPerSecond = (int)MaxBoltsPerSecondSlider.Value;
        _effect.Configuration.Set("perf_maxBoltsPerSecond", _effect.MaxBoltsPerSecond);
        MaxBoltsPerSecondValue.Text = _effect.MaxBoltsPerSecond.ToString();
    }
}
