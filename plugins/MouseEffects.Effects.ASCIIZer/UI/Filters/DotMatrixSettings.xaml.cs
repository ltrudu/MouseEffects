using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Settings control for Dot Matrix filter.
/// </summary>
public partial class DotMatrixSettings : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;

    public DotMatrixSettings()
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
            // Layout
            LayoutModeCombo.SelectedIndex = _effect.DM_LayoutMode;
            UpdateLayoutVisibility(_effect.DM_LayoutMode);

            RadiusSlider.Value = _effect.DM_Radius;
            RadiusValue.Text = $"{_effect.DM_Radius:F0} px";

            RectWidthSlider.Value = _effect.DM_RectWidth;
            RectWidthValue.Text = $"{_effect.DM_RectWidth:F0} px";

            RectHeightSlider.Value = _effect.DM_RectHeight;
            RectHeightValue.Text = $"{_effect.DM_RectHeight:F0} px";

            // Dot settings
            CellSizeSlider.Value = _effect.DM_CellSize;
            CellSizeValue.Text = $"{_effect.DM_CellSize:F0} px";

            DotSizeSlider.Value = _effect.DM_DotSize;
            DotSizeValue.Text = $"{_effect.DM_DotSize * 100:F0}%";

            DotSpacingSlider.Value = _effect.DM_DotSpacing;
            DotSpacingValue.Text = $"{_effect.DM_DotSpacing:F0} px";

            LedShapeCombo.SelectedIndex = _effect.DM_LedShape;

            // Color mode
            ColorModeCombo.SelectedIndex = _effect.DM_ColorMode;
            MonochromeColorsPanel.Visibility = _effect.DM_ColorMode > 0 ? Visibility.Visible : Visibility.Collapsed;

            ForegroundColorTextBox.Text = Vector4ToHex(_effect.DM_ForegroundColor);
            ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.DM_ForegroundColor));

            BackgroundColorTextBox.Text = Vector4ToHex(_effect.DM_BackgroundColor);
            BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.DM_BackgroundColor));

            OffBrightnessSlider.Value = _effect.DM_OffBrightness;
            OffBrightnessValue.Text = $"{_effect.DM_OffBrightness * 100:F0}%";

            // Advanced
            RgbModeCheckBox.IsChecked = _effect.DM_RgbMode;

            // Brightness
            BrightnessSlider.Value = _effect.DM_Brightness;
            BrightnessValue.Text = $"{_effect.DM_Brightness:F2}";

            ContrastSlider.Value = _effect.DM_Contrast;
            ContrastValue.Text = $"{_effect.DM_Contrast:F2}";

            SaturationSlider.Value = _effect.DM_Saturation;
            SaturationValue.Text = $"{_effect.DM_Saturation * 100:F0}%";
        }
        finally
        {
            _isLoading = false;
        }
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

    private void LayoutModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.DM_LayoutMode = layoutMode;
        _effect.Configuration.Set("dm_layoutMode", layoutMode);
        UpdateLayoutVisibility(layoutMode);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.DM_Radius = (float)e.NewValue;
        _effect.Configuration.Set("dm_radius", (float)e.NewValue);
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.DM_RectWidth = (float)e.NewValue;
        _effect.Configuration.Set("dm_rectWidth", (float)e.NewValue);
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.DM_RectHeight = (float)e.NewValue;
        _effect.Configuration.Set("dm_rectHeight", (float)e.NewValue);
    }

    private void CellSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellSizeValue != null)
            CellSizeValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.DM_CellSize = (float)e.NewValue;
        _effect.Configuration.Set("dm_cellSize", (float)e.NewValue);
    }

    private void DotSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DotSizeValue != null)
            DotSizeValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.DM_DotSize = (float)e.NewValue;
        _effect.Configuration.Set("dm_dotSize", (float)e.NewValue);
    }

    private void DotSpacingSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DotSpacingValue != null)
            DotSpacingValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.DM_DotSpacing = (float)e.NewValue;
        _effect.Configuration.Set("dm_dotSpacing", (float)e.NewValue);
    }

    private void LedShapeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int shape = LedShapeCombo.SelectedIndex;
        _effect.DM_LedShape = shape;
        _effect.Configuration.Set("dm_ledShape", shape);
    }

    private void ColorModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int colorMode = ColorModeCombo.SelectedIndex;
        _effect.DM_ColorMode = colorMode;
        _effect.Configuration.Set("dm_colorMode", colorMode);
        MonochromeColorsPanel.Visibility = colorMode > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ForegroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || ForegroundColorPreview == null) return;

        var color = HexToVector4(ForegroundColorTextBox.Text);
        ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.DM_ForegroundColor = color;
        _effect.Configuration.Set("dm_foregroundColor", color);
    }

    private void BackgroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || BackgroundColorPreview == null) return;

        var color = HexToVector4(BackgroundColorTextBox.Text);
        BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.DM_BackgroundColor = color;
        _effect.Configuration.Set("dm_backgroundColor", color);
    }

    private void OffBrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OffBrightnessValue != null)
            OffBrightnessValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.DM_OffBrightness = (float)e.NewValue;
        _effect.Configuration.Set("dm_offBrightness", (float)e.NewValue);
    }

    private void RgbModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool rgbMode = RgbModeCheckBox.IsChecked == true;
        _effect.DM_RgbMode = rgbMode;
        _effect.Configuration.Set("dm_rgbMode", rgbMode);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessValue != null)
            BrightnessValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.DM_Brightness = (float)e.NewValue;
        _effect.Configuration.Set("dm_brightness", (float)e.NewValue);
    }

    private void ContrastSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ContrastValue != null)
            ContrastValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.DM_Contrast = (float)e.NewValue;
        _effect.Configuration.Set("dm_contrast", (float)e.NewValue);
    }

    private void SaturationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SaturationValue != null)
            SaturationValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.DM_Saturation = (float)e.NewValue;
        _effect.Configuration.Set("dm_saturation", (float)e.NewValue);
    }

    #endregion
}
