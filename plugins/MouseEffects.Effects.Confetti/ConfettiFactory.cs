using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Confetti.UI;

namespace MouseEffects.Effects.Confetti;

public sealed class ConfettiFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "confetti",
        Name = "Confetti",
        Description = "Colorful confetti particles bursting from clicks or following the cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new ConfettiEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        config.Set("maxParticles", 5000);
        config.Set("burstCount", 50);
        config.Set("particleLifespan", 3.0f);
        config.Set("minParticleSize", 8f);
        config.Set("maxParticleSize", 16f);
        config.Set("gravity", 200f);
        config.Set("airResistance", 0.985f);
        config.Set("flutterAmount", 2.0f);
        config.Set("burstForce", 400f);
        config.Set("trailSpacing", 20f);
        config.Set("burstOnClick", true);
        config.Set("trailOnMove", false);
        config.Set("rainbowMode", true);
        config.Set("rainbowSpeed", 0.5f);
        config.Set("useRectangles", true);
        config.Set("useCircles", true);
        config.Set("useRibbons", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new IntParameter
                {
                    Key = "maxParticles",
                    DisplayName = "Max Particles",
                    Description = "Maximum number of particles in the system",
                    MinValue = 100,
                    MaxValue = 10000,
                    DefaultValue = 5000
                },
                new IntParameter
                {
                    Key = "burstCount",
                    DisplayName = "Burst Count",
                    Description = "Number of confetti pieces per burst",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 50
                },
                new FloatParameter
                {
                    Key = "particleLifespan",
                    DisplayName = "Particle Lifespan",
                    Description = "How long confetti pieces live (seconds)",
                    MinValue = 1f,
                    MaxValue = 10f,
                    DefaultValue = 3.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "minParticleSize",
                    DisplayName = "Min Particle Size",
                    Description = "Minimum confetti piece size in pixels",
                    MinValue = 2f,
                    MaxValue = 30f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "maxParticleSize",
                    DisplayName = "Max Particle Size",
                    Description = "Maximum confetti piece size in pixels",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 16f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "gravity",
                    DisplayName = "Gravity",
                    Description = "Downward acceleration (pixels/sec^2)",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "airResistance",
                    DisplayName = "Air Resistance",
                    Description = "Velocity damping per frame (lower = more drag)",
                    MinValue = 0.9f,
                    MaxValue = 1f,
                    DefaultValue = 0.985f,
                    Step = 0.005f
                },
                new FloatParameter
                {
                    Key = "flutterAmount",
                    DisplayName = "Flutter Amount",
                    Description = "Amount of tumbling/spinning motion",
                    MinValue = 0f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "burstForce",
                    DisplayName = "Burst Force",
                    Description = "Initial velocity of burst particles (pixels/sec)",
                    MinValue = 100f,
                    MaxValue = 800f,
                    DefaultValue = 400f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "trailSpacing",
                    DisplayName = "Trail Spacing",
                    Description = "Distance between trail spawns (pixels)",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },
                new BoolParameter
                {
                    Key = "burstOnClick",
                    DisplayName = "Burst on Click",
                    Description = "Create confetti burst when clicking",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "trailOnMove",
                    DisplayName = "Trail on Move",
                    Description = "Continuous confetti trail following cursor",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "rainbowMode",
                    DisplayName = "Rainbow Mode",
                    Description = "Cycle through rainbow colors over time",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "useRectangles",
                    DisplayName = "Use Rectangles",
                    Description = "Include rectangle-shaped confetti",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "useCircles",
                    DisplayName = "Use Circles",
                    Description = "Include circle-shaped confetti",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "useRibbons",
                    DisplayName = "Use Ribbons",
                    Description = "Include ribbon/streamer-shaped confetti",
                    DefaultValue = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new ConfettiSettingsControl(effect);
}
