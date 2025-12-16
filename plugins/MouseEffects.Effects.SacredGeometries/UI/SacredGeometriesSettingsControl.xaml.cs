using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.SacredGeometries.UI.Effects;

namespace MouseEffects.Effects.SacredGeometries.UI;

public partial class SacredGeometriesSettingsControl : System.Windows.Controls.UserControl
{
    private SacredGeometriesEffect? _effect;
    private bool _isLoading;
    private bool _isExpanded;

    // Lazy-loaded effect settings controls
    private MandalaSettings? _mandalaSettings;
    private ShapesSettings? _shapesSettings;

    public SacredGeometriesSettingsControl(IEffect effect)
    {
        InitializeComponent();
        DataContext = effect;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SacredGeometriesEffect sgEffect)
        {
            _effect = sgEffect;
            _isLoading = true;

            try
            {
                EnabledCheckBox.IsChecked = _effect.IsEnabled;

                // Load the selected effect type from config
                if (_effect.Configuration.TryGet("selectedEffectType", out int effectType))
                {
                    EffectTypeCombo.SelectedIndex = effectType;
                }

                LoadEffectSettings();
            }
            finally
            {
                _isLoading = false;
            }
        }
    }

    private void LoadEffectSettings()
    {
        if (_effect == null) return;

        int effectType = EffectTypeCombo.SelectedIndex;

        switch (effectType)
        {
            case 0: // Mandala
                if (_mandalaSettings == null)
                {
                    _mandalaSettings = new MandalaSettings();
                }
                EffectSettingsHost.Content = _mandalaSettings;
                _mandalaSettings.Initialize(_effect);
                break;

            case 1: // Shapes
                if (_shapesSettings == null)
                {
                    _shapesSettings = new ShapesSettings();
                }
                EffectSettingsHost.Content = _shapesSettings;
                _shapesSettings.Initialize(_effect);
                break;
        }
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect == null || _isLoading) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked == true;
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "\u25B6" : "\u25BC";
    }

    private void EffectTypeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int effectType = EffectTypeCombo.SelectedIndex;

        // Save the selected effect type to config and effect property
        _effect.Configuration.Set("selectedEffectType", effectType);
        _effect.SelectedEffectType = effectType;

        // Load the appropriate settings control
        LoadEffectSettings();
    }
}
