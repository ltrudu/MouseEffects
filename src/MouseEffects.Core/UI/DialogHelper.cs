namespace MouseEffects.Core.UI;

/// <summary>
/// Global helper for showing dialogs by temporarily suspending the overlay's topmost state.
/// This allows dialogs to appear above the overlay without being topmost themselves.
/// Initialize this from the main application during startup.
/// </summary>
public static class DialogHelper
{
    private static Action? _suspendOverlayTopmost;
    private static Action? _resumeOverlayTopmost;

    /// <summary>
    /// Initialize the helper with actions to suspend/resume overlay topmost.
    /// Call this during application initialization.
    /// </summary>
    public static void Initialize(Action suspendOverlayTopmost, Action resumeOverlayTopmost)
    {
        _suspendOverlayTopmost = suspendOverlayTopmost;
        _resumeOverlayTopmost = resumeOverlayTopmost;
    }

    /// <summary>
    /// Temporarily suspend overlay topmost state.
    /// Call ResumeOverlayTopmost() when done showing dialogs.
    /// </summary>
    public static void SuspendOverlayTopmost()
    {
        _suspendOverlayTopmost?.Invoke();
    }

    /// <summary>
    /// Resume overlay topmost state after dialogs are closed.
    /// </summary>
    public static void ResumeOverlayTopmost()
    {
        _resumeOverlayTopmost?.Invoke();
    }

    /// <summary>
    /// Execute an action with overlay topmost temporarily suspended.
    /// Automatically resumes topmost state when done.
    /// </summary>
    public static void WithSuspendedTopmost(Action action)
    {
        _suspendOverlayTopmost?.Invoke();
        try
        {
            action();
        }
        finally
        {
            _resumeOverlayTopmost?.Invoke();
        }
    }

    /// <summary>
    /// Execute a function with overlay topmost temporarily suspended.
    /// Automatically resumes topmost state when done.
    /// </summary>
    public static T WithSuspendedTopmost<T>(Func<T> func)
    {
        _suspendOverlayTopmost?.Invoke();
        try
        {
            return func();
        }
        finally
        {
            _resumeOverlayTopmost?.Invoke();
        }
    }
}
