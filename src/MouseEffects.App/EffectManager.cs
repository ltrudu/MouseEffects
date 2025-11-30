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

    public IReadOnlyList<IEffect> Effects => _effects;

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
        foreach (var effect in _effects)
        {
            if (effect.IsEnabled)
            {
                effect.Render(context);
            }
        }
    }

    /// <summary>
    /// Checks if any enabled effect requires continuous screen capture.
    /// </summary>
    public bool RequiresContinuousScreenCapture()
    {
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
