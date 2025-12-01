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

        // Load curves
        if (_colorBlindnessEffect != null)
        {
            CurveEditorControl.MasterCurve = _colorBlindnessEffect.MasterCurve;
            CurveEditorControl.RedCurve = _colorBlindnessEffect.RedCurve;
            CurveEditorControl.GreenCurve = _colorBlindnessEffect.GreenCurve;
            CurveEditorControl.BlueCurve = _colorBlindnessEffect.BlueCurve;
        }
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
        int oldShapeMode = newShapeMode == 2 ? 0 : 2; // Guess previous mode for sync

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

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
