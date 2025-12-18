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
    public EffectCategory Category { get; init; } = EffectCategory.Other;

    /// <summary>Default trigger mode.</summary>
    public EffectTrigger DefaultTrigger { get; init; } = EffectTrigger.Always;
}

/// <summary>
/// Categories for organizing effects in the UI.
/// </summary>
public enum EffectCategory
{
    /// <summary>Particle effects like sparkles, confetti, snow.</summary>
    Particle,

    /// <summary>Cosmic and space-themed effects.</summary>
    Cosmic,

    /// <summary>Nature-inspired effects.</summary>
    Nature,

    /// <summary>Trail effects that follow mouse movement.</summary>
    Trail,

    /// <summary>Digital and tech-themed effects.</summary>
    Digital,

    /// <summary>Artistic and creative effects.</summary>
    Artistic,

    /// <summary>Physics-based and abstract effects.</summary>
    Physics,

    /// <summary>Light and glow effects.</summary>
    Light,

    /// <summary>Screen transformation effects.</summary>
    Screen,

    /// <summary>Other effects that don't fit other categories.</summary>
    Other
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
