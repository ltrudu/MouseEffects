using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Settings control for Typewriter filter.
/// </summary>
public partial class TypewriterSettings : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;

    public TypewriterSettings()
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
            LayoutModeCombo.SelectedIndex = _effect.TW_LayoutMode;
            UpdateLayoutVisibility(_effect.TW_LayoutMode);

            RadiusSlider.Value = _effect.TW_Radius;
            RadiusValue.Text = $"{_effect.TW_Radius:F0} px";

            RectWidthSlider.Value = _effect.TW_RectWidth;
            RectWidthValue.Text = $"{_effect.TW_RectWidth:F0} px";

            RectHeightSlider.Value = _effect.TW_RectHeight;
            RectHeightValue.Text = $"{_effect.TW_RectHeight:F0} px";

            // Cell size
            CellWidthSlider.Value = _effect.TW_CellWidth;
            CellWidthValue.Text = $"{_effect.TW_CellWidth:F0} px";

            CellHeightSlider.Value = _effect.TW_CellHeight;
            CellHeightValue.Text = $"{_effect.TW_CellHeight:F0} px";

            // Typewriter effects
            InkVariationSlider.Value = _effect.TW_InkVariation;
            InkVariationValue.Text = $"{_effect.TW_InkVariation * 100:F0}%";

            PositionJitterSlider.Value = _effect.TW_PositionJitter;
            PositionJitterValue.Text = $"{_effect.TW_PositionJitter:F1} px";

            RibbonWearCheckBox.IsChecked = _effect.TW_RibbonWear;
            DoubleStrikeCheckBox.IsChecked = _effect.TW_DoubleStrike;

            AgeEffectSlider.Value = _effect.TW_AgeEffect;
            AgeEffectValue.Text = $"{_effect.TW_AgeEffect * 100:F0}%";

            // Colors
            InkColorTextBox.Text = Vector4ToHex(_effect.TW_InkColor);
            InkColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.TW_InkColor));

            PaperColorTextBox.Text = Vector4ToHex(_effect.TW_PaperColor);
            PaperColorPreview.Fill = new SolidColorBrush(Vector4ToColor(_effect.TW_PaperColor));

            // Brightness
            BrightnessSlider.Value = _effect.TW_Brightness;
            BrightnessValue.Text = $"{_effect.TW_Brightness:F2}";

            ContrastSlider.Value = _effect.TW_Contrast;
            ContrastValue.Text = $"{_effect.TW_Contrast:F2}";
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
        return new Vector4(0.1f, 0.1f, 0.1f, 1f); // Default dark ink
    }

    #endregion

    #region Event Handlers

    private void LayoutModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.TW_LayoutMode = layoutMode;
        _effect.Configuration.Set("tw_layoutMode", layoutMode);
        UpdateLayoutVisibility(layoutMode);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.TW_Radius = (float)e.NewValue;
        _effect.Configuration.Set("tw_radius", (float)e.NewValue);
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.TW_RectWidth = (float)e.NewValue;
        _effect.Configuration.Set("tw_rectWidth", (float)e.NewValue);
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.TW_RectHeight = (float)e.NewValue;
        _effect.Configuration.Set("tw_rectHeight", (float)e.NewValue);
    }

    private void CellWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellWidthValue != null)
            CellWidthValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.TW_CellWidth = (float)e.NewValue;
        _effect.Configuration.Set("tw_cellWidth", (float)e.NewValue);
    }

    private void CellHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CellHeightValue != null)
            CellHeightValue.Text = $"{e.NewValue:F0} px";

        if (_effect == null || _isLoading) return;

        _effect.TW_CellHeight = (float)e.NewValue;
        _effect.Configuration.Set("tw_cellHeight", (float)e.NewValue);
    }

    private void InkVariationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (InkVariationValue != null)
            InkVariationValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.TW_InkVariation = (float)e.NewValue;
        _effect.Configuration.Set("tw_inkVariation", (float)e.NewValue);
    }

    private void PositionJitterSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PositionJitterValue != null)
            PositionJitterValue.Text = $"{e.NewValue:F1} px";

        if (_effect == null || _isLoading) return;

        _effect.TW_PositionJitter = (float)e.NewValue;
        _effect.Configuration.Set("tw_positionJitter", (float)e.NewValue);
    }

    private void RibbonWearCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool isChecked = RibbonWearCheckBox.IsChecked == true;
        _effect.TW_RibbonWear = isChecked;
        _effect.Configuration.Set("tw_ribbonWear", isChecked);
    }

    private void DoubleStrikeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        bool isChecked = DoubleStrikeCheckBox.IsChecked == true;
        _effect.TW_DoubleStrike = isChecked;
        _effect.Configuration.Set("tw_doubleStrike", isChecked);
    }

    private void AgeEffectSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AgeEffectValue != null)
            AgeEffectValue.Text = $"{e.NewValue * 100:F0}%";

        if (_effect == null || _isLoading) return;

        _effect.TW_AgeEffect = (float)e.NewValue;
        _effect.Configuration.Set("tw_ageEffect", (float)e.NewValue);
    }

    private void InkColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || InkColorPreview == null) return;

        var color = HexToVector4(InkColorTextBox.Text);
        InkColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.TW_InkColor = color;
        _effect.Configuration.Set("tw_inkColor", color);
    }

    private void PaperColorTextBox_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || PaperColorPreview == null) return;

        var color = HexToVector4(PaperColorTextBox.Text);
        PaperColorPreview.Fill = new SolidColorBrush(Vector4ToColor(color));

        if (_effect == null) return;

        _effect.TW_PaperColor = color;
        _effect.Configuration.Set("tw_paperColor", color);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessValue != null)
            BrightnessValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.TW_Brightness = (float)e.NewValue;
        _effect.Configuration.Set("tw_brightness", (float)e.NewValue);
    }

    private void ContrastSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ContrastValue != null)
            ContrastValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.TW_Contrast = (float)e.NewValue;
        _effect.Configuration.Set("tw_contrast", (float)e.NewValue);
    }

    #endregion
}
