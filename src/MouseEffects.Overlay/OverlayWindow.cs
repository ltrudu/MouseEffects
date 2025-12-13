using System.Drawing;
using System.Runtime.InteropServices;
using MouseEffects.Core.Rendering;
using MouseEffects.DirectX.Graphics;
using MouseEffects.Overlay.Win32;

namespace MouseEffects.Overlay;

/// <summary>
/// Transparent, click-through overlay window for rendering effects.
/// </summary>
public sealed class OverlayWindow : IDisposable
{
    private const string WindowClassName = "MouseEffectsOverlay";

    private readonly nint _hwnd;
    private readonly D3D11GraphicsDevice _graphicsDevice;
    private readonly SwapChainManager _swapChain;
    private readonly D3D11RenderContext _renderContext;
    private readonly NativeMethods.WndProc _wndProcDelegate;

    private bool _disposed;

    public nint Handle => _hwnd;
    public IRenderContext RenderContext => _renderContext;
    public Rectangle Bounds { get; private set; }
    public int Width => Bounds.Width;
    public int Height => Bounds.Height;
    public bool IsHdrEnabled => _swapChain.IsHdrEnabled;

    public OverlayWindow(Rectangle bounds, D3D11GraphicsDevice? sharedDevice = null, bool hdrEnabled = false, float hdrPeakBrightness = 4.0f)
    {
        // Validate bounds
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentException($"Invalid bounds: {bounds.Width}x{bounds.Height}");
        }

        Bounds = bounds;

        // Keep delegate alive
        _wndProcDelegate = WndProc;

        RegisterWindowClass();
        _hwnd = CreateOverlayWindow(bounds);

        if (_hwnd == nint.Zero)
        {
            throw new InvalidOperationException($"Failed to create overlay window. Error: {Marshal.GetLastWin32Error()}");
        }

        // Initialize DirectX (use shared device if provided)
        _graphicsDevice = sharedDevice ?? new D3D11GraphicsDevice();
        _swapChain = new SwapChainManager(_graphicsDevice, _hwnd, bounds.Width, bounds.Height, hdrEnabled);
        _renderContext = new D3D11RenderContext(_graphicsDevice, bounds.Width, bounds.Height, hdrEnabled, hdrPeakBrightness);

        // Make window click-through and topmost
        SetClickThrough(true);
        SetAlwaysOnTop(true);

        // Show window
        NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_SHOWNOACTIVATE);
        NativeMethods.UpdateWindow(_hwnd);
    }

    private void RegisterWindowClass()
    {
        var wndClass = new NativeMethods.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            style = NativeMethods.CS_HREDRAW | NativeMethods.CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = NativeMethods.GetModuleHandleW(null),
            hCursor = NativeMethods.LoadCursorW(nint.Zero, NativeMethods.IDC_ARROW),
            lpszClassName = WindowClassName
        };

        // RegisterClassEx may fail if class already registered - that's OK
        NativeMethods.RegisterClassExW(ref wndClass);
    }

    private nint CreateOverlayWindow(Rectangle bounds)
    {
        // Extended styles for transparent, click-through overlay
        // WS_EX_LAYERED + WS_EX_TRANSPARENT + SetLayeredWindowAttributes = click-through
        const uint exStyle =
            NativeMethods.WS_EX_LAYERED |              // Required for click-through with WS_EX_TRANSPARENT
            NativeMethods.WS_EX_TRANSPARENT |          // Click-through
            NativeMethods.WS_EX_TOPMOST |              // Always on top
            NativeMethods.WS_EX_TOOLWINDOW |           // Don't show in taskbar
            NativeMethods.WS_EX_NOACTIVATE;            // Don't activate on click

        const uint style = NativeMethods.WS_POPUP | NativeMethods.WS_VISIBLE;

        var hwnd = NativeMethods.CreateWindowExW(
            exStyle,
            WindowClassName,
            "MouseEffects Overlay",
            style,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            nint.Zero,
            nint.Zero,
            NativeMethods.GetModuleHandleW(null),
            nint.Zero
        );

        if (hwnd != nint.Zero)
        {
            // Set layered window to fully opaque - combined with WS_EX_TRANSPARENT, this enables click-through
            NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 255, NativeMethods.LWA_ALPHA);

            // Extend frame into client area for DWM composition
            var margins = new NativeMethods.MARGINS
            {
                cxLeftWidth = -1  // -1 = entire window
            };
            NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);

            // Exclude this window from screen capture (DXGI Desktop Duplication)
            // This prevents feedback loops when effects sample the captured screen
            NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_EXCLUDEFROMCAPTURE);
        }

        return hwnd;
    }

    public void SetClickThrough(bool enable)
    {
        var exStyle = (uint)NativeMethods.GetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE);

        if (enable)
        {
            exStyle |= NativeMethods.WS_EX_TRANSPARENT;
        }
        else
        {
            exStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
        }

        NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE, (nint)exStyle);
    }

    private bool _isTopmost = true;
    private bool _topmostSuspended = false;

    public void SetAlwaysOnTop(bool enable)
    {
        _isTopmost = enable;
        if (!_topmostSuspended)
        {
            ApplyTopmostState(enable);
        }
    }

    /// <summary>
    /// Temporarily suspend topmost state to allow modal dialogs to appear above the overlay.
    /// Call ResumeTopmost() when done.
    /// </summary>
    public void SuspendTopmost()
    {
        if (!_topmostSuspended)
        {
            _topmostSuspended = true;
            ApplyTopmostState(false);
        }
    }

    /// <summary>
    /// Resume topmost state after modal dialog is closed.
    /// This ensures the overlay is brought back to the front of the Z-order.
    /// </summary>
    public void ResumeTopmost()
    {
        if (_topmostSuspended)
        {
            _topmostSuspended = false;
            if (_isTopmost)
            {
                // Aggressive approach to force overlay to front:
                // 1. First remove topmost to reset Z-order state
                NativeMethods.SetWindowPos(
                    _hwnd,
                    NativeMethods.HWND_NOTOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE
                );

                // 2. Bring window to top of Z-order
                NativeMethods.BringWindowToTop(_hwnd);

                // 3. Re-apply topmost with SHOWWINDOW flag to force refresh
                NativeMethods.SetWindowPos(
                    _hwnd,
                    NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW
                );

                // 4. Force window to redraw and update
                NativeMethods.UpdateWindow(_hwnd);
            }
            else
            {
                ApplyTopmostState(false);
            }
        }
    }

    /// <summary>
    /// Re-apply topmost state. Call periodically to ensure overlay stays on top.
    /// Uses aggressive approach to force window to front of Z-order.
    /// </summary>
    public void EnforceTopmost()
    {
        if (!_topmostSuspended && _isTopmost)
        {
            // Check if we're already at the top of the Z-order
            var prevWindow = NativeMethods.GetWindow(_hwnd, NativeMethods.GW_HWNDPREV);
            if (prevWindow == nint.Zero)
            {
                // Already at top, just ensure topmost flag is set
                NativeMethods.SetWindowPos(
                    _hwnd,
                    NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE
                );
                return;
            }

            // Use AttachThreadInput trick for aggressive Z-order control
            var foregroundHwnd = NativeMethods.GetForegroundWindow();
            if (foregroundHwnd != nint.Zero && foregroundHwnd != _hwnd)
            {
                var foregroundThread = NativeMethods.GetWindowThreadProcessId(foregroundHwnd, out _);
                var currentThread = NativeMethods.GetCurrentThreadId();

                if (foregroundThread != currentThread)
                {
                    // Temporarily attach to foreground thread to gain Z-order control
                    NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);

                    // Now we can manipulate Z-order more reliably
                    NativeMethods.SetWindowPos(
                        _hwnd,
                        NativeMethods.HWND_TOPMOST,
                        0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW
                    );

                    NativeMethods.BringWindowToTop(_hwnd);

                    // Detach from foreground thread
                    NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
                    return;
                }
            }

            // Fallback: cycle topmost state to force Z-order recalculation
            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_NOTOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE
            );

            NativeMethods.BringWindowToTop(_hwnd);

            NativeMethods.SetWindowPos(
                _hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW
            );
        }
    }

    private void ApplyTopmostState(bool topmost)
    {
        NativeMethods.SetWindowPos(
            _hwnd,
            topmost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE
        );
    }

    public void BeginFrame(bool captureScreen = true)
    {
        // Only capture screen if effects need it
        if (captureScreen)
        {
            _renderContext.CaptureScreen();
        }
        _swapChain.BeginFrame();
    }

    public void EndFrame()
    {
        _swapChain.Present();
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        if (width == Bounds.Width && height == Bounds.Height) return;

        Bounds = new Rectangle(Bounds.X, Bounds.Y, width, height);
        _swapChain.Resize(width, height);
        _renderContext.UpdateViewportSize(width, height);
    }

    /// <summary>
    /// Capture the current frame content to a bitmap.
    /// Used for screen capture functionality to blend overlay with screen.
    /// </summary>
    public Bitmap? CaptureFrame()
    {
        return _swapChain.CaptureFrame();
    }

    private nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case NativeMethods.WM_NCHITTEST:
                // Return HTTRANSPARENT to make clicks pass through to windows below
                return NativeMethods.HTTRANSPARENT;

            case NativeMethods.WM_WINDOWPOSCHANGING:
                // Intercept Z-order changes to maintain topmost status
                if (_isTopmost && !_topmostSuspended)
                {
                    HandleWindowPosChanging(lParam);
                }
                break;

            case NativeMethods.WM_DESTROY:
                NativeMethods.PostQuitMessage(0);
                return nint.Zero;

            case NativeMethods.WM_DISPLAYCHANGE:
                // Handle display settings change
                OnDisplayChange();
                return nint.Zero;
        }

        return NativeMethods.DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private static void HandleWindowPosChanging(nint lParam)
    {
        // Get the WINDOWPOS structure
        var pos = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);

        // If something is trying to change our Z-order and we should stay topmost
        if ((pos.flags & NativeMethods.SWP_NOZORDER) == 0)
        {
            // Check if we're being moved below HWND_TOPMOST
            if (pos.hwndInsertAfter != NativeMethods.HWND_TOPMOST &&
                pos.hwndInsertAfter != NativeMethods.HWND_TOP)
            {
                // Force HWND_TOPMOST and set NOZORDER to prevent the change
                pos.hwndInsertAfter = NativeMethods.HWND_TOPMOST;
                Marshal.StructureToPtr(pos, lParam, false);
            }
        }
    }

    private void OnDisplayChange()
    {
        // Could be called through OverlayManager for coordinated updates
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _renderContext.Dispose();
        _swapChain.Dispose();
        _graphicsDevice.Dispose();

        if (_hwnd != nint.Zero)
        {
            NativeMethods.DestroyWindow(_hwnd);
        }
    }
}
