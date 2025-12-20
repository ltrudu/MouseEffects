using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.CherryBlossoms.UI;

namespace MouseEffects.Effects.CherryBlossoms;

public sealed class CherryBlossomsFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "cherry-blossoms",
        Name = "Cherry Blossoms",
        Description = "Beautiful sakura petals floating gently around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new CherryBlossomsEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Max Petals
        config.Set("cb_maxPetals", 500);

        // Cherry Blossom Settings (cb_ prefix)
        config.Set("cb_petalCount", 30);
        config.Set("cb_fallSpeed", 60f);
        config.Set("cb_swayAmount", 40f);
        config.Set("cb_swayFrequency", 0.8f);
        config.Set("cb_minSize", 10f);
        config.Set("cb_maxSize", 18f);
        config.Set("cb_spinSpeed", 1.5f);
        config.Set("cb_glowIntensity", 0.8f);
        config.Set("cb_spawnRadius", 180f);
        config.Set("cb_lifetime", 60f);
        config.Set("cb_colorPalette", 0); // Cherry Blossom default

        // Cursor interaction
        config.Set("cb_cursorInteraction", 1); // Attract
        config.Set("cb_cursorForceStrength", 2500f);
        config.Set("cb_cursorFieldRadius", 140f);

        // Wind settings
        config.Set("cb_windEnabled", true);
        config.Set("cb_windStrength", 50f);
        config.Set("cb_windDirection", 0f);
        config.Set("cb_windRandomDirection", true);
        config.Set("cb_windMinDirection", -154f);
        config.Set("cb_windMaxDirection", 120f);
        config.Set("cb_windTransitionMode", 7); // Logarithmic
        config.Set("cb_windTransitionDuration", 2f);
        config.Set("cb_windChangeFrequency", 5f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Max Petals
                new IntParameter
                {
                    Key = "cb_maxPetals",
                    DisplayName = "Max Petals",
                    Description = "Maximum number of petals on screen",
                    MinValue = 1,
                    MaxValue = 2500,
                    DefaultValue = 500
                },

                // Petal Count
                new IntParameter
                {
                    Key = "cb_petalCount",
                    DisplayName = "Petal Count",
                    Description = "Number of petals spawned per second",
                    MinValue = 10,
                    MaxValue = 100,
                    DefaultValue = 30
                },

                // Fall Speed
                new FloatParameter
                {
                    Key = "cb_fallSpeed",
                    DisplayName = "Fall Speed",
                    Description = "Downward falling speed of petals",
                    MinValue = 5f,
                    MaxValue = 250f,
                    DefaultValue = 60f,
                    Step = 5f
                },

                // Sway Amount
                new FloatParameter
                {
                    Key = "cb_swayAmount",
                    DisplayName = "Sway Amount",
                    Description = "Strength of side-to-side swaying motion",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },

                // Sway Frequency
                new FloatParameter
                {
                    Key = "cb_swayFrequency",
                    DisplayName = "Sway Frequency",
                    Description = "How quickly the petals sway back and forth",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "cb_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum petal size in pixels",
                    MinValue = 5f,
                    MaxValue = 25f,
                    DefaultValue = 10f,
                    Step = 1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "cb_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum petal size in pixels",
                    MinValue = 8f,
                    MaxValue = 40f,
                    DefaultValue = 18f,
                    Step = 1f
                },

                // Spin Speed
                new FloatParameter
                {
                    Key = "cb_spinSpeed",
                    DisplayName = "Spin Speed",
                    Description = "Speed at which petals tumble and spin",
                    MinValue = 0f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Glow Intensity
                new FloatParameter
                {
                    Key = "cb_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of petal soft glow",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },

                // Spawn Radius
                new FloatParameter
                {
                    Key = "cb_spawnRadius",
                    DisplayName = "Spawn Spread",
                    Description = "Horizontal spread around mouse X position where petals spawn from top",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 180f,
                    Step = 10f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "cb_lifetime",
                    DisplayName = "Petal Lifetime",
                    Description = "How long petals exist before fading (they also reset when off-screen)",
                    MinValue = 5f,
                    MaxValue = 120f,
                    DefaultValue = 60f,
                    Step = 5f
                },

                // Color Palette
                new IntParameter
                {
                    Key = "cb_colorPalette",
                    DisplayName = "Color Palette",
                    Description = "Color theme for the petals",
                    MinValue = 0,
                    MaxValue = 8,
                    DefaultValue = 0
                },

                // Cursor Interaction
                new IntParameter
                {
                    Key = "cb_cursorInteraction",
                    DisplayName = "Cursor Interaction",
                    Description = "How petals react to the cursor (0=None, 1=Attract, 2=Repel)",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 1
                },
                new FloatParameter
                {
                    Key = "cb_cursorForceStrength",
                    DisplayName = "Cursor Force",
                    Description = "Strength of cursor attraction/repulsion",
                    MinValue = 10f,
                    MaxValue = 5000f,
                    DefaultValue = 2500f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "cb_cursorFieldRadius",
                    DisplayName = "Cursor Field Radius",
                    Description = "Radius of cursor influence on petals",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 140f,
                    Step = 10f
                },

                // Wind Settings
                new BoolParameter
                {
                    Key = "cb_windEnabled",
                    DisplayName = "Enable Wind",
                    Description = "Enable wind effect on petals",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "cb_windStrength",
                    DisplayName = "Wind Strength",
                    Description = "How strong the wind pushes petals",
                    MinValue = 0f,
                    MaxValue = 200f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "cb_windDirection",
                    DisplayName = "Wind Direction",
                    Description = "Fixed wind direction in degrees (0=right, 90=down, 180=left, -90=up)",
                    MinValue = -180f,
                    MaxValue = 180f,
                    DefaultValue = 0f,
                    Step = 5f
                },
                new BoolParameter
                {
                    Key = "cb_windRandomDirection",
                    DisplayName = "Random Wind Direction",
                    Description = "Enable random wind direction changes",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "cb_windMinDirection",
                    DisplayName = "Min Random Direction",
                    Description = "Minimum random wind direction in degrees",
                    MinValue = -180f,
                    MaxValue = 180f,
                    DefaultValue = -154f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "cb_windMaxDirection",
                    DisplayName = "Max Random Direction",
                    Description = "Maximum random wind direction in degrees",
                    MinValue = -180f,
                    MaxValue = 180f,
                    DefaultValue = 120f,
                    Step = 5f
                },
                new IntParameter
                {
                    Key = "cb_windTransitionMode",
                    DisplayName = "Wind Transition Mode",
                    Description = "How wind direction changes (0=Instant, 1=Linear, 2=EaseIn, 3=EaseOut, 4=SmoothStep, 5=EaseInOut, 6=Exponential, 7=Logarithmic)",
                    MinValue = 0,
                    MaxValue = 7,
                    DefaultValue = 7
                },
                new FloatParameter
                {
                    Key = "cb_windTransitionDuration",
                    DisplayName = "Transition Duration",
                    Description = "How long it takes to transition between wind directions (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 10f,
                    DefaultValue = 2f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cb_windChangeFrequency",
                    DisplayName = "Direction Change Frequency",
                    Description = "How often wind direction changes (seconds)",
                    MinValue = 1f,
                    MaxValue = 30f,
                    DefaultValue = 5f,
                    Step = 0.5f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new CherryBlossomsSettingsControl(effect);
}
