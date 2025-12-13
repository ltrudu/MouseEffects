using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.Retro.UI.Filters;

/// <summary>
/// Settings control for TV Filter - Phosphor settings and Layout.
/// Scaling mode is handled by PreFilterSettings.
/// </summary>
public partial class TVFilterSettings : UserControl
{
    private RetroEffect? _effect;
    private bool _isLoading;

    public TVFilterSettings()
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
            // Phosphor Settings
            PhosphorWidthSlider.Value = _effect.TV_PhosphorWidth;
            PhosphorWidthValue.Text = $"{_effect.TV_PhosphorWidth:F2}";

            PhosphorHeightSlider.Value = _effect.TV_PhosphorHeight;
            PhosphorHeightValue.Text = $"{_effect.TV_PhosphorHeight:F2}";

            PhosphorGapSlider.Value = _effect.TV_PhosphorGap;
            PhosphorGapValue.Text = $"{_effect.TV_PhosphorGap:F2}";

            BrightnessSlider.Value = _effect.TV_Brightness;
            BrightnessValue.Text = $"{_effect.TV_Brightness:F2}x";

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

    #region Event Handlers - Phosphor Settings

    private void PhosphorWidthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PhosphorWidthValue != null)
            PhosphorWidthValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.TV_PhosphorWidth = (float)e.NewValue;
        _effect.Configuration.Set("tv_phosphorWidth", (float)e.NewValue);
    }

    private void PhosphorHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PhosphorHeightValue != null)
            PhosphorHeightValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.TV_PhosphorHeight = (float)e.NewValue;
        _effect.Configuration.Set("tv_phosphorHeight", (float)e.NewValue);
    }

    private void PhosphorGapSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PhosphorGapValue != null)
            PhosphorGapValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.TV_PhosphorGap = (float)e.NewValue;
        _effect.Configuration.Set("tv_phosphorGap", (float)e.NewValue);
    }

    private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BrightnessValue != null)
            BrightnessValue.Text = $"{e.NewValue:F2}x";

        if (_effect == null || _isLoading) return;

        _effect.TV_Brightness = (float)e.NewValue;
        _effect.Configuration.Set("tv_brightness", (float)e.NewValue);
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
