using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Glitter style - constant spray of sparkling particles.
/// Many small particles with random brightness variation, like fountain sparkles.
/// </summary>
public class GlitterStyle : FireworkStyleBase
{
    public override string Name => "Glitter";
    public override string Description => "A constant spray of strobing glitter effect usually seen in cones and fountains";
    public override int StyleId => 18;

    // Style-specific parameters
    private float _sparkleRate = 25f;
    private float _sparkleIntensity = 1.5f;
    private float _particleDensity = 3f;

    public float SparkleRate
    {
        get => _sparkleRate;
        set => _sparkleRate = value;
    }

    public float SparkleIntensity
    {
        get => _sparkleIntensity;
        set => _sparkleIntensity = value;
    }

    public float ParticleDensity
    {
        get => _particleDensity;
        set => _particleDensity = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 1.8f,         // Shorter life for constant spray effect
        Gravity = 180f,
        Drag = 0.96f,
        MinParticlesPerFirework = 150,   // Many particles
        MaxParticlesPerFirework = 400,
        ExplosionForce = 320f,
        MinParticleSize = 1.5f,          // Small sparkly particles
        MaxParticleSize = 4f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,
        StyleSpecific = new Dictionary<string, object>
        {
            ["sparkleRate"] = 25f,
            ["sparkleIntensity"] = 1.5f,
            ["particleDensity"] = 3f
        }
    };

    protected override int GetParticleCount(int baseCount, float force, bool isSecondary)
    {
        // Many more particles for dense glitter effect
        return (int)(base.GetParticleCount(baseCount, force, isSecondary) * _particleDensity);
    }

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = sparkle phase (randomized starting point)
        p.StyleData1 = ctx.GetRandomFloat() * MathF.PI * 2f;

        // StyleData2 = sparkle frequency (randomized per particle for variety)
        p.StyleData2 = _sparkleRate * (0.5f + ctx.GetRandomFloat() * 1.5f);

        // StyleData3 = brightness variation seed
        p.StyleData3 = ctx.GetRandomFloat() * 1000f;

        // Add random velocity variation for spray effect
        float spreadFactor = 1.0f + ctx.GetRandomFloat() * 0.5f;
        p.Velocity *= spreadFactor;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Continuous sparkle calculation using sin wave
        float sparklePhase = particle.StyleData1 + time * particle.StyleData2;

        // Add micro-jitter for sparkle effect
        float jitterPhase = particle.StyleData3 + time * 30f;
        float jitterX = MathF.Sin(jitterPhase * 1.3f) * 15f * _sparkleIntensity;
        float jitterY = MathF.Cos(jitterPhase * 1.7f) * 15f * _sparkleIntensity;

        particle.Velocity.X += jitterX * dt;
        particle.Velocity.Y += jitterY * dt;

        // Update sparkle phase for GPU
        particle.StyleData1 = sparklePhase;
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Sparkle phase
        gpu.StyleData2 = particle.StyleData2;  // Sparkle frequency
        gpu.StyleData3 = particle.StyleData3;  // Brightness variation seed
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Very small particles for glitter effect
        return base.GetParticleSize(ctx, isSecondary) * 0.4f;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Short, varied lifespan for constant spray
        return ctx.ParticleLifespan * (0.5f + ctx.GetRandomFloat() * 0.7f);
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        // Bright, varied colors with metallic sheen
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Add golden/silver metallic tint
        float metallicTint = ctx.GetRandomFloat();
        if (metallicTint > 0.5f)
        {
            // Gold tint
            color.X = MathF.Min(1f, color.X * 1.3f);
            color.Y = MathF.Min(1f, color.Y * 1.2f);
            color.Z = color.Z * 0.8f;
        }
        else
        {
            // Silver tint
            float silver = 0.9f + ctx.GetRandomFloat() * 0.1f;
            color.X = color.X * 0.6f + silver * 0.4f;
            color.Y = color.Y * 0.6f + silver * 0.4f;
            color.Z = color.Z * 0.6f + silver * 0.4f;
        }

        // Boost brightness for sparkle
        color.X = MathF.Min(1f, color.X * _sparkleIntensity);
        color.Y = MathF.Min(1f, color.Y * _sparkleIntensity);
        color.Z = MathF.Min(1f, color.Z * _sparkleIntensity);

        return color;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "sparkleRate",
            DisplayName = "Sparkle Rate",
            Description = "Frequency of sparkle variation (Hz)",
            MinValue = 5f,
            MaxValue = 50f,
            DefaultValue = 25f,
            Step = 1f
        };
        yield return new FloatStyleParameter
        {
            Key = "sparkleIntensity",
            DisplayName = "Sparkle Intensity",
            Description = "Brightness variation and jitter intensity",
            MinValue = 0.5f,
            MaxValue = 3f,
            DefaultValue = 1.5f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "particleDensity",
            DisplayName = "Particle Density",
            Description = "Multiply particle count for denser glitter (higher = more particles, may impact performance)",
            MinValue = 0.5f,
            MaxValue = 10f,
            DefaultValue = 3f,
            Step = 0.5f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "sparkleRate":
                _sparkleRate = Convert.ToSingle(value);
                break;
            case "sparkleIntensity":
                _sparkleIntensity = Convert.ToSingle(value);
                break;
            case "particleDensity":
                _particleDensity = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "sparkleRate" => _sparkleRate,
            "sparkleIntensity" => _sparkleIntensity,
            "particleDensity" => _particleDensity,
            _ => base.GetParameter(key)
        };
    }
}
