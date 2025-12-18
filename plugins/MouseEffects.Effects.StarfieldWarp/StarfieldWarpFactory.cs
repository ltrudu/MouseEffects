using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.StarfieldWarp.UI;

namespace MouseEffects.Effects.StarfieldWarp;

public sealed class StarfieldWarpFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "starfieldwarp",
        Name = "Starfield Warp",
        Description = "Hyperspace/warp speed effect with stars streaking past, centered on mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new StarfieldWarpEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Star field settings (sw_ prefix for StarfieldWarp)
        config.Set("sw_starCount", 500);
        config.Set("sw_warpSpeed", 1.0f);
        config.Set("sw_streakLength", 0.5f);
        config.Set("sw_effectRadius", 800f);
        config.Set("sw_starBrightness", 1.2f);
        config.Set("sw_starSize", 2.0f);
        config.Set("sw_depthLayers", 3);

        // Color settings
        config.Set("sw_colorTintEnabled", true);
        config.Set("sw_colorTint", new Vector4(0.6f, 0.8f, 1f, 1f));

        // Tunnel effect
        config.Set("sw_tunnelEffect", true);
        config.Set("sw_tunnelDarkness", 0.3f);

        // Pulse effect
        config.Set("sw_pulseEffect", true);
        config.Set("sw_pulseSpeed", 1.0f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Star Field Settings
                new IntParameter
                {
                    Key = "sw_starCount",
                    DisplayName = "Star Count",
                    Description = "Number of stars in the field",
                    MinValue = 100,
                    MaxValue = 1000,
                    DefaultValue = 500
                },
                new FloatParameter
                {
                    Key = "sw_warpSpeed",
                    DisplayName = "Warp Speed",
                    Description = "Speed of the warp effect",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sw_streakLength",
                    DisplayName = "Streak Length",
                    Description = "Length of star streaks",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sw_effectRadius",
                    DisplayName = "Effect Radius",
                    Description = "Radius of the starfield effect",
                    MinValue = 200f,
                    MaxValue = 1500f,
                    DefaultValue = 800f,
                    Step = 50f
                },
                new FloatParameter
                {
                    Key = "sw_starBrightness",
                    DisplayName = "Star Brightness",
                    Description = "Overall brightness of stars",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sw_starSize",
                    DisplayName = "Star Size",
                    Description = "Base size of individual stars",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new IntParameter
                {
                    Key = "sw_depthLayers",
                    DisplayName = "Depth Layers",
                    Description = "Number of depth layers for parallax effect",
                    MinValue = 1,
                    MaxValue = 5,
                    DefaultValue = 3
                },

                // Color Settings
                new BoolParameter
                {
                    Key = "sw_colorTintEnabled",
                    DisplayName = "Color Tint",
                    Description = "Enable color tint for stars",
                    DefaultValue = true
                },
                new ColorParameter
                {
                    Key = "sw_colorTint",
                    DisplayName = "Tint Color",
                    Description = "Color tint for stars (blue shift for hyperspace)",
                    DefaultValue = new Vector4(0.6f, 0.8f, 1f, 1f),
                    SupportsAlpha = false
                },

                // Tunnel Effect
                new BoolParameter
                {
                    Key = "sw_tunnelEffect",
                    DisplayName = "Tunnel Effect",
                    Description = "Darken center to create tunnel effect",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sw_tunnelDarkness",
                    DisplayName = "Tunnel Darkness",
                    Description = "How dark the center of the tunnel is",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },

                // Pulse Effect
                new BoolParameter
                {
                    Key = "sw_pulseEffect",
                    DisplayName = "Pulse Effect",
                    Description = "Stars pulse/flicker as they pass",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sw_pulseSpeed",
                    DisplayName = "Pulse Speed",
                    Description = "Speed of the pulsing effect",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new StarfieldWarpSettingsControl(effect);
}
