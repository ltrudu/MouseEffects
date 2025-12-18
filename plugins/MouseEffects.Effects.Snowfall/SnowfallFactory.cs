using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Snowfall.UI;

namespace MouseEffects.Effects.Snowfall;

public sealed class SnowfallFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "snowfall",
        Name = "Snowfall",
        Description = "Gentle snowflakes falling around the mouse cursor with wind physics",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new SnowfallEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Snowflake Settings (sf_ prefix)
        config.Set("sf_snowflakeCount", 50);
        config.Set("sf_fallSpeed", 80f);
        config.Set("sf_windStrength", 30f);
        config.Set("sf_windFrequency", 0.5f);
        config.Set("sf_minSize", 8f);
        config.Set("sf_maxSize", 20f);
        config.Set("sf_rotationSpeed", 1.0f);
        config.Set("sf_glowIntensity", 1.0f);
        config.Set("sf_spawnRadius", 150f);
        config.Set("sf_lifetime", 8f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Snowflake Count
                new IntParameter
                {
                    Key = "sf_snowflakeCount",
                    DisplayName = "Snowflake Count",
                    Description = "Number of snowflakes spawned per second",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 50
                },

                // Fall Speed
                new FloatParameter
                {
                    Key = "sf_fallSpeed",
                    DisplayName = "Fall Speed",
                    Description = "Downward falling speed of snowflakes",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 80f,
                    Step = 5f
                },

                // Wind Strength
                new FloatParameter
                {
                    Key = "sf_windStrength",
                    DisplayName = "Wind Strength",
                    Description = "Strength of horizontal wind drift",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },

                // Wind Frequency
                new FloatParameter
                {
                    Key = "sf_windFrequency",
                    DisplayName = "Wind Frequency",
                    Description = "How quickly the wind oscillates",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "sf_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum snowflake size in pixels",
                    MinValue = 3f,
                    MaxValue = 30f,
                    DefaultValue = 8f,
                    Step = 1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "sf_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum snowflake size in pixels",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 20f,
                    Step = 1f
                },

                // Rotation Speed
                new FloatParameter
                {
                    Key = "sf_rotationSpeed",
                    DisplayName = "Rotation Speed",
                    Description = "Speed at which snowflakes tumble",
                    MinValue = 0f,
                    MaxValue = 5f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Glow Intensity
                new FloatParameter
                {
                    Key = "sf_glowIntensity",
                    DisplayName = "Glow/Sparkle Intensity",
                    Description = "Brightness of snowflake glow",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Spawn Radius
                new FloatParameter
                {
                    Key = "sf_spawnRadius",
                    DisplayName = "Spawn Radius",
                    Description = "Radius around cursor where snowflakes spawn",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 150f,
                    Step = 10f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "sf_lifetime",
                    DisplayName = "Snowflake Lifetime",
                    Description = "How long snowflakes exist before fading",
                    MinValue = 2f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new SnowfallSettingsControl(effect);
}
