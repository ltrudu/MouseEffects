using System.Numerics;

namespace MouseEffects.Core.Input;

/// <summary>
/// Represents the current state of the mouse.
/// </summary>
public readonly struct MouseState
{
    /// <summary>Current mouse position in screen coordinates.</summary>
    public Vector2 Position { get; init; }

    /// <summary>Previous mouse position.</summary>
    public Vector2 PreviousPosition { get; init; }

    /// <summary>Mouse velocity (pixels per second).</summary>
    public Vector2 Velocity { get; init; }

    /// <summary>Buttons currently held down.</summary>
    public MouseButtons ButtonsDown { get; init; }

    /// <summary>Buttons pressed this frame.</summary>
    public MouseButtons ButtonsPressed { get; init; }

    /// <summary>Buttons released this frame.</summary>
    public MouseButtons ButtonsReleased { get; init; }

    /// <summary>Scroll wheel delta.</summary>
    public int ScrollDelta { get; init; }

    /// <summary>Timestamp when this state was captured.</summary>
    public TimeSpan Timestamp { get; init; }

    /// <summary>Check if a button is currently down.</summary>
    public bool IsButtonDown(MouseButtons button) => (ButtonsDown & button) != 0;

    /// <summary>Check if a button was just pressed this frame.</summary>
    public bool IsButtonPressed(MouseButtons button) => (ButtonsPressed & button) != 0;

    /// <summary>Check if a button was just released this frame.</summary>
    public bool IsButtonReleased(MouseButtons button) => (ButtonsReleased & button) != 0;

    /// <summary>Get the movement delta since last frame.</summary>
    public Vector2 Delta => Position - PreviousPosition;

    /// <summary>Get the speed of mouse movement.</summary>
    public float Speed => Velocity.Length();
}

/// <summary>
/// Mouse button flags.
/// </summary>
[Flags]
public enum MouseButtons
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
    XButton1 = 8,
    XButton2 = 16
}
