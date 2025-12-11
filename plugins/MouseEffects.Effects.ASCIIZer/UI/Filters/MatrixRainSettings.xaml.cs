using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Settings control for Matrix Rain filter.
/// </summary>
public partial class MatrixRainSettings : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;

    public MatrixRainSettings()
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
            LayoutModeCombo.SelectedIndex = _effect.MR_LayoutMode;
            UpdateLayoutVisibility(_effect.MR_LayoutMode);

            RadiusSlider.Value = _effect.MR_Radius;
            RadiusValue.Text = $"{_effect.MR_Radius:F0} px";

            RectWidthSlider.Value = _effect.MR_RectWidth;
            RectWidthValue.Text = $"{_effect.MR_RectWidth:F0} px";

            RectHeightSlider.Value = _effect.MR_RectHeight;
            RectHeightValue.Text = $"{_effect.MR_RectHeight:F0} px";

            // Animation
            FallSpeedSlider.Value = _effect.MR_FallSpeed;
            FallSpeedValue.Text = $"{_effect.MR_FallSpeed:F1}x";

            TrailLengthSlider.Value = _effect.MR_TrailLength;
            TrailLengthValue.Text = $"{_effect.MR_TrailLength:F0} chars";

            CharCycleSpeedSlider.Value = _effect.MR_CharCycleSpeed;
            CharCycleSpeedValue.Text = $"{_effect.MR_CharCycleSpeed:F1}x";

            ColumnDensitySlider.Value = _effect.MR_ColumnDensity;
            ColumnDensityValue.Text = $"{_effect.MR_ColumnDensity * 100:F0}%";

            // Cell size
            CellWidthSlider.Value = _effect.MR_CellWidth;
            CellWidthValue.Text = $"{_effect.MR_CellWidth:F0} px";

            CellHeightSlider.Value = _effect.MR_CellHeight;
            CellHeightValue.Text = $"{_effect.MR_CellHeight:F0} px";

            // Colors
            PrimaryColorTextBox.Text = Vector4ToHex(_effect.MR_PrimaryColor);
            PrimaryColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.MR_PrimaryColor));

            GlowColorTextBox.Text = Vector4ToHex(_effect.MR_GlowColor);
            GlowColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.MR_GlowColor));

            GlowIntensitySlider.Value = _effect.MR_GlowIntensity;
            GlowIntensityValue.Text = $"{_effect.MR_GlowIntensity * 100:F0}%";

            // Background
            BackgroundFadeSlider.Value = _effect.MR_BackgroundFade;
            BackgroundFadeValue.Text = $"{_effect.MR_BackgroundFade * 100:F0}%";

            BrightnessSlider.Value = _effect.MR_Brightness;
            BrightnessValue.Text = $"{_effect.MR_Brightness:F2}";

            ContrastSlider.Value = _effect.MR_Contrast;
            ContrastValue.Text = $"{_effect.MR_Contrast:F2}";
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
        return new Vector4(0, 1, 0.25f, 1); // Default matrix green
    }

    #endregion

    #region Event Handlers

    private void LayoutModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.MR_LayoutMode = layoutMode;
        _effect.Configuration.Set("mr_layoutMode", layoutMode);
        UpdateLayoutVisibility(layoutMode);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.MR_Radius = (float)e.NewValue;
        _effect.Configuration.Set("mr_radius", (float)e.NewValue);
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.MR_RectWidth = (float)e.NewValue;
        _effect.Configuration.Set("mr_rectWidth", (float)e.NewValue);
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.MR_RectHeight = (float)e.NewValue;
        _effect.Configuration.Set("mr_rectHeight", (float)e.NewValue);
    }

    private void FallSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FallSpeedValue != null)
            FallSpeedValue.Text = $"{e.NewValue:F1}x";

        if (_effect == null || _isLoading) return;

        _effect.MR_FallSpeed = (float)e.NewValue;
        _effect.Configuration.Set("mr_fallSpeed", (float)e.NewValue);
    }

    private void TrailLengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrailLengthValue != null)
            TrailLengthValue.Text = $"{(int)e.NewValue} chars";

        if (_effect == null || _isLoading) return;

        _effect.MR_TrailLength = (float)e.NewValue;
        _effect.Configuration.Set("mr_trailLength", (float)e.NewValue);
    }

    private void CharCycleSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CharCycleSpeedValue != null)
            CharCycleSpeedValue.Text = $"{e.NewValue:F1}x";

        if (_effect == null || _isLoading) return;

        _effect.MR_CharCycleSpeed = (float)e.NewValue;
        _effect.Configuration.Set("mr_charCycleSpeed", (float)e.NewValue);
    }

    private void ColumnDensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ColumnDensityValue != null)
            ColumnDensityValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.MR_ColumnDensity = (float)e.NewValue;
        _effect.Configuration.Set("mr_columnDensity", (float)e.NewValue);
    }

    private void CellWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellWidthValue != null)
            CellWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.MR_CellWidth = (float)e.NewValue;
        _effect.Configuration.Set("mr_cellWidth", (float)e.NewValue);
    }

    private void CellHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellHeightValue != null)
            CellHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.MR_CellHeight = (float)e.NewValue;
        _effect.Configuration.Set("mr_cellHeight", (float)e.NewValue);
    }

    private void PrimaryColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || PrimaryColorPreview == null) return;

        var color = HexToVector4(PrimaryColorTextBox.Text);
        PrimaryColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.MR_PrimaryColor = color;
        _effect.Configuration.Set("mr_primaryColor", color);
    }

    private void GlowColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || GlowColorPreview == null) return;

        var color = HexToVector4(GlowColorTextBox.Text);
        GlowColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.MR_GlowColor = color;
        _effect.Configuration.Set("mr_glowColor", color);
    }

    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.MR_GlowIntensity = (float)e.NewValue;
        _effect.Configuration.Set("mr_glowIntensity", (float)e.NewValue);
    }

    private void BackgroundFadeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BackgroundFadeValue != null)
            BackgroundFadeValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.MR_BackgroundFade = (float)e.NewValue;
        _effect.Configuration.Set("mr_backgroundFade", (float)e.NewValue);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessValue != null)
            BrightnessValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.MR_Brightness = (float)e.NewValue;
        _effect.Configuration.Set("mr_brightness", (float)e.NewValue);
    }

    private void ContrastSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ContrastValue != null)
            ContrastValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.MR_Contrast = (float)e.NewValue;
        _effect.Configuration.Set("mr_contrast", (float)e.NewValue);
    }

    #endregion
}
