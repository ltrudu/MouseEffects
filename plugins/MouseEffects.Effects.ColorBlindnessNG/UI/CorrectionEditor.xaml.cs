using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Reusable correction settings editor control for each zone.
/// </summary>
public partial class CorrectionEditor : System.Windows.Controls.UserControl
{
    private bool _isLoading;
    private ZoneSettings? _zone;

    /// <summary>
    /// Event raised when any setting changes.
    /// </summary>
    public event EventHandler? SettingsChanged;

    public CorrectionEditor()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Bind this editor to a zone's settings.
    /// </summary>
    public void BindToZone(ZoneSettings zone)
    {
        _zone = zone;
        LoadFromZone();
    }

    /// <summary>
    /// Load UI values from the bound zone.
    /// </summary>
    public void LoadFromZone()
    {
        if (_zone == null) return;

        _isLoading = true;
        try
        {
            // Application mode
            AppModeCombo.SelectedIndex = (int)_zone.ApplicationMode;
            ThresholdSlider.Value = _zone.Threshold;
            ThresholdLabel.Text = $"Threshold ({_zone.Threshold:F2})";
            ThresholdPanel.Visibility = _zone.ApplicationMode == ApplicationMode.Threshold
                ? Visibility.Visible : Visibility.Collapsed;

            // Gradient type
            GradientCombo.SelectedIndex = (int)_zone.GradientType;

            // Red channel
            RedEnabled.IsChecked = _zone.RedChannel.Enabled;
            RedStrength.Value = _zone.RedChannel.Strength;
            RedStrengthLabel.Text = $"Strength ({_zone.RedChannel.Strength:F2})";
            RedWhiteProt.Value = _zone.RedChannel.WhiteProtection;
            RedWhiteProtLabel.Text = $"White Protection ({_zone.RedChannel.WhiteProtection:F2})";
            RedDominance.Value = _zone.RedChannel.DominanceThreshold;
            RedDominanceLabel.Text = FormatDominanceLabel(_zone.RedChannel.DominanceThreshold);
            RedGradient.StartColor = _zone.RedChannel.StartColor;
            RedGradient.EndColor = _zone.RedChannel.EndColor;
            RedGradient.GradientType = _zone.GradientType;
            RedBlendMode.SelectedMode = _zone.RedChannel.BlendMode;
            RedBlendMode.StartColor = _zone.RedChannel.StartColor;
            RedBlendMode.EndColor = _zone.RedChannel.EndColor;
            RedPanel.Visibility = _zone.RedChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;

            // Green channel
            GreenEnabled.IsChecked = _zone.GreenChannel.Enabled;
            GreenStrength.Value = _zone.GreenChannel.Strength;
            GreenStrengthLabel.Text = $"Strength ({_zone.GreenChannel.Strength:F2})";
            GreenWhiteProt.Value = _zone.GreenChannel.WhiteProtection;
            GreenWhiteProtLabel.Text = $"White Protection ({_zone.GreenChannel.WhiteProtection:F2})";
            GreenDominance.Value = _zone.GreenChannel.DominanceThreshold;
            GreenDominanceLabel.Text = FormatDominanceLabel(_zone.GreenChannel.DominanceThreshold);
            GreenGradient.StartColor = _zone.GreenChannel.StartColor;
            GreenGradient.EndColor = _zone.GreenChannel.EndColor;
            GreenGradient.GradientType = _zone.GradientType;
            GreenBlendMode.SelectedMode = _zone.GreenChannel.BlendMode;
            GreenBlendMode.StartColor = _zone.GreenChannel.StartColor;
            GreenBlendMode.EndColor = _zone.GreenChannel.EndColor;
            GreenPanel.Visibility = _zone.GreenChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;

            // Blue channel
            BlueEnabled.IsChecked = _zone.BlueChannel.Enabled;
            BlueStrength.Value = _zone.BlueChannel.Strength;
            BlueStrengthLabel.Text = $"Strength ({_zone.BlueChannel.Strength:F2})";
            BlueWhiteProt.Value = _zone.BlueChannel.WhiteProtection;
            BlueWhiteProtLabel.Text = $"White Protection ({_zone.BlueChannel.WhiteProtection:F2})";
            BlueDominance.Value = _zone.BlueChannel.DominanceThreshold;
            BlueDominanceLabel.Text = FormatDominanceLabel(_zone.BlueChannel.DominanceThreshold);
            BlueGradient.StartColor = _zone.BlueChannel.StartColor;
            BlueGradient.EndColor = _zone.BlueChannel.EndColor;
            BlueGradient.GradientType = _zone.GradientType;
            BlueBlendMode.SelectedMode = _zone.BlueChannel.BlendMode;
            BlueBlendMode.StartColor = _zone.BlueChannel.StartColor;
            BlueBlendMode.EndColor = _zone.BlueChannel.EndColor;
            BluePanel.Visibility = _zone.BlueChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;

            // Update preview strip
            UpdatePreviewStrip();
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Get current settings as a CustomPreset.
    /// </summary>
    public CustomPreset GetAsPreset(string name)
    {
        if (_zone == null)
        {
            return new CustomPreset { Name = name };
        }

        return new CustomPreset
        {
            Name = name,
            Description = $"Custom preset created on {DateTime.Now:yyyy-MM-dd}",
            IsCustom = true,
            CreatedDate = DateTime.UtcNow,

            RedEnabled = _zone.RedChannel.Enabled,
            RedStrength = _zone.RedChannel.Strength,
            RedWhiteProtection = _zone.RedChannel.WhiteProtection,
            RedDominanceThreshold = _zone.RedChannel.DominanceThreshold,
            RedBlendMode = (int)_zone.RedChannel.BlendMode,
            RedStartColor = CustomPreset.ToHexColor(_zone.RedChannel.StartColor),
            RedEndColor = CustomPreset.ToHexColor(_zone.RedChannel.EndColor),

            GreenEnabled = _zone.GreenChannel.Enabled,
            GreenStrength = _zone.GreenChannel.Strength,
            GreenWhiteProtection = _zone.GreenChannel.WhiteProtection,
            GreenDominanceThreshold = _zone.GreenChannel.DominanceThreshold,
            GreenBlendMode = (int)_zone.GreenChannel.BlendMode,
            GreenStartColor = CustomPreset.ToHexColor(_zone.GreenChannel.StartColor),
            GreenEndColor = CustomPreset.ToHexColor(_zone.GreenChannel.EndColor),

            BlueEnabled = _zone.BlueChannel.Enabled,
            BlueStrength = _zone.BlueChannel.Strength,
            BlueWhiteProtection = _zone.BlueChannel.WhiteProtection,
            BlueDominanceThreshold = _zone.BlueChannel.DominanceThreshold,
            BlueBlendMode = (int)_zone.BlueChannel.BlendMode,
            BlueStartColor = CustomPreset.ToHexColor(_zone.BlueChannel.StartColor),
            BlueEndColor = CustomPreset.ToHexColor(_zone.BlueChannel.EndColor),

            DefaultIntensity = _zone.Intensity,
            RecommendedGradientType = (int)_zone.GradientType,
            RecommendedApplicationMode = (int)_zone.ApplicationMode,
            Threshold = _zone.Threshold,

            // Simulation-Guided settings
            SimulationGuidedEnabled = _zone.SimulationGuidedEnabled,
            SimulationGuidedAlgorithm = (int)_zone.SimulationGuidedAlgorithm,
            SimulationGuidedFilterType = _zone.SimulationGuidedFilterType,
            SimulationGuidedSensitivity = _zone.SimulationGuidedSensitivity

            // Note: Post-simulation settings are NOT saved in presets (they are zone-specific)
        };
    }

    private void AppMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.ApplicationMode = (ApplicationMode)AppModeCombo.SelectedIndex;
        ThresholdPanel.Visibility = _zone.ApplicationMode == ApplicationMode.Threshold
            ? Visibility.Visible : Visibility.Collapsed;

        OnSettingsChanged();
    }

    private void Threshold_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading || _zone == null) return;

        _zone.Threshold = (float)ThresholdSlider.Value;
        ThresholdLabel.Text = $"Threshold ({_zone.Threshold:F2})";

        OnSettingsChanged();
    }

    private void Gradient_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.GradientType = (GradientType)GradientCombo.SelectedIndex;

        // Update gradient editors with new interpolation type
        RedGradient.GradientType = _zone.GradientType;
        GreenGradient.GradientType = _zone.GradientType;
        BlueGradient.GradientType = _zone.GradientType;

        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void RedBlendMode_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.RedChannel.BlendMode = RedBlendMode.SelectedMode;
        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void GreenBlendMode_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.GreenChannel.BlendMode = GreenBlendMode.SelectedMode;
        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void BlueBlendMode_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.BlueChannel.BlendMode = BlueBlendMode.SelectedMode;
        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void RedGradient_ValueChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.RedChannel.StartColor = RedGradient.StartColor;
        _zone.RedChannel.EndColor = RedGradient.EndColor;
        RedBlendMode.StartColor = RedGradient.StartColor;
        RedBlendMode.EndColor = RedGradient.EndColor;
        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void GreenGradient_ValueChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.GreenChannel.StartColor = GreenGradient.StartColor;
        _zone.GreenChannel.EndColor = GreenGradient.EndColor;
        GreenBlendMode.StartColor = GreenGradient.StartColor;
        GreenBlendMode.EndColor = GreenGradient.EndColor;
        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void BlueGradient_ValueChanged(object? sender, EventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.BlueChannel.StartColor = BlueGradient.StartColor;
        _zone.BlueChannel.EndColor = BlueGradient.EndColor;
        BlueBlendMode.StartColor = BlueGradient.StartColor;
        BlueBlendMode.EndColor = BlueGradient.EndColor;
        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void Channel_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _zone == null) return;

        // Update zone settings
        _zone.RedChannel.Enabled = RedEnabled.IsChecked == true;
        _zone.RedChannel.Strength = (float)RedStrength.Value;
        _zone.RedChannel.WhiteProtection = (float)RedWhiteProt.Value;
        _zone.RedChannel.DominanceThreshold = (float)RedDominance.Value;

        _zone.GreenChannel.Enabled = GreenEnabled.IsChecked == true;
        _zone.GreenChannel.Strength = (float)GreenStrength.Value;
        _zone.GreenChannel.WhiteProtection = (float)GreenWhiteProt.Value;
        _zone.GreenChannel.DominanceThreshold = (float)GreenDominance.Value;

        _zone.BlueChannel.Enabled = BlueEnabled.IsChecked == true;
        _zone.BlueChannel.Strength = (float)BlueStrength.Value;
        _zone.BlueChannel.WhiteProtection = (float)BlueWhiteProt.Value;
        _zone.BlueChannel.DominanceThreshold = (float)BlueDominance.Value;

        // Update labels with current values
        RedStrengthLabel.Text = $"Strength ({_zone.RedChannel.Strength:F2})";
        RedWhiteProtLabel.Text = $"White Protection ({_zone.RedChannel.WhiteProtection:F2})";
        RedDominanceLabel.Text = FormatDominanceLabel(_zone.RedChannel.DominanceThreshold);
        GreenStrengthLabel.Text = $"Strength ({_zone.GreenChannel.Strength:F2})";
        GreenWhiteProtLabel.Text = $"White Protection ({_zone.GreenChannel.WhiteProtection:F2})";
        GreenDominanceLabel.Text = FormatDominanceLabel(_zone.GreenChannel.DominanceThreshold);
        BlueStrengthLabel.Text = $"Strength ({_zone.BlueChannel.Strength:F2})";
        BlueWhiteProtLabel.Text = $"White Protection ({_zone.BlueChannel.WhiteProtection:F2})";
        BlueDominanceLabel.Text = FormatDominanceLabel(_zone.BlueChannel.DominanceThreshold);

        // Update panel visibility
        RedPanel.Visibility = _zone.RedChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;
        GreenPanel.Visibility = _zone.GreenChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;
        BluePanel.Visibility = _zone.BlueChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;

        UpdatePreviewStrip();
        OnSettingsChanged();
    }

    private void Channel_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Channel_Changed(sender, (RoutedEventArgs)e);
    }

    private void UpdatePreviewStrip()
    {
        if (_zone == null) return;

        PreviewStrip.RedChannel = _zone.RedChannel;
        PreviewStrip.GreenChannel = _zone.GreenChannel;
        PreviewStrip.BlueChannel = _zone.BlueChannel;
        PreviewStrip.GradientType = _zone.GradientType;
        PreviewStrip.ApplicationMode = _zone.ApplicationMode;
        PreviewStrip.Threshold = _zone.Threshold;
        PreviewStrip.Intensity = _zone.Intensity;
        PreviewStrip.Refresh();
    }

    private void OnSettingsChanged()
    {
        if (_zone != null)
        {
            _zone.LutsNeedUpdate = true;
        }
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatDominanceLabel(float value)
    {
        if (value < 0.01f)
            return "Dominance Threshold (Off)";
        return $"Dominance Threshold ({value * 100:F0}%)";
    }
}
