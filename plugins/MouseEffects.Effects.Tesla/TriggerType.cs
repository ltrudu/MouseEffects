namespace MouseEffects.Effects.Tesla;

/// <summary>
/// Types of effects that can be triggered by mouse actions.
/// </summary>
public enum TriggerType
{
    /// <summary>No effect triggered.</summary>
    None = 0,

    /// <summary>Lightning bolt effect.</summary>
    LightningBolt = 1,

    /// <summary>Electrical trail effect (mouse move only).</summary>
    ElectricalFollow = 2
}
