using ModernWpf;
using MouseEffects.App.Settings;

namespace MouseEffects.App;

public partial class App : System.Windows.Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Apply the theme from settings.
    /// </summary>
    public void ApplyTheme(AppTheme theme)
    {
        ApplicationTheme? modernTheme = theme switch
        {
            AppTheme.Light => ApplicationTheme.Light,
            AppTheme.Dark => ApplicationTheme.Dark,
            AppTheme.System => null, // null = follow system
            _ => null
        };

        ThemeManager.Current.ApplicationTheme = modernTheme;
    }

    /// <summary>
    /// Get the current effective theme (for UI elements that need to know).
    /// </summary>
    public static ApplicationTheme GetEffectiveTheme()
    {
        var current = ThemeManager.Current.ActualApplicationTheme;
        return current;
    }
}
