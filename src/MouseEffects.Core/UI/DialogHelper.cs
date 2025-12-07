using System.Runtime.InteropServices;

namespace MouseEffects.Core.UI;

/// <summary>
/// Global helper for showing dialogs above the overlay.
/// Makes dialogs topmost so they appear above the overlay without disrupting it.
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Initialize the helper. Kept for API compatibility.
    /// </summary>
    public static void Initialize(Action suspendEnforcement, Action resumeEnforcement)
    {
        // No longer needed - enforcement is managed by SettingsWindow only
    }

    /// <summary>
    /// Temporarily suspend overlay topmost enforcement.
    /// No-op - enforcement is managed by SettingsWindow only.
    /// </summary>
    public static void SuspendOverlayTopmost()
    {
        // No-op - enforcement is managed by SettingsWindow only
    }

    /// <summary>
    /// Resume overlay topmost enforcement after dialogs are closed.
    /// No-op - enforcement is managed by SettingsWindow only.
    /// </summary>
    public static void ResumeOverlayTopmost()
    {
        // No-op - enforcement is managed by SettingsWindow only
    }

    /// <summary>
    /// Execute an action (typically showing a dialog) with the dialog made topmost.
    /// Does NOT affect enforcement - that's managed by SettingsWindow.
    /// </summary>
    public static void WithSuspendedTopmost(Action action)
    {
        // Hook to catch the dialog window and make it topmost
        using var hook = new TopmostDialogHook();
        action();
    }

    /// <summary>
    /// Execute a function (typically showing a dialog) with the dialog made topmost.
    /// Does NOT affect enforcement - that's managed by SettingsWindow.
    /// </summary>
    public static T WithSuspendedTopmost<T>(Func<T> func)
    {
        // Hook to catch the dialog window and make it topmost
        using var hook = new TopmostDialogHook();
        return func();
    }

    /// <summary>
    /// Helper class that hooks window creation to make dialogs topmost.
    /// </summary>
    private sealed class TopmostDialogHook : IDisposable
    {
        private readonly nint _hook;
        private readonly WinEventDelegate _callback;
        private bool _disposed;

        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        public TopmostDialogHook()
        {
            _callback = WinEventProc;
            _hook = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE,
                nint.Zero, _callback, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        private void WinEventProc(nint hWinEventHook, uint eventType, nint hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == nint.Zero || idObject != 0) return;

            // Check if this is a dialog window (has WS_POPUP or WS_DLGFRAME)
            var style = GetWindowLongPtr(hwnd, GWL_STYLE);
            var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);

            // Make the window topmost if it looks like a dialog
            bool isPopup = (style & WS_POPUP) != 0;
            bool isDialog = (style & WS_DLGFRAME) != 0;
            bool isToolWindow = (exStyle & WS_EX_TOOLWINDOW) != 0;

            if ((isPopup || isDialog) && !isToolWindow)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_hook != nint.Zero)
            {
                UnhookWinEvent(_hook);
            }
        }

        private delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const long WS_POPUP = 0x80000000L;
        private const long WS_DLGFRAME = 0x00400000L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private static readonly nint HWND_TOPMOST = new(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        private static extern nint SetWinEventHook(uint eventMin, uint eventMax,
            nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
            uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWinEvent(nint hWinEventHook);

        [DllImport("user32.dll")]
        private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);
    }
}
