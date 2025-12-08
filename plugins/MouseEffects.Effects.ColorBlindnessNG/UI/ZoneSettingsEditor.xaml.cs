using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.UI;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Reusable zone settings editor component.
/// Contains all zone-specific UI and event handlers for a single zone.
/// </summary>
public partial class ZoneSettingsEditor : UserControl
{
    private ColorBlindnessNGEffect? _effect;
    private ZoneSettings? _zone;
    private PresetManager? _presetManager;
    private int _zoneIndex;
    private bool _isLoading;
    private int _builtInPresetCount;

    /// <summary>
    /// Fired when any setting changes (for parent to save configuration).
    /// </summary>
    public event EventHandler? SettingsChanged;

    /// <summary>
    /// Fired when a preset is created (parent should refresh all dropdowns).
    /// </summary>
    public event Action? PresetCreated;

    /// <summary>
    /// Fired when a preset is deleted (parent should refresh all dropdowns).
    /// </summary>
    public event Action? PresetDeleted;

    /// <summary>
    /// Fired when a preset is updated (parent should reload in other zones if selected).
    /// </summary>
    public event Action<string>? PresetUpdated;

    public ZoneSettingsEditor()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Bind this editor to a specific zone.
    /// </summary>
    public void BindToZone(ZoneSettings zone, int zoneIndex, ColorBlindnessNGEffect effect, PresetManager presetManager)
    {
        _zone = zone;
        _zoneIndex = zoneIndex;
        _effect = effect;
        _presetManager = presetManager;

        // Bind the nested CorrectionEditor
        CorrectionEditor.BindToZone(zone);
        CorrectionEditor.SettingsChanged += (s, e) => OnSettingsChanged();

        LoadFromZone();
        PopulatePresetComboBox();
        RestoreSavedPresetSelection();
    }

    /// <summary>
    /// Refresh the preset dropdown (called by parent after create/delete).
    /// </summary>
    public void RefreshPresetDropdown()
    {
        PopulatePresetComboBox();
    }

    /// <summary>
    /// Reload the preset if it's currently selected in this zone.
    /// Called by parent when a preset is updated in another zone.
    /// </summary>
    public void ReloadIfPresetSelected(string presetName)
    {
        if (_zone == null || _effect == null || _presetManager == null) return;

        // Check if this preset is currently selected
        if (PresetCombo.SelectedItem is not ComboBoxItem item) return;

        var itemContent = item.Content?.ToString() ?? "";
        var cleanName = itemContent.StartsWith("★ ") ? itemContent.Substring(2) : itemContent;

        if (cleanName != presetName) return;

        // Find and apply the updated preset
        var customPreset = _presetManager.CustomPresets.FirstOrDefault(p => p.Name == presetName);
        if (customPreset == null) return;

        var preset = customPreset.ToCorrectionPreset();
        _zone.ApplyPreset(preset);

        // Refresh the CorrectionEditor to show new values
        CorrectionEditor.BindToZone(_zone);

        // Refresh simulation-guided and post-simulation UI
        RefreshSimulationGuidedUI();
        RefreshPostSimulationUI();

        OnSettingsChanged();
    }

    private void RefreshSimulationGuidedUI()
    {
        if (_zone == null) return;
        _isLoading = true;
        try
        {
            SimGuidedCheckBox.IsChecked = _zone.SimulationGuidedEnabled;
            SimGuidedPanel.Visibility = _zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
            SimGuidedMachadoRadio.IsChecked = _zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Machado;
            SimGuidedStrictRadio.IsChecked = _zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Strict;
            SimGuidedFilterCombo.SelectedIndex = MapFilterTypeToComboIndex(_zone.SimulationGuidedFilterType);
            SimGuidedSensitivitySlider.Value = _zone.SimulationGuidedSensitivity;
            SimGuidedSensitivityLabel.Text = $"Sensitivity ({_zone.SimulationGuidedSensitivity:F2})";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void RefreshPostSimulationUI()
    {
        if (_zone == null) return;
        _isLoading = true;
        try
        {
            PostSimCheckBox.IsChecked = _zone.PostCorrectionSimEnabled;
            PostSimPanel.Visibility = _zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
            PostSimMachadoRadio.IsChecked = _zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Machado;
            PostSimStrictRadio.IsChecked = _zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Strict;
            PostSimFilterCombo.SelectedIndex = MapFilterTypeToComboIndex(_zone.PostCorrectionSimFilterType);
            PostSimIntensitySlider.Value = _zone.PostCorrectionSimIntensity;
            PostSimIntensityLabel.Text = $"Simulation Intensity ({_zone.PostCorrectionSimIntensity:F2})";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string ConfigKey(string name) => $"zone{_zoneIndex}_{name}";

    private void OnSettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);

    private void LoadFromZone()
    {
        if (_zone == null || _effect == null) return;
        _isLoading = true;

        try
        {
            // Mode
            ModeCombo.SelectedIndex = (int)_zone.Mode;
            UpdateModeUI();

            // Simulation settings
            MachadoRadio.IsChecked = _zone.SimulationAlgorithm == SimulationAlgorithm.Machado;
            StrictRadio.IsChecked = _zone.SimulationAlgorithm == SimulationAlgorithm.Strict;
            SimFilterCombo.SelectedIndex = MapFilterTypeToComboIndex(_zone.SimulationFilterType);

            // Correction algorithm
            CorrectionAlgorithmCombo.SelectedIndex = (int)_zone.CorrectionAlgorithm;
            UpdateCorrectionAlgorithmUI();

            // Hue Rotation
            HueRotationCVDCombo.SelectedIndex = (int)_zone.HueRotationCVDType;
            HueRotationStrengthSlider.Value = _zone.HueRotationStrength;
            HueRotationStrengthLabel.Text = $"Strength ({_zone.HueRotationStrength:F2})";
            HueRotationAdvancedCheckBox.IsChecked = _zone.HueRotationAdvancedMode;
            HueRotationAdvancedPanel.Visibility = _zone.HueRotationAdvancedMode ? Visibility.Visible : Visibility.Collapsed;
            HueRotationSourceStartSlider.Value = _zone.HueRotationSourceStart;
            HueRotationSourceStartLabel.Text = $"Source Start ({_zone.HueRotationSourceStart:F0}°)";
            HueRotationSourceEndSlider.Value = _zone.HueRotationSourceEnd;
            HueRotationSourceEndLabel.Text = $"Source End ({_zone.HueRotationSourceEnd:F0}°)";
            HueRotationShiftSlider.Value = _zone.HueRotationShift;
            HueRotationShiftLabel.Text = $"Hue Shift ({(_zone.HueRotationShift >= 0 ? "+" : "")}{_zone.HueRotationShift:F0}°)";
            HueRotationFalloffSlider.Value = _zone.HueRotationFalloff;
            HueRotationFalloffLabel.Text = $"Edge Falloff ({_zone.HueRotationFalloff:F2})";

            // CIELAB
            CIELABCVDCombo.SelectedIndex = (int)_zone.CIELABCVDType;
            CIELABStrengthSlider.Value = _zone.CIELABStrength;
            CIELABStrengthLabel.Text = $"Strength ({_zone.CIELABStrength:F2})";
            CIELABAdvancedCheckBox.IsChecked = _zone.CIELABAdvancedMode;
            CIELABAdvancedPanel.Visibility = _zone.CIELABAdvancedMode ? Visibility.Visible : Visibility.Collapsed;
            CIELABAtoBSlider.Value = _zone.CIELABAtoB;
            CIELABAtoBLabel.Text = $"a* → b* Transfer ({_zone.CIELABAtoB:F2})";
            CIELABBtoASlider.Value = _zone.CIELABBtoA;
            CIELABBtoALabel.Text = $"b* → a* Transfer ({_zone.CIELABBtoA:F2})";
            CIELABAEnhanceSlider.Value = _zone.CIELABAEnhance;
            CIELABAEnhanceLabel.Text = $"a* Enhancement ({_zone.CIELABAEnhance:F2})";
            CIELABBEnhanceSlider.Value = _zone.CIELABBEnhance;
            CIELABBEnhanceLabel.Text = $"b* Enhancement ({_zone.CIELABBEnhance:F2})";
            CIELABLEnhanceSlider.Value = _zone.CIELABLEnhance;
            CIELABLEnhanceLabel.Text = $"Lightness Encoding ({_zone.CIELABLEnhance:F2})";

            // Daltonization
            DaltonizationCVDCombo.SelectedIndex = MapDaltonizationCVDToComboIndex(_zone.DaltonizationCVDType);
            DaltonizationStrengthSlider.Value = _zone.DaltonizationStrength;
            DaltonizationStrengthLabel.Text = $"Strength ({_zone.DaltonizationStrength:F2})";

            // Simulation-Guided
            SimGuidedCheckBox.IsChecked = _zone.SimulationGuidedEnabled;
            SimGuidedPanel.Visibility = _zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
            SimGuidedMachadoRadio.IsChecked = _zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Machado;
            SimGuidedStrictRadio.IsChecked = _zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Strict;
            SimGuidedFilterCombo.SelectedIndex = MapFilterTypeToComboIndex(_zone.SimulationGuidedFilterType);
            SimGuidedSensitivitySlider.Value = _zone.SimulationGuidedSensitivity;
            SimGuidedSensitivityLabel.Text = $"Sensitivity ({_zone.SimulationGuidedSensitivity:F2})";

            // Post-Simulation
            PostSimCheckBox.IsChecked = _zone.PostCorrectionSimEnabled;
            PostSimPanel.Visibility = _zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
            PostSimMachadoRadio.IsChecked = _zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Machado;
            PostSimStrictRadio.IsChecked = _zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Strict;
            PostSimFilterCombo.SelectedIndex = MapFilterTypeToComboIndex(_zone.PostCorrectionSimFilterType);
            PostSimIntensitySlider.Value = _zone.PostCorrectionSimIntensity;
            PostSimIntensityLabel.Text = $"Simulation Intensity ({_zone.PostCorrectionSimIntensity:F2})";

            // Intensity
            IntensitySlider.Value = _zone.Intensity;
            IntensityLabel.Text = $"Intensity ({_zone.Intensity:F2})";
        }
        finally
        {
            _isLoading = false;
        }
    }

    #region Mode and Algorithm Handlers

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.Mode = (ZoneMode)ModeCombo.SelectedIndex;
        _effect.Configuration.Set(ConfigKey("mode"), ModeCombo.SelectedIndex);
        UpdateModeUI();
        OnSettingsChanged();
    }

    private void UpdateModeUI()
    {
        int mode = ModeCombo.SelectedIndex;
        SimulationPanel.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        CorrectionPanel.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;
        IntensityPanel.Visibility = mode > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Algorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.SimulationAlgorithm = StrictRadio.IsChecked == true ? SimulationAlgorithm.Strict : SimulationAlgorithm.Machado;
        _effect.Configuration.Set(ConfigKey("simAlgorithm"), (int)_zone.SimulationAlgorithm);
        OnSettingsChanged();
    }

    private void SimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.SimulationFilterType = MapComboIndexToFilterType(SimFilterCombo.SelectedIndex);
        _effect.Configuration.Set(ConfigKey("simFilterType"), _zone.SimulationFilterType);
        OnSettingsChanged();
    }

    private void Intensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.Intensity = (float)IntensitySlider.Value;
        IntensityLabel.Text = $"Intensity ({_zone.Intensity:F2})";
        _effect.Configuration.Set(ConfigKey("intensity"), _zone.Intensity);
        OnSettingsChanged();
    }

    #endregion

    #region Correction Algorithm Handlers

    private void CorrectionAlgorithm_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CorrectionAlgorithm = (CorrectionAlgorithm)CorrectionAlgorithmCombo.SelectedIndex;
        _effect.Configuration.Set(ConfigKey("correctionAlgorithm"), (int)_zone.CorrectionAlgorithm);
        UpdateCorrectionAlgorithmUI();
        OnSettingsChanged();
    }

    private void UpdateCorrectionAlgorithmUI()
    {
        int algorithm = CorrectionAlgorithmCombo.SelectedIndex;
        LUTPanel.Visibility = algorithm == 0 ? Visibility.Visible : Visibility.Collapsed;
        DaltonizationPanel.Visibility = algorithm == 1 ? Visibility.Visible : Visibility.Collapsed;
        HueRotationPanel.Visibility = algorithm == 2 ? Visibility.Visible : Visibility.Collapsed;
        CIELABPanel.Visibility = algorithm == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    #endregion

    #region Hue Rotation Handlers

    private void HueRotationCVD_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.HueRotationCVDType = (CVDCorrectionType)HueRotationCVDCombo.SelectedIndex;
        _effect.Configuration.Set(ConfigKey("hueRotCVDType"), (int)_zone.HueRotationCVDType);
        OnSettingsChanged();
    }

    private void HueRotationStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.HueRotationStrength = (float)HueRotationStrengthSlider.Value;
        HueRotationStrengthLabel.Text = $"Strength ({_zone.HueRotationStrength:F2})";
        _effect.Configuration.Set(ConfigKey("hueRotStrength"), _zone.HueRotationStrength);
        OnSettingsChanged();
    }

    private void HueRotationAdvanced_Changed(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.HueRotationAdvancedMode = HueRotationAdvancedCheckBox.IsChecked == true;
        HueRotationAdvancedPanel.Visibility = _zone.HueRotationAdvancedMode ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set(ConfigKey("hueRotAdvanced"), _zone.HueRotationAdvancedMode);
        OnSettingsChanged();
    }

    private void HueRotationSourceStart_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.HueRotationSourceStart = (float)HueRotationSourceStartSlider.Value;
        HueRotationSourceStartLabel.Text = $"Source Start ({_zone.HueRotationSourceStart:F0}°)";
        _effect.Configuration.Set(ConfigKey("hueRotSourceStart"), _zone.HueRotationSourceStart);
        OnSettingsChanged();
    }

    private void HueRotationSourceEnd_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.HueRotationSourceEnd = (float)HueRotationSourceEndSlider.Value;
        HueRotationSourceEndLabel.Text = $"Source End ({_zone.HueRotationSourceEnd:F0}°)";
        _effect.Configuration.Set(ConfigKey("hueRotSourceEnd"), _zone.HueRotationSourceEnd);
        OnSettingsChanged();
    }

    private void HueRotationShift_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.HueRotationShift = (float)HueRotationShiftSlider.Value;
        HueRotationShiftLabel.Text = $"Hue Shift ({(_zone.HueRotationShift >= 0 ? "+" : "")}{_zone.HueRotationShift:F0}°)";
        _effect.Configuration.Set(ConfigKey("hueRotShift"), _zone.HueRotationShift);
        OnSettingsChanged();
    }

    private void HueRotationFalloff_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.HueRotationFalloff = (float)HueRotationFalloffSlider.Value;
        HueRotationFalloffLabel.Text = $"Edge Falloff ({_zone.HueRotationFalloff:F2})";
        _effect.Configuration.Set(ConfigKey("hueRotFalloff"), _zone.HueRotationFalloff);
        OnSettingsChanged();
    }

    private void HueRotationReset_Click(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null) return;

        // Get defaults based on selected CVD type
        var (sourceStart, sourceEnd, shift, falloff) = GetHueRotationDefaults(_zone.HueRotationCVDType);

        _isLoading = true;
        try
        {
            // Apply defaults
            _zone.HueRotationSourceStart = sourceStart;
            _zone.HueRotationSourceEnd = sourceEnd;
            _zone.HueRotationShift = shift;
            _zone.HueRotationFalloff = falloff;

            // Update UI
            HueRotationSourceStartSlider.Value = sourceStart;
            HueRotationSourceStartLabel.Text = $"Source Start ({sourceStart:F0}°)";
            HueRotationSourceEndSlider.Value = sourceEnd;
            HueRotationSourceEndLabel.Text = $"Source End ({sourceEnd:F0}°)";
            HueRotationShiftSlider.Value = shift;
            HueRotationShiftLabel.Text = $"Hue Shift ({(shift >= 0 ? "+" : "")}{shift:F0}°)";
            HueRotationFalloffSlider.Value = falloff;
            HueRotationFalloffLabel.Text = $"Edge Falloff ({falloff:F2})";

            // Save to config
            _effect.Configuration.Set(ConfigKey("hueRotSourceStart"), sourceStart);
            _effect.Configuration.Set(ConfigKey("hueRotSourceEnd"), sourceEnd);
            _effect.Configuration.Set(ConfigKey("hueRotShift"), shift);
            _effect.Configuration.Set(ConfigKey("hueRotFalloff"), falloff);
        }
        finally
        {
            _isLoading = false;
        }
        OnSettingsChanged();
    }

    private static (float sourceStart, float sourceEnd, float shift, float falloff) GetHueRotationDefaults(CVDCorrectionType cvdType)
    {
        return cvdType switch
        {
            // Protan (red-blind/weak): Rotate reds (0-60°) toward yellow/orange
            CVDCorrectionType.Protanopia or CVDCorrectionType.Protanomaly
                => (0f, 60f, 40f, 0.3f),
            // Deutan (green-blind/weak): Rotate greens (60-150°) toward blue/cyan
            CVDCorrectionType.Deuteranopia or CVDCorrectionType.Deuteranomaly
                => (60f, 150f, 60f, 0.3f),
            // Tritan (blue-blind/weak): Rotate blues (180-270°) toward cyan/green
            CVDCorrectionType.Tritanopia or CVDCorrectionType.Tritanomaly
                => (180f, 270f, -40f, 0.3f),
            _ => (0f, 120f, 60f, 0.3f)
        };
    }

    #endregion

    #region CIELAB Handlers

    private void CIELABCVD_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABCVDType = (CVDCorrectionType)CIELABCVDCombo.SelectedIndex;
        _effect.Configuration.Set(ConfigKey("cielabCVDType"), (int)_zone.CIELABCVDType);
        OnSettingsChanged();
    }

    private void CIELABStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABStrength = (float)CIELABStrengthSlider.Value;
        CIELABStrengthLabel.Text = $"Strength ({_zone.CIELABStrength:F2})";
        _effect.Configuration.Set(ConfigKey("cielabStrength"), _zone.CIELABStrength);
        OnSettingsChanged();
    }

    private void CIELABAdvanced_Changed(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABAdvancedMode = CIELABAdvancedCheckBox.IsChecked == true;
        CIELABAdvancedPanel.Visibility = _zone.CIELABAdvancedMode ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set(ConfigKey("cielabAdvanced"), _zone.CIELABAdvancedMode);
        OnSettingsChanged();
    }

    private void CIELABAtoB_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABAtoB = (float)CIELABAtoBSlider.Value;
        CIELABAtoBLabel.Text = $"a* → b* Transfer ({_zone.CIELABAtoB:F2})";
        _effect.Configuration.Set(ConfigKey("cielabAtoB"), _zone.CIELABAtoB);
        OnSettingsChanged();
    }

    private void CIELABBtoA_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABBtoA = (float)CIELABBtoASlider.Value;
        CIELABBtoALabel.Text = $"b* → a* Transfer ({_zone.CIELABBtoA:F2})";
        _effect.Configuration.Set(ConfigKey("cielabBtoA"), _zone.CIELABBtoA);
        OnSettingsChanged();
    }

    private void CIELABAEnhance_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABAEnhance = (float)CIELABAEnhanceSlider.Value;
        CIELABAEnhanceLabel.Text = $"a* Enhancement ({_zone.CIELABAEnhance:F2})";
        _effect.Configuration.Set(ConfigKey("cielabAEnhance"), _zone.CIELABAEnhance);
        OnSettingsChanged();
    }

    private void CIELABBEnhance_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABBEnhance = (float)CIELABBEnhanceSlider.Value;
        CIELABBEnhanceLabel.Text = $"b* Enhancement ({_zone.CIELABBEnhance:F2})";
        _effect.Configuration.Set(ConfigKey("cielabBEnhance"), _zone.CIELABBEnhance);
        OnSettingsChanged();
    }

    private void CIELABLEnhance_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.CIELABLEnhance = (float)CIELABLEnhanceSlider.Value;
        CIELABLEnhanceLabel.Text = $"Lightness Encoding ({_zone.CIELABLEnhance:F2})";
        _effect.Configuration.Set(ConfigKey("cielabLEnhance"), _zone.CIELABLEnhance);
        OnSettingsChanged();
    }

    private void CIELABReset_Click(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null) return;

        // Get defaults based on selected CVD type
        var (aToB, bToA, aEnhance, bEnhance, lEnhance) = GetCIELABDefaults(_zone.CIELABCVDType);

        _isLoading = true;
        try
        {
            // Apply defaults
            _zone.CIELABAtoB = aToB;
            _zone.CIELABBtoA = bToA;
            _zone.CIELABAEnhance = aEnhance;
            _zone.CIELABBEnhance = bEnhance;
            _zone.CIELABLEnhance = lEnhance;

            // Update UI
            CIELABAtoBSlider.Value = aToB;
            CIELABAtoBLabel.Text = $"a* → b* Transfer ({aToB:F2})";
            CIELABBtoASlider.Value = bToA;
            CIELABBtoALabel.Text = $"b* → a* Transfer ({bToA:F2})";
            CIELABAEnhanceSlider.Value = aEnhance;
            CIELABAEnhanceLabel.Text = $"a* Enhancement ({aEnhance:F2})";
            CIELABBEnhanceSlider.Value = bEnhance;
            CIELABBEnhanceLabel.Text = $"b* Enhancement ({bEnhance:F2})";
            CIELABLEnhanceSlider.Value = lEnhance;
            CIELABLEnhanceLabel.Text = $"Lightness Encoding ({lEnhance:F2})";

            // Save to config
            _effect.Configuration.Set(ConfigKey("cielabAtoB"), aToB);
            _effect.Configuration.Set(ConfigKey("cielabBtoA"), bToA);
            _effect.Configuration.Set(ConfigKey("cielabAEnhance"), aEnhance);
            _effect.Configuration.Set(ConfigKey("cielabBEnhance"), bEnhance);
            _effect.Configuration.Set(ConfigKey("cielabLEnhance"), lEnhance);
        }
        finally
        {
            _isLoading = false;
        }
        OnSettingsChanged();
    }

    private static (float aToB, float bToA, float aEnhance, float bEnhance, float lEnhance) GetCIELABDefaults(CVDCorrectionType cvdType)
    {
        return cvdType switch
        {
            // Protan/Deutan: Transfer a* (red-green) info to b* (blue-yellow), enhance b*
            CVDCorrectionType.Protanopia or CVDCorrectionType.Protanomaly
                => (0.5f, 0f, 1f, 1.2f, 0.1f),
            CVDCorrectionType.Deuteranopia or CVDCorrectionType.Deuteranomaly
                => (0.5f, 0f, 1f, 1.2f, 0.1f),
            // Tritan: Transfer b* (blue-yellow) info to a* (red-green), enhance a*
            CVDCorrectionType.Tritanopia or CVDCorrectionType.Tritanomaly
                => (0f, 0.5f, 1.2f, 1f, 0.1f),
            _ => (0.5f, 0f, 1f, 1f, 0f)
        };
    }

    #endregion

    #region Daltonization Handlers

    private void DaltonizationCVD_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.DaltonizationCVDType = MapComboIndexToDaltonizationCVD(DaltonizationCVDCombo.SelectedIndex);
        _effect.Configuration.Set(ConfigKey("daltonizationCVDType"), _zone.DaltonizationCVDType);
        OnSettingsChanged();
    }

    private void DaltonizationStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.DaltonizationStrength = (float)DaltonizationStrengthSlider.Value;
        DaltonizationStrengthLabel.Text = $"Strength ({_zone.DaltonizationStrength:F2})";
        _effect.Configuration.Set(ConfigKey("daltonizationStrength"), _zone.DaltonizationStrength);
        OnSettingsChanged();
    }

    #endregion

    #region Simulation-Guided Handlers

    private void SimGuided_Changed(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.SimulationGuidedEnabled = SimGuidedCheckBox.IsChecked == true;
        SimGuidedPanel.Visibility = _zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set(ConfigKey("simGuidedEnabled"), _zone.SimulationGuidedEnabled);
        OnSettingsChanged();
    }

    private void SimGuidedAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.SimulationGuidedAlgorithm = SimGuidedStrictRadio.IsChecked == true ? SimulationAlgorithm.Strict : SimulationAlgorithm.Machado;
        _effect.Configuration.Set(ConfigKey("simGuidedAlgorithm"), (int)_zone.SimulationGuidedAlgorithm);
        OnSettingsChanged();
    }

    private void SimGuidedFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.SimulationGuidedFilterType = MapComboIndexToFilterType(SimGuidedFilterCombo.SelectedIndex);
        _effect.Configuration.Set(ConfigKey("simGuidedFilterType"), _zone.SimulationGuidedFilterType);
        OnSettingsChanged();
    }

    private void SimGuidedSensitivity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.SimulationGuidedSensitivity = (float)SimGuidedSensitivitySlider.Value;
        SimGuidedSensitivityLabel.Text = $"Sensitivity ({_zone.SimulationGuidedSensitivity:F2})";
        _effect.Configuration.Set(ConfigKey("simGuidedSensitivity"), _zone.SimulationGuidedSensitivity);
        OnSettingsChanged();
    }

    #endregion

    #region Post-Simulation Handlers

    private void PostSim_Changed(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.PostCorrectionSimEnabled = PostSimCheckBox.IsChecked == true;
        PostSimPanel.Visibility = _zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set(ConfigKey("postSimEnabled"), _zone.PostCorrectionSimEnabled);
        OnSettingsChanged();
    }

    private void PostSimAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.PostCorrectionSimAlgorithm = PostSimStrictRadio.IsChecked == true ? SimulationAlgorithm.Strict : SimulationAlgorithm.Machado;
        _effect.Configuration.Set(ConfigKey("postSimAlgorithm"), (int)_zone.PostCorrectionSimAlgorithm);
        OnSettingsChanged();
    }

    private void PostSimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.PostCorrectionSimFilterType = MapComboIndexToFilterType(PostSimFilterCombo.SelectedIndex);
        _effect.Configuration.Set(ConfigKey("postSimFilterType"), _zone.PostCorrectionSimFilterType);
        OnSettingsChanged();
    }

    private void PostSimIntensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_zone == null || _effect == null || _isLoading) return;
        _zone.PostCorrectionSimIntensity = (float)PostSimIntensitySlider.Value;
        PostSimIntensityLabel.Text = $"Simulation Intensity ({_zone.PostCorrectionSimIntensity:F2})";
        _effect.Configuration.Set(ConfigKey("postSimIntensity"), _zone.PostCorrectionSimIntensity);
        OnSettingsChanged();
    }

    #endregion

    #region Preset Management

    private void PopulatePresetComboBox()
    {
        if (_presetManager == null) return;

        var builtInPresets = CorrectionPresets.All;
        _builtInPresetCount = builtInPresets.Count;

        PresetCombo.Items.Clear();
        foreach (var preset in builtInPresets)
            PresetCombo.Items.Add(new ComboBoxItem { Content = preset.Name });

        if (_presetManager.CustomPresets.Count > 0)
        {
            PresetCombo.Items.Add(new Separator());
            foreach (var preset in _presetManager.CustomPresets)
                PresetCombo.Items.Add(new ComboBoxItem { Content = $"★ {preset.Name}" });
        }

        if (PresetCombo.Items.Count > 0)
            PresetCombo.SelectedIndex = 0;
    }

    private void RestoreSavedPresetSelection()
    {
        if (_effect == null) return;

        if (!_effect.Configuration.TryGet(ConfigKey("presetName"), out string? savedPresetName) ||
            string.IsNullOrEmpty(savedPresetName))
            return;

        // Find and select the preset
        for (int i = 0; i < PresetCombo.Items.Count; i++)
        {
            if (PresetCombo.Items[i] is ComboBoxItem item)
            {
                var itemContent = item.Content?.ToString() ?? "";
                var cleanName = itemContent.StartsWith("★ ") ? itemContent.Substring(2) : itemContent;
                if (cleanName == savedPresetName)
                {
                    PresetCombo.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePresetButtonStates();
    }

    private void UpdatePresetButtonStates()
    {
        if (PresetCombo.SelectedItem is Separator)
        {
            UpdatePresetButton.IsEnabled = false;
            DeletePresetButton.IsEnabled = false;
            return;
        }

        bool isCustomPreset = PresetCombo.SelectedIndex > _builtInPresetCount && _presetManager?.CustomPresets.Count > 0;
        UpdatePresetButton.IsEnabled = isCustomPreset;
        DeletePresetButton.IsEnabled = isCustomPreset;
    }

    private void ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || PresetCombo.SelectedIndex < 0) return;
        if (PresetCombo.SelectedItem is Separator) return;

        CorrectionPreset? preset = null;
        string presetName = "Custom";

        int selectedIndex = PresetCombo.SelectedIndex;
        if (selectedIndex < _builtInPresetCount)
        {
            preset = CorrectionPresets.All[selectedIndex];
            presetName = preset.Name;
        }
        else if (_presetManager != null && selectedIndex > _builtInPresetCount)
        {
            int customIndex = selectedIndex - _builtInPresetCount - 1;
            if (customIndex >= 0 && customIndex < _presetManager.CustomPresets.Count)
            {
                var customPreset = _presetManager.CustomPresets[customIndex];
                preset = customPreset.ToCorrectionPreset();
                presetName = customPreset.Name;
            }
        }

        if (preset == null) return;

        _zone.ApplyPreset(preset);
        _effect.Configuration.Set(ConfigKey("presetName"), presetName);

        // Refresh the CorrectionEditor to show new values
        CorrectionEditor.BindToZone(_zone);
        OnSettingsChanged();
    }

    private void SaveAsPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _presetManager == null) return;

        var dialog = new PresetNameDialog(_presetManager, "", true)
        {
            Owner = Window.GetWindow(this)
        };

        if (DialogHelper.WithSuspendedTopmost(() => dialog.ShowDialog()) == true && !string.IsNullOrWhiteSpace(dialog.PresetName))
        {
            var presetName = dialog.PresetName.Trim();
            var preset = CorrectionEditor.GetAsPreset(presetName);
            _presetManager.SaveCustomPreset(preset);
            _presetManager.LoadCustomPresets();

            ShowTopmostMessageBox($"Preset '{presetName}' saved successfully.", "Preset Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);

            PresetCreated?.Invoke();
        }
    }

    private void UpdatePreset_Click(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _presetManager == null) return;

        int selectedIndex = PresetCombo.SelectedIndex;
        if (selectedIndex <= _builtInPresetCount) return;

        int customIndex = selectedIndex - _builtInPresetCount - 1;
        if (customIndex < 0 || customIndex >= _presetManager.CustomPresets.Count) return;

        var existingPreset = _presetManager.CustomPresets[customIndex];
        var presetName = existingPreset.Name;

        var updatedPreset = CorrectionEditor.GetAsPreset(presetName);
        updatedPreset.CreatedDate = existingPreset.CreatedDate;
        _presetManager.SaveCustomPreset(updatedPreset);

        // Reload presets to get updated version
        _presetManager.LoadCustomPresets();

        ShowTopmostMessageBox($"Preset '{presetName}' updated successfully.", "Preset Updated",
            MessageBoxButton.OK, MessageBoxImage.Information);

        // Notify parent to reload this preset in other zones that have it selected
        PresetUpdated?.Invoke(presetName);
    }

    private void ExportPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_zone == null || _effect == null || _presetManager == null || PresetCombo.SelectedIndex < 0) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = GetPresetFileName()
        };

        if (DialogHelper.WithSuspendedTopmost(() => dialog.ShowDialog()) == true)
        {
            try
            {
                int selectedIndex = PresetCombo.SelectedIndex;
                if (selectedIndex < _builtInPresetCount)
                {
                    var preset = CorrectionPresets.All[selectedIndex];
                    _presetManager.ExportPreset(preset, dialog.FileName);
                }
                else if (selectedIndex > _builtInPresetCount)
                {
                    int customIndex = selectedIndex - _builtInPresetCount - 1;
                    if (customIndex >= 0 && customIndex < _presetManager.CustomPresets.Count)
                    {
                        var preset = _presetManager.CustomPresets[customIndex];
                        _presetManager.ExportPreset(preset, dialog.FileName);
                    }
                }

                ShowTopmostMessageBox("Preset exported successfully.", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowTopmostMessageBox($"Failed to export preset: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_presetManager == null) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (DialogHelper.WithSuspendedTopmost(() => dialog.ShowDialog()) == true)
        {
            try
            {
                var importedPreset = _presetManager.ImportPresetFromFile(dialog.FileName);
                if (importedPreset == null)
                {
                    ShowTopmostMessageBox("Failed to read preset file. The file may be invalid or corrupted.",
                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string finalName = importedPreset.Name;

                if (_presetManager.PresetExists(finalName))
                {
                    var conflictDialog = new PresetConflictDialog(_presetManager, finalName, _presetManager.GetUniqueName(finalName))
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (DialogHelper.WithSuspendedTopmost(() => conflictDialog.ShowDialog()) != true)
                        return;

                    if (conflictDialog.Resolution == ConflictResolution.Rename)
                        finalName = conflictDialog.NewName;
                }

                _presetManager.SaveImportedPreset(importedPreset, finalName);
                _presetManager.LoadCustomPresets();

                ShowTopmostMessageBox($"Preset '{finalName}' imported successfully.", "Import Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                PresetCreated?.Invoke();
            }
            catch (Exception ex)
            {
                ShowTopmostMessageBox($"Failed to import preset: {ex.Message}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DeletePreset_Click(object sender, RoutedEventArgs e)
    {
        if (_presetManager == null) return;

        int selectedIndex = PresetCombo.SelectedIndex;
        if (selectedIndex <= _builtInPresetCount || _presetManager.CustomPresets.Count == 0)
            return;

        int customIndex = selectedIndex - _builtInPresetCount - 1;
        if (customIndex < 0 || customIndex >= _presetManager.CustomPresets.Count)
            return;

        var presetName = _presetManager.CustomPresets[customIndex].Name;

        var result = DialogHelper.WithSuspendedTopmost(() =>
            System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the preset \"{presetName}\"?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning));

        if (result != MessageBoxResult.Yes)
            return;

        if (_presetManager.DeleteCustomPreset(presetName))
        {
            ShowTopmostMessageBox($"Preset '{presetName}' deleted successfully.", "Preset Deleted",
                MessageBoxButton.OK, MessageBoxImage.Information);

            PresetDeleted?.Invoke();
        }
        else
        {
            ShowTopmostMessageBox($"Failed to delete preset '{presetName}'.", "Delete Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetPresetFileName()
    {
        if (PresetCombo.SelectedItem is ComboBoxItem item && item.Content is string content)
        {
            var name = content.StartsWith("★ ") ? content.Substring(2) : content;
            return name.Replace(" ", "_").ToLowerInvariant();
        }
        return "preset";
    }

    #endregion

    #region Filter Type Mapping Helpers

    private static int MapFilterTypeToComboIndex(int filterType)
    {
        // Filter types: 0=None, 1-6=Machado types, 7-12=Strict types (same as 1-6), 13-14=Achro
        if (filterType == 0) return 0;
        if (filterType >= 1 && filterType <= 6) return filterType;
        if (filterType >= 7 && filterType <= 12) return filterType - 6;
        if (filterType == 13) return 7; // Achromatopsia
        if (filterType == 14) return 8; // Achromatomaly
        return 0;
    }

    private static int MapComboIndexToFilterType(int comboIndex)
    {
        // Combo: 0=None, 1-6=CVD types, 7=Achro, 8=Achromaly
        if (comboIndex <= 6) return comboIndex;
        if (comboIndex == 7) return 13;
        if (comboIndex == 8) return 14;
        return 0;
    }

    private static int MapDaltonizationCVDToComboIndex(int cvdType)
    {
        // Daltonization CVD types: 1-6 for Machado, 7-12 for Strict
        if (cvdType >= 1 && cvdType <= 6) return cvdType - 1;
        if (cvdType >= 7 && cvdType <= 12) return cvdType - 7;
        return 2; // Default to Deuteranopia
    }

    private static int MapComboIndexToDaltonizationCVD(int comboIndex)
    {
        // Daltonization uses Machado filter types (1-6)
        return comboIndex + 1;
    }

    #endregion

    #region Helper Methods

    private static void ShowTopmostMessageBox(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        DialogHelper.WithSuspendedTopmost(() =>
        {
            System.Windows.MessageBox.Show(message, caption, button, icon);
        });
    }

    #endregion
}
