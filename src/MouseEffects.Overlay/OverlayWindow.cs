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

    public OverlayWindow(Rectangle bounds, D3D11GraphicsDevice? sharedDevice = null)
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
        _swapChain = new SwapChainManager(_graphicsDevice, _hwnd, bounds.Width, bounds.Height);
        _renderContext = new D3D11RenderContext(_graphicsDevice, bounds.Width, bounds.Height);

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

    public void SetAlwaysOnTop(bool enable)
    {
        NativeMethods.SetWindowPos(
            _hwnd,
            enable ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE
        );
    }

    public void BeginFrame()
    {
        // Capture screen before rendering effects that need it
        _renderContext.CaptureScreen();
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

    private nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case NativeMethods.WM_NCHITTEST:
                // Return HTTRANSPARENT to make clicks pass through to windows below
                return NativeMethods.HTTRANSPARENT;

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
