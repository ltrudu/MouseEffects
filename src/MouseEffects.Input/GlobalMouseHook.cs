using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Input;
using MouseEffects.Input.Win32;

namespace MouseEffects.Input;

/// <summary>
/// Global low-level mouse hook that captures mouse events system-wide.
/// Implements IMouseInputProvider for use with effects.
/// </summary>
public sealed class GlobalMouseHook : IMouseInputProvider
{
    private readonly MouseHookNativeMethods.LowLevelMouseProc _hookCallback;
    private readonly Stopwatch _stopwatch;
    private readonly object _stateLock = new();
    private nint _hookHandle;
    private bool _disposed;

    private Vector2 _currentPosition;
    private Vector2 _previousPosition;
    private Vector2 _velocity;
    private MouseButtons _buttonsDown;
    private MouseButtons _buttonsPressed;
    private MouseButtons _buttonsReleased;
    private int _scrollDelta;
    private DateTime _lastMoveTime;

    /// <summary>
    /// Optional click consumer that can block clicks from reaching the desktop.
    /// </summary>
    private IClickConsumer? _clickConsumer;

    /// <summary>
    /// Tracks whether we consumed the left mouse-down event.
    /// We only consume mouse-up if we consumed the corresponding mouse-down.
    /// </summary>
    private bool _consumedLeftButtonDown;

    public bool IsCapturing => _hookHandle != nint.Zero;

    /// <summary>
    /// Register a click consumer that can block clicks from reaching the desktop.
    /// Only one consumer can be active at a time.
    /// </summary>
    public void SetClickConsumer(IClickConsumer? consumer)
    {
        _clickConsumer = consumer;
        _consumedLeftButtonDown = false; // Reset tracking when consumer changes
    }

    public MouseState CurrentState
    {
        get
        {
            // Poll cursor position directly to ensure we have the latest position
            // even during window dragging when WM_MOUSEMOVE isn't received
            PollCursorPosition();

            lock (_stateLock)
            {
                return new MouseState
                {
                    Position = _currentPosition,
                    PreviousPosition = _previousPosition,
                    Velocity = _velocity,
                    ButtonsDown = _buttonsDown,
                    ButtonsPressed = _buttonsPressed,
                    ButtonsReleased = _buttonsReleased,
                    ScrollDelta = _scrollDelta,
                    Timestamp = _stopwatch.Elapsed
                };
            }
        }
    }

    /// <summary>
    /// Polls the current cursor position using GetCursorPos.
    /// This ensures we have up-to-date position even during window dragging.
    /// </summary>
    private void PollCursorPosition()
    {
        if (MouseHookNativeMethods.GetCursorPos(out var pt))
        {
            var newPosition = new Vector2(pt.x, pt.y);

            lock (_stateLock)
            {
                if (_currentPosition != newPosition)
                {
                    var now = DateTime.UtcNow;
                    var dt = (float)(now - _lastMoveTime).TotalSeconds;
                    _lastMoveTime = now;

                    _previousPosition = _currentPosition;
                    _currentPosition = newPosition;

                    if (dt > 0.0001f)
                    {
                        var delta = _currentPosition - _previousPosition;
                        _velocity = delta / dt;
                    }
                }
            }
        }
    }

    public event EventHandler<MouseMoveEventArgs>? MouseMove;
    public event EventHandler<MouseButtonEventArgs>? MouseDown;
    public event EventHandler<MouseButtonEventArgs>? MouseUp;
    public event EventHandler<MouseWheelEventArgs>? MouseWheel;

    public GlobalMouseHook()
    {
        _hookCallback = HookCallback;
        _stopwatch = Stopwatch.StartNew();
        _lastMoveTime = DateTime.UtcNow;

        // Get initial position
        if (MouseHookNativeMethods.GetCursorPos(out var pt))
        {
            _currentPosition = new Vector2(pt.x, pt.y);
            _previousPosition = _currentPosition;
        }
    }

    public void Start()
    {
        if (_hookHandle != nint.Zero) return;

        var moduleHandle = MouseHookNativeMethods.GetModuleHandleW(null);
        _hookHandle = MouseHookNativeMethods.SetWindowsHookExW(
            MouseHookNativeMethods.WH_MOUSE_LL,
            _hookCallback,
            moduleHandle,
            0);

        if (_hookHandle == nint.Zero)
        {
            throw new InvalidOperationException(
                $"Failed to install mouse hook. Error: {Marshal.GetLastWin32Error()}");
        }
    }

    public void Stop()
    {
        if (_hookHandle == nint.Zero) return;

        MouseHookNativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = nint.Zero;
    }

    /// <summary>
    /// Call at the end of each frame to clear per-frame button state.
    /// </summary>
    public void EndFrame()
    {
        lock (_stateLock)
        {
            _buttonsPressed = MouseButtons.None;
            _buttonsReleased = MouseButtons.None;
            _scrollDelta = 0;
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        bool shouldBlockClick = false;

        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<MouseHookNativeMethods.MSLLHOOKSTRUCT>(lParam);
            var messageId = (int)wParam;
            var timestamp = _stopwatch.Elapsed;

            // Check if we should consume left button down
            if (messageId == MouseHookNativeMethods.WM_LBUTTONDOWN)
            {
                if (_clickConsumer?.ShouldConsumeClicks == true)
                {
                    shouldBlockClick = true;
                    _consumedLeftButtonDown = true;
                }
                else
                {
                    _consumedLeftButtonDown = false;
                }
            }
            // Only consume mouse-up if we consumed the corresponding mouse-down
            else if (messageId == MouseHookNativeMethods.WM_LBUTTONUP)
            {
                if (_consumedLeftButtonDown)
                {
                    shouldBlockClick = true;
                }
                _consumedLeftButtonDown = false;
            }

            switch (messageId)
            {
                case MouseHookNativeMethods.WM_MOUSEMOVE:
                    ProcessMouseMove(hookStruct.pt.x, hookStruct.pt.y, timestamp);
                    break;

                case MouseHookNativeMethods.WM_LBUTTONDOWN:
                    ProcessButtonDown(MouseButtons.Left, hookStruct.pt.x, hookStruct.pt.y, timestamp);
                    break;

                case MouseHookNativeMethods.WM_LBUTTONUP:
                    ProcessButtonUp(MouseButtons.Left, hookStruct.pt.x, hookStruct.pt.y, timestamp);
                    break;

                case MouseHookNativeMethods.WM_RBUTTONDOWN:
                    ProcessButtonDown(MouseButtons.Right, hookStruct.pt.x, hookStruct.pt.y, timestamp);
                    break;

                case MouseHookNativeMethods.WM_RBUTTONUP:
                    ProcessButtonUp(MouseButtons.Right, hookStruct.pt.x, hookStruct.pt.y, timestamp);
                    break;

                case MouseHookNativeMethods.WM_MBUTTONDOWN:
                    ProcessButtonDown(MouseButtons.Middle, hookStruct.pt.x, hookStruct.pt.y, timestamp);
                    break;

                case MouseHookNativeMethods.WM_MBUTTONUP:
                    ProcessButtonUp(MouseButtons.Middle, hookStruct.pt.x, hookStruct.pt.y, timestamp);
                    break;

                case MouseHookNativeMethods.WM_MOUSEWHEEL:
                    ProcessMouseWheel(hookStruct.pt.x, hookStruct.pt.y, hookStruct.mouseData, timestamp);
                    break;
            }
        }

        // If click should be blocked, return 1 to prevent it from reaching other applications
        if (shouldBlockClick)
        {
            return (nint)1;
        }

        return MouseHookNativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void ProcessMouseMove(int x, int y, TimeSpan timestamp)
    {
        Vector2 delta;

        lock (_stateLock)
        {
            var now = DateTime.UtcNow;
            var dt = (float)(now - _lastMoveTime).TotalSeconds;
            _lastMoveTime = now;

            _previousPosition = _currentPosition;
            _currentPosition = new Vector2(x, y);
            delta = _currentPosition - _previousPosition;

            if (dt > 0.0001f)
            {
                _velocity = delta / dt;
            }
        }

        MouseMove?.Invoke(this, new MouseMoveEventArgs
        {
            Position = new Vector2(x, y),
            Delta = delta,
            Timestamp = timestamp
        });
    }

    private void ProcessButtonDown(MouseButtons button, int x, int y, TimeSpan timestamp)
    {
        lock (_stateLock)
        {
            _buttonsDown |= button;
            _buttonsPressed |= button;
        }

        MouseDown?.Invoke(this, new MouseButtonEventArgs
        {
            Position = new Vector2(x, y),
            Button = button,
            Timestamp = timestamp
        });
    }

    private void ProcessButtonUp(MouseButtons button, int x, int y, TimeSpan timestamp)
    {
        lock (_stateLock)
        {
            _buttonsDown &= ~button;
            _buttonsReleased |= button;
        }

        MouseUp?.Invoke(this, new MouseButtonEventArgs
        {
            Position = new Vector2(x, y),
            Button = button,
            Timestamp = timestamp
        });
    }

    private void ProcessMouseWheel(int x, int y, uint mouseData, TimeSpan timestamp)
    {
        var delta = (short)(mouseData >> 16);

        lock (_stateLock)
        {
            _scrollDelta += delta;
        }

        MouseWheel?.Invoke(this, new MouseWheelEventArgs
        {
            Position = new Vector2(x, y),
            Delta = delta,
            Timestamp = timestamp
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
    }
}
