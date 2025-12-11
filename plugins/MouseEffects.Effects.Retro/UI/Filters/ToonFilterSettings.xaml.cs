using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.Retro.UI.Filters;

/// <summary>
/// Settings control for Toon Filter - Edge detection and color quantization.
/// Scaling mode is handled by PreFilterSettings.
/// </summary>
public partial class ToonFilterSettings : UserControl
{
    private RetroEffect? _effect;
    private bool _isLoading;

    public ToonFilterSettings()
    {
        InitializeComponent();
    }

    public void Initialize(RetroEffect effect)
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
            // Toon Settings
            EdgeThresholdSlider.Value = _effect.Toon_EdgeThreshold;
            EdgeThresholdValue.Text = $"{_effect.Toon_EdgeThreshold:F2}";

            EdgeWidthSlider.Value = _effect.Toon_EdgeWidth;
            EdgeWidthValue.Text = $"{_effect.Toon_EdgeWidth:F1} px";

            ColorLevelsSlider.Value = _effect.Toon_ColorLevels;
            ColorLevelsValue.Text = $"{(int)_effect.Toon_ColorLevels}";

            SaturationSlider.Value = _effect.Toon_Saturation;
            SaturationValue.Text = $"{_effect.Toon_Saturation:F2}x";

            // Layout Mode
            LayoutModeCombo.SelectedIndex = _effect.LayoutMode;
            UpdateLayoutPanelVisibility();

            // Radius
            RadiusSlider.Value = _effect.Radius;
            RadiusValue.Text = $"{(int)_effect.Radius} px";

            // Rectangle
            RectWidthSlider.Value = _effect.RectWidth;
            RectWidthValue.Text = $"{(int)_effect.RectWidth} px";
            RectHeightSlider.Value = _effect.RectHeight;
            RectHeightValue.Text = $"{(int)_effect.RectHeight} px";

            // Edge Softness
            EdgeSoftnessSlider.Value = _effect.EdgeSoftness;
            EdgeSoftnessValue.Text = $"{(int)_effect.EdgeSoftness} px";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateLayoutPanelVisibility()
    {
        int layoutMode = LayoutModeCombo.SelectedIndex;

        CircleSettingsPanel.Visibility = layoutMode == 1 ? Visibility.Visible : Visibility.Collapsed;
        RectangleSettingsPanel.Visibility = layoutMode == 2 ? Visibility.Visible : Visibility.Collapsed;
        EdgeSoftnessPanel.Visibility = layoutMode > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    #region Event Handlers - Toon Settings

    private void EdgeThresholdSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeThresholdValue != null)
            EdgeThresholdValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.Toon_EdgeThreshold = (float)e.NewValue;
        _effect.Configuration.Set("toon_edgeThreshold", (float)e.NewValue);
    }

    private void EdgeWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeWidthValue != null)
            EdgeWidthValue.Text = $"{e.NewValue:F1} px";

        if (_effect == null || _isLoading) return;

        _effect.Toon_EdgeWidth = (float)e.NewValue;
        _effect.Configuration.Set("toon_edgeWidth", (float)e.NewValue);
    }

    private void ColorLevelsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ColorLevelsValue != null)
            ColorLevelsValue.Text = $"{(int)e.NewValue}";

        if (_effect == null || _isLoading) return;

        _effect.Toon_ColorLevels = (float)e.NewValue;
        _effect.Configuration.Set("toon_colorLevels", (float)e.NewValue);
    }

    private void SaturationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SaturationValue != null)
            SaturationValue.Text = $"{e.NewValue:F2}x";

        if (_effect == null || _isLoading) return;

        _effect.Toon_Saturation = (float)e.NewValue;
        _effect.Configuration.Set("toon_saturation", (float)e.NewValue);
    }

    #endregion

    #region Event Handlers - Layout

    private void LayoutModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int layoutMode = LayoutModeCombo.SelectedIndex;
        _effect.LayoutMode = layoutMode;
        _effect.Configuration.Set("layoutMode", layoutMode);

        UpdateLayoutPanelVisibility();
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = $"{(int)e.NewValue} px";

        if (_effect == null || _isLoading) return;

        _effect.Radius = (float)e.NewValue;
        _effect.Configuration.Set("radius", (float)e.NewValue);
    }

    private void RectWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectWidthValue != null)
            RectWidthValue.Text = $"{(int)e.NewValue} px";

        if (_effect == null || _isLoading) return;

        _effect.RectWidth = (float)e.NewValue;
        _effect.Configuration.Set("rectWidth", (float)e.NewValue);
    }

    private void RectHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RectHeightValue != null)
            RectHeightValue.Text = $"{(int)e.NewValue} px";

        if (_effect == null || _isLoading) return;

        _effect.RectHeight = (float)e.NewValue;
        _effect.Configuration.Set("rectHeight", (float)e.NewValue);
    }

    private void EdgeSoftnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeSoftnessValue != null)
            EdgeSoftnessValue.Text = $"{(int)e.NewValue} px";

        if (_effect == null || _isLoading) return;

        _effect.EdgeSoftness = (float)e.NewValue;
        _effect.Configuration.Set("edgeSoftness", (float)e.NewValue);
    }

    #endregion
}
