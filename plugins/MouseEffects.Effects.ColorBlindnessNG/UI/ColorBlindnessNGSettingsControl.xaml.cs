using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Settings control for ColorBlindnessNG effect.
/// Uses ZoneSettingsEditor components for per-zone settings.
/// </summary>
public partial class ColorBlindnessNGSettingsControl : UserControl
{
    private ColorBlindnessNGEffect? _effect;
    private bool _isLoading;
    private bool _isExpanded;
    private PresetManager _presetManager = new();
    private ZoneSettingsEditor[] _zoneEditors = Array.Empty<ZoneSettingsEditor>();

    public ColorBlindnessNGSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IEffect effect && effect is ColorBlindnessNGEffect cbEffect)
        {
            _effect = cbEffect;
            _presetManager.LoadCustomPresets();

            // Initialize zone editor array
            _zoneEditors = [Zone0Editor, Zone1Editor, Zone2Editor, Zone3Editor];

            // Bind all zone editors
            for (int i = 0; i < 4; i++)
            {
                int zoneIndex = i; // Capture for closure
                _zoneEditors[i].BindToZone(_effect.GetZone(i), i, _effect, _presetManager);
                _zoneEditors[i].SettingsChanged += (s, args) => SaveZoneConfiguration(zoneIndex);
                _zoneEditors[i].PresetCreated += RefreshAllPresetDropdowns;
                _zoneEditors[i].PresetDeleted += RefreshAllPresetDropdowns;
                _zoneEditors[i].PresetUpdated += presetName => ReloadPresetInOtherZones(zoneIndex, presetName);
            }

            LoadConfiguration();
        }
    }

    private void RefreshAllPresetDropdowns()
    {
        foreach (var editor in _zoneEditors)
            editor.RefreshPresetDropdown();
    }

    private void ReloadPresetInOtherZones(int sourceZoneIndex, string presetName)
    {
        // Reload the preset in all zones except the one that triggered the update
        for (int i = 0; i < _zoneEditors.Length; i++)
        {
            if (i != sourceZoneIndex)
                _zoneEditors[i].ReloadIfPresetSelected(presetName);
        }
    }

    private void SaveZoneConfiguration(int zoneIndex)
    {
        if (_effect == null) return;
        var zone = _effect.GetZone(zoneIndex);
        var prefix = $"zone{zoneIndex}_";

        // Mode and basic settings
        _effect.Configuration.Set(prefix + "mode", (int)zone.Mode);
        _effect.Configuration.Set(prefix + "simAlgorithm", (int)zone.SimulationAlgorithm);
        _effect.Configuration.Set(prefix + "simFilterType", zone.SimulationFilterType);
        _effect.Configuration.Set(prefix + "intensity", zone.Intensity);

        // Correction algorithm settings
        _effect.Configuration.Set(prefix + "correctionAlgorithm", (int)zone.CorrectionAlgorithm);
        _effect.Configuration.Set(prefix + "daltonizationCVDType", zone.DaltonizationCVDType);
        _effect.Configuration.Set(prefix + "daltonizationStrength", zone.DaltonizationStrength);

        // Hue Rotation settings
        _effect.Configuration.Set(prefix + "hueRotCVDType", (int)zone.HueRotationCVDType);
        _effect.Configuration.Set(prefix + "hueRotStrength", zone.HueRotationStrength);
        _effect.Configuration.Set(prefix + "hueRotAdvanced", zone.HueRotationAdvancedMode);
        _effect.Configuration.Set(prefix + "hueRotSourceStart", zone.HueRotationSourceStart);
        _effect.Configuration.Set(prefix + "hueRotSourceEnd", zone.HueRotationSourceEnd);
        _effect.Configuration.Set(prefix + "hueRotShift", zone.HueRotationShift);
        _effect.Configuration.Set(prefix + "hueRotFalloff", zone.HueRotationFalloff);

        // CIELAB settings
        _effect.Configuration.Set(prefix + "cielabCVDType", (int)zone.CIELABCVDType);
        _effect.Configuration.Set(prefix + "cielabStrength", zone.CIELABStrength);
        _effect.Configuration.Set(prefix + "cielabAdvanced", zone.CIELABAdvancedMode);
        _effect.Configuration.Set(prefix + "cielabAtoB", zone.CIELABAtoB);
        _effect.Configuration.Set(prefix + "cielabBtoA", zone.CIELABBtoA);
        _effect.Configuration.Set(prefix + "cielabAEnhance", zone.CIELABAEnhance);
        _effect.Configuration.Set(prefix + "cielabBEnhance", zone.CIELABBEnhance);
        _effect.Configuration.Set(prefix + "cielabLEnhance", zone.CIELABLEnhance);

        // Simulation-Guided settings
        _effect.Configuration.Set(prefix + "simGuidedEnabled", zone.SimulationGuidedEnabled);
        _effect.Configuration.Set(prefix + "simGuidedAlgorithm", (int)zone.SimulationGuidedAlgorithm);
        _effect.Configuration.Set(prefix + "simGuidedFilterType", zone.SimulationGuidedFilterType);
        _effect.Configuration.Set(prefix + "simGuidedSensitivity", zone.SimulationGuidedSensitivity);

        // Post-Simulation settings
        _effect.Configuration.Set(prefix + "postSimEnabled", zone.PostCorrectionSimEnabled);
        _effect.Configuration.Set(prefix + "postSimAlgorithm", (int)zone.PostCorrectionSimAlgorithm);
        _effect.Configuration.Set(prefix + "postSimFilterType", zone.PostCorrectionSimFilterType);
        _effect.Configuration.Set(prefix + "postSimIntensity", zone.PostCorrectionSimIntensity);

        // LUT channel settings
        _effect.Configuration.Set(prefix + "appMode", (int)zone.ApplicationMode);
        _effect.Configuration.Set(prefix + "threshold", zone.Threshold);
        _effect.Configuration.Set(prefix + "gradientType", (int)zone.GradientType);

        _effect.Configuration.Set(prefix + "redEnabled", zone.RedChannel.Enabled);
        _effect.Configuration.Set(prefix + "redStrength", zone.RedChannel.Strength);
        _effect.Configuration.Set(prefix + "redWhiteProtection", zone.RedChannel.WhiteProtection);
        _effect.Configuration.Set(prefix + "redDominanceThreshold", zone.RedChannel.DominanceThreshold);
        _effect.Configuration.Set(prefix + "redBlendMode", (int)zone.RedChannel.BlendMode);
        _effect.Configuration.Set(prefix + "redStartColor", CustomPreset.ToHexColor(zone.RedChannel.StartColor));
        _effect.Configuration.Set(prefix + "redEndColor", CustomPreset.ToHexColor(zone.RedChannel.EndColor));

        _effect.Configuration.Set(prefix + "greenEnabled", zone.GreenChannel.Enabled);
        _effect.Configuration.Set(prefix + "greenStrength", zone.GreenChannel.Strength);
        _effect.Configuration.Set(prefix + "greenWhiteProtection", zone.GreenChannel.WhiteProtection);
        _effect.Configuration.Set(prefix + "greenDominanceThreshold", zone.GreenChannel.DominanceThreshold);
        _effect.Configuration.Set(prefix + "greenBlendMode", (int)zone.GreenChannel.BlendMode);
        _effect.Configuration.Set(prefix + "greenStartColor", CustomPreset.ToHexColor(zone.GreenChannel.StartColor));
        _effect.Configuration.Set(prefix + "greenEndColor", CustomPreset.ToHexColor(zone.GreenChannel.EndColor));

        _effect.Configuration.Set(prefix + "blueEnabled", zone.BlueChannel.Enabled);
        _effect.Configuration.Set(prefix + "blueStrength", zone.BlueChannel.Strength);
        _effect.Configuration.Set(prefix + "blueWhiteProtection", zone.BlueChannel.WhiteProtection);
        _effect.Configuration.Set(prefix + "blueDominanceThreshold", zone.BlueChannel.DominanceThreshold);
        _effect.Configuration.Set(prefix + "blueBlendMode", (int)zone.BlueChannel.BlendMode);
        _effect.Configuration.Set(prefix + "blueStartColor", CustomPreset.ToHexColor(zone.BlueChannel.StartColor));
        _effect.Configuration.Set(prefix + "blueEndColor", CustomPreset.ToHexColor(zone.BlueChannel.EndColor));
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;
        _isLoading = true;

        try
        {
            // Load global settings
            EnabledCheckBox.IsChecked = _effect.IsEnabled;
            SplitModeCombo.SelectedIndex = (int)_effect.SplitMode;
            SplitPositionSlider.Value = _effect.SplitPosition;
            SplitPositionVSlider.Value = _effect.SplitPositionV;
            ComparisonModeCheckBox.IsChecked = _effect.ComparisonMode;

            // Load shape settings
            if (RadiusSlider != null)
            {
                RadiusSlider.Value = _effect.Radius;
                RadiusValue.Text = $"{_effect.Radius:F0} px";
            }
            if (RectWidthSlider != null)
            {
                RectWidthSlider.Value = _effect.RectWidth;
                RectWidthValue.Text = $"{_effect.RectWidth:F0} px";
            }
            if (RectHeightSlider != null)
            {
                RectHeightSlider.Value = _effect.RectHeight;
                RectHeightValue.Text = $"{_effect.RectHeight:F0} px";
            }
            if (EdgeSoftnessSlider != null)
            {
                EdgeSoftnessSlider.Value = _effect.EdgeSoftness;
                UpdateEdgeSoftnessLabel();
            }

            // Update UI visibility
            UpdateSplitModeUI((int)_effect.SplitMode);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateEdgeSoftnessLabel()
    {
        if (EdgeSoftnessSlider == null || EdgeSoftnessValue == null) return;
        string description = EdgeSoftnessSlider.Value < 0.1 ? "Hard" : EdgeSoftnessSlider.Value > 0.7 ? "Very Soft" : "Soft";
        EdgeSoftnessValue.Text = $"{EdgeSoftnessSlider.Value:F2} ({description})";
    }

    #region UI Event Handlers

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "\u25B2" : "\u25BC";
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect != null && !_isLoading)
        {
            _effect.IsEnabled = EnabledCheckBox.IsChecked == true;
        }
    }

    private void SplitModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int splitMode = SplitModeCombo.SelectedIndex;
        _effect.SplitMode = (SplitMode)splitMode;
        _effect.Configuration.Set("splitMode", splitMode);
        UpdateSplitModeUI(splitMode);
    }

    private void UpdateSplitModeUI(int splitMode)
    {
        // Guard against null controls during initialization
        if (SplitPositionPanel == null || ShapeSettingsPanel == null) return;

        bool isSplit = splitMode > 0 && splitMode <= 3;
        bool isVerticalSplit = splitMode == 1;   // Left/Right - needs horizontal position slider
        bool isHorizontalSplit = splitMode == 2; // Top/Bottom - needs vertical position slider
        bool isQuadrant = splitMode == 3;        // Needs both sliders
        bool isCircle = splitMode == 4;
        bool isRectangle = splitMode == 5;
        bool isShapeMode = isCircle || isRectangle;

        // Horizontal Split Position slider (for vertical split: left/right, or quadrants)
        SplitPositionPanel.Visibility = (isVerticalSplit || isQuadrant) ? Visibility.Visible : Visibility.Collapsed;
        // Vertical Split Position slider (for horizontal split: top/bottom, or quadrants)
        SplitPositionVPanel.Visibility = (isHorizontalSplit || isQuadrant) ? Visibility.Visible : Visibility.Collapsed;

        // Comparison mode (hidden for shape modes)
        ComparisonModeCheckBox.Visibility = isSplit ? Visibility.Visible : Visibility.Collapsed;

        // Shape settings panel
        ShapeSettingsPanel.Visibility = isShapeMode ? Visibility.Visible : Visibility.Collapsed;
        if (CircleSettingsPanel != null)
            CircleSettingsPanel.Visibility = isCircle ? Visibility.Visible : Visibility.Collapsed;
        if (RectangleSettingsPanel != null)
            RectangleSettingsPanel.Visibility = isRectangle ? Visibility.Visible : Visibility.Collapsed;

        // Update zone visibility (shape modes have 2 zones: inner and outer)
        bool hasMultipleZones = isSplit || isShapeMode;
        Zone1Expander.Visibility = hasMultipleZones ? Visibility.Visible : Visibility.Collapsed;
        Zone2Expander.Visibility = isQuadrant ? Visibility.Visible : Visibility.Collapsed;
        Zone3Expander.Visibility = isQuadrant ? Visibility.Visible : Visibility.Collapsed;

        // Update zone headers based on split mode
        switch (splitMode)
        {
            case 0: // Fullscreen
                Zone0Header.Text = "Main Settings";
                break;
            case 1: // Vertical
                Zone0Header.Text = "Zone 1 (Left)";
                Zone1Header.Text = "Zone 2 (Right)";
                break;
            case 2: // Horizontal
                Zone0Header.Text = "Zone 1 (Top)";
                Zone1Header.Text = "Zone 2 (Bottom)";
                break;
            case 3: // Quadrants
                Zone0Header.Text = "Zone 1 (Top-Left)";
                Zone1Header.Text = "Zone 2 (Top-Right)";
                Zone2Header.Text = "Zone 3 (Bottom-Left)";
                Zone3Header.Text = "Zone 4 (Bottom-Right)";
                break;
            case 4: // Circle
                Zone0Header.Text = "Inner Zone (inside circle)";
                Zone1Header.Text = "Outer Zone (outside circle)";
                break;
            case 5: // Rectangle
                Zone0Header.Text = "Inner Zone (inside rectangle)";
                Zone1Header.Text = "Outer Zone (outside rectangle)";
                break;
        }
    }

    private void SplitPositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        _effect.SplitPosition = (float)SplitPositionSlider.Value;
        _effect.Configuration.Set("splitPosition", (float)SplitPositionSlider.Value);
        SplitPositionValue.Text = $"{SplitPositionSlider.Value:P0}";
    }

    private void SplitPositionVSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        _effect.SplitPositionV = (float)SplitPositionVSlider.Value;
        _effect.Configuration.Set("splitPositionV", (float)SplitPositionVSlider.Value);
        SplitPositionVValue.Text = $"{SplitPositionVSlider.Value:P0}";
    }

    private void ComparisonModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        _effect.ComparisonMode = ComparisonModeCheckBox.IsChecked == true;
        _effect.Configuration.Set("comparisonMode", ComparisonModeCheckBox.IsChecked == true);
    }

    #endregion

    #region Shape Settings Event Handlers

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        _effect.Radius = (float)RadiusSlider.Value;
        _effect.Configuration.Set("radius", (float)RadiusSlider.Value);
        RadiusValue.Text = $"{RadiusSlider.Value:F0} px";
    }

    private void RectWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        _effect.RectWidth = (float)RectWidthSlider.Value;
        _effect.Configuration.Set("rectWidth", (float)RectWidthSlider.Value);
        RectWidthValue.Text = $"{RectWidthSlider.Value:F0} px";

        // Sync height if Square checkbox is checked
        if (SquareCheckBox.IsChecked == true && Math.Abs(RectHeightSlider.Value - RectWidthSlider.Value) > 0.1)
        {
            _isLoading = true;
            RectHeightSlider.Value = RectWidthSlider.Value;
            _effect.RectHeight = (float)RectWidthSlider.Value;
            _effect.Configuration.Set("rectHeight", (float)RectWidthSlider.Value);
            RectHeightValue.Text = $"{RectWidthSlider.Value:F0} px";
            _isLoading = false;
        }
    }

    private void RectHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        _effect.RectHeight = (float)RectHeightSlider.Value;
        _effect.Configuration.Set("rectHeight", (float)RectHeightSlider.Value);
        RectHeightValue.Text = $"{RectHeightSlider.Value:F0} px";

        // Sync width if Square checkbox is checked
        if (SquareCheckBox.IsChecked == true && Math.Abs(RectWidthSlider.Value - RectHeightSlider.Value) > 0.1)
        {
            _isLoading = true;
            RectWidthSlider.Value = RectHeightSlider.Value;
            _effect.RectWidth = (float)RectHeightSlider.Value;
            _effect.Configuration.Set("rectWidth", (float)RectHeightSlider.Value);
            RectWidthValue.Text = $"{RectHeightSlider.Value:F0} px";
            _isLoading = false;
        }
    }

    private void SquareCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        // When Square is checked, sync height to width
        if (SquareCheckBox.IsChecked == true && Math.Abs(RectHeightSlider.Value - RectWidthSlider.Value) > 0.1)
        {
            _isLoading = true;
            RectHeightSlider.Value = RectWidthSlider.Value;
            _effect.RectHeight = (float)RectWidthSlider.Value;
            _effect.Configuration.Set("rectHeight", (float)RectWidthSlider.Value);
            RectHeightValue.Text = $"{RectWidthSlider.Value:F0} px";
            _isLoading = false;
        }
    }

    private void EdgeSoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        _effect.EdgeSoftness = (float)EdgeSoftnessSlider.Value;
        _effect.Configuration.Set("edgeSoftness", (float)EdgeSoftnessSlider.Value);
        UpdateEdgeSoftnessLabel();
    }

    #endregion
}
