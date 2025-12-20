namespace MouseEffects.Core.Effects;

/// <summary>
/// Metadata describing an effect.
/// </summary>
public sealed record EffectMetadata
{
    /// <summary>Unique identifier for this effect type.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Description of what the effect does.</summary>
    public required string Description { get; init; }

    /// <summary>Author name.</summary>
    public required string Author { get; init; }

    /// <summary>Version of the effect.</summary>
    public required Version Version { get; init; }

    /// <summary>Optional icon path.</summary>
    public string? IconPath { get; init; }

    /// <summary>Effect category for organization.</summary>
    public EffectCategory Category { get; init; } = EffectCategory.Particle;

    /// <summary>Default trigger mode.</summary>
    public EffectTrigger DefaultTrigger { get; init; } = EffectTrigger.Always;
}

/// <summary>
/// Categories for organizing effects in the UI.
/// </summary>
public enum EffectCategory
{
    /// <summary>Particle effects like bubbles, confetti, snow, cherry blossoms.</summary>
    Particle,

    /// <summary>Fire and energy effects like flames, lasers, lightning, tesla.</summary>
    FireEnergy,

    /// <summary>Space and cosmic effects like black holes, nebula, portals, starfields.</summary>
    Cosmic,

    /// <summary>Visual filter effects that transform screen content (screen capture based).</summary>
    VisualFilter,

    /// <summary>Artistic and geometric effects like circuits, crystals, sacred geometry.</summary>
    Artistic,

    /// <summary>Interactive and game effects like fireworks and space invaders.</summary>
    Interactive
}

/// <summary>
/// Trigger modes for effects.
/// </summary>
public enum EffectTrigger
{
    /// <summary>Effect runs continuously.</summary>
    Always,

    /// <summary>Triggered on mouse click.</summary>
    OnClick,

    /// <summary>Triggered on mouse movement.</summary>
    OnMove,

    /// <summary>Triggered when mouse is idle.</summary>
    OnIdle,

    /// <summary>Manually triggered.</summary>
    Manual
}
