using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.ASCIIZer.UI.Filters;

/// <summary>
/// Shared post-effects settings control for all ASCIIZer filter types.
/// Uses the dual-update pattern: directly modify effect properties for real-time
/// shader updates, and also update Configuration for JSON persistence.
/// </summary>
public partial class PostEffectsSettings : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;

    public PostEffectsSettings()
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
            // Scanlines
            ScanlinesCheckBox.IsChecked = _effect.Scanlines;
            ScanlinesSettingsPanel.Visibility = _effect.Scanlines ? Visibility.Visible : Visibility.Collapsed;
            ScanlineIntensitySlider.Value = _effect.ScanlineIntensity;
            ScanlineIntensityValue.Text = $"{_effect.ScanlineIntensity:F2}";
            ScanlineSpacingSlider.Value = _effect.ScanlineSpacing;
            ScanlineSpacingValue.Text = $"{_effect.ScanlineSpacing} px";

            // CRT Curvature
            CrtCurvatureCheckBox.IsChecked = _effect.CrtCurvature;
            CrtSettingsPanel.Visibility = _effect.CrtCurvature ? Visibility.Visible : Visibility.Collapsed;
            CrtAmountSlider.Value = _effect.CrtAmount;
            CrtAmountValue.Text = $"{_effect.CrtAmount:F2}";

            // Phosphor Glow
            PhosphorGlowCheckBox.IsChecked = _effect.PhosphorGlow;
            PhosphorGlowSettingsPanel.Visibility = _effect.PhosphorGlow ? Visibility.Visible : Visibility.Collapsed;
            PhosphorIntensitySlider.Value = _effect.PhosphorIntensity;
            PhosphorIntensityValue.Text = $"{_effect.PhosphorIntensity:F2}";

            // Vignette
            VignetteCheckBox.IsChecked = _effect.Vignette;
            VignetteSettingsPanel.Visibility = _effect.Vignette ? Visibility.Visible : Visibility.Collapsed;
            VignetteIntensitySlider.Value = _effect.VignetteIntensity;
            VignetteIntensityValue.Text = $"{_effect.VignetteIntensity:F2}";
            VignetteRadiusSlider.Value = _effect.VignetteRadius;
            VignetteRadiusValue.Text = $"{_effect.VignetteRadius:F2}";

            // Chromatic Aberration
            ChromaticCheckBox.IsChecked = _effect.Chromatic;
            ChromaticSettingsPanel.Visibility = _effect.Chromatic ? Visibility.Visible : Visibility.Collapsed;
            ChromaticOffsetSlider.Value = _effect.ChromaticOffset;
            ChromaticOffsetValue.Text = $"{_effect.ChromaticOffset:F1} px";

            // Noise
            NoiseCheckBox.IsChecked = _effect.Noise;
            NoiseSettingsPanel.Visibility = _effect.Noise ? Visibility.Visible : Visibility.Collapsed;
            NoiseAmountSlider.Value = _effect.NoiseAmount;
            NoiseAmountValue.Text = $"{_effect.NoiseAmount:F2}";

            // Flicker
            FlickerCheckBox.IsChecked = _effect.Flicker;
            FlickerSettingsPanel.Visibility = _effect.Flicker ? Visibility.Visible : Visibility.Collapsed;
            FlickerSpeedSlider.Value = _effect.FlickerSpeed;
            FlickerSpeedValue.Text = $"{_effect.FlickerSpeed:F1}x";
        }
        finally
        {
            _isLoading = false;
        }
    }

    #region Event Handlers - Scanlines

    private void ScanlinesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = ScanlinesCheckBox.IsChecked == true;
        ScanlinesSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_effect == null) return;

        _effect.Scanlines = enabled;
        _effect.Configuration.Set("scanlines", enabled);
    }

    private void ScanlineIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScanlineIntensityValue != null)
            ScanlineIntensityValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.ScanlineIntensity = (float)e.NewValue;
        _effect.Configuration.Set("scanlineIntensity", (float)e.NewValue);
    }

    private void ScanlineSpacingSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScanlineSpacingValue != null)
            ScanlineSpacingValue.Text = $"{(int)e.NewValue} px";

        if (_effect == null || _isLoading) return;

        _effect.ScanlineSpacing = (int)e.NewValue;
        _effect.Configuration.Set("scanlineSpacing", (int)e.NewValue);
    }

    #endregion

    #region Event Handlers - CRT Curvature

    private void CrtCurvatureCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = CrtCurvatureCheckBox.IsChecked == true;
        CrtSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_effect == null) return;

        _effect.CrtCurvature = enabled;
        _effect.Configuration.Set("crtCurvature", enabled);
    }

    private void CrtAmountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CrtAmountValue != null)
            CrtAmountValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.CrtAmount = (float)e.NewValue;
        _effect.Configuration.Set("crtAmount", (float)e.NewValue);
    }

    #endregion

    #region Event Handlers - Phosphor Glow

    private void PhosphorGlowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = PhosphorGlowCheckBox.IsChecked == true;
        PhosphorGlowSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_effect == null) return;

        _effect.PhosphorGlow = enabled;
        _effect.Configuration.Set("phosphorGlow", enabled);
    }

    private void PhosphorIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PhosphorIntensityValue != null)
            PhosphorIntensityValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.PhosphorIntensity = (float)e.NewValue;
        _effect.Configuration.Set("phosphorIntensity", (float)e.NewValue);
    }

    #endregion

    #region Event Handlers - Vignette

    private void VignetteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = VignetteCheckBox.IsChecked == true;
        VignetteSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_effect == null) return;

        _effect.Vignette = enabled;
        _effect.Configuration.Set("vignette", enabled);
    }

    private void VignetteIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VignetteIntensityValue != null)
            VignetteIntensityValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.VignetteIntensity = (float)e.NewValue;
        _effect.Configuration.Set("vignetteIntensity", (float)e.NewValue);
    }

    private void VignetteRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VignetteRadiusValue != null)
            VignetteRadiusValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.VignetteRadius = (float)e.NewValue;
        _effect.Configuration.Set("vignetteRadius", (float)e.NewValue);
    }

    #endregion

    #region Event Handlers - Chromatic Aberration

    private void ChromaticCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = ChromaticCheckBox.IsChecked == true;
        ChromaticSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_effect == null) return;

        _effect.Chromatic = enabled;
        _effect.Configuration.Set("chromatic", enabled);
    }

    private void ChromaticOffsetSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ChromaticOffsetValue != null)
            ChromaticOffsetValue.Text = $"{e.NewValue:F1} px";

        if (_effect == null || _isLoading) return;

        _effect.ChromaticOffset = (float)e.NewValue;
        _effect.Configuration.Set("chromaticOffset", (float)e.NewValue);
    }

    #endregion

    #region Event Handlers - Noise

    private void NoiseCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = NoiseCheckBox.IsChecked == true;
        NoiseSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_effect == null) return;

        _effect.Noise = enabled;
        _effect.Configuration.Set("noise", enabled);
    }

    private void NoiseAmountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (NoiseAmountValue != null)
            NoiseAmountValue.Text = $"{e.NewValue:F2}";

        if (_effect == null || _isLoading) return;

        _effect.NoiseAmount = (float)e.NewValue;
        _effect.Configuration.Set("noiseAmount", (float)e.NewValue);
    }

    #endregion

    #region Event Handlers - Flicker

    private void FlickerCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        bool enabled = FlickerCheckBox.IsChecked == true;
        FlickerSettingsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_effect == null) return;

        _effect.Flicker = enabled;
        _effect.Configuration.Set("flicker", enabled);
    }

    private void FlickerSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FlickerSpeedValue != null)
            FlickerSpeedValue.Text = $"{e.NewValue:F1}x";

        if (_effect == null || _isLoading) return;

        _effect.FlickerSpeed = (float)e.NewValue;
        _effect.Configuration.Set("flickerSpeed", (float)e.NewValue);
    }

    #endregion
}
