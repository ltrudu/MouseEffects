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
            MaxSegmentsSlider.Value = _effect.MaxSegments;
            TraceCountSlider.Value = _effect.TraceCount;
            GrowthSpeedSlider.Value = _effect.GrowthSpeed;
            MaxLengthSlider.Value = _effect.MaxLength;
            BranchProbSlider.Value = _effect.BranchProbability;
            NodeSizeSlider.Value = _effect.NodeSize;
            GlowSlider.Value = _effect.GlowIntensity;
            GlowAnimationCheckBox.IsChecked = _effect.GlowAnimationEnabled;
            GlowAnimSpeedSlider.Value = _effect.GlowAnimationSpeed;
            GlowMinSlider.Value = _effect.GlowMinIntensity;
            GlowMaxSlider.Value = _effect.GlowMaxIntensity;
            UpdateGlowAnimationVisibility();
            ThicknessSlider.Value = _effect.LineThickness;
            LifetimeSlider.Value = _effect.TraceLifetime;
            ThresholdSlider.Value = _effect.SpawnThreshold;
            ColorPresetCombo.SelectedIndex = _effect.ColorPreset;

            CustomRSlider.Value = _effect.CustomColor.X;
            CustomGSlider.Value = _effect.CustomColor.Y;
            CustomBSlider.Value = _effect.CustomColor.Z;

            RainbowCheckBox.IsChecked = _effect.RainbowEnabled;
            RainbowSpeedSlider.Value = _effect.RainbowSpeed;

            UpdateCustomColorVisibility();
            UpdateRainbowVisibility();
            UpdateColorPreview();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void MaxSegmentsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxSegments = (int)MaxSegmentsSlider.Value;
        _effect.Configuration.Set("cir_maxSegments", _effect.MaxSegments);
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

    private void GlowAnimationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowAnimationEnabled = GlowAnimationCheckBox.IsChecked == true;
        _effect.Configuration.Set("cir_glowAnimationEnabled", _effect.GlowAnimationEnabled);
        UpdateGlowAnimationVisibility();
    }

    private void GlowAnimSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowAnimationSpeed = (float)GlowAnimSpeedSlider.Value;
        _effect.Configuration.Set("cir_glowAnimationSpeed", _effect.GlowAnimationSpeed);
    }

    private void GlowMinSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        // Ensure min < max
        if (GlowMinSlider.Value >= GlowMaxSlider.Value - 0.1)
        {
            GlowMinSlider.Value = GlowMaxSlider.Value - 0.1;
            return;
        }

        _effect.GlowMinIntensity = (float)GlowMinSlider.Value;
        _effect.Configuration.Set("cir_glowMinIntensity", _effect.GlowMinIntensity);
    }

    private void GlowMaxSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        // Ensure max > min
        if (GlowMaxSlider.Value <= GlowMinSlider.Value + 0.1)
        {
            GlowMaxSlider.Value = GlowMinSlider.Value + 0.1;
            return;
        }

        _effect.GlowMaxIntensity = (float)GlowMaxSlider.Value;
        _effect.Configuration.Set("cir_glowMaxIntensity", _effect.GlowMaxIntensity);
    }

    private void UpdateGlowAnimationVisibility()
    {
        GlowAnimationPanel.Visibility = GlowAnimationCheckBox.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
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

    private void RainbowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowEnabled = RainbowCheckBox.IsChecked == true;
        _effect.Configuration.Set("cir_rainbowEnabled", _effect.RainbowEnabled);

        UpdateRainbowVisibility();
    }

    private void RainbowSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("cir_rainbowSpeed", _effect.RainbowSpeed);
    }

    private void UpdateCustomColorVisibility()
    {
        CustomColorPanel.Visibility = ColorPresetCombo.SelectedIndex == 5
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateRainbowVisibility()
    {
        bool isRainbow = RainbowCheckBox.IsChecked == true;
        RainbowSpeedPanel.Visibility = isRainbow ? Visibility.Visible : Visibility.Collapsed;
        StaticColorPanel.Visibility = isRainbow ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateColorPreview()
    {
        byte r = (byte)(CustomRSlider.Value * 255);
        byte g = (byte)(CustomGSlider.Value * 255);
        byte b = (byte)(CustomBSlider.Value * 255);

        ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
    }
}
