using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;
using MouseEffects.Input;

namespace MouseEffects.App;

/// <summary>
/// Manages effect lifecycle, loading, and rendering.
/// Supports a single active effect at a time.
/// </summary>
public sealed class EffectManager : IDisposable
{
    private readonly Dictionary<string, IEffectFactory> _factories = new();
    private readonly IRenderContext _sharedContext;
    private GlobalMouseHook? _mouseHook;
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
    /// When paused, click consumption is also disabled.
    /// </summary>
    public bool IsGloballyPaused
    {
        get => _globallyPaused;
        set
        {
            if (_globallyPaused != value)
            {
                _globallyPaused = value;
                UpdateClickConsumer();
            }
        }
    }

    public EffectManager(IRenderContext sharedContext, GlobalMouseHook? mouseHook = null)
    {
        _sharedContext = sharedContext;
        _mouseHook = mouseHook;
    }

    /// <summary>
    /// Sets the mouse hook for click consumer support. Call after mouse hook is created.
    /// </summary>
    public void SetMouseHook(GlobalMouseHook mouseHook)
    {
        _mouseHook = mouseHook;
        UpdateClickConsumer();
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
            UpdateClickConsumer();
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
            UpdateClickConsumer();
            return null;
        }

        var effect = factory.Create();
        effect.Initialize(_sharedContext);
        _activeEffect = effect;
        UpdateClickConsumer();
        return effect;
    }

    /// <summary>
    /// Updates the click consumer on the mouse hook based on the active effect.
    /// Click consumption is disabled when effects are globally paused.
    /// </summary>
    private void UpdateClickConsumer()
    {
        if (_mouseHook == null) return;

        // Don't consume clicks when globally paused
        if (_globallyPaused)
        {
            _mouseHook.SetClickConsumer(null);
            return;
        }

        if (_activeEffect is IClickConsumer clickConsumer)
        {
            _mouseHook.SetClickConsumer(clickConsumer);
        }
        else
        {
            _mouseHook.SetClickConsumer(null);
        }
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
