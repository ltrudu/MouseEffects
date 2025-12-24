namespace MouseEffects.Core.Audio;

/// <summary>
/// Represents a loaded sound effect that can be played.
/// </summary>
public interface ISoundEffect : IDisposable
{
    /// <summary>
    /// Name or identifier of the sound effect.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Duration of the sound effect.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Whether this sound effect is valid and can be played.
    /// </summary>
    bool IsValid { get; }
}
