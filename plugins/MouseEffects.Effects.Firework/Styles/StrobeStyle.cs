using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Strobe style - flickering flashing particles.
/// Rapid ON/OFF flashing like camera flash bulbs, bright white flashes with random timing.
/// </summary>
public class StrobeStyle : FireworkStyleBase
{
    public override string Name => "Strobe";
    public override string Description => "An effect that flickers and flashes in the sky like a gallery of flash bulbs";
    public override int StyleId => 17;

    // Style-specific parameters
    private float _flashRate = 15f;
    private float _flashDuration = 0.08f;
    private float _flashBrightness = 2.5f;

    public float FlashRate
    {
        get => _flashRate;
        set => _flashRate = value;
    }

    public float FlashDuration
    {
        get => _flashDuration;
        set => _flashDuration = value;
    }

    public float FlashBrightness
    {
        get => _flashBrightness;
        set => _flashBrightness = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 2.0f,
        Gravity = 150f,
        Drag = 0.98f,
        MinParticlesPerFirework = 60,
        MaxParticlesPerFirework = 150,
        ExplosionForce = 280f,
        MinParticleSize = 4f,
        MaxParticleSize = 10f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,
        StyleSpecific = new Dictionary<string, object>
        {
            ["flashRate"] = 15f,
            ["flashDuration"] = 0.08f,
            ["flashBrightness"] = 2.5f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = next flash time (randomized)
        p.StyleData1 = ctx.GetRandomFloat() / _flashRate;

        // StyleData2 = flash duration for this particle
        p.StyleData2 = _flashDuration * (0.8f + ctx.GetRandomFloat() * 0.4f);

        // StyleData3 = flash state: 0 = off, 1 = on, values between = transitioning
        p.StyleData3 = 0f;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        float particleAge = particle.MaxLife - particle.Life;

        // Check if it's time for next flash
        if (particleAge >= particle.StyleData1 && particle.StyleData3 <= 0f)
        {
            // Start flash
            particle.StyleData3 = 1f;

            // Schedule next flash at random interval
            float flashInterval = (1f / _flashRate) * (0.7f + ((particle.StyleData1 * 1000f) % 1f) * 0.6f);
            particle.StyleData1 = particleAge + flashInterval;
        }

        // Handle flash state
        if (particle.StyleData3 > 0f)
        {
            // Flash is on, count down duration
            particle.StyleData3 -= dt / particle.StyleData2;
            if (particle.StyleData3 < 0f)
            {
                particle.StyleData3 = 0f;
            }
        }
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Next flash time
        gpu.StyleData2 = particle.StyleData2;  // Flash duration
        gpu.StyleData3 = particle.StyleData3;  // Flash state (0=off, 1=on)
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        // Bright white/tinted colors for strobe effect
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Add significant white component for bright flash
        float whiteMix = 0.6f + ctx.GetRandomFloat() * 0.3f;
        color.X = color.X * (1f - whiteMix) + whiteMix;
        color.Y = color.Y * (1f - whiteMix) + whiteMix;
        color.Z = color.Z * (1f - whiteMix) + whiteMix;

        // Boost overall brightness
        color.X = MathF.Min(1f, color.X * _flashBrightness);
        color.Y = MathF.Min(1f, color.Y * _flashBrightness);
        color.Z = MathF.Min(1f, color.Z * _flashBrightness);

        return color;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Moderate lifespan for multiple flashes
        return ctx.ParticleLifespan * (0.7f + ctx.GetRandomFloat() * 0.6f);
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "flashRate",
            DisplayName = "Flash Rate",
            Description = "Average flashes per second (Hz)",
            MinValue = 2f,
            MaxValue = 30f,
            DefaultValue = 15f,
            Step = 1f
        };
        yield return new FloatStyleParameter
        {
            Key = "flashDuration",
            DisplayName = "Flash Duration",
            Description = "How long each flash lasts (seconds)",
            MinValue = 0.02f,
            MaxValue = 0.3f,
            DefaultValue = 0.08f,
            Step = 0.01f
        };
        yield return new FloatStyleParameter
        {
            Key = "flashBrightness",
            DisplayName = "Flash Brightness",
            Description = "Brightness multiplier for flashes",
            MinValue = 1f,
            MaxValue = 5f,
            DefaultValue = 2.5f,
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
            case "flashDuration":
                _flashDuration = Convert.ToSingle(value);
                break;
            case "flashBrightness":
                _flashBrightness = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "flashRate" => _flashRate,
            "flashDuration" => _flashDuration,
            "flashBrightness" => _flashBrightness,
            _ => base.GetParameter(key)
        };
    }
}
