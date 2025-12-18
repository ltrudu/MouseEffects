using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Retro.UI.Filters;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.Retro.UI;

/// <summary>
/// Main settings control for Retro effect.
/// Hosts filter-specific settings based on selected filter type.
/// </summary>
public partial class RetroSettingsControl : UserControl
{
    private RetroEffect? _effect;
    private bool _isLoading;

    // Filter-specific settings controls
    private XSaISettings? _xsaiSettings;
    private TVFilterSettings? _tvFilterSettings;
    private ToonFilterSettings? _toonFilterSettings;

    public RetroSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public RetroSettingsControl(IEffect effect) : this()
    {
        DataContext = effect;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IEffect effect && effect is RetroEffect retroEffect)
        {
            _effect = retroEffect;
            LoadConfiguration();
            LoadFilterSettings();

            // Initialize the shared pre-filter panel (scaling mode)
            PreFilterPanel.Initialize(_effect);

            // Initialize the shared post-effects panel
            PostEffectsPanel.Initialize(_effect);
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;
        _isLoading = true;

        try
        {
            if (_effect.Configuration.TryGet("filterType", out int filterType))
            {
                FilterTypeCombo.SelectedIndex = filterType;
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LoadFilterSettings()
    {
        if (_effect == null) return;

        int filterType = FilterTypeCombo.SelectedIndex;

        switch (filterType)
        {
            case 2: // Toon Filter
                if (_toonFilterSettings == null)
                {
                    _toonFilterSettings = new ToonFilterSettings();
                    _toonFilterSettings.DataContext = _effect;
                }
                FilterSettingsHost.Content = _toonFilterSettings;
                _toonFilterSettings.Initialize(_effect);
                break;
            case 1: // TV Filter
                if (_tvFilterSettings == null)
                {
                    _tvFilterSettings = new TVFilterSettings();
                    _tvFilterSettings.DataContext = _effect;
                }
                FilterSettingsHost.Content = _tvFilterSettings;
                _tvFilterSettings.Initialize(_effect);
                break;
            case 0: // xSaI
            default:
                if (_xsaiSettings == null)
                {
                    _xsaiSettings = new XSaISettings();
                    _xsaiSettings.DataContext = _effect;
                }
                FilterSettingsHost.Content = _xsaiSettings;
                _xsaiSettings.Initialize(_effect);
                break;
        }
    }

    private void FilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int filterType = FilterTypeCombo.SelectedIndex;
        _effect.FilterType = (FilterType)filterType;
        _effect.Configuration.Set("filterType", filterType);
        LoadFilterSettings();
    }
}
