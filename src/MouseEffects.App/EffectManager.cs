using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.App;

/// <summary>
/// Manages effect lifecycle, loading, and rendering.
/// Supports lazy effect creation - effects are only initialized when first needed.
/// </summary>
public sealed class EffectManager : IDisposable
{
    private readonly List<IEffect> _effects = [];
    private readonly Dictionary<string, IEffectFactory> _factories = new();
    private readonly Dictionary<string, IEffect> _effectsById = new();
    private readonly IRenderContext _sharedContext;
    private bool _disposed;
    private bool _globallyPaused;

    public IReadOnlyList<IEffect> Effects => _effects;

    /// <summary>
    /// Gets all registered factories (for lazy loading - metadata only).
    /// </summary>
    public IReadOnlyDictionary<string, IEffectFactory> Factories => _factories;

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
        // Return existing effect if already created
        if (_effectsById.TryGetValue(effectId, out var existingEffect))
        {
            return existingEffect;
        }

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
        _effectsById[effectId] = effect;
        return effect;
    }

    /// <summary>
    /// Get or create an effect instance (lazy loading).
    /// Returns the effect and whether it was newly created.
    /// </summary>
    public (IEffect? Effect, bool WasCreated) GetOrCreateEffect(string effectId)
    {
        if (_effectsById.TryGetValue(effectId, out var existingEffect))
        {
            return (existingEffect, false);
        }

        var effect = CreateEffect(effectId);
        return (effect, effect != null);
    }

    /// <summary>
    /// Check if an effect has been created.
    /// </summary>
    public bool IsEffectCreated(string effectId)
    {
        return _effectsById.ContainsKey(effectId);
    }

    /// <summary>
    /// Get an effect by ID if it exists.
    /// </summary>
    public IEffect? GetEffect(string effectId)
    {
        return _effectsById.GetValueOrDefault(effectId);
    }

    /// <summary>
    /// Get a factory by ID.
    /// </summary>
    public IEffectFactory? GetFactory(string effectId)
    {
        return _factories.GetValueOrDefault(effectId);
    }

    /// <summary>
    /// Remove an effect instance.
    /// </summary>
    public void RemoveEffect(IEffect effect)
    {
        if (_effects.Remove(effect))
        {
            _effectsById.Remove(effect.Metadata.Id);
            effect.Dispose();
        }
    }

    /// <summary>
    /// Unload an effect by ID - disposes GPU resources and removes from active effects.
    /// The effect can be recreated later via CreateEffect (lazy loading).
    /// </summary>
    public bool UnloadEffect(string effectId)
    {
        if (_effectsById.TryGetValue(effectId, out var effect))
        {
            _effects.Remove(effect);
            _effectsById.Remove(effectId);
            effect.Dispose();
            return true;
        }
        return false;
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
