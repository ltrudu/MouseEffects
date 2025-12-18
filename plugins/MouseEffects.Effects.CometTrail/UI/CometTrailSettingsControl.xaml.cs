using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.CometTrail.UI;

public partial class CometTrailSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private bool _isExpanded;

    public event Action<string>? SettingsChanged;

    public CometTrailSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        // Trail settings
        if (_effect.Configuration.TryGet("ct_maxTrailPoints", out int maxPoints))
        {
            MaxTrailLengthSlider.Value = maxPoints;
            MaxTrailLengthValue.Text = maxPoints.ToString();
        }

        if (_effect.Configuration.TryGet("ct_trailSpacing", out float spacing))
        {
            TrailSpacingSlider.Value = spacing;
            TrailSpacingValue.Text = spacing.ToString("F0");
        }

        // Comet appearance
        if (_effect.Configuration.TryGet("ct_headSize", out float headSize))
        {
            HeadSizeSlider.Value = headSize;
            HeadSizeValue.Text = headSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ct_trailWidth", out float width))
        {
            TrailWidthSlider.Value = width;
            TrailWidthValue.Text = width.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ct_glowIntensity", out float intensity))
        {
            GlowIntensitySlider.Value = intensity;
            GlowIntensityValue.Text = intensity.ToString("F1");
        }

        // Spark settings
        if (_effect.Configuration.TryGet("ct_sparkCount", out int sparkCount))
        {
            SparkCountSlider.Value = sparkCount;
            SparkCountValue.Text = sparkCount.ToString();
        }

        if (_effect.Configuration.TryGet("ct_sparkSize", out float sparkSize))
        {
            SparkSizeSlider.Value = sparkSize;
            SparkSizeValue.Text = sparkSize.ToString("F0");
        }

        // Color settings
        if (_effect.Configuration.TryGet("ct_colorTemperature", out float temp))
        {
            ColorTemperatureSlider.Value = temp;
            ColorTemperatureValue.Text = temp.ToString("F2");
        }

        // Animation settings
        if (_effect.Configuration.TryGet("ct_fadeSpeed", out float fadeSpd))
        {
            FadeSpeedSlider.Value = fadeSpd;
            FadeSpeedValue.Text = fadeSpd.ToString("F1");
        }

        if (_effect.Configuration.TryGet("ct_smoothingFactor", out float smooth))
        {
            SmoothingFactorSlider.Value = smooth;
            SmoothingFactorValue.Text = smooth.ToString("F2");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();

        // Trail settings
        config.Set("ct_maxTrailPoints", (int)MaxTrailLengthSlider.Value);
        config.Set("ct_trailSpacing", (float)TrailSpacingSlider.Value);

        // Comet appearance
        config.Set("ct_headSize", (float)HeadSizeSlider.Value);
        config.Set("ct_trailWidth", (float)TrailWidthSlider.Value);
        config.Set("ct_glowIntensity", (float)GlowIntensitySlider.Value);

        // Spark settings
        config.Set("ct_sparkCount", (int)SparkCountSlider.Value);
        config.Set("ct_sparkSize", (float)SparkSizeSlider.Value);

        // Color settings
        config.Set("ct_colorTemperature", (float)ColorTemperatureSlider.Value);

        // Animation settings
        config.Set("ct_fadeSpeed", (float)FadeSpeedSlider.Value);
        config.Set("ct_smoothingFactor", (float)SmoothingFactorSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void MaxTrailLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxTrailLengthValue != null)
            MaxTrailLengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void TrailSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrailSpacingValue != null)
            TrailSpacingValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void HeadSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HeadSizeValue != null)
            HeadSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void TrailWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrailWidthValue != null)
            TrailWidthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void SparkCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SparkCountValue != null)
            SparkCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SparkSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SparkSizeValue != null)
            SparkSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ColorTemperatureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ColorTemperatureValue != null)
            ColorTemperatureValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void FadeSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FadeSpeedValue != null)
            FadeSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void SmoothingFactorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SmoothingFactorValue != null)
            SmoothingFactorValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
