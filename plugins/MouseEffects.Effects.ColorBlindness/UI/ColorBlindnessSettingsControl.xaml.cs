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

    // Matrix presets (3x3 matrices stored as 9 floats: R0,R1,R2, G0,G1,G2, B0,B1,B2)
    private static readonly Dictionary<int, float[]> MatrixPresets = new()
    {
        // 0: Normal Vision (Identity)
        [0] = new[] { 1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f },
        // 1: Protanopia (simulation matrix - for custom experimentation)
        [1] = new[] { 0.567f, 0.433f, 0f, 0.558f, 0.442f, 0f, 0f, 0.242f, 0.758f },
        // 2: Protanomaly
        [2] = new[] { 0.817f, 0.183f, 0f, 0.333f, 0.667f, 0f, 0f, 0.125f, 0.875f },
        // 3: Deuteranopia
        [3] = new[] { 0.625f, 0.375f, 0f, 0.7f, 0.3f, 0f, 0f, 0.3f, 0.7f },
        // 4: Deuteranomaly
        [4] = new[] { 0.8f, 0.2f, 0f, 0.258f, 0.742f, 0f, 0f, 0.142f, 0.858f },
        // 5: Tritanopia
        [5] = new[] { 0.95f, 0.05f, 0f, 0f, 0.433f, 0.567f, 0f, 0.475f, 0.525f },
        // 6: Tritanomaly
        [6] = new[] { 0.967f, 0.033f, 0f, 0f, 0.733f, 0.267f, 0f, 0.183f, 0.817f },
        // 7: Achromatopsia (grayscale)
        [7] = new[] { 0.299f, 0.587f, 0.114f, 0.299f, 0.587f, 0.114f, 0.299f, 0.587f, 0.114f },
        // 8: Achromatomaly
        [8] = new[] { 0.618f, 0.320f, 0.062f, 0.163f, 0.775f, 0.062f, 0.163f, 0.320f, 0.516f },
        // 9: Grayscale (luminance)
        [9] = new[] { 0.2126f, 0.7152f, 0.0722f, 0.2126f, 0.7152f, 0.0722f, 0.2126f, 0.7152f, 0.0722f },
        // 10: Sepia
        [10] = new[] { 0.393f, 0.769f, 0.189f, 0.349f, 0.686f, 0.168f, 0.272f, 0.534f, 0.131f }
        // Note: Inversion cannot be represented with a 3x3 matrix (needs affine transform)
        // Use the "Inverted Grayscale" filter type instead for proper inversion
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

        if (_effect.Configuration.TryGet("filterType", out int filterType))
        {
            FilterTypeCombo.SelectedIndex = filterType;
            InsideFilterTypeCombo.SelectedIndex = filterType;
        }

        if (_effect.Configuration.TryGet("outsideFilterType", out int outsideFilterType))
        {
            OutsideFilterTypeCombo.SelectedIndex = outsideFilterType;
        }

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

        if (_effect.Configuration.TryGet("edgeSoftness", out float edgeSoftness))
        {
            EdgeSoftnessSlider.Value = edgeSoftness;
            EdgeSoftnessValue.Text = edgeSoftness.ToString("F2");
        }

        if (_effect.Configuration.TryGet("enableCurves", out bool enableCurves))
        {
            EnableCurvesCheckBox.IsChecked = enableCurves;
        }

        if (_effect.Configuration.TryGet("curveStrength", out float curveStrength))
        {
            CurveStrengthSlider.Value = curveStrength;
            CurveStrengthValue.Text = curveStrength.ToString("F2");
        }

        // Load custom matrix settings
        if (_effect.Configuration.TryGet("enableCustomMatrix", out bool enableCustomMatrix))
        {
            EnableCustomMatrixCheckBox.IsChecked = enableCustomMatrix;
            CustomMatrixPanel.Visibility = enableCustomMatrix ? Visibility.Visible : Visibility.Collapsed;
        }

        // Load matrix values
        LoadMatrixValue("matrixR0", MatrixR0, 1f);
        LoadMatrixValue("matrixR1", MatrixR1, 0f);
        LoadMatrixValue("matrixR2", MatrixR2, 0f);
        LoadMatrixValue("matrixG0", MatrixG0, 0f);
        LoadMatrixValue("matrixG1", MatrixG1, 1f);
        LoadMatrixValue("matrixG2", MatrixG2, 0f);
        LoadMatrixValue("matrixB0", MatrixB0, 0f);
        LoadMatrixValue("matrixB1", MatrixB1, 0f);
        LoadMatrixValue("matrixB2", MatrixB2, 1f);

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
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("rectWidth", (float)RectWidthSlider.Value);
        config.Set("rectHeight", (float)RectHeightSlider.Value);
        config.Set("shapeMode", ShapeModeCombo.SelectedIndex);

        // Use appropriate filter combo based on shape mode
        int shapeMode = ShapeModeCombo.SelectedIndex;
        if (shapeMode == 2) // Fullscreen
        {
            config.Set("filterType", FilterTypeCombo.SelectedIndex);
            config.Set("outsideFilterType", 0); // Not used in fullscreen
        }
        else // Circle or Rectangle
        {
            config.Set("filterType", InsideFilterTypeCombo.SelectedIndex);
            config.Set("outsideFilterType", OutsideFilterTypeCombo.SelectedIndex);
        }

        config.Set("intensity", (float)IntensitySlider.Value);
        config.Set("colorBoost", (float)ColorBoostSlider.Value);
        config.Set("edgeSoftness", (float)EdgeSoftnessSlider.Value);
        config.Set("enableCurves", EnableCurvesCheckBox.IsChecked ?? false);
        config.Set("curveStrength", (float)CurveStrengthSlider.Value);

        // Save custom matrix settings
        config.Set("enableCustomMatrix", EnableCustomMatrixCheckBox.IsChecked ?? false);
        config.Set("matrixR0", ParseFloat(MatrixR0.Text, 1f));
        config.Set("matrixR1", ParseFloat(MatrixR1.Text, 0f));
        config.Set("matrixR2", ParseFloat(MatrixR2.Text, 0f));
        config.Set("matrixG0", ParseFloat(MatrixG0.Text, 0f));
        config.Set("matrixG1", ParseFloat(MatrixG1.Text, 1f));
        config.Set("matrixG2", ParseFloat(MatrixG2.Text, 0f));
        config.Set("matrixB0", ParseFloat(MatrixB0.Text, 0f));
        config.Set("matrixB1", ParseFloat(MatrixB1.Text, 0f));
        config.Set("matrixB2", ParseFloat(MatrixB2.Text, 1f));

        // Save curves as JSON
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
        // Controls may be null during initialization
        if (CircleSettings != null)
            CircleSettings.Visibility = shapeMode == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (RectangleSettings != null)
            RectangleSettings.Visibility = shapeMode == 1 ? Visibility.Visible : Visibility.Collapsed;

        // Hide edge softness for fullscreen mode
        if (EdgeSoftnessSlider != null)
            EdgeSoftnessSlider.IsEnabled = shapeMode != 2;

        // Toggle filter panels based on shape mode
        // Fullscreen: single filter dropdown
        // Circle/Rectangle: inside + outside filter dropdowns
        if (FullscreenFilterPanel != null)
            FullscreenFilterPanel.Visibility = shapeMode == 2 ? Visibility.Visible : Visibility.Collapsed;
        if (ShapeFilterPanel != null)
            ShapeFilterPanel.Visibility = shapeMode != 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetMatrixFromPreset(int presetIndex)
    {
        if (!MatrixPresets.TryGetValue(presetIndex, out var preset))
            return;

        _isInitializing = true;
        MatrixR0.Text = preset[0].ToString("F3", CultureInfo.InvariantCulture);
        MatrixR1.Text = preset[1].ToString("F3", CultureInfo.InvariantCulture);
        MatrixR2.Text = preset[2].ToString("F3", CultureInfo.InvariantCulture);
        MatrixG0.Text = preset[3].ToString("F3", CultureInfo.InvariantCulture);
        MatrixG1.Text = preset[4].ToString("F3", CultureInfo.InvariantCulture);
        MatrixG2.Text = preset[5].ToString("F3", CultureInfo.InvariantCulture);
        MatrixB0.Text = preset[6].ToString("F3", CultureInfo.InvariantCulture);
        MatrixB1.Text = preset[7].ToString("F3", CultureInfo.InvariantCulture);
        MatrixB2.Text = preset[8].ToString("F3", CultureInfo.InvariantCulture);
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

    private void ShapeModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int newShapeMode = ShapeModeCombo.SelectedIndex;

        // Sync filter values when switching between fullscreen and shape modes
        if (newShapeMode == 2 && FilterTypeCombo != null && InsideFilterTypeCombo != null)
        {
            // Switching to fullscreen: copy inside filter to fullscreen filter
            FilterTypeCombo.SelectedIndex = InsideFilterTypeCombo.SelectedIndex;
        }
        else if (newShapeMode != 2 && FilterTypeCombo != null && InsideFilterTypeCombo != null)
        {
            // Switching to shape mode: copy fullscreen filter to inside filter
            InsideFilterTypeCombo.SelectedIndex = FilterTypeCombo.SelectedIndex;
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

    private void FilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void InsideFilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void OutsideFilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

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

    private void EnableCustomMatrixCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        bool isEnabled = EnableCustomMatrixCheckBox.IsChecked ?? false;
        CustomMatrixPanel.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
        UpdateConfiguration();
    }

    private void MatrixPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int selectedIndex = MatrixPresetCombo.SelectedIndex;
        SetMatrixFromPreset(selectedIndex);
    }

    private void Matrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ResetMatrixButton_Click(object sender, RoutedEventArgs e)
    {
        MatrixPresetCombo.SelectedIndex = 0; // Reset to Normal Vision (Identity)
        SetMatrixFromPreset(0);
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }

    #endregion
}
