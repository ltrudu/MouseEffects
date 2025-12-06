using System.IO;
using System.Text.Json;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Manages loading, saving, importing, and exporting custom presets.
/// </summary>
public class PresetManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null // Keep PascalCase
    };

    private readonly string _presetsFolder;
    private List<CustomPreset> _customPresets = new();

    public PresetManager()
    {
        var pluginDir = Path.GetDirectoryName(typeof(PresetManager).Assembly.Location);
        _presetsFolder = Path.Combine(pluginDir!, "ColorBlindnessNG_Presets");
        EnsurePresetsFolderExists();
    }

    /// <summary>
    /// Gets the presets folder path.
    /// </summary>
    public string PresetsFolder => _presetsFolder;

    /// <summary>
    /// Gets the loaded custom presets.
    /// </summary>
    public IReadOnlyList<CustomPreset> CustomPresets => _customPresets.AsReadOnly();

    /// <summary>
    /// Ensures the presets folder exists.
    /// </summary>
    private void EnsurePresetsFolderExists()
    {
        if (!Directory.Exists(_presetsFolder))
        {
            Directory.CreateDirectory(_presetsFolder);
        }
    }

    /// <summary>
    /// Loads all custom presets from the presets folder.
    /// </summary>
    public void LoadCustomPresets()
    {
        _customPresets.Clear();
        EnsurePresetsFolderExists();

        var jsonFiles = Directory.GetFiles(_presetsFolder, "*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<CustomPreset>(json, JsonOptions);
                if (preset != null)
                {
                    preset.IsCustom = true; // Ensure it's marked as custom
                    _customPresets.Add(preset);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue loading other presets
                System.Diagnostics.Debug.WriteLine($"Failed to load preset from {file}: {ex.Message}");
            }
        }

        // Sort by name
        _customPresets = _customPresets.OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    /// Saves a custom preset to the presets folder.
    /// </summary>
    public void SaveCustomPreset(CustomPreset preset)
    {
        EnsurePresetsFolderExists();

        preset.IsCustom = true;
        var fileName = preset.GetSafeFileName() + ".json";
        var filePath = Path.Combine(_presetsFolder, fileName);

        var json = JsonSerializer.Serialize(preset, JsonOptions);
        File.WriteAllText(filePath, json);

        // Reload to keep list in sync
        LoadCustomPresets();
    }

    /// <summary>
    /// Deletes a custom preset from the presets folder.
    /// </summary>
    public bool DeleteCustomPreset(string name)
    {
        var preset = _customPresets.FirstOrDefault(p => p.Name == name);
        if (preset == null)
            return false;

        var fileName = preset.GetSafeFileName() + ".json";
        var filePath = Path.Combine(_presetsFolder, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            LoadCustomPresets();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Exports a preset (built-in or custom) to a specified file path.
    /// </summary>
    public void ExportPreset(CorrectionPreset preset, string filePath)
    {
        var customPreset = CustomPreset.FromCorrectionPreset(preset);
        customPreset.IsCustom = true; // Mark as custom when exporting
        customPreset.CreatedDate = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(customPreset, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Exports a custom preset to a specified file path.
    /// </summary>
    public void ExportPreset(CustomPreset preset, string filePath)
    {
        var json = JsonSerializer.Serialize(preset, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Imports a preset from a file. Returns the loaded preset or null if invalid.
    /// </summary>
    public CustomPreset? ImportPresetFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var preset = JsonSerializer.Deserialize<CustomPreset>(json, JsonOptions);
            return preset;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Copies an imported preset to the presets folder with the specified name.
    /// </summary>
    public void SaveImportedPreset(CustomPreset preset, string newName)
    {
        preset.Name = newName;
        preset.IsCustom = true;
        SaveCustomPreset(preset);
    }

    /// <summary>
    /// Checks if a preset with the given name already exists.
    /// </summary>
    public bool PresetExists(string name)
    {
        // Check built-in presets
        if (CorrectionPresets.All.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check custom presets
        return _customPresets.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a custom preset with the given name exists (not built-in).
    /// </summary>
    public bool CustomPresetExists(string name)
    {
        return _customPresets.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a unique name suggestion for a preset (adds number suffix if needed).
    /// </summary>
    public string GetUniqueName(string baseName)
    {
        if (!PresetExists(baseName))
            return baseName;

        int counter = 2;
        string newName;
        do
        {
            newName = $"{baseName} ({counter})";
            counter++;
        } while (PresetExists(newName));

        return newName;
    }

    /// <summary>
    /// Gets a custom preset by name.
    /// </summary>
    public CustomPreset? GetCustomPreset(string name)
    {
        return _customPresets.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
