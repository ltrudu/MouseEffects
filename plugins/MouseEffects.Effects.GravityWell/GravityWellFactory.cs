using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.GravityWell.UI;

namespace MouseEffects.Effects.GravityWell;

public sealed class GravityWellFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "gravitywell",
        Name = "Gravity Well",
        Description = "Particles attracted to or repelled from the mouse cursor, simulating gravitational physics",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new GravityWellEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Particle settings (gw_ prefix for gravity well)
        config.Set("gw_particleCount", 100);
        config.Set("gw_particleSize", 8f);
        config.Set("gw_particleColor", new Vector4(0.2f, 0.8f, 1.0f, 1f)); // Cyan
        config.Set("gw_randomColors", false);

        // Physics settings
        config.Set("gw_gravityStrength", 50000f);
        config.Set("gw_gravityMode", (int)GravityMode.Attract);
        config.Set("gw_orbitSpeed", 200f);
        config.Set("gw_damping", 0.98f);

        // Trail settings
        config.Set("gw_trailEnabled", true);
        config.Set("gw_trailLength", 0.3f);

        // Visual settings
        config.Set("gw_hdrMultiplier", 1.0f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Particle Settings
                new IntParameter
                {
                    Key = "gw_particleCount",
                    DisplayName = "Particle Count",
                    Description = "Number of particles in the gravity field",
                    MinValue = 50,
                    MaxValue = 500,
                    DefaultValue = 100
                },
                new FloatParameter
                {
                    Key = "gw_particleSize",
                    DisplayName = "Particle Size",
                    Description = "Size of particles in pixels",
                    MinValue = 3f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 0.5f
                },
                new ColorParameter
                {
                    Key = "gw_particleColor",
                    DisplayName = "Particle Color",
                    Description = "Color of the particles",
                    DefaultValue = new Vector4(0.2f, 0.8f, 1.0f, 1f),
                    SupportsAlpha = false
                },
                new BoolParameter
                {
                    Key = "gw_randomColors",
                    DisplayName = "Random Colors",
                    Description = "Use random rainbow colors for particles",
                    DefaultValue = false
                },

                // Physics Settings
                new FloatParameter
                {
                    Key = "gw_gravityStrength",
                    DisplayName = "Gravity Strength",
                    Description = "Strength of gravitational attraction/repulsion",
                    MinValue = 10000f,
                    MaxValue = 200000f,
                    DefaultValue = 50000f,
                    Step = 5000f
                },
                new IntParameter
                {
                    Key = "gw_gravityMode",
                    DisplayName = "Gravity Mode",
                    Description = "0=Attract, 1=Repel, 2=Orbit",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "gw_orbitSpeed",
                    DisplayName = "Orbit Speed",
                    Description = "Tangential velocity for orbit mode",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "gw_damping",
                    DisplayName = "Damping",
                    Description = "Energy loss per frame (0.9=high loss, 1.0=no loss)",
                    MinValue = 0.9f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.98f,
                    Step = 0.01f
                },

                // Trail Settings
                new BoolParameter
                {
                    Key = "gw_trailEnabled",
                    DisplayName = "Trail Enabled",
                    Description = "Show particle motion trails",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "gw_trailLength",
                    DisplayName = "Trail Length",
                    Description = "Length/opacity of particle trails",
                    MinValue = 0.1f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },

                // Visual Settings
                new FloatParameter
                {
                    Key = "gw_hdrMultiplier",
                    DisplayName = "HDR Multiplier",
                    Description = "Brightness multiplier for HDR displays",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new GravityWellSettingsControl(effect);
}
