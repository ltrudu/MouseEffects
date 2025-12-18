using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.App;

/// <summary>
/// Manages effect lifecycle, loading, and rendering.
/// Supports a single active effect at a time.
/// </summary>
public sealed class EffectManager : IDisposable
{
    private readonly Dictionary<string, IEffectFactory> _factories = new();
    private readonly IRenderContext _sharedContext;
    private IEffect? _activeEffect;
    private bool _disposed;
    private bool _globallyPaused;

    /// <summary>
    /// Gets the currently active effect, or null if none.
    /// </summary>
    public IEffect? ActiveEffect => _activeEffect;

    /// <summary>
    /// Gets the ID of the currently active effect, or null if none.
    /// </summary>
    public string? ActiveEffectId => _activeEffect?.Metadata.Id;

    /// <summary>
    /// Gets all registered factories (for lazy loading - metadata only).
    /// </summary>
    public IReadOnlyDictionary<string, IEffectFactory> Factories => _factories;

    /// <summary>
    /// Gets or sets whether effects are globally paused.
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
    /// Get a factory by ID.
    /// </summary>
    public IEffectFactory? GetFactory(string effectId)
    {
        return _factories.GetValueOrDefault(effectId);
    }

    /// <summary>
    /// Get all registered effect metadata.
    /// </summary>
    public IEnumerable<EffectMetadata> GetAvailableEffects()
    {
        return _factories.Values.Select(f => f.Metadata);
    }

    /// <summary>
    /// Set the active effect by ID. Pass null or empty string to clear the active effect.
    /// Disposes the previous effect if one was active.
    /// Returns the newly active effect, or null if cleared.
    /// </summary>
    public IEffect? SetActiveEffect(string? effectId)
    {
        // If clearing active effect
        if (string.IsNullOrEmpty(effectId))
        {
            if (_activeEffect != null)
            {
                _activeEffect.Dispose();
                _activeEffect = null;
            }
            return null;
        }

        // If same effect, return current
        if (_activeEffect?.Metadata.Id == effectId)
        {
            return _activeEffect;
        }

        // Dispose previous effect
        if (_activeEffect != null)
        {
            _activeEffect.Dispose();
            _activeEffect = null;
        }

        // Create new effect
        if (!_factories.TryGetValue(effectId, out var factory))
        {
            return null;
        }

        var effect = factory.Create();
        effect.Initialize(_sharedContext);
        _activeEffect = effect;
        return effect;
    }

    /// <summary>
    /// Check if a factory exists for the given effect ID.
    /// </summary>
    public bool HasFactory(string effectId)
    {
        return _factories.ContainsKey(effectId);
    }

    /// <summary>
    /// Checks if an effect is currently active.
    /// </summary>
    public bool HasActiveEffect()
    {
        return _activeEffect != null && !_globallyPaused;
    }

    /// <summary>
    /// Checks if the active effect requires continuous screen capture.
    /// </summary>
    public bool RequiresContinuousScreenCapture()
    {
        if (_globallyPaused || _activeEffect == null) return false;
        return _activeEffect.RequiresContinuousScreenCapture;
    }

    /// <summary>
    /// Update the active effect.
    /// </summary>
    public void Update(GameTime gameTime, MouseState mouseState)
    {
        if (_globallyPaused || _activeEffect == null) return;
        _activeEffect.Update(gameTime, mouseState);
    }

    /// <summary>
    /// Render the active effect.
    /// </summary>
    public void Render(IRenderContext context)
    {
        if (_globallyPaused || _activeEffect == null) return;
        _activeEffect.Render(context);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _activeEffect?.Dispose();
        _activeEffect = null;
    }
}
