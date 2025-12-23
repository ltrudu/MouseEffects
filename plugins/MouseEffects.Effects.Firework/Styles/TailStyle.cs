using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Tail style - comet-like trails with streaming light effects.
/// Main particles leave very long persistent trails creating dramatic streaming light effects.
/// </summary>
public class TailStyle : FireworkStyleBase
{
    public override string Name => "Tail";
    public override string Description => "The comet-like trail that is left behind the star, creating streaming light effects";
    public override int StyleId => 16;

    // Style-specific parameters
    private float _tailLength = 3.0f;
    private int _tailDensity = 8;
    private float _tailFade = 1.0f;

    public float TailLength
    {
        get => _tailLength;
        set => _tailLength = value;
    }

    public int TailDensity
    {
        get => _tailDensity;
        set => _tailDensity = value;
    }

    public float TailFade
    {
        get => _tailFade;
        set => _tailFade = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 4.0f,      // Longer life for extended travel
        Gravity = 150f,               // Moderate gravity for arc motion
        Drag = 0.98f,                 // Low drag for long streaming trails
        MinParticlesPerFirework = 20, // Moderate main particle count
        MaxParticlesPerFirework = 40,
        ExplosionForce = 450f,        // High force for long travel distance
        MinParticleSize = 8f,         // Large main particles
        MaxParticleSize = 14f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false, // Trails are the main effect
        StyleSpecific = new Dictionary<string, object>
        {
            ["tailLength"] = 3.0f,
            ["tailDensity"] = 8,
            ["tailFade"] = 1.0f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = tail spawn timer (accumulates over time)
        p.StyleData1 = 0f;

        // StyleData2 = is this a tail particle? (0 = main particle, 1 = tail particle)
        p.StyleData2 = 0f;

        // StyleData3 = tail particle phase (for variation in fade patterns)
        p.StyleData3 = ctx.GetRandomFloat() * 1000f;

        // Add upward bias for arc trajectory
        p.Velocity.Y -= 120f + ctx.GetRandomFloat() * 80f;

        // Main particles are brighter
        if (!isSecondary)
        {
            p.Color.X = MathF.Min(1f, p.Color.X * 1.3f);
            p.Color.Y = MathF.Min(1f, p.Color.Y * 1.3f);
            p.Color.Z = MathF.Min(1f, p.Color.Z * 1.3f);
        }
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Only main particles spawn tail trails (StyleData2 == 0)
        if (particle.StyleData2 < 0.5f)
        {
            // Accumulate tail spawn timer
            particle.StyleData1 += dt;
        }
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Tail spawn timer
        gpu.StyleData2 = particle.StyleData2;  // Is tail particle flag
        gpu.StyleData3 = particle.StyleData3;  // Tail phase
        gpu.StyleFlags = (uint)StyleId;
    }

    // Trail particle spawning - tails leave long streaming trails
    public override bool HasTrailParticles => true;

    public override bool ShouldSpawnTrail(ref FireworkParticle particle, float dt)
    {
        // Only main particles spawn tails
        if (particle.StyleData2 >= 0.5f)
            return false;

        float spawnInterval = 1f / _tailDensity;

        if (particle.StyleData1 >= spawnInterval)
        {
            particle.StyleData1 -= spawnInterval;  // Reset timer (allow continuous spawning)
            return true;
        }

        return false;
    }

    public override FireworkParticle CreateTrailParticle(in FireworkParticle parent, FireworkContext ctx)
    {
        // Tail particles spawn at exact position of parent
        Vector2 position = parent.Position;

        // Inherit small portion of velocity (trails lag behind)
        Vector2 velocity = parent.Velocity * 0.15f;

        // Add slight random variation to velocity
        velocity.X += (ctx.GetRandomFloat() - 0.5f) * 15f;
        velocity.Y += (ctx.GetRandomFloat() - 0.5f) * 15f;

        // Inherit color but slightly dimmer
        Vector4 color = parent.Color * 0.85f;
        color.W = 1f;

        // Very long lifespan for persistent streaming effect
        float baseLife = ctx.ParticleLifespan * _tailLength * (0.5f + ctx.GetRandomFloat() * 0.5f);
        float lifespan = baseLife / _tailFade;  // Longer life with lower fade values

        return new FireworkParticle
        {
            Position = position,
            Velocity = velocity,
            Color = color,
            Size = parent.Size * 0.6f,  // Tail particles slightly smaller
            Life = lifespan,
            MaxLife = lifespan,
            CanExplode = false,
            HasExploded = true,  // Prevent tail particles from exploding
            StyleId = StyleId,
            StyleData1 = 0f,
            StyleData2 = 1f,  // Mark as tail particle
            StyleData3 = ctx.Time + ctx.GetRandomFloat() * 100f  // Phase for variation
        };
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Longer lifespan for extended arc trajectory
        return base.GetParticleLifespan(ctx, isSecondary) * 1.6f;
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Brighter main particles for visible trail sources
        if (!isSecondary)
        {
            color.X = MathF.Min(1f, color.X * 1.25f);
            color.Y = MathF.Min(1f, color.Y * 1.25f);
            color.Z = MathF.Min(1f, color.Z * 1.25f);
        }

        return color;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "tailLength",
            DisplayName = "Tail Length",
            Description = "Length and persistence of streaming tails",
            MinValue = 1.0f,
            MaxValue = 5.0f,
            DefaultValue = 3.0f,
            Step = 0.1f
        };
        yield return new IntStyleParameter
        {
            Key = "tailDensity",
            DisplayName = "Tail Density",
            Description = "Trail particles spawned per main particle per second",
            MinValue = 5,
            MaxValue = 15,
            DefaultValue = 8
        };
        yield return new FloatStyleParameter
        {
            Key = "tailFade",
            DisplayName = "Tail Fade",
            Description = "How fast tail particles fade (lower = longer visible trails)",
            MinValue = 0.5f,
            MaxValue = 2.0f,
            DefaultValue = 1.0f,
            Step = 0.1f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "tailLength":
                _tailLength = Convert.ToSingle(value);
                break;
            case "tailDensity":
                _tailDensity = Convert.ToInt32(value);
                break;
            case "tailFade":
                _tailFade = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "tailLength" => _tailLength,
            "tailDensity" => _tailDensity,
            "tailFade" => _tailFade,
            _ => base.GetParameter(key)
        };
    }
}
