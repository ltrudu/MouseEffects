using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Diagnostics;

namespace MouseEffects.Plugins;

/// <summary>
/// Discovers and loads effect plugins from a directory.
/// </summary>
public sealed class PluginLoader
{
    private readonly string _pluginsDirectory;
    private readonly List<PluginInfo> _loadedPlugins = [];
    private readonly List<IEffectFactory> _factories = [];
    private readonly object _lock = new();

    public IReadOnlyList<PluginInfo> LoadedPlugins => _loadedPlugins;
    public IReadOnlyList<IEffectFactory> Factories => _factories;

    /// <summary>
    /// Event raised when a plugin is loaded (for progress reporting).
    /// </summary>
    public event Action<string, int, int>? PluginLoaded;

    public PluginLoader(string pluginsDirectory)
    {
        _pluginsDirectory = pluginsDirectory;
    }

    /// <summary>
    /// Load all plugins from the plugins directory asynchronously.
    /// </summary>
    public async Task LoadPluginsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_pluginsDirectory))
        {
            Log($"Plugins directory not found: {_pluginsDirectory}");
            Directory.CreateDirectory(_pluginsDirectory);
            Log($"Created plugins directory: {_pluginsDirectory}");
            return;
        }

        Log($"Scanning plugins directory: {_pluginsDirectory}");

        // Find all DLLs in the plugins directory (including subdirectories)
        var dllFiles = await Task.Run(() =>
            Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories)
                .Where(f => !IsSystemAssembly(Path.GetFileName(f)))
                .ToArray(),
            cancellationToken);

        Log($"Found {dllFiles.Length} plugin DLL file(s) to load");

        if (dllFiles.Length == 0)
            return;

        // Load plugins in parallel
        var loadedPlugins = new ConcurrentBag<(PluginInfo info, List<IEffectFactory> factories)>();
        int loadedCount = 0;

        await Parallel.ForEachAsync(dllFiles, cancellationToken, async (dllPath, ct) =>
        {
            try
            {
                var result = await Task.Run(() => LoadPluginInternal(dllPath), ct);
                if (result.HasValue)
                {
                    loadedPlugins.Add(result.Value);
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to load plugin {Path.GetFileName(dllPath)}: {ex.Message}");
            }
            finally
            {
                var count = Interlocked.Increment(ref loadedCount);
                PluginLoaded?.Invoke(Path.GetFileName(dllPath), count, dllFiles.Length);
            }
        });

        // Add all loaded plugins to the main lists (thread-safe)
        lock (_lock)
        {
            foreach (var (info, factories) in loadedPlugins)
            {
                _loadedPlugins.Add(info);
                _factories.AddRange(factories);
            }
        }

        Log($"Loaded {_factories.Count} effect factory(ies) from {_loadedPlugins.Count} plugin(s)");
    }

    /// <summary>
    /// Load all plugins from the plugins directory (synchronous, for backward compatibility).
    /// </summary>
    public void LoadPlugins()
    {
        if (!Directory.Exists(_pluginsDirectory))
        {
            Log($"Plugins directory not found: {_pluginsDirectory}");
            Directory.CreateDirectory(_pluginsDirectory);
            Log($"Created plugins directory: {_pluginsDirectory}");
            return;
        }

        Log($"Scanning plugins directory: {_pluginsDirectory}");

        // Find all DLLs in the plugins directory (including subdirectories)
        var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
        Log($"Found {dllFiles.Length} DLL file(s)");

        foreach (var dllPath in dllFiles)
        {
            // Skip common runtime/framework DLLs
            var fileName = Path.GetFileName(dllPath);
            if (IsSystemAssembly(fileName))
            {
                continue;
            }

            try
            {
                LoadPlugin(dllPath);
            }
            catch (Exception ex)
            {
                Log($"Failed to load plugin {fileName}: {ex.Message}");
            }
        }

        Log($"Loaded {_factories.Count} effect factory(ies) from {_loadedPlugins.Count} plugin(s)");
    }

    private void LoadPlugin(string dllPath)
    {
        var result = LoadPluginInternal(dllPath);
        if (result.HasValue)
        {
            var (info, factories) = result.Value;
            _loadedPlugins.Add(info);
            _factories.AddRange(factories);
        }
    }

    private (PluginInfo info, List<IEffectFactory> factories)? LoadPluginInternal(string dllPath)
    {
        var fileName = Path.GetFileName(dllPath);

        // Load into default context so types match with the main app
        // This allows IEffectFactory from plugins to be recognized
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);

        // Find all IEffectFactory implementations
        var factoryTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(IEffectFactory).IsAssignableFrom(t))
            .ToList();

        if (factoryTypes.Count == 0)
        {
            return null; // Not a plugin DLL
        }

        Log($"Loading plugin: {fileName}");

        var pluginInfo = new PluginInfo
        {
            FilePath = dllPath,
            Assembly = assembly,
            FactoryTypes = factoryTypes
        };

        var factories = new List<IEffectFactory>();
        foreach (var factoryType in factoryTypes)
        {
            try
            {
                var factory = (IEffectFactory)Activator.CreateInstance(factoryType)!;
                factories.Add(factory);
                Log($"  Registered factory: {factory.Metadata.Name} ({factory.Metadata.Id})");
            }
            catch (Exception ex)
            {
                Log($"  Failed to create factory {factoryType.Name}: {ex.Message}");
            }
        }

        return (pluginInfo, factories);
    }

    private static bool IsSystemAssembly(string fileName)
    {
        // Skip common system/framework assemblies
        return fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("Vortice.", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("SharpGen.", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("MouseEffects.Core.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("MouseEffects.DirectX.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("MouseEffects.Plugins.dll", StringComparison.OrdinalIgnoreCase);
    }

    private static void Log(string message) => Logger.Log("PluginLoader", message);
}

/// <summary>
/// Information about a loaded plugin.
/// </summary>
public sealed class PluginInfo
{
    public required string FilePath { get; init; }
    public required Assembly Assembly { get; init; }
    public required List<Type> FactoryTypes { get; init; }
}

