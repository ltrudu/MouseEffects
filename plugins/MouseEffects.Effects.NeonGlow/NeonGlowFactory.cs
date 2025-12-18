using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.NeonGlow.UI;

namespace MouseEffects.Effects.NeonGlow;

public sealed class NeonGlowFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "neonglow",
        Name = "Neon Glow",
        Description = "80s synthwave style neon trails with multilayer bloom following the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new NeonGlowEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Trail settings (ng_ prefix for NeonGlow)
        config.Set("ng_maxTrailPoints", 200);
        config.Set("ng_trailSpacing", 8f);

        // Line and glow settings
        config.Set("ng_lineThickness", 4f);
        config.Set("ng_glowLayers", 3);
        config.Set("ng_glowIntensity", 1.5f);

        // Fade and smoothing
        config.Set("ng_fadeSpeed", 1.0f);
        config.Set("ng_smoothingFactor", 0.3f);

        // Color settings
        config.Set("ng_colorMode", 1); // 0=fixed, 1=rainbow, 2=gradient
        config.Set("ng_primaryColor", new Vector4(1f, 0.08f, 0.58f, 1f));  // Hot pink #FF1493
        config.Set("ng_secondaryColor", new Vector4(0f, 1f, 1f, 1f));      // Cyan #00FFFF
        config.Set("ng_rainbowSpeed", 0.5f);

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
                    Key = "ng_maxTrailPoints",
                    DisplayName = "Trail Length",
                    Description = "Number of points in the trail (longer = more GPU usage)",
                    MinValue = 50,
                    MaxValue = 500,
                    DefaultValue = 200
                },
                new FloatParameter
                {
                    Key = "ng_trailSpacing",
                    DisplayName = "Trail Spacing",
                    Description = "Distance between trail points in pixels",
                    MinValue = 2f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 1f
                },

                // Line and Glow Settings
                new FloatParameter
                {
                    Key = "ng_lineThickness",
                    DisplayName = "Line Thickness",
                    Description = "Thickness of the neon line core",
                    MinValue = 1f,
                    MaxValue = 10f,
                    DefaultValue = 4f,
                    Step = 0.5f
                },
                new IntParameter
                {
                    Key = "ng_glowLayers",
                    DisplayName = "Glow Layers",
                    Description = "Number of glow layers (1-5, more = stronger bloom)",
                    MinValue = 1,
                    MaxValue = 5,
                    DefaultValue = 3
                },
                new FloatParameter
                {
                    Key = "ng_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of the glow effect",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Fade and Smoothing
                new FloatParameter
                {
                    Key = "ng_fadeSpeed",
                    DisplayName = "Fade Speed",
                    Description = "How quickly the trail fades out",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ng_smoothingFactor",
                    DisplayName = "Smoothing",
                    Description = "Trail smoothness (reduces jitter, 0 = no smoothing)",
                    MinValue = 0f,
                    MaxValue = 0.9f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                // Color Settings
                new ChoiceParameter
                {
                    Key = "ng_colorMode",
                    DisplayName = "Color Mode",
                    Description = "How colors are applied to the trail",
                    Choices = ["Fixed Color", "Rainbow", "Gradient"],
                    DefaultValue = "Rainbow"
                },
                new ColorParameter
                {
                    Key = "ng_primaryColor",
                    DisplayName = "Primary Color",
                    Description = "Main neon color (used in Fixed and Gradient modes)",
                    DefaultValue = new Vector4(1f, 0.08f, 0.58f, 1f),  // Hot pink
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "ng_secondaryColor",
                    DisplayName = "Secondary Color",
                    Description = "Secondary color for Gradient mode",
                    DefaultValue = new Vector4(0f, 1f, 1f, 1f),  // Cyan
                    SupportsAlpha = false
                },
                new FloatParameter
                {
                    Key = "ng_rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new NeonGlowSettingsControl(effect);
}
