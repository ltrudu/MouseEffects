using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MouseEffects.Effects.Tesla.UI.Effects;

public partial class ElectricalFollowSettings : System.Windows.Controls.UserControl
{
    private TeslaEffect? _effect;
    private bool _isLoading;

    public ElectricalFollowSettings()
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
            MouseMoveEnabledCheckBox.IsChecked = _effect.EfMouseMoveEnabled;
            DistanceThresholdSlider.Value = _effect.EfPieceSize;
            DistanceThresholdValue.Text = $"{_effect.EfPieceSize:F0} px";

            // Trail general
            MaxPointsSlider.Value = _effect.EfMaxPieces;
            MaxPointsValue.Text = _effect.EfMaxPieces.ToString();
            LifetimeSlider.Value = _effect.EfLifetime;
            LifetimeValue.Text = $"{_effect.EfLifetime:F1} s";
            RandomLifetimeCheckBox.IsChecked = _effect.EfRandomLifetime;
            MinLifetimeSlider.Value = _effect.EfMinLifetime;
            MinLifetimeValue.Text = $"{_effect.EfMinLifetime:F1} s";
            MaxLifetimeSlider.Value = _effect.EfMaxLifetime;
            MaxLifetimeValue.Text = $"{_effect.EfMaxLifetime:F1} s";
            RandomLifetimePanel.Visibility = _effect.EfRandomLifetime ? Visibility.Visible : Visibility.Collapsed;

            // Trail appearance
            LineThicknessSlider.Value = _effect.EfLineThickness;
            LineThicknessValue.Text = $"{_effect.EfLineThickness:F1}";
            RandomThicknessCheckBox.IsChecked = _effect.EfRandomThickness;
            MinThicknessSlider.Value = _effect.EfMinThickness;
            MinThicknessValue.Text = $"{_effect.EfMinThickness:F1}";
            MaxThicknessSlider.Value = _effect.EfMaxThickness;
            MaxThicknessValue.Text = $"{_effect.EfMaxThickness:F1}";
            RandomThicknessPanel.Visibility = _effect.EfRandomThickness ? Visibility.Visible : Visibility.Collapsed;
            GlowIntensitySlider.Value = _effect.EfGlowIntensity;
            GlowIntensityValue.Text = $"{_effect.EfGlowIntensity:F1}";

            // Trail flicker
            FlickerSpeedSlider.Value = _effect.EfFlickerSpeed;
            FlickerSpeedValue.Text = $"{_effect.EfFlickerSpeed:F0}";
            FlickerIntensitySlider.Value = _effect.EfFlickerIntensity;
            FlickerIntensityValue.Text = $"{_effect.EfFlickerIntensity:F1}";

            // Trail crackle
            CrackleIntensitySlider.Value = _effect.EfCrackleIntensity;
            CrackleIntensityValue.Text = $"{_effect.EfCrackleIntensity:F1}";
            NoiseScaleSlider.Value = _effect.EfNoiseScale;
            NoiseScaleValue.Text = $"{_effect.EfNoiseScale:F1}";

            // Branch bolts
            BranchBoltEnabledCheckBox.IsChecked = _effect.EfBranchBoltEnabled;
            BranchBoltPanel.Visibility = _effect.EfBranchBoltEnabled ? Visibility.Visible : Visibility.Collapsed;
            BranchCountSlider.Value = _effect.EfBranchBoltCount;
            BranchCountValue.Text = _effect.EfBranchBoltCount.ToString();
            RandomBranchCountCheckBox.IsChecked = _effect.EfRandomBranchCount;
            MinBranchCountSlider.Value = _effect.EfMinBranchCount;
            MinBranchCountValue.Text = _effect.EfMinBranchCount.ToString();
            MaxBranchCountSlider.Value = _effect.EfMaxBranchCount;
            MaxBranchCountValue.Text = _effect.EfMaxBranchCount.ToString();
            RandomBranchCountPanel.Visibility = _effect.EfRandomBranchCount ? Visibility.Visible : Visibility.Collapsed;
            BranchLengthSlider.Value = _effect.EfBranchBoltLength;
            BranchLengthValue.Text = $"{_effect.EfBranchBoltLength:F0} px";
            BranchThicknessSlider.Value = _effect.EfBranchBoltThickness;
            BranchThicknessValue.Text = $"{_effect.EfBranchBoltThickness:F1}";
            BranchSpreadSlider.Value = _effect.EfBranchBoltSpread;
            BranchSpreadValue.Text = $"{_effect.EfBranchBoltSpread:F0} deg";
            UpdateColorPreview(BranchColorPreview, _effect.EfBranchBoltColor);
            UseBranchColorCheckBox.IsChecked = _effect.EfBranchBoltColor.W > 0.5f;

            // Sparkles
            SparkleEnabledCheckBox.IsChecked = _effect.EfSparkleEnabled;
            SparklePanel.Visibility = _effect.EfSparkleEnabled ? Visibility.Visible : Visibility.Collapsed;
            SparkleCountSlider.Value = _effect.EfSparkleCount;
            SparkleCountValue.Text = _effect.EfSparkleCount.ToString();
            RandomSparkleCountCheckBox.IsChecked = _effect.EfRandomSparkleCount;
            MinSparkleCountSlider.Value = _effect.EfMinSparkleCount;
            MinSparkleCountValue.Text = _effect.EfMinSparkleCount.ToString();
            MaxSparkleCountSlider.Value = _effect.EfMaxSparkleCount;
            MaxSparkleCountValue.Text = _effect.EfMaxSparkleCount.ToString();
            RandomSparkleCountPanel.Visibility = _effect.EfRandomSparkleCount ? Visibility.Visible : Visibility.Collapsed;
            SparkleSizeSlider.Value = _effect.EfSparkleSize;
            SparkleSizeValue.Text = $"{_effect.EfSparkleSize:F0} px";
            SparkleIntensitySlider.Value = _effect.EfSparkleIntensity;
            SparkleIntensityValue.Text = $"{_effect.EfSparkleIntensity:F1}";
            UpdateColorPreview(SparkleColorPreview, _effect.EfSparkleColor);
            UseSparkleColorCheckBox.IsChecked = _effect.EfSparkleColor.W > 0.5f;

            // Trail colors
            UpdateColorPreview(PrimaryColorPreview, _effect.EfPrimaryColor);
            UpdateColorPreview(SecondaryColorPreview, _effect.EfSecondaryColor);
            RandomColorVariationCheckBox.IsChecked = _effect.EfRandomColorVariation;
            RainbowModeCheckBox.IsChecked = _effect.EfRainbowMode;
            RainbowSpeedSlider.Value = _effect.EfRainbowSpeed;
            RainbowSpeedValue.Text = $"{_effect.EfRainbowSpeed:F1}";
            RainbowSpeedPanel.Visibility = _effect.EfRainbowMode ? Visibility.Visible : Visibility.Collapsed;
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

    // ===== Trigger Settings =====
    private void MouseMoveEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMouseMoveEnabled = MouseMoveEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_mouseMoveEnabled", _effect.EfMouseMoveEnabled);
    }

    private void DistanceThresholdSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfPieceSize = (float)DistanceThresholdSlider.Value;
        _effect.Configuration.Set("ef_pieceSize", _effect.EfPieceSize);
        DistanceThresholdValue.Text = $"{_effect.EfPieceSize:F0} px";
    }

    // ===== Trail General =====
    private void MaxPointsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMaxPieces = (int)MaxPointsSlider.Value;
        _effect.Configuration.Set("ef_maxPieces", _effect.EfMaxPieces);
        MaxPointsValue.Text = _effect.EfMaxPieces.ToString();
    }

    private void LifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfLifetime = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("ef_lifetime", _effect.EfLifetime);
        LifetimeValue.Text = $"{_effect.EfLifetime:F1} s";
    }

    private void RandomLifetimeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfRandomLifetime = RandomLifetimeCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_randomLifetime", _effect.EfRandomLifetime);
        RandomLifetimePanel.Visibility = _effect.EfRandomLifetime ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MinLifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMinLifetime = (float)MinLifetimeSlider.Value;
        _effect.Configuration.Set("ef_minLifetime", _effect.EfMinLifetime);
        MinLifetimeValue.Text = $"{_effect.EfMinLifetime:F1} s";
    }

    private void MaxLifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMaxLifetime = (float)MaxLifetimeSlider.Value;
        _effect.Configuration.Set("ef_maxLifetime", _effect.EfMaxLifetime);
        MaxLifetimeValue.Text = $"{_effect.EfMaxLifetime:F1} s";
    }

    // ===== Trail Appearance =====
    private void LineThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfLineThickness = (float)LineThicknessSlider.Value;
        _effect.Configuration.Set("ef_lineThickness", _effect.EfLineThickness);
        LineThicknessValue.Text = $"{_effect.EfLineThickness:F1}";
    }

    private void RandomThicknessCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfRandomThickness = RandomThicknessCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_randomThickness", _effect.EfRandomThickness);
        RandomThicknessPanel.Visibility = _effect.EfRandomThickness ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MinThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMinThickness = (float)MinThicknessSlider.Value;
        _effect.Configuration.Set("ef_minThickness", _effect.EfMinThickness);
        MinThicknessValue.Text = $"{_effect.EfMinThickness:F1}";
    }

    private void MaxThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMaxThickness = (float)MaxThicknessSlider.Value;
        _effect.Configuration.Set("ef_maxThickness", _effect.EfMaxThickness);
        MaxThicknessValue.Text = $"{_effect.EfMaxThickness:F1}";
    }

    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfGlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("ef_glowIntensity", _effect.EfGlowIntensity);
        GlowIntensityValue.Text = $"{_effect.EfGlowIntensity:F1}";
    }

    // ===== Trail Flicker =====
    private void FlickerSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfFlickerSpeed = (float)FlickerSpeedSlider.Value;
        _effect.Configuration.Set("ef_flickerSpeed", _effect.EfFlickerSpeed);
        FlickerSpeedValue.Text = $"{_effect.EfFlickerSpeed:F0}";
    }

    private void FlickerIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfFlickerIntensity = (float)FlickerIntensitySlider.Value;
        _effect.Configuration.Set("ef_flickerIntensity", _effect.EfFlickerIntensity);
        FlickerIntensityValue.Text = $"{_effect.EfFlickerIntensity:F1}";
    }

    // ===== Trail Crackle =====
    private void CrackleIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfCrackleIntensity = (float)CrackleIntensitySlider.Value;
        _effect.Configuration.Set("ef_crackleIntensity", _effect.EfCrackleIntensity);
        CrackleIntensityValue.Text = $"{_effect.EfCrackleIntensity:F1}";
    }

    private void NoiseScaleSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfNoiseScale = (float)NoiseScaleSlider.Value;
        _effect.Configuration.Set("ef_noiseScale", _effect.EfNoiseScale);
        NoiseScaleValue.Text = $"{_effect.EfNoiseScale:F1}";
    }

    // ===== Branch Bolts =====
    private void BranchBoltEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfBranchBoltEnabled = BranchBoltEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_branchBoltEnabled", _effect.EfBranchBoltEnabled);
        BranchBoltPanel.Visibility = _effect.EfBranchBoltEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BranchCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfBranchBoltCount = (int)BranchCountSlider.Value;
        _effect.Configuration.Set("ef_branchBoltCount", _effect.EfBranchBoltCount);
        BranchCountValue.Text = _effect.EfBranchBoltCount.ToString();
    }

    private void RandomBranchCountCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfRandomBranchCount = RandomBranchCountCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_randomBranchCount", _effect.EfRandomBranchCount);
        RandomBranchCountPanel.Visibility = _effect.EfRandomBranchCount ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MinBranchCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMinBranchCount = (int)MinBranchCountSlider.Value;
        _effect.Configuration.Set("ef_minBranchCount", _effect.EfMinBranchCount);
        MinBranchCountValue.Text = _effect.EfMinBranchCount.ToString();
    }

    private void MaxBranchCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMaxBranchCount = (int)MaxBranchCountSlider.Value;
        _effect.Configuration.Set("ef_maxBranchCount", _effect.EfMaxBranchCount);
        MaxBranchCountValue.Text = _effect.EfMaxBranchCount.ToString();
    }

    private void BranchLengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfBranchBoltLength = (float)BranchLengthSlider.Value;
        _effect.Configuration.Set("ef_branchBoltLength", _effect.EfBranchBoltLength);
        BranchLengthValue.Text = $"{_effect.EfBranchBoltLength:F0} px";
    }

    private void BranchThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfBranchBoltThickness = (float)BranchThicknessSlider.Value;
        _effect.Configuration.Set("ef_branchBoltThickness", _effect.EfBranchBoltThickness);
        BranchThicknessValue.Text = $"{_effect.EfBranchBoltThickness:F1}";
    }

    private void BranchSpreadSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfBranchBoltSpread = (float)BranchSpreadSlider.Value;
        _effect.Configuration.Set("ef_branchBoltSpread", _effect.EfBranchBoltSpread);
        BranchSpreadValue.Text = $"{_effect.EfBranchBoltSpread:F0} deg";
    }

    private void BranchColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_effect.EfBranchBoltColor.X * 255),
            (int)(_effect.EfBranchBoltColor.Y * 255),
            (int)(_effect.EfBranchBoltColor.Z * 255));

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            float alpha = UseBranchColorCheckBox.IsChecked == true ? 1f : 0f;
            _effect.EfBranchBoltColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                alpha);
            _effect.Configuration.Set("ef_branchBoltColor", _effect.EfBranchBoltColor);
            UpdateColorPreview(BranchColorPreview, _effect.EfBranchBoltColor);
        }
    }

    private void UseBranchColorCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        float alpha = UseBranchColorCheckBox.IsChecked == true ? 1f : 0f;
        _effect.EfBranchBoltColor = new Vector4(
            _effect.EfBranchBoltColor.X,
            _effect.EfBranchBoltColor.Y,
            _effect.EfBranchBoltColor.Z,
            alpha);
        _effect.Configuration.Set("ef_branchBoltColor", _effect.EfBranchBoltColor);
    }

    // ===== Sparkles =====
    private void SparkleEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfSparkleEnabled = SparkleEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_sparkleEnabled", _effect.EfSparkleEnabled);
        SparklePanel.Visibility = _effect.EfSparkleEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SparkleCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfSparkleCount = (int)SparkleCountSlider.Value;
        _effect.Configuration.Set("ef_sparkleCount", _effect.EfSparkleCount);
        SparkleCountValue.Text = _effect.EfSparkleCount.ToString();
    }

    private void RandomSparkleCountCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfRandomSparkleCount = RandomSparkleCountCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_randomSparkleCount", _effect.EfRandomSparkleCount);
        RandomSparkleCountPanel.Visibility = _effect.EfRandomSparkleCount ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MinSparkleCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMinSparkleCount = (int)MinSparkleCountSlider.Value;
        _effect.Configuration.Set("ef_minSparkleCount", _effect.EfMinSparkleCount);
        MinSparkleCountValue.Text = _effect.EfMinSparkleCount.ToString();
    }

    private void MaxSparkleCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfMaxSparkleCount = (int)MaxSparkleCountSlider.Value;
        _effect.Configuration.Set("ef_maxSparkleCount", _effect.EfMaxSparkleCount);
        MaxSparkleCountValue.Text = _effect.EfMaxSparkleCount.ToString();
    }

    private void SparkleSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfSparkleSize = (float)SparkleSizeSlider.Value;
        _effect.Configuration.Set("ef_sparkleSize", _effect.EfSparkleSize);
        SparkleSizeValue.Text = $"{_effect.EfSparkleSize:F0} px";
    }

    private void SparkleIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfSparkleIntensity = (float)SparkleIntensitySlider.Value;
        _effect.Configuration.Set("ef_sparkleIntensity", _effect.EfSparkleIntensity);
        SparkleIntensityValue.Text = $"{_effect.EfSparkleIntensity:F1}";
    }

    private void SparkleColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_effect.EfSparkleColor.X * 255),
            (int)(_effect.EfSparkleColor.Y * 255),
            (int)(_effect.EfSparkleColor.Z * 255));

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            float alpha = UseSparkleColorCheckBox.IsChecked == true ? 1f : 0f;
            _effect.EfSparkleColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                alpha);
            _effect.Configuration.Set("ef_sparkleColor", _effect.EfSparkleColor);
            UpdateColorPreview(SparkleColorPreview, _effect.EfSparkleColor);
        }
    }

    private void UseSparkleColorCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        float alpha = UseSparkleColorCheckBox.IsChecked == true ? 1f : 0f;
        _effect.EfSparkleColor = new Vector4(
            _effect.EfSparkleColor.X,
            _effect.EfSparkleColor.Y,
            _effect.EfSparkleColor.Z,
            alpha);
        _effect.Configuration.Set("ef_sparkleColor", _effect.EfSparkleColor);
    }

    // ===== Trail Colors =====
    private void PrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_effect.EfPrimaryColor.X * 255),
            (int)(_effect.EfPrimaryColor.Y * 255),
            (int)(_effect.EfPrimaryColor.Z * 255));

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _effect.EfPrimaryColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1f);
            _effect.Configuration.Set("ef_primaryColor", _effect.EfPrimaryColor);
            UpdateColorPreview(PrimaryColorPreview, _effect.EfPrimaryColor);
        }
    }

    private void SecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect == null) return;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_effect.EfSecondaryColor.X * 255),
            (int)(_effect.EfSecondaryColor.Y * 255),
            (int)(_effect.EfSecondaryColor.Z * 255));

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _effect.EfSecondaryColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1f);
            _effect.Configuration.Set("ef_secondaryColor", _effect.EfSecondaryColor);
            UpdateColorPreview(SecondaryColorPreview, _effect.EfSecondaryColor);
        }
    }

    private void RandomColorVariationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfRandomColorVariation = RandomColorVariationCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_randomColorVariation", _effect.EfRandomColorVariation);
    }

    private void RainbowModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfRainbowMode = RainbowModeCheckBox.IsChecked == true;
        _effect.Configuration.Set("ef_rainbowMode", _effect.EfRainbowMode);
        RainbowSpeedPanel.Visibility = _effect.EfRainbowMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RainbowSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EfRainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("ef_rainbowSpeed", _effect.EfRainbowSpeed);
        RainbowSpeedValue.Text = $"{_effect.EfRainbowSpeed:F1}";
    }
}
