using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Hearts.UI;

namespace MouseEffects.Effects.Hearts;

public sealed class HeartsFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "hearts",
        Name = "Hearts",
        Description = "Floating heart particles following the mouse cursor with gentle wobble animation",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new HeartsEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Heart Settings (h_ prefix)
        config.Set("h_heartCount", 15);
        config.Set("h_floatSpeed", 40f);
        config.Set("h_wobbleAmount", 30f);
        config.Set("h_wobbleFrequency", 1.2f);
        config.Set("h_minSize", 12f);
        config.Set("h_maxSize", 25f);
        config.Set("h_rotationAmount", 0.3f);
        config.Set("h_glowIntensity", 1.0f);
        config.Set("h_sparkleIntensity", 0.5f);
        config.Set("h_lifetime", 8f);
        config.Set("h_colorMode", 0); // 0=Red, 1=Pink, 2=Mixed, 3=Rainbow

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Heart Count
                new IntParameter
                {
                    Key = "h_heartCount",
                    DisplayName = "Heart Count",
                    Description = "Number of hearts spawned per second while moving",
                    MinValue = 5,
                    MaxValue = 50,
                    DefaultValue = 15
                },

                // Float Speed
                new FloatParameter
                {
                    Key = "h_floatSpeed",
                    DisplayName = "Float Speed",
                    Description = "Upward floating speed of hearts",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },

                // Wobble Amount
                new FloatParameter
                {
                    Key = "h_wobbleAmount",
                    DisplayName = "Wobble Amount",
                    Description = "Strength of side-to-side wobble movement",
                    MinValue = 0f,
                    MaxValue = 80f,
                    DefaultValue = 30f,
                    Step = 5f
                },

                // Wobble Frequency
                new FloatParameter
                {
                    Key = "h_wobbleFrequency",
                    DisplayName = "Wobble Speed",
                    Description = "Speed of wobble oscillation",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "h_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum heart size in pixels",
                    MinValue = 5f,
                    MaxValue = 30f,
                    DefaultValue = 12f,
                    Step = 1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "h_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum heart size in pixels",
                    MinValue = 10f,
                    MaxValue = 50f,
                    DefaultValue = 25f,
                    Step = 1f
                },

                // Rotation Amount
                new FloatParameter
                {
                    Key = "h_rotationAmount",
                    DisplayName = "Rotation Amount",
                    Description = "Amount of gentle rotation/tilt",
                    MinValue = 0f,
                    MaxValue = 1.5f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                // Glow Intensity
                new FloatParameter
                {
                    Key = "h_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness intensity of heart glow",
                    MinValue = 0.3f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Sparkle Intensity
                new FloatParameter
                {
                    Key = "h_sparkleIntensity",
                    DisplayName = "Sparkle Intensity",
                    Description = "Intensity of sparkle effect",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "h_lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long hearts float before fading (seconds)",
                    MinValue = 3f,
                    MaxValue = 15f,
                    DefaultValue = 8f,
                    Step = 1f
                },

                // Color Mode
                new IntParameter
                {
                    Key = "h_colorMode",
                    DisplayName = "Color Mode",
                    Description = "0=Red, 1=Pink, 2=Mixed, 3=Rainbow",
                    MinValue = 0,
                    MaxValue = 3,
                    DefaultValue = 0
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new HeartsSettingsControl(effect);
}
