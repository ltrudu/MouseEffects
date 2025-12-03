using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.App;

/// <summary>
/// Manages effect lifecycle, loading, and rendering.
/// </summary>
public sealed class EffectManager : IDisposable
{
    private readonly List<IEffect> _effects = [];
    private readonly Dictionary<string, IEffectFactory> _factories = new();
    private readonly IRenderContext _sharedContext;
    private bool _disposed;
    private bool _globallyPaused;

    public IReadOnlyList<IEffect> Effects => _effects;

    /// <summary>
    /// Gets or sets whether all effects are globally paused.
    /// This doesn't change individual effect enabled states.
    /// </summary>
    public bool IsGloballyPaused
    {
        get => _globallyPaused;
        set => _globallyPaused = value;
    }

    public EffectManager(IRenderContext sharedContext)
    {
        _sharedContext = sharedContext;
    }

    /// <summary>
    /// Register an effect factory.
    /// </summary>
    public void RegisterFactory(IEffectFactory factory)
    {
        _factories[factory.Metadata.Id] = factory;
    }

    /// <summary>
    /// Create and add an effect instance.
    /// </summary>
    public IEffect? CreateEffect(string effectId, EffectConfiguration? config = null)
    {
        if (!_factories.TryGetValue(effectId, out var factory))
        {
            return null;
        }

        var effect = factory.Create();
        effect.Initialize(_sharedContext);

        if (config != null)
        {
            effect.Configure(config);
        }

        _effects.Add(effect);
        return effect;
    }

    /// <summary>
    /// Remove an effect instance.
    /// </summary>
    public void RemoveEffect(IEffect effect)
    {
        if (_effects.Remove(effect))
        {
            effect.Dispose();
        }
    }

    /// <summary>
    /// Get all registered effect metadata.
    /// </summary>
    public IEnumerable<EffectMetadata> GetAvailableEffects()
    {
        return _factories.Values.Select(f => f.Metadata);
    }

    /// <summary>
    /// Update all active effects.
    /// </summary>
    public void Update(GameTime gameTime, MouseState mouseState)
    {
        if (_globallyPaused) return;

        foreach (var effect in _effects)
        {
            if (effect.IsEnabled)
            {
                effect.Update(gameTime, mouseState);
            }
        }
    }

    /// <summary>
    /// Render all active effects.
    /// </summary>
    public void Render(IRenderContext context)
    {
        if (_globallyPaused) return;

        foreach (var effect in _effects)
        {
            if (effect.IsEnabled)
            {
                effect.Render(context);
            }
        }
    }

    /// <summary>
    /// Checks if any effect is currently enabled.
    /// </summary>
    public bool HasAnyEffectEnabled()
    {
        if (_globallyPaused) return false;

        foreach (var effect in _effects)
        {
            if (effect.IsEnabled)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if any enabled effect requires continuous screen capture.
    /// </summary>
    public bool RequiresContinuousScreenCapture()
    {
        if (_globallyPaused) return false;

        foreach (var effect in _effects)
        {
            if (effect.IsEnabled && effect.RequiresContinuousScreenCapture)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Enable or disable all effects.
    /// </summary>
    public void SetAllEnabled(bool enabled)
    {
        foreach (var effect in _effects)
        {
            effect.IsEnabled = enabled;
        }
    }

    /// <summary>
    /// Enable only the specified effect, disabling all others.
    /// Returns list of effect IDs whose enabled state changed.
    /// </summary>
    public List<string> EnableExclusively(string effectId)
    {
        var changedEffects = new List<string>();

        foreach (var effect in _effects)
        {
            bool shouldBeEnabled = effect.Metadata.Id == effectId;
            if (effect.IsEnabled != shouldBeEnabled)
            {
                effect.IsEnabled = shouldBeEnabled;
                changedEffects.Add(effect.Metadata.Id);
            }
        }

        return changedEffects;
    }

    /// <summary>
    /// Disable all effects.
    /// Returns list of effect IDs whose enabled state changed.
    /// </summary>
    public List<string> DisableAll()
    {
        var changedEffects = new List<string>();

        foreach (var effect in _effects)
        {
            if (effect.IsEnabled)
            {
                effect.IsEnabled = false;
                changedEffects.Add(effect.Metadata.Id);
            }
        }

        return changedEffects;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var effect in _effects)
        {
            effect.Dispose();
        }
        _effects.Clear();
    }
}
