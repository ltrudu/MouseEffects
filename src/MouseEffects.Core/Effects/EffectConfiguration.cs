using System.Numerics;

namespace MouseEffects.Core.Effects;

/// <summary>
/// Configuration storage for effects.
/// </summary>
public class EffectConfiguration
{
    private readonly Dictionary<string, object> _values = new();

    /// <summary>Get a configuration value.</summary>
    public T Get<T>(string key, T defaultValue = default!)
    {
        if (_values.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }

    /// <summary>Set a configuration value.</summary>
    public void Set<T>(string key, T value)
    {
        _values[key] = value!;
    }

    /// <summary>Try to get a configuration value.</summary>
    public bool TryGet<T>(string key, out T value)
    {
        if (_values.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>Get all configuration values.</summary>
    public IReadOnlyDictionary<string, object> GetAll() => _values;

    /// <summary>Clone this configuration.</summary>
    public EffectConfiguration Clone()
    {
        var clone = new EffectConfiguration();
        foreach (var kvp in _values)
            clone._values[kvp.Key] = kvp.Value;
        return clone;
    }

    /// <summary>Clear all values.</summary>
    public void Clear() => _values.Clear();

    /// <summary>Check if a key exists.</summary>
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <summary>Remove a key.</summary>
    public bool Remove(string key) => _values.Remove(key);
}

/// <summary>
/// Schema for effect configuration (for UI generation).
/// </summary>
public sealed class EffectConfigurationSchema
{
    public required IReadOnlyList<ConfigurationParameter> Parameters { get; init; }
}

/// <summary>
/// Base class for configuration parameters.
/// </summary>
public abstract record ConfigurationParameter
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? Group { get; init; }
}

/// <summary>Float parameter with range.</summary>
public sealed record FloatParameter : ConfigurationParameter
{
    public float MinValue { get; init; }
    public float MaxValue { get; init; } = 1f;
    public float DefaultValue { get; init; }
    public float Step { get; init; } = 0.1f;
}

/// <summary>Integer parameter with range.</summary>
public sealed record IntParameter : ConfigurationParameter
{
    public int MinValue { get; init; }
    public int MaxValue { get; init; } = 100;
    public int DefaultValue { get; init; }
}

/// <summary>Boolean parameter.</summary>
public sealed record BoolParameter : ConfigurationParameter
{
    public bool DefaultValue { get; init; }
}

/// <summary>Color parameter (RGBA).</summary>
public sealed record ColorParameter : ConfigurationParameter
{
    public Vector4 DefaultValue { get; init; } = Vector4.One;
    public bool SupportsAlpha { get; init; } = true;
}

/// <summary>Enum/choice parameter.</summary>
public sealed record ChoiceParameter : ConfigurationParameter
{
    public required IReadOnlyList<string> Choices { get; init; }
    public string DefaultValue { get; init; } = "";
}
