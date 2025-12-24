namespace MouseEffects.Core.Audio;

/// <summary>
/// Provides audio playback functionality for sound effects.
/// </summary>
public interface IAudioProvider : IDisposable
{
    /// <summary>
    /// Load a sound effect from an embedded resource or file path.
    /// </summary>
    /// <param name="resourcePath">Path to the sound resource (e.g., "Sounds/explosion.wav")</param>
    /// <returns>The loaded sound effect, or null if loading failed.</returns>
    ISoundEffect? LoadSound(string resourcePath);

    /// <summary>
    /// Load a sound effect from raw WAV data.
    /// </summary>
    /// <param name="wavData">Raw WAV file bytes.</param>
    /// <param name="name">Name for the sound effect.</param>
    /// <returns>The loaded sound effect, or null if loading failed.</returns>
    ISoundEffect? LoadSound(byte[] wavData, string name);

    /// <summary>
    /// Load a sound effect from a stream.
    /// </summary>
    /// <param name="stream">Stream containing WAV data.</param>
    /// <param name="name">Name for the sound effect.</param>
    /// <returns>The loaded sound effect, or null if loading failed.</returns>
    ISoundEffect? LoadSound(Stream stream, string name);

    /// <summary>
    /// Play a one-shot sound effect.
    /// </summary>
    /// <param name="sound">The sound effect to play.</param>
    /// <param name="volume">Volume multiplier (0.0 to 1.0).</param>
    /// <param name="pitch">Pitch multiplier (0.5 to 2.0, 1.0 = normal).</param>
    void PlayOneShot(ISoundEffect sound, float volume = 1.0f, float pitch = 1.0f);

    /// <summary>
    /// Global master volume (0.0 to 1.0).
    /// </summary>
    float MasterVolume { get; set; }

    /// <summary>
    /// Enable or disable all audio playback.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Whether the audio provider is properly initialized and functional.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Maximum number of concurrent sounds that can play simultaneously.
    /// </summary>
    int MaxConcurrentSounds { get; }
}
