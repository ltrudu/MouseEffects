using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Palm style - palm tree branch effect.
/// Bright arms of cascading, long lasting stars shaped like palm tree branches.
/// Particles travel upward and outward first, then cascade down with heavy gravity.
/// </summary>
public class PalmStyle : FireworkStyleBase
{
    public override string Name => "Palm";
    public override string Description => "Bright arms of cascading, long lasting stars shaped like palm tree branches";
    public override int StyleId => 9;

    // Style-specific parameters
    private int _armCount = 6;
    private float _armSpread = 60f;
    private float _cascadeSpeed = 1.0f;

    public int ArmCount
    {
        get => _armCount;
        set => _armCount = value;
    }

    public float ArmSpread
    {
        get => _armSpread;
        set => _armSpread = value;
    }

    public float CascadeSpeed
    {
        get => _cascadeSpeed;
        set => _cascadeSpeed = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 5.0f,   // Very long-lasting stars
        Gravity = 400f,            // Heavy gravity for cascade effect
        Drag = 0.98f,              // Minimal drag for long trails
        MinParticlesPerFirework = 60,
        MaxParticlesPerFirework = 180,
        ExplosionForce = 350f,     // Strong upward/outward initial force
        MinParticleSize = 3f,      // Bright, visible stars
        MaxParticleSize = 6f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,  // No secondary for palm
        StyleSpecific = new Dictionary<string, object>
        {
            ["armCount"] = 6,
            ["armSpread"] = 60f,
            ["cascadeSpeed"] = 1.0f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // Assign particle to one of the arms
        int armIndex = index % _armCount;
        float armAngle = (armIndex / (float)_armCount) * MathF.PI * 2f;

        // Add spread variation within the arm
        float armSpreadRad = _armSpread * MathF.PI / 180f;
        float variation = (ctx.GetRandomFloat() - 0.5f) * armSpreadRad;
        float finalAngle = armAngle + variation;

        // Strong upward component for palm frond rising
        // Negative Y is up in screen coordinates
        float upwardBias = -1.5f;
        float outwardBias = 0.8f;

        // Modify the existing velocity that was set by base class
        float velocityMagnitude = p.Velocity.Length();
        Vector2 newVelocity = new Vector2(
            MathF.Cos(finalAngle) * velocityMagnitude * outwardBias,
            MathF.Sin(finalAngle) * velocityMagnitude * upwardBias
        );

        p.Velocity = newVelocity;

        // StyleData1 = arm index (for grouping)
        p.StyleData1 = armIndex;

        // StyleData2 = distance from arm center (for trail variation)
        p.StyleData2 = MathF.Abs(variation / armSpreadRad);

        // StyleData3 = spawn time for cascade calculation
        p.StyleData3 = ctx.Time;

        // Brighten particles for gold/silver effect
        p.Color.X = MathF.Min(1f, p.Color.X * 1.2f);
        p.Color.Y = MathF.Min(1f, p.Color.Y * 1.2f);
        p.Color.Z = MathF.Min(1f, p.Color.Z * 1.2f);
        p.Color.W = 1.0f; // Full alpha
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Calculate age of particle
        float age = time - particle.StyleData3;
        float normalizedAge = age / particle.MaxLife;

        // After initial rise (first 30% of life), apply cascade effect
        if (normalizedAge > 0.3f)
        {
            // Increased gravity for cascade
            float cascadeGravity = _cascadeSpeed * 200f * dt;
            particle.Velocity.Y += cascadeGravity;
        }

        // Maintain horizontal velocity longer for palm frond shape
        // Only reduce slightly over time
        particle.Velocity.X *= 0.998f;

        // Keep particles bright for longer
        if (normalizedAge < 0.7f)
        {
            particle.Color.W = 1.0f;
        }
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Arm index
        gpu.StyleData2 = particle.StyleData2;  // Distance from arm center
        gpu.StyleData3 = particle.StyleData3;  // Spawn time
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Palm particles are larger and brighter
        return base.GetParticleSize(ctx, isSecondary) * 1.1f;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Very long lifespan for cascading effect
        return base.GetParticleLifespan(ctx, isSecondary) * 1.8f;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new IntStyleParameter
        {
            Key = "armCount",
            DisplayName = "Palm Arms",
            Description = "Number of palm tree branches",
            MinValue = 4,
            MaxValue = 10,
            DefaultValue = 6
        };
        yield return new FloatStyleParameter
        {
            Key = "armSpread",
            DisplayName = "Arm Spread",
            Description = "Spread angle of each arm in degrees",
            MinValue = 30f,
            MaxValue = 90f,
            DefaultValue = 60f,
            Step = 5f
        };
        yield return new FloatStyleParameter
        {
            Key = "cascadeSpeed",
            DisplayName = "Cascade Speed",
            Description = "How fast particles fall after rising",
            MinValue = 0.5f,
            MaxValue = 2f,
            DefaultValue = 1.0f,
            Step = 0.1f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "armCount":
                _armCount = Convert.ToInt32(value);
                break;
            case "armSpread":
                _armSpread = Convert.ToSingle(value);
                break;
            case "cascadeSpeed":
                _cascadeSpeed = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "armCount" => _armCount,
            "armSpread" => _armSpread,
            "cascadeSpeed" => _cascadeSpeed,
            _ => base.GetParameter(key)
        };
    }
}
