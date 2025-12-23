using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Fish style - silver stars that wriggle and swim away from center.
/// Sinusoidal swimming motion with silver/blue colors and medium trails.
/// </summary>
public class FishStyle : FireworkStyleBase
{
    public override string Name => "Fish";
    public override string Description => "Silver stars that wriggle and swim away from the centre in a mesmerising dance";
    public override int StyleId => 12;

    // Style-specific parameters
    private float _wiggleFrequency = 5f;
    private float _wiggleAmplitude = 40f;
    private float _swimSpeed = 1.2f;

    public float WiggleFrequency
    {
        get => _wiggleFrequency;
        set => _wiggleFrequency = value;
    }

    public float WiggleAmplitude
    {
        get => _wiggleAmplitude;
        set => _wiggleAmplitude = value;
    }

    public float SwimSpeed
    {
        get => _swimSpeed;
        set => _swimSpeed = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.0f,
        Gravity = 80f,                   // Low gravity for swimming effect
        Drag = 0.97f,                    // Medium drag
        MinParticlesPerFirework = 40,
        MaxParticlesPerFirework = 100,
        ExplosionForce = 200f,
        MinParticleSize = 3f,
        MaxParticleSize = 6f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,
        StyleSpecific = new Dictionary<string, object>
        {
            ["wiggleFrequency"] = 5f,
            ["wiggleAmplitude"] = 40f,
            ["swimSpeed"] = 1.2f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = wiggle phase offset (randomized for variety)
        p.StyleData1 = ctx.GetRandomFloat() * MathF.PI * 2f;

        // StyleData2 = wiggle frequency multiplier (each fish slightly different)
        p.StyleData2 = _wiggleFrequency * (0.8f + ctx.GetRandomFloat() * 0.4f);

        // StyleData3 = swim direction angle (perpendicular to velocity)
        float velocityAngle = MathF.Atan2(p.Velocity.Y, p.Velocity.X);
        p.StyleData3 = velocityAngle;

        // Apply swim speed multiplier
        p.Velocity *= _swimSpeed;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Calculate wiggle motion perpendicular to direction of travel
        float wigglePhase = particle.StyleData1 + time * particle.StyleData2;
        float wiggleOffset = MathF.Sin(wigglePhase) * _wiggleAmplitude;

        // Calculate perpendicular direction
        float angle = particle.StyleData3;
        float perpAngle = angle + MathF.PI / 2f;

        // Apply wiggle in perpendicular direction
        float wiggleX = MathF.Cos(perpAngle) * wiggleOffset * dt;
        float wiggleY = MathF.Sin(perpAngle) * wiggleOffset * dt;

        particle.Velocity.X += wiggleX;
        particle.Velocity.Y += wiggleY;

        // Update swim direction based on current velocity
        if (particle.Velocity.LengthSquared() > 0.1f)
        {
            particle.StyleData3 = MathF.Atan2(particle.Velocity.Y, particle.Velocity.X);
        }

        // Add slight forward propulsion (swimming motion)
        float currentAngle = particle.StyleData3;
        particle.Velocity.X += MathF.Cos(currentAngle) * _swimSpeed * 20f * dt;
        particle.Velocity.Y += MathF.Sin(currentAngle) * _swimSpeed * 20f * dt;
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Wiggle phase
        gpu.StyleData2 = particle.StyleData2;  // Wiggle frequency
        gpu.StyleData3 = particle.StyleData3;  // Swim direction
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        // Silver/blue tinted colors for fish effect
        var color = base.GetParticleColor(ctx, baseColor, isSecondary, index, total);

        // Apply silver-blue tint
        float silver = 0.8f + ctx.GetRandomFloat() * 0.2f;
        color.X = color.X * 0.7f + silver * 0.3f;  // Add white to red channel
        color.Y = color.Y * 0.7f + silver * 0.3f;  // Add white to green channel
        color.Z = MathF.Min(1f, color.Z * 1.2f);   // Boost blue channel

        return color;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Moderate lifespan variation for swimming fish
        return ctx.ParticleLifespan * (0.8f + ctx.GetRandomFloat() * 0.4f);
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "wiggleFrequency",
            DisplayName = "Wiggle Frequency",
            Description = "How fast the fish wiggle (Hz)",
            MinValue = 1f,
            MaxValue = 15f,
            DefaultValue = 5f,
            Step = 0.5f
        };
        yield return new FloatStyleParameter
        {
            Key = "wiggleAmplitude",
            DisplayName = "Wiggle Amplitude",
            Description = "How much the fish wiggle side to side",
            MinValue = 10f,
            MaxValue = 100f,
            DefaultValue = 40f,
            Step = 5f
        };
        yield return new FloatStyleParameter
        {
            Key = "swimSpeed",
            DisplayName = "Swim Speed",
            Description = "How fast the fish swim",
            MinValue = 0.5f,
            MaxValue = 2.5f,
            DefaultValue = 1.2f,
            Step = 0.1f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "wiggleFrequency":
                _wiggleFrequency = Convert.ToSingle(value);
                break;
            case "wiggleAmplitude":
                _wiggleAmplitude = Convert.ToSingle(value);
                break;
            case "swimSpeed":
                _swimSpeed = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "wiggleFrequency" => _wiggleFrequency,
            "wiggleAmplitude" => _wiggleAmplitude,
            "swimSpeed" => _swimSpeed,
            _ => base.GetParameter(key)
        };
    }
}
