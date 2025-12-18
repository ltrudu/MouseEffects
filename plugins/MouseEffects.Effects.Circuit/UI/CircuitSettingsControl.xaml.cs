using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Circuit.UI;

public partial class CircuitSettingsControl : UserControl
{
    private readonly CircuitEffect? _effect;
    private bool _isLoading = true;

    public CircuitSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is CircuitEffect circuitEffect)
        {
            _effect = circuitEffect;
            LoadConfiguration();
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        _isLoading = true;
        try
        {
            TraceCountSlider.Value = _effect.TraceCount;
            GrowthSpeedSlider.Value = _effect.GrowthSpeed;
            MaxLengthSlider.Value = _effect.MaxLength;
            BranchProbSlider.Value = _effect.BranchProbability;
            NodeSizeSlider.Value = _effect.NodeSize;
            GlowSlider.Value = _effect.GlowIntensity;
            ThicknessSlider.Value = _effect.LineThickness;
            LifetimeSlider.Value = _effect.TraceLifetime;
            ThresholdSlider.Value = _effect.SpawnThreshold;
            ColorPresetCombo.SelectedIndex = _effect.ColorPreset;

            CustomRSlider.Value = _effect.CustomColor.X;
            CustomGSlider.Value = _effect.CustomColor.Y;
            CustomBSlider.Value = _effect.CustomColor.Z;

            UpdateCustomColorVisibility();
            UpdateColorPreview();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void TraceCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.TraceCount = (int)TraceCountSlider.Value;
        _effect.Configuration.Set("cir_traceCount", _effect.TraceCount);
    }

    private void GrowthSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GrowthSpeed = (float)GrowthSpeedSlider.Value;
        _effect.Configuration.Set("cir_growthSpeed", _effect.GrowthSpeed);
    }

    private void MaxLengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxLength = (float)MaxLengthSlider.Value;
        _effect.Configuration.Set("cir_maxLength", _effect.MaxLength);
    }

    private void BranchProbSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.BranchProbability = (float)BranchProbSlider.Value;
        _effect.Configuration.Set("cir_branchProbability", _effect.BranchProbability);
    }

    private void NodeSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.NodeSize = (float)NodeSizeSlider.Value;
        _effect.Configuration.Set("cir_nodeSize", _effect.NodeSize);
    }

    private void GlowSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowSlider.Value;
        _effect.Configuration.Set("cir_glowIntensity", _effect.GlowIntensity);
    }

    private void ThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LineThickness = (float)ThicknessSlider.Value;
        _effect.Configuration.Set("cir_lineThickness", _effect.LineThickness);
    }

    private void LifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.TraceLifetime = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("cir_traceLifetime", _effect.TraceLifetime);
    }

    private void ThresholdSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpawnThreshold = (float)ThresholdSlider.Value;
        _effect.Configuration.Set("cir_spawnThreshold", _effect.SpawnThreshold);
    }

    private void ColorPresetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorPreset = ColorPresetCombo.SelectedIndex;
        _effect.Configuration.Set("cir_colorPreset", _effect.ColorPreset);

        UpdateCustomColorVisibility();
    }

    private void CustomColorSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        var color = new Vector4(
            (float)CustomRSlider.Value,
            (float)CustomGSlider.Value,
            (float)CustomBSlider.Value,
            1f
        );

        _effect.CustomColor = color;
        _effect.Configuration.Set("cir_customColor", color);

        UpdateColorPreview();
    }

    private void UpdateCustomColorVisibility()
    {
        CustomColorPanel.Visibility = ColorPresetCombo.SelectedIndex == 5
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateColorPreview()
    {
        byte r = (byte)(CustomRSlider.Value * 255);
        byte g = (byte)(CustomGSlider.Value * 255);
        byte b = (byte)(CustomBSlider.Value * 255);

        ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
    }
}
