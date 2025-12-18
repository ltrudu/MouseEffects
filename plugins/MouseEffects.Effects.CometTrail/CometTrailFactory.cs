using MouseEffects.Core.Effects;
using MouseEffects.Effects.CometTrail.UI;

namespace MouseEffects.Effects.CometTrail;

public sealed class CometTrailFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "comettrail",
        Name = "Comet Trail",
        Description = "A blazing comet with fiery tail and sparks following the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Trail
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new CometTrailEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Trail settings (ct_ prefix for CometTrail)
        config.Set("ct_maxTrailPoints", 250);
        config.Set("ct_trailSpacing", 6f);

        // Comet appearance
        config.Set("ct_headSize", 20f);
        config.Set("ct_trailWidth", 8f);
        config.Set("ct_glowIntensity", 2.0f);

        // Spark settings
        config.Set("ct_sparkCount", 5);
        config.Set("ct_sparkSize", 3f);

        // Color temperature (0-1, cooler to hotter)
        config.Set("ct_colorTemperature", 0.7f);

        // Fade and smoothing
        config.Set("ct_fadeSpeed", 1.0f);
        config.Set("ct_smoothingFactor", 0.2f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Trail Settings
                new IntParameter
                {
                    Key = "ct_maxTrailPoints",
                    DisplayName = "Trail Length",
                    Description = "Number of points in the comet trail",
                    MinValue = 100,
                    MaxValue = 500,
                    DefaultValue = 250
                },
                new FloatParameter
                {
                    Key = "ct_trailSpacing",
                    DisplayName = "Trail Spacing",
                    Description = "Distance between trail points in pixels",
                    MinValue = 3f,
                    MaxValue = 15f,
                    DefaultValue = 6f,
                    Step = 1f
                },

                // Comet Appearance
                new FloatParameter
                {
                    Key = "ct_headSize",
                    DisplayName = "Head Size",
                    Description = "Size of the bright comet head",
                    MinValue = 10f,
                    MaxValue = 50f,
                    DefaultValue = 20f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ct_trailWidth",
                    DisplayName = "Trail Width",
                    Description = "Width of the fiery trail",
                    MinValue = 3f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "ct_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of the fire glow effect",
                    MinValue = 0.5f,
                    MaxValue = 4f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },

                // Spark Settings
                new IntParameter
                {
                    Key = "ct_sparkCount",
                    DisplayName = "Spark Count",
                    Description = "Number of sparks emitted per second",
                    MinValue = 0,
                    MaxValue = 20,
                    DefaultValue = 5
                },
                new FloatParameter
                {
                    Key = "ct_sparkSize",
                    DisplayName = "Spark Size",
                    Description = "Size of the embers breaking off",
                    MinValue = 1f,
                    MaxValue = 8f,
                    DefaultValue = 3f,
                    Step = 0.5f
                },

                // Color Settings
                new FloatParameter
                {
                    Key = "ct_colorTemperature",
                    DisplayName = "Color Temperature",
                    Description = "Fire temperature (0 = cooler orange, 1 = hotter white)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.05f
                },

                // Fade and Smoothing
                new FloatParameter
                {
                    Key = "ct_fadeSpeed",
                    DisplayName = "Fade Speed",
                    Description = "How quickly the trail fades out",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ct_smoothingFactor",
                    DisplayName = "Smoothing",
                    Description = "Trail smoothness (reduces jitter)",
                    MinValue = 0f,
                    MaxValue = 0.9f,
                    DefaultValue = 0.2f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new CometTrailSettingsControl(effect);
}
