using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.MatrixRain.UI;

public partial class MatrixRainSettingsControl : System.Windows.Controls.UserControl
{
    private readonly MatrixRainEffect? _effect;
    private bool _isLoading = true;

    public MatrixRainSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is MatrixRainEffect matrixEffect)
        {
            _effect = matrixEffect;
            LoadConfiguration();
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        _isLoading = true;
        try
        {
            ColumnDensitySlider.Value = _effect.ColumnDensity;
            MinSpeedSlider.Value = _effect.MinFallSpeed;
            MaxSpeedSlider.Value = _effect.MaxFallSpeed;
            CharChangeSlider.Value = _effect.CharChangeRate;
            GlowSlider.Value = _effect.GlowIntensity;
            TrailSlider.Value = _effect.TrailLength;
            RadiusSlider.Value = _effect.EffectRadius;

            ColorRSlider.Value = _effect.Color.X;
            ColorGSlider.Value = _effect.Color.Y;
            ColorBSlider.Value = _effect.Color.Z;

            UpdateColorPreview();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ColumnDensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColumnDensity = (float)ColumnDensitySlider.Value;
        _effect.Configuration.Set("mr_columnDensity", _effect.ColumnDensity);
    }

    private void MinSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MinFallSpeed = (float)MinSpeedSlider.Value;
        _effect.Configuration.Set("mr_minFallSpeed", _effect.MinFallSpeed);
    }

    private void MaxSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxFallSpeed = (float)MaxSpeedSlider.Value;
        _effect.Configuration.Set("mr_maxFallSpeed", _effect.MaxFallSpeed);
    }

    private void CharChangeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.CharChangeRate = (float)CharChangeSlider.Value;
        _effect.Configuration.Set("mr_charChangeRate", _effect.CharChangeRate);
    }

    private void GlowSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowSlider.Value;
        _effect.Configuration.Set("mr_glowIntensity", _effect.GlowIntensity);
    }

    private void TrailSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.TrailLength = (float)TrailSlider.Value;
        _effect.Configuration.Set("mr_trailLength", _effect.TrailLength);
    }

    private void RadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EffectRadius = (float)RadiusSlider.Value;
        _effect.Configuration.Set("mr_effectRadius", _effect.EffectRadius);
    }

    private void ColorSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var color = new Vector4(
            (float)ColorRSlider.Value,
            (float)ColorGSlider.Value,
            (float)ColorBSlider.Value,
            1f
        );

        _effect.Color = color;
        _effect.Configuration.Set("mr_color", color);

        UpdateColorPreview();
    }

    private void UpdateColorPreview()
    {
        byte r = (byte)(ColorRSlider.Value * 255);
        byte g = (byte)(ColorGSlider.Value * 255);
        byte b = (byte)(ColorBSlider.Value * 255);

        ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
    }
}
