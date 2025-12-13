using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.Retro.UI.Filters;

/// <summary>
/// Shared pre-filter settings control for all Retro filter types.
/// Controls scaling mode applied before the main filter renders.
/// Uses the dual-update pattern: directly modify effect properties for real-time
/// shader updates, and also update Configuration for JSON persistence.
///
/// Scaling Modes:
/// - 0: Enhancement (native resolution with edge smoothing)
/// - 1: Pixelate + Scale (pixelate then apply filter)
/// - 2: Downscale + Upscale (reduce resolution then upscale with filter)
/// </summary>
public partial class PreFilterSettings : UserControl
{
    private RetroEffect? _effect;
    private bool _isLoading;

    public PreFilterSettings()
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
            // Scaling Mode
            ScalingModeCombo.SelectedIndex = _effect.XS_Mode;
            UpdateScalingPanelVisibility();

            // Pixel Size (mode 1)
            PixelSizeSlider.Value = _effect.XS_PixelSize;
            PixelSizeValue.Text = $"{(int)_effect.XS_PixelSize} px";

            // Scale Factor (mode 2)
            int scaleFactor = _effect.XS_ScaleFactor;
            for (int i = 0; i < ScaleFactorCombo.Items.Count; i++)
            {
                if (ScaleFactorCombo.Items[i] is ComboBoxItem item && item.Tag?.ToString() == scaleFactor.ToString())
                {
                    ScaleFactorCombo.SelectedIndex = i;
                    break;
                }
            }

            // Strength
            StrengthSlider.Value = _effect.XS_Strength;
            StrengthValue.Text = $"{(int)(_effect.XS_Strength * 100)}%";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateScalingPanelVisibility()
    {
        int mode = ScalingModeCombo.SelectedIndex;
        // Mode 0: Enhancement - show neither
        // Mode 1: Pixelate + Scale - show PixelSize
        // Mode 2: Downscale + Upscale - show ScaleFactor
        PixelSizePanel.Visibility = mode == 1 ? Visibility.Visible : Visibility.Collapsed;
        ScaleFactorPanel.Visibility = mode == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    #region Event Handlers

    private void ScalingModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int mode = ScalingModeCombo.SelectedIndex;
        _effect.XS_Mode = mode;
        _effect.Configuration.Set("xs_mode", mode);

        UpdateScalingPanelVisibility();
    }

    private void PixelSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PixelSizeValue != null)
            PixelSizeValue.Text = $"{(int)e.NewValue} px";

        if (_effect == null || _isLoading) return;

        _effect.XS_PixelSize = (float)e.NewValue;
        _effect.Configuration.Set("xs_pixelSize", (float)e.NewValue);
    }

    private void ScaleFactorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        if (ScaleFactorCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            int scaleFactor = int.Parse(item.Tag.ToString()!);
            _effect.XS_ScaleFactor = scaleFactor;
            _effect.Configuration.Set("xs_scaleFactor", scaleFactor);
        }
    }

    private void StrengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (StrengthValue != null)
            StrengthValue.Text = $"{(int)(e.NewValue * 100)}%";

        if (_effect == null || _isLoading) return;

        _effect.XS_Strength = (float)e.NewValue;
        _effect.Configuration.Set("xs_strength", (float)e.NewValue);
    }

    #endregion
}
