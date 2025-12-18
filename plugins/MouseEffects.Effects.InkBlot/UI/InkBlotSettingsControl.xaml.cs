using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.InkBlot.UI;

public partial class InkBlotSettingsControl : System.Windows.Controls.UserControl
{
    private InkBlotEffect? _effect;
    private bool _isLoading = true;

    public InkBlotSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect as InkBlotEffect;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;

        try
        {
            DropSizeSlider.Value = _effect.DropSize;
            SpreadSpeedSlider.Value = _effect.SpreadSpeed;
            EdgeIrregularitySlider.Value = _effect.EdgeIrregularity;
            OpacitySlider.Value = _effect.Opacity;
            LifetimeSlider.Value = _effect.Lifetime;
            ColorModeCombo.SelectedIndex = _effect.ColorMode;
            InkColorCombo.SelectedIndex = _effect.InkColorIndex;
            WatercolorCombo.SelectedIndex = _effect.WatercolorIndex;
            RandomColorCheck.IsChecked = _effect.RandomColor;
            SpawnOnClickCheck.IsChecked = _effect.SpawnOnClick;
            SpawnOnMoveCheck.IsChecked = _effect.SpawnOnMove;
            MoveDistanceSlider.Value = _effect.MoveDistance;
            MaxBlotsSlider.Value = _effect.MaxBlots;
            MaxBlotsPerSecondSlider.Value = _effect.MaxBlotsPerSecond;

            // Update panel visibility
            UpdateColorPanelVisibility();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateColorPanelVisibility()
    {
        bool isInkMode = ColorModeCombo.SelectedIndex == 0;
        InkColorPanel.Visibility = isInkMode ? Visibility.Visible : Visibility.Collapsed;
        WatercolorPanel.Visibility = isInkMode ? Visibility.Collapsed : Visibility.Visible;
    }

    private void DropSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.DropSize = (float)DropSizeSlider.Value;
        _effect.Configuration.Set("dropSize", _effect.DropSize);
    }

    private void SpreadSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpreadSpeed = (float)SpreadSpeedSlider.Value;
        _effect.Configuration.Set("spreadSpeed", _effect.SpreadSpeed);
    }

    private void EdgeIrregularitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.EdgeIrregularity = (float)EdgeIrregularitySlider.Value;
        _effect.Configuration.Set("edgeIrregularity", _effect.EdgeIrregularity);
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Opacity = (float)OpacitySlider.Value;
        _effect.Configuration.Set("opacity", _effect.Opacity);
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.Lifetime = (float)LifetimeSlider.Value;
        _effect.Configuration.Set("lifetime", _effect.Lifetime);
    }

    private void ColorModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.ColorMode = ColorModeCombo.SelectedIndex;
        _effect.Configuration.Set("colorMode", _effect.ColorMode);
        UpdateColorPanelVisibility();
    }

    private void InkColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.InkColorIndex = InkColorCombo.SelectedIndex;
        _effect.Configuration.Set("inkColorIndex", _effect.InkColorIndex);
    }

    private void WatercolorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.WatercolorIndex = WatercolorCombo.SelectedIndex;
        _effect.Configuration.Set("watercolorIndex", _effect.WatercolorIndex);
    }

    private void RandomColorCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.RandomColor = RandomColorCheck.IsChecked == true;
        _effect.Configuration.Set("randomColor", _effect.RandomColor);
    }

    private void SpawnOnClickCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpawnOnClick = SpawnOnClickCheck.IsChecked == true;
        _effect.Configuration.Set("spawnOnClick", _effect.SpawnOnClick);
    }

    private void SpawnOnMoveCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.SpawnOnMove = SpawnOnMoveCheck.IsChecked == true;
        _effect.Configuration.Set("spawnOnMove", _effect.SpawnOnMove);
    }

    private void MoveDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MoveDistance = (float)MoveDistanceSlider.Value;
        _effect.Configuration.Set("moveDistance", _effect.MoveDistance);
    }

    private void MaxBlotsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxBlots = (int)MaxBlotsSlider.Value;
        _effect.Configuration.Set("maxBlots", _effect.MaxBlots);
    }

    private void MaxBlotsPerSecondSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_effect == null || _isLoading) return;
        _effect.MaxBlotsPerSecond = (int)MaxBlotsPerSecondSlider.Value;
        _effect.Configuration.Set("maxBlotsPerSecond", _effect.MaxBlotsPerSecond);
    }
}
