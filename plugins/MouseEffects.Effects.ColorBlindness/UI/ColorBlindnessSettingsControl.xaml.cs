using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.ColorBlindness.UI;

public partial class ColorBlindnessSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private readonly ColorBlindnessEffect? _colorBlindnessEffect;
    private bool _isInitializing = true;
    private bool _isExpanded;

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    // RGB Matrix presets (3x3 matrices stored as 9 floats: R0,R1,R2, G0,G1,G2, B0,B1,B2)
    // Preset indices match combo box order (0=Normal, 1=Protanopia, etc.)
    private static readonly Dictionary<int, float[]> MatrixPresets = new()
    {
        // 0: Normal Vision (Identity)
        [0] = [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f],
        // 1: Protanopia (simulation matrix - for custom experimentation)
        [1] = [0.567f, 0.433f, 0f, 0.558f, 0.442f, 0f, 0f, 0.242f, 0.758f],
        // 2: Protanomaly
        [2] = [0.817f, 0.183f, 0f, 0.333f, 0.667f, 0f, 0f, 0.125f, 0.875f],
        // 3: Deuteranopia
        [3] = [0.625f, 0.375f, 0f, 0.7f, 0.3f, 0f, 0f, 0.3f, 0.7f],
        // 4: Deuteranomaly
        [4] = [0.8f, 0.2f, 0f, 0.258f, 0.742f, 0f, 0f, 0.142f, 0.858f],
        // 5: Tritanopia
        [5] = [0.95f, 0.05f, 0f, 0f, 0.433f, 0.567f, 0f, 0.475f, 0.525f],
        // 6: Tritanomaly
        [6] = [0.967f, 0.033f, 0f, 0f, 0.733f, 0.267f, 0f, 0.183f, 0.817f],
        // 7: Achromatopsia (grayscale)
        [7] = [0.299f, 0.587f, 0.114f, 0.299f, 0.587f, 0.114f, 0.299f, 0.587f, 0.114f],
        // 8: Achromatomaly
        [8] = [0.618f, 0.320f, 0.062f, 0.163f, 0.775f, 0.062f, 0.163f, 0.320f, 0.516f],
        // 9: Grayscale (luminance)
        [9] = [0.2126f, 0.7152f, 0.0722f, 0.2126f, 0.7152f, 0.0722f, 0.2126f, 0.7152f, 0.0722f],
        // 10: Sepia
        [10] = [0.393f, 0.769f, 0.189f, 0.349f, 0.686f, 0.168f, 0.272f, 0.534f, 0.131f]
        // 11: Custom Matrix - no preset, uses current values
    };

    public ColorBlindnessSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        _colorBlindnessEffect = effect as ColorBlindnessEffect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        // Shape settings
        if (_effect.Configuration.TryGet("radius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("rectWidth", out float rectWidth))
        {
            RectWidthSlider.Value = rectWidth;
            RectWidthValue.Text = rectWidth.ToString("F0");
        }

        if (_effect.Configuration.TryGet("rectHeight", out float rectHeight))
        {
            RectHeightSlider.Value = rectHeight;
            RectHeightValue.Text = rectHeight.ToString("F0");
        }

        if (_effect.Configuration.TryGet("shapeMode", out int shapeMode))
        {
            ShapeModeCombo.SelectedIndex = shapeMode;
            UpdateShapeSettings(shapeMode);
        }

        if (_effect.Configuration.TryGet("edgeSoftness", out float edgeSoftness))
        {
            EdgeSoftnessSlider.Value = edgeSoftness;
            EdgeSoftnessValue.Text = edgeSoftness.ToString("F2");
        }

        // Correction method (0=LMS, 1=RGB)
        if (_effect.Configuration.TryGet("correctionMode", out int correctionMode))
        {
            CorrectionMethodCombo.SelectedIndex = correctionMode;
            UpdateCorrectionMethodPanels(correctionMode);
        }
        else
        {
            UpdateCorrectionMethodPanels(0); // Default to LMS
        }

        // LMS filter types
        if (_effect.Configuration.TryGet("lmsFilterType", out int lmsFilterType))
        {
            LMSFilterTypeCombo.SelectedIndex = lmsFilterType;
            LMSInsideFilterTypeCombo.SelectedIndex = lmsFilterType;
        }

        if (_effect.Configuration.TryGet("lmsOutsideFilterType", out int lmsOutsideFilterType))
        {
            LMSOutsideFilterTypeCombo.SelectedIndex = lmsOutsideFilterType;
        }

        // Load inside matrix values (fullscreen and shape inside share these)
        LoadMatrixValue("insideMatrixR0", InsideMatrixR0, 1f);
        LoadMatrixValue("insideMatrixR1", InsideMatrixR1, 0f);
        LoadMatrixValue("insideMatrixR2", InsideMatrixR2, 0f);
        LoadMatrixValue("insideMatrixG0", InsideMatrixG0, 0f);
        LoadMatrixValue("insideMatrixG1", InsideMatrixG1, 1f);
        LoadMatrixValue("insideMatrixG2", InsideMatrixG2, 0f);
        LoadMatrixValue("insideMatrixB0", InsideMatrixB0, 0f);
        LoadMatrixValue("insideMatrixB1", InsideMatrixB1, 0f);
        LoadMatrixValue("insideMatrixB2", InsideMatrixB2, 1f);

        // Also set shape inside matrix textboxes to same values
        ShapeInsideR0.Text = InsideMatrixR0.Text;
        ShapeInsideR1.Text = InsideMatrixR1.Text;
        ShapeInsideR2.Text = InsideMatrixR2.Text;
        ShapeInsideG0.Text = InsideMatrixG0.Text;
        ShapeInsideG1.Text = InsideMatrixG1.Text;
        ShapeInsideG2.Text = InsideMatrixG2.Text;
        ShapeInsideB0.Text = InsideMatrixB0.Text;
        ShapeInsideB1.Text = InsideMatrixB1.Text;
        ShapeInsideB2.Text = InsideMatrixB2.Text;

        // Load outside matrix values
        LoadMatrixValue("outsideMatrixR0", ShapeOutsideR0, 1f);
        LoadMatrixValue("outsideMatrixR1", ShapeOutsideR1, 0f);
        LoadMatrixValue("outsideMatrixR2", ShapeOutsideR2, 0f);
        LoadMatrixValue("outsideMatrixG0", ShapeOutsideG0, 0f);
        LoadMatrixValue("outsideMatrixG1", ShapeOutsideG1, 1f);
        LoadMatrixValue("outsideMatrixG2", ShapeOutsideG2, 0f);
        LoadMatrixValue("outsideMatrixB0", ShapeOutsideB0, 0f);
        LoadMatrixValue("outsideMatrixB1", ShapeOutsideB1, 0f);
        LoadMatrixValue("outsideMatrixB2", ShapeOutsideB2, 1f);

        // Adjustment settings
        if (_effect.Configuration.TryGet("intensity", out float intensity))
        {
            IntensitySlider.Value = intensity;
            IntensityValue.Text = intensity.ToString("F2");
        }

        if (_effect.Configuration.TryGet("colorBoost", out float colorBoost))
        {
            ColorBoostSlider.Value = colorBoost;
            ColorBoostValue.Text = colorBoost.ToString("F2");
        }

        // Curves
        if (_effect.Configuration.TryGet("enableCurves", out bool enableCurves))
        {
            EnableCurvesCheckBox.IsChecked = enableCurves;
        }

        if (_effect.Configuration.TryGet("curveStrength", out float curveStrength))
        {
            CurveStrengthSlider.Value = curveStrength;
            CurveStrengthValue.Text = curveStrength.ToString("F2");
        }

        // Load curves
        if (_colorBlindnessEffect != null)
        {
            CurveEditorControl.MasterCurve = _colorBlindnessEffect.MasterCurve;
            CurveEditorControl.RedCurve = _colorBlindnessEffect.RedCurve;
            CurveEditorControl.GreenCurve = _colorBlindnessEffect.GreenCurve;
            CurveEditorControl.BlueCurve = _colorBlindnessEffect.BlueCurve;
        }
    }

    private void LoadMatrixValue(string key, System.Windows.Controls.TextBox textBox, float defaultValue)
    {
        if (_effect.Configuration.TryGet(key, out float value))
            textBox.Text = value.ToString("F3", CultureInfo.InvariantCulture);
        else
            textBox.Text = defaultValue.ToString("F3", CultureInfo.InvariantCulture);
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();

        // Shape settings
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("rectWidth", (float)RectWidthSlider.Value);
        config.Set("rectHeight", (float)RectHeightSlider.Value);
        config.Set("shapeMode", ShapeModeCombo.SelectedIndex);
        config.Set("edgeSoftness", (float)EdgeSoftnessSlider.Value);

        // Correction mode (0=LMS, 1=RGB)
        int correctionMode = CorrectionMethodCombo.SelectedIndex;
        config.Set("correctionMode", correctionMode);

        // LMS filter types
        int shapeMode = ShapeModeCombo.SelectedIndex;
        if (shapeMode == 2) // Fullscreen
        {
            config.Set("lmsFilterType", LMSFilterTypeCombo.SelectedIndex);
            config.Set("lmsOutsideFilterType", 0); // Not used in fullscreen
        }
        else // Circle or Rectangle
        {
            config.Set("lmsFilterType", LMSInsideFilterTypeCombo.SelectedIndex);
            config.Set("lmsOutsideFilterType", LMSOutsideFilterTypeCombo.SelectedIndex);
        }

        // Inside matrix values (from fullscreen or shape inside based on current mode)
        if (shapeMode == 2)
        {
            config.Set("insideMatrixR0", ParseFloat(InsideMatrixR0.Text, 1f));
            config.Set("insideMatrixR1", ParseFloat(InsideMatrixR1.Text, 0f));
            config.Set("insideMatrixR2", ParseFloat(InsideMatrixR2.Text, 0f));
            config.Set("insideMatrixG0", ParseFloat(InsideMatrixG0.Text, 0f));
            config.Set("insideMatrixG1", ParseFloat(InsideMatrixG1.Text, 1f));
            config.Set("insideMatrixG2", ParseFloat(InsideMatrixG2.Text, 0f));
            config.Set("insideMatrixB0", ParseFloat(InsideMatrixB0.Text, 0f));
            config.Set("insideMatrixB1", ParseFloat(InsideMatrixB1.Text, 0f));
            config.Set("insideMatrixB2", ParseFloat(InsideMatrixB2.Text, 1f));
        }
        else
        {
            config.Set("insideMatrixR0", ParseFloat(ShapeInsideR0.Text, 1f));
            config.Set("insideMatrixR1", ParseFloat(ShapeInsideR1.Text, 0f));
            config.Set("insideMatrixR2", ParseFloat(ShapeInsideR2.Text, 0f));
            config.Set("insideMatrixG0", ParseFloat(ShapeInsideG0.Text, 0f));
            config.Set("insideMatrixG1", ParseFloat(ShapeInsideG1.Text, 1f));
            config.Set("insideMatrixG2", ParseFloat(ShapeInsideG2.Text, 0f));
            config.Set("insideMatrixB0", ParseFloat(ShapeInsideB0.Text, 0f));
            config.Set("insideMatrixB1", ParseFloat(ShapeInsideB1.Text, 0f));
            config.Set("insideMatrixB2", ParseFloat(ShapeInsideB2.Text, 1f));
        }

        // Outside matrix values
        config.Set("outsideMatrixR0", ParseFloat(ShapeOutsideR0.Text, 1f));
        config.Set("outsideMatrixR1", ParseFloat(ShapeOutsideR1.Text, 0f));
        config.Set("outsideMatrixR2", ParseFloat(ShapeOutsideR2.Text, 0f));
        config.Set("outsideMatrixG0", ParseFloat(ShapeOutsideG0.Text, 0f));
        config.Set("outsideMatrixG1", ParseFloat(ShapeOutsideG1.Text, 1f));
        config.Set("outsideMatrixG2", ParseFloat(ShapeOutsideG2.Text, 0f));
        config.Set("outsideMatrixB0", ParseFloat(ShapeOutsideB0.Text, 0f));
        config.Set("outsideMatrixB1", ParseFloat(ShapeOutsideB1.Text, 0f));
        config.Set("outsideMatrixB2", ParseFloat(ShapeOutsideB2.Text, 1f));

        // Adjustment settings
        config.Set("intensity", (float)IntensitySlider.Value);
        config.Set("colorBoost", (float)ColorBoostSlider.Value);

        // Curves
        config.Set("enableCurves", EnableCurvesCheckBox.IsChecked ?? false);
        config.Set("curveStrength", (float)CurveStrengthSlider.Value);
        config.Set("masterCurve", CurveEditorControl.MasterCurve.ToJson());
        config.Set("redCurve", CurveEditorControl.RedCurve.ToJson());
        config.Set("greenCurve", CurveEditorControl.GreenCurve.ToJson());
        config.Set("blueCurve", CurveEditorControl.BlueCurve.ToJson());

        _effect.Configure(config);

        // Update effect curves directly
        if (_colorBlindnessEffect != null)
        {
            _colorBlindnessEffect.MasterCurve = CurveEditorControl.MasterCurve;
            _colorBlindnessEffect.RedCurve = CurveEditorControl.RedCurve;
            _colorBlindnessEffect.GreenCurve = CurveEditorControl.GreenCurve;
            _colorBlindnessEffect.BlueCurve = CurveEditorControl.BlueCurve;
            _colorBlindnessEffect.InvalidateCurves();
        }

        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private static float ParseFloat(string text, float defaultValue)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            return value;
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            return value;
        return defaultValue;
    }

    private void UpdateShapeSettings(int shapeMode)
    {
        // Shape size controls
        if (CircleSettings != null)
            CircleSettings.Visibility = shapeMode == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (RectangleSettings != null)
            RectangleSettings.Visibility = shapeMode == 1 ? Visibility.Visible : Visibility.Collapsed;

        // Hide edge softness for fullscreen mode
        if (EdgeSoftnessSlider != null)
            EdgeSoftnessSlider.IsEnabled = shapeMode != 2;

        // Update LMS panels
        if (LMSFullscreenPanel != null)
            LMSFullscreenPanel.Visibility = shapeMode == 2 ? Visibility.Visible : Visibility.Collapsed;
        if (LMSShapePanel != null)
            LMSShapePanel.Visibility = shapeMode != 2 ? Visibility.Visible : Visibility.Collapsed;

        // Update RGB panels
        if (RGBFullscreenPanel != null)
            RGBFullscreenPanel.Visibility = shapeMode == 2 ? Visibility.Visible : Visibility.Collapsed;
        if (RGBShapePanel != null)
            RGBShapePanel.Visibility = shapeMode != 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateCorrectionMethodPanels(int correctionMode)
    {
        // 0 = LMS Correction, 1 = RGB Matrix
        if (LMSSettingsPanel != null)
            LMSSettingsPanel.Visibility = correctionMode == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (RGBSettingsPanel != null)
            RGBSettingsPanel.Visibility = correctionMode == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetMatrixTextBoxesFromPreset(int presetIndex,
        System.Windows.Controls.TextBox r0, System.Windows.Controls.TextBox r1, System.Windows.Controls.TextBox r2,
        System.Windows.Controls.TextBox g0, System.Windows.Controls.TextBox g1, System.Windows.Controls.TextBox g2,
        System.Windows.Controls.TextBox b0, System.Windows.Controls.TextBox b1, System.Windows.Controls.TextBox b2)
    {
        if (!MatrixPresets.TryGetValue(presetIndex, out var preset))
            return;

        _isInitializing = true;
        r0.Text = preset[0].ToString("F3", CultureInfo.InvariantCulture);
        r1.Text = preset[1].ToString("F3", CultureInfo.InvariantCulture);
        r2.Text = preset[2].ToString("F3", CultureInfo.InvariantCulture);
        g0.Text = preset[3].ToString("F3", CultureInfo.InvariantCulture);
        g1.Text = preset[4].ToString("F3", CultureInfo.InvariantCulture);
        g2.Text = preset[5].ToString("F3", CultureInfo.InvariantCulture);
        b0.Text = preset[6].ToString("F3", CultureInfo.InvariantCulture);
        b1.Text = preset[7].ToString("F3", CultureInfo.InvariantCulture);
        b2.Text = preset[8].ToString("F3", CultureInfo.InvariantCulture);
        _isInitializing = false;

        UpdateConfiguration();
    }

    #region Event Handlers

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

    private void ShapeModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int newShapeMode = ShapeModeCombo.SelectedIndex;

        // Sync LMS filter values when switching between fullscreen and shape modes
        if (newShapeMode == 2)
        {
            // Switching to fullscreen: copy inside filter to fullscreen filter
            if (LMSFilterTypeCombo != null && LMSInsideFilterTypeCombo != null)
                LMSFilterTypeCombo.SelectedIndex = LMSInsideFilterTypeCombo.SelectedIndex;
            if (RGBPresetCombo != null && RGBInsidePresetCombo != null)
                RGBPresetCombo.SelectedIndex = RGBInsidePresetCombo.SelectedIndex;
        }
        else
        {
            // Switching to shape mode: copy fullscreen filter to inside filter
            if (LMSFilterTypeCombo != null && LMSInsideFilterTypeCombo != null)
                LMSInsideFilterTypeCombo.SelectedIndex = LMSFilterTypeCombo.SelectedIndex;
            if (RGBPresetCombo != null && RGBInsidePresetCombo != null)
                RGBInsidePresetCombo.SelectedIndex = RGBPresetCombo.SelectedIndex;
        }

        UpdateShapeSettings(newShapeMode);
        UpdateConfiguration();
    }

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RectWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RectHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void EdgeSoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeSoftnessValue != null)
            EdgeSoftnessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void CorrectionMethodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int correctionMode = CorrectionMethodCombo.SelectedIndex;
        UpdateCorrectionMethodPanels(correctionMode);
        UpdateConfiguration();
    }

    // LMS Filter Type handlers
    private void LMSFilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void LMSInsideFilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void LMSOutsideFilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    // RGB Preset handlers - just load preset values into matrix
    private void RGBPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int selectedIndex = RGBPresetCombo.SelectedIndex;
        if (MatrixPresets.ContainsKey(selectedIndex))
        {
            SetMatrixTextBoxesFromPreset(selectedIndex,
                InsideMatrixR0, InsideMatrixR1, InsideMatrixR2,
                InsideMatrixG0, InsideMatrixG1, InsideMatrixG2,
                InsideMatrixB0, InsideMatrixB1, InsideMatrixB2);
        }
    }

    private void RGBInsidePresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int selectedIndex = RGBInsidePresetCombo.SelectedIndex;
        if (MatrixPresets.ContainsKey(selectedIndex))
        {
            SetMatrixTextBoxesFromPreset(selectedIndex,
                ShapeInsideR0, ShapeInsideR1, ShapeInsideR2,
                ShapeInsideG0, ShapeInsideG1, ShapeInsideG2,
                ShapeInsideB0, ShapeInsideB1, ShapeInsideB2);
        }
    }

    private void RGBOutsidePresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int selectedIndex = RGBOutsidePresetCombo.SelectedIndex;
        if (MatrixPresets.ContainsKey(selectedIndex))
        {
            SetMatrixTextBoxesFromPreset(selectedIndex,
                ShapeOutsideR0, ShapeOutsideR1, ShapeOutsideR2,
                ShapeOutsideG0, ShapeOutsideG1, ShapeOutsideG2,
                ShapeOutsideB0, ShapeOutsideB1, ShapeOutsideB2);
        }
    }

    // Matrix text changed handlers
    private void InsideMatrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ShapeInsideMatrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ShapeOutsideMatrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    // Reset buttons - reset matrix to identity
    private void ResetInsideMatrixButton_Click(object sender, RoutedEventArgs e)
    {
        SetMatrixTextBoxesFromPreset(0, // Identity matrix
            InsideMatrixR0, InsideMatrixR1, InsideMatrixR2,
            InsideMatrixG0, InsideMatrixG1, InsideMatrixG2,
            InsideMatrixB0, InsideMatrixB1, InsideMatrixB2);
    }

    private void ResetShapeInsideMatrixButton_Click(object sender, RoutedEventArgs e)
    {
        SetMatrixTextBoxesFromPreset(0, // Identity matrix
            ShapeInsideR0, ShapeInsideR1, ShapeInsideR2,
            ShapeInsideG0, ShapeInsideG1, ShapeInsideG2,
            ShapeInsideB0, ShapeInsideB1, ShapeInsideB2);
    }

    private void ResetShapeOutsideMatrixButton_Click(object sender, RoutedEventArgs e)
    {
        SetMatrixTextBoxesFromPreset(0, // Identity matrix
            ShapeOutsideR0, ShapeOutsideR1, ShapeOutsideR2,
            ShapeOutsideG0, ShapeOutsideG1, ShapeOutsideG2,
            ShapeOutsideB0, ShapeOutsideB1, ShapeOutsideB2);
    }

    // Adjustment sliders
    private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntensityValue != null)
            IntensityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ColorBoostSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ColorBoostValue != null)
            ColorBoostValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    // Curves
    private void EnableCurvesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void CurveStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CurveStrengthValue != null)
            CurveStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void CurveEditorControl_CurveChanged(object? sender, EventArgs e)
    {
        UpdateConfiguration();
    }

    #endregion
}
