using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Willow style - weeping willow effect.
/// Heavy gravity, slow drag, graceful drooping trails.
/// </summary>
public class WillowStyle : FireworkStyleBase
{
    public override string Name => "Willow";
    public override string Description => "Graceful drooping trails like a weeping willow tree";
    public override int StyleId => 2;

    // Style-specific parameters
    private float _droopIntensity = 2f;
    private float _branchDensity = 2f;
    private int _trailDensity = 6;

    public float DroopIntensity
    {
        get => _droopIntensity;
        set => _droopIntensity = value;
    }

    public float BranchDensity
    {
        get => _branchDensity;
        set => _branchDensity = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 4.0f,   // Longer life for graceful falling
        Gravity = 300f,            // Heavy gravity for droop effect
        Drag = 0.96f,              // More drag for slower fall
        MinParticlesPerFirework = 80,
        MaxParticlesPerFirework = 200,  // More particles for dense effect
        ExplosionForce = 250f,
        MinParticleSize = 2f,      // Smaller particles
        MaxParticleSize = 5f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,  // No secondary for willow
        StyleSpecific = new Dictionary<string, object>
        {
            ["droopIntensity"] = 2f,
            ["branchDensity"] = 2f
        }
    };

    protected override int GetParticleCount(int baseCount, float force, bool isSecondary)
    {
        // More particles based on branch density
        return (int)(base.GetParticleCount(baseCount, force, isSecondary) * _branchDensity);
    }

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = trail spawn timer
        p.StyleData1 = 0f;

        // StyleData2 = droop factor (how much extra gravity this particle gets)
        p.StyleData2 = _droopIntensity * (0.5f + ctx.GetRandomFloat());

        // StyleData3 = is trail particle flag (0 = main, 1 = trail)
        p.StyleData3 = isSecondary ? 1f : 0f;

        // Reduce initial velocity for more graceful effect
        p.Velocity *= 0.7f;

        // Add slight upward bias at start for the "burst then droop" effect
        p.Velocity.Y -= 50f + ctx.GetRandomFloat() * 50f;

        // Golden color for willow (as per reference image)
        p.Color = new Vector4(1.0f, 0.85f, 0.3f, 1f);
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Extra gravity based on droop intensity
        float extraGravity = particle.StyleData2 * 50f * dt;
        particle.Velocity.Y += extraGravity;

        // Gradually reduce horizontal velocity for drooping effect
        particle.Velocity.X *= 0.995f;

        // Accumulate trail timer for main particles
        if (particle.StyleData3 < 0.5f)
        {
            particle.StyleData1 += dt;
        }
    }

    // Trail particle spawning - willow leaves golden trails as it droops
    public override bool HasTrailParticles => true;

    public override bool ShouldSpawnTrail(ref FireworkParticle particle, float dt)
    {
        // Only main particles spawn trails
        if (particle.StyleData3 >= 0.5f) return false;

        float spawnInterval = 1f / _trailDensity;
        if (particle.StyleData1 >= spawnInterval)
        {
            particle.StyleData1 = 0f;
            return true;
        }
        return false;
    }

    public override FireworkParticle CreateTrailParticle(in FireworkParticle parent, FireworkContext ctx)
    {
        // Trail particles stay mostly in place with slight downward drift
        Vector2 velocity = new Vector2(
            (ctx.GetRandomFloat() - 0.5f) * 10f,
            ctx.GetRandomFloat() * 20f + 10f  // Slight downward
        );

        // Golden fading trail
        Vector4 color = new Vector4(1.0f, 0.8f, 0.25f, 0.8f);

        float lifespan = 0.4f + ctx.GetRandomFloat() * 0.4f;

        return new FireworkParticle
        {
            Position = parent.Position,
            Velocity = velocity,
            Color = color,
            Size = parent.Size * 0.5f,
            Life = lifespan,
            MaxLife = lifespan,
            CanExplode = false,
            HasExploded = true,
            StyleId = StyleId,
            StyleData1 = 0f,
            StyleData2 = parent.StyleData2,
            StyleData3 = 1f  // Mark as trail particle
        };
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Droop factor
        gpu.StyleData2 = particle.StyleData2;  // Initial horizontal velocity
        gpu.StyleData3 = particle.StyleData3;  // Spawn time
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Willow particles are generally smaller
        return base.GetParticleSize(ctx, isSecondary) * 0.7f;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Longer lifespan for graceful fall
        return base.GetParticleLifespan(ctx, isSecondary) * 1.5f;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "droopIntensity",
            DisplayName = "Droop Intensity",
            Description = "How much the particles droop down (gravity multiplier)",
            MinValue = 0.5f,
            MaxValue = 3f,
            DefaultValue = 2f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "branchDensity",
            DisplayName = "Branch Density",
            Description = "Particle count multiplier for denser willow branches",
            MinValue = 1f,
            MaxValue = 5f,
            DefaultValue = 2f,
            Step = 0.5f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "droopIntensity":
                _droopIntensity = Convert.ToSingle(value);
                break;
            case "branchDensity":
                _branchDensity = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "droopIntensity" => _droopIntensity,
            "branchDensity" => _branchDensity,
            _ => base.GetParameter(key)
        };
    }
}
