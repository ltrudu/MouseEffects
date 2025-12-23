using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Pistil style - a central palm-like effect that can be added to Peony or Chrysanthemum for bigger impact.
/// Creates a bright central core/ball of particles that expand outward like a palm but more concentrated.
/// </summary>
public class PistilStyle : FireworkStyleBase
{
    public override string Name => "Pistil";
    public override string Description => "A central palm-like effect that can be added to Peony or Chrysanthemum for bigger impact";
    public override int StyleId => 14;

    // Style-specific parameters
    private float _coreSize = 0.5f;
    private float _coreBrightness = 1.5f;
    private int _outerRingCount = 40;

    public float CoreSize
    {
        get => _coreSize;
        set => _coreSize = value;
    }

    public float CoreBrightness
    {
        get => _coreBrightness;
        set => _coreBrightness = value;
    }

    public int OuterRingCount
    {
        get => _outerRingCount;
        set => _outerRingCount = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 2.5f,
        Gravity = 150f,              // Moderate gravity for balanced expansion
        Drag = 0.98f,                // Low drag for sustained movement
        MinParticlesPerFirework = 60,
        MaxParticlesPerFirework = 120,
        ExplosionForce = 320f,
        MinParticleSize = 5f,        // Larger particles for visibility
        MaxParticleSize = 12f,
        SpreadAngle = 360f,          // Full sphere
        EnableSecondaryExplosion = false,
        StyleSpecific = new Dictionary<string, object>
        {
            ["coreSize"] = 0.5f,
            ["coreBrightness"] = 1.5f,
            ["outerRingCount"] = 40
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // Determine if particle is in core or outer ring
        bool isCore = index < total * _coreSize;

        // StyleData1 = particle type (0 = core, 1 = outer ring)
        p.StyleData1 = isCore ? 0f : 1f;

        // StyleData2 = initial brightness multiplier
        p.StyleData2 = isCore ? _coreBrightness : 1.0f;

        // StyleData3 = spawn time for effects
        p.StyleData3 = ctx.Time;

        if (isCore)
        {
            // Core particles: stay closer to center initially, then expand
            // Create tight initial clustering
            float angle = (index / (float)(total * _coreSize)) * MathF.PI * 2f;
            angle += (ctx.GetRandomFloat() - 0.5f) * 0.3f;

            // Reduced initial force for core, will accelerate later
            // Use existing velocity magnitude as base force
            float baseForce = p.Velocity.Length();
            float coreForce = baseForce * (0.4f + ctx.GetRandomFloat() * 0.2f);

            p.Velocity = new Vector2(
                MathF.Cos(angle) * coreForce,
                MathF.Sin(angle) * coreForce
            );

            // Brighter, more intense colors at center
            p.Color.X = MathF.Min(1f, p.Color.X * _coreBrightness);
            p.Color.Y = MathF.Min(1f, p.Color.Y * _coreBrightness);
            p.Color.Z = MathF.Min(1f, p.Color.Z * _coreBrightness);
            p.Color.W = 1.0f;

            // Larger size for core
            p.Size *= 1.4f;
        }
        else
        {
            // Outer ring particles: form distinct palm-like arms
            int outerIndex = index - (int)(total * _coreSize);
            int outerTotal = total - (int)(total * _coreSize);

            // Create discrete arms
            int armIndex = outerIndex % _outerRingCount;
            float armAngle = (armIndex / (float)_outerRingCount) * MathF.PI * 2f;

            // Add spread within arm
            float spread = (ctx.GetRandomFloat() - 0.5f) * 0.4f;
            float finalAngle = armAngle + spread;

            // Higher force for outer ring to create contrast
            // Use existing velocity magnitude as base force
            float baseForce = p.Velocity.Length();
            float outerForce = baseForce * (0.9f + ctx.GetRandomFloat() * 0.2f);

            p.Velocity = new Vector2(
                MathF.Cos(finalAngle) * outerForce,
                MathF.Sin(finalAngle) * outerForce
            );

            // Standard brightness for outer ring
            p.Color.W = 1.0f;
        }
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        float age = time - particle.StyleData3;
        float normalizedAge = age / particle.MaxLife;

        bool isCore = particle.StyleData1 < 0.5f;

        if (isCore)
        {
            // Core particles accelerate outward after brief delay
            // Creates the "heart explosion" effect
            if (normalizedAge > 0.15f && normalizedAge < 0.5f)
            {
                float accelerationPhase = (normalizedAge - 0.15f) / 0.35f;
                float acceleration = 300f * accelerationPhase;

                Vector2 direction = Vector2.Normalize(particle.Velocity);
                particle.Velocity += direction * acceleration * dt;
            }

            // Maintain brightness longer for core
            if (normalizedAge < 0.6f)
            {
                particle.Color.W = 1.0f;
            }
        }
        else
        {
            // Outer ring maintains consistent velocity
            // Creates stable palm-like effect
            particle.Velocity *= 0.995f;
        }
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Particle type (core/outer)
        gpu.StyleData2 = particle.StyleData2;  // Brightness multiplier
        gpu.StyleData3 = particle.StyleData3;  // Spawn time
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Pistil particles are larger for visibility
        return base.GetParticleSize(ctx, isSecondary) * 1.2f;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "coreSize",
            DisplayName = "Core Size",
            Description = "Size of central core as ratio of total particles",
            MinValue = 0.3f,
            MaxValue = 0.8f,
            DefaultValue = 0.5f,
            Step = 0.05f
        };
        yield return new FloatStyleParameter
        {
            Key = "coreBrightness",
            DisplayName = "Core Brightness",
            Description = "Brightness multiplier for core particles",
            MinValue = 1.0f,
            MaxValue = 2.0f,
            DefaultValue = 1.5f,
            Step = 0.1f
        };
        yield return new IntStyleParameter
        {
            Key = "outerRingCount",
            DisplayName = "Outer Ring Count",
            Description = "Number of particles in outer ring arms",
            MinValue = 20,
            MaxValue = 60,
            DefaultValue = 40
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "coreSize":
                _coreSize = Convert.ToSingle(value);
                break;
            case "coreBrightness":
                _coreBrightness = Convert.ToSingle(value);
                break;
            case "outerRingCount":
                _outerRingCount = Convert.ToInt32(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "coreSize" => _coreSize,
            "coreBrightness" => _coreBrightness,
            "outerRingCount" => _outerRingCount,
            _ => base.GetParameter(key)
        };
    }
}
