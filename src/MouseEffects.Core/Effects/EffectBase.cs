using System.Numerics;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Core.Effects;

/// <summary>
/// Base class for effects with common functionality.
/// </summary>
public abstract class EffectBase : IEffect
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public abstract EffectMetadata Metadata { get; }
    public EffectConfiguration Configuration { get; private set; } = new();

    public virtual int RenderOrder => 0;
    public virtual bool IsComplete => false;

    /// <summary>
    /// Whether this effect requires continuous screen capture.
    /// Override and return true for effects that read and transform screen content.
    /// </summary>
    public virtual bool RequiresContinuousScreenCapture => false;

    protected IRenderContext? Context { get; private set; }
    protected Vector2 ViewportSize { get; private set; }
    protected bool IsInitialized { get; private set; }

    public void Initialize(IRenderContext context)
    {
        if (IsInitialized)
            throw new InvalidOperationException("Effect already initialized");

        Context = context;
        ViewportSize = context.ViewportSize;
        OnInitialize(context);
        IsInitialized = true;
    }

    public void Configure(EffectConfiguration config)
    {
        Configuration = config.Clone();
        OnConfigurationChanged();
    }

    public void Update(GameTime gameTime, MouseState mouseState)
    {
        if (!IsInitialized) return;
        OnUpdate(gameTime, mouseState);
    }

    public void Render(IRenderContext context)
    {
        if (!IsInitialized) return;
        OnRender(context);
    }

    public void OnViewportChanged(Vector2 newSize)
    {
        ViewportSize = newSize;
        OnViewportSizeChanged(newSize);
    }

    public void Dispose()
    {
        if (IsInitialized)
        {
            OnDispose();
            IsInitialized = false;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>Initialize GPU resources. Called once.</summary>
    protected abstract void OnInitialize(IRenderContext context);

    /// <summary>Update effect state each frame.</summary>
    protected abstract void OnUpdate(GameTime gameTime, MouseState mouseState);

    /// <summary>Render the effect.</summary>
    protected abstract void OnRender(IRenderContext context);

    /// <summary>Called when configuration changes.</summary>
    protected virtual void OnConfigurationChanged() { }

    /// <summary>Called when viewport size changes.</summary>
    protected virtual void OnViewportSizeChanged(Vector2 newSize) { }

    /// <summary>Cleanup GPU resources.</summary>
    protected virtual void OnDispose() { }
}
