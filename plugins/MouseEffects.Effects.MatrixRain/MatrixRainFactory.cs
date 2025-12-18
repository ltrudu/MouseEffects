using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.MatrixRain.UI;

namespace MouseEffects.Effects.MatrixRain;

public sealed class MatrixRainFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "matrixrain",
        Name = "Matrix Rain",
        Description = "Iconic falling green code effect from The Matrix centered around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Digital
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new MatrixRainEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Matrix Rain settings (mr_ prefix)
        config.Set("mr_columnDensity", 0.04f);        // Columns per pixel (~25px spacing)
        config.Set("mr_minFallSpeed", 100f);          // Min fall speed (pixels/sec)
        config.Set("mr_maxFallSpeed", 300f);          // Max fall speed (pixels/sec)
        config.Set("mr_charChangeRate", 8f);          // Character changes per second
        config.Set("mr_glowIntensity", 1.2f);         // Glow brightness
        config.Set("mr_trailLength", 0.7f);           // Trail fade length (0-1)
        config.Set("mr_effectRadius", 300f);          // Effect radius around cursor
        config.Set("mr_color", new Vector4(0.2f, 1f, 0.3f, 1f));  // Matrix green

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Column Settings
                new FloatParameter
                {
                    Key = "mr_columnDensity",
                    DisplayName = "Column Density",
                    Description = "Number of falling columns (higher = more columns)",
                    MinValue = 0.01f,
                    MaxValue = 0.1f,
                    DefaultValue = 0.04f,
                    Step = 0.005f
                },

                // Speed Settings
                new FloatParameter
                {
                    Key = "mr_minFallSpeed",
                    DisplayName = "Min Fall Speed",
                    Description = "Minimum falling speed in pixels per second",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "mr_maxFallSpeed",
                    DisplayName = "Max Fall Speed",
                    Description = "Maximum falling speed in pixels per second",
                    MinValue = 100f,
                    MaxValue = 800f,
                    DefaultValue = 300f,
                    Step = 10f
                },

                // Character Settings
                new FloatParameter
                {
                    Key = "mr_charChangeRate",
                    DisplayName = "Character Change Rate",
                    Description = "How fast characters change (times per second)",
                    MinValue = 1f,
                    MaxValue = 30f,
                    DefaultValue = 8f,
                    Step = 1f
                },

                // Appearance Settings
                new FloatParameter
                {
                    Key = "mr_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of the glow effect",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "mr_trailLength",
                    DisplayName = "Trail Length",
                    Description = "Length of the fading trail (0 = short, 1 = long)",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.05f
                },

                // Effect Radius
                new FloatParameter
                {
                    Key = "mr_effectRadius",
                    DisplayName = "Effect Radius",
                    Description = "Radius around cursor where effect appears (pixels)",
                    MinValue = 100f,
                    MaxValue = 800f,
                    DefaultValue = 300f,
                    Step = 10f
                },

                // Color
                new ColorParameter
                {
                    Key = "mr_color",
                    DisplayName = "Color",
                    Description = "Color of the Matrix rain (default: green)",
                    DefaultValue = new Vector4(0.2f, 1f, 0.3f, 1f),
                    SupportsAlpha = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new MatrixRainSettingsControl(effect);
}
