using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Helper class to show file dialogs above the topmost overlay.
/// </summary>
public static class TopmostDialogHelper
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>
    /// Shows an OpenFileDialog with topmost behavior.
    /// </summary>
    public static bool? ShowOpenFileDialog(Microsoft.Win32.OpenFileDialog dialog, Window? owner = null)
    {
        // Create a temporary topmost window as owner
        var tempWindow = CreateTopmostOwnerWindow(owner);
        try
        {
            return dialog.ShowDialog(tempWindow);
        }
        finally
        {
            tempWindow?.Close();
        }
    }

    /// <summary>
    /// Shows a SaveFileDialog with topmost behavior.
    /// </summary>
    public static bool? ShowSaveFileDialog(Microsoft.Win32.SaveFileDialog dialog, Window? owner = null)
    {
        // Create a temporary topmost window as owner
        var tempWindow = CreateTopmostOwnerWindow(owner);
        try
        {
            return dialog.ShowDialog(tempWindow);
        }
        finally
        {
            tempWindow?.Close();
        }
    }

    private static Window CreateTopmostOwnerWindow(Window? parentOwner)
    {
        var tempWindow = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000
        };

        if (parentOwner != null)
        {
            tempWindow.Owner = parentOwner;
        }

        tempWindow.Show();

        // Ensure the window is topmost
        var hwnd = new WindowInteropHelper(tempWindow).Handle;
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        return tempWindow;
    }
}
