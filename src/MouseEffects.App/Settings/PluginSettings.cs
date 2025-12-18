using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using MouseEffects.Core.Diagnostics;
using MouseEffects.Core.Effects;

namespace MouseEffects.App.Settings;

/// <summary>
/// Settings for a single plugin, stored in its own file.
/// Each plugin has a separate JSON file: %APPDATA%/MouseEffects/plugins/{effectId}.json
/// </summary>
public class PluginSettings
{
    private static readonly string PluginsSettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MouseEffects",
        "plugins");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new Vector4JsonConverter() }
    };

    /// <summary>
    /// Plugin configuration settings as key-value pairs.
    /// </summary>
    public Dictionary<string, JsonElement> Configuration { get; set; } = new();

    /// <summary>
    /// Load settings for a specific plugin.
    /// </summary>
    public static PluginSettings Load(string effectId)
    {
        var filePath = GetSettingsPath(effectId);

        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<PluginSettings>(json, JsonOptions);
                if (settings != null)
                {
                    Logger.Log("PluginSettings", $"Loaded settings for '{effectId}': {settings.Configuration.Count} config values");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("PluginSettings", $"Error loading settings for '{effectId}': {ex.Message}");
        }

        return new PluginSettings();
    }

    /// <summary>
    /// Save settings for a specific plugin.
    /// </summary>
    public void Save(string effectId)
    {
        var filePath = GetSettingsPath(effectId);

        try
        {
            if (!Directory.Exists(PluginsSettingsDir))
            {
                Directory.CreateDirectory(PluginsSettingsDir);
            }

            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(filePath, json);
            Logger.Log("PluginSettings", $"Saved settings for '{effectId}'");
        }
        catch (Exception ex)
        {
            Logger.Log("PluginSettings", $"Error saving settings for '{effectId}': {ex.Message}");
        }
    }

    /// <summary>
    /// Get the file path for a plugin's settings.
    /// </summary>
    private static string GetSettingsPath(string effectId)
    {
        // Sanitize the effect ID for use as a filename
        var safeFileName = string.Join("_", effectId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(PluginsSettingsDir, $"{safeFileName}.json");
    }

    /// <summary>
    /// Apply the saved configuration to an effect.
    /// </summary>
    public void ApplyToEffect(IEffect effect)
    {
        // Apply configuration if we have any saved settings
        if (Configuration.Count > 0)
        {
            // Start with the effect's current config and merge saved values
            var config = effect.Configuration.Clone();

            foreach (var kvp in Configuration)
            {
                try
                {
                    var value = DeserializeValue(kvp.Value);
                    if (value != null)
                    {
                        config.Set(kvp.Key, value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("PluginSettings", $"Error applying config key '{kvp.Key}': {ex.Message}");
                }
            }

            effect.Configure(config);
        }
    }

    /// <summary>
    /// Save the effect's current configuration to this settings object.
    /// </summary>
    public void SaveFromEffect(IEffect effect)
    {
        Configuration.Clear();

        foreach (var kvp in effect.Configuration.GetAll())
        {
            try
            {
                var jsonValue = SerializeValue(kvp.Value);
                Configuration[kvp.Key] = jsonValue;
            }
            catch (Exception ex)
            {
                Logger.Log("PluginSettings", $"Error serializing config key '{kvp.Key}': {ex.Message}");
            }
        }
    }

    private static JsonElement SerializeValue(object value)
    {
        var json = value switch
        {
            Vector4 v => JsonSerializer.Serialize(v, JsonOptions),
            _ => JsonSerializer.Serialize(value, value.GetType(), JsonOptions)
        };

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static object? DeserializeValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetSingle(out var f) => f,
            JsonValueKind.Number when element.TryGetDouble(out var d) => (float)d,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Object when TryDeserializeVector4(element, out var v) => v,
            _ => null
        };
    }

    private static bool TryDeserializeVector4(JsonElement element, out Vector4 vector)
    {
        vector = default;

        if (element.TryGetProperty("X", out var x) &&
            element.TryGetProperty("Y", out var y) &&
            element.TryGetProperty("Z", out var z) &&
            element.TryGetProperty("W", out var w))
        {
            vector = new Vector4(
                x.GetSingle(),
                y.GetSingle(),
                z.GetSingle(),
                w.GetSingle());
            return true;
        }

        return false;
    }
}

/// <summary>
/// JSON converter for Vector4 (used for colors).
/// </summary>
public class Vector4JsonConverter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        return new Vector4(
            root.GetProperty("X").GetSingle(),
            root.GetProperty("Y").GetSingle(),
            root.GetProperty("Z").GetSingle(),
            root.GetProperty("W").GetSingle());
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteNumber("W", value.W);
        writer.WriteEndObject();
    }
}
