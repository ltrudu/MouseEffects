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

            UpdateCustomColorVisibility();
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
        _effect.Configuration.Set("cg_customColor", color);

        UpdateColorPreview();
    }

    private void UpdateCustomColorVisibility()
    {
        CustomColorPanel.Visibility = ColorPresetCombo.SelectedIndex == 4
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
