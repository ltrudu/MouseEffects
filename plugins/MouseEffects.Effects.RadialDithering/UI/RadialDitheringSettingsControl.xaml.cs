using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.RadialDithering.UI;

public partial class RadialDitheringSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private bool _isExpanded;
    private Vector4 _color1 = new(1.0f, 1.0f, 1.0f, 1.0f);
    private Vector4 _color2 = new(0.0f, 0.0f, 0.0f, 1.0f);
    private Vector4 _glowColor = new(0.3f, 0.5f, 1.0f, 1.0f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public RadialDitheringSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("radius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("intensity", out float intensity))
        {
            IntensitySlider.Value = intensity;
            IntensityValue.Text = intensity.ToString("F2");
        }

        if (_effect.Configuration.TryGet("alpha", out float alpha))
        {
            AlphaSlider.Value = alpha;
            AlphaValue.Text = alpha.ToString("F2");
        }

        if (_effect.Configuration.TryGet("patternScale", out float patternScale))
        {
            PatternScaleSlider.Value = patternScale;
            PatternScaleValue.Text = patternScale.ToString("F0");
        }

        if (_effect.Configuration.TryGet("threshold", out float threshold))
        {
            ThresholdSlider.Value = threshold;
            ThresholdValue.Text = threshold.ToString("F2");
        }

        if (_effect.Configuration.TryGet("edgeSoftness", out float edgeSoftness))
        {
            EdgeSoftnessSlider.Value = edgeSoftness;
            EdgeSoftnessValue.Text = edgeSoftness.ToString("F2");
        }

        if (_effect.Configuration.TryGet("falloffType", out int falloffType))
        {
            FalloffTypeCombo.SelectedIndex = falloffType;
        }

        if (_effect.Configuration.TryGet("ringWidth", out float ringWidth))
        {
            RingWidthSlider.Value = ringWidth;
            RingWidthValue.Text = ringWidth.ToString("F2");
        }

        if (_effect.Configuration.TryGet("color1", out Vector4 color1))
        {
            _color1 = color1;
            UpdateColor1Preview();
        }

        if (_effect.Configuration.TryGet("color2", out Vector4 color2))
        {
            _color2 = color2;
            UpdateColor2Preview();
        }

        if (_effect.Configuration.TryGet("colorBlendMode", out int blendMode))
        {
            BlendModeCombo.SelectedIndex = blendMode;
        }

        if (_effect.Configuration.TryGet("invertPattern", out bool invertPattern))
        {
            InvertPatternCheckBox.IsChecked = invertPattern;
        }

        if (_effect.Configuration.TryGet("enableAnimation", out bool enableAnimation))
        {
            EnableAnimationCheckBox.IsChecked = enableAnimation;
        }

        if (_effect.Configuration.TryGet("animationSpeed", out float animSpeed))
        {
            AnimationSpeedSlider.Value = animSpeed;
            AnimationSpeedValue.Text = animSpeed.ToString("F1");
        }

        if (_effect.Configuration.TryGet("enableNoise", out bool enableNoise))
        {
            EnableNoiseCheckBox.IsChecked = enableNoise;
        }

        if (_effect.Configuration.TryGet("noiseAmount", out float noiseAmount))
        {
            NoiseAmountSlider.Value = noiseAmount;
            NoiseAmountValue.Text = noiseAmount.ToString("F2");
        }

        if (_effect.Configuration.TryGet("enableGlow", out bool enableGlow))
        {
            EnableGlowCheckBox.IsChecked = enableGlow;
        }

        if (_effect.Configuration.TryGet("glowIntensity", out float glowIntensity))
        {
            GlowIntensitySlider.Value = glowIntensity;
            GlowIntensityValue.Text = glowIntensity.ToString("F2");
        }

        if (_effect.Configuration.TryGet("glowColor", out Vector4 glowColor))
        {
            _glowColor = glowColor;
            UpdateGlowColorPreview();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("intensity", (float)IntensitySlider.Value);
        config.Set("alpha", (float)AlphaSlider.Value);
        config.Set("patternScale", (float)PatternScaleSlider.Value);
        config.Set("threshold", (float)ThresholdSlider.Value);
        config.Set("edgeSoftness", (float)EdgeSoftnessSlider.Value);
        config.Set("falloffType", FalloffTypeCombo.SelectedIndex);
        config.Set("ringWidth", (float)RingWidthSlider.Value);
        config.Set("color1", _color1);
        config.Set("color2", _color2);
        config.Set("colorBlendMode", BlendModeCombo.SelectedIndex);
        config.Set("invertPattern", InvertPatternCheckBox.IsChecked ?? false);
        config.Set("enableAnimation", EnableAnimationCheckBox.IsChecked ?? false);
        config.Set("animationSpeed", (float)AnimationSpeedSlider.Value);
        config.Set("enableNoise", EnableNoiseCheckBox.IsChecked ?? false);
        config.Set("noiseAmount", (float)NoiseAmountSlider.Value);
        config.Set("enableGlow", EnableGlowCheckBox.IsChecked ?? false);
        config.Set("glowIntensity", (float)GlowIntensitySlider.Value);
        config.Set("glowColor", _glowColor);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateColor1Preview()
    {
        Color1Preview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_color1.W * 255),
            (byte)(_color1.X * 255),
            (byte)(_color1.Y * 255),
            (byte)(_color1.Z * 255)));
    }

    private void UpdateColor2Preview()
    {
        Color2Preview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_color2.W * 255),
            (byte)(_color2.X * 255),
            (byte)(_color2.Y * 255),
            (byte)(_color2.Z * 255)));
    }

    private void UpdateGlowColorPreview()
    {
        GlowColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_glowColor.W * 255),
            (byte)(_glowColor.X * 255),
            (byte)(_glowColor.Y * 255),
            (byte)(_glowColor.Z * 255)));
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntensityValue != null)
            IntensityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void AlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AlphaValue != null)
            AlphaValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void PatternScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PatternScaleValue != null)
            PatternScaleValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThresholdValue != null)
            ThresholdValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void EdgeSoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeSoftnessValue != null)
            EdgeSoftnessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void FalloffTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RingWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RingWidthValue != null)
            RingWidthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void Color1PickerButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_color1.W * 255),
            (int)(_color1.X * 255),
            (int)(_color1.Y * 255),
            (int)(_color1.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _color1 = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);

            UpdateColor1Preview();
            UpdateConfiguration();
        }
    }

    private void Color2PickerButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_color2.W * 255),
            (int)(_color2.X * 255),
            (int)(_color2.Y * 255),
            (int)(_color2.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _color2 = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);

            UpdateColor2Preview();
            UpdateConfiguration();
        }
    }

    private void BlendModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void InvertPatternCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void EnableAnimationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void AnimationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AnimationSpeedValue != null)
            AnimationSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void EnableNoiseCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void NoiseAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (NoiseAmountValue != null)
            NoiseAmountValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void EnableGlowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void GlowColorPickerButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_glowColor.W * 255),
            (int)(_glowColor.X * 255),
            (int)(_glowColor.Y * 255),
            (int)(_glowColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _glowColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);

            UpdateGlowColorPreview();
            UpdateConfiguration();
        }
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
