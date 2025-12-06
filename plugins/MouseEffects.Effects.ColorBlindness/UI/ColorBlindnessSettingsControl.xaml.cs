using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MouseEffects.Core.Effects;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.ColorBlindness.UI;

public partial class ColorBlindnessSettingsControl : UserControl
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
    private static readonly Dictionary<int, float[]> MatrixPresets = new()
    {
        [0] = [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f],           // Normal Vision (Identity)
        [1] = [0.567f, 0.433f, 0f, 0.558f, 0.442f, 0f, 0f, 0.242f, 0.758f], // Protanopia
        [2] = [0.817f, 0.183f, 0f, 0.333f, 0.667f, 0f, 0f, 0.125f, 0.875f], // Protanomaly
        [3] = [0.625f, 0.375f, 0f, 0.7f, 0.3f, 0f, 0f, 0.3f, 0.7f],         // Deuteranopia
        [4] = [0.8f, 0.2f, 0f, 0.258f, 0.742f, 0f, 0f, 0.142f, 0.858f],     // Deuteranomaly
        [5] = [0.95f, 0.05f, 0f, 0f, 0.433f, 0.567f, 0f, 0.475f, 0.525f],   // Tritanopia
        [6] = [0.967f, 0.033f, 0f, 0f, 0.733f, 0.267f, 0f, 0.183f, 0.817f], // Tritanomaly
        [7] = [0.299f, 0.587f, 0.114f, 0.299f, 0.587f, 0.114f, 0.299f, 0.587f, 0.114f], // Achromatopsia
        [8] = [0.618f, 0.320f, 0.062f, 0.163f, 0.775f, 0.062f, 0.163f, 0.320f, 0.516f], // Achromatomaly
        [9] = [0.2126f, 0.7152f, 0.0722f, 0.2126f, 0.7152f, 0.0722f, 0.2126f, 0.7152f, 0.0722f], // Grayscale
        [10] = [0.393f, 0.769f, 0.189f, 0.349f, 0.686f, 0.168f, 0.272f, 0.534f, 0.131f] // Sepia
    };

    // Zone names by layout mode
    private static readonly string[][] ZoneNames =
    [
        ["Screen"],                                           // Fullscreen
        ["Inside Circle", "Outside Circle"],                  // Circle
        ["Inside Rectangle", "Outside Rectangle"],            // Rectangle
        ["Left Side", "Right Side"],                          // Split Vertical
        ["Top Half", "Bottom Half"],                          // Split Horizontal
        ["Top-Left", "Top-Right", "Bottom-Left", "Bottom-Right"] // Quadrants
    ];

    public ColorBlindnessSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        _colorBlindnessEffect = effect as ColorBlindnessEffect;

        // Subscribe to comparison mode changes from hotkey
        if (_colorBlindnessEffect != null)
        {
            _colorBlindnessEffect.ComparisonModeChanged += OnComparisonModeChangedFromHotkey;
        }

        LoadConfiguration();
        _isInitializing = false;
    }

    private void OnComparisonModeChangedFromHotkey(bool isEnabled)
    {
        // Update UI from hotkey toggle - must run on UI thread
        Dispatcher.Invoke(() =>
        {
            _isInitializing = true;
            ComparisonModeCheckBox.IsChecked = isEnabled;
            _isInitializing = false;

            // Save the change
            UpdateConfiguration();
        });
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        // Layout mode
        if (_effect.Configuration.TryGet("layoutMode", out int layoutMode))
            LayoutModeCombo.SelectedIndex = layoutMode;

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

        // Split positions
        if (_effect.Configuration.TryGet("splitPosition", out float splitPosition))
        {
            SplitPositionSlider.Value = splitPosition;
            SplitPositionHSlider.Value = splitPosition;
            QuadSplitHSlider.Value = splitPosition;
            UpdateSplitPositionLabels();
        }

        if (_effect.Configuration.TryGet("splitPositionV", out float splitPositionV))
        {
            QuadSplitVSlider.Value = splitPositionV;
            QuadSplitVValue.Text = $"{(splitPositionV * 100):F0}%";
        }

        if (_effect.Configuration.TryGet("edgeSoftness", out float edgeSoftness))
        {
            EdgeSoftnessSlider.Value = edgeSoftness;
            EdgeSoftnessValue.Text = edgeSoftness.ToString("F2");
        }

        // Comparison mode
        if (_effect.Configuration.TryGet("comparisonMode", out bool comparisonMode))
            ComparisonModeCheckBox.IsChecked = comparisonMode;

        // Comparison hotkey
        if (_effect.Configuration.TryGet("enableComparisonHotkey", out bool enableComparisonHotkey))
            EnableComparisonHotkeyCheckBox.IsChecked = enableComparisonHotkey;

        // Load zone settings
        LoadZoneSettings(0, Zone0CorrectionModeCombo, Zone0LMSFilterCombo, Zone0ModeCombo, Zone0RGBPresetCombo,
            Zone0MatrixR0, Zone0MatrixR1, Zone0MatrixR2,
            Zone0MatrixG0, Zone0MatrixG1, Zone0MatrixG2,
            Zone0MatrixB0, Zone0MatrixB1, Zone0MatrixB2);

        LoadZoneSettings(1, Zone1CorrectionModeCombo, Zone1LMSFilterCombo, Zone1ModeCombo, Zone1RGBPresetCombo,
            Zone1MatrixR0, Zone1MatrixR1, Zone1MatrixR2,
            Zone1MatrixG0, Zone1MatrixG1, Zone1MatrixG2,
            Zone1MatrixB0, Zone1MatrixB1, Zone1MatrixB2);

        LoadZoneSettings(2, Zone2CorrectionModeCombo, Zone2LMSFilterCombo, Zone2ModeCombo, Zone2RGBPresetCombo,
            Zone2MatrixR0, Zone2MatrixR1, Zone2MatrixR2,
            Zone2MatrixG0, Zone2MatrixG1, Zone2MatrixG2,
            Zone2MatrixB0, Zone2MatrixB1, Zone2MatrixB2);

        LoadZoneSettings(3, Zone3CorrectionModeCombo, Zone3LMSFilterCombo, Zone3ModeCombo, Zone3RGBPresetCombo,
            Zone3MatrixR0, Zone3MatrixR1, Zone3MatrixR2,
            Zone3MatrixG0, Zone3MatrixG1, Zone3MatrixG2,
            Zone3MatrixB0, Zone3MatrixB1, Zone3MatrixB2);

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
            EnableCurvesCheckBox.IsChecked = enableCurves;

        if (_effect.Configuration.TryGet("curveStrength", out float curveStrength))
        {
            CurveStrengthSlider.Value = curveStrength;
            CurveStrengthValue.Text = curveStrength.ToString("F2");
        }

        if (_colorBlindnessEffect != null)
        {
            CurveEditorControl.MasterCurve = _colorBlindnessEffect.MasterCurve;
            CurveEditorControl.RedCurve = _colorBlindnessEffect.RedCurve;
            CurveEditorControl.GreenCurve = _colorBlindnessEffect.GreenCurve;
            CurveEditorControl.BlueCurve = _colorBlindnessEffect.BlueCurve;
        }

        // Update UI based on layout mode
        UpdateLayoutModeUI(LayoutModeCombo.SelectedIndex);
    }

    private void LoadZoneSettings(int zoneIndex, ComboBox correctionModeCombo, ComboBox lmsFilterCombo,
        ComboBox modeCombo, ComboBox rgbPresetCombo, TextBox r0, TextBox r1, TextBox r2,
        TextBox g0, TextBox g1, TextBox g2, TextBox b0, TextBox b1, TextBox b2)
    {
        string prefix = $"zone{zoneIndex}_";

        if (_effect.Configuration.TryGet($"{prefix}correctionMode", out int correctionMode))
        {
            correctionModeCombo.SelectedIndex = correctionMode;
        }

        if (_effect.Configuration.TryGet($"{prefix}lmsFilterType", out int lmsFilterType))
        {
            lmsFilterCombo.SelectedIndex = lmsFilterType;
        }

        if (_effect.Configuration.TryGet($"{prefix}simulationMode", out int simulationMode))
        {
            modeCombo.SelectedIndex = simulationMode;
        }

        // Load matrix values
        LoadMatrixValue($"{prefix}matrixR0", r0, 1f);
        LoadMatrixValue($"{prefix}matrixR1", r1, 0f);
        LoadMatrixValue($"{prefix}matrixR2", r2, 0f);
        LoadMatrixValue($"{prefix}matrixG0", g0, 0f);
        LoadMatrixValue($"{prefix}matrixG1", g1, 1f);
        LoadMatrixValue($"{prefix}matrixG2", g2, 0f);
        LoadMatrixValue($"{prefix}matrixB0", b0, 0f);
        LoadMatrixValue($"{prefix}matrixB1", b1, 0f);
        LoadMatrixValue($"{prefix}matrixB2", b2, 1f);
    }

    private void LoadMatrixValue(string key, TextBox textBox, float defaultValue)
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

        // Layout mode
        int layoutMode = LayoutModeCombo.SelectedIndex;
        config.Set("layoutMode", layoutMode);

        // Shape settings
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("rectWidth", (float)RectWidthSlider.Value);
        config.Set("rectHeight", (float)RectHeightSlider.Value);

        // Split positions (use appropriate slider based on layout mode)
        float splitPosition = layoutMode switch
        {
            3 => (float)SplitPositionSlider.Value,    // Split Vertical
            4 => (float)SplitPositionHSlider.Value,   // Split Horizontal
            5 => (float)QuadSplitHSlider.Value,       // Quadrants
            _ => 0.5f
        };
        config.Set("splitPosition", splitPosition);
        config.Set("splitPositionV", (float)QuadSplitVSlider.Value);

        config.Set("edgeSoftness", (float)EdgeSoftnessSlider.Value);
        config.Set("comparisonMode", ComparisonModeCheckBox.IsChecked ?? false);
        config.Set("enableComparisonHotkey", EnableComparisonHotkeyCheckBox.IsChecked ?? true);

        // Zone settings
        SaveZoneSettings(config, 0,
            Zone0CorrectionModeCombo, Zone0LMSFilterCombo, Zone0ModeCombo,
            Zone0MatrixR0, Zone0MatrixR1, Zone0MatrixR2,
            Zone0MatrixG0, Zone0MatrixG1, Zone0MatrixG2,
            Zone0MatrixB0, Zone0MatrixB1, Zone0MatrixB2);

        SaveZoneSettings(config, 1,
            Zone1CorrectionModeCombo, Zone1LMSFilterCombo, Zone1ModeCombo,
            Zone1MatrixR0, Zone1MatrixR1, Zone1MatrixR2,
            Zone1MatrixG0, Zone1MatrixG1, Zone1MatrixG2,
            Zone1MatrixB0, Zone1MatrixB1, Zone1MatrixB2);

        SaveZoneSettings(config, 2,
            Zone2CorrectionModeCombo, Zone2LMSFilterCombo, Zone2ModeCombo,
            Zone2MatrixR0, Zone2MatrixR1, Zone2MatrixR2,
            Zone2MatrixG0, Zone2MatrixG1, Zone2MatrixG2,
            Zone2MatrixB0, Zone2MatrixB1, Zone2MatrixB2);

        SaveZoneSettings(config, 3,
            Zone3CorrectionModeCombo, Zone3LMSFilterCombo, Zone3ModeCombo,
            Zone3MatrixR0, Zone3MatrixR1, Zone3MatrixR2,
            Zone3MatrixG0, Zone3MatrixG1, Zone3MatrixG2,
            Zone3MatrixB0, Zone3MatrixB1, Zone3MatrixB2);

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

    private void SaveZoneSettings(EffectConfiguration config, int zoneIndex,
        ComboBox correctionModeCombo, ComboBox lmsFilterCombo, ComboBox modeCombo,
        TextBox r0, TextBox r1, TextBox r2,
        TextBox g0, TextBox g1, TextBox g2,
        TextBox b0, TextBox b1, TextBox b2)
    {
        string prefix = $"zone{zoneIndex}_";

        config.Set($"{prefix}correctionMode", correctionModeCombo.SelectedIndex);
        config.Set($"{prefix}lmsFilterType", lmsFilterCombo.SelectedIndex);
        config.Set($"{prefix}simulationMode", modeCombo.SelectedIndex);

        config.Set($"{prefix}matrixR0", ParseFloat(r0.Text, 1f));
        config.Set($"{prefix}matrixR1", ParseFloat(r1.Text, 0f));
        config.Set($"{prefix}matrixR2", ParseFloat(r2.Text, 0f));
        config.Set($"{prefix}matrixG0", ParseFloat(g0.Text, 0f));
        config.Set($"{prefix}matrixG1", ParseFloat(g1.Text, 1f));
        config.Set($"{prefix}matrixG2", ParseFloat(g2.Text, 0f));
        config.Set($"{prefix}matrixB0", ParseFloat(b0.Text, 0f));
        config.Set($"{prefix}matrixB1", ParseFloat(b1.Text, 0f));
        config.Set($"{prefix}matrixB2", ParseFloat(b2.Text, 1f));
    }

    private static float ParseFloat(string text, float defaultValue)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            return value;
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            return value;
        return defaultValue;
    }

    private void UpdateLayoutModeUI(int layoutMode)
    {
        // Hide all layout-specific settings
        CircleSettings.Visibility = Visibility.Collapsed;
        RectangleSettings.Visibility = Visibility.Collapsed;
        SplitVerticalSettings.Visibility = Visibility.Collapsed;
        SplitHorizontalSettings.Visibility = Visibility.Collapsed;
        QuadrantSettings.Visibility = Visibility.Collapsed;

        // Show layout-specific settings
        switch (layoutMode)
        {
            case 1: CircleSettings.Visibility = Visibility.Visible; break;
            case 2: RectangleSettings.Visibility = Visibility.Visible; break;
            case 3: SplitVerticalSettings.Visibility = Visibility.Visible; break;
            case 4: SplitHorizontalSettings.Visibility = Visibility.Visible; break;
            case 5: QuadrantSettings.Visibility = Visibility.Visible; break;
        }

        // Edge softness (hidden for fullscreen)
        EdgeSoftnessPanel.Visibility = layoutMode == 0 ? Visibility.Collapsed : Visibility.Visible;

        // Comparison mode (only for split and quadrant modes)
        ComparisonModePanel.Visibility = layoutMode >= 3 ? Visibility.Visible : Visibility.Collapsed;

        // Update zone visibility and headers
        int zoneCount = layoutMode switch
        {
            0 => 1, // Fullscreen
            1 => 2, // Circle
            2 => 2, // Rectangle
            3 => 2, // Split Vertical
            4 => 2, // Split Horizontal
            5 => 4, // Quadrants
            _ => 1
        };

        // Update zone panels
        Zone0Panel.Visibility = Visibility.Visible; // Always visible
        Zone1Panel.Visibility = zoneCount >= 2 ? Visibility.Visible : Visibility.Collapsed;
        Zone2Panel.Visibility = zoneCount >= 3 ? Visibility.Visible : Visibility.Collapsed;
        Zone3Panel.Visibility = zoneCount >= 4 ? Visibility.Visible : Visibility.Collapsed;

        // Update zone headers
        var names = ZoneNames[layoutMode];
        Zone0Header.Text = names.Length > 0 ? names[0] : "Zone 0";
        Zone1Header.Text = names.Length > 1 ? names[1] : "Zone 1";
        Zone2Header.Text = names.Length > 2 ? names[2] : "Zone 2";
        Zone3Header.Text = names.Length > 3 ? names[3] : "Zone 3";
    }

    private void UpdateZoneCorrectionModeUI(int zoneIndex, int correctionMode,
        StackPanel lmsPanel, StackPanel rgbPanel)
    {
        lmsPanel.Visibility = correctionMode == 0 ? Visibility.Visible : Visibility.Collapsed;
        rgbPanel.Visibility = correctionMode == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetMatrixFromPreset(int presetIndex,
        TextBox r0, TextBox r1, TextBox r2,
        TextBox g0, TextBox g1, TextBox g2,
        TextBox b0, TextBox b1, TextBox b2)
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

    private void UpdateSplitPositionLabels()
    {
        SplitPositionValue.Text = $"{(SplitPositionSlider.Value * 100):F0}%";
        SplitPositionHValue.Text = $"{(SplitPositionHSlider.Value * 100):F0}%";
        QuadSplitHValue.Text = $"{(QuadSplitHSlider.Value * 100):F0}%";
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

    private void LayoutModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        UpdateLayoutModeUI(layoutMode);
        UpdateConfiguration();
    }

    private void ComparisonModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void EnableComparisonHotkeyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    // Slider value changed handlers
    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null) RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RectWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null) RectWidthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RectHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null) RectHeightValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SplitPositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SplitPositionValue != null) SplitPositionValue.Text = $"{(e.NewValue * 100):F0}%";
        UpdateConfiguration();
    }

    private void SplitPositionHSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SplitPositionHValue != null) SplitPositionHValue.Text = $"{(e.NewValue * 100):F0}%";
        UpdateConfiguration();
    }

    private void QuadSplitHSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QuadSplitHValue != null) QuadSplitHValue.Text = $"{(e.NewValue * 100):F0}%";
        UpdateConfiguration();
    }

    private void QuadSplitVSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QuadSplitVValue != null) QuadSplitVValue.Text = $"{(e.NewValue * 100):F0}%";
        UpdateConfiguration();
    }

    private void EdgeSoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeSoftnessValue != null) EdgeSoftnessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntensityValue != null) IntensityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ColorBoostSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ColorBoostValue != null) ColorBoostValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void CurveStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CurveStrengthValue != null) CurveStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    // Zone 0 handlers
    private void Zone0CorrectionModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateZoneCorrectionModeUI(0, Zone0CorrectionModeCombo.SelectedIndex, Zone0LMSPanel, Zone0RGBPanel);
        UpdateConfiguration();
    }

    private void Zone0LMSFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone0ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone0RGBPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        SetMatrixFromPreset(Zone0RGBPresetCombo.SelectedIndex,
            Zone0MatrixR0, Zone0MatrixR1, Zone0MatrixR2,
            Zone0MatrixG0, Zone0MatrixG1, Zone0MatrixG2,
            Zone0MatrixB0, Zone0MatrixB1, Zone0MatrixB2);
    }

    private void Zone0Matrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone0ResetMatrix_Click(object sender, RoutedEventArgs e)
    {
        SetMatrixFromPreset(0,
            Zone0MatrixR0, Zone0MatrixR1, Zone0MatrixR2,
            Zone0MatrixG0, Zone0MatrixG1, Zone0MatrixG2,
            Zone0MatrixB0, Zone0MatrixB1, Zone0MatrixB2);
    }

    // Zone 1 handlers
    private void Zone1CorrectionModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateZoneCorrectionModeUI(1, Zone1CorrectionModeCombo.SelectedIndex, Zone1LMSPanel, Zone1RGBPanel);
        UpdateConfiguration();
    }

    private void Zone1LMSFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone1ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone1RGBPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        SetMatrixFromPreset(Zone1RGBPresetCombo.SelectedIndex,
            Zone1MatrixR0, Zone1MatrixR1, Zone1MatrixR2,
            Zone1MatrixG0, Zone1MatrixG1, Zone1MatrixG2,
            Zone1MatrixB0, Zone1MatrixB1, Zone1MatrixB2);
    }

    private void Zone1Matrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone1ResetMatrix_Click(object sender, RoutedEventArgs e)
    {
        SetMatrixFromPreset(0,
            Zone1MatrixR0, Zone1MatrixR1, Zone1MatrixR2,
            Zone1MatrixG0, Zone1MatrixG1, Zone1MatrixG2,
            Zone1MatrixB0, Zone1MatrixB1, Zone1MatrixB2);
    }

    // Zone 2 handlers
    private void Zone2CorrectionModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateZoneCorrectionModeUI(2, Zone2CorrectionModeCombo.SelectedIndex, Zone2LMSPanel, Zone2RGBPanel);
        UpdateConfiguration();
    }

    private void Zone2LMSFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone2ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone2RGBPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        SetMatrixFromPreset(Zone2RGBPresetCombo.SelectedIndex,
            Zone2MatrixR0, Zone2MatrixR1, Zone2MatrixR2,
            Zone2MatrixG0, Zone2MatrixG1, Zone2MatrixG2,
            Zone2MatrixB0, Zone2MatrixB1, Zone2MatrixB2);
    }

    private void Zone2Matrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone2ResetMatrix_Click(object sender, RoutedEventArgs e)
    {
        SetMatrixFromPreset(0,
            Zone2MatrixR0, Zone2MatrixR1, Zone2MatrixR2,
            Zone2MatrixG0, Zone2MatrixG1, Zone2MatrixG2,
            Zone2MatrixB0, Zone2MatrixB1, Zone2MatrixB2);
    }

    // Zone 3 handlers
    private void Zone3CorrectionModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateZoneCorrectionModeUI(3, Zone3CorrectionModeCombo.SelectedIndex, Zone3LMSPanel, Zone3RGBPanel);
        UpdateConfiguration();
    }

    private void Zone3LMSFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone3ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone3RGBPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        SetMatrixFromPreset(Zone3RGBPresetCombo.SelectedIndex,
            Zone3MatrixR0, Zone3MatrixR1, Zone3MatrixR2,
            Zone3MatrixG0, Zone3MatrixG1, Zone3MatrixG2,
            Zone3MatrixB0, Zone3MatrixB1, Zone3MatrixB2);
    }

    private void Zone3Matrix_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void Zone3ResetMatrix_Click(object sender, RoutedEventArgs e)
    {
        SetMatrixFromPreset(0,
            Zone3MatrixR0, Zone3MatrixR1, Zone3MatrixR2,
            Zone3MatrixG0, Zone3MatrixG1, Zone3MatrixG2,
            Zone3MatrixB0, Zone3MatrixB1, Zone3MatrixB2);
    }

    // Curves
    private void EnableCurvesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void CurveEditorControl_CurveChanged(object? sender, EventArgs e)
    {
        UpdateConfiguration();
    }

    #endregion
}
