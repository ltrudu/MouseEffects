namespace MouseEffects.Core.Input;

/// <summary>
/// Provides mouse input state.
/// </summary>
public interface IMouseInputProvider : IDisposable
{
    /// <summary>Get the current mouse state.</summary>
    MouseState CurrentState { get; }

    /// <summary>Start capturing mouse input.</summary>
    void Start();

    /// <summary>Stop capturing mouse input.</summary>
    void Stop();

    /// <summary>Whether input capture is currently active.</summary>
    bool IsCapturing { get; }

    /// <summary>Event fired when mouse moves.</summary>
    event EventHandler<MouseMoveEventArgs>? MouseMove;

    /// <summary>Event fired when mouse button is pressed.</summary>
    event EventHandler<MouseButtonEventArgs>? MouseDown;

    /// <summary>Event fired when mouse button is released.</summary>
    event EventHandler<MouseButtonEventArgs>? MouseUp;

    /// <summary>Event fired when mouse wheel is scrolled.</summary>
    event EventHandler<MouseWheelEventArgs>? MouseWheel;
}

/// <summary>
/// Mouse move event arguments.
/// </summary>
public class MouseMoveEventArgs : EventArgs
{
    public required System.Numerics.Vector2 Position { get; init; }
    public required System.Numerics.Vector2 Delta { get; init; }
    public required TimeSpan Timestamp { get; init; }
}

/// <summary>
/// Mouse button event arguments.
/// </summary>
public class MouseButtonEventArgs : EventArgs
{
    public required System.Numerics.Vector2 Position { get; init; }
    public required MouseButtons Button { get; init; }
    public required TimeSpan Timestamp { get; init; }
}

/// <summary>
/// Mouse wheel event arguments.
/// </summary>
public class MouseWheelEventArgs : EventArgs
{
    public required System.Numerics.Vector2 Position { get; init; }
    public required int Delta { get; init; }
    public required TimeSpan Timestamp { get; init; }
}
