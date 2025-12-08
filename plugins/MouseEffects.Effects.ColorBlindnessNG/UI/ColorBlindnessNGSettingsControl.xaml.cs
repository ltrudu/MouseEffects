using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;
using MouseEffects.Core.UI;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

public partial class ColorBlindnessNGSettingsControl : UserControl
{
    private ColorBlindnessNGEffect? _effect;
    private bool _isLoading;
    private bool _isExpanded;
    private PresetManager _presetManager = new();
    private int _builtInPresetCount;

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
            LoadConfiguration();
            PopulatePresetComboBoxes();
            RestoreSavedPresetSelections();
            InitializeCorrectionEditors();
        }
    }

    private void InitializeCorrectionEditors()
    {
        if (_effect == null) return;

        // Bind each CorrectionEditor to its corresponding zone
        Zone0CorrectionEditor.BindToZone(_effect.GetZone(0));
        Zone1CorrectionEditor.BindToZone(_effect.GetZone(1));
        Zone2CorrectionEditor.BindToZone(_effect.GetZone(2));
        Zone3CorrectionEditor.BindToZone(_effect.GetZone(3));

        // Subscribe to settings changes to save config
        Zone0CorrectionEditor.SettingsChanged += (s, e) => SaveZoneConfiguration(0);
        Zone1CorrectionEditor.SettingsChanged += (s, e) => SaveZoneConfiguration(1);
        Zone2CorrectionEditor.SettingsChanged += (s, e) => SaveZoneConfiguration(2);
        Zone3CorrectionEditor.SettingsChanged += (s, e) => SaveZoneConfiguration(3);
    }

    private void SaveZoneConfiguration(int zoneIndex)
    {
        if (_effect == null) return;
        var zone = _effect.GetZone(zoneIndex);
        var prefix = $"zone{zoneIndex}_";

        // Correction algorithm settings
        _effect.Configuration.Set(prefix + "correctionAlgorithm", (int)zone.CorrectionAlgorithm);
        _effect.Configuration.Set(prefix + "daltonizationCVDType", zone.DaltonizationCVDType);
        _effect.Configuration.Set(prefix + "daltonizationStrength", zone.DaltonizationStrength);

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

    private void PopulatePresetComboBoxes()
    {
        var builtInPresets = CorrectionPresets.All;
        _builtInPresetCount = builtInPresets.Count;

        // Build combined list with separator
        var presetItems = BuildPresetItemsList();

        Zone0PresetCombo.ItemsSource = presetItems;
        Zone1PresetCombo.ItemsSource = BuildPresetItemsList();
        Zone2PresetCombo.ItemsSource = BuildPresetItemsList();
        Zone3PresetCombo.ItemsSource = BuildPresetItemsList();

        if (presetItems.Count > 0)
        {
            Zone0PresetCombo.SelectedIndex = 0;
            Zone1PresetCombo.SelectedIndex = 0;
            Zone2PresetCombo.SelectedIndex = 0;
            Zone3PresetCombo.SelectedIndex = 0;
        }

        UpdateAllSaveButtonStates();
    }

    private List<object> BuildPresetItemsList()
    {
        var presetItems = new List<object>();

        // Add built-in presets
        foreach (var preset in CorrectionPresets.All)
        {
            presetItems.Add(preset.Name);
        }

        // Add separator and custom presets if any exist
        if (_presetManager.CustomPresets.Count > 0)
        {
            presetItems.Add(new Separator());
            foreach (var customPreset in _presetManager.CustomPresets)
            {
                presetItems.Add($"* {customPreset.Name}");
            }
        }

        return presetItems;
    }

    private void Zone0PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSaveButtonState(Zone0PresetCombo, SavePresetButton);
    }

    private void Zone1PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSaveButtonState(Zone1PresetCombo, Zone1SavePresetButton);
    }

    private void Zone2PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSaveButtonState(Zone2PresetCombo, Zone2SavePresetButton);
    }

    private void Zone3PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSaveButtonState(Zone3PresetCombo, Zone3SavePresetButton);
    }

    private void UpdateAllSaveButtonStates()
    {
        UpdateSaveButtonState(Zone0PresetCombo, SavePresetButton);
        UpdateSaveButtonState(Zone1PresetCombo, Zone1SavePresetButton);
        UpdateSaveButtonState(Zone2PresetCombo, Zone2SavePresetButton);
        UpdateSaveButtonState(Zone3PresetCombo, Zone3SavePresetButton);
    }

    private void UpdateSaveButtonState(ComboBox combo, Button saveButton)
    {
        if (combo.SelectedItem is Separator)
        {
            saveButton.IsEnabled = false;
            return;
        }

        // Enable Save button only for custom presets (after built-in count + separator)
        int selectedIndex = combo.SelectedIndex;
        bool isCustomPreset = selectedIndex > _builtInPresetCount && _presetManager.CustomPresets.Count > 0;
        saveButton.IsEnabled = isCustomPreset;
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

            // Load all zone settings
            LoadZone0Settings();
            LoadZone1Settings();
            LoadZone2Settings();
            LoadZone3Settings();

            // Update UI visibility
            UpdateSplitModeUI((int)_effect.SplitMode);
            UpdateZone0ModeUI();
            UpdateZone1ModeUI();
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

    private void LoadZone0Settings()
    {
        if (_effect == null) return;
        var zone = _effect.GetZone(0);

        Zone0ModeCombo.SelectedIndex = (int)zone.Mode;
        Zone0MachadoRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Machado;
        Zone0StrictRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Strict;

        // Map simulation filter type to combo index
        int filterIndex = zone.SimulationFilterType;
        if (filterIndex >= 13) filterIndex = filterIndex - 5; // Achro types
        else if (filterIndex >= 7) filterIndex = filterIndex - 6; // Strict -> same display
        Zone0SimFilterCombo.SelectedIndex = Math.Min(filterIndex, Zone0SimFilterCombo.Items.Count - 1);

        // Correction algorithm settings
        Zone0CorrectionAlgorithmCombo.SelectedIndex = (int)zone.CorrectionAlgorithm;
        UpdateZone0CorrectionAlgorithmUI();

        // Daltonization settings
        int daltonCVDIndex = zone.DaltonizationCVDType - 1; // 1-6 -> 0-5
        Zone0DaltonizationCVDCombo.SelectedIndex = Math.Max(0, Math.Min(daltonCVDIndex, 5));
        Zone0DaltonizationStrengthSlider.Value = zone.DaltonizationStrength;
        Zone0DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";

        Zone0IntensitySlider.Value = zone.Intensity;
        Zone0IntensityLabel.Text = $"Intensity ({zone.Intensity:F2})";

        // Simulation-guided correction settings
        Zone0SimGuidedCheckBox.IsChecked = zone.SimulationGuidedEnabled;
        Zone0SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone0SimGuidedMachadoRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Machado;
        Zone0SimGuidedStrictRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Strict;
        // Map filter type to combo index (1-6 -> 0-5, 13-14 -> 6-7)
        int simGuidedFilterIndex = zone.SimulationGuidedFilterType;
        if (simGuidedFilterIndex >= 13) simGuidedFilterIndex = simGuidedFilterIndex - 7; // 13->6, 14->7
        else if (simGuidedFilterIndex >= 1) simGuidedFilterIndex = simGuidedFilterIndex - 1; // 1->0, 6->5
        Zone0SimGuidedFilterCombo.SelectedIndex = Math.Max(0, Math.Min(simGuidedFilterIndex, Zone0SimGuidedFilterCombo.Items.Count - 1));
        Zone0SimGuidedSensitivitySlider.Value = zone.SimulationGuidedSensitivity;
        Zone0SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";

        // Post-correction simulation settings
        Zone0PostSimCheckBox.IsChecked = zone.PostCorrectionSimEnabled;
        Zone0PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone0PostSimMachadoRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Machado;
        Zone0PostSimStrictRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Strict;
        // Map filter type to combo index (1-6 -> 0-5, 13-14 -> 6-7)
        int postSimFilterIndex = zone.PostCorrectionSimFilterType;
        if (postSimFilterIndex >= 13) postSimFilterIndex = postSimFilterIndex - 7; // 13->6, 14->7
        else if (postSimFilterIndex >= 1) postSimFilterIndex = postSimFilterIndex - 1; // 1->0, 6->5
        Zone0PostSimFilterCombo.SelectedIndex = Math.Max(0, Math.Min(postSimFilterIndex, Zone0PostSimFilterCombo.Items.Count - 1));
        Zone0PostSimIntensitySlider.Value = zone.PostCorrectionSimIntensity;
        Zone0PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";

        // Channel settings are loaded via CorrectionEditor in InitializeCorrectionEditors()
    }

    private void LoadZone1Settings()
    {
        if (_effect == null) return;
        var zone = _effect.GetZone(1);

        Zone1ModeCombo.SelectedIndex = (int)zone.Mode;
        Zone1MachadoRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Machado;
        Zone1StrictRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Strict;

        int filterIndex = zone.SimulationFilterType;
        if (filterIndex >= 13) filterIndex = filterIndex - 5;
        else if (filterIndex >= 7) filterIndex = filterIndex - 6;
        Zone1SimFilterCombo.SelectedIndex = Math.Min(filterIndex, Zone1SimFilterCombo.Items.Count - 1);

        // Correction algorithm settings
        Zone1CorrectionAlgorithmCombo.SelectedIndex = (int)zone.CorrectionAlgorithm;
        UpdateZone1CorrectionAlgorithmUI();

        // Daltonization settings
        int daltonCVDIndex = zone.DaltonizationCVDType - 1;
        Zone1DaltonizationCVDCombo.SelectedIndex = Math.Max(0, Math.Min(daltonCVDIndex, 5));
        Zone1DaltonizationStrengthSlider.Value = zone.DaltonizationStrength;
        Zone1DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";

        Zone1IntensitySlider.Value = zone.Intensity;
        Zone1IntensityLabel.Text = $"Intensity ({zone.Intensity:F2})";

        // Simulation-guided correction settings
        Zone1SimGuidedCheckBox.IsChecked = zone.SimulationGuidedEnabled;
        Zone1SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone1SimGuidedMachadoRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Machado;
        Zone1SimGuidedStrictRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Strict;
        int simGuidedFilterIndex = zone.SimulationGuidedFilterType;
        if (simGuidedFilterIndex >= 13) simGuidedFilterIndex = simGuidedFilterIndex - 7;
        else if (simGuidedFilterIndex >= 1) simGuidedFilterIndex = simGuidedFilterIndex - 1;
        Zone1SimGuidedFilterCombo.SelectedIndex = Math.Max(0, Math.Min(simGuidedFilterIndex, Zone1SimGuidedFilterCombo.Items.Count - 1));
        Zone1SimGuidedSensitivitySlider.Value = zone.SimulationGuidedSensitivity;
        Zone1SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";

        // Post-correction simulation settings
        Zone1PostSimCheckBox.IsChecked = zone.PostCorrectionSimEnabled;
        Zone1PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone1PostSimMachadoRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Machado;
        Zone1PostSimStrictRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Strict;
        int postSimFilterIndex = zone.PostCorrectionSimFilterType;
        if (postSimFilterIndex >= 13) postSimFilterIndex = postSimFilterIndex - 7;
        else if (postSimFilterIndex >= 1) postSimFilterIndex = postSimFilterIndex - 1;
        Zone1PostSimFilterCombo.SelectedIndex = Math.Max(0, Math.Min(postSimFilterIndex, Zone1PostSimFilterCombo.Items.Count - 1));
        Zone1PostSimIntensitySlider.Value = zone.PostCorrectionSimIntensity;
        Zone1PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";
    }

    private void LoadZone2Settings()
    {
        if (_effect == null) return;
        var zone = _effect.GetZone(2);

        Zone2ModeCombo.SelectedIndex = (int)zone.Mode;
        Zone2MachadoRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Machado;
        Zone2StrictRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Strict;

        // Map simulation filter type to combo index (full list like Zone0/Zone1)
        int filterIndex = zone.SimulationFilterType;
        if (filterIndex >= 13) filterIndex = filterIndex - 5; // Achro types
        else if (filterIndex >= 7) filterIndex = filterIndex - 6; // Strict -> same display
        Zone2SimFilterCombo.SelectedIndex = Math.Min(filterIndex, Zone2SimFilterCombo.Items.Count - 1);

        // Correction algorithm settings
        Zone2CorrectionAlgorithmCombo.SelectedIndex = (int)zone.CorrectionAlgorithm;
        UpdateZone2CorrectionAlgorithmUI();

        // Daltonization settings
        int daltonCVDIndex = zone.DaltonizationCVDType - 1;
        Zone2DaltonizationCVDCombo.SelectedIndex = Math.Max(0, Math.Min(daltonCVDIndex, 5));
        Zone2DaltonizationStrengthSlider.Value = zone.DaltonizationStrength;
        Zone2DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";

        // Simulation-guided correction settings
        Zone2SimGuidedCheckBox.IsChecked = zone.SimulationGuidedEnabled;
        Zone2SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone2SimGuidedMachadoRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Machado;
        Zone2SimGuidedStrictRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Strict;
        int simGuidedFilterIndex = zone.SimulationGuidedFilterType;
        if (simGuidedFilterIndex >= 13) simGuidedFilterIndex = simGuidedFilterIndex - 7;
        else if (simGuidedFilterIndex >= 1) simGuidedFilterIndex = simGuidedFilterIndex - 1;
        Zone2SimGuidedFilterCombo.SelectedIndex = Math.Max(0, Math.Min(simGuidedFilterIndex, Zone2SimGuidedFilterCombo.Items.Count - 1));
        Zone2SimGuidedSensitivitySlider.Value = zone.SimulationGuidedSensitivity;
        Zone2SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";

        // Post-correction simulation settings
        Zone2PostSimCheckBox.IsChecked = zone.PostCorrectionSimEnabled;
        Zone2PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone2PostSimMachadoRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Machado;
        Zone2PostSimStrictRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Strict;
        int postSimFilterIndex = zone.PostCorrectionSimFilterType;
        if (postSimFilterIndex >= 13) postSimFilterIndex = postSimFilterIndex - 7;
        else if (postSimFilterIndex >= 1) postSimFilterIndex = postSimFilterIndex - 1;
        Zone2PostSimFilterCombo.SelectedIndex = Math.Max(0, Math.Min(postSimFilterIndex, Zone2PostSimFilterCombo.Items.Count - 1));
        Zone2PostSimIntensitySlider.Value = zone.PostCorrectionSimIntensity;
        Zone2PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";

        // Update panel visibility based on mode
        Zone2SimulationPanel.Visibility = zone.Mode == ZoneMode.Simulation ? Visibility.Visible : Visibility.Collapsed;
        Zone2CorrectionPanel.Visibility = zone.Mode == ZoneMode.Correction ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadZone3Settings()
    {
        if (_effect == null) return;
        var zone = _effect.GetZone(3);

        Zone3ModeCombo.SelectedIndex = (int)zone.Mode;
        Zone3MachadoRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Machado;
        Zone3StrictRadio.IsChecked = zone.SimulationAlgorithm == SimulationAlgorithm.Strict;

        // Map simulation filter type to combo index (full list like Zone0/Zone1)
        int filterIndex = zone.SimulationFilterType;
        if (filterIndex >= 13) filterIndex = filterIndex - 5; // Achro types
        else if (filterIndex >= 7) filterIndex = filterIndex - 6; // Strict -> same display
        Zone3SimFilterCombo.SelectedIndex = Math.Min(filterIndex, Zone3SimFilterCombo.Items.Count - 1);

        // Correction algorithm settings
        Zone3CorrectionAlgorithmCombo.SelectedIndex = (int)zone.CorrectionAlgorithm;
        UpdateZone3CorrectionAlgorithmUI();

        // Daltonization settings
        int daltonCVDIndex = zone.DaltonizationCVDType - 1;
        Zone3DaltonizationCVDCombo.SelectedIndex = Math.Max(0, Math.Min(daltonCVDIndex, 5));
        Zone3DaltonizationStrengthSlider.Value = zone.DaltonizationStrength;
        Zone3DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";

        // Simulation-guided correction settings
        Zone3SimGuidedCheckBox.IsChecked = zone.SimulationGuidedEnabled;
        Zone3SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone3SimGuidedMachadoRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Machado;
        Zone3SimGuidedStrictRadio.IsChecked = zone.SimulationGuidedAlgorithm == SimulationAlgorithm.Strict;
        int simGuidedFilterIndex = zone.SimulationGuidedFilterType;
        if (simGuidedFilterIndex >= 13) simGuidedFilterIndex = simGuidedFilterIndex - 7;
        else if (simGuidedFilterIndex >= 1) simGuidedFilterIndex = simGuidedFilterIndex - 1;
        Zone3SimGuidedFilterCombo.SelectedIndex = Math.Max(0, Math.Min(simGuidedFilterIndex, Zone3SimGuidedFilterCombo.Items.Count - 1));
        Zone3SimGuidedSensitivitySlider.Value = zone.SimulationGuidedSensitivity;
        Zone3SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";

        // Post-correction simulation settings
        Zone3PostSimCheckBox.IsChecked = zone.PostCorrectionSimEnabled;
        Zone3PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        Zone3PostSimMachadoRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Machado;
        Zone3PostSimStrictRadio.IsChecked = zone.PostCorrectionSimAlgorithm == SimulationAlgorithm.Strict;
        int postSimFilterIndex = zone.PostCorrectionSimFilterType;
        if (postSimFilterIndex >= 13) postSimFilterIndex = postSimFilterIndex - 7;
        else if (postSimFilterIndex >= 1) postSimFilterIndex = postSimFilterIndex - 1;
        Zone3PostSimFilterCombo.SelectedIndex = Math.Max(0, Math.Min(postSimFilterIndex, Zone3PostSimFilterCombo.Items.Count - 1));
        Zone3PostSimIntensitySlider.Value = zone.PostCorrectionSimIntensity;
        Zone3PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";

        // Update panel visibility based on mode
        Zone3SimulationPanel.Visibility = zone.Mode == ZoneMode.Simulation ? Visibility.Visible : Visibility.Collapsed;
        Zone3CorrectionPanel.Visibility = zone.Mode == ZoneMode.Correction ? Visibility.Visible : Visibility.Collapsed;
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

    #region Zone 0 Event Handlers

    private void Zone0ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.Mode = (ZoneMode)Zone0ModeCombo.SelectedIndex;
        _effect.Configuration.Set("zone0_mode", Zone0ModeCombo.SelectedIndex);
        UpdateZone0ModeUI();
    }

    private void UpdateZone0ModeUI()
    {
        if (Zone0SimulationPanel == null) return;

        int mode = Zone0ModeCombo.SelectedIndex;
        bool isSimulation = mode == 1;
        bool isCorrection = mode == 2;
        bool hasProcessing = mode > 0;

        Zone0SimulationPanel.Visibility = isSimulation ? Visibility.Visible : Visibility.Collapsed;
        Zone0CorrectionPanel.Visibility = isCorrection ? Visibility.Visible : Visibility.Collapsed;
        Zone0IntensityPanel.Visibility = hasProcessing ? Visibility.Visible : Visibility.Collapsed;

        // Channel panels are now managed by CorrectionEditor
    }

    private void Zone0Algorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.SimulationAlgorithm = Zone0StrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone0_simAlgorithm", (int)zone.SimulationAlgorithm);
    }

    private void Zone0SimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        int index = Zone0SimFilterCombo.SelectedIndex;

        // Map combo index to filter type
        // 0=None, 1-6=Normal types, 7-8=Achro
        if (index <= 6)
            zone.SimulationFilterType = index;
        else
            zone.SimulationFilterType = index + 5; // 7->13, 8->14

        _effect.Configuration.Set("zone0_simFilterType", zone.SimulationFilterType);
    }

    private void Zone0Intensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.Intensity = (float)Zone0IntensitySlider.Value;
        _effect.Configuration.Set("zone0_intensity", zone.Intensity);
        Zone0IntensityLabel.Text = $"Intensity ({zone.Intensity:F2})";
    }

    private void Zone0CorrectionAlgorithm_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.CorrectionAlgorithm = (CorrectionAlgorithm)Zone0CorrectionAlgorithmCombo.SelectedIndex;
        _effect.Configuration.Set("zone0_correctionAlgorithm", (int)zone.CorrectionAlgorithm);
        UpdateZone0CorrectionAlgorithmUI();
    }

    private void UpdateZone0CorrectionAlgorithmUI()
    {
        if (Zone0DaltonizationPanel == null || Zone0LUTPanel == null) return;

        bool isDaltonization = Zone0CorrectionAlgorithmCombo.SelectedIndex == 1;
        Zone0DaltonizationPanel.Visibility = isDaltonization ? Visibility.Visible : Visibility.Collapsed;
        Zone0LUTPanel.Visibility = isDaltonization ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Zone0DaltonizationCVD_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        // Map combo index (0-5) to CVD type (1-6 for Machado)
        zone.DaltonizationCVDType = Zone0DaltonizationCVDCombo.SelectedIndex + 1;
        _effect.Configuration.Set("zone0_daltonizationCVDType", zone.DaltonizationCVDType);
    }

    private void Zone0DaltonizationStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.DaltonizationStrength = (float)Zone0DaltonizationStrengthSlider.Value;
        _effect.Configuration.Set("zone0_daltonizationStrength", zone.DaltonizationStrength);
        Zone0DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";
    }

    private void Zone0ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(0, Zone0PresetCombo);
        Zone0CorrectionEditor.LoadFromZone();
        SaveZoneConfiguration(0);
    }

    #endregion

    #region Zone 1 Event Handlers

    private void Zone1ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.Mode = (ZoneMode)Zone1ModeCombo.SelectedIndex;
        _effect.Configuration.Set("zone1_mode", Zone1ModeCombo.SelectedIndex);
        UpdateZone1ModeUI();
    }

    private void UpdateZone1ModeUI()
    {
        if (Zone1SimulationPanel == null) return;

        int mode = Zone1ModeCombo.SelectedIndex;
        Zone1SimulationPanel.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        Zone1CorrectionPanel.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;
        Zone1IntensityPanel.Visibility = mode > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Zone1Algorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.SimulationAlgorithm = Zone1StrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone1_simAlgorithm", (int)zone.SimulationAlgorithm);
    }

    private void Zone1SimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        int index = Zone1SimFilterCombo.SelectedIndex;
        if (index <= 6)
            zone.SimulationFilterType = index;
        else
            zone.SimulationFilterType = index + 5;
        _effect.Configuration.Set("zone1_simFilterType", zone.SimulationFilterType);
    }

    private void Zone1Intensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.Intensity = (float)Zone1IntensitySlider.Value;
        _effect.Configuration.Set("zone1_intensity", zone.Intensity);
        Zone1IntensityLabel.Text = $"Intensity ({zone.Intensity:F2})";
    }

    private void Zone1CorrectionAlgorithm_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.CorrectionAlgorithm = (CorrectionAlgorithm)Zone1CorrectionAlgorithmCombo.SelectedIndex;
        _effect.Configuration.Set("zone1_correctionAlgorithm", (int)zone.CorrectionAlgorithm);
        UpdateZone1CorrectionAlgorithmUI();
    }

    private void UpdateZone1CorrectionAlgorithmUI()
    {
        if (Zone1DaltonizationPanel == null || Zone1LUTPanel == null) return;

        bool isDaltonization = Zone1CorrectionAlgorithmCombo.SelectedIndex == 1;
        Zone1DaltonizationPanel.Visibility = isDaltonization ? Visibility.Visible : Visibility.Collapsed;
        Zone1LUTPanel.Visibility = isDaltonization ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Zone1DaltonizationCVD_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.DaltonizationCVDType = Zone1DaltonizationCVDCombo.SelectedIndex + 1;
        _effect.Configuration.Set("zone1_daltonizationCVDType", zone.DaltonizationCVDType);
    }

    private void Zone1DaltonizationStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.DaltonizationStrength = (float)Zone1DaltonizationStrengthSlider.Value;
        _effect.Configuration.Set("zone1_daltonizationStrength", zone.DaltonizationStrength);
        Zone1DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";
    }

    private void Zone1ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(1, Zone1PresetCombo);
        Zone1CorrectionEditor.LoadFromZone();
        _effect?.Configuration.Set("zone1_mode", (int)ZoneMode.Correction);
        Zone1ModeCombo.SelectedIndex = 2;
        SaveZoneConfiguration(1);
    }

    #endregion

    #region Zone 2 & 3 Event Handlers

    private void Zone2ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.Mode = (ZoneMode)Zone2ModeCombo.SelectedIndex;
        _effect.Configuration.Set("zone2_mode", Zone2ModeCombo.SelectedIndex);

        Zone2SimulationPanel.Visibility = zone.Mode == ZoneMode.Simulation ? Visibility.Visible : Visibility.Collapsed;
        Zone2CorrectionPanel.Visibility = zone.Mode == ZoneMode.Correction ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Zone2Algorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.SimulationAlgorithm = Zone2StrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone2_simAlgorithm", (int)zone.SimulationAlgorithm);
    }

    private void Zone2SimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        int index = Zone2SimFilterCombo.SelectedIndex;
        // Full filter list: 0=None, 1-6=Normal types, 7-8=Achro
        if (index <= 6)
            zone.SimulationFilterType = index;
        else
            zone.SimulationFilterType = index + 5; // 7->13, 8->14
        _effect.Configuration.Set("zone2_simFilterType", zone.SimulationFilterType);
    }

    private void Zone2ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(2, Zone2PresetCombo);
        Zone2CorrectionEditor.LoadFromZone();
        SaveZoneConfiguration(2);
    }

    private void Zone2CorrectionAlgorithm_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.CorrectionAlgorithm = (CorrectionAlgorithm)Zone2CorrectionAlgorithmCombo.SelectedIndex;
        _effect.Configuration.Set("zone2_correctionAlgorithm", (int)zone.CorrectionAlgorithm);
        UpdateZone2CorrectionAlgorithmUI();
    }

    private void UpdateZone2CorrectionAlgorithmUI()
    {
        if (Zone2DaltonizationPanel == null || Zone2LUTPanel == null) return;

        bool isDaltonization = Zone2CorrectionAlgorithmCombo.SelectedIndex == 1;
        Zone2DaltonizationPanel.Visibility = isDaltonization ? Visibility.Visible : Visibility.Collapsed;
        Zone2LUTPanel.Visibility = isDaltonization ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Zone2DaltonizationCVD_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.DaltonizationCVDType = Zone2DaltonizationCVDCombo.SelectedIndex + 1;
        _effect.Configuration.Set("zone2_daltonizationCVDType", zone.DaltonizationCVDType);
    }

    private void Zone2DaltonizationStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.DaltonizationStrength = (float)Zone2DaltonizationStrengthSlider.Value;
        _effect.Configuration.Set("zone2_daltonizationStrength", zone.DaltonizationStrength);
        Zone2DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";
    }

    private void Zone3ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.Mode = (ZoneMode)Zone3ModeCombo.SelectedIndex;
        _effect.Configuration.Set("zone3_mode", Zone3ModeCombo.SelectedIndex);

        Zone3SimulationPanel.Visibility = zone.Mode == ZoneMode.Simulation ? Visibility.Visible : Visibility.Collapsed;
        Zone3CorrectionPanel.Visibility = zone.Mode == ZoneMode.Correction ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Zone3Algorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.SimulationAlgorithm = Zone3StrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone3_simAlgorithm", (int)zone.SimulationAlgorithm);
    }

    private void Zone3SimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        int index = Zone3SimFilterCombo.SelectedIndex;
        // Full filter list: 0=None, 1-6=Normal types, 7-8=Achro
        if (index <= 6)
            zone.SimulationFilterType = index;
        else
            zone.SimulationFilterType = index + 5; // 7->13, 8->14
        _effect.Configuration.Set("zone3_simFilterType", zone.SimulationFilterType);
    }

    private void Zone3ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(3, Zone3PresetCombo);
        Zone3CorrectionEditor.LoadFromZone();
        SaveZoneConfiguration(3);
    }

    private void Zone3CorrectionAlgorithm_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.CorrectionAlgorithm = (CorrectionAlgorithm)Zone3CorrectionAlgorithmCombo.SelectedIndex;
        _effect.Configuration.Set("zone3_correctionAlgorithm", (int)zone.CorrectionAlgorithm);
        UpdateZone3CorrectionAlgorithmUI();
    }

    private void UpdateZone3CorrectionAlgorithmUI()
    {
        if (Zone3DaltonizationPanel == null || Zone3LUTPanel == null) return;

        bool isDaltonization = Zone3CorrectionAlgorithmCombo.SelectedIndex == 1;
        Zone3DaltonizationPanel.Visibility = isDaltonization ? Visibility.Visible : Visibility.Collapsed;
        Zone3LUTPanel.Visibility = isDaltonization ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Zone3DaltonizationCVD_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.DaltonizationCVDType = Zone3DaltonizationCVDCombo.SelectedIndex + 1;
        _effect.Configuration.Set("zone3_daltonizationCVDType", zone.DaltonizationCVDType);
    }

    private void Zone3DaltonizationStrength_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.DaltonizationStrength = (float)Zone3DaltonizationStrengthSlider.Value;
        _effect.Configuration.Set("zone3_daltonizationStrength", zone.DaltonizationStrength);
        Zone3DaltonizationStrengthLabel.Text = $"Strength ({zone.DaltonizationStrength:F2})";
    }

    // Zone 1 custom preset handlers
    private void Zone1SaveAsPreset_Click(object sender, RoutedEventArgs e) => SaveAsPresetForZone(1);
    private void Zone1SavePreset_Click(object sender, RoutedEventArgs e) => SavePresetForZone(1, Zone1PresetCombo);
    private void Zone1ExportPreset_Click(object sender, RoutedEventArgs e) => ExportPresetForZone(Zone1PresetCombo);
    private void Zone1ImportPreset_Click(object sender, RoutedEventArgs e) => ImportPreset();

    // Zone 2 custom preset handlers
    private void Zone2SaveAsPreset_Click(object sender, RoutedEventArgs e) => SaveAsPresetForZone(2);
    private void Zone2SavePreset_Click(object sender, RoutedEventArgs e) => SavePresetForZone(2, Zone2PresetCombo);
    private void Zone2ExportPreset_Click(object sender, RoutedEventArgs e) => ExportPresetForZone(Zone2PresetCombo);
    private void Zone2ImportPreset_Click(object sender, RoutedEventArgs e) => ImportPreset();

    // Zone 3 custom preset handlers
    private void Zone3SaveAsPreset_Click(object sender, RoutedEventArgs e) => SaveAsPresetForZone(3);
    private void Zone3SavePreset_Click(object sender, RoutedEventArgs e) => SavePresetForZone(3, Zone3PresetCombo);
    private void Zone3ExportPreset_Click(object sender, RoutedEventArgs e) => ExportPresetForZone(Zone3PresetCombo);
    private void Zone3ImportPreset_Click(object sender, RoutedEventArgs e) => ImportPreset();

    #endregion

    #region Simulation-Guided Correction Event Handlers

    // Zone 0
    private void Zone0SimGuided_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.SimulationGuidedEnabled = Zone0SimGuidedCheckBox.IsChecked == true;
        Zone0SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone0_simGuidedEnabled", zone.SimulationGuidedEnabled);
    }

    private void Zone0SimGuidedAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.SimulationGuidedAlgorithm = Zone0SimGuidedStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone0_simGuidedAlgorithm", (int)zone.SimulationGuidedAlgorithm);
    }

    private void Zone0SimGuidedFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        int index = Zone0SimGuidedFilterCombo.SelectedIndex;
        // Map combo index (0-7) to filter type (1-8 for Machado, then 13-14 for Achro)
        if (index <= 5)
            zone.SimulationGuidedFilterType = index + 1; // 1-6
        else
            zone.SimulationGuidedFilterType = index + 7; // 7->13, 8->14

        _effect.Configuration.Set("zone0_simGuidedFilterType", zone.SimulationGuidedFilterType);
    }

    // Zone 1
    private void Zone1SimGuided_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.SimulationGuidedEnabled = Zone1SimGuidedCheckBox.IsChecked == true;
        Zone1SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone1_simGuidedEnabled", zone.SimulationGuidedEnabled);
    }

    private void Zone1SimGuidedAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.SimulationGuidedAlgorithm = Zone1SimGuidedStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone1_simGuidedAlgorithm", (int)zone.SimulationGuidedAlgorithm);
    }

    private void Zone1SimGuidedFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        int index = Zone1SimGuidedFilterCombo.SelectedIndex;
        if (index <= 5)
            zone.SimulationGuidedFilterType = index + 1;
        else
            zone.SimulationGuidedFilterType = index + 7;

        _effect.Configuration.Set("zone1_simGuidedFilterType", zone.SimulationGuidedFilterType);
    }

    // Zone 2
    private void Zone2SimGuided_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.SimulationGuidedEnabled = Zone2SimGuidedCheckBox.IsChecked == true;
        Zone2SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone2_simGuidedEnabled", zone.SimulationGuidedEnabled);
    }

    private void Zone2SimGuidedAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.SimulationGuidedAlgorithm = Zone2SimGuidedStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone2_simGuidedAlgorithm", (int)zone.SimulationGuidedAlgorithm);
    }

    private void Zone2SimGuidedFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        int index = Zone2SimGuidedFilterCombo.SelectedIndex;
        if (index <= 5)
            zone.SimulationGuidedFilterType = index + 1;
        else
            zone.SimulationGuidedFilterType = index + 7;

        _effect.Configuration.Set("zone2_simGuidedFilterType", zone.SimulationGuidedFilterType);
    }

    // Zone 3
    private void Zone3SimGuided_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.SimulationGuidedEnabled = Zone3SimGuidedCheckBox.IsChecked == true;
        Zone3SimGuidedPanel.Visibility = zone.SimulationGuidedEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone3_simGuidedEnabled", zone.SimulationGuidedEnabled);
    }

    private void Zone3SimGuidedAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.SimulationGuidedAlgorithm = Zone3SimGuidedStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone3_simGuidedAlgorithm", (int)zone.SimulationGuidedAlgorithm);
    }

    private void Zone3SimGuidedFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        int index = Zone3SimGuidedFilterCombo.SelectedIndex;
        if (index <= 5)
            zone.SimulationGuidedFilterType = index + 1;
        else
            zone.SimulationGuidedFilterType = index + 7;

        _effect.Configuration.Set("zone3_simGuidedFilterType", zone.SimulationGuidedFilterType);
    }

    // Sensitivity sliders
    private void Zone0SimGuidedSensitivity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.SimulationGuidedSensitivity = (float)Zone0SimGuidedSensitivitySlider.Value;
        Zone0SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";
        _effect.Configuration.Set("zone0_simGuidedSensitivity", zone.SimulationGuidedSensitivity);
    }

    private void Zone1SimGuidedSensitivity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.SimulationGuidedSensitivity = (float)Zone1SimGuidedSensitivitySlider.Value;
        Zone1SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";
        _effect.Configuration.Set("zone1_simGuidedSensitivity", zone.SimulationGuidedSensitivity);
    }

    private void Zone2SimGuidedSensitivity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.SimulationGuidedSensitivity = (float)Zone2SimGuidedSensitivitySlider.Value;
        Zone2SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";
        _effect.Configuration.Set("zone2_simGuidedSensitivity", zone.SimulationGuidedSensitivity);
    }

    private void Zone3SimGuidedSensitivity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.SimulationGuidedSensitivity = (float)Zone3SimGuidedSensitivitySlider.Value;
        Zone3SimGuidedSensitivityLabel.Text = $"Sensitivity ({zone.SimulationGuidedSensitivity:F2})";
        _effect.Configuration.Set("zone3_simGuidedSensitivity", zone.SimulationGuidedSensitivity);
    }

    #endregion

    #region Post-Correction Simulation Event Handlers

    // Zone 0
    private void Zone0PostSim_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.PostCorrectionSimEnabled = Zone0PostSimCheckBox.IsChecked == true;
        Zone0PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone0_postSimEnabled", zone.PostCorrectionSimEnabled);
    }

    private void Zone0PostSimAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.PostCorrectionSimAlgorithm = Zone0PostSimStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone0_postSimAlgorithm", (int)zone.PostCorrectionSimAlgorithm);
    }

    private void Zone0PostSimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        int index = Zone0PostSimFilterCombo.SelectedIndex;
        // Map combo index (0-7) to filter type (1-6 for base types, 13-14 for Achro)
        if (index <= 5)
            zone.PostCorrectionSimFilterType = index + 1; // 0->1, 5->6
        else
            zone.PostCorrectionSimFilterType = index + 7; // 6->13, 7->14

        _effect.Configuration.Set("zone0_postSimFilterType", zone.PostCorrectionSimFilterType);
    }

    private void Zone0PostSimIntensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.PostCorrectionSimIntensity = (float)Zone0PostSimIntensitySlider.Value;
        Zone0PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";
        _effect.Configuration.Set("zone0_postSimIntensity", zone.PostCorrectionSimIntensity);
    }

    // Zone 1
    private void Zone1PostSim_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.PostCorrectionSimEnabled = Zone1PostSimCheckBox.IsChecked == true;
        Zone1PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone1_postSimEnabled", zone.PostCorrectionSimEnabled);
    }

    private void Zone1PostSimAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.PostCorrectionSimAlgorithm = Zone1PostSimStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone1_postSimAlgorithm", (int)zone.PostCorrectionSimAlgorithm);
    }

    private void Zone1PostSimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        int index = Zone1PostSimFilterCombo.SelectedIndex;
        if (index <= 5)
            zone.PostCorrectionSimFilterType = index + 1;
        else
            zone.PostCorrectionSimFilterType = index + 7;

        _effect.Configuration.Set("zone1_postSimFilterType", zone.PostCorrectionSimFilterType);
    }

    private void Zone1PostSimIntensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(1);
        zone.PostCorrectionSimIntensity = (float)Zone1PostSimIntensitySlider.Value;
        Zone1PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";
        _effect.Configuration.Set("zone1_postSimIntensity", zone.PostCorrectionSimIntensity);
    }

    // Zone 2
    private void Zone2PostSim_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.PostCorrectionSimEnabled = Zone2PostSimCheckBox.IsChecked == true;
        Zone2PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone2_postSimEnabled", zone.PostCorrectionSimEnabled);
    }

    private void Zone2PostSimAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.PostCorrectionSimAlgorithm = Zone2PostSimStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone2_postSimAlgorithm", (int)zone.PostCorrectionSimAlgorithm);
    }

    private void Zone2PostSimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        int index = Zone2PostSimFilterCombo.SelectedIndex;
        if (index <= 5)
            zone.PostCorrectionSimFilterType = index + 1;
        else
            zone.PostCorrectionSimFilterType = index + 7;

        _effect.Configuration.Set("zone2_postSimFilterType", zone.PostCorrectionSimFilterType);
    }

    private void Zone2PostSimIntensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        zone.PostCorrectionSimIntensity = (float)Zone2PostSimIntensitySlider.Value;
        Zone2PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";
        _effect.Configuration.Set("zone2_postSimIntensity", zone.PostCorrectionSimIntensity);
    }

    // Zone 3
    private void Zone3PostSim_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.PostCorrectionSimEnabled = Zone3PostSimCheckBox.IsChecked == true;
        Zone3PostSimPanel.Visibility = zone.PostCorrectionSimEnabled ? Visibility.Visible : Visibility.Collapsed;
        _effect.Configuration.Set("zone3_postSimEnabled", zone.PostCorrectionSimEnabled);
    }

    private void Zone3PostSimAlgorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.PostCorrectionSimAlgorithm = Zone3PostSimStrictRadio.IsChecked == true
            ? SimulationAlgorithm.Strict
            : SimulationAlgorithm.Machado;
        _effect.Configuration.Set("zone3_postSimAlgorithm", (int)zone.PostCorrectionSimAlgorithm);
    }

    private void Zone3PostSimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        int index = Zone3PostSimFilterCombo.SelectedIndex;
        if (index <= 5)
            zone.PostCorrectionSimFilterType = index + 1;
        else
            zone.PostCorrectionSimFilterType = index + 7;

        _effect.Configuration.Set("zone3_postSimFilterType", zone.PostCorrectionSimFilterType);
    }

    private void Zone3PostSimIntensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        zone.PostCorrectionSimIntensity = (float)Zone3PostSimIntensitySlider.Value;
        Zone3PostSimIntensityLabel.Text = $"Simulation Intensity ({zone.PostCorrectionSimIntensity:F2})";
        _effect.Configuration.Set("zone3_postSimIntensity", zone.PostCorrectionSimIntensity);
    }

    #endregion

    #region Custom Preset Management

    // Zone 0 specific handlers (delegate to generic methods)
    private void SaveAsPresetButton_Click(object sender, RoutedEventArgs e) => SaveAsPresetForZone(0);
    private void SavePresetButton_Click(object sender, RoutedEventArgs e) => SavePresetForZone(0, Zone0PresetCombo);
    private void ExportPresetButton_Click(object sender, RoutedEventArgs e) => ExportPresetForZone(Zone0PresetCombo);
    private void ImportPresetButton_Click(object sender, RoutedEventArgs e) => ImportPreset();

    /// <summary>
    /// Apply a preset (built-in or custom) to a specific zone.
    /// </summary>
    private void ApplyPresetToZone(int zoneIndex, ComboBox presetCombo)
    {
        if (_effect == null || presetCombo.SelectedIndex < 0) return;

        // Skip if separator selected
        if (presetCombo.SelectedItem is Separator)
            return;

        int selectedIndex = presetCombo.SelectedIndex;
        CorrectionPreset? preset = null;
        string presetName = "Custom";

        if (selectedIndex < _builtInPresetCount)
        {
            // Built-in preset
            preset = CorrectionPresets.All[selectedIndex];
            presetName = preset.Name;
        }
        else if (selectedIndex > _builtInPresetCount && _presetManager.CustomPresets.Count > 0)
        {
            // Custom preset
            int customIndex = selectedIndex - _builtInPresetCount - 1;
            if (customIndex >= 0 && customIndex < _presetManager.CustomPresets.Count)
            {
                var customPreset = _presetManager.CustomPresets[customIndex];
                preset = customPreset.ToCorrectionPreset();
                presetName = customPreset.Name;
            }
        }

        if (preset == null) return;

        var zone = _effect.GetZone(zoneIndex);
        zone.ApplyPreset(preset);

        // Save preset name and zone configuration to persist the selection
        _effect.Configuration.Set($"zone{zoneIndex}_presetName", presetName);
        SaveZoneConfiguration(zoneIndex);
    }

    /// <summary>
    /// Save current zone settings as a new custom preset.
    /// </summary>
    private void SaveAsPresetForZone(int zoneIndex)
    {
        if (_effect == null) return;

        var dialog = new PresetNameDialog(_presetManager, "", true)
        {
            Owner = Window.GetWindow(this)
        };

        if (DialogHelper.WithSuspendedTopmost(() => dialog.ShowDialog()) == true && !string.IsNullOrWhiteSpace(dialog.PresetName))
        {
            var presetName = dialog.PresetName.Trim();

            // Create preset from specified zone settings
            var preset = CreatePresetFromZone(zoneIndex, presetName);
            _presetManager.SaveCustomPreset(preset);

            // Refresh all combo boxes
            _presetManager.LoadCustomPresets();
            PopulatePresetComboBoxesRefresh();

            ShowTopmostMessageBox($"Preset '{presetName}' saved successfully.", "Preset Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Update an existing custom preset with current zone settings.
    /// </summary>
    private void SavePresetForZone(int zoneIndex, ComboBox presetCombo)
    {
        if (_effect == null) return;

        int selectedIndex = presetCombo.SelectedIndex;
        if (selectedIndex <= _builtInPresetCount || _presetManager.CustomPresets.Count == 0)
            return;

        // Get the custom preset index (accounting for built-in presets and separator)
        int customIndex = selectedIndex - _builtInPresetCount - 1;
        if (customIndex < 0 || customIndex >= _presetManager.CustomPresets.Count)
            return;

        var existingPreset = _presetManager.CustomPresets[customIndex];
        var presetName = existingPreset.Name;

        // Update with current settings from specified zone
        var updatedPreset = CreatePresetFromZone(zoneIndex, presetName);
        updatedPreset.CreatedDate = existingPreset.CreatedDate;
        _presetManager.SaveCustomPreset(updatedPreset);

        ShowTopmostMessageBox($"Preset '{presetName}' updated successfully.", "Preset Updated",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Export the selected preset to a file.
    /// </summary>
    private void ExportPresetForZone(ComboBox presetCombo)
    {
        if (_effect == null || presetCombo.SelectedIndex < 0) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = GetSelectedPresetName(presetCombo)
        };

        if (DialogHelper.WithSuspendedTopmost(() => dialog.ShowDialog()) == true)
        {
            try
            {
                int selectedIndex = presetCombo.SelectedIndex;

                if (selectedIndex < _builtInPresetCount)
                {
                    // Export built-in preset
                    var preset = CorrectionPresets.All[selectedIndex];
                    _presetManager.ExportPreset(preset, dialog.FileName);
                }
                else if (selectedIndex > _builtInPresetCount)
                {
                    // Export custom preset
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

    /// <summary>
    /// Import a preset from a file.
    /// </summary>
    private void ImportPreset()
    {
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

                // Check for duplicate name
                if (_presetManager.PresetExists(finalName))
                {
                    var conflictDialog = new PresetConflictDialog(_presetManager, finalName, _presetManager.GetUniqueName(finalName))
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (DialogHelper.WithSuspendedTopmost(() => conflictDialog.ShowDialog()) != true)
                        return;

                    if (conflictDialog.Resolution == ConflictResolution.Rename)
                    {
                        finalName = conflictDialog.NewName;
                    }
                    // If Overwrite, we keep the original name
                }

                _presetManager.SaveImportedPreset(importedPreset, finalName);

                // Refresh all combo boxes
                _presetManager.LoadCustomPresets();
                PopulatePresetComboBoxesRefresh();

                ShowTopmostMessageBox($"Preset '{finalName}' imported successfully.", "Import Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowTopmostMessageBox($"Failed to import preset: {ex.Message}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Create a CustomPreset from a specific zone's settings.
    /// </summary>
    private CustomPreset CreatePresetFromZone(int zoneIndex, string name)
    {
        var editor = zoneIndex switch
        {
            0 => Zone0CorrectionEditor,
            1 => Zone1CorrectionEditor,
            2 => Zone2CorrectionEditor,
            3 => Zone3CorrectionEditor,
            _ => Zone0CorrectionEditor
        };

        return editor.GetAsPreset(name);
    }

    private string GetSelectedPresetName(ComboBox presetCombo)
    {
        if (presetCombo.SelectedItem is string name)
        {
            return name.StartsWith("* ") ? name[2..] : name;
        }
        return "preset";
    }

    private void PopulatePresetComboBoxesRefresh()
    {
        var builtInPresets = CorrectionPresets.All;
        _builtInPresetCount = builtInPresets.Count;

        var presetItems = BuildPresetItemsList();

        Zone0PresetCombo.ItemsSource = presetItems;
        Zone1PresetCombo.ItemsSource = BuildPresetItemsList();
        Zone2PresetCombo.ItemsSource = BuildPresetItemsList();
        Zone3PresetCombo.ItemsSource = BuildPresetItemsList();

        if (presetItems.Count > 0)
        {
            Zone0PresetCombo.SelectedIndex = 0;
            Zone1PresetCombo.SelectedIndex = 0;
            Zone2PresetCombo.SelectedIndex = 0;
            Zone3PresetCombo.SelectedIndex = 0;
        }

        UpdateAllSaveButtonStates();
    }

    /// <summary>
    /// Restores the saved preset selection for all zones after combo boxes are populated.
    /// Falls back to Passthrough if a saved preset is not found.
    /// </summary>
    private void RestoreSavedPresetSelections()
    {
        if (_effect == null) return;

        RestorePresetSelectionForZone(0, Zone0PresetCombo);
        RestorePresetSelectionForZone(1, Zone1PresetCombo);
        RestorePresetSelectionForZone(2, Zone2PresetCombo);
        RestorePresetSelectionForZone(3, Zone3PresetCombo);
    }

    /// <summary>
    /// Restores the saved preset selection for a specific zone and applies the preset values.
    /// </summary>
    private void RestorePresetSelectionForZone(int zoneIndex, ComboBox presetCombo)
    {
        if (_effect == null) return;

        string savedPresetName = _effect.Configuration.Get($"zone{zoneIndex}_presetName", "");
        if (string.IsNullOrEmpty(savedPresetName))
        {
            // No saved preset, default to Custom (index 0)
            presetCombo.SelectedIndex = 0;
            return;
        }

        // Try to find the preset in the combo box
        int foundIndex = FindPresetIndexByName(presetCombo, savedPresetName);

        if (foundIndex >= 0)
        {
            // Found the preset, select it and apply the preset values
            presetCombo.SelectedIndex = foundIndex;
            ApplyPresetToZoneByIndex(zoneIndex, presetCombo, foundIndex);
        }
        else
        {
            // Preset not found - show error and fall back to Passthrough
            ShowTopmostMessageBox(
                $"Preset \"{savedPresetName}\" not found. Falling back to \"Passthrough\".",
                "Preset Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Find and select Passthrough (should be at index 1)
            int passthroughIndex = FindPresetIndexByName(presetCombo, "Passthrough");
            presetCombo.SelectedIndex = passthroughIndex >= 0 ? passthroughIndex : 0;
            if (passthroughIndex >= 0)
            {
                ApplyPresetToZoneByIndex(zoneIndex, presetCombo, passthroughIndex);
            }

            // Clear the invalid saved preset name
            _effect.Configuration.Set($"zone{zoneIndex}_presetName", "Passthrough");
        }
    }

    /// <summary>
    /// Applies a preset to a zone by combo box index (without saving config).
    /// Used during startup to apply saved preset selections.
    /// </summary>
    private void ApplyPresetToZoneByIndex(int zoneIndex, ComboBox presetCombo, int selectedIndex)
    {
        if (_effect == null || selectedIndex < 0) return;

        // Skip if separator selected
        if (presetCombo.Items[selectedIndex] is Separator)
            return;

        CorrectionPreset? preset = null;

        if (selectedIndex < _builtInPresetCount)
        {
            // Built-in preset
            preset = CorrectionPresets.All[selectedIndex];
        }
        else if (selectedIndex > _builtInPresetCount && _presetManager.CustomPresets.Count > 0)
        {
            // Custom preset
            int customIndex = selectedIndex - _builtInPresetCount - 1;
            if (customIndex >= 0 && customIndex < _presetManager.CustomPresets.Count)
            {
                var customPreset = _presetManager.CustomPresets[customIndex];
                preset = customPreset.ToCorrectionPreset();
            }
        }

        if (preset == null) return;

        // Apply preset to zone
        var zone = _effect.GetZone(zoneIndex);
        zone.ApplyPreset(preset);

        // Refresh the correction editor UI to show the applied values
        var editor = zoneIndex switch
        {
            0 => Zone0CorrectionEditor,
            1 => Zone1CorrectionEditor,
            2 => Zone2CorrectionEditor,
            3 => Zone3CorrectionEditor,
            _ => null
        };
        editor?.LoadFromZone();
    }

    /// <summary>
    /// Finds the index of a preset by name in the combo box.
    /// </summary>
    private int FindPresetIndexByName(ComboBox presetCombo, string presetName)
    {
        if (presetCombo.ItemsSource == null) return -1;

        var items = presetCombo.ItemsSource.Cast<object>().ToList();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is Separator) continue;

            if (items[i] is string itemName)
            {
                // Check for exact match (built-in) or custom preset match (prefixed with "* ")
                if (itemName == presetName || itemName == $"* {presetName}")
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Shows a MessageBox that appears above the topmost overlay.
    /// </summary>
    private static void ShowTopmostMessageBox(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        DialogHelper.WithSuspendedTopmost(() =>
        {
            System.Windows.MessageBox.Show(message, caption, button, icon);
        });
    }

    #endregion
}
