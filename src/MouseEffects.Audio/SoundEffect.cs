using MouseEffects.Core.Audio;
using NAudio.Wave;

namespace MouseEffects.Audio;

/// <summary>
/// Represents a loaded sound effect backed by NAudio.
/// </summary>
public sealed class SoundEffect : ISoundEffect
{
    private readonly byte[] _audioData;
    private readonly WaveFormat _waveFormat;
    private bool _disposed;

    public string Name { get; }
    public TimeSpan Duration { get; }
    public bool IsValid => !_disposed && _audioData.Length > 0;

    internal byte[] AudioData => _audioData;
    internal WaveFormat WaveFormat => _waveFormat;

    internal SoundEffect(string name, byte[] audioData, WaveFormat waveFormat, TimeSpan duration)
    {
        Name = name;
        _audioData = audioData;
        _waveFormat = waveFormat;
        Duration = duration;
    }

    /// <summary>
    /// Create a SoundEffect from a WAV stream.
    /// </summary>
    public static SoundEffect? FromStream(Stream stream, string name)
    {
        try
        {
            using var reader = new WaveFileReader(stream);
            var format = reader.WaveFormat;

            // Read all audio data into memory
            var audioData = new byte[reader.Length];
            int bytesRead = reader.Read(audioData, 0, audioData.Length);
            if (bytesRead < audioData.Length)
            {
                Array.Resize(ref audioData, bytesRead);
            }

            var duration = reader.TotalTime;
            return new SoundEffect(name, audioData, format, duration);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Create a SoundEffect from raw WAV bytes.
    /// </summary>
    public static SoundEffect? FromBytes(byte[] wavData, string name)
    {
        try
        {
            using var stream = new MemoryStream(wavData);
            return FromStream(stream, name);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Audio data is managed, no explicit disposal needed
    }
}
