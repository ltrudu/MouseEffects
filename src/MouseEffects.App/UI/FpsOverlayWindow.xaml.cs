using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace MouseEffects.App.UI;

/// <summary>
/// Small overlay window displaying FPS counter in the top right corner.
/// </summary>
public partial class FpsOverlayWindow : Window
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int GWL_EXSTYLE = -20;

    // Use GetWindowLongPtrW on 64-bit, GetWindowLongW on 32-bit
#if TARGET_64BIT
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr(nint hwnd, int index, nint newStyle);
#else
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static partial int GetWindowLong32(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static partial int SetWindowLong32(nint hwnd, int index, int newStyle);

    private static nint GetWindowLongPtr(nint hwnd, int index) => GetWindowLong32(hwnd, index);
    private static nint SetWindowLongPtr(nint hwnd, int index, nint newStyle) => SetWindowLong32(hwnd, index, (int)newStyle);
#endif

    private readonly DispatcherTimer _updateTimer;

    public FpsOverlayWindow()
    {
        InitializeComponent();

        // Position in top right corner
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = screenWidth - Width - 10;
        Top = 10;

        // Update timer
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _updateTimer.Tick += UpdateTimer_Tick;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Make window click-through
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = (int)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, (nint)(extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW));
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        var gameLoop = Program.GameLoop;
        if (gameLoop != null)
        {
            var currentFps = gameLoop.CurrentFps;
            var targetFps = gameLoop.TargetFrameRate;
            FpsText.Text = $"{currentFps:F1} / {targetFps} fps";

            // Color code based on performance
            var ratio = currentFps / targetFps;
            if (ratio >= 0.95)
                FpsText.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A6E3A1")!); // Green
            else if (ratio >= 0.8)
                FpsText.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9E2AF")!); // Yellow
            else
                FpsText.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F38BA8")!); // Red
        }
    }

    public new void Show()
    {
        base.Show();
        _updateTimer.Start();
    }

    public new void Hide()
    {
        _updateTimer.Stop();
        base.Hide();
    }

    private bool _forceClose;

    public new void Close()
    {
        _forceClose = true;
        _updateTimer.Stop();
        base.Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
