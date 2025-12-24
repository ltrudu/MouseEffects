using System.Collections.Concurrent;
using System.Reflection;
using MouseEffects.Core.Audio;

namespace MouseEffects.Audio;

/// <summary>
/// Audio provider implementation using NAudio.
/// Provides sound effect loading and playback for MouseEffects.
/// </summary>
public sealed class AudioProvider : IAudioProvider
{
    private readonly ConcurrentDictionary<string, SoundEffect> _loadedSounds = new();
    private readonly SoundPool _pool;
    private bool _disposed;
    private bool _enabled = true;

    public bool IsInitialized { get; }
    public int MaxConcurrentSounds => _pool.MaxConcurrentSounds;

    public float MasterVolume
    {
        get => _pool.MasterVolume;
        set => _pool.MasterVolume = value;
    }

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public AudioProvider(int maxConcurrentSounds = 32)
    {
        try
        {
            _pool = new SoundPool(maxConcurrentSounds);
            IsInitialized = true;
        }
        catch
        {
            _pool = new SoundPool(0); // Create a non-functional pool
            IsInitialized = false;
        }
    }

    public ISoundEffect? LoadSound(string resourcePath)
    {
        if (_disposed) return null;

        return _loadedSounds.GetOrAdd(resourcePath, path =>
        {
            // Try to load from embedded resource first
            var stream = TryLoadEmbeddedResource(path);
            if (stream != null)
            {
                using (stream)
                {
                    return SoundEffect.FromStream(stream, path)!;
                }
            }

            // Try to load from file
            if (File.Exists(path))
            {
                using var fileStream = File.OpenRead(path);
                return SoundEffect.FromStream(fileStream, path)!;
            }

            // Return a placeholder that indicates failure
            return null!;
        });
    }

    public ISoundEffect? LoadSound(byte[] wavData, string name)
    {
        if (_disposed) return null;

        return _loadedSounds.GetOrAdd(name, _ =>
            SoundEffect.FromBytes(wavData, name)!);
    }

    public ISoundEffect? LoadSound(Stream stream, string name)
    {
        if (_disposed) return null;

        return _loadedSounds.GetOrAdd(name, _ =>
            SoundEffect.FromStream(stream, name)!);
    }

    public void PlayOneShot(ISoundEffect sound, float volume = 1.0f, float pitch = 1.0f)
    {
        if (_disposed || !_enabled || !IsInitialized) return;
        if (sound is not SoundEffect sfx || !sfx.IsValid) return;

        _pool.Play(sfx, volume, pitch);
    }

    private static Stream? TryLoadEmbeddedResource(string resourcePath)
    {
        // Try calling assembly first (the effect's assembly)
        var callingAssembly = Assembly.GetCallingAssembly();
        var stream = TryLoadFromAssembly(callingAssembly, resourcePath);
        if (stream != null) return stream;

        // Try entry assembly
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            stream = TryLoadFromAssembly(entryAssembly, resourcePath);
            if (stream != null) return stream;
        }

        return null;
    }

    private static Stream? TryLoadFromAssembly(Assembly assembly, string resourcePath)
    {
        // Convert path to resource name format
        var resourceName = resourcePath.Replace('/', '.').Replace('\\', '.');

        // Try exact match
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null) return stream;

        // Try with assembly name prefix
        var assemblyName = assembly.GetName().Name;
        stream = assembly.GetManifestResourceStream($"{assemblyName}.{resourceName}");
        if (stream != null) return stream;

        // Search for partial match
        var resourceNames = assembly.GetManifestResourceNames();
        foreach (var name in resourceNames)
        {
            if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(resourceName, StringComparison.OrdinalIgnoreCase))
            {
                return assembly.GetManifestResourceStream(name);
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _pool.Dispose();

        foreach (var sound in _loadedSounds.Values)
        {
            sound?.Dispose();
        }
        _loadedSounds.Clear();
    }
}
