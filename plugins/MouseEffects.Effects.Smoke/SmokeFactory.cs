using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Smoke.UI;

namespace MouseEffects.Effects.Smoke;

public sealed class SmokeFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "smoke",
        Name = "Smoke",
        Description = "Soft, wispy smoke trails following the mouse cursor with rising motion and turbulence",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new SmokeEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Smoke particle settings (sm_ prefix for Smoke)
        config.Set("sm_particleCount", 100);
        config.Set("sm_particleSize", 20f);
        config.Set("sm_particleLifetime", 3.0f);
        config.Set("sm_spawnRate", 0.05f);

        // Motion settings
        config.Set("sm_riseSpeed", 50f);
        config.Set("sm_expansionRate", 15f);
        config.Set("sm_turbulenceStrength", 30f);

        // Visual settings
        config.Set("sm_opacity", 0.6f);
        config.Set("sm_softness", 0.8f);

        // Color settings
        config.Set("sm_colorMode", 0); // 0=gray smoke, 1=white smoke, 2=black smoke, 3=colored smoke
        config.Set("sm_smokeColor", new Vector4(0.8f, 0.8f, 0.8f, 1f)); // Light gray

        // Trigger settings
        config.Set("sm_mouseMoveEnabled", true);
        config.Set("sm_moveDistanceThreshold", 10f);
        config.Set("sm_leftClickEnabled", true);
        config.Set("sm_leftClickBurstCount", 50);
        config.Set("sm_rightClickEnabled", true);
        config.Set("sm_rightClickBurstCount", 80);

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
                    Key = "sm_particleCount",
                    DisplayName = "Particle Count",
                    Description = "Number of smoke puffs per spawn",
                    MinValue = 20,
                    MaxValue = 200,
                    DefaultValue = 100
                },
                new FloatParameter
                {
                    Key = "sm_particleSize",
                    DisplayName = "Particle Size",
                    Description = "Initial size of smoke puffs",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 20f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "sm_particleLifetime",
                    DisplayName = "Lifetime",
                    Description = "How long smoke puffs last (seconds)",
                    MinValue = 1f,
                    MaxValue = 8f,
                    DefaultValue = 3.0f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "sm_spawnRate",
                    DisplayName = "Spawn Rate",
                    Description = "Time between spawns (seconds)",
                    MinValue = 0.01f,
                    MaxValue = 0.2f,
                    DefaultValue = 0.05f,
                    Step = 0.01f
                },

                // Motion Settings
                new FloatParameter
                {
                    Key = "sm_riseSpeed",
                    DisplayName = "Rise Speed",
                    Description = "How fast smoke rises upward",
                    MinValue = 10f,
                    MaxValue = 150f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "sm_expansionRate",
                    DisplayName = "Expansion Rate",
                    Description = "How fast smoke puffs expand",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "sm_turbulenceStrength",
                    DisplayName = "Turbulence",
                    Description = "Random drift and swirl strength",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },

                // Visual Settings
                new FloatParameter
                {
                    Key = "sm_opacity",
                    DisplayName = "Opacity",
                    Description = "Overall smoke opacity",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.6f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sm_softness",
                    DisplayName = "Softness",
                    Description = "Edge softness of smoke puffs",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },

                // Color Settings
                new ChoiceParameter
                {
                    Key = "sm_colorMode",
                    DisplayName = "Smoke Type",
                    Description = "Smoke color preset",
                    Choices = ["Gray Smoke", "White Smoke", "Black Smoke", "Colored Smoke"],
                    DefaultValue = "Gray Smoke"
                },

                // Trigger Settings
                new BoolParameter
                {
                    Key = "sm_mouseMoveEnabled",
                    DisplayName = "Mouse Move Trigger",
                    Description = "Emit smoke while moving mouse",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sm_moveDistanceThreshold",
                    DisplayName = "Move Distance",
                    Description = "Distance to move before spawning smoke",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 10f,
                    Step = 5f
                },
                new BoolParameter
                {
                    Key = "sm_leftClickEnabled",
                    DisplayName = "Left Click Burst",
                    Description = "Emit smoke burst on left click",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "sm_leftClickBurstCount",
                    DisplayName = "Left Click Count",
                    Description = "Particles per left click",
                    MinValue = 10,
                    MaxValue = 150,
                    DefaultValue = 50
                },
                new BoolParameter
                {
                    Key = "sm_rightClickEnabled",
                    DisplayName = "Right Click Burst",
                    Description = "Emit smoke burst on right click",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "sm_rightClickBurstCount",
                    DisplayName = "Right Click Count",
                    Description = "Particles per right click",
                    MinValue = 20,
                    MaxValue = 200,
                    DefaultValue = 80
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new SmokeSettingsControl(effect);
}
