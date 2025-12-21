namespace MouseEffects.Core.Input;

/// <summary>
/// Interface for effects that want to consume/block mouse clicks from reaching the desktop.
/// When these properties return true, the corresponding button clicks are intercepted and not passed
/// to the underlying windows/desktop.
/// </summary>
public interface IClickConsumer
{
    /// <summary>
    /// Whether left clicks should currently be consumed (blocked from desktop).
    /// This is checked on every click, so it can change dynamically based on game state.
    /// </summary>
    bool ShouldConsumeClicks { get; }

    /// <summary>
    /// Whether right clicks should currently be consumed (blocked from desktop).
    /// This is checked on every click, so it can change dynamically based on game state.
    /// Default implementation returns false for backwards compatibility.
    /// </summary>
    bool ShouldConsumeRightClicks => false;
}
