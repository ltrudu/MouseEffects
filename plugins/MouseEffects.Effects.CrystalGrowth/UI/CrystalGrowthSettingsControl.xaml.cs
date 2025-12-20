using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.CrystalGrowth.UI;

public partial class CrystalGrowthSettingsControl : UserControl
{
    private readonly CrystalGrowthEffect? _effect;
    private bool _isLoading = true;

    public CrystalGrowthSettingsControl(IEffect effect)
    {
        InitializeComponent();

        if (effect is CrystalGrowthEffect crystalEffect)
        {
            _effect = crystalEffect;
            LoadConfiguration();
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        _isLoading = true;
        try
        {
            LeftClickCheck.IsChecked = _effect.LeftClickEnabled;
            RightClickCheck.IsChecked = _effect.RightClickEnabled;
            CrystalCountSlider.Value = _effect.CrystalsPerClick;
            GrowthSpeedSlider.Value = _effect.GrowthSpeed;
            MaxSizeSlider.Value = _effect.MaxSize;
            BranchProbSlider.Value = _effect.BranchProbability;
            MaxGenerationsSlider.Value = _effect.MaxGenerations;
            ThicknessSlider.Value = _effect.BranchThickness;
            GlowSlider.Value = _effect.GlowIntensity;
            SparkleSlider.Value = _effect.SparkleIntensity;
            LifetimeSlider.Value = _effect.Lifetime;
            ColorPresetCombo.SelectedIndex = _effect.ColorPreset;

            CustomRSlider.Value = _effect.CustomColor.X;
            CustomGSlider.Value = _effect.CustomColor.Y;
            CustomBSlider.Value = _effect.CustomColor.Z;

            RainbowSpeedSlider.Value = _effect.RainbowSpeed;
            MultiColorCheck.IsChecked = _effect.RainbowMultiColor;

            RotationEnabledCheck.IsChecked = _effect.RotationEnabled;
            RotationDirectionCombo.SelectedIndex = _effect.RotationDirection;
            RandomSpeedCheck.IsChecked = _effect.RotationRandomSpeed;
            RotationSpeedSlider.Value = _effect.RotationSpeed;
            MinSpeedSlider.Value = _effect.RotationMinSpeed;
            MaxSpeedSlider.Value = _effect.RotationMaxSpeed;

            UpdateColorPanelVisibility();
            UpdateRotationPanelVisibility();
            UpdateColorPreview();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LeftClickCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.LeftClickEnabled = LeftClickCheck.IsChecked ?? true;
        _effect.Configuration.Set("cg_leftClickEnabled", _effect.LeftClickEnabled);
    }

    private void RightClickCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RightClickEnabled = RightClickCheck.IsChecked ?? true;
        _effect.Configuration.Set("cg_rightClickEnabled", _effect.RightClickEnabled);
    }

    private void CrystalCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.CrystalsPerClick = (int)CrystalCountSlider.Value;
        _effect.Configuration.Set("cg_crystalsPerClick", _effect.CrystalsPerClick);
    }

    private void GrowthSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GrowthSpeed = (float)GrowthSpeedSlider.Value;
        _effect.Configuration.Set("cg_growthSpeed", _effect.GrowthSpeed);
    }

    private void MaxSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxSize = (float)MaxSizeSlider.Value;
        _effect.Configuration.Set("cg_maxSize", _effect.MaxSize);
    }

    private void BranchProbSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.BranchProbability = (float)BranchProbSlider.Value;
        _effect.Configuration.Set("cg_branchProbability", _effect.BranchProbability);
    }

    private void MaxGenerationsSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxGenerations = (int)MaxGenerationsSlider.Value;
        _effect.Configuration.Set("cg_maxGenerations", _effect.MaxGenerations);
    }

    private void ThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.BranchThickness = (float)ThicknessSlider.Value;
        _effect.Configuration.Set("cg_branchThickness", _effect.BranchThickness);
    }

    private void GlowSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.GlowIntensity = (float)GlowSlider.Value;
        _effect.Configuration.Set("cg_glowIntensity", _effect.GlowIntensity);
    }

    private void SparkleSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SparkleIntensity = (float)SparkleSlider.Value;
        _effect.Configuration.Set("cg_sparkleIntensity", _effect.SparkleIntensity);
    }

    private void LifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Lifetime = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("cg_lifetime", _effect.Lifetime);
    }

    private void ColorPresetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorPreset = ColorPresetCombo.SelectedIndex;
        _effect.Configuration.Set("cg_colorPreset", _effect.ColorPreset);

        UpdateColorPanelVisibility();
    }

    private void RainbowSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowSpeed = (float)RainbowSpeedSlider.Value;
        _effect.Configuration.Set("cg_rainbowSpeed", _effect.RainbowSpeed);
    }

    private void MultiColorCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RainbowMultiColor = MultiColorCheck.IsChecked ?? false;
        _effect.Configuration.Set("cg_rainbowMultiColor", _effect.RainbowMultiColor);
    }

    private void RotationEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationEnabled = RotationEnabledCheck.IsChecked ?? false;
        _effect.Configuration.Set("cg_rotationEnabled", _effect.RotationEnabled);
        UpdateRotationPanelVisibility();
    }

    private void RotationDirectionCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationDirection = RotationDirectionCombo.SelectedIndex;
        _effect.Configuration.Set("cg_rotationDirection", _effect.RotationDirection);
    }

    private void RandomSpeedCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationRandomSpeed = RandomSpeedCheck.IsChecked ?? false;
        _effect.Configuration.Set("cg_rotationRandomSpeed", _effect.RotationRandomSpeed);
        UpdateRotationPanelVisibility();
    }

    private void RotationSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RotationSpeed = (float)RotationSpeedSlider.Value;
        _effect.Configuration.Set("cg_rotationSpeed", _effect.RotationSpeed);
    }

    private void MinSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        // Ensure min < max
        if (MinSpeedSlider.Value >= MaxSpeedSlider.Value)
        {
            MinSpeedSlider.Value = MaxSpeedSlider.Value - 0.1;
        }

        _effect.RotationMinSpeed = (float)MinSpeedSlider.Value;
        _effect.Configuration.Set("cg_rotationMinSpeed", _effect.RotationMinSpeed);
    }

    private void MaxSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;

        // Ensure max > min
        if (MaxSpeedSlider.Value <= MinSpeedSlider.Value)
        {
            MaxSpeedSlider.Value = MinSpeedSlider.Value + 0.1;
        }

        _effect.RotationMaxSpeed = (float)MaxSpeedSlider.Value;
        _effect.Configuration.Set("cg_rotationMaxSpeed", _effect.RotationMaxSpeed);
    }

    private void UpdateRotationPanelVisibility()
    {
        if (RotationSettingsPanel == null) return;

        RotationSettingsPanel.Visibility = RotationEnabledCheck.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (FixedSpeedPanel != null && RandomSpeedPanel != null)
        {
            bool isRandom = RandomSpeedCheck.IsChecked == true;
            FixedSpeedPanel.Visibility = isRandom ? Visibility.Collapsed : Visibility.Visible;
            RandomSpeedPanel.Visibility = isRandom ? Visibility.Visible : Visibility.Collapsed;
        }
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
        _effect.Configuration.Set("cg_customColor", color);

        UpdateColorPreview();
    }

    private void UpdateColorPanelVisibility()
    {
        // Index 4 = Custom, Index 5 = Rainbow
        CustomColorPanel.Visibility = ColorPresetCombo.SelectedIndex == 4
            ? Visibility.Visible
            : Visibility.Collapsed;

        RainbowPanel.Visibility = ColorPresetCombo.SelectedIndex == 5
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
