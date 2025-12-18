using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Shockwave.UI;

namespace MouseEffects.Effects.Shockwave;

/// <summary>
/// Factory for creating ShockwaveEffect instances.
/// </summary>
public sealed class ShockwaveFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "shockwave",
        Name = "Shockwave",
        Description = "Creates expanding circular shockwave rings on click with glow and optional distortion",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Physics
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new ShockwaveEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // General settings
        config.Set("sw_maxShockwaves", 20);
        config.Set("sw_ringLifespan", 2.0f);
        config.Set("sw_expansionSpeed", 600f);
        config.Set("sw_maxRadius", 500f);
        config.Set("sw_ringThickness", 15f);
        config.Set("sw_glowIntensity", 1.5f);
        config.Set("sw_enableDistortion", true);
        config.Set("sw_distortionStrength", 20f);
        config.Set("sw_hdrBrightness", 1.0f);

        // Click settings
        config.Set("sw_spawnOnLeftClick", true);
        config.Set("sw_spawnOnRightClick", false);

        // Mouse move settings
        config.Set("sw_spawnOnMove", false);
        config.Set("sw_moveSpawnDistance", 100f);
        config.Set("sw_moveRingLifespan", 1.5f);
        config.Set("sw_moveExpansionSpeed", 400f);

        // Color settings
        config.Set("sw_colorPreset", 0); // Energy Blue
        config.Set("sw_customColor", new Vector4(0.0f, 0.5f, 1.0f, 1.0f));

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // General settings
                new IntParameter
                {
                    Key = "sw_maxShockwaves",
                    DisplayName = "Max Shockwaves",
                    Description = "Maximum number of shockwaves at the same time",
                    MinValue = 1,
                    MaxValue = 100,
                    DefaultValue = 20
                },
                new FloatParameter
                {
                    Key = "sw_ringLifespan",
                    DisplayName = "Ring Lifespan",
                    Description = "Seconds before ring fades out",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sw_expansionSpeed",
                    DisplayName = "Expansion Speed",
                    Description = "Speed of ring expansion (pixels/second)",
                    MinValue = 100f,
                    MaxValue = 2000f,
                    DefaultValue = 600f,
                    Step = 50f
                },
                new FloatParameter
                {
                    Key = "sw_maxRadius",
                    DisplayName = "Max Radius",
                    Description = "Maximum radius before ring disappears (pixels)",
                    MinValue = 100f,
                    MaxValue = 2000f,
                    DefaultValue = 500f,
                    Step = 50f
                },
                new FloatParameter
                {
                    Key = "sw_ringThickness",
                    DisplayName = "Ring Thickness",
                    Description = "Thickness of the shockwave ring (pixels)",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "sw_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the ring glow effect",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "sw_enableDistortion",
                    DisplayName = "Enable Distortion",
                    Description = "Distort screen inside the ring",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sw_distortionStrength",
                    DisplayName = "Distortion Strength",
                    Description = "Strength of screen distortion effect",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "sw_hdrBrightness",
                    DisplayName = "HDR Brightness",
                    Description = "Brightness multiplier for HDR displays",
                    MinValue = 0.1f,
                    MaxValue = 10f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Click settings
                new BoolParameter
                {
                    Key = "sw_spawnOnLeftClick",
                    DisplayName = "Spawn on Left Click",
                    Description = "Create shockwave when left mouse button is clicked",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "sw_spawnOnRightClick",
                    DisplayName = "Spawn on Right Click",
                    Description = "Create shockwave when right mouse button is clicked",
                    DefaultValue = false
                },

                // Mouse move settings
                new BoolParameter
                {
                    Key = "sw_spawnOnMove",
                    DisplayName = "Spawn on Mouse Move",
                    Description = "Create shockwaves as the mouse moves",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "sw_moveSpawnDistance",
                    DisplayName = "Move Spawn Distance",
                    Description = "Minimum mouse movement before spawning a new shockwave (pixels)",
                    MinValue = 10f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "sw_moveRingLifespan",
                    DisplayName = "Move Ring Lifespan",
                    Description = "Seconds before movement ring fades out",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sw_moveExpansionSpeed",
                    DisplayName = "Move Expansion Speed",
                    Description = "Speed of movement ring expansion (pixels/second)",
                    MinValue = 100f,
                    MaxValue = 2000f,
                    DefaultValue = 400f,
                    Step = 50f
                },

                // Color settings
                new IntParameter
                {
                    Key = "sw_colorPreset",
                    DisplayName = "Color Preset",
                    Description = "Ring color preset (0=Energy Blue, 1=Fire Red, 2=White, 3=Custom)",
                    MinValue = 0,
                    MaxValue = 3,
                    DefaultValue = 0
                },
                new ColorParameter
                {
                    Key = "sw_customColor",
                    DisplayName = "Custom Color",
                    Description = "Custom ring color (used when preset is Custom)",
                    DefaultValue = new Vector4(0.0f, 0.5f, 1.0f, 1.0f),
                    SupportsAlpha = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new ShockwaveSettingsControl(effect);
    }
}
