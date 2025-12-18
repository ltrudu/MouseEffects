using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.ParticleTrail.UI;

public partial class ParticleTrailSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public ParticleTrailSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        // Load current values from effect configuration
        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        if (_effect.Configuration.TryGet("emissionRate", out float emissionRate))
        {
            EmissionRateSlider.Value = emissionRate;
            EmissionRateValue.Text = emissionRate.ToString("F0");
        }

        if (_effect.Configuration.TryGet("particleLifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F1");
        }

        if (_effect.Configuration.TryGet("particleSize", out float size))
        {
            SizeSlider.Value = size;
            SizeValue.Text = size.ToString("F0");
        }

        if (_effect.Configuration.TryGet("spreadAngle", out float spread))
        {
            SpreadSlider.Value = spread;
            SpreadValue.Text = spread.ToString("F2");
        }

        if (_effect.Configuration.TryGet("initialSpeed", out float speed))
        {
            SpeedSlider.Value = speed;
            SpeedValue.Text = speed.ToString("F0");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("emissionRate", (float)EmissionRateSlider.Value);
        config.Set("particleLifetime", (float)LifetimeSlider.Value);
        config.Set("particleSize", (float)SizeSlider.Value);
        config.Set("spreadAngle", (float)SpreadSlider.Value);
        config.Set("initialSpeed", (float)SpeedSlider.Value);

        // Preserve color settings
        if (_effect.Configuration.TryGet("startColor", out System.Numerics.Vector4 startColor))
            config.Set("startColor", startColor);
        if (_effect.Configuration.TryGet("endColor", out System.Numerics.Vector4 endColor))
            config.Set("endColor", endColor);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EmissionRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EmissionRateValue != null)
            EmissionRateValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SizeValue != null)
            SizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SpreadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpreadValue != null)
            SpreadValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpeedValue != null)
            SpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }
}
