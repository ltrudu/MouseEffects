using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.CherryBlossoms.UI;

public partial class CherryBlossomsSettingsControl : UserControl
{
    private readonly CherryBlossomsEffect? _effect;
    private bool _isLoading = true;

    public CherryBlossomsSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect as CherryBlossomsEffect;

        if (_effect != null)
        {
            LoadConfiguration();
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        _isLoading = true;
        try
        {
            // Max petals
            MaxPetalsSlider.Value = _effect.MaxPetals;

            // Cursor interaction
            CursorInteractionCombo.SelectedIndex = _effect.CursorInteraction;
            CursorForceSlider.Value = _effect.CursorForceStrength;
            CursorRadiusSlider.Value = _effect.CursorFieldRadius;
            UpdateCursorInteractionVisibility();

            // Spawn settings
            PetalCountSlider.Value = _effect.PetalCount;
            SpawnRadiusSlider.Value = _effect.SpawnRadius;

            // Motion settings
            FallSpeedSlider.Value = _effect.FallSpeed;
            SwayAmountSlider.Value = _effect.SwayAmount;
            SwayFrequencySlider.Value = _effect.SwayFrequency;
            SpinSpeedSlider.Value = _effect.SpinSpeed;

            // Appearance settings
            ColorPaletteCombo.SelectedIndex = _effect.ColorPalette;
            MinSizeSlider.Value = _effect.MinSize;
            MaxSizeSlider.Value = _effect.MaxSize;
            GlowIntensitySlider.Value = _effect.GlowIntensity;

            // Wind settings
            WindEnabledCheckBox.IsChecked = _effect.WindEnabled;
            WindStrengthSlider.Value = _effect.WindStrength;
            WindRandomCheckBox.IsChecked = _effect.WindRandomDirection;
            WindDirectionSlider.Value = _effect.WindDirection;
            WindMinDirSlider.Value = _effect.WindMinDirection;
            WindMaxDirSlider.Value = _effect.WindMaxDirection;
            WindTransitionCombo.SelectedIndex = _effect.WindTransitionMode;
            WindTransDurationSlider.Value = _effect.WindTransitionDuration;
            WindChangeFreqSlider.Value = _effect.WindChangeFrequency;
            UpdateWindVisibility();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateCursorInteractionVisibility()
    {
        CursorInteractionPanel.Visibility = CursorInteractionCombo.SelectedIndex > 0
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateWindVisibility()
    {
        bool windEnabled = WindEnabledCheckBox.IsChecked == true;
        WindSettingsPanel.Visibility = windEnabled ? Visibility.Visible : Visibility.Collapsed;

        bool randomDir = WindRandomCheckBox.IsChecked == true;
        WindFixedPanel.Visibility = randomDir ? Visibility.Collapsed : Visibility.Visible;
        WindRandomPanel.Visibility = randomDir ? Visibility.Visible : Visibility.Collapsed;
    }

    // Max Petals
    private void MaxPetalsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        int value = (int)MaxPetalsSlider.Value;
        _effect.MaxPetals = value;
        _effect.Configuration.Set("cb_maxPetals", value);
        MaxPetalsValue.Text = value.ToString();
    }

    // Cursor Interaction
    private void CursorInteractionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        int value = CursorInteractionCombo.SelectedIndex;
        _effect.CursorInteraction = value;
        _effect.Configuration.Set("cb_cursorInteraction", value);
        UpdateCursorInteractionVisibility();
    }

    private void CursorForceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)CursorForceSlider.Value;
        _effect.CursorForceStrength = value;
        _effect.Configuration.Set("cb_cursorForceStrength", value);
        CursorForceValue.Text = value.ToString("F0");
    }

    private void CursorRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)CursorRadiusSlider.Value;
        _effect.CursorFieldRadius = value;
        _effect.Configuration.Set("cb_cursorFieldRadius", value);
        CursorRadiusValue.Text = value.ToString("F0");
    }

    // Spawn Settings
    private void PetalCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        int value = (int)PetalCountSlider.Value;
        _effect.PetalCount = value;
        _effect.Configuration.Set("cb_petalCount", value);
        PetalCountValue.Text = value.ToString();
    }

    private void SpawnRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SpawnRadiusSlider.Value;
        _effect.SpawnRadius = value;
        _effect.Configuration.Set("cb_spawnRadius", value);
        SpawnRadiusValue.Text = value.ToString("F0");
    }

    // Motion Settings
    private void FallSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)FallSpeedSlider.Value;
        _effect.FallSpeed = value;
        _effect.Configuration.Set("cb_fallSpeed", value);
        FallSpeedValue.Text = value.ToString("F0");
    }

    private void SwayAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SwayAmountSlider.Value;
        _effect.SwayAmount = value;
        _effect.Configuration.Set("cb_swayAmount", value);
        SwayAmountValue.Text = value.ToString("F0");
    }

    private void SwayFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SwayFrequencySlider.Value;
        _effect.SwayFrequency = value;
        _effect.Configuration.Set("cb_swayFrequency", value);
        SwayFrequencyValue.Text = value.ToString("F1");
    }

    private void SpinSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)SpinSpeedSlider.Value;
        _effect.SpinSpeed = value;
        _effect.Configuration.Set("cb_spinSpeed", value);
        SpinSpeedValue.Text = value.ToString("F1");
    }

    // Appearance Settings
    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MinSizeSlider.Value;
        _effect.MinSize = value;
        _effect.Configuration.Set("cb_minSize", value);
        MinSizeValue.Text = value.ToString("F0");
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)MaxSizeSlider.Value;
        _effect.MaxSize = value;
        _effect.Configuration.Set("cb_maxSize", value);
        MaxSizeValue.Text = value.ToString("F0");
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)GlowIntensitySlider.Value;
        _effect.GlowIntensity = value;
        _effect.Configuration.Set("cb_glowIntensity", value);
        GlowIntensityValue.Text = value.ToString("F1");
    }

    private void ColorPaletteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        int value = ColorPaletteCombo.SelectedIndex;
        _effect.ColorPalette = value;
        _effect.Configuration.Set("cb_colorPalette", value);
    }

    // Wind Settings
    private void WindEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        bool value = WindEnabledCheckBox.IsChecked == true;
        _effect.WindEnabled = value;
        _effect.Configuration.Set("cb_windEnabled", value);
        UpdateWindVisibility();
    }

    private void WindStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindStrengthSlider.Value;
        _effect.WindStrength = value;
        _effect.Configuration.Set("cb_windStrength", value);
        WindStrengthValue.Text = value.ToString("F0");
    }

    private void WindRandomCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        bool value = WindRandomCheckBox.IsChecked == true;
        _effect.WindRandomDirection = value;
        _effect.Configuration.Set("cb_windRandomDirection", value);
        UpdateWindVisibility();
    }

    private void WindDirectionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindDirectionSlider.Value;
        _effect.WindDirection = value;
        _effect.Configuration.Set("cb_windDirection", value);
        WindDirectionValue.Text = value.ToString("F0");
    }

    private void WindMinDirSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindMinDirSlider.Value;
        _effect.WindMinDirection = value;
        _effect.Configuration.Set("cb_windMinDirection", value);
        WindMinDirValue.Text = value.ToString("F0");
    }

    private void WindMaxDirSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindMaxDirSlider.Value;
        _effect.WindMaxDirection = value;
        _effect.Configuration.Set("cb_windMaxDirection", value);
        WindMaxDirValue.Text = value.ToString("F0");
    }

    private void WindTransitionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        int value = WindTransitionCombo.SelectedIndex;
        _effect.WindTransitionMode = value;
        _effect.Configuration.Set("cb_windTransitionMode", value);
    }

    private void WindTransDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindTransDurationSlider.Value;
        _effect.WindTransitionDuration = value;
        _effect.Configuration.Set("cb_windTransitionDuration", value);
        WindTransDurationValue.Text = value.ToString("F1");
    }

    private void WindChangeFreqSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        float value = (float)WindChangeFreqSlider.Value;
        _effect.WindChangeFrequency = value;
        _effect.Configuration.Set("cb_windChangeFrequency", value);
        WindChangeFreqValue.Text = value.ToString("F1");
    }
}
