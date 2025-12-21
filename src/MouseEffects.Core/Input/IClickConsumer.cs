namespace MouseEffects.Core.Input;

/// <summary>
/// Interface for effects that want to consume/block mouse clicks from reaching the desktop.
/// When ShouldConsumeClicks returns true, left button clicks are intercepted and not passed
/// to the underlying windows/desktop.
/// </summary>
public interface IClickConsumer
{
    /// <summary>
    /// Whether clicks should currently be consumed (blocked from desktop).
    /// This is checked on every click, so it can change dynamically based on game state.
    /// </summary>
    bool ShouldConsumeClicks { get; }
}
