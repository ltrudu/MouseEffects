using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Comet style - star projectiles with glittering trails.
/// Fewer but larger main particles that travel in arcs, leaving sparkling trails behind them.
/// </summary>
public class CometStyle : FireworkStyleBase
{
    public override string Name => "Comet";
    public override string Description => "A star projectile which leaves a glittering trail behind it";
    public override int StyleId => 7;

    // Style-specific parameters
    private int _trailDensity = 10;
    private float _trailSparkle = 0.7f;
    private float _cometSize = 2.0f;

    public int TrailDensity
    {
        get => _trailDensity;
        set => _trailDensity = value;
    }

    public float TrailSparkle
    {
        get => _trailSparkle;
        set => _trailSparkle = value;
    }

    public float CometSize
    {
        get => _cometSize;
        set => _cometSize = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.5f,      // Longer life for arc trajectory
        Gravity = 180f,               // Moderate gravity for arc motion
        Drag = 0.99f,                 // Low drag for longer travel
        MinParticlesPerFirework = 15, // Fewer main particles
        MaxParticlesPerFirework = 30,
        ExplosionForce = 400f,        // Higher force for longer travel
        MinParticleSize = 6f,         // Larger particles
        MaxParticleSize = 12f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false, // No secondary for comet
        StyleSpecific = new Dictionary<string, object>
        {
            ["trailDensity"] = 10,
            ["trailSparkle"] = 0.7f,
            ["cometSize"] = 2.0f
        }
    };

    protected override int GetParticleCount(int baseCount, float force, bool isSecondary)
    {
        // Fewer main particles for comet effect
        return (int)(base.GetParticleCount(baseCount, force, isSecondary) * 0.5f);
    }

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = trail spawn timer (accumulates over time)
        p.StyleData1 = 0f;

        // StyleData2 = sparkle seed (for random brightness variation)
        p.StyleData2 = ctx.GetRandomFloat() * 1000f;

        // StyleData3 = is this a trail particle? (0 = main comet, 1 = trail)
        p.StyleData3 = 0f;

        // Add slight upward bias for arc trajectory
        p.Velocity.Y -= 100f + ctx.GetRandomFloat() * 100f;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Only main comet particles spawn trails (StyleData3 == 0)
        if (particle.StyleData3 < 0.5f)
        {
            // Accumulate trail spawn timer
            particle.StyleData1 += dt;
        }
    }

    // Trail particle spawning - comets leave glittering trails
    public override bool HasTrailParticles => true;

    public override bool ShouldSpawnTrail(ref FireworkParticle particle, float dt)
    {
        // Only main comet particles spawn trails (StyleData3 == 0 means main particle)
        if (particle.StyleData3 >= 0.5f) return false;

        float spawnInterval = 1.0f / _trailDensity;
        if (particle.StyleData1 >= spawnInterval)
        {
            particle.StyleData1 = 0f; // Reset timer
            return true;
        }
        return false;
    }

    public override FireworkParticle CreateTrailParticle(in FireworkParticle parent, FireworkContext ctx)
    {
        // Trail inherits small portion of velocity with sparkle variation
        Vector2 velocity = parent.Velocity * 0.15f;
        float sparkleOffset = MathF.Sin(parent.StyleData2 + ctx.Time * 10f) * _trailSparkle;
        velocity.X += (ctx.GetRandomFloat() - 0.5f) * 30f;
        velocity.Y += (ctx.GetRandomFloat() - 0.5f) * 30f;

        // Dimmer trail color
        Vector4 color = parent.Color * 0.7f;
        color.W = 1f;

        float lifespan = 0.3f + ctx.GetRandomFloat() * 0.3f;

        return new FireworkParticle
        {
            Position = parent.Position,
            Velocity = velocity,
            Color = color,
            Size = parent.Size * 0.35f,
            Life = lifespan,
            MaxLife = lifespan,
            CanExplode = false,
            HasExploded = true,
            StyleId = StyleId,
            StyleData1 = 0f,
            StyleData2 = parent.StyleData2 + ctx.GetRandomFloat() * 100f,
            StyleData3 = 1f // Mark as trail particle
        };
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Trail spawn timer
        gpu.StyleData2 = particle.StyleData2;  // Sparkle seed
        gpu.StyleData3 = particle.StyleData3;  // Is trail particle flag
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Larger comet particles
        return base.GetParticleSize(ctx, isSecondary) * _cometSize;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Longer lifespan for arc trajectory
        return base.GetParticleLifespan(ctx, isSecondary) * 1.8f;
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Brighter core color for comet particles
        color.X = MathF.Min(1f, color.X * 1.2f);
        color.Y = MathF.Min(1f, color.Y * 1.2f);
        color.Z = MathF.Min(1f, color.Z * 1.2f);

        return color;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new IntStyleParameter
        {
            Key = "trailDensity",
            DisplayName = "Trail Density",
            Description = "Particles spawned per main particle per second",
            MinValue = 5,
            MaxValue = 20,
            DefaultValue = 10
        };
        yield return new FloatStyleParameter
        {
            Key = "trailSparkle",
            DisplayName = "Trail Sparkle",
            Description = "Sparkle intensity (random brightness variation)",
            MinValue = 0.3f,
            MaxValue = 1.0f,
            DefaultValue = 0.7f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "cometSize",
            DisplayName = "Comet Size",
            Description = "Size multiplier for main comet particles",
            MinValue = 1.0f,
            MaxValue = 3.0f,
            DefaultValue = 2.0f,
            Step = 0.1f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "trailDensity":
                _trailDensity = Convert.ToInt32(value);
                break;
            case "trailSparkle":
                _trailSparkle = Convert.ToSingle(value);
                break;
            case "cometSize":
                _cometSize = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "trailDensity" => _trailDensity,
            "trailSparkle" => _trailSparkle,
            "cometSize" => _cometSize,
            _ => base.GetParameter(key)
        };
    }
}
