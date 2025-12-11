using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Settings control for Braille filter.
/// </summary>
public partial class BrailleSettings : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;

    public BrailleSettings()
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
            LayoutModeCombo.SelectedIndex = _effect.BR_LayoutMode;
            UpdateLayoutVisibility(_effect.BR_LayoutMode);

            RadiusSlider.Value = _effect.BR_Radius;
            RadiusValue.Text = $"{_effect.BR_Radius:F0} px";

            RectWidthSlider.Value = _effect.BR_RectWidth;
            RectWidthValue.Text = $"{_effect.BR_RectWidth:F0} px";

            RectHeightSlider.Value = _effect.BR_RectHeight;
            RectHeightValue.Text = $"{_effect.BR_RectHeight:F0} px";

            // Braille settings
            ThresholdSlider.Value = _effect.BR_Threshold;
            ThresholdValue.Text = $"{_effect.BR_Threshold * 100:F0}%";

            AdaptiveThresholdCheckBox.IsChecked = _effect.BR_AdaptiveThreshold;

            DotSizeSlider.Value = _effect.BR_DotSize;
            DotSizeValue.Text = $"{_effect.BR_DotSize * 100:F0}%";

            DotSpacingSlider.Value = _effect.BR_DotSpacing;
            DotSpacingValue.Text = $"{_effect.BR_DotSpacing:F1} px";

            InvertDotsCheckBox.IsChecked = _effect.BR_InvertDots;

            // Cell size
            CellWidthSlider.Value = _effect.BR_CellWidth;
            CellWidthValue.Text = $"{_effect.BR_CellWidth:F0} px";

            CellHeightSlider.Value = _effect.BR_CellHeight;
            CellHeightValue.Text = $"{_effect.BR_CellHeight:F0} px";

            // Colors
            ForegroundColorTextBox.Text = Vector4ToHex(_effect.BR_ForegroundColor);
            ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.BR_ForegroundColor));

            BackgroundColorTextBox.Text = Vector4ToHex(_effect.BR_BackgroundColor);
            BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.BR_BackgroundColor));

            // Brightness
            BrightnessSlider.Value = _effect.BR_Brightness;
            BrightnessValue.Text = $"{_effect.BR_Brightness:F2}";

            ContrastSlider.Value = _effect.BR_Contrast;
            ContrastValue.Text = $"{_effect.BR_Contrast:F2}";
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
        return new Vector4(1, 1, 1, 1); // Default white
    }

    #endregion

    #region Event Handlers

    private void LayoutModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.BR_LayoutMode = layoutMode;
        _effect.Configuration.Set("br_layoutMode", layoutMode);
        UpdateLayoutVisibility(layoutMode);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.BR_Radius = (float)e.NewValue;
        _effect.Configuration.Set("br_radius", (float)e.NewValue);
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.BR_RectWidth = (float)e.NewValue;
        _effect.Configuration.Set("br_rectWidth", (float)e.NewValue);
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.BR_RectHeight = (float)e.NewValue;
        _effect.Configuration.Set("br_rectHeight", (float)e.NewValue);
    }

    private void ThresholdSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThresholdValue != null)
            ThresholdValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.BR_Threshold = (float)e.NewValue;
        _effect.Configuration.Set("br_threshold", (float)e.NewValue);
    }

    private void AdaptiveThresholdCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool isChecked = AdaptiveThresholdCheckBox.IsChecked == true;
        _effect.BR_AdaptiveThreshold = isChecked;
        _effect.Configuration.Set("br_adaptiveThreshold", isChecked);
    }

    private void DotSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DotSizeValue != null)
            DotSizeValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.BR_DotSize = (float)e.NewValue;
        _effect.Configuration.Set("br_dotSize", (float)e.NewValue);
    }

    private void DotSpacingSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DotSpacingValue != null)
            DotSpacingValue.Text = $"{e.NewValue:F1} px";

        if (_effect == null || _isLoading) return;

        _effect.BR_DotSpacing = (float)e.NewValue;
        _effect.Configuration.Set("br_dotSpacing", (float)e.NewValue);
    }

    private void InvertDotsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool isChecked = InvertDotsCheckBox.IsChecked == true;
        _effect.BR_InvertDots = isChecked;
        _effect.Configuration.Set("br_invertDots", isChecked);
    }

    private void CellWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellWidthValue != null)
            CellWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.BR_CellWidth = (float)e.NewValue;
        _effect.Configuration.Set("br_cellWidth", (float)e.NewValue);
    }

    private void CellHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellHeightValue != null)
            CellHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.BR_CellHeight = (float)e.NewValue;
        _effect.Configuration.Set("br_cellHeight", (float)e.NewValue);
    }

    private void ForegroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || ForegroundColorPreview == null) return;

        var color = HexToVector4(ForegroundColorTextBox.Text);
        ForegroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.BR_ForegroundColor = color;
        _effect.Configuration.Set("br_foregroundColor", color);
    }

    private void BackgroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || BackgroundColorPreview == null) return;

        var color = HexToVector4(BackgroundColorTextBox.Text);
        BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.BR_BackgroundColor = color;
        _effect.Configuration.Set("br_backgroundColor", color);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessValue != null)
            BrightnessValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.BR_Brightness = (float)e.NewValue;
        _effect.Configuration.Set("br_brightness", (float)e.NewValue);
    }

    private void ContrastSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ContrastValue != null)
            ContrastValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.BR_Contrast = (float)e.NewValue;
        _effect.Configuration.Set("br_contrast", (float)e.NewValue);
    }

    #endregion
}
