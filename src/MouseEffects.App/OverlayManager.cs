using System.Drawing;
using System.Runtime.InteropServices;
using MouseEffects.Core.Diagnostics;
using MouseEffects.DirectX.Graphics;
using MouseEffects.Overlay;

namespace MouseEffects.App;

/// <summary>
/// Manages overlay windows for each monitor.
/// </summary>
public sealed partial class OverlayManager : IDisposable
{
    private readonly List<OverlayWindow> _overlays = [];
    private readonly D3D11GraphicsDevice _sharedDevice;
    private readonly bool _hdrEnabled;
    private readonly float _hdrPeakBrightness;
    private bool _disposed;

    public IReadOnlyList<OverlayWindow> Overlays => _overlays;
    public D3D11GraphicsDevice SharedDevice => _sharedDevice;
    public bool IsHdrEnabled => _hdrEnabled;
    public float HdrPeakBrightness => _hdrPeakBrightness;
    public bool IsHdrSupported => _sharedDevice.IsHdrSupported();

    public OverlayManager(string? preferredGpu = null, bool hdrEnabled = false, float hdrPeakBrightness = 4.0f)
    {
        Log($"Creating D3D11GraphicsDevice (preferred GPU: {preferredGpu ?? "auto"})...");
        _sharedDevice = new D3D11GraphicsDevice(preferredGpu);
        Log($"D3D11GraphicsDevice created successfully using: {_sharedDevice.AdapterName}");

        // Only enable HDR if requested AND supported
        _hdrEnabled = hdrEnabled && _sharedDevice.IsHdrSupported();
        _hdrPeakBrightness = _hdrEnabled ? hdrPeakBrightness : 1.0f;
        if (hdrEnabled && !_hdrEnabled)
        {
            Log("HDR requested but not supported on this display");
        }
        else if (_hdrEnabled)
        {
            Log($"HDR mode enabled (peak brightness: {_hdrPeakBrightness}x)");
        }
    }

    public string CurrentGpuName => _sharedDevice.AdapterName;

    /// <summary>
    /// Create overlay windows for all monitors.
    /// </summary>
    public void Initialize()
    {
        Log("Getting monitors...");
        var monitors = GetAllMonitors();

        if (monitors.Count == 0)
        {
            throw new InvalidOperationException("No monitors detected");
        }

        Log($"Found {monitors.Count} monitor(s)");

        foreach (var monitor in monitors)
        {
            Log($"Creating overlay for monitor: X={monitor.X}, Y={monitor.Y}, W={monitor.Width}, H={monitor.Height}");

            if (monitor.Width <= 0 || monitor.Height <= 0)
            {
                Log($"Skipping invalid monitor bounds");
                continue;
            }

            try
            {
                var overlay = new OverlayWindow(monitor, _sharedDevice, _hdrEnabled, _hdrPeakBrightness);
                _overlays.Add(overlay);
                Log($"Overlay created successfully (HDR: {_hdrEnabled})");
            }
            catch (Exception ex)
            {
                Log($"Failed to create overlay: {ex.Message}");
                throw;
            }
        }

        if (_overlays.Count == 0)
        {
            throw new InvalidOperationException("Failed to create any overlay windows");
        }
    }

    private static void Log(string message) => Logger.Log("OverlayManager", message);

    /// <summary>
    /// Recreate overlays when display configuration changes.
    /// </summary>
    public void RefreshMonitors()
    {
        // Dispose existing overlays
        foreach (var overlay in _overlays)
        {
            overlay.Dispose();
        }
        _overlays.Clear();

        // Recreate for new configuration
        Initialize();
    }

    private bool _topmostEnforcementSuspended;

    /// <summary>
    /// Gets whether topmost enforcement is currently suspended.
    /// </summary>
    public bool IsTopmostEnforcementSuspended => _topmostEnforcementSuspended;

    /// <summary>
    /// Suspend topmost enforcement to allow dialogs to appear above the overlay.
    /// The overlay keeps its topmost state, but EnforceTopmost() becomes a no-op.
    /// </summary>
    public void SuspendTopmostEnforcement()
    {
        _topmostEnforcementSuspended = true;
        Log("Topmost enforcement suspended for dialogs");
    }

    /// <summary>
    /// Resume topmost enforcement after dialogs are closed.
    /// </summary>
    public void ResumeTopmostEnforcement()
    {
        _topmostEnforcementSuspended = false;
        Log("Topmost enforcement resumed");

        // Immediately enforce topmost to reclaim position
        foreach (var overlay in _overlays)
        {
            overlay.EnforceTopmost();
        }
    }

    /// <summary>
    /// Temporarily suspend topmost state for all overlays to allow modal dialogs to appear.
    /// </summary>
    public void SuspendTopmost()
    {
        foreach (var overlay in _overlays)
        {
            overlay.SuspendTopmost();
        }
    }

    /// <summary>
    /// Resume topmost state for all overlays after modal dialog is closed.
    /// </summary>
    public void ResumeTopmost()
    {
        foreach (var overlay in _overlays)
        {
            overlay.ResumeTopmost();
        }
    }

    /// <summary>
    /// Re-apply topmost state for all overlays. Call periodically to ensure overlays stay on top.
    /// Skipped if topmost enforcement is suspended (dialogs are open).
    /// </summary>
    public void EnforceTopmost()
    {
        if (_topmostEnforcementSuspended) return;

        foreach (var overlay in _overlays)
        {
            overlay.EnforceTopmost();
        }
    }

    /// <summary>
    /// Force topmost state for all overlays regardless of suspension state.
    /// Use this for manual user-triggered topmost enforcement via hotkey.
    /// </summary>
    public void ForceTopmost()
    {
        foreach (var overlay in _overlays)
        {
            overlay.EnforceTopmost();
        }
        Log("Force topmost applied to all overlays");
    }

    private static List<Rectangle> GetAllMonitors()
    {
        var monitors = new List<Rectangle>();

        EnumDisplayMonitors(nint.Zero, nint.Zero,
            (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
            {
                var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfoW(hMonitor, ref info))
                {
                    monitors.Add(new Rectangle(
                        info.rcMonitor.left,
                        info.rcMonitor.top,
                        info.rcMonitor.right - info.rcMonitor.left,
                        info.rcMonitor.bottom - info.rcMonitor.top));
                }
                return true;
            },
            nint.Zero);

        // Fallback to primary screen if enumeration fails
        if (monitors.Count == 0)
        {
            monitors.Add(new Rectangle(
                0, 0,
                GetSystemMetrics(SM_CXSCREEN),
                GetSystemMetrics(SM_CYSCREEN)));
        }

        return monitors;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var overlay in _overlays)
        {
            overlay.Dispose();
        }
        _overlays.Clear();

        _sharedDevice.Dispose();
    }

    #region Native Methods

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, nint lprcMonitor, nint dwData);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfoW(nint hMonitor, ref MONITORINFO lpmi);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int nIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    #endregion
}
