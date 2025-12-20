using MouseEffects.Core.Effects;
using MouseEffects.Effects.DandelionSeeds.UI;

namespace MouseEffects.Effects.DandelionSeeds;

public sealed class DandelionSeedsFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "dandelionseeds",
        Name = "Dandelion Seeds",
        Description = "Delicate dandelion seeds floating away from the mouse cursor on the wind",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new DandelionSeedsEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Dandelion Seed Settings (ds_ prefix)
        config.Set("ds_seedCount", 20);
        config.Set("ds_floatSpeed", 40f);
        config.Set("ds_windStrength", 50f);
        config.Set("ds_windFrequency", 0.3f);
        config.Set("ds_minSize", 12f);
        config.Set("ds_maxSize", 25f);
        config.Set("ds_tumbleSpeed", 0.8f);
        config.Set("ds_glowIntensity", 1.0f);
        config.Set("ds_spawnRadius", 120f);
        config.Set("ds_lifetime", 12f);
        config.Set("ds_upwardDrift", 20f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Seed Count
                new IntParameter
                {
                    Key = "ds_seedCount",
                    DisplayName = "Seed Count",
                    Description = "Number of seeds spawned per second",
                    MinValue = 5,
                    MaxValue = 50,
                    DefaultValue = 20
                },

                // Float Speed
                new FloatParameter
                {
                    Key = "ds_floatSpeed",
                    DisplayName = "Float Speed",
                    Description = "Speed of gentle floating oscillation",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },

                // Wind Strength
                new FloatParameter
                {
                    Key = "ds_windStrength",
                    DisplayName = "Wind Strength",
                    Description = "Strength of horizontal wind drift",
                    MinValue = 10f,
                    MaxValue = 150f,
                    DefaultValue = 50f,
                    Step = 5f
                },

                // Wind Frequency
                new FloatParameter
                {
                    Key = "ds_windFrequency",
                    DisplayName = "Wind Frequency",
                    Description = "How quickly the wind oscillates",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                // Upward Drift
                new FloatParameter
                {
                    Key = "ds_upwardDrift",
                    DisplayName = "Upward Drift",
                    Description = "Strength of gentle upward floating motion",
                    MinValue = 0f,
                    MaxValue = 60f,
                    DefaultValue = 20f,
                    Step = 5f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "ds_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum seed size in pixels",
                    MinValue = 5f,
                    MaxValue = 30f,
                    DefaultValue = 12f,
                    Step = 1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "ds_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum seed size in pixels",
                    MinValue = 10f,
                    MaxValue = 50f,
                    DefaultValue = 25f,
                    Step = 1f
                },

                // Tumble Speed
                new FloatParameter
                {
                    Key = "ds_tumbleSpeed",
                    DisplayName = "Tumble Speed",
                    Description = "Speed at which seeds tumble and spin",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },

                // Glow Intensity
                new FloatParameter
                {
                    Key = "ds_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of seed glow",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Spawn Radius
                new FloatParameter
                {
                    Key = "ds_spawnRadius",
                    DisplayName = "Spawn Radius",
                    Description = "Radius around cursor where seeds spawn",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 120f,
                    Step = 10f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "ds_lifetime",
                    DisplayName = "Seed Lifetime",
                    Description = "How long seeds exist before fading",
                    MinValue = 3f,
                    MaxValue = 30f,
                    DefaultValue = 12f,
                    Step = 1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new DandelionSeedsSettingsControl(effect);
}
