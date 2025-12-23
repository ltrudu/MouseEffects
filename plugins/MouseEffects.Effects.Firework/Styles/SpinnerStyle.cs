using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Spinner style - rotating mini-fireworks (Tourbillon).
/// Particles orbit while emitting spark trails.
/// </summary>
public class SpinnerStyle : FireworkStyleBase
{
    public override string Name => "Spinner";
    public override string Description => "Rotating mini-fireworks that spin and sparkle as they fall";
    public override int StyleId => 1;

    // Style-specific parameters
    private float _spinSpeed = 8f;
    private float _spinRadius = 30f;
    private bool _enableSparkTrails = true;

    public float SpinSpeed
    {
        get => _spinSpeed;
        set => _spinSpeed = value;
    }

    public float SpinRadius
    {
        get => _spinRadius;
        set => _spinRadius = value;
    }

    public bool EnableSparkTrails
    {
        get => _enableSparkTrails;
        set => _enableSparkTrails = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 3.0f,
        Gravity = 100f,  // Less gravity for better spinning effect
        Drag = 0.985f,   // Less drag to maintain spin
        MinParticlesPerFirework = 30,
        MaxParticlesPerFirework = 60,
        ExplosionForce = 200f,  // Lower force, they spin instead
        MinParticleSize = 4f,
        MaxParticleSize = 10f,
        SpreadAngle = 360f,
        EnableSecondaryExplosion = false,  // Spinners don't have secondary
        StyleSpecific = new Dictionary<string, object>
        {
            ["spinSpeed"] = 8f,
            ["spinRadius"] = 30f,
            ["enableSparkTrails"] = true
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // StyleData1 = angular velocity (randomized direction)
        float direction = ctx.GetRandomFloat() > 0.5f ? 1f : -1f;
        p.StyleData1 = _spinSpeed * direction * (0.8f + ctx.GetRandomFloat() * 0.4f);

        // StyleData2 = spin radius (slightly randomized)
        p.StyleData2 = _spinRadius * (0.7f + ctx.GetRandomFloat() * 0.6f);

        // StyleData3 = initial phase offset for variety
        p.StyleData3 = ctx.GetRandomFloat() * MathF.PI * 2f;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Particles spin around their trajectory
        float angularVelocity = particle.StyleData1;
        float radius = particle.StyleData2;
        float phase = particle.StyleData3 + angularVelocity * time;

        // Add circular motion offset to velocity
        // This creates the spinning effect without changing position directly
        float spinX = MathF.Cos(phase) * radius * dt * MathF.Abs(angularVelocity) * 0.1f;
        float spinY = MathF.Sin(phase) * radius * dt * MathF.Abs(angularVelocity) * 0.1f;

        particle.Velocity.X += spinX;
        particle.Velocity.Y += spinY;
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Angular velocity
        gpu.StyleData2 = particle.StyleData2;  // Spin radius
        gpu.StyleData3 = particle.StyleData3;  // Phase offset
        gpu.StyleFlags = (uint)StyleId | (_enableSparkTrails ? 0x100u : 0u);
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "spinSpeed",
            DisplayName = "Spin Speed",
            Description = "Rotation speed of the spinning particles (rad/s)",
            MinValue = 1f,
            MaxValue = 20f,
            DefaultValue = 8f,
            Step = 0.5f
        };
        yield return new FloatStyleParameter
        {
            Key = "spinRadius",
            DisplayName = "Spin Radius",
            Description = "Radius of the spinning motion (pixels)",
            MinValue = 10f,
            MaxValue = 100f,
            DefaultValue = 30f,
            Step = 5f
        };
        yield return new BoolStyleParameter
        {
            Key = "enableSparkTrails",
            DisplayName = "Spark Trails",
            Description = "Emit sparks while spinning",
            DefaultValue = true
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "spinSpeed":
                _spinSpeed = Convert.ToSingle(value);
                break;
            case "spinRadius":
                _spinRadius = Convert.ToSingle(value);
                break;
            case "enableSparkTrails":
                _enableSparkTrails = Convert.ToBoolean(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "spinSpeed" => _spinSpeed,
            "spinRadius" => _spinRadius,
            "enableSparkTrails" => _enableSparkTrails,
            _ => base.GetParameter(key)
        };
    }
}
