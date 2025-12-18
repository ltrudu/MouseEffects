using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.FallingLeaves.UI;

namespace MouseEffects.Effects.FallingLeaves;

public sealed class FallingLeavesFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "fallingleaves",
        Name = "Falling Leaves",
        Description = "Autumn leaves drifting down from the mouse cursor with natural tumbling motion",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new FallingLeavesEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Leaf Settings (fl_ prefix)
        config.Set("fl_leafCount", 30);
        config.Set("fl_fallSpeed", 50f);
        config.Set("fl_windStrength", 25f);
        config.Set("fl_windFrequency", 0.3f);
        config.Set("fl_minSize", 12f);
        config.Set("fl_maxSize", 28f);
        config.Set("fl_tumbleSpeed", 2.0f);
        config.Set("fl_swayAmount", 40f);
        config.Set("fl_spawnRadius", 120f);
        config.Set("fl_lifetime", 10f);
        config.Set("fl_colorVariety", 0.8f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Leaf Count
                new IntParameter
                {
                    Key = "fl_leafCount",
                    DisplayName = "Leaf Count",
                    Description = "Number of leaves spawned per second",
                    MinValue = 5,
                    MaxValue = 100,
                    DefaultValue = 30
                },

                // Fall Speed
                new FloatParameter
                {
                    Key = "fl_fallSpeed",
                    DisplayName = "Fall Speed",
                    Description = "Downward falling speed of leaves",
                    MinValue = 10f,
                    MaxValue = 150f,
                    DefaultValue = 50f,
                    Step = 5f
                },

                // Wind Strength
                new FloatParameter
                {
                    Key = "fl_windStrength",
                    DisplayName = "Wind Strength",
                    Description = "Strength of horizontal wind drift",
                    MinValue = 0f,
                    MaxValue = 80f,
                    DefaultValue = 25f,
                    Step = 5f
                },

                // Wind Frequency
                new FloatParameter
                {
                    Key = "fl_windFrequency",
                    DisplayName = "Wind Frequency",
                    Description = "How quickly the wind oscillates",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "fl_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum leaf size in pixels",
                    MinValue = 5f,
                    MaxValue = 30f,
                    DefaultValue = 12f,
                    Step = 1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "fl_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum leaf size in pixels",
                    MinValue = 10f,
                    MaxValue = 60f,
                    DefaultValue = 28f,
                    Step = 1f
                },

                // Tumble Speed
                new FloatParameter
                {
                    Key = "fl_tumbleSpeed",
                    DisplayName = "Tumble Speed",
                    Description = "Speed at which leaves tumble and flip",
                    MinValue = 0f,
                    MaxValue = 6f,
                    DefaultValue = 2.0f,
                    Step = 0.2f
                },

                // Sway Amount
                new FloatParameter
                {
                    Key = "fl_swayAmount",
                    DisplayName = "Sway Amount",
                    Description = "Amount of horizontal swaying motion",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },

                // Spawn Radius
                new FloatParameter
                {
                    Key = "fl_spawnRadius",
                    DisplayName = "Spawn Radius",
                    Description = "Radius around cursor where leaves spawn",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 120f,
                    Step = 10f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "fl_lifetime",
                    DisplayName = "Leaf Lifetime",
                    Description = "How long leaves exist before fading (seconds)",
                    MinValue = 3f,
                    MaxValue = 20f,
                    DefaultValue = 10f,
                    Step = 1f
                },

                // Color Variety
                new FloatParameter
                {
                    Key = "fl_colorVariety",
                    DisplayName = "Color Variety",
                    Description = "Variety of autumn colors (0 = single color, 1 = full variety)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new FallingLeavesSettingsControl(effect);
}
