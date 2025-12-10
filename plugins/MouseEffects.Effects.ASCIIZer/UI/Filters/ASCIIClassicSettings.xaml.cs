using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Settings control for ASCII Art Classic filter.
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
            if (_effect.Configuration.TryGet("advancedMode", out bool advancedMode))
            {
                AdvancedModeRadio.IsChecked = advancedMode;
                BasicModeRadio.IsChecked = !advancedMode;
                UpdateSettingsPanelVisibility(advancedMode);
            }

            // Layout
            if (_effect.Configuration.TryGet("layoutMode", out int layoutMode))
            {
                LayoutModeCombo.SelectedIndex = layoutMode;
                UpdateLayoutVisibility(layoutMode);
            }

            if (_effect.Configuration.TryGet("radius", out float radius))
            {
                RadiusSlider.Value = radius;
                RadiusValue.Text = $"{radius:F0} px";
            }

            if (_effect.Configuration.TryGet("rectWidth", out float rectWidth))
            {
                RectWidthSlider.Value = rectWidth;
                RectWidthValue.Text = $"{rectWidth:F0} px";
            }

            if (_effect.Configuration.TryGet("rectHeight", out float rectHeight))
            {
                RectHeightSlider.Value = rectHeight;
                RectHeightValue.Text = $"{rectHeight:F0} px";
            }

            // Cell size
            if (_effect.Configuration.TryGet("cellWidth", out float cellWidth))
            {
                CellWidthSlider.Value = cellWidth;
                CellWidthValue.Text = $"{cellWidth:F0} px";
            }

            if (_effect.Configuration.TryGet("cellHeight", out float cellHeight))
            {
                CellHeightSlider.Value = cellHeight;
                CellHeightValue.Text = $"{cellHeight:F0} px";
            }

            // Character set
            if (_effect.Configuration.TryGet("charsetPreset", out int charsetPreset))
            {
                CharsetCombo.SelectedIndex = charsetPreset;
                CustomCharsetPanel.Visibility = charsetPreset == 4 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("customCharset", out string? customCharset))
            {
                CustomCharsetTextBox.Text = customCharset ?? "";
            }

            // Color mode
            if (_effect.Configuration.TryGet("colorMode", out int colorMode))
            {
                ColorModeCombo.SelectedIndex = colorMode;
                MonochromeColorsPanel.Visibility = colorMode > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("foreground", out Vector4 foreground))
            {
                ForegroundColorTextBox.Text = Vector4ToHex(foreground);
                ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(foreground));
            }

            if (_effect.Configuration.TryGet("background", out Vector4 background))
            {
                BackgroundColorTextBox.Text = Vector4ToHex(background);
                BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(background));
            }

            // Advanced: Font
            if (_effect.Configuration.TryGet("fontFamily", out int fontFamily))
                FontFamilyCombo.SelectedIndex = fontFamily;

            if (_effect.Configuration.TryGet("fontWeight", out int fontWeight))
                FontWeightCombo.SelectedIndex = fontWeight;

            // Advanced: Brightness
            if (_effect.Configuration.TryGet("brightness", out float brightness))
            {
                BrightnessSlider.Value = brightness;
                BrightnessValue.Text = $"{brightness:F2}";
            }

            if (_effect.Configuration.TryGet("contrast", out float contrast))
            {
                ContrastSlider.Value = contrast;
                ContrastValue.Text = $"{contrast:F2}";
            }

            if (_effect.Configuration.TryGet("gamma", out float gamma))
            {
                GammaSlider.Value = gamma;
                GammaValue.Text = $"{gamma:F2}";
            }

            if (_effect.Configuration.TryGet("invert", out bool invert))
                InvertCheckBox.IsChecked = invert;

            // Advanced: Color
            if (_effect.Configuration.TryGet("saturation", out float saturation))
            {
                SaturationSlider.Value = saturation;
                SaturationValue.Text = $"{saturation * 100:F0}%";
            }

            if (_effect.Configuration.TryGet("quantizeLevels", out int quantizeLevels))
            {
                QuantizeLevelsSlider.Value = quantizeLevels;
                QuantizeLevelsValue.Text = $"{quantizeLevels}";
            }

            if (_effect.Configuration.TryGet("preserveLuminance", out bool preserveLuminance))
                PreserveLuminanceCheckBox.IsChecked = preserveLuminance;

            // Advanced: Visual Effects
            if (_effect.Configuration.TryGet("scanlines", out bool scanlines))
            {
                ScanlinesCheckBox.IsChecked = scanlines;
                ScanlinesSettingsPanel.Visibility = scanlines ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("scanlineIntensity", out float scanlineIntensity))
            {
                ScanlineIntensitySlider.Value = scanlineIntensity;
                ScanlineIntensityValue.Text = $"{scanlineIntensity:F2}";
            }

            if (_effect.Configuration.TryGet("crtCurvature", out bool crtCurvature))
            {
                CrtCurvatureCheckBox.IsChecked = crtCurvature;
                CrtSettingsPanel.Visibility = crtCurvature ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("crtAmount", out float crtAmount))
            {
                CrtAmountSlider.Value = crtAmount;
                CrtAmountValue.Text = $"{crtAmount:F2}";
            }

            if (_effect.Configuration.TryGet("vignette", out bool vignette))
            {
                VignetteCheckBox.IsChecked = vignette;
                VignetteSettingsPanel.Visibility = vignette ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("vignetteIntensity", out float vignetteIntensity))
            {
                VignetteIntensitySlider.Value = vignetteIntensity;
                VignetteIntensityValue.Text = $"{vignetteIntensity:F2}";
            }

            if (_effect.Configuration.TryGet("chromatic", out bool chromatic))
            {
                ChromaticCheckBox.IsChecked = chromatic;
                ChromaticSettingsPanel.Visibility = chromatic ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("chromaticOffset", out float chromaticOffset))
            {
                ChromaticOffsetSlider.Value = chromaticOffset;
                ChromaticOffsetValue.Text = $"{chromaticOffset:F1} px";
            }

            if (_effect.Configuration.TryGet("noise", out bool noise))
            {
                NoiseCheckBox.IsChecked = noise;
                NoiseSettingsPanel.Visibility = noise ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("noiseAmount", out float noiseAmount))
            {
                NoiseAmountSlider.Value = noiseAmount;
                NoiseAmountValue.Text = $"{noiseAmount:F2}";
            }

            if (_effect.Configuration.TryGet("flicker", out bool flicker))
            {
                FlickerCheckBox.IsChecked = flicker;
                FlickerSettingsPanel.Visibility = flicker ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_effect.Configuration.TryGet("flickerSpeed", out float flickerSpeed))
            {
                FlickerSpeedSlider.Value = flickerSpeed;
                FlickerSpeedValue.Text = $"{flickerSpeed:F1}x";
            }

            // Advanced: Character Rendering
            if (_effect.Configuration.TryGet("antialiasing", out int antialiasing))
                AntialiasingCombo.SelectedIndex = antialiasing;

            if (_effect.Configuration.TryGet("charShadow", out bool charShadow))
                CharShadowCheckBox.IsChecked = charShadow;

            if (_effect.Configuration.TryGet("gridLines", out bool gridLines))
                GridLinesCheckBox.IsChecked = gridLines;

            // Advanced: Edge
            if (_effect.Configuration.TryGet("edgeSoftness", out float edgeSoftness))
            {
                EdgeSoftnessSlider.Value = edgeSoftness;
                EdgeSoftnessValue.Text = $"{edgeSoftness:F0} px";
            }

            if (_effect.Configuration.TryGet("innerGlow", out bool innerGlow))
                InnerGlowCheckBox.IsChecked = innerGlow;
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
        _effect.Configuration.Set("advancedMode", advanced);
        UpdateSettingsPanelVisibility(advanced);
    }

    private void LayoutModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.Configuration.Set("layoutMode", layoutMode);
        UpdateLayoutVisibility(layoutMode);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)RadiusSlider.Value;
        _effect.Configuration.Set("radius", value);
        RadiusValue.Text = $"{value:F0} px";
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)RectWidthSlider.Value;
        _effect.Configuration.Set("rectWidth", value);
        RectWidthValue.Text = $"{value:F0} px";
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)RectHeightSlider.Value;
        _effect.Configuration.Set("rectHeight", value);
        RectHeightValue.Text = $"{value:F0} px";
    }

    private void CellWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)CellWidthSlider.Value;
        _effect.Configuration.Set("cellWidth", value);
        CellWidthValue.Text = $"{value:F0} px";
    }

    private void CellHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)CellHeightSlider.Value;
        _effect.Configuration.Set("cellHeight", value);
        CellHeightValue.Text = $"{value:F0} px";
    }

    private void CharsetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int preset = CharsetCombo.SelectedIndex;
        _effect.Configuration.Set("charsetPreset", preset);
        CustomCharsetPanel.Visibility = preset == 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CustomCharsetTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        _effect.Configuration.Set("customCharset", CustomCharsetTextBox.Text);
    }

    private void ColorModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int colorMode = ColorModeCombo.SelectedIndex;
        _effect.Configuration.Set("colorMode", colorMode);
        MonochromeColorsPanel.Visibility = colorMode > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ForegroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var color = HexToVector4(ForegroundColorTextBox.Text);
        _effect.Configuration.Set("foreground", color);
        ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));
    }

    private void BackgroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        var color = HexToVector4(BackgroundColorTextBox.Text);
        _effect.Configuration.Set("background", color);
        BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));
    }

    private void FontFamilyCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("fontFamily", FontFamilyCombo.SelectedIndex);
    }

    private void FontWeightCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("fontWeight", FontWeightCombo.SelectedIndex);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)BrightnessSlider.Value;
        _effect.Configuration.Set("brightness", value);
        BrightnessValue.Text = $"{value:F2}";
    }

    private void ContrastSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)ContrastSlider.Value;
        _effect.Configuration.Set("contrast", value);
        ContrastValue.Text = $"{value:F2}";
    }

    private void GammaSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)GammaSlider.Value;
        _effect.Configuration.Set("gamma", value);
        GammaValue.Text = $"{value:F2}";
    }

    private void InvertCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("invert", InvertCheckBox.IsChecked == true);
    }

    private void SaturationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)SaturationSlider.Value;
        _effect.Configuration.Set("saturation", value);
        SaturationValue.Text = $"{value * 100:F0}%";
    }

    private void QuantizeLevelsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        int value = (int)QuantizeLevelsSlider.Value;
        _effect.Configuration.Set("quantizeLevels", value);
        QuantizeLevelsValue.Text = $"{value}";
    }

    private void PreserveLuminanceCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("preserveLuminance", PreserveLuminanceCheckBox.IsChecked == true);
    }

    private void ScanlinesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool enabled = ScanlinesCheckBox.IsChecked == true;
        _effect.Configuration.Set("scanlines", enabled);
        ScanlinesSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ScanlineIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)ScanlineIntensitySlider.Value;
        _effect.Configuration.Set("scanlineIntensity", value);
        ScanlineIntensityValue.Text = $"{value:F2}";
    }

    private void CrtCurvatureCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool enabled = CrtCurvatureCheckBox.IsChecked == true;
        _effect.Configuration.Set("crtCurvature", enabled);
        CrtSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CrtAmountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)CrtAmountSlider.Value;
        _effect.Configuration.Set("crtAmount", value);
        CrtAmountValue.Text = $"{value:F2}";
    }

    private void VignetteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool enabled = VignetteCheckBox.IsChecked == true;
        _effect.Configuration.Set("vignette", enabled);
        VignetteSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void VignetteIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)VignetteIntensitySlider.Value;
        _effect.Configuration.Set("vignetteIntensity", value);
        VignetteIntensityValue.Text = $"{value:F2}";
    }

    private void ChromaticCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool enabled = ChromaticCheckBox.IsChecked == true;
        _effect.Configuration.Set("chromatic", enabled);
        ChromaticSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ChromaticOffsetSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)ChromaticOffsetSlider.Value;
        _effect.Configuration.Set("chromaticOffset", value);
        ChromaticOffsetValue.Text = $"{value:F1} px";
    }

    private void NoiseCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool enabled = NoiseCheckBox.IsChecked == true;
        _effect.Configuration.Set("noise", enabled);
        NoiseSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NoiseAmountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)NoiseAmountSlider.Value;
        _effect.Configuration.Set("noiseAmount", value);
        NoiseAmountValue.Text = $"{value:F2}";
    }

    private void FlickerCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool enabled = FlickerCheckBox.IsChecked == true;
        _effect.Configuration.Set("flicker", enabled);
        FlickerSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void FlickerSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)FlickerSpeedSlider.Value;
        _effect.Configuration.Set("flickerSpeed", value);
        FlickerSpeedValue.Text = $"{value:F1}x";
    }

    private void AntialiasingCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("antialiasing", AntialiasingCombo.SelectedIndex);
    }

    private void CharShadowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("charShadow", CharShadowCheckBox.IsChecked == true);
    }

    private void GridLinesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("gridLines", GridLinesCheckBox.IsChecked == true);
    }

    private void EdgeSoftnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        float value = (float)EdgeSoftnessSlider.Value;
        _effect.Configuration.Set("edgeSoftness", value);
        EdgeSoftnessValue.Text = $"{value:F0} px";
    }

    private void InnerGlowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Configuration.Set("innerGlow", InnerGlowCheckBox.IsChecked == true);
    }

    #endregion
}
