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
        Category = EffectCategory.Cosmic
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new GravityWellEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Reset settings
        config.Set("gw_resetOnLeftClick", false);
        config.Set("gw_resetOnRightClick", false);

        // Particle settings (gw_ prefix for gravity well)
        config.Set("gw_particleCount", 346);
        config.Set("gw_particleSize", 8f);
        config.Set("gw_particleColor", new Vector4(0.2f, 0.8f, 1.0f, 1f)); // Cyan
        config.Set("gw_randomColors", true);

        // Physics settings
        config.Set("gw_gravityStrength", 87143f);
        config.Set("gw_gravityRadius", 1500f);
        config.Set("gw_gravityMode", (int)GravityMode.Orbit);
        config.Set("gw_orbitSpeed", 81f);
        config.Set("gw_damping", 1.0f);
        config.Set("gw_edgeBehavior", (int)EdgeBehavior.Reset);

        // Trail settings
        config.Set("gw_trailEnabled", true);
        config.Set("gw_trailLength", 50f);

        // Trigger settings (when gravity is active)
        config.Set("gw_triggerAlwaysActive", false);
        config.Set("gw_triggerOnLeftMouseDown", true);
        config.Set("gw_triggerOnRightMouseDown", false);
        config.Set("gw_triggerOnMouseMove", true);
        config.Set("gw_mouseMoveTimeMultiplier", 3.0f);

        // Drift settings (when gravity is inactive)
        config.Set("gw_driftEnabled", true);
        config.Set("gw_driftAmount", 0.51f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Reset Settings
                new BoolParameter
                {
                    Key = "gw_resetOnLeftClick",
                    DisplayName = "Reset on Left Click",
                    Description = "Reset particles when left mouse button is clicked",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "gw_resetOnRightClick",
                    DisplayName = "Reset on Right Click",
                    Description = "Reset particles when right mouse button is clicked",
                    DefaultValue = false
                },

                // Particle Settings
                new IntParameter
                {
                    Key = "gw_particleCount",
                    DisplayName = "Particle Count",
                    Description = "Number of particles in the gravity field",
                    MinValue = 50,
                    MaxValue = 500,
                    DefaultValue = 346
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
                    DefaultValue = true
                },

                // Physics Settings
                new FloatParameter
                {
                    Key = "gw_gravityStrength",
                    DisplayName = "Gravity Strength",
                    Description = "Strength of gravitational attraction/repulsion",
                    MinValue = 10000f,
                    MaxValue = 200000f,
                    DefaultValue = 87143f,
                    Step = 5000f
                },
                new FloatParameter
                {
                    Key = "gw_gravityRadius",
                    DisplayName = "Gravity Radius",
                    Description = "Radius of the gravity field effect in pixels",
                    MinValue = 100f,
                    MaxValue = 1500f,
                    DefaultValue = 1500f,
                    Step = 50f
                },
                new IntParameter
                {
                    Key = "gw_gravityMode",
                    DisplayName = "Gravity Mode",
                    Description = "0=Attract, 1=Repel, 2=Orbit",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 2
                },
                new FloatParameter
                {
                    Key = "gw_orbitSpeed",
                    DisplayName = "Orbit Speed",
                    Description = "Tangential velocity for orbit mode",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 81f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "gw_damping",
                    DisplayName = "Damping",
                    Description = "Energy loss per frame (0.9=high loss, 1.0=no loss)",
                    MinValue = 0.9f,
                    MaxValue = 1.0f,
                    DefaultValue = 1.0f,
                    Step = 0.01f
                },
                new IntParameter
                {
                    Key = "gw_edgeBehavior",
                    DisplayName = "Edge Behavior",
                    Description = "0=Teleport, 1=Bounce, 2=Reset",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 2
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
                    MaxValue = 50.0f,
                    DefaultValue = 50.0f,
                    Step = 0.1f
                },

                // Trigger Settings
                new BoolParameter
                {
                    Key = "gw_triggerAlwaysActive",
                    DisplayName = "Always Active",
                    Description = "Gravity is always active (overrides other triggers)",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "gw_triggerOnLeftMouseDown",
                    DisplayName = "On Left Mouse Down",
                    Description = "Gravity is active while left mouse button is held",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "gw_triggerOnRightMouseDown",
                    DisplayName = "On Right Mouse Down",
                    Description = "Gravity is active while right mouse button is held",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "gw_triggerOnMouseMove",
                    DisplayName = "On Mouse Move",
                    Description = "Gravity is active while the mouse is moving",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "gw_mouseMoveTimeMultiplier",
                    DisplayName = "Mouse Move Speed",
                    Description = "Animation speed multiplier when mouse is moving (1x to 10x)",
                    MinValue = 1.0f,
                    MaxValue = 10.0f,
                    DefaultValue = 3.0f,
                    Step = 0.5f
                },

                // Drift Settings
                new BoolParameter
                {
                    Key = "gw_driftEnabled",
                    DisplayName = "Drift Enabled",
                    Description = "Apply deceleration to particles when gravity is inactive",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "gw_driftAmount",
                    DisplayName = "Drift Amount",
                    Description = "Deceleration factor when drifting (0.5=fast stop, 1.0=no deceleration)",
                    MinValue = 0.5f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.51f,
                    Step = 0.01f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new GravityWellSettingsControl(effect);
}
