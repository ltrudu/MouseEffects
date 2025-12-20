using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.ParticleTrail.UI;

namespace MouseEffects.Effects.ParticleTrail;

/// <summary>
/// Factory for creating ParticleTrailEffect instances.
/// </summary>
public sealed class ParticleTrailFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "particle-trail",
        Name = "Particle Trail",
        Description = "Creates colorful particle trails that follow the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new ParticleTrailEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        config.Set("emissionRate", 500f);
        config.Set("particleLifetime", 1.5f);
        config.Set("particleSize", 8f);
        config.Set("spreadAngle", 0.5f);
        config.Set("initialSpeed", 50f);
        config.Set("startColor", new Vector4(1f, 0.6f, 0.2f, 1f));
        config.Set("endColor", new Vector4(1f, 0.2f, 0.8f, 0.5f));
        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new FloatParameter
                {
                    Key = "emissionRate",
                    DisplayName = "Emission Rate",
                    Description = "Particles spawned per second while moving",
                    MinValue = 10f,
                    MaxValue = 500f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "particleLifetime",
                    DisplayName = "Lifetime",
                    Description = "How long particles live (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "particleSize",
                    DisplayName = "Size",
                    Description = "Base particle size in pixels",
                    MinValue = 2f,
                    MaxValue = 32f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "spreadAngle",
                    DisplayName = "Spread",
                    Description = "Angular spread of particles",
                    MinValue = 0f,
                    MaxValue = 3.14f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "initialSpeed",
                    DisplayName = "Speed",
                    Description = "Initial particle speed",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new ColorParameter
                {
                    Key = "startColor",
                    DisplayName = "Start Color",
                    Description = "Color at beginning of trail",
                    DefaultValue = new Vector4(1f, 0.6f, 0.2f, 1f),
                    SupportsAlpha = true
                },
                new ColorParameter
                {
                    Key = "endColor",
                    DisplayName = "End Color",
                    Description = "Color at end of trail",
                    DefaultValue = new Vector4(1f, 0.2f, 0.8f, 0.5f),
                    SupportsAlpha = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new ParticleTrailSettingsControl(effect);
    }
}
