using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Settings control for EdgeASCII filter.
/// </summary>
public partial class EdgeASCIISettings : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;

    public EdgeASCIISettings()
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
            LayoutModeCombo.SelectedIndex = _effect.EA_LayoutMode;
            UpdateLayoutVisibility(_effect.EA_LayoutMode);

            RadiusSlider.Value = _effect.EA_Radius;
            RadiusValue.Text = $"{_effect.EA_Radius:F0} px";

            RectWidthSlider.Value = _effect.EA_RectWidth;
            RectWidthValue.Text = $"{_effect.EA_RectWidth:F0} px";

            RectHeightSlider.Value = _effect.EA_RectHeight;
            RectHeightValue.Text = $"{_effect.EA_RectHeight:F0} px";

            // Cell size
            CellWidthSlider.Value = _effect.EA_CellWidth;
            CellWidthValue.Text = $"{_effect.EA_CellWidth:F0} px";

            CellHeightSlider.Value = _effect.EA_CellHeight;
            CellHeightValue.Text = $"{_effect.EA_CellHeight:F0} px";

            // Edge detection
            EdgeThresholdSlider.Value = _effect.EA_EdgeThreshold;
            EdgeThresholdValue.Text = $"{_effect.EA_EdgeThreshold * 100:F0}%";

            LineThicknessSlider.Value = _effect.EA_LineThickness;
            LineThicknessValue.Text = $"{_effect.EA_LineThickness:F0}";

            ShowCornersCheckBox.IsChecked = _effect.EA_ShowCorners;
            FillBackgroundCheckBox.IsChecked = _effect.EA_FillBackground;
            UpdateBackgroundOpacityVisibility(_effect.EA_FillBackground);

            BackgroundOpacitySlider.Value = _effect.EA_BackgroundOpacity;
            BackgroundOpacityValue.Text = $"{_effect.EA_BackgroundOpacity * 100:F0}%";

            EdgeBrightnessSlider.Value = _effect.EA_EdgeBrightness;
            EdgeBrightnessValue.Text = $"{_effect.EA_EdgeBrightness:F2}";

            // Colors
            EdgeColorTextBox.Text = Vector4ToHex(_effect.EA_EdgeColor);
            EdgeColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.EA_EdgeColor));

            BackgroundColorTextBox.Text = Vector4ToHex(_effect.EA_BackgroundColor);
            BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.EA_BackgroundColor));

            // Brightness
            BrightnessSlider.Value = _effect.EA_Brightness;
            BrightnessValue.Text = $"{_effect.EA_Brightness:F2}";

            ContrastSlider.Value = _effect.EA_Contrast;
            ContrastValue.Text = $"{_effect.EA_Contrast:F2}";
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

    private void UpdateBackgroundOpacityVisibility(bool showBackground)
    {
        BackgroundOpacityPanel.Visibility = showBackground ? Visibility.Visible : Visibility.Collapsed;
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
        return new Vector4(0f, 1f, 0f, 1f); // Default green
    }

    #endregion

    #region Event Handlers

    private void LayoutModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.EA_LayoutMode = layoutMode;
        _effect.Configuration.Set("ea_layoutMode", layoutMode);
        UpdateLayoutVisibility(layoutMode);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.EA_Radius = (float)e.NewValue;
        _effect.Configuration.Set("ea_radius", (float)e.NewValue);
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.EA_RectWidth = (float)e.NewValue;
        _effect.Configuration.Set("ea_rectWidth", (float)e.NewValue);
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.EA_RectHeight = (float)e.NewValue;
        _effect.Configuration.Set("ea_rectHeight", (float)e.NewValue);
    }

    private void CellWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellWidthValue != null)
            CellWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.EA_CellWidth = (float)e.NewValue;
        _effect.Configuration.Set("ea_cellWidth", (float)e.NewValue);
    }

    private void CellHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellHeightValue != null)
            CellHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.EA_CellHeight = (float)e.NewValue;
        _effect.Configuration.Set("ea_cellHeight", (float)e.NewValue);
    }

    private void EdgeThresholdSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeThresholdValue != null)
            EdgeThresholdValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.EA_EdgeThreshold = (float)e.NewValue;
        _effect.Configuration.Set("ea_edgeThreshold", (float)e.NewValue);
    }

    private void LineThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LineThicknessValue != null)
            LineThicknessValue.Text = $"{e.NewValue:F0}";

        if (_effect == null || _isLoading) return;

        _effect.EA_LineThickness = (float)e.NewValue;
        _effect.Configuration.Set("ea_lineThickness", (float)e.NewValue);
    }

    private void ShowCornersCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool isChecked = ShowCornersCheckBox.IsChecked == true;
        _effect.EA_ShowCorners = isChecked;
        _effect.Configuration.Set("ea_showCorners", isChecked);
    }

    private void FillBackgroundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool isChecked = FillBackgroundCheckBox.IsChecked == true;
        _effect.EA_FillBackground = isChecked;
        _effect.Configuration.Set("ea_fillBackground", isChecked);
        UpdateBackgroundOpacityVisibility(isChecked);
    }

    private void BackgroundOpacitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BackgroundOpacityValue != null)
            BackgroundOpacityValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.EA_BackgroundOpacity = (float)e.NewValue;
        _effect.Configuration.Set("ea_backgroundOpacity", (float)e.NewValue);
    }

    private void EdgeBrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeBrightnessValue != null)
            EdgeBrightnessValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.EA_EdgeBrightness = (float)e.NewValue;
        _effect.Configuration.Set("ea_edgeBrightness", (float)e.NewValue);
    }

    private void EdgeColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || EdgeColorPreview == null) return;

        var color = HexToVector4(EdgeColorTextBox.Text);
        EdgeColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.EA_EdgeColor = color;
        _effect.Configuration.Set("ea_edgeColor", color);
    }

    private void BackgroundColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || BackgroundColorPreview == null) return;

        var color = HexToVector4(BackgroundColorTextBox.Text);
        BackgroundColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.EA_BackgroundColor = color;
        _effect.Configuration.Set("ea_backgroundColor", color);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessValue != null)
            BrightnessValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.EA_Brightness = (float)e.NewValue;
        _effect.Configuration.Set("ea_brightness", (float)e.NewValue);
    }

    private void ContrastSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ContrastValue != null)
            ContrastValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.EA_Contrast = (float)e.NewValue;
        _effect.Configuration.Set("ea_contrast", (float)e.NewValue);
    }

    #endregion
}
