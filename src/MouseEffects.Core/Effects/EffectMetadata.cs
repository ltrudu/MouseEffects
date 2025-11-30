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
    public EffectCategory Category { get; init; } = EffectCategory.Visual;

    /// <summary>Default trigger mode.</summary>
    public EffectTrigger DefaultTrigger { get; init; } = EffectTrigger.Always;
}

/// <summary>
/// Categories for organizing effects.
/// </summary>
public enum EffectCategory
{
    /// <summary>Visual effects like particles, trails.</summary>
    Visual,

    /// <summary>Interactive effects that respond to input.</summary>
    Interactive,

    /// <summary>Ambient/background effects.</summary>
    Ambient,

    /// <summary>Utility effects.</summary>
    Utility
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
