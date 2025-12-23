using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Crossette style - Stars that split into four or five stars in a cross pattern.
/// Each star splits after a delay, creating distinctive cross/X patterns as they map the skies.
/// </summary>
public class CrossetteStyle : FireworkStyleBase
{
    public override string Name => "Crossette";
    public override string Description => "Stars that split into four or five stars and leave a trail as they map the skies";
    public override int StyleId => 8;

    // Style-specific parameters
    private int _splitCount = 4;          // Number of stars each particle splits into
    private float _splitDelay = 0.5f;     // Seconds before split occurs
    private float _splitForce = 100f;     // Force of the split

    public int SplitCount
    {
        get => _splitCount;
        set => _splitCount = value;
    }

    public float SplitDelay
    {
        get => _splitDelay;
        set => _splitDelay = value;
    }

    public float SplitForce
    {
        get => _splitForce;
        set => _splitForce = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.5f,
        Gravity = 100f,
        Drag = 0.98f,
        MinParticlesPerFirework = 20,
        MaxParticlesPerFirework = 40,
        ExplosionForce = 250f,
        MinParticleSize = 5f,
        MaxParticleSize = 9f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,  // Splits replace secondary
        StyleSpecific = new Dictionary<string, object>
        {
            ["splitCount"] = 4,
            ["splitDelay"] = 0.5f,
            ["splitForce"] = 100f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = age timer (counts up to trigger split)
        p.StyleData1 = 0f;

        // StyleData2 = split flag (0 = not split, 1 = has split)
        p.StyleData2 = isSecondary ? 1f : 0f;  // Secondary particles (split stars) can't split again

        // StyleData3 = spawn time for reference
        p.StyleData3 = ctx.Time;

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
        // Track age in StyleData1
        particle.StyleData1 += dt;

        // Note: Actual split spawning happens in the effect's update loop
        // because we need access to the particle pool
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Age timer
        gpu.StyleData2 = particle.StyleData2;  // Split flag
        gpu.StyleData3 = particle.StyleData3;  // Spawn time
        gpu.StyleFlags = (uint)StyleId;
    }

    /// <summary>
    /// Called from effect update loop to determine if a particle should split.
    /// Returns true if a split should occur.
    /// </summary>
    public bool ShouldSplit(ref FireworkParticle particle)
    {
        // Only split if:
        // 1. Has not split yet (StyleData2 == 0)
        // 2. Has lived long enough (StyleData1 >= _splitDelay)
        // 3. Still has significant life left
        if (particle.StyleData2 == 0f &&
            particle.StyleData1 >= _splitDelay &&
            particle.Life > particle.MaxLife * 0.3f)
        {
            particle.StyleData2 = 1f;  // Mark as split
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a split star particle from a parent particle.
    /// Split stars form a cross pattern radiating outward.
    /// </summary>
    public FireworkParticle CreateSplitStar(in FireworkParticle parent, FireworkContext ctx, int splitIndex)
    {
        // Calculate split angle - distribute evenly in cross pattern
        float angleStep = 360f / _splitCount;
        float baseAngle = angleStep * splitIndex;

        // Add slight randomization to break perfect symmetry
        float angle = (baseAngle + (ctx.GetRandomFloat() - 0.5f) * 15f) * MathF.PI / 180f;

        // Calculate split velocity - perpendicular to parent movement with split force
        Vector2 splitDirection = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 velocity = parent.Velocity * 0.4f + splitDirection * _splitForce;

        // Inherit color but slightly dimmer
        Vector4 color = parent.Color * 0.85f;
        color.W = 1f;

        // Split stars have shorter lifespan
        float lifespan = parent.Life * 0.7f;

        return new FireworkParticle
        {
            Position = parent.Position,
            Velocity = velocity,
            Color = color,
            Size = parent.Size * 0.7f,
            Life = lifespan,
            MaxLife = lifespan,
            CanExplode = false,
            HasExploded = true,  // Prevent any further explosions
            StyleId = StyleId,
            StyleData1 = 0f,      // Reset age
            StyleData2 = 1f,      // Mark as already split
            StyleData3 = ctx.Time
        };
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new IntStyleParameter
        {
            Key = "splitCount",
            DisplayName = "Split Count",
            Description = "Number of stars each particle splits into (higher = denser cross pattern)",
            MinValue = 3,
            MaxValue = 6,
            DefaultValue = 4
        };
        yield return new FloatStyleParameter
        {
            Key = "splitDelay",
            DisplayName = "Split Delay",
            Description = "Seconds before stars split (higher = stars travel farther before splitting)",
            MinValue = 0.3f,
            MaxValue = 1.0f,
            DefaultValue = 0.5f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "splitForce",
            DisplayName = "Split Force",
            Description = "Force of the split (higher = split stars spread wider)",
            MinValue = 50f,
            MaxValue = 200f,
            DefaultValue = 100f,
            Step = 10f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "splitCount":
                _splitCount = Convert.ToInt32(value);
                break;
            case "splitDelay":
                _splitDelay = Convert.ToSingle(value);
                break;
            case "splitForce":
                _splitForce = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "splitCount" => _splitCount,
            "splitDelay" => _splitDelay,
            "splitForce" => _splitForce,
            _ => base.GetParameter(key)
        };
    }
}
