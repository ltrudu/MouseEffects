using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Kaleidoscope.UI;

public partial class KaleidoscopeSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public KaleidoscopeSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;
        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        if (_effect.Configuration.TryGet("radius", out float radius))
        {
            RadiusSlider.Value = radius;
            RadiusValue.Text = radius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("segments", out int segments))
        {
            SegmentsSlider.Value = segments;
            SegmentsValue.Text = segments.ToString();
        }

        if (_effect.Configuration.TryGet("rotationSpeed", out float rotationSpeed))
        {
            RotationSpeedSlider.Value = rotationSpeed;
            RotationSpeedValue.Text = rotationSpeed.ToString("F1");
        }

        if (_effect.Configuration.TryGet("rotationOffset", out float rotationOffset))
        {
            RotationOffsetSlider.Value = rotationOffset;
            RotationOffsetValue.Text = rotationOffset.ToString("F2");
        }

        if (_effect.Configuration.TryGet("edgeSoftness", out float edgeSoftness))
        {
            EdgeSoftnessSlider.Value = edgeSoftness;
            EdgeSoftnessValue.Text = edgeSoftness.ToString("F2");
        }

        if (_effect.Configuration.TryGet("zoomFactor", out float zoomFactor))
        {
            ZoomSlider.Value = zoomFactor;
            ZoomValue.Text = zoomFactor.ToString("F1");
        }

        if (_effect.Configuration.TryGet("alpha", out float alpha))
        {
            AlphaSlider.Value = alpha;
            AlphaValue.Text = alpha.ToString("F2");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();
        config.Set("radius", (float)RadiusSlider.Value);
        config.Set("segments", (int)SegmentsSlider.Value);
        config.Set("rotationSpeed", (float)RotationSpeedSlider.Value);
        config.Set("rotationOffset", (float)RotationOffsetSlider.Value);
        config.Set("edgeSoftness", (float)EdgeSoftnessSlider.Value);
        config.Set("zoomFactor", (float)ZoomSlider.Value);
        config.Set("alpha", (float)AlphaSlider.Value);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RadiusValue != null)
            RadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SegmentsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SegmentsValue != null)
            SegmentsValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void RotationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RotationSpeedValue != null)
            RotationSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RotationOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RotationOffsetValue != null)
            RotationOffsetValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void EdgeSoftnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeSoftnessValue != null)
            EdgeSoftnessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomValue != null)
            ZoomValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void AlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AlphaValue != null)
            AlphaValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }
}
