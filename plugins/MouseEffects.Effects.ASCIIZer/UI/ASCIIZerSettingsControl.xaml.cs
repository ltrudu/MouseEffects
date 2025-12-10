using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.ASCIIZer.UI.Filters;
using UserControl = System.Windows.Controls.UserControl;

namespace MouseEffects.Effects.ASCIIZer.UI;

/// <summary>
/// Main settings control for ASCIIZer effect.
/// Hosts filter-specific settings based on selected filter type.
/// </summary>
public partial class ASCIIZerSettingsControl : UserControl
{
    private ASCIIZerEffect? _effect;
    private bool _isLoading;
    private bool _isExpanded;

    // Filter-specific settings controls
    private ASCIIClassicSettings? _asciiClassicSettings;

    public ASCIIZerSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public ASCIIZerSettingsControl(IEffect effect) : this()
    {
        DataContext = effect;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IEffect effect && effect is ASCIIZerEffect asciiEffect)
        {
            _effect = asciiEffect;
            LoadConfiguration();
            LoadFilterSettings();
        }
    }

    private void LoadConfiguration()
    {
        if (_effect == null) return;
        _isLoading = true;

        try
        {
            EnabledCheckBox.IsChecked = _effect.IsEnabled;

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
            case 0: // ASCII Art Classic
                if (_asciiClassicSettings == null)
                {
                    _asciiClassicSettings = new ASCIIClassicSettings();
                    _asciiClassicSettings.DataContext = _effect;
                }
                FilterSettingsHost.Content = _asciiClassicSettings;
                _asciiClassicSettings.Initialize(_effect);
                break;

            // Future filters will be added here
            default:
                FilterSettingsHost.Content = null;
                break;
        }
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "\u25B2" : "\u25BC";
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_effect != null && !_isLoading)
        {
            _effect.IsEnabled = EnabledCheckBox.IsChecked == true;
        }
    }

    private void FilterTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_effect == null || _isLoading) return;

        int filterType = FilterTypeCombo.SelectedIndex;
        _effect.Configuration.Set("filterType", filterType);
        LoadFilterSettings();
    }
}
