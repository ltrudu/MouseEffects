using System.Numerics;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Core.Effects;

/// <summary>
/// Interface for visual effects that respond to mouse input.
/// </summary>
public interface IEffect : IDisposable
{
    /// <summary>Unique identifier for this effect instance.</summary>
    Guid InstanceId { get; }

    /// <summary>Effect metadata (name, description, author).</summary>
    EffectMetadata Metadata { get; }

    /// <summary>Current configuration.</summary>
    EffectConfiguration Configuration { get; }

    /// <summary>Render order (lower = rendered first).</summary>
    int RenderOrder { get; }

    /// <summary>Whether this effect has completed and can be removed.</summary>
    bool IsComplete { get; }

    /// <summary>Whether the effect is currently enabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Whether this effect requires continuous screen capture.
    /// Effects that read and transform screen content (like color filters) should return true.
    /// Default is false - screen capture only happens when needed.
    /// </summary>
    bool RequiresContinuousScreenCapture => false;

    /// <summary>Initialize GPU resources.</summary>
    void Initialize(IRenderContext context);

    /// <summary>Apply configuration changes.</summary>
    void Configure(EffectConfiguration config);

    /// <summary>Update effect state based on time and mouse input.</summary>
    void Update(GameTime gameTime, MouseState mouseState);

    /// <summary>Render the effect.</summary>
    void Render(IRenderContext context);

    /// <summary>Called when viewport size changes.</summary>
    void OnViewportChanged(Vector2 newSize);
}

/// <summary>
/// Factory for creating effect instances.
/// </summary>
public interface IEffectFactory
{
    /// <summary>Metadata describing the effect this factory creates.</summary>
    EffectMetadata Metadata { get; }

    /// <summary>Create a new instance of the effect.</summary>
    IEffect Create();

    /// <summary>Get default configuration for this effect.</summary>
    EffectConfiguration GetDefaultConfiguration();

    /// <summary>Get configuration schema for UI generation.</summary>
    EffectConfigurationSchema GetConfigurationSchema();

    /// <summary>
    /// Create a settings control for this effect.
    /// Returns a WPF FrameworkElement (UserControl) that can be embedded in the settings window.
    /// Return null if no custom settings UI is needed.
    /// </summary>
    /// <param name="effect">The effect instance to configure.</param>
    /// <returns>A WPF control or null if using default schema-based UI.</returns>
    object? CreateSettingsControl(IEffect effect) => null;
}
