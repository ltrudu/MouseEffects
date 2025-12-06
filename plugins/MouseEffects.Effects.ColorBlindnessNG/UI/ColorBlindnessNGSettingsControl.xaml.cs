using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        }
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

            // Load zone 0 settings
            LoadZone0Settings();

            // Load zone 1 settings
            LoadZone1Settings();

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

        Zone0AppModeCombo.SelectedIndex = (int)zone.ApplicationMode;
        Zone0ThresholdSlider.Value = zone.Threshold;
        Zone0GradientCombo.SelectedIndex = (int)zone.GradientType;
        Zone0IntensitySlider.Value = zone.Intensity;

        // Channel settings
        Zone0RedEnabled.IsChecked = zone.RedChannel.Enabled;
        Zone0RedStrength.Value = zone.RedChannel.Strength;
        Zone0RedWhiteProt.Value = zone.RedChannel.WhiteProtection;
        UpdateColorBorder(Zone0RedStart, zone.RedChannel.StartColor);
        UpdateColorBorder(Zone0RedEnd, zone.RedChannel.EndColor);

        Zone0GreenEnabled.IsChecked = zone.GreenChannel.Enabled;
        Zone0GreenStrength.Value = zone.GreenChannel.Strength;
        Zone0GreenWhiteProt.Value = zone.GreenChannel.WhiteProtection;
        UpdateColorBorder(Zone0GreenStart, zone.GreenChannel.StartColor);
        UpdateColorBorder(Zone0GreenEnd, zone.GreenChannel.EndColor);

        Zone0BlueEnabled.IsChecked = zone.BlueChannel.Enabled;
        Zone0BlueStrength.Value = zone.BlueChannel.Strength;
        Zone0BlueWhiteProt.Value = zone.BlueChannel.WhiteProtection;
        UpdateColorBorder(Zone0BlueStart, zone.BlueChannel.StartColor);
        UpdateColorBorder(Zone0BlueEnd, zone.BlueChannel.EndColor);
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

        Zone1IntensitySlider.Value = zone.Intensity;
    }

    private void UpdateColorBorder(Border border, Vector3 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        border.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
    }

    private Vector3 GetColorFromBorder(Border border)
    {
        if (border.Background is SolidColorBrush brush)
        {
            return new Vector3(brush.Color.R / 255f, brush.Color.G / 255f, brush.Color.B / 255f);
        }
        return new Vector3(1, 1, 1);
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
        if (SplitPositionPanel == null) return;

        bool isSplit = splitMode > 0;
        bool isQuadrant = splitMode == 3;

        SplitPositionPanel.Visibility = isSplit ? Visibility.Visible : Visibility.Collapsed;
        SplitPositionVPanel.Visibility = isQuadrant ? Visibility.Visible : Visibility.Collapsed;
        ComparisonModeCheckBox.Visibility = isSplit ? Visibility.Visible : Visibility.Collapsed;

        // Update zone visibility
        Zone1Expander.Visibility = isSplit ? Visibility.Visible : Visibility.Collapsed;
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

        // Update channel panels
        Zone0RedPanel.Visibility = Zone0RedEnabled.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        Zone0GreenPanel.Visibility = Zone0GreenEnabled.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        Zone0BluePanel.Visibility = Zone0BlueEnabled.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        // Update threshold panel
        Zone0ThresholdPanel.Visibility = Zone0AppModeCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
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

    private void Zone0AppMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.ApplicationMode = (ApplicationMode)Zone0AppModeCombo.SelectedIndex;
        _effect.Configuration.Set("zone0_appMode", Zone0AppModeCombo.SelectedIndex);
        Zone0ThresholdPanel.Visibility = Zone0AppModeCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Zone0Threshold_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.Threshold = (float)Zone0ThresholdSlider.Value;
        _effect.Configuration.Set("zone0_threshold", zone.Threshold);
        Zone0ThresholdValue.Text = zone.Threshold.ToString("F2");
    }

    private void Zone0Gradient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.GradientType = (GradientType)Zone0GradientCombo.SelectedIndex;
        zone.LutsNeedUpdate = true;
        _effect.Configuration.Set("zone0_gradientType", Zone0GradientCombo.SelectedIndex);
    }

    private void Zone0Intensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);
        zone.Intensity = (float)Zone0IntensitySlider.Value;
        _effect.Configuration.Set("zone0_intensity", zone.Intensity);
        Zone0IntensityValue.Text = zone.Intensity.ToString("F2");
    }

    private void Zone0Channel_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(0);

        zone.RedChannel.Enabled = Zone0RedEnabled.IsChecked == true;
        zone.RedChannel.Strength = (float)Zone0RedStrength.Value;
        zone.RedChannel.WhiteProtection = (float)Zone0RedWhiteProt.Value;

        zone.GreenChannel.Enabled = Zone0GreenEnabled.IsChecked == true;
        zone.GreenChannel.Strength = (float)Zone0GreenStrength.Value;
        zone.GreenChannel.WhiteProtection = (float)Zone0GreenWhiteProt.Value;

        zone.BlueChannel.Enabled = Zone0BlueEnabled.IsChecked == true;
        zone.BlueChannel.Strength = (float)Zone0BlueStrength.Value;
        zone.BlueChannel.WhiteProtection = (float)Zone0BlueWhiteProt.Value;

        // Update visibility
        Zone0RedPanel.Visibility = zone.RedChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;
        Zone0GreenPanel.Visibility = zone.GreenChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;
        Zone0BluePanel.Visibility = zone.BlueChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;

        SaveZone0ChannelSettings();
    }

    private void SaveZone0ChannelSettings()
    {
        if (_effect == null) return;
        var zone = _effect.GetZone(0);

        _effect.Configuration.Set("zone0_redEnabled", zone.RedChannel.Enabled);
        _effect.Configuration.Set("zone0_redStrength", zone.RedChannel.Strength);
        _effect.Configuration.Set("zone0_redWhiteProtection", zone.RedChannel.WhiteProtection);
        _effect.Configuration.Set("zone0_redStartColor", ToHexColor(zone.RedChannel.StartColor));
        _effect.Configuration.Set("zone0_redEndColor", ToHexColor(zone.RedChannel.EndColor));

        _effect.Configuration.Set("zone0_greenEnabled", zone.GreenChannel.Enabled);
        _effect.Configuration.Set("zone0_greenStrength", zone.GreenChannel.Strength);
        _effect.Configuration.Set("zone0_greenWhiteProtection", zone.GreenChannel.WhiteProtection);
        _effect.Configuration.Set("zone0_greenStartColor", ToHexColor(zone.GreenChannel.StartColor));
        _effect.Configuration.Set("zone0_greenEndColor", ToHexColor(zone.GreenChannel.EndColor));

        _effect.Configuration.Set("zone0_blueEnabled", zone.BlueChannel.Enabled);
        _effect.Configuration.Set("zone0_blueStrength", zone.BlueChannel.Strength);
        _effect.Configuration.Set("zone0_blueWhiteProtection", zone.BlueChannel.WhiteProtection);
        _effect.Configuration.Set("zone0_blueStartColor", ToHexColor(zone.BlueChannel.StartColor));
        _effect.Configuration.Set("zone0_blueEndColor", ToHexColor(zone.BlueChannel.EndColor));
    }

    private void Zone0ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(0, Zone0PresetCombo);

        _isLoading = true;
        LoadZone0Settings();
        _isLoading = false;

        UpdateZone0ModeUI();
        SaveZone0ChannelSettings();
    }

    private void Zone0RedStart_Click(object sender, MouseButtonEventArgs e) => PickColorForBorder(Zone0RedStart, 0, "redStart");
    private void Zone0RedEnd_Click(object sender, MouseButtonEventArgs e) => PickColorForBorder(Zone0RedEnd, 0, "redEnd");
    private void Zone0GreenStart_Click(object sender, MouseButtonEventArgs e) => PickColorForBorder(Zone0GreenStart, 0, "greenStart");
    private void Zone0GreenEnd_Click(object sender, MouseButtonEventArgs e) => PickColorForBorder(Zone0GreenEnd, 0, "greenEnd");
    private void Zone0BlueStart_Click(object sender, MouseButtonEventArgs e) => PickColorForBorder(Zone0BlueStart, 0, "blueStart");
    private void Zone0BlueEnd_Click(object sender, MouseButtonEventArgs e) => PickColorForBorder(Zone0BlueEnd, 0, "blueEnd");

    private void PickColorForBorder(Border border, int zoneIndex, string colorType)
    {
        if (_effect == null) return;

        var currentColor = GetColorFromBorder(border);
        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(currentColor.X * 255),
                (int)(currentColor.Y * 255),
                (int)(currentColor.Z * 255))
        };

        // Show dialog in topmost form
        using var form = new System.Windows.Forms.Form { TopMost = true };
        if (dialog.ShowDialog(form) == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector3(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f);

            UpdateColorBorder(border, newColor);

            var zone = _effect.GetZone(zoneIndex);
            switch (colorType)
            {
                case "redStart": zone.RedChannel.StartColor = newColor; break;
                case "redEnd": zone.RedChannel.EndColor = newColor; break;
                case "greenStart": zone.GreenChannel.StartColor = newColor; break;
                case "greenEnd": zone.GreenChannel.EndColor = newColor; break;
                case "blueStart": zone.BlueChannel.StartColor = newColor; break;
                case "blueEnd": zone.BlueChannel.EndColor = newColor; break;
            }

            zone.LutsNeedUpdate = true;
            SaveZone0ChannelSettings();
        }
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
    }

    private void Zone1ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(1, Zone1PresetCombo);
        _effect?.Configuration.Set("zone1_mode", (int)ZoneMode.Correction);
        Zone1ModeCombo.SelectedIndex = 2;
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

    private void Zone2SimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(2);
        // Simplified filter list: 0=None, 1=Protan, 2=Deutan, 3=Tritan
        int index = Zone2SimFilterCombo.SelectedIndex;
        zone.SimulationFilterType = index == 0 ? 0 : (index * 2 - 1); // 0, 1, 3, 5
        _effect.Configuration.Set("zone2_simFilterType", zone.SimulationFilterType);
    }

    private void Zone2ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(2, Zone2PresetCombo);
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

    private void Zone3SimFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var zone = _effect.GetZone(3);
        int index = Zone3SimFilterCombo.SelectedIndex;
        zone.SimulationFilterType = index == 0 ? 0 : (index * 2 - 1);
        _effect.Configuration.Set("zone3_simFilterType", zone.SimulationFilterType);
    }

    private void Zone3ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        ApplyPresetToZone(3, Zone3PresetCombo);
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
                preset = _presetManager.CustomPresets[customIndex].ToCorrectionPreset();
            }
        }

        if (preset == null) return;

        var zone = _effect.GetZone(zoneIndex);
        zone.ApplyPreset(preset);
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
        var zone = _effect!.GetZone(zoneIndex);

        return new CustomPreset
        {
            Name = name,
            Description = $"Custom preset created on {DateTime.Now:yyyy-MM-dd}",
            IsCustom = true,
            CreatedDate = DateTime.UtcNow,

            RedEnabled = zone.RedChannel.Enabled,
            RedStrength = zone.RedChannel.Strength,
            RedStartColor = CustomPreset.ToHexColor(zone.RedChannel.StartColor),
            RedEndColor = CustomPreset.ToHexColor(zone.RedChannel.EndColor),
            RedWhiteProtection = zone.RedChannel.WhiteProtection,

            GreenEnabled = zone.GreenChannel.Enabled,
            GreenStrength = zone.GreenChannel.Strength,
            GreenStartColor = CustomPreset.ToHexColor(zone.GreenChannel.StartColor),
            GreenEndColor = CustomPreset.ToHexColor(zone.GreenChannel.EndColor),
            GreenWhiteProtection = zone.GreenChannel.WhiteProtection,

            BlueEnabled = zone.BlueChannel.Enabled,
            BlueStrength = zone.BlueChannel.Strength,
            BlueStartColor = CustomPreset.ToHexColor(zone.BlueChannel.StartColor),
            BlueEndColor = CustomPreset.ToHexColor(zone.BlueChannel.EndColor),
            BlueWhiteProtection = zone.BlueChannel.WhiteProtection,

            DefaultIntensity = zone.Intensity,
            RecommendedGradientType = (int)zone.GradientType,
            RecommendedApplicationMode = (int)zone.ApplicationMode
        };
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

    #region Helpers

    private static string ToHexColor(Vector3 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    #endregion
}
