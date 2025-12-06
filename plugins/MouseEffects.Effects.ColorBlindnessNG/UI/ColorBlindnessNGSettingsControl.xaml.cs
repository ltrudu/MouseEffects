using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;
using WpfColor = System.Windows.Media.Color;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

public partial class ColorBlindnessNGSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private readonly ColorBlindnessNGEffect? _ngEffect;
    private bool _isInitializing = true;
    private bool _isExpanded;

    public event Action<string>? SettingsChanged;

    // Current color values
    private Vector3 _redStartColor = new(1, 0, 0);
    private Vector3 _redEndColor = new(0, 1, 1);
    private Vector3 _greenStartColor = new(0, 1, 0);
    private Vector3 _greenEndColor = new(0, 1, 1);
    private Vector3 _blueStartColor = new(0, 0, 1);
    private Vector3 _blueEndColor = new(1, 1, 0);

    public ColorBlindnessNGSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        _ngEffect = effect as ColorBlindnessNGEffect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        // Mode
        if (_effect.Configuration.TryGet("mode", out int mode))
            ModeCombo.SelectedIndex = mode;

        // Simulation settings
        if (_effect.Configuration.TryGet("simulationAlgorithm", out int algorithm))
        {
            MachadoRadio.IsChecked = algorithm == 0;
            StrictRadio.IsChecked = algorithm == 1;
        }

        if (_effect.Configuration.TryGet("simulationFilterType", out int filterType))
            SimulationFilterCombo.SelectedIndex = filterType;

        // Correction settings
        if (_effect.Configuration.TryGet("applicationMode", out int appMode))
            ApplicationModeCombo.SelectedIndex = appMode;

        if (_effect.Configuration.TryGet("gradientType", out int gradType))
            GradientTypeCombo.SelectedIndex = gradType;

        if (_effect.Configuration.TryGet("threshold", out float threshold))
        {
            ThresholdSlider.Value = threshold;
            ThresholdValue.Text = threshold.ToString("F2");
        }

        // Red channel
        if (_effect.Configuration.TryGet("redEnabled", out bool redEnabled))
            RedEnabledCheckBox.IsChecked = redEnabled;
        if (_effect.Configuration.TryGet("redStrength", out float redStrength))
        {
            RedStrengthSlider.Value = redStrength;
            RedStrengthValue.Text = redStrength.ToString("F2");
        }
        if (_effect.Configuration.TryGet("redStartColor", out string? redStart) && redStart != null)
        {
            _redStartColor = ParseHexColor(redStart);
            UpdateColorBorder(RedStartColorBorder, _redStartColor);
        }
        if (_effect.Configuration.TryGet("redEndColor", out string? redEnd) && redEnd != null)
        {
            _redEndColor = ParseHexColor(redEnd);
            UpdateColorBorder(RedEndColorBorder, _redEndColor);
        }

        // Green channel
        if (_effect.Configuration.TryGet("greenEnabled", out bool greenEnabled))
            GreenEnabledCheckBox.IsChecked = greenEnabled;
        if (_effect.Configuration.TryGet("greenStrength", out float greenStrength))
        {
            GreenStrengthSlider.Value = greenStrength;
            GreenStrengthValue.Text = greenStrength.ToString("F2");
        }
        if (_effect.Configuration.TryGet("greenStartColor", out string? greenStart) && greenStart != null)
        {
            _greenStartColor = ParseHexColor(greenStart);
            UpdateColorBorder(GreenStartColorBorder, _greenStartColor);
        }
        if (_effect.Configuration.TryGet("greenEndColor", out string? greenEnd) && greenEnd != null)
        {
            _greenEndColor = ParseHexColor(greenEnd);
            UpdateColorBorder(GreenEndColorBorder, _greenEndColor);
        }

        // Blue channel
        if (_effect.Configuration.TryGet("blueEnabled", out bool blueEnabled))
            BlueEnabledCheckBox.IsChecked = blueEnabled;
        if (_effect.Configuration.TryGet("blueStrength", out float blueStrength))
        {
            BlueStrengthSlider.Value = blueStrength;
            BlueStrengthValue.Text = blueStrength.ToString("F2");
        }
        if (_effect.Configuration.TryGet("blueStartColor", out string? blueStart) && blueStart != null)
        {
            _blueStartColor = ParseHexColor(blueStart);
            UpdateColorBorder(BlueStartColorBorder, _blueStartColor);
        }
        if (_effect.Configuration.TryGet("blueEndColor", out string? blueEnd) && blueEnd != null)
        {
            _blueEndColor = ParseHexColor(blueEnd);
            UpdateColorBorder(BlueEndColorBorder, _blueEndColor);
        }

        // Global intensity
        if (_effect.Configuration.TryGet("intensity", out float intensity))
        {
            IntensitySlider.Value = intensity;
            IntensityValue.Text = intensity.ToString("F2");
        }

        // Update UI visibility based on mode
        UpdateModeUI(ModeCombo.SelectedIndex);
        UpdateChannelPanels();
        UpdateThresholdVisibility();
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();

        // Mode
        config.Set("mode", ModeCombo.SelectedIndex);

        // Simulation settings
        config.Set("simulationAlgorithm", StrictRadio.IsChecked == true ? 1 : 0);
        config.Set("simulationFilterType", SimulationFilterCombo.SelectedIndex);

        // Correction settings
        config.Set("applicationMode", ApplicationModeCombo.SelectedIndex);
        config.Set("gradientType", GradientTypeCombo.SelectedIndex);
        config.Set("threshold", (float)ThresholdSlider.Value);

        // Red channel
        config.Set("redEnabled", RedEnabledCheckBox.IsChecked ?? false);
        config.Set("redStrength", (float)RedStrengthSlider.Value);
        config.Set("redStartColor", ToHexColor(_redStartColor));
        config.Set("redEndColor", ToHexColor(_redEndColor));

        // Green channel
        config.Set("greenEnabled", GreenEnabledCheckBox.IsChecked ?? false);
        config.Set("greenStrength", (float)GreenStrengthSlider.Value);
        config.Set("greenStartColor", ToHexColor(_greenStartColor));
        config.Set("greenEndColor", ToHexColor(_greenEndColor));

        // Blue channel
        config.Set("blueEnabled", BlueEnabledCheckBox.IsChecked ?? false);
        config.Set("blueStrength", (float)BlueStrengthSlider.Value);
        config.Set("blueStartColor", ToHexColor(_blueStartColor));
        config.Set("blueEndColor", ToHexColor(_blueEndColor));

        // Global
        config.Set("intensity", (float)IntensitySlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateModeUI(int mode)
    {
        SimulationPanel.Visibility = mode == 0 ? Visibility.Visible : Visibility.Collapsed;
        CorrectionPanel.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateChannelPanels()
    {
        RedChannelPanel.Visibility = RedEnabledCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        GreenChannelPanel.Visibility = GreenEnabledCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        BlueChannelPanel.Visibility = BlueEnabledCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateThresholdVisibility()
    {
        ThresholdPanel.Visibility = ApplicationModeCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateColorBorder(Border border, Vector3 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        border.Background = new SolidColorBrush(WpfColor.FromRgb(r, g, b));
    }

    private static Vector3 ParseHexColor(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex[1..];

        if (hex.Length != 6)
            return new Vector3(1, 1, 1);

        try
        {
            int r = Convert.ToInt32(hex[..2], 16);
            int g = Convert.ToInt32(hex[2..4], 16);
            int b = Convert.ToInt32(hex[4..6], 16);
            return new Vector3(r / 255f, g / 255f, b / 255f);
        }
        catch
        {
            return new Vector3(1, 1, 1);
        }
    }

    private static string ToHexColor(Vector3 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private void ShowColorPicker(ref Vector3 currentColor, Border colorBorder)
    {
        // Use Windows Forms color dialog
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(currentColor.X * 255),
                (int)(currentColor.Y * 255),
                (int)(currentColor.Z * 255)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            currentColor = new Vector3(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f);

            UpdateColorBorder(colorBorder, currentColor);
            UpdateConfiguration();
        }
    }

    private void ApplyPreset(CorrectionPreset preset)
    {
        _isInitializing = true;

        // Red channel
        RedEnabledCheckBox.IsChecked = preset.RedEnabled;
        RedStrengthSlider.Value = preset.RedStrength;
        RedStrengthValue.Text = preset.RedStrength.ToString("F2");
        _redStartColor = preset.RedStartColor;
        _redEndColor = preset.RedEndColor;
        UpdateColorBorder(RedStartColorBorder, _redStartColor);
        UpdateColorBorder(RedEndColorBorder, _redEndColor);

        // Green channel
        GreenEnabledCheckBox.IsChecked = preset.GreenEnabled;
        GreenStrengthSlider.Value = preset.GreenStrength;
        GreenStrengthValue.Text = preset.GreenStrength.ToString("F2");
        _greenStartColor = preset.GreenStartColor;
        _greenEndColor = preset.GreenEndColor;
        UpdateColorBorder(GreenStartColorBorder, _greenStartColor);
        UpdateColorBorder(GreenEndColorBorder, _greenEndColor);

        // Blue channel
        BlueEnabledCheckBox.IsChecked = preset.BlueEnabled;
        BlueStrengthSlider.Value = preset.BlueStrength;
        BlueStrengthValue.Text = preset.BlueStrength.ToString("F2");
        _blueStartColor = preset.BlueStartColor;
        _blueEndColor = preset.BlueEndColor;
        UpdateColorBorder(BlueStartColorBorder, _blueStartColor);
        UpdateColorBorder(BlueEndColorBorder, _blueEndColor);

        // Recommended settings
        GradientTypeCombo.SelectedIndex = (int)preset.RecommendedGradientType;
        ApplicationModeCombo.SelectedIndex = (int)preset.RecommendedApplicationMode;
        IntensitySlider.Value = preset.DefaultIntensity;
        IntensityValue.Text = preset.DefaultIntensity.ToString("F2");

        UpdateChannelPanels();
        UpdateThresholdVisibility();

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
        FoldButton.Content = _isExpanded ? "\u25B2" : "\u25BC";
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateModeUI(ModeCombo.SelectedIndex);
        UpdateConfiguration();
    }

    private void Algorithm_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateConfiguration();
    }

    private void SimulationFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ApplicationModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateThresholdVisibility();
        UpdateConfiguration();
    }

    private void GradientTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThresholdValue != null) ThresholdValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ApplyPresetButton_Click(object sender, RoutedEventArgs e)
    {
        var preset = CorrectionPresets.GetByIndex(PresetCombo.SelectedIndex);
        ApplyPreset(preset);
    }

    private void ChannelEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateChannelPanels();
        UpdateConfiguration();
    }

    private void RedStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RedStrengthValue != null) RedStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void GreenStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GreenStrengthValue != null) GreenStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void BlueStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BlueStrengthValue != null) BlueStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntensityValue != null) IntensityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void RedStartColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowColorPicker(ref _redStartColor, RedStartColorBorder);
    }

    private void RedEndColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowColorPicker(ref _redEndColor, RedEndColorBorder);
    }

    private void GreenStartColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowColorPicker(ref _greenStartColor, GreenStartColorBorder);
    }

    private void GreenEndColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowColorPicker(ref _greenEndColor, GreenEndColorBorder);
    }

    private void BlueStartColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowColorPicker(ref _blueStartColor, BlueStartColorBorder);
    }

    private void BlueEndColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowColorPicker(ref _blueEndColor, BlueEndColorBorder);
    }

    #endregion
}
