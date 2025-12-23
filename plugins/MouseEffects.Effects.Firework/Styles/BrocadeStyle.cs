using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Brocade style - woven gold clusters bound together within a star burst.
/// Multiple clusters of particles that stay grouped together with golden trails.
/// </summary>
public class BrocadeStyle : FireworkStyleBase
{
    public override string Name => "Brocade";
    public override string Description => "Woven gold clusters bound together within a star burst";
    public override int StyleId => 6;

    // Style-specific parameters
    private int _clusterCount = 5;
    private float _tailLength = 1.0f;
    private float _goldIntensity = 0.8f;

    public int ClusterCount
    {
        get => _clusterCount;
        set => _clusterCount = value;
    }

    public float TailLength
    {
        get => _tailLength;
        set => _tailLength = value;
    }

    public float GoldIntensity
    {
        get => _goldIntensity;
        set => _goldIntensity = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.5f,    // Long persistence
        Gravity = 250f,             // Heavy, slow-falling
        Drag = 0.94f,               // More drag for slower movement
        MinParticlesPerFirework = 60,
        MaxParticlesPerFirework = 120,
        ExplosionForce = 220f,      // Slower initial burst
        MinParticleSize = 3f,
        MaxParticleSize = 7f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,
        StyleSpecific = new Dictionary<string, object>
        {
            ["clusterCount"] = 5,
            ["tailLength"] = 1.0f,
            ["goldIntensity"] = 0.8f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = cluster ID (which cluster this particle belongs to)
        p.StyleData1 = index % _clusterCount;

        // StyleData2 = cluster attraction strength (varies per particle)
        p.StyleData2 = 0.5f + ctx.GetRandomFloat() * 0.5f;

        // StyleData3 = tail phase offset for rendering long trails
        p.StyleData3 = ctx.GetRandomFloat() * MathF.PI * 2f;

        // Modify velocity based on cluster grouping
        float clusterAngle = (p.StyleData1 / _clusterCount) * MathF.PI * 2f;
        float clusterBias = 30f; // How much to bias toward cluster direction
        p.Velocity.X += MathF.Cos(clusterAngle) * clusterBias;
        p.Velocity.Y += MathF.Sin(clusterAngle) * clusterBias;
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        // Override to create gold/amber tones
        float goldR = 1.0f;
        float goldG = 0.84f * _goldIntensity + (1f - _goldIntensity) * 0.4f;
        float goldB = 0.0f;

        // Blend base color with gold based on goldIntensity
        Vector4 goldColor = new Vector4(goldR, goldG, goldB, 1.0f);
        Vector4 color = Vector4.Lerp(baseColor, goldColor, _goldIntensity);

        // Add variation for sparkle effect
        float variation = ctx.GetRandomFloat() * 0.2f - 0.1f;
        color.X = MathF.Max(0f, MathF.Min(1f, color.X + variation));
        color.Y = MathF.Max(0f, MathF.Min(1f, color.Y + variation));
        color.Z = MathF.Max(0f, MathF.Min(1f, color.Z + variation * 0.5f)); // Less blue variation

        return color;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Simulate particle attraction to nearby cluster members
        // This creates the "woven" effect where particles stay grouped

        // Add slight wobble/weaving motion based on cluster ID
        float wobbleSpeed = 2.0f + particle.StyleData1 * 0.5f;
        float wobbleStrength = 15f * particle.StyleData2;

        particle.Velocity.X += MathF.Sin(time * wobbleSpeed + particle.StyleData3) * wobbleStrength * dt;
        particle.Velocity.Y += MathF.Cos(time * wobbleSpeed * 1.3f + particle.StyleData3) * wobbleStrength * dt * 0.5f;

        // Additional gravity for heavy, slow-falling effect
        particle.Velocity.Y += 50f * dt;
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Cluster ID
        gpu.StyleData2 = _tailLength;          // Tail length for shader
        gpu.StyleData3 = particle.StyleData3;  // Tail phase offset
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Longer lifespan with variation based on tail length
        return base.GetParticleLifespan(ctx, isSecondary) * (1.0f + _tailLength * 0.3f);
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new IntStyleParameter
        {
            Key = "clusterCount",
            DisplayName = "Cluster Count",
            Description = "Number of distinct particle clusters that weave together",
            MinValue = 3,
            MaxValue = 8,
            DefaultValue = 5
        };
        yield return new FloatStyleParameter
        {
            Key = "tailLength",
            DisplayName = "Tail Length",
            Description = "Length of particle trails (longer = more pronounced tails)",
            MinValue = 0.5f,
            MaxValue = 2.0f,
            DefaultValue = 1.0f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "goldIntensity",
            DisplayName = "Gold Intensity",
            Description = "How gold/amber the particles appear (0.5 = subtle, 1.0 = pure gold)",
            MinValue = 0.5f,
            MaxValue = 1.0f,
            DefaultValue = 0.8f,
            Step = 0.05f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "clusterCount":
                _clusterCount = Convert.ToInt32(value);
                break;
            case "tailLength":
                _tailLength = Convert.ToSingle(value);
                break;
            case "goldIntensity":
                _goldIntensity = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "clusterCount" => _clusterCount,
            "tailLength" => _tailLength,
            "goldIntensity" => _goldIntensity,
            _ => base.GetParameter(key)
        };
    }
}
