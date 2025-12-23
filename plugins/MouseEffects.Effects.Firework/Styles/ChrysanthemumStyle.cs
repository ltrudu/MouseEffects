using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Chrysanthemum style - dense star pattern with trailing sparks.
/// Each particle spawns trailing sparks creating a flower-like pattern.
/// </summary>
public class ChrysanthemumStyle : FireworkStyleBase
{
    public override string Name => "Chrysanthemum";
    public override string Description => "Dense star pattern where particles leave glowing trails like flower petals";
    public override int StyleId => 4;

    // Style-specific parameters
    private int _sparkDensity = 5;      // Reduced from 15 for performance
    private float _trailPersistence = 0.5f;  // Shorter trails
    private int _maxSparksPerParticle = 3;   // Reduced from 8 for performance

    public int SparkDensity
    {
        get => _sparkDensity;
        set => _sparkDensity = value;
    }

    public float TrailPersistence
    {
        get => _trailPersistence;
        set => _trailPersistence = value;
    }

    public int MaxSparksPerParticle
    {
        get => _maxSparksPerParticle;
        set => _maxSparksPerParticle = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.0f,
        Gravity = 120f,
        Drag = 0.975f,
        MinParticlesPerFirework = 40,
        MaxParticlesPerFirework = 80,  // Fewer main particles, more trails
        ExplosionForce = 280f,
        MinParticleSize = 4f,
        MaxParticleSize = 8f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,  // Trails replace secondary
        StyleSpecific = new Dictionary<string, object>
        {
            ["sparkDensity"] = 5,
            ["trailPersistence"] = 0.5f,
            ["maxSparksPerParticle"] = 3
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = spark timer (counts up to spawn sparks)
        p.StyleData1 = 0f;

        // StyleData2 = sparks spawned count
        p.StyleData2 = 0f;

        // StyleData3 = spawn time for trail persistence calculation
        p.StyleData3 = ctx.Time;

        // Main particles are brighter
        if (!isSecondary)
        {
            p.Color.X = MathF.Min(1f, p.Color.X * 1.2f);
            p.Color.Y = MathF.Min(1f, p.Color.Y * 1.2f);
            p.Color.Z = MathF.Min(1f, p.Color.Z * 1.2f);
        }
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Track spark timing in StyleData1
        particle.StyleData1 += dt;

        // Note: Actual spark spawning happens in the effect's update loop
        // because we need access to the particle pool
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Spark timer
        gpu.StyleData2 = particle.StyleData2;  // Sparks spawned
        gpu.StyleData3 = particle.StyleData3;  // Spawn time
        gpu.StyleFlags = (uint)StyleId;
    }

    // Trail particle spawning
    public override bool HasTrailParticles => true;

    public override bool ShouldSpawnTrail(ref FireworkParticle particle, float dt)
    {
        float sparkInterval = 1f / _sparkDensity;

        if (particle.StyleData1 >= sparkInterval && particle.StyleData2 < _maxSparksPerParticle)
        {
            particle.StyleData1 = 0f;  // Reset timer
            particle.StyleData2 += 1f; // Increment spark count
            return true;
        }

        return false;
    }

    public override FireworkParticle CreateTrailParticle(in FireworkParticle parent, FireworkContext ctx)
    {
        // Trail sparks inherit position with slight offset
        Vector2 offset = new(
            (ctx.GetRandomFloat() - 0.5f) * 5f,
            (ctx.GetRandomFloat() - 0.5f) * 5f
        );

        // Inherit velocity with reduction and slight randomization
        Vector2 velocity = parent.Velocity * 0.3f;
        velocity.X += (ctx.GetRandomFloat() - 0.5f) * 30f;
        velocity.Y += (ctx.GetRandomFloat() - 0.5f) * 30f;

        // Dimmer, smaller version of parent color
        Vector4 color = parent.Color * 0.7f;
        color.W = 1f;

        float lifespan = ctx.ParticleLifespan * _trailPersistence * (0.2f + ctx.GetRandomFloat() * 0.3f);

        return new FireworkParticle
        {
            Position = parent.Position + offset,
            Velocity = velocity,
            Color = color,
            Size = parent.Size * 0.4f,
            Life = lifespan,
            MaxLife = lifespan,
            CanExplode = false,
            HasExploded = true,  // Prevent any further explosions
            StyleId = StyleId,
            StyleData1 = 0f,
            StyleData2 = _maxSparksPerParticle,  // Max out so no sub-sparks
            StyleData3 = ctx.Time
        };
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new IntStyleParameter
        {
            Key = "sparkDensity",
            DisplayName = "Spark Density",
            Description = "Sparks spawned per particle per second (higher = more GPU load)",
            MinValue = 1,
            MaxValue = 20,
            DefaultValue = 5
        };
        yield return new FloatStyleParameter
        {
            Key = "trailPersistence",
            DisplayName = "Trail Persistence",
            Description = "How long trail sparks last (higher = more particles alive)",
            MinValue = 0.2f,
            MaxValue = 1.5f,
            DefaultValue = 0.5f,
            Step = 0.1f
        };
        yield return new IntStyleParameter
        {
            Key = "maxSparksPerParticle",
            DisplayName = "Max Sparks/Particle",
            Description = "Maximum sparks each particle can spawn (higher = exponential growth)",
            MinValue = 1,
            MaxValue = 10,
            DefaultValue = 3
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "sparkDensity":
                _sparkDensity = Convert.ToInt32(value);
                break;
            case "trailPersistence":
                _trailPersistence = Convert.ToSingle(value);
                break;
            case "maxSparksPerParticle":
                _maxSparksPerParticle = Convert.ToInt32(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "sparkDensity" => _sparkDensity,
            "trailPersistence" => _trailPersistence,
            "maxSparksPerParticle" => _maxSparksPerParticle,
            _ => base.GetParameter(key)
        };
    }
}
