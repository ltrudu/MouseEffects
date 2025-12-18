using MouseEffects.Core.Effects;
using MouseEffects.Effects.InkBlot.UI;

namespace MouseEffects.Effects.InkBlot;

public sealed class InkBlotFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "inkblot",
        Name = "Ink Blot",
        Description = "Animated metaball ink drops that drip and merge organically",
        Author = "MouseEffects",
        Version = new Version(1, 1, 0),
        Category = EffectCategory.Artistic
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new InkBlotEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Physics settings
        config.Set("dropRadius", 25f);
        config.Set("gravity", 0f);
        config.Set("surfaceTension", 0.5f);
        config.Set("viscosity", 0.98f);
        config.Set("spawnSpread", 30f);

        // Appearance settings
        config.Set("metaballThreshold", 1.0f);
        config.Set("edgeSoftness", 0.3f);
        config.Set("opacity", 0.85f);
        config.Set("lifetime", 2.8f);
        config.Set("glowIntensity", 0.2f);
        config.Set("animateGlow", true);
        config.Set("glowMin", 0.1f);
        config.Set("glowMax", 1.38f);
        config.Set("glowAnimSpeed", 4.7f);
        config.Set("innerDarkening", 0.3f);
        config.Set("colorMode", 4); // Rainbow
        config.Set("rainbowSpeed", 1.0f);

        // Spawn settings
        config.Set("spawnOnClick", true);
        config.Set("spawnOnMove", true);
        config.Set("moveDistance", 40f);
        config.Set("dropsPerSpawn", 3);
        config.Set("maxDropsPerSecond", 60);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Physics
                new FloatParameter
                {
                    Key = "dropRadius",
                    DisplayName = "Drop Size",
                    Description = "Base radius of ink drops in pixels",
                    MinValue = 10f,
                    MaxValue = 60f,
                    DefaultValue = 25f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "gravity",
                    DisplayName = "Gravity",
                    Description = "Downward acceleration force",
                    MinValue = 0f,
                    MaxValue = 400f,
                    DefaultValue = 0f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "surfaceTension",
                    DisplayName = "Surface Tension",
                    Description = "Attraction between nearby drops (causes merging)",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "viscosity",
                    DisplayName = "Viscosity",
                    Description = "Air resistance/drag (higher = slower movement)",
                    MinValue = 0.9f,
                    MaxValue = 1f,
                    DefaultValue = 0.98f,
                    Step = 0.01f
                },
                new FloatParameter
                {
                    Key = "spawnSpread",
                    DisplayName = "Spawn Spread",
                    Description = "Random offset when spawning drops",
                    MinValue = 0f,
                    MaxValue = 80f,
                    DefaultValue = 30f,
                    Step = 5f
                },

                // Appearance
                new ChoiceParameter
                {
                    Key = "colorMode",
                    DisplayName = "Ink Color",
                    Description = "Color of the ink",
                    Choices = ["Black Ink", "Blue Ink", "Red Ink", "Sepia", "Rainbow"],
                    DefaultValue = "Rainbow"
                },
                new FloatParameter
                {
                    Key = "rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "metaballThreshold",
                    DisplayName = "Metaball Threshold",
                    Description = "Controls blob shape (lower = larger, softer blobs)",
                    MinValue = 0.5f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "edgeSoftness",
                    DisplayName = "Edge Softness",
                    Description = "How soft/fuzzy the ink edges appear",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "opacity",
                    DisplayName = "Opacity",
                    Description = "Base opacity of ink",
                    MinValue = 0.3f,
                    MaxValue = 1f,
                    DefaultValue = 0.85f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Subtle glow around ink edges",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.2f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "animateGlow",
                    DisplayName = "Animate Glow",
                    Description = "Pulse the glow effect",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "glowMin",
                    DisplayName = "Glow Min",
                    Description = "Minimum glow intensity when animated",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.1f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "glowMax",
                    DisplayName = "Glow Max",
                    Description = "Maximum glow intensity when animated",
                    MinValue = 0f,
                    MaxValue = 1.5f,
                    DefaultValue = 1.38f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "glowAnimSpeed",
                    DisplayName = "Glow Speed",
                    Description = "Speed of glow animation",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 4.7f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "innerDarkening",
                    DisplayName = "Inner Darkening",
                    Description = "Darker center for depth effect",
                    MinValue = 0f,
                    MaxValue = 0.8f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long drops remain visible (seconds)",
                    MinValue = 1f,
                    MaxValue = 10f,
                    DefaultValue = 2.8f,
                    Step = 0.5f
                },

                // Spawn settings
                new BoolParameter
                {
                    Key = "spawnOnClick",
                    DisplayName = "Spawn on Click",
                    Description = "Create drops when clicking",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "spawnOnMove",
                    DisplayName = "Spawn on Move",
                    Description = "Create drops when moving the mouse",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "moveDistance",
                    DisplayName = "Move Distance",
                    Description = "Distance mouse must move to spawn drops (pixels)",
                    MinValue = 20f,
                    MaxValue = 150f,
                    DefaultValue = 40f,
                    Step = 10f
                },
                new IntParameter
                {
                    Key = "dropsPerSpawn",
                    DisplayName = "Drops per Spawn",
                    Description = "Number of drops created each spawn event",
                    MinValue = 1,
                    MaxValue = 8,
                    DefaultValue = 3
                },
                new IntParameter
                {
                    Key = "maxDropsPerSecond",
                    DisplayName = "Max Drops/Second",
                    Description = "Rate limiting for drop spawning",
                    MinValue = 5,
                    MaxValue = 120,
                    DefaultValue = 60
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new InkBlotSettingsControl(effect);
}
