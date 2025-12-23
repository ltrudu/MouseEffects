using System;
using System.Collections.Generic;
using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Green Bees style - swarm of points of light that move and dissipate under their own power.
/// Similar to Fish but less vigorous, with erratic bee-like movement and green coloring.
/// </summary>
public class GreenBeesStyle : FireworkStyleBase
{
    public override string Name => "Green Bees";
    public override string Description => "A swarm of points of light that move and dissipate under their own power, similar to Fish but less vigorous";
    public override int StyleId => 13;

    // Style-specific parameters
    private float _buzzIntensity = 0.6f;
    private float _swarmDensity = 2.0f;
    private float _greenHue = 0.35f;

    public float BuzzIntensity
    {
        get => _buzzIntensity;
        set => _buzzIntensity = value;
    }

    public float SwarmDensity
    {
        get => _swarmDensity;
        set => _swarmDensity = value;
    }

    public float GreenHue
    {
        get => _greenHue;
        set => _greenHue = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 2.5f,   // Less persistent than Fish
        Gravity = 50f,             // Minimal gravity for floating effect
        Drag = 0.98f,              // Some drag to slow down over time
        MinParticlesPerFirework = 150,
        MaxParticlesPerFirework = 400,  // Many particles for swarm effect
        ExplosionForce = 180f,          // Moderate initial force
        MinParticleSize = 2f,           // Small bee-like particles
        MaxParticleSize = 4f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,
        StyleSpecific = new Dictionary<string, object>
        {
            ["buzzIntensity"] = 0.6f,
            ["swarmDensity"] = 2.0f,
            ["greenHue"] = 0.35f
        }
    };

    protected override int GetParticleCount(int baseCount, float force, bool isSecondary)
    {
        // More particles for dense swarm effect
        return (int)(base.GetParticleCount(baseCount, force, isSecondary) * _swarmDensity);
    }

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = buzz direction phase (controls erratic movement direction)
        p.StyleData1 = ctx.GetRandomFloat() * MathF.PI * 2f;

        // StyleData2 = buzz frequency (each bee has slightly different movement pattern)
        p.StyleData2 = (0.8f + ctx.GetRandomFloat() * 0.4f) * _buzzIntensity * 5f;

        // StyleData3 = individual movement seed for pseudo-random trajectories
        p.StyleData3 = ctx.GetRandomFloat() * 1000f;

        // Reduce initial velocity for more controlled swarm behavior
        p.Velocity *= 0.6f;

        // Add slight random initial direction change
        float angleOffset = (ctx.GetRandomFloat() - 0.5f) * MathF.PI;
        float speed = p.Velocity.Length();
        float currentAngle = MathF.Atan2(p.Velocity.Y, p.Velocity.X);
        float newAngle = currentAngle + angleOffset;
        p.Velocity.X = MathF.Cos(newAngle) * speed;
        p.Velocity.Y = MathF.Sin(newAngle) * speed;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Erratic bee-like movement - random direction changes
        float buzzPhase = particle.StyleData1 + time * particle.StyleData2;
        float buzzSeed = particle.StyleData3;

        // Create pseudo-random movement using multiple sine waves with different frequencies
        float moveX = MathF.Sin(buzzPhase * 1.3f + buzzSeed) * MathF.Cos(buzzPhase * 0.7f);
        float moveY = MathF.Cos(buzzPhase * 1.1f + buzzSeed) * MathF.Sin(buzzPhase * 0.9f);

        // Apply buzz intensity to movement
        float buzzForce = _buzzIntensity * 80f * dt;
        particle.Velocity.X += moveX * buzzForce;
        particle.Velocity.Y += moveY * buzzForce;

        // Add slight attraction toward center of swarm (clustering behavior)
        // This creates a more cohesive swarm effect
        float clusterPull = 20f * dt;
        float distanceFromOrigin = particle.Position.Length();
        if (distanceFromOrigin > 100f)
        {
            Vector2 pullDirection = -Vector2.Normalize(particle.Position);
            particle.Velocity.X += pullDirection.X * clusterPull;
            particle.Velocity.Y += pullDirection.Y * clusterPull;
        }

        // Limit maximum velocity to prevent particles from flying off too fast
        float maxSpeed = 300f;
        float currentSpeed = particle.Velocity.Length();
        if (currentSpeed > maxSpeed)
        {
            particle.Velocity = Vector2.Normalize(particle.Velocity) * maxSpeed;
        }
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Buzz direction phase
        gpu.StyleData2 = particle.StyleData2;  // Buzz frequency
        gpu.StyleData3 = particle.StyleData3;  // Movement seed
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Small particles like bees
        return base.GetParticleSize(ctx, isSecondary) * 0.6f;
    }

    protected override float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        // Shorter lifespan than Fish - particles dissipate faster
        return base.GetParticleLifespan(ctx, isSecondary) * 0.8f;
    }

    protected override Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        // Override to create green color based on greenHue parameter
        // Convert HSV to RGB for green color
        float hue = _greenHue;  // 0.35 = green in HSV (0.0-1.0 range)
        float saturation = 0.7f + ctx.GetRandomFloat() * 0.3f;  // High saturation for vivid green
        float value = 0.8f + ctx.GetRandomFloat() * 0.2f;  // Bright

        // HSV to RGB conversion
        float c = value * saturation;
        float x = c * (1f - MathF.Abs((hue * 6f) % 2f - 1f));
        float m = value - c;

        float r, g, b;
        float hue6 = hue * 6f;

        if (hue6 < 1f)
        {
            r = c; g = x; b = 0;
        }
        else if (hue6 < 2f)
        {
            r = x; g = c; b = 0;
        }
        else if (hue6 < 3f)
        {
            r = 0; g = c; b = x;
        }
        else if (hue6 < 4f)
        {
            r = 0; g = x; b = c;
        }
        else if (hue6 < 5f)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        // Add slight variation per particle
        r = MathF.Max(0f, MathF.Min(1f, (r + m) + (ctx.GetRandomFloat() - 0.5f) * 0.1f));
        g = MathF.Max(0f, MathF.Min(1f, (g + m) + (ctx.GetRandomFloat() - 0.5f) * 0.1f));
        b = MathF.Max(0f, MathF.Min(1f, (b + m) + (ctx.GetRandomFloat() - 0.5f) * 0.1f));

        return new Vector4(r, g, b, 1f);
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "buzzIntensity",
            DisplayName = "Buzz Intensity",
            Description = "How erratic the bee movement is",
            MinValue = 0.3f,
            MaxValue = 1.0f,
            DefaultValue = 0.6f,
            Step = 0.05f
        };
        yield return new FloatStyleParameter
        {
            Key = "swarmDensity",
            DisplayName = "Swarm Density",
            Description = "Particle count multiplier for denser swarms",
            MinValue = 1.0f,
            MaxValue = 3.0f,
            DefaultValue = 2.0f,
            Step = 0.1f
        };
        yield return new FloatStyleParameter
        {
            Key = "greenHue",
            DisplayName = "Green Hue",
            Description = "Hue of the green color (0.25 = yellow-green, 0.35 = pure green, 0.45 = cyan-green)",
            MinValue = 0.25f,
            MaxValue = 0.45f,
            DefaultValue = 0.35f,
            Step = 0.01f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "buzzIntensity":
                _buzzIntensity = Convert.ToSingle(value);
                break;
            case "swarmDensity":
                _swarmDensity = Convert.ToSingle(value);
                break;
            case "greenHue":
                _greenHue = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "buzzIntensity" => _buzzIntensity,
            "swarmDensity" => _swarmDensity,
            "greenHue" => _greenHue,
            _ => base.GetParameter(key)
        };
    }
}
