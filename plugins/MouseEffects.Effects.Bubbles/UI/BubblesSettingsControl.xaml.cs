using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Bubbles.UI;

public partial class BubblesSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public BubblesSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Animation settings
        if (_effect.Configuration.TryGet("b_appearsAnimation", out int appearsAnim))
        {
            AppearsCombo.SelectedIndex = appearsAnim;
            UpdateAppearsPanelVisibility(appearsAnim);
        }

        if (_effect.Configuration.TryGet("b_disappearsAnimation", out int disappearsAnim))
        {
            DisappearsCombo.SelectedIndex = disappearsAnim;
            UpdateDisappearsPanelVisibility(disappearsAnim);
        }

        // Fade In settings
        if (_effect.Configuration.TryGet("b_fadeInSpeed", out float fadeInSpeed))
        {
            FadeInSpeedSlider.Value = fadeInSpeed;
            FadeInSpeedValue.Text = fadeInSpeed.ToString("F1");
        }
        if (_effect.Configuration.TryGet("b_fadeInStartAlpha", out float fadeInStartAlpha))
        {
            FadeInStartAlphaSlider.Value = fadeInStartAlpha;
            FadeInStartAlphaValue.Text = fadeInStartAlpha.ToString("F2");
        }
        if (_effect.Configuration.TryGet("b_fadeInEndAlpha", out float fadeInEndAlpha))
        {
            FadeInEndAlphaSlider.Value = fadeInEndAlpha;
            FadeInEndAlphaValue.Text = fadeInEndAlpha.ToString("F2");
        }

        // Zoom In settings
        if (_effect.Configuration.TryGet("b_zoomInSpeed", out float zoomInSpeed))
        {
            ZoomInSpeedSlider.Value = zoomInSpeed;
            ZoomInSpeedValue.Text = zoomInSpeed.ToString("F1");
        }
        if (_effect.Configuration.TryGet("b_zoomInStartScale", out float zoomInStartScale))
        {
            ZoomInStartScaleSlider.Value = zoomInStartScale;
            ZoomInStartScaleValue.Text = zoomInStartScale.ToString("F2");
        }
        if (_effect.Configuration.TryGet("b_zoomInEndScale", out float zoomInEndScale))
        {
            ZoomInEndScaleSlider.Value = zoomInEndScale;
            ZoomInEndScaleValue.Text = zoomInEndScale.ToString("F2");
        }

        // Fade Out settings
        if (_effect.Configuration.TryGet("b_fadeOutSpeed", out float fadeOutSpeed))
        {
            FadeOutSpeedSlider.Value = fadeOutSpeed;
            FadeOutSpeedValue.Text = fadeOutSpeed.ToString("F1");
        }
        if (_effect.Configuration.TryGet("b_fadeOutStartAlpha", out float fadeOutStartAlpha))
        {
            FadeOutStartAlphaSlider.Value = fadeOutStartAlpha;
            FadeOutStartAlphaValue.Text = fadeOutStartAlpha.ToString("F2");
        }
        if (_effect.Configuration.TryGet("b_fadeOutEndAlpha", out float fadeOutEndAlpha))
        {
            FadeOutEndAlphaSlider.Value = fadeOutEndAlpha;
            FadeOutEndAlphaValue.Text = fadeOutEndAlpha.ToString("F2");
        }

        // Zoom Out settings
        if (_effect.Configuration.TryGet("b_zoomOutSpeed", out float zoomOutSpeed))
        {
            ZoomOutSpeedSlider.Value = zoomOutSpeed;
            ZoomOutSpeedValue.Text = zoomOutSpeed.ToString("F1");
        }
        if (_effect.Configuration.TryGet("b_zoomOutStartScale", out float zoomOutStartScale))
        {
            ZoomOutStartScaleSlider.Value = zoomOutStartScale;
            ZoomOutStartScaleValue.Text = zoomOutStartScale.ToString("F2");
        }
        if (_effect.Configuration.TryGet("b_zoomOutEndScale", out float zoomOutEndScale))
        {
            ZoomOutEndScaleSlider.Value = zoomOutEndScale;
            ZoomOutEndScaleValue.Text = zoomOutEndScale.ToString("F2");
        }

        // Pop Out settings
        if (_effect.Configuration.TryGet("b_popDuration", out float popDur))
        {
            PopDurationSlider.Value = popDur;
            PopDurationValue.Text = popDur.ToString("F1");
        }

        // Bubble settings
        if (_effect.Configuration.TryGet("b_maxBubbles", out int maxBubbles))
        {
            MaxBubblesSlider.Value = maxBubbles;
            MaxBubblesValue.Text = maxBubbles.ToString();
        }

        if (_effect.Configuration.TryGet("b_bubbleCount", out int count))
        {
            BubbleCountSlider.Value = count;
            BubbleCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("b_minSize", out float minSize))
        {
            MinSizeSlider.Value = minSize;
            MinSizeValue.Text = minSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_maxSize", out float maxSize))
        {
            MaxSizeSlider.Value = maxSize;
            MaxSizeValue.Text = maxSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F0");
        }

        // Motion settings
        if (_effect.Configuration.TryGet("b_floatSpeed", out float floatSpeed))
        {
            FloatSpeedSlider.Value = floatSpeed;
            FloatSpeedValue.Text = floatSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_wobbleAmount", out float wobble))
        {
            WobbleAmountSlider.Value = wobble;
            WobbleAmountValue.Text = wobble.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_wobbleFrequency", out float freq))
        {
            WobbleFrequencySlider.Value = freq;
            WobbleFrequencyValue.Text = freq.ToString("F1");
        }

        if (_effect.Configuration.TryGet("b_driftSpeed", out float drift))
        {
            DriftSpeedSlider.Value = drift;
            DriftSpeedValue.Text = drift.ToString("F0");
        }

        // Visual effects
        if (_effect.Configuration.TryGet("b_iridescenceIntensity", out float iridInt))
        {
            IridescenceIntensitySlider.Value = iridInt;
            IridescenceIntensityValue.Text = iridInt.ToString("F1");
        }

        if (_effect.Configuration.TryGet("b_iridescenceSpeed", out float iridSpd))
        {
            IridescenceSpeedSlider.Value = iridSpd;
            IridescenceSpeedValue.Text = iridSpd.ToString("F1");
        }

        if (_effect.Configuration.TryGet("b_transparency", out float trans))
        {
            TransparencySlider.Value = trans;
            TransparencyValue.Text = trans.ToString("F2");
        }

        if (_effect.Configuration.TryGet("b_rimThickness", out float rim))
        {
            RimThicknessSlider.Value = rim;
            RimThicknessValue.Text = rim.ToString("F2");
        }

        // Diffraction settings
        if (_effect.Configuration.TryGet("b_diffractionEnabled", out bool diffractionEnabled))
        {
            DiffractionCheckBox.IsChecked = diffractionEnabled;
            DiffractionPanel.Visibility = diffractionEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_effect.Configuration.TryGet("b_diffractionStrength", out float diffractionStrength))
        {
            DiffractionStrengthSlider.Value = diffractionStrength;
            DiffractionStrengthValue.Text = diffractionStrength.ToString("F2");
        }
    }

    private void UpdateAppearsPanelVisibility(int selectedIndex)
    {
        FadeInPanel.Visibility = selectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        ZoomInPanel.Visibility = selectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateDisappearsPanelVisibility(int selectedIndex)
    {
        FadeOutPanel.Visibility = selectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        ZoomOutPanel.Visibility = selectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        PopOutPanel.Visibility = selectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Animation settings
        config.Set("b_appearsAnimation", AppearsCombo.SelectedIndex);
        config.Set("b_disappearsAnimation", DisappearsCombo.SelectedIndex);

        // Fade In settings
        config.Set("b_fadeInSpeed", (float)FadeInSpeedSlider.Value);
        config.Set("b_fadeInStartAlpha", (float)FadeInStartAlphaSlider.Value);
        config.Set("b_fadeInEndAlpha", (float)FadeInEndAlphaSlider.Value);

        // Zoom In settings
        config.Set("b_zoomInSpeed", (float)ZoomInSpeedSlider.Value);
        config.Set("b_zoomInStartScale", (float)ZoomInStartScaleSlider.Value);
        config.Set("b_zoomInEndScale", (float)ZoomInEndScaleSlider.Value);

        // Fade Out settings
        config.Set("b_fadeOutSpeed", (float)FadeOutSpeedSlider.Value);
        config.Set("b_fadeOutStartAlpha", (float)FadeOutStartAlphaSlider.Value);
        config.Set("b_fadeOutEndAlpha", (float)FadeOutEndAlphaSlider.Value);

        // Zoom Out settings
        config.Set("b_zoomOutSpeed", (float)ZoomOutSpeedSlider.Value);
        config.Set("b_zoomOutStartScale", (float)ZoomOutStartScaleSlider.Value);
        config.Set("b_zoomOutEndScale", (float)ZoomOutEndScaleSlider.Value);

        // Pop Out settings
        config.Set("b_popDuration", (float)PopDurationSlider.Value);

        // Bubble settings
        config.Set("b_maxBubbles", (int)MaxBubblesSlider.Value);
        config.Set("b_bubbleCount", (int)BubbleCountSlider.Value);
        config.Set("b_minSize", (float)MinSizeSlider.Value);
        config.Set("b_maxSize", (float)MaxSizeSlider.Value);
        config.Set("b_lifetime", (float)LifetimeSlider.Value);

        // Motion settings
        config.Set("b_floatSpeed", (float)FloatSpeedSlider.Value);
        config.Set("b_wobbleAmount", (float)WobbleAmountSlider.Value);
        config.Set("b_wobbleFrequency", (float)WobbleFrequencySlider.Value);
        config.Set("b_driftSpeed", (float)DriftSpeedSlider.Value);

        // Visual effects
        config.Set("b_iridescenceIntensity", (float)IridescenceIntensitySlider.Value);
        config.Set("b_iridescenceSpeed", (float)IridescenceSpeedSlider.Value);
        config.Set("b_transparency", (float)TransparencySlider.Value);
        config.Set("b_rimThickness", (float)RimThicknessSlider.Value);

        // Diffraction settings
        config.Set("b_diffractionEnabled", DiffractionCheckBox.IsChecked == true);
        config.Set("b_diffractionStrength", (float)DiffractionStrengthSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    // Animation dropdown handlers
    private void AppearsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FadeInPanel == null || ZoomInPanel == null) return;
        UpdateAppearsPanelVisibility(AppearsCombo.SelectedIndex);
        UpdateConfiguration();
    }

    private void DisappearsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FadeOutPanel == null || ZoomOutPanel == null || PopOutPanel == null) return;
        UpdateDisappearsPanelVisibility(DisappearsCombo.SelectedIndex);
        UpdateConfiguration();
    }

    // Fade In handlers
    private void FadeInSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeInSpeedValue != null)
            FadeInSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FadeInStartAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeInStartAlphaValue != null)
            FadeInStartAlphaValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void FadeInEndAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeInEndAlphaValue != null)
            FadeInEndAlphaValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    // Zoom In handlers
    private void ZoomInSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomInSpeedValue != null)
            ZoomInSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ZoomInStartScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomInStartScaleValue != null)
            ZoomInStartScaleValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ZoomInEndScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomInEndScaleValue != null)
            ZoomInEndScaleValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    // Fade Out handlers
    private void FadeOutSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeOutSpeedValue != null)
            FadeOutSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FadeOutStartAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeOutStartAlphaValue != null)
            FadeOutStartAlphaValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void FadeOutEndAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeOutEndAlphaValue != null)
            FadeOutEndAlphaValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    // Zoom Out handlers
    private void ZoomOutSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomOutSpeedValue != null)
            ZoomOutSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ZoomOutStartScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomOutStartScaleValue != null)
            ZoomOutStartScaleValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ZoomOutEndScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomOutEndScaleValue != null)
            ZoomOutEndScaleValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    // Pop Out handler
    private void PopDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PopDurationValue != null)
            PopDurationValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    // Bubble settings handlers
    private void BubbleCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BubbleCountValue != null)
            BubbleCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinSizeValue != null)
            MinSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxSizeValue != null)
            MaxSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    // Motion settings handlers
    private void FloatSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FloatSpeedValue != null)
            FloatSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WobbleAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WobbleAmountValue != null)
            WobbleAmountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WobbleFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WobbleFrequencyValue != null)
            WobbleFrequencyValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void DriftSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DriftSpeedValue != null)
            DriftSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    // Visual effects handlers
    private void IridescenceIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IridescenceIntensityValue != null)
            IridescenceIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void IridescenceSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IridescenceSpeedValue != null)
            IridescenceSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TransparencyValue != null)
            TransparencyValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void RimThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RimThicknessValue != null)
            RimThicknessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    // Max bubbles handler
    private void MaxBubblesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxBubblesValue != null)
            MaxBubblesValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    // Diffraction handlers
    private void DiffractionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (DiffractionPanel != null)
            DiffractionPanel.Visibility = DiffractionCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        UpdateConfiguration();
    }

    private void DiffractionStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DiffractionStrengthValue != null)
            DiffractionStrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }
}
