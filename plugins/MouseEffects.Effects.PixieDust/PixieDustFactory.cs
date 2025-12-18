using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.PixieDust.UI;

namespace MouseEffects.Effects.PixieDust;

public sealed class PixieDustFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "pixiedust",
        Name = "Pixie Dust",
        Description = "Magical sparkle particles that follow the mouse cursor with floating and fading effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new PixieDustEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Particle Settings (pd_ prefix)
        config.Set("pd_particleCount", 10);
        config.Set("pd_particleSize", 15f);
        config.Set("pd_lifetime", 2.0f);
        config.Set("pd_spawnRate", 0.05f);
        config.Set("pd_glowIntensity", 1.2f);
        config.Set("pd_driftSpeed", 30f);

        // Color Settings
        config.Set("pd_rainbowMode", true);
        config.Set("pd_rainbowSpeed", 0.5f);
        config.Set("pd_fixedColor", new Vector4(1f, 0.8f, 0.2f, 1f)); // Golden sparkles

        // Trigger Settings
        config.Set("pd_mouseMoveEnabled", true);
        config.Set("pd_moveDistanceThreshold", 15f);
        config.Set("pd_leftClickEnabled", true);
        config.Set("pd_leftClickBurstCount", 30);
        config.Set("pd_rightClickEnabled", true);
        config.Set("pd_rightClickBurstCount", 50);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Particle Count
                new IntParameter
                {
                    Key = "pd_particleCount",
                    DisplayName = "Particle Count",
                    Description = "Number of particles spawned per trigger",
                    MinValue = 1,
                    MaxValue = 100,
                    DefaultValue = 10
                },

                // Particle Size
                new FloatParameter
                {
                    Key = "pd_particleSize",
                    DisplayName = "Particle Size",
                    Description = "Size of sparkle particles in pixels",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 1f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "pd_lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long particles exist before fading (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },

                // Spawn Rate
                new FloatParameter
                {
                    Key = "pd_spawnRate",
                    DisplayName = "Spawn Rate",
                    Description = "Delay between particle spawns during movement",
                    MinValue = 0.01f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.05f,
                    Step = 0.01f
                },

                // Glow Intensity
                new FloatParameter
                {
                    Key = "pd_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness intensity of particle glow",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },

                // Drift Speed
                new FloatParameter
                {
                    Key = "pd_driftSpeed",
                    DisplayName = "Drift Speed",
                    Description = "Upward floating speed of particles",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },

                // Rainbow Mode
                new BoolParameter
                {
                    Key = "pd_rainbowMode",
                    DisplayName = "Rainbow Mode",
                    Description = "Cycle through rainbow colors over time",
                    DefaultValue = true
                },

                // Rainbow Speed
                new FloatParameter
                {
                    Key = "pd_rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Fixed Color
                new ColorParameter
                {
                    Key = "pd_fixedColor",
                    DisplayName = "Fixed Color",
                    Description = "Color when rainbow mode is disabled",
                    DefaultValue = new Vector4(1f, 0.8f, 0.2f, 1f),
                    SupportsAlpha = false
                },

                // Mouse Move Trigger
                new BoolParameter
                {
                    Key = "pd_mouseMoveEnabled",
                    DisplayName = "Mouse Move Trigger",
                    Description = "Spawn particles when moving the mouse",
                    DefaultValue = true
                },

                // Move Distance Threshold
                new FloatParameter
                {
                    Key = "pd_moveDistanceThreshold",
                    DisplayName = "Move Distance",
                    Description = "Distance in pixels before spawning particles",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 15f,
                    Step = 5f
                },

                // Left Click Trigger
                new BoolParameter
                {
                    Key = "pd_leftClickEnabled",
                    DisplayName = "Left Click Trigger",
                    Description = "Spawn particle burst on left click",
                    DefaultValue = true
                },

                // Left Click Burst Count
                new IntParameter
                {
                    Key = "pd_leftClickBurstCount",
                    DisplayName = "Left Click Burst",
                    Description = "Number of particles in left click burst",
                    MinValue = 10,
                    MaxValue = 100,
                    DefaultValue = 30
                },

                // Right Click Trigger
                new BoolParameter
                {
                    Key = "pd_rightClickEnabled",
                    DisplayName = "Right Click Trigger",
                    Description = "Spawn particle burst on right click",
                    DefaultValue = true
                },

                // Right Click Burst Count
                new IntParameter
                {
                    Key = "pd_rightClickBurstCount",
                    DisplayName = "Right Click Burst",
                    Description = "Number of particles in right click burst",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 50
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new PixieDustSettingsControl(effect);
}
