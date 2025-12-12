using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Tesla.UI.Effects;

namespace MouseEffects.Effects.Tesla.UI;

public partial class TeslaSettingsControl : System.Windows.Controls.UserControl
{
    private TeslaEffect? _effect;
    private bool _isLoading;
    private bool _isExpanded;

    // Lazy-loaded effect settings controls
    private LightningBoltSettings? _lightningBoltSettings;
    private ElectricalFollowSettings? _electricalFollowSettings;

    public TeslaSettingsControl(IEffect effect)
    {
        InitializeComponent();
        DataContext = effect;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TeslaEffect teslaEffect)
        {
            _effect = teslaEffect;
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
            case 0: // Lightning Bolt
                if (_lightningBoltSettings == null)
                {
                    _lightningBoltSettings = new LightningBoltSettings();
                }
                EffectSettingsHost.Content = _lightningBoltSettings;
                _lightningBoltSettings.Initialize(_effect);
                break;

            case 1: // Electrical Follow
                if (_electricalFollowSettings == null)
                {
                    _electricalFollowSettings = new ElectricalFollowSettings();
                }
                EffectSettingsHost.Content = _electricalFollowSettings;
                _electricalFollowSettings.Initialize(_effect);
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
        FoldButton.Content = _isExpanded ? "-" : "+";
    }

    private void EffectTypeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        // Save the selected effect type to config
        _effect.Configuration.Set("selectedEffectType", EffectTypeCombo.SelectedIndex);

        // Load the appropriate settings control
        LoadEffectSettings();
    }
}
