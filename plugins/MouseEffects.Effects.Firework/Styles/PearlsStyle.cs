using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Pearls style - bright glowing stars that rise upward and dissolve.
/// Particles rise with negative gravity, no trails, slowly dissolve into the heavens.
/// </summary>
public class PearlsStyle : FireworkStyleBase
{
    public override string Name => "Pearls";
    public override string Description => "Bright glowing stars that do not leave a trail but silently rise and dissolve into the heavens";
    public override int StyleId => 11;

    // Style-specific parameters
    private float _riseSpeed = 150f;
    private float _glowPulse = 2f;
    private float _dissolveRate = 1f;

    public float RiseSpeed
    {
        get => _riseSpeed;
        set => _riseSpeed = value;
    }

    public float GlowPulse
    {
        get => _glowPulse;
        set => _glowPulse = value;
    }

    public float DissolveRate
    {
        get => _dissolveRate;
        set => _dissolveRate = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.5f,        // Longer life for slow rise
        Gravity = -150f,                 // Negative gravity (rise upward)
        Drag = 0.99f,                    // Very low drag for smooth rise
        MinParticlesPerFirework = 30,
        MaxParticlesPerFirework = 80,   // Moderate count for pearls
        ExplosionForce = 80f,            // Gentle initial spread
        MinParticleSize = 4f,            // Larger glowing particles
        MaxParticleSize = 10f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,  // No secondary explosions
        StyleSpecific = new Dictionary<string, object>
        {
            ["riseSpeed"] = 150f,
            ["glowPulse"] = 2f,
            ["dissolveRate"] = 1f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = glow pulse phase (randomized starting point)
        p.StyleData1 = ctx.GetRandomFloat() * MathF.PI * 2f;

        // StyleData2 = glow pulse frequency (slightly randomized per particle)
        p.StyleData2 = _glowPulse * (0.8f + ctx.GetRandomFloat() * 0.4f);

        // StyleData3 = dissolve start time (when particle starts fading)
        p.StyleData3 = p.MaxLife * (0.6f + ctx.GetRandomFloat() * 0.2f);

        // Add upward velocity boost
        p.Velocity.Y -= _riseSpeed * (0.8f + ctx.GetRandomFloat() * 0.4f);

        // Reduce horizontal spread for more vertical rise
        p.Velocity.X *= 0.5f;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Add gentle upward drift
        particle.Velocity.Y -= _riseSpeed * 0.5f * dt;

        // Gradually slow horizontal movement (rise more vertically over time)
        particle.Velocity.X *= 0.992f;

        // Apply dissolve effect based on life remaining
        float age = particle.MaxLife - particle.Life;
        if (age > particle.StyleData3)
        {
            // Start dissolving after reaching StyleData3 threshold
            float dissolveProgress = (age - particle.StyleData3) / (particle.MaxLife - particle.StyleData3);
            particle.Size *= 1f - (dissolveProgress * _dissolveRate * dt * 0.5f);
        }
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Glow pulse phase
        gpu.StyleData2 = particle.StyleData2;  // Glow pulse frequency
        gpu.StyleData3 = particle.StyleData3;  // Dissolve start time
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Varied lifespan for staggered dissolve effect
        return ctx.ParticleLifespan * (0.7f + ctx.GetRandomFloat() * 0.6f);
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        // Bright, saturated colors for glowing pearls
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Boost brightness significantly
        color.X = MathF.Min(1f, color.X * 1.4f);
        color.Y = MathF.Min(1f, color.Y * 1.4f);
        color.Z = MathF.Min(1f, color.Z * 1.4f);

        return color;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "riseSpeed",
            DisplayName = "Rise Speed",
            Description = "How fast particles rise upward",
            MinValue = 50f,
            MaxValue = 300f,
            DefaultValue = 150f,
            Step = 10f
        };
        yield return new FloatStyleParameter
        {
            Key = "glowPulse",
            DisplayName = "Glow Pulse",
            Description = "Frequency of glow pulsing (Hz)",
            MinValue = 0.5f,
            MaxValue = 5f,
            DefaultValue = 2f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "dissolveRate",
            DisplayName = "Dissolve Rate",
            Description = "How quickly particles fade away",
            MinValue = 0.2f,
            MaxValue = 3f,
            DefaultValue = 1f,
            Step = 0.1f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "riseSpeed":
                _riseSpeed = Convert.ToSingle(value);
                break;
            case "glowPulse":
                _glowPulse = Convert.ToSingle(value);
                break;
            case "dissolveRate":
                _dissolveRate = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "riseSpeed" => _riseSpeed,
            "glowPulse" => _glowPulse,
            "dissolveRate" => _dissolveRate,
            _ => base.GetParameter(key)
        };
    }
}
