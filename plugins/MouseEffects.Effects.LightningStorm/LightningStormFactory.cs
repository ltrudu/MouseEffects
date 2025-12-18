using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.LightningStorm.UI;

namespace MouseEffects.Effects.LightningStorm;

public sealed class LightningStormFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "lightningstorm",
        Name = "Lightning Storm",
        Description = "Creates dramatic lightning bolts that arc from or around the mouse cursor with electric effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new LightningStormEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Trigger settings (ls_ prefix)
        config.Set("ls_onClickTrigger", true);
        config.Set("ls_onMoveTrigger", false);
        config.Set("ls_moveDistance", 50f);
        config.Set("ls_randomTiming", false);
        config.Set("ls_minStrikeInterval", 0.5f);
        config.Set("ls_maxStrikeInterval", 2.0f);

        // Bolt settings
        config.Set("ls_minBoltCount", 1);
        config.Set("ls_maxBoltCount", 3);
        config.Set("ls_boltThickness", 2.0f);
        config.Set("ls_branchCount", 3);
        config.Set("ls_branchProbability", 0.7f);

        // Direction and targeting
        config.Set("ls_strikeFromCursor", true);
        config.Set("ls_chainLightning", false);
        config.Set("ls_minStrikeDistance", 100f);
        config.Set("ls_maxStrikeDistance", 300f);

        // Visual settings
        config.Set("ls_boltLifetime", 0.2f);
        config.Set("ls_flickerSpeed", 25f);
        config.Set("ls_flashIntensity", 0.5f);
        config.Set("ls_glowIntensity", 1.0f);
        config.Set("ls_persistenceEffect", false);
        config.Set("ls_persistenceFade", 0.3f);

        // Colors
        config.Set("ls_colorMode", 0); // 0=White/Blue, 1=Purple, 2=Green, 3=Custom
        config.Set("ls_customColor", new Vector4(0.4f, 0.6f, 1f, 1f));

        // Sparks
        config.Set("ls_enableSparks", true);
        config.Set("ls_sparkCount", 8);
        config.Set("ls_sparkLifetime", 0.5f);
        config.Set("ls_sparkSpeed", 200f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Trigger Settings
                new BoolParameter
                {
                    Key = "ls_onClickTrigger",
                    DisplayName = "Trigger on Click",
                    Description = "Lightning strikes when clicking the mouse",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "ls_onMoveTrigger",
                    DisplayName = "Trigger on Move",
                    Description = "Lightning strikes when moving the mouse",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ls_moveDistance",
                    DisplayName = "Move Distance",
                    Description = "Distance in pixels before triggering lightning",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new BoolParameter
                {
                    Key = "ls_randomTiming",
                    DisplayName = "Random Timing",
                    Description = "Lightning strikes at random intervals",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ls_minStrikeInterval",
                    DisplayName = "Min Strike Interval",
                    Description = "Minimum time between random strikes (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ls_maxStrikeInterval",
                    DisplayName = "Max Strike Interval",
                    Description = "Maximum time between random strikes (seconds)",
                    MinValue = 1f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },

                // Bolt Settings
                new IntParameter
                {
                    Key = "ls_minBoltCount",
                    DisplayName = "Min Bolt Count",
                    Description = "Minimum number of lightning bolts per strike",
                    MinValue = 1,
                    MaxValue = 5,
                    DefaultValue = 1
                },
                new IntParameter
                {
                    Key = "ls_maxBoltCount",
                    DisplayName = "Max Bolt Count",
                    Description = "Maximum number of lightning bolts per strike",
                    MinValue = 1,
                    MaxValue = 10,
                    DefaultValue = 3
                },
                new FloatParameter
                {
                    Key = "ls_boltThickness",
                    DisplayName = "Bolt Thickness",
                    Description = "Thickness of lightning bolts",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.0f,
                    Step = 0.5f
                },
                new IntParameter
                {
                    Key = "ls_branchCount",
                    DisplayName = "Branch Count",
                    Description = "Number of branches per bolt",
                    MinValue = 0,
                    MaxValue = 8,
                    DefaultValue = 3
                },
                new FloatParameter
                {
                    Key = "ls_branchProbability",
                    DisplayName = "Branch Probability",
                    Description = "Probability of branches appearing (0-1)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.1f
                },

                // Direction and Targeting
                new BoolParameter
                {
                    Key = "ls_strikeFromCursor",
                    DisplayName = "Strike From Cursor",
                    Description = "Bolts strike from cursor outward (false = strike toward cursor)",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "ls_chainLightning",
                    DisplayName = "Chain Lightning",
                    Description = "Multiple bolts connect in a chain",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ls_minStrikeDistance",
                    DisplayName = "Min Strike Distance",
                    Description = "Minimum distance of lightning strikes (pixels)",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "ls_maxStrikeDistance",
                    DisplayName = "Max Strike Distance",
                    Description = "Maximum distance of lightning strikes (pixels)",
                    MinValue = 100f,
                    MaxValue = 800f,
                    DefaultValue = 300f,
                    Step = 10f
                },

                // Visual Settings
                new FloatParameter
                {
                    Key = "ls_boltLifetime",
                    DisplayName = "Bolt Lifetime",
                    Description = "How long each bolt exists (seconds)",
                    MinValue = 0.05f,
                    MaxValue = 1f,
                    DefaultValue = 0.2f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "ls_flickerSpeed",
                    DisplayName = "Flicker Speed",
                    Description = "Speed of the flickering effect",
                    MinValue = 1f,
                    MaxValue = 50f,
                    DefaultValue = 25f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ls_flashIntensity",
                    DisplayName = "Flash Intensity",
                    Description = "Intensity of screen flash on strike",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ls_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the glow effect around bolts",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ls_persistenceEffect",
                    DisplayName = "Persistence Effect",
                    Description = "Bolts leave afterimage trails",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ls_persistenceFade",
                    DisplayName = "Persistence Fade",
                    Description = "Duration of afterimage fade (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                // Colors
                new ChoiceParameter
                {
                    Key = "ls_colorMode",
                    DisplayName = "Color Mode",
                    Description = "Color scheme for lightning bolts",
                    Choices = ["White/Blue", "Purple", "Green", "Custom"],
                    DefaultValue = "White/Blue"
                },
                new ColorParameter
                {
                    Key = "ls_customColor",
                    DisplayName = "Custom Color",
                    Description = "Custom color for lightning bolts",
                    DefaultValue = new Vector4(0.4f, 0.6f, 1f, 1f),
                    SupportsAlpha = false
                },

                // Sparks
                new BoolParameter
                {
                    Key = "ls_enableSparks",
                    DisplayName = "Enable Sparks",
                    Description = "Show particle sparks at impact points",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "ls_sparkCount",
                    DisplayName = "Spark Count",
                    Description = "Number of sparks per impact",
                    MinValue = 2,
                    MaxValue = 20,
                    DefaultValue = 8
                },
                new FloatParameter
                {
                    Key = "ls_sparkLifetime",
                    DisplayName = "Spark Lifetime",
                    Description = "How long sparks exist (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ls_sparkSpeed",
                    DisplayName = "Spark Speed",
                    Description = "Initial speed of spark particles",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 10f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new LightningStormSettingsControl(effect);
}
