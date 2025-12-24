using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MouseEffects.Audio;

/// <summary>
/// Pool of audio output channels for concurrent sound playback.
/// Uses NAudio's mixing capabilities to play multiple sounds simultaneously.
/// </summary>
internal sealed class SoundPool : IDisposable
{
    private readonly MixingSampleProvider _mixer;
    private readonly IWavePlayer _wavePlayer;
    private readonly object _lock = new();
    private bool _disposed;
    private float _masterVolume = 1.0f;

    public int MaxConcurrentSounds { get; }

    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Math.Clamp(value, 0f, 1f);
        }
    }

    public SoundPool(int maxConcurrentSounds = 32)
    {
        MaxConcurrentSounds = maxConcurrentSounds;

        // Create mixer with standard format (44.1kHz stereo)
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };

        // Use WaveOutEvent for reliable background playback
        _wavePlayer = new WaveOutEvent
        {
            DesiredLatency = 100 // Low latency for responsive playback
        };

        try
        {
            _wavePlayer.Init(_mixer);
            _wavePlayer.Play();
        }
        catch
        {
            // Audio device not available, pool will be non-functional
        }
    }

    /// <summary>
    /// Play a sound effect with the specified volume and pitch.
    /// </summary>
    public void Play(SoundEffect sound, float volume, float pitch)
    {
        if (_disposed || sound == null || !sound.IsValid) return;

        lock (_lock)
        {
            try
            {
                // Create a sample provider from the sound effect
                var rawStream = new RawSourceWaveStream(
                    new MemoryStream(sound.AudioData),
                    sound.WaveFormat);

                ISampleProvider sampleProvider = rawStream.ToSampleProvider();

                // Convert to mixer format if needed
                if (sampleProvider.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
                {
                    sampleProvider = new WdlResamplingSampleProvider(sampleProvider, _mixer.WaveFormat.SampleRate);
                }

                // Convert mono to stereo if needed
                if (sampleProvider.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
                {
                    sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
                }

                // Apply volume
                var volumeAdjusted = new VolumeSampleProvider(sampleProvider)
                {
                    Volume = volume * _masterVolume
                };

                // Apply pitch (simple implementation - affects playback speed)
                // For proper pitch shifting without tempo change, would need more complex processing
                ISampleProvider finalProvider = volumeAdjusted;

                if (Math.Abs(pitch - 1.0f) > 0.01f)
                {
                    // Resample to change pitch (also changes tempo)
                    var targetRate = (int)(volumeAdjusted.WaveFormat.SampleRate * pitch);
                    targetRate = Math.Clamp(targetRate, 8000, 192000);

                    // Create intermediate sample provider at modified rate then resample back
                    finalProvider = new PitchShiftingSampleProvider(volumeAdjusted, pitch);
                }

                // Add to mixer
                _mixer.AddMixerInput(finalProvider);
            }
            catch
            {
                // Ignore playback errors
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _wavePlayer.Stop();
            _wavePlayer.Dispose();
        }
    }
}

/// <summary>
/// Simple pitch shifting sample provider that adjusts playback rate.
/// Note: This changes tempo along with pitch. For time-preserving pitch shift,
/// a more complex algorithm would be needed.
/// </summary>
internal sealed class PitchShiftingSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _pitchFactor;
    private float _position;
    private float[] _sourceBuffer;
    private int _sourceBufferValid;
    private int _sourceBufferPosition;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public PitchShiftingSampleProvider(ISampleProvider source, float pitchFactor)
    {
        _source = source;
        _pitchFactor = Math.Clamp(pitchFactor, 0.5f, 2.0f);
        _sourceBuffer = new float[4096];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = 0;

        while (samplesRead < count)
        {
            // Ensure we have source samples
            if (_sourceBufferPosition >= _sourceBufferValid)
            {
                _sourceBufferValid = _source.Read(_sourceBuffer, 0, _sourceBuffer.Length);
                _sourceBufferPosition = 0;

                if (_sourceBufferValid == 0)
                {
                    break; // End of source
                }
            }

            // Calculate how many samples we can process
            int channels = WaveFormat.Channels;
            int framesRemaining = (_sourceBufferValid - _sourceBufferPosition) / channels;
            int outputFramesNeeded = (count - samplesRead) / channels;

            int framesToProcess = Math.Min(framesRemaining, (int)(outputFramesNeeded * _pitchFactor) + 1);

            for (int frame = 0; frame < outputFramesNeeded && _sourceBufferPosition < _sourceBufferValid; frame++)
            {
                int sourceFrame = (int)_position;
                float fraction = _position - sourceFrame;

                for (int ch = 0; ch < channels; ch++)
                {
                    int sourceIndex = _sourceBufferPosition + ch;
                    int nextIndex = sourceIndex + channels;

                    if (nextIndex < _sourceBufferValid)
                    {
                        // Linear interpolation
                        float sample = _sourceBuffer[sourceIndex] * (1 - fraction) +
                                      _sourceBuffer[nextIndex] * fraction;
                        buffer[offset + samplesRead + ch] = sample;
                    }
                    else
                    {
                        buffer[offset + samplesRead + ch] = _sourceBuffer[sourceIndex];
                    }
                }

                _position += _pitchFactor;
                while (_position >= 1.0f)
                {
                    _position -= 1.0f;
                    _sourceBufferPosition += channels;
                }

                samplesRead += channels;
            }
        }

        return samplesRead;
    }
}
