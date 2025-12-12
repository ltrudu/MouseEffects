namespace MouseEffects.Effects.Tesla;

/// <summary>
/// Direction modes for lightning bolt emission.
/// </summary>
public enum DirectionMode
{
    /// <summary>Bolts emit in all directions (360 degrees).</summary>
    AllDirections = 0,

    /// <summary>Bolts emit within a configurable spread angle.</summary>
    ConfigurableSpread = 1,

    /// <summary>Bolts emit in the direction of mouse movement (only valid for mouse move trigger).</summary>
    VelocityBased = 2
}
