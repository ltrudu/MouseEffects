using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.FlowerBloom.UI;

namespace MouseEffects.Effects.FlowerBloom;

public sealed class FlowerBloomFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "flowerbloom",
        Name = "Flower Bloom",
        Description = "Beautiful flowers that bloom and grow from the mouse cursor with organic petal unfurling",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new FlowerBloomEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Flower Settings (fb_ prefix)
        config.Set("fb_flowerType", 0); // 0=rose, 1=daisy, 2=lotus, 3=cherry
        config.Set("fb_colorPalette", 0); // 0=spring, 1=summer, 2=tropical, 3=pastel
        config.Set("fb_petalCount", 6);
        config.Set("fb_flowerSize", 80f);
        config.Set("fb_bloomDuration", 1.5f);
        config.Set("fb_flowerLifetime", 5.0f);
        config.Set("fb_fadeOutDuration", 1.0f);
        config.Set("fb_showStem", true);
        config.Set("fb_sizeVariation", true);
        config.Set("fb_sizeVariationAmount", 0.3f);

        // Spawn Settings
        config.Set("fb_continuousSpawn", false);
        config.Set("fb_spawnRate", 0.5f);

        // Trigger Settings
        config.Set("fb_leftClickEnabled", true);
        config.Set("fb_rightClickEnabled", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Flower Type
                new IntParameter
                {
                    Key = "fb_flowerType",
                    DisplayName = "Flower Type",
                    Description = "Type of flower (0=Rose, 1=Daisy, 2=Lotus, 3=Cherry Blossom)",
                    MinValue = 0,
                    MaxValue = 3,
                    DefaultValue = 0
                },

                // Color Palette
                new IntParameter
                {
                    Key = "fb_colorPalette",
                    DisplayName = "Color Palette",
                    Description = "Color scheme (0=Spring, 1=Summer, 2=Tropical, 3=Pastel)",
                    MinValue = 0,
                    MaxValue = 3,
                    DefaultValue = 0
                },

                // Petal Count
                new IntParameter
                {
                    Key = "fb_petalCount",
                    DisplayName = "Petal Count",
                    Description = "Number of petals per flower",
                    MinValue = 3,
                    MaxValue = 12,
                    DefaultValue = 6
                },

                // Flower Size
                new FloatParameter
                {
                    Key = "fb_flowerSize",
                    DisplayName = "Flower Size",
                    Description = "Size of flowers in pixels",
                    MinValue = 30f,
                    MaxValue = 200f,
                    DefaultValue = 80f,
                    Step = 5f
                },

                // Bloom Duration
                new FloatParameter
                {
                    Key = "fb_bloomDuration",
                    DisplayName = "Bloom Duration",
                    Description = "Time for flower to fully bloom (seconds)",
                    MinValue = 0.3f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Flower Lifetime
                new FloatParameter
                {
                    Key = "fb_flowerLifetime",
                    DisplayName = "Flower Lifetime",
                    Description = "How long flowers exist before fading (seconds)",
                    MinValue = 2f,
                    MaxValue = 15f,
                    DefaultValue = 5.0f,
                    Step = 0.5f
                },

                // Fade Out Duration
                new FloatParameter
                {
                    Key = "fb_fadeOutDuration",
                    DisplayName = "Fade Out Duration",
                    Description = "Time for flower to fade away (seconds)",
                    MinValue = 0.3f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Show Stem
                new BoolParameter
                {
                    Key = "fb_showStem",
                    DisplayName = "Show Stem",
                    Description = "Display stem and leaves with flowers",
                    DefaultValue = true
                },

                // Size Variation
                new BoolParameter
                {
                    Key = "fb_sizeVariation",
                    DisplayName = "Size Variation",
                    Description = "Randomize flower sizes",
                    DefaultValue = true
                },

                // Size Variation Amount
                new FloatParameter
                {
                    Key = "fb_sizeVariationAmount",
                    DisplayName = "Size Variation Amount",
                    Description = "How much to vary flower sizes (0-1)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },

                // Continuous Spawn
                new BoolParameter
                {
                    Key = "fb_continuousSpawn",
                    DisplayName = "Continuous Spawn",
                    Description = "Continuously spawn flowers at mouse position",
                    DefaultValue = false
                },

                // Spawn Rate
                new FloatParameter
                {
                    Key = "fb_spawnRate",
                    DisplayName = "Spawn Rate",
                    Description = "Delay between continuous spawns (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Left Click Trigger
                new BoolParameter
                {
                    Key = "fb_leftClickEnabled",
                    DisplayName = "Left Click Trigger",
                    Description = "Spawn flower on left click",
                    DefaultValue = true
                },

                // Right Click Trigger
                new BoolParameter
                {
                    Key = "fb_rightClickEnabled",
                    DisplayName = "Right Click Trigger",
                    Description = "Spawn flower on right click",
                    DefaultValue = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new FlowerBloomSettingsControl(effect);
}
