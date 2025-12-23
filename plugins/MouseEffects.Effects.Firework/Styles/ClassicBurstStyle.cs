using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Classic burst style - the original firework behavior.
/// Radial explosion with optional secondary bursts.
/// </summary>
public class ClassicBurstStyle : FireworkStyleBase
{
    public override string Name => "Classic Burst";
    public override string Description => "Traditional radial explosion with colorful particles and optional secondary bursts";
    public override int StyleId => 0;

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 2.5f,
        Gravity = 150f,
        Drag = 0.98f,
        MinParticlesPerFirework = 50,
        MaxParticlesPerFirework = 150,
        ExplosionForce = 300f,
        MinParticleSize = 3f,
        MaxParticleSize = 8f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = true,
        SecondaryParticleCount = 20,
        SecondaryExplosionForce = 100f,
        SecondaryExplosionDelay = 0.8f
    };

    public override IEnumerable<StyleParameter> GetParameters()
    {
        // Classic burst uses all common parameters, no style-specific ones
        yield break;
    }
}
