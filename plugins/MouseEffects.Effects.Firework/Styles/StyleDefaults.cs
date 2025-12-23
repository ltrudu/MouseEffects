namespace MouseEffects.Effects.Firework.Styles;

public class StyleDefaults
{
    public float ParticleLifespan { get; init; } = 2.5f;
    public float Gravity { get; init; } = 150f;
    public float Drag { get; init; } = 0.98f;
    public int MinParticlesPerFirework { get; init; } = 50;
    public int MaxParticlesPerFirework { get; init; } = 150;
    public float ExplosionForce { get; init; } = 300f;
    public float MinParticleSize { get; init; } = 3f;
    public float MaxParticleSize { get; init; } = 8f;
    public float SpreadAngle { get; init; } = 360f;
    public bool EnableSecondaryExplosion { get; init; } = true;
    public int SecondaryParticleCount { get; init; } = 20;
    public float SecondaryExplosionForce { get; init; } = 100f;
    public float SecondaryExplosionDelay { get; init; } = 0.8f;

    // Style-specific defaults (can be extended per style)
    public Dictionary<string, object> StyleSpecific { get; init; } = new();
}
