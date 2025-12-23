using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Stars style - glowing, flaming balls of colored light.
/// Larger, brighter particles that can rise upward or burst into mini star-shells.
/// </summary>
public class StarsStyle : FireworkStyleBase
{
    public override string Name => "Stars";
    public override string Description => "A glowing, flaming ball of coloured light that can rise on their own or burst into variegated mini star-shells";
    public override int StyleId => 15;

    // Style-specific parameters
    private float _riseChance = 0.3f;
    private float _flameIntensity = 1.0f;
    private float _burstChance = 0.1f;

    public float RiseChance
    {
        get => _riseChance;
        set => _riseChance = value;
    }

    public float FlameIntensity
    {
        get => _flameIntensity;
        set => _flameIntensity = value;
    }

    public float BurstChance
    {
        get => _burstChance;
        set => _burstChance = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.5f,
        Gravity = 100f,               // Lower gravity for rising effect
        Drag = 0.98f,
        MinParticlesPerFirework = 30,
        MaxParticlesPerFirework = 60,
        ExplosionForce = 300f,
        MinParticleSize = 8f,         // Larger particles
        MaxParticleSize = 14f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = true,  // Mini star-shell bursts
        StyleSpecific = new Dictionary<string, object>
        {
            ["riseChance"] = 0.3f,
            ["flameIntensity"] = 1.0f,
            ["burstChance"] = 0.1f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = flame flicker phase (for brightness variation)
        p.StyleData1 = ctx.GetRandomFloat() * 1000f;

        // StyleData2 = rise factor (0 = normal, 1 = rising star)
        p.StyleData2 = ctx.GetRandomFloat() < _riseChance ? 1f : 0f;

        // StyleData3 = burst trigger timer (accumulates for potential burst)
        p.StyleData3 = 0f;

        // Rising stars get upward velocity
        if (p.StyleData2 > 0.5f)
        {
            p.Velocity.Y -= 200f + ctx.GetRandomFloat() * 150f;
        }

        // Make particles brighter and more vibrant
        p.Color.X = MathF.Min(1f, p.Color.X * 1.4f);
        p.Color.Y = MathF.Min(1f, p.Color.Y * 1.4f);
        p.Color.Z = MathF.Min(1f, p.Color.Z * 1.4f);

        // Secondary burst particles are smaller and less bright
        if (isSecondary)
        {
            p.Color *= 0.8f;
            p.Color.W = 1f;
            p.StyleData2 = 0f; // Secondary stars don't rise
            p.CanExplode = false; // No recursive bursts
        }
        else
        {
            // Main stars can burst into mini star-shells
            p.CanExplode = ctx.GetRandomFloat() < _burstChance;
        }
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Update flame flicker phase
        particle.StyleData1 += dt * 5f * _flameIntensity;

        // Apply rising effect (negative gravity)
        if (particle.StyleData2 > 0.5f)
        {
            particle.Velocity.Y -= 80f * dt;
        }

        // Accumulate burst timer for main particles
        if (particle.CanExplode && !particle.HasExploded)
        {
            particle.StyleData3 += dt;

            // Trigger burst after random time (30-70% through lifetime)
            float burstTime = particle.MaxLife * (0.3f + particle.StyleData1 % 1f * 0.4f);
            if (particle.StyleData3 >= burstTime)
            {
                particle.HasExploded = true;
                // Actual burst creation handled by effect system
            }
        }
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Flame flicker phase
        gpu.StyleData2 = particle.StyleData2;  // Rise factor
        gpu.StyleData3 = particle.StyleData3;  // Burst timer
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Larger main particles, smaller burst particles
        float baseSize = base.GetParticleSize(ctx, isSecondary);
        return isSecondary ? baseSize * 0.5f : baseSize * 1.5f;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Secondary burst particles have shorter lifespan
        return isSecondary
            ? base.GetParticleLifespan(ctx, isSecondary) * 0.6f
            : base.GetParticleLifespan(ctx, isSecondary) * 1.2f;
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Very bright, glowing colors
        color.X = MathF.Min(1f, color.X * 1.5f);
        color.Y = MathF.Min(1f, color.Y * 1.5f);
        color.Z = MathF.Min(1f, color.Z * 1.5f);

        return color;
    }

    protected override int GetParticleCount(int baseCount, float force, bool isSecondary)
    {
        // More particles in secondary bursts for dense mini star-shell effect
        return isSecondary
            ? (int)(base.GetParticleCount(baseCount, force, isSecondary) * 1.5f)
            : base.GetParticleCount(baseCount, force, isSecondary);
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "riseChance",
            DisplayName = "Rise Chance",
            Description = "Probability that a star rises upward instead of falling",
            MinValue = 0.1f,
            MaxValue = 0.5f,
            DefaultValue = 0.3f,
            Step = 0.05f
        };
        yield return new FloatStyleParameter
        {
            Key = "flameIntensity",
            DisplayName = "Flame Intensity",
            Description = "Intensity of the flickering flame effect",
            MinValue = 0.5f,
            MaxValue = 1.5f,
            DefaultValue = 1.0f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "burstChance",
            DisplayName = "Burst Chance",
            Description = "Probability that a star bursts into mini star-shells",
            MinValue = 0.0f,
            MaxValue = 0.3f,
            DefaultValue = 0.1f,
            Step = 0.05f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "riseChance":
                _riseChance = Convert.ToSingle(value);
                break;
            case "flameIntensity":
                _flameIntensity = Convert.ToSingle(value);
                break;
            case "burstChance":
                _burstChance = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "riseChance" => _riseChance,
            "flameIntensity" => _flameIntensity,
            "burstChance" => _burstChance,
            _ => base.GetParameter(key)
        };
    }
}
