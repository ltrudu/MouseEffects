using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Settings control for ASCII Art Classic filter.
/// Uses the same pattern as ColorBlindnessNG: directly modify effect properties
/// for real-time shader updates, and also update Configuration for JSON persistence.
/// </summary>
public partial class ASCIIClassicSettings : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;

    public ASCIIClassicSettings()
    {
        InitializeComponent();
    }

    public void Initialize(ASCIIZerEffect effect)
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
            // Advanced mode
            AdvancedModeRadio.IsChecked = _effect.AdvancedMode;
            BasicModeRadio.IsChecked = !_effect.AdvancedMode;
            UpdateSettingsPanelVisibility(_effect.AdvancedMode);

            // Layout
            LayoutModeCombo.SelectedIndex = _effect.LayoutMode;
            UpdateLayoutVisibility(_effect.LayoutMode);

            RadiusSlider.Value = _effect.Radius;
            RadiusValue.Text = $"{_effect.Radius:F0} px";

            RectWidthSlider.Value = _effect.RectWidth;
            RectWidthValue.Text = $"{_effect.RectWidth:F0} px";

            RectHeightSlider.Value = _effect.RectHeight;
            RectHeightValue.Text = $"{_effect.RectHeight:F0} px";

            // Cell size
            CellWidthSlider.Value = _effect.CellWidth;
            CellWidthValue.Text = $"{_effect.CellWidth:F0} px";

            CellHeightSlider.Value = _effect.CellHeight;
            CellHeightValue.Text = $"{_effect.CellHeight:F0} px";

            // Character set
            CharsetCombo.SelectedIndex = _effect.CharsetPreset;
            CustomCharsetPanel.Visibility = _effect.CharsetPreset == 4 ? Visibility.Visible : Visibility.Collapsed;
            CustomCharsetTextBox.Text = _effect.CustomCharset;

            // Color mode
            ColorModeCombo.SelectedIndex = _effect.ColorMode;
            MonochromeColorsPanel.Visibility = _effect.ColorMode > 0 ? Visibility.Visible : Visibility.Collapsed;

            ForegroundColorTextBox.Text = Vector4ToHex(_effect.Foreground);
            ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.Foreground));

            BackgroundColorTextBox.Text = Vector4ToHex(_effect.Background);
            BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.Background));

            // Advanced: Font
            FontFamilyCombo.SelectedIndex = _effect.FontFamily;
            FontWeightCombo.SelectedIndex = _effect.FontWeight;

            // Advanced: Brightness
            BrightnessSlider.Value = _effect.Brightness;
            BrightnessValue.Text = $"{_effect.Brightness:F2}";

            ContrastSlider.Value = _effect.Contrast;
            ContrastValue.Text = $"{_effect.Contrast:F2}";

            GammaSlider.Value = _effect.Gamma;
            GammaValue.Text = $"{_effect.Gamma:F2}";

            InvertCheckBox.IsChecked = _effect.Invert;

            // Advanced: Color
            SaturationSlider.Value = _effect.Saturation;
            SaturationValue.Text = $"{_effect.Saturation * 100:F0}%";

            QuantizeLevelsSlider.Value = _effect.QuantizeLevels;
            QuantizeLevelsValue.Text = $"{_effect.QuantizeLevels}";

            PreserveLuminanceCheckBox.IsChecked = _effect.PreserveLuminance;

            // Advanced: Character Rendering
            AntialiasingCombo.SelectedIndex = _effect.Antialiasing;
            CharShadowCheckBox.IsChecked = _effect.CharShadow;
            GridLinesCheckBox.IsChecked = _effect.GridLines;

            // Advanced: Edge
            EdgeSoftnessSlider.Value = _effect.EdgeSoftness;
            EdgeSoftnessValue.Text = $"{_effect.EdgeSoftness:F0} px";

            InnerGlowCheckBox.IsChecked = _effect.InnerGlow;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateSettingsPanelVisibility(bool advanced)
    {
        BasicSettingsPanel.Visibility = Visibility.Visible; // Always show basic
        AdvancedSettingsPanel.Visibility = advanced ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateLayoutVisibility(int layoutMode)
    {
        CircleSettingsPanel.Visibility = layoutMode == 1 ? Visibility.Visible : Visibility.Collapsed;
        RectangleSettingsPanel.Visibility = layoutMode == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    #region Helper Methods

    private static string Vector4ToHex(Vector4 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static Color Vector4ToColor(Vector4 color)
    {
        return Color.FromArgb(
            (byte)(color.W * 255),
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255));
    }

    private static Vector4 HexToVector4(string hex)
    {
        try
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Vector4(r / 255f, g / 255f, b / 255f, 1f);
            }
        }
        catch { }
        return new Vector4(1, 1, 1, 1);
    }

    #endregion

    #region Event Handlers

    private void ModeRadio_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool advanced = AdvancedModeRadio.IsChecked == true;
        _effect.AdvancedMode = advanced;
        _effect.Configuration.Set("advancedMode", advanced);
        UpdateSettingsPanelVisibility(advanced);
    }

    private void LayoutModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.LayoutMode = layoutMode;
        _effect.Configuration.Set("layoutMode", layoutMode);
        UpdateLayoutVisibility(layoutMode);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.Radius = (float)e.NewValue;
        _effect.Configuration.Set("radius", (float)e.NewValue);
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.RectWidth = (float)e.NewValue;
        _effect.Configuration.Set("rectWidth", (float)e.NewValue);
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.RectHeight = (float)e.NewValue;
        _effect.Configuration.Set("rectHeight", (float)e.NewValue);
    }

    private void CellWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellWidthValue != null)
            CellWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.CellWidth = (float)e.NewValue;
        _effect.Configuration.Set("cellWidth", (float)e.NewValue);
    }

    private void CellHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellHeightValue != null)
            CellHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.CellHeight = (float)e.NewValue;
        _effect.Configuration.Set("cellHeight", (float)e.NewValue);
    }

    private void CharsetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int preset = CharsetCombo.SelectedIndex;
        _effect.CharsetPreset = preset;
        _effect.Configuration.Set("charsetPreset", preset);
        CustomCharsetPanel.Visibility = preset == 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CustomCharsetTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        string charset = CustomCharsetTextBox.Text ?? "";
        _effect.CustomCharset = charset;
        _effect.Configuration.Set("customCharset", charset);
    }

    private void ColorModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int colorMode = ColorModeCombo.SelectedIndex;
        _effect.ColorMode = colorMode;
        _effect.Configuration.Set("colorMode", colorMode);
        MonochromeColorsPanel.Visibility = colorMode > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ForegroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || ForegroundColorPreview == null) return;

        var color = HexToVector4(ForegroundColorTextBox.Text);
        ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.Foreground = color;
        _effect.Configuration.Set("foreground", color);
    }

    private void BackgroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || BackgroundColorPreview == null) return;

        var color = HexToVector4(BackgroundColorTextBox.Text);
        BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.Background = color;
        _effect.Configuration.Set("background", color);
    }

    private void FontFamilyCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int fontFamily = FontFamilyCombo.SelectedIndex;
        _effect.FontFamily = fontFamily;
        _effect.Configuration.Set("fontFamily", fontFamily);
    }

    private void FontWeightCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int fontWeight = FontWeightCombo.SelectedIndex;
        _effect.FontWeight = fontWeight;
        _effect.Configuration.Set("fontWeight", fontWeight);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessValue != null)
            BrightnessValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.Brightness = (float)e.NewValue;
        _effect.Configuration.Set("brightness", (float)e.NewValue);
    }

    private void ContrastSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ContrastValue != null)
            ContrastValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.Contrast = (float)e.NewValue;
        _effect.Configuration.Set("contrast", (float)e.NewValue);
    }

    private void GammaSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GammaValue != null)
            GammaValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.Gamma = (float)e.NewValue;
        _effect.Configuration.Set("gamma", (float)e.NewValue);
    }

    private void InvertCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool invert = InvertCheckBox.IsChecked == true;
        _effect.Invert = invert;
        _effect.Configuration.Set("invert", invert);
    }

    private void SaturationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SaturationValue != null)
            SaturationValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.Saturation = (float)e.NewValue;
        _effect.Configuration.Set("saturation", (float)e.NewValue);
    }

    private void QuantizeLevelsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QuantizeLevelsValue != null)
            QuantizeLevelsValue.Text = $"{(int)e.NewValue}";

        if (_effect == null || _isLoading) return;

        _effect.QuantizeLevels = (int)e.NewValue;
        _effect.Configuration.Set("quantizeLevels", (int)e.NewValue);
    }

    private void PreserveLuminanceCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool preserve = PreserveLuminanceCheckBox.IsChecked == true;
        _effect.PreserveLuminance = preserve;
        _effect.Configuration.Set("preserveLuminance", preserve);
    }

    private void AntialiasingCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int antialiasing = AntialiasingCombo.SelectedIndex;
        _effect.Antialiasing = antialiasing;
        _effect.Configuration.Set("antialiasing", antialiasing);
    }

    private void CharShadowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool charShadow = CharShadowCheckBox.IsChecked == true;
        _effect.CharShadow = charShadow;
        _effect.Configuration.Set("charShadow", charShadow);
    }

    private void GridLinesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool gridLines = GridLinesCheckBox.IsChecked == true;
        _effect.GridLines = gridLines;
        _effect.Configuration.Set("gridLines", gridLines);
    }

    private void EdgeSoftnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeSoftnessValue != null)
            EdgeSoftnessValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.EdgeSoftness = (float)e.NewValue;
        _effect.Configuration.Set("edgeSoftness", (float)e.NewValue);
    }

    private void InnerGlowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool innerGlow = InnerGlowCheckBox.IsChecked == true;
        _effect.InnerGlow = innerGlow;
        _effect.Configuration.Set("innerGlow", innerGlow);
    }

    #endregion
}
