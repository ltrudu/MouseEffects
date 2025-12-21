namespace MouseEffects.Text.Style;

/// <summary>
/// Animation types for text rendering.
/// </summary>
public enum AnimationType
{
    /// <summary>No animation.</summary>
    None = 0,

    /// <summary>Alpha/glow pulsing.</summary>
    Pulse = 1,

    /// <summary>Vertical wave per character.</summary>
    Wave = 2,

    /// <summary>Rainbow color cycling.</summary>
    Rainbow = 3,

    /// <summary>Size breathing effect.</summary>
    Breathing = 4,

    /// <summary>Random shake/jitter.</summary>
    Shake = 5
}

/// <summary>
/// Defines an animation effect for text.
/// </summary>
public class TextAnimation
{
    /// <summary>Type of animation.</summary>
    public AnimationType Type { get; init; }

    /// <summary>Animation speed multiplier (default 1.0).</summary>
    public float Speed { get; init; } = 1.0f;

    /// <summary>Animation intensity/amplitude (default 1.0).</summary>
    public float Intensity { get; init; } = 1.0f;

    /// <summary>Per-character phase offset for wave-like effects.</summary>
    public float PhaseOffset { get; init; } = 0.5f;

    /// <summary>No animation.</summary>
    public static TextAnimation None => new() { Type = AnimationType.None };

    /// <summary>Create a pulsing glow effect.</summary>
    public static TextAnimation Pulse(float speed = 3f, float intensity = 0.4f) =>
        new() { Type = AnimationType.Pulse, Speed = speed, Intensity = intensity };

    /// <summary>Create a vertical wave effect.</summary>
    public static TextAnimation Wave(float speed = 2f, float intensity = 3f, float phaseOffset = 0.5f) =>
        new() { Type = AnimationType.Wave, Speed = speed, Intensity = intensity, PhaseOffset = phaseOffset };

    /// <summary>Create a rainbow color cycling effect.</summary>
    public static TextAnimation Rainbow(float speed = 0.5f, float phaseOffset = 0.1f) =>
        new() { Type = AnimationType.Rainbow, Speed = speed, PhaseOffset = phaseOffset };

    /// <summary>Create a breathing size effect.</summary>
    public static TextAnimation Breathing(float speed = 1.5f, float intensity = 0.05f) =>
        new() { Type = AnimationType.Breathing, Speed = speed, Intensity = intensity };

    /// <summary>Create a shake/jitter effect.</summary>
    public static TextAnimation Shake(float speed = 10f, float intensity = 2f) =>
        new() { Type = AnimationType.Shake, Speed = speed, Intensity = intensity };
}
