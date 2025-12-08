using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MouseEffects.Core.UI;
using WpfColor = System.Windows.Media.Color;

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
            RedBlendModeCombo.SelectedIndex = (int)_zone.RedChannel.BlendMode;
            RedStart.Background = new SolidColorBrush(Vector3ToColor(_zone.RedChannel.StartColor));
            RedEnd.Background = new SolidColorBrush(Vector3ToColor(_zone.RedChannel.EndColor));
            RedPanel.Visibility = _zone.RedChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;

            // Green channel
            GreenEnabled.IsChecked = _zone.GreenChannel.Enabled;
            GreenStrength.Value = _zone.GreenChannel.Strength;
            GreenStrengthLabel.Text = $"Strength ({_zone.GreenChannel.Strength:F2})";
            GreenWhiteProt.Value = _zone.GreenChannel.WhiteProtection;
            GreenWhiteProtLabel.Text = $"White Protection ({_zone.GreenChannel.WhiteProtection:F2})";
            GreenDominance.Value = _zone.GreenChannel.DominanceThreshold;
            GreenDominanceLabel.Text = FormatDominanceLabel(_zone.GreenChannel.DominanceThreshold);
            GreenBlendModeCombo.SelectedIndex = (int)_zone.GreenChannel.BlendMode;
            GreenStart.Background = new SolidColorBrush(Vector3ToColor(_zone.GreenChannel.StartColor));
            GreenEnd.Background = new SolidColorBrush(Vector3ToColor(_zone.GreenChannel.EndColor));
            GreenPanel.Visibility = _zone.GreenChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;

            // Blue channel
            BlueEnabled.IsChecked = _zone.BlueChannel.Enabled;
            BlueStrength.Value = _zone.BlueChannel.Strength;
            BlueStrengthLabel.Text = $"Strength ({_zone.BlueChannel.Strength:F2})";
            BlueWhiteProt.Value = _zone.BlueChannel.WhiteProtection;
            BlueWhiteProtLabel.Text = $"White Protection ({_zone.BlueChannel.WhiteProtection:F2})";
            BlueDominance.Value = _zone.BlueChannel.DominanceThreshold;
            BlueDominanceLabel.Text = FormatDominanceLabel(_zone.BlueChannel.DominanceThreshold);
            BlueBlendModeCombo.SelectedIndex = (int)_zone.BlueChannel.BlendMode;
            BlueStart.Background = new SolidColorBrush(Vector3ToColor(_zone.BlueChannel.StartColor));
            BlueEnd.Background = new SolidColorBrush(Vector3ToColor(_zone.BlueChannel.EndColor));
            BluePanel.Visibility = _zone.BlueChannel.Enabled ? Visibility.Visible : Visibility.Collapsed;
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
            SimulationGuidedSensitivity = _zone.SimulationGuidedSensitivity,

            // Post-Correction Simulation settings
            PostCorrectionSimEnabled = _zone.PostCorrectionSimEnabled,
            PostCorrectionSimAlgorithm = (int)_zone.PostCorrectionSimAlgorithm,
            PostCorrectionSimFilterType = _zone.PostCorrectionSimFilterType,
            PostCorrectionSimIntensity = _zone.PostCorrectionSimIntensity
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

        OnSettingsChanged();
    }

    private void RedBlendMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.RedChannel.BlendMode = (LutBlendMode)RedBlendModeCombo.SelectedIndex;

        OnSettingsChanged();
    }

    private void GreenBlendMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.GreenChannel.BlendMode = (LutBlendMode)GreenBlendModeCombo.SelectedIndex;

        OnSettingsChanged();
    }

    private void BlueBlendMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _zone == null) return;

        _zone.BlueChannel.BlendMode = (LutBlendMode)BlueBlendModeCombo.SelectedIndex;

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

        OnSettingsChanged();
    }

    private void Channel_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Channel_Changed(sender, (RoutedEventArgs)e);
    }

    private void RedStart_Click(object sender, MouseButtonEventArgs e) =>
        PickColor(RedStart, c => { if (_zone != null) _zone.RedChannel.StartColor = ColorToVector3(c); });

    private void RedEnd_Click(object sender, MouseButtonEventArgs e) =>
        PickColor(RedEnd, c => { if (_zone != null) _zone.RedChannel.EndColor = ColorToVector3(c); });

    private void GreenStart_Click(object sender, MouseButtonEventArgs e) =>
        PickColor(GreenStart, c => { if (_zone != null) _zone.GreenChannel.StartColor = ColorToVector3(c); });

    private void GreenEnd_Click(object sender, MouseButtonEventArgs e) =>
        PickColor(GreenEnd, c => { if (_zone != null) _zone.GreenChannel.EndColor = ColorToVector3(c); });

    private void BlueStart_Click(object sender, MouseButtonEventArgs e) =>
        PickColor(BlueStart, c => { if (_zone != null) _zone.BlueChannel.StartColor = ColorToVector3(c); });

    private void BlueEnd_Click(object sender, MouseButtonEventArgs e) =>
        PickColor(BlueEnd, c => { if (_zone != null) _zone.BlueChannel.EndColor = ColorToVector3(c); });

    private void PickColor(Border colorBorder, Action<WpfColor> updateZone)
    {
        var currentColor = ((SolidColorBrush)colorBorder.Background).Color;

        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B),
            FullOpen = true
        };

        DialogHelper.SuspendOverlayTopmost();
        try
        {
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var newColor = WpfColor.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
                colorBorder.Background = new SolidColorBrush(newColor);
                updateZone(newColor);
                OnSettingsChanged();
            }
        }
        finally
        {
            DialogHelper.ResumeOverlayTopmost();
        }
    }

    private void OnSettingsChanged()
    {
        if (_zone != null)
        {
            _zone.LutsNeedUpdate = true;
        }
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static WpfColor Vector3ToColor(Vector3 v) =>
        WpfColor.FromRgb((byte)(v.X * 255), (byte)(v.Y * 255), (byte)(v.Z * 255));

    private static Vector3 ColorToVector3(WpfColor c) =>
        new(c.R / 255f, c.G / 255f, c.B / 255f);

    private static string FormatDominanceLabel(float value)
    {
        if (value < 0.01f)
            return "Dominance Threshold (Off)";
        return $"Dominance Threshold ({value * 100:F0}%)";
    }
}
