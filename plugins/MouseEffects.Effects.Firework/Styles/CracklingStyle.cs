using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Crackling style - popping/flashing sparks.
/// Many small particles with random flash on/off effect.
/// </summary>
public class CracklingStyle : FireworkStyleBase
{
    public override string Name => "Crackling";
    public override string Description => "Popping sparks that flash and crackle randomly";
    public override int StyleId => 3;

    // Style-specific parameters
    private float _flashRate = 20f;
    private float _popIntensity = 0.5f;
    private float _particleMultiplier = 2f;

    public float FlashRate
    {
        get => _flashRate;
        set => _flashRate = value;
    }

    public float PopIntensity
    {
        get => _popIntensity;
        set => _popIntensity = value;
    }

    public float ParticleMultiplier
    {
        get => _particleMultiplier;
        set => _particleMultiplier = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 1.5f,   // Shorter life for snappy effect
        Gravity = 200f,
        Drag = 0.97f,
        MinParticlesPerFirework = 100,
        MaxParticlesPerFirework = 300,  // Lots of particles
        ExplosionForce = 350f,          // Higher force for scatter
        MinParticleSize = 2f,           // Small particles
        MaxParticleSize = 4f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,
        StyleSpecific = new Dictionary<string, object>
        {
            ["flashRate"] = 20f,
            ["popIntensity"] = 0.5f,
            ["particleMultiplier"] = 2f
        }
    };

    protected override int GetParticleCount(int baseCount, float force, bool isSecondary)
    {
        // Much more particles for crackling effect
        return (int)(base.GetParticleCount(baseCount, force, isSecondary) * _particleMultiplier);
    }

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = flash phase (randomized starting point)
        p.StyleData1 = ctx.GetRandomFloat() * MathF.PI * 2f;

        // StyleData2 = flash frequency (slightly randomized per particle)
        p.StyleData2 = _flashRate * (0.5f + ctx.GetRandomFloat());

        // StyleData3 = jitter seed for position randomization
        p.StyleData3 = ctx.GetRandomFloat() * 1000f;

        // Add random velocity jitter for scattered effect
        p.Velocity.X += (ctx.GetRandomFloat() - 0.5f) * 100f * _popIntensity;
        p.Velocity.Y += (ctx.GetRandomFloat() - 0.5f) * 100f * _popIntensity;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Add random position jitter (the "pop" effect)
        float jitterAmount = _popIntensity * 2f;
        float jitterPhase = particle.StyleData3 + time * 50f;

        // Use sin waves with the jitter seed to create pseudo-random jitter
        float jitterX = MathF.Sin(jitterPhase * 1.7f) * MathF.Cos(jitterPhase * 2.3f);
        float jitterY = MathF.Cos(jitterPhase * 1.3f) * MathF.Sin(jitterPhase * 1.9f);

        particle.Velocity.X += jitterX * jitterAmount;
        particle.Velocity.Y += jitterY * jitterAmount;
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Flash phase
        gpu.StyleData2 = particle.StyleData2;  // Flash frequency
        gpu.StyleData3 = particle.StyleData3;  // Jitter seed
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Crackling particles are small
        return base.GetParticleSize(ctx, isSecondary) * 0.5f;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Shorter, more varied lifespan
        return ctx.ParticleLifespan * (0.3f + ctx.GetRandomFloat() * 0.5f);
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        // Brighter, more saturated colors for the flash effect
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Boost brightness
        color.X = MathF.Min(1f, color.X * 1.3f);
        color.Y = MathF.Min(1f, color.Y * 1.3f);
        color.Z = MathF.Min(1f, color.Z * 1.3f);

        return color;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "flashRate",
            DisplayName = "Flash Rate",
            Description = "Frequency of flashing (Hz)",
            MinValue = 5f,
            MaxValue = 50f,
            DefaultValue = 20f,
            Step = 1f
        };
        yield return new FloatStyleParameter
        {
            Key = "popIntensity",
            DisplayName = "Pop Intensity",
            Description = "Amount of position jitter for pop effect",
            MinValue = 0f,
            MaxValue = 1f,
            DefaultValue = 0.5f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "particleMultiplier",
            DisplayName = "Particle Density",
            Description = "Multiply particle count for denser crackling (higher = more particles, may impact performance)",
            MinValue = 0.1f,
            MaxValue = 10f,
            DefaultValue = 2f,
            Step = 0.1f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "flashRate":
                _flashRate = Convert.ToSingle(value);
                break;
            case "popIntensity":
                _popIntensity = Convert.ToSingle(value);
                break;
            case "particleMultiplier":
                _particleMultiplier = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "flashRate" => _flashRate,
            "popIntensity" => _popIntensity,
            "particleMultiplier" => _particleMultiplier,
            _ => base.GetParameter(key)
        };
    }
}
