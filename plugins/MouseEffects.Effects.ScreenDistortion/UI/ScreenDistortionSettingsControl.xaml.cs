using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.ScreenDistortion.UI;

public partial class ScreenDistortionSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private bool _isExpanded;
    private Vector4 _glowColor = new(0.3f, 0.5f, 1.0f, 1.0f);
    private Vector4 _wireframeColor = new(0.0f, 1.0f, 0.5f, 0.8f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public ScreenDistortionSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("distortionRadius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("distortionStrength", out float strength))
        {
            StrengthSlider.Value = strength;
            StrengthValue.Text = strength.ToString("F2");
        }

        if (_effect.Configuration.TryGet("rippleFrequency", out float frequency))
        {
            FrequencySlider.Value = frequency;
            FrequencyValue.Text = frequency.ToString("F0");
        }

        if (_effect.Configuration.TryGet("rippleSpeed", out float speed))
        {
            SpeedSlider.Value = speed;
            SpeedValue.Text = speed.ToString("F1");
        }

        if (_effect.Configuration.TryGet("waveHeight", out float waveHeight))
        {
            WaveHeightSlider.Value = waveHeight;
            WaveHeightValue.Text = waveHeight.ToString("F2");
        }

        if (_effect.Configuration.TryGet("waveWidth", out float waveWidth))
        {
            WaveWidthSlider.Value = waveWidth;
            WaveWidthValue.Text = waveWidth.ToString("F1");
        }

        if (_effect.Configuration.TryGet("enableChromatic", out bool chromatic))
        {
            ChromaticCheckBox.IsChecked = chromatic;
        }

        if (_effect.Configuration.TryGet("enableGlow", out bool glow))
        {
            GlowCheckBox.IsChecked = glow;
        }

        if (_effect.Configuration.TryGet("glowIntensity", out float intensity))
        {
            GlowIntensitySlider.Value = intensity;
            GlowIntensityValue.Text = intensity.ToString("F2");
        }

        if (_effect.Configuration.TryGet("glowColor", out Vector4 color))
        {
            _glowColor = color;
            UpdateGlowColorPreview();
        }

        // Wireframe settings
        if (_effect.Configuration.TryGet("enableWireframe", out bool wireframe))
        {
            WireframeCheckBox.IsChecked = wireframe;
        }

        if (_effect.Configuration.TryGet("wireframeSpacing", out float spacing))
        {
            WireframeSpacingSlider.Value = spacing;
            WireframeSpacingValue.Text = spacing.ToString("F0");
        }

        if (_effect.Configuration.TryGet("wireframeThickness", out float thickness))
        {
            WireframeThicknessSlider.Value = thickness;
            WireframeThicknessValue.Text = thickness.ToString("F1");
        }

        if (_effect.Configuration.TryGet("wireframeColor", out Vector4 wireColor))
        {
            _wireframeColor = wireColor;
            UpdateWireframeColorPreview();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("distortionRadius", (float)RadiusSlider.Value);
        config.Set("distortionStrength", (float)StrengthSlider.Value);
        config.Set("rippleFrequency", (float)FrequencySlider.Value);
        config.Set("rippleSpeed", (float)SpeedSlider.Value);
        config.Set("waveHeight", (float)WaveHeightSlider.Value);
        config.Set("waveWidth", (float)WaveWidthSlider.Value);
        config.Set("enableChromatic", ChromaticCheckBox.IsChecked ?? true);
        config.Set("enableGlow", GlowCheckBox.IsChecked ?? true);
        config.Set("glowIntensity", (float)GlowIntensitySlider.Value);
        config.Set("glowColor", _glowColor);
        config.Set("enableWireframe", WireframeCheckBox.IsChecked ?? false);
        config.Set("wireframeSpacing", (float)WireframeSpacingSlider.Value);
        config.Set("wireframeThickness", (float)WireframeThicknessSlider.Value);
        config.Set("wireframeColor", _wireframeColor);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateGlowColorPreview()
    {
        ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_glowColor.W * 255),
            (byte)(_glowColor.X * 255),
            (byte)(_glowColor.Y * 255),
            (byte)(_glowColor.Z * 255)));
    }

    private void UpdateWireframeColorPreview()
    {
        WireframeColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)(_wireframeColor.W * 255),
            (byte)(_wireframeColor.X * 255),
            (byte)(_wireframeColor.Y * 255),
            (byte)(_wireframeColor.Z * 255)));
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void StrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (StrengthValue != null)
            StrengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FrequencyValue != null)
            FrequencyValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpeedValue != null)
            SpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void WaveHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WaveHeightValue != null)
            WaveHeightValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void WaveWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WaveWidthValue != null)
            WaveWidthValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ChromaticCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void GlowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
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

    private void WireframeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void WireframeSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WireframeSpacingValue != null)
            WireframeSpacingValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WireframeThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WireframeThicknessValue != null)
            WireframeThicknessValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void WireframeColorPickerButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(
            (int)(_wireframeColor.W * 255),
            (int)(_wireframeColor.X * 255),
            (int)(_wireframeColor.Y * 255),
            (int)(_wireframeColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _wireframeColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                0.8f); // Keep some transparency

            UpdateWireframeColorPreview();
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
