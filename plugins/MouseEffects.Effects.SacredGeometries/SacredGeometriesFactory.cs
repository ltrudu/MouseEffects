using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.SacredGeometries.UI;

namespace MouseEffects.Effects.SacredGeometries;

public sealed class SacredGeometriesFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "sacredgeometries",
        Name = "Sacred Geometries",
        Description = "Mystical mandala patterns with sacred geometry around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new SacredGeometriesEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Selected effect type for UI (0 = Mandala, future: more effects)
        config.Set("selectedEffectType", 0);

        // ===== Pattern Settings (sg_pat_ prefix) =====
        config.Set("sg_pat_selected", (int)PatternType.FlowerOfLife);
        config.Set("sg_pat_randomEnabled", true);
        config.Set("sg_pat_complexity", 1.0f);

        // ===== Radius Settings (sg_rad_ prefix) =====
        config.Set("sg_rad_fixed", 100f);
        config.Set("sg_rad_animated", true);
        config.Set("sg_rad_min", 60f);
        config.Set("sg_rad_max", 140f);
        config.Set("sg_rad_oscSpeed", 1.0f);

        // ===== Rotation Settings (sg_rot_ prefix) =====
        config.Set("sg_rot_speed", 30f);  // degrees per second
        config.Set("sg_rot_direction", 2);  // 0=left, 1=right, 2=random
        config.Set("sg_rot_randomSpeed", true);
        config.Set("sg_rot_minSpeed", 15f);
        config.Set("sg_rot_maxSpeed", 60f);

        // ===== Color Settings (sg_col_ prefix) =====
        config.Set("sg_col_primary", new Vector4(0.8f, 0.5f, 1f, 1f));  // Purple
        config.Set("sg_col_secondary", new Vector4(0.4f, 0.8f, 1f, 1f));  // Cyan
        config.Set("sg_col_rainbowMode", true);
        config.Set("sg_col_rainbowSpeed", 0.5f);
        config.Set("sg_col_randomRainbowSpeed", false);
        config.Set("sg_col_rainbowSpeedMin", 0.2f);
        config.Set("sg_col_rainbowSpeedMax", 1.5f);

        // ===== Glow Settings (sg_glow_ prefix) =====
        config.Set("sg_glow_intensity", 1.2f);
        config.Set("sg_glow_lineThickness", 2.0f);
        config.Set("sg_glow_twinkleIntensity", 0.3f);

        // ===== Appearance Settings (sg_app_ prefix) =====
        config.Set("sg_app_mode", (int)AppearanceMode.Both);
        config.Set("sg_app_randomMode", false);
        config.Set("sg_app_fadeInDuration", 0.3f);
        config.Set("sg_app_fadeOutDuration", 0.5f);
        config.Set("sg_app_scaleInDuration", 0.3f);
        config.Set("sg_app_scaleOutDuration", 0.4f);

        // ===== Spawn Settings (sg_spawn_ prefix) =====
        config.Set("sg_spawn_maxCount", 5);

        // ===== Trigger Settings (sg_trig_ prefix) =====
        config.Set("sg_trig_mouseMoveEnabled", true);
        config.Set("sg_trig_moveDistance", 80f);
        config.Set("sg_trig_leftClickEnabled", true);
        config.Set("sg_trig_rightClickEnabled", false);

        // ===== Lifetime Settings (sg_life_ prefix) =====
        config.Set("sg_life_duration", 3.0f);
        config.Set("sg_life_whileActiveMode", false);

        // ===== Performance Settings (sg_perf_ prefix) =====
        config.Set("sg_perf_maxActive", 20);
        config.Set("sg_perf_maxSpawnsPerSecond", 10);

        // ===== Morphing Settings (sg_morph_ prefix) =====
        config.Set("sg_morph_enabled", true);
        config.Set("sg_morph_speed", 0.5f);
        config.Set("sg_morph_intensity", 0.5f);
        config.Set("sg_morph_betweenPatterns", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Pattern Settings
                new ChoiceParameter
                {
                    Key = "sg_pat_selected",
                    DisplayName = "Pattern",
                    Description = "Sacred geometry pattern to display",
                    Choices = [
                        "Seed of Life", "Flower of Life", "Vesica Piscis", "Merkaba",
                        "Sri Yantra", "Metatron's Cube", "Tree of Life", "Torus",
                        "Tetrahedron Grid", "Platonic Solids"
                    ],
                    DefaultValue = "Flower of Life"
                },
                new BoolParameter
                {
                    Key = "sg_pat_randomEnabled",
                    DisplayName = "Random Pattern",
                    Description = "Randomize pattern for each mandala",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sg_pat_complexity",
                    DisplayName = "Complexity",
                    Description = "Pattern detail level (where applicable)",
                    MinValue = 0.5f,
                    MaxValue = 3.0f,
                    DefaultValue = 1.0f
                },

                // Radius Settings
                new FloatParameter
                {
                    Key = "sg_rad_fixed",
                    DisplayName = "Radius",
                    Description = "Mandala radius in pixels",
                    MinValue = 20f,
                    MaxValue = 300f,
                    DefaultValue = 100f
                },
                new BoolParameter
                {
                    Key = "sg_rad_animated",
                    DisplayName = "Animate Radius",
                    Description = "Oscillate radius between min and max",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sg_rad_min",
                    DisplayName = "Min Radius",
                    Description = "Minimum radius when animating",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 60f
                },
                new FloatParameter
                {
                    Key = "sg_rad_max",
                    DisplayName = "Max Radius",
                    Description = "Maximum radius when animating",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 140f
                },
                new FloatParameter
                {
                    Key = "sg_rad_oscSpeed",
                    DisplayName = "Oscillation Speed",
                    Description = "Speed of radius animation",
                    MinValue = 0.1f,
                    MaxValue = 5.0f,
                    DefaultValue = 1.0f
                },

                // Rotation Settings
                new FloatParameter
                {
                    Key = "sg_rot_speed",
                    DisplayName = "Rotation Speed",
                    Description = "Rotation speed in degrees per second",
                    MinValue = 0f,
                    MaxValue = 180f,
                    DefaultValue = 30f
                },
                new ChoiceParameter
                {
                    Key = "sg_rot_direction",
                    DisplayName = "Direction",
                    Description = "Rotation direction",
                    Choices = ["Left", "Right", "Random"],
                    DefaultValue = "Random"
                },
                new BoolParameter
                {
                    Key = "sg_rot_randomSpeed",
                    DisplayName = "Random Speed",
                    Description = "Randomize rotation speed per mandala",
                    DefaultValue = true
                },

                // Color Settings
                new ColorParameter
                {
                    Key = "sg_col_primary",
                    DisplayName = "Primary Color",
                    Description = "Main mandala color",
                    DefaultValue = new Vector4(0.8f, 0.5f, 1f, 1f)
                },
                new ColorParameter
                {
                    Key = "sg_col_secondary",
                    DisplayName = "Secondary Color",
                    Description = "Accent color for patterns",
                    DefaultValue = new Vector4(0.4f, 0.8f, 1f, 1f)
                },
                new BoolParameter
                {
                    Key = "sg_col_rainbowMode",
                    DisplayName = "Rainbow Mode",
                    Description = "Cycle through rainbow colors",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sg_col_rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Color cycling speed",
                    MinValue = 0.1f,
                    MaxValue = 3.0f,
                    DefaultValue = 0.5f
                },

                // Glow Settings
                new FloatParameter
                {
                    Key = "sg_glow_intensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of the glow effect",
                    MinValue = 0f,
                    MaxValue = 3.0f,
                    DefaultValue = 1.2f
                },
                new FloatParameter
                {
                    Key = "sg_glow_lineThickness",
                    DisplayName = "Line Thickness",
                    Description = "Thickness of pattern lines",
                    MinValue = 0.5f,
                    MaxValue = 5.0f,
                    DefaultValue = 2.0f
                },

                // Appearance Settings
                new ChoiceParameter
                {
                    Key = "sg_app_mode",
                    DisplayName = "Appearance Mode",
                    Description = "How mandalas appear and disappear",
                    Choices = ["Fade", "Scale", "Both"],
                    DefaultValue = "Both"
                },
                new BoolParameter
                {
                    Key = "sg_app_randomMode",
                    DisplayName = "Random Mode",
                    Description = "Randomize appearance mode per mandala",
                    DefaultValue = false
                },

                // Trigger Settings
                new BoolParameter
                {
                    Key = "sg_trig_mouseMoveEnabled",
                    DisplayName = "Mouse Move",
                    Description = "Spawn mandalas on mouse movement",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "sg_trig_moveDistance",
                    DisplayName = "Move Distance",
                    Description = "Distance threshold for spawning",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 80f
                },
                new BoolParameter
                {
                    Key = "sg_trig_leftClickEnabled",
                    DisplayName = "Left Click",
                    Description = "Spawn mandala on left click",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "sg_trig_rightClickEnabled",
                    DisplayName = "Right Click",
                    Description = "Spawn mandala on right click",
                    DefaultValue = false
                },

                // Lifetime Settings
                new FloatParameter
                {
                    Key = "sg_life_duration",
                    DisplayName = "Lifetime",
                    Description = "How long mandalas last (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 10.0f,
                    DefaultValue = 3.0f
                },
                new BoolParameter
                {
                    Key = "sg_life_whileActiveMode",
                    DisplayName = "While Active",
                    Description = "Stay visible while trigger is active",
                    DefaultValue = false
                },

                // Spawn Settings
                new IntParameter
                {
                    Key = "sg_spawn_maxCount",
                    DisplayName = "Max Mandalas",
                    Description = "Maximum simultaneous mandalas (1 = follow cursor)",
                    MinValue = 1,
                    MaxValue = 50,
                    DefaultValue = 5
                },

                // Performance Settings
                new IntParameter
                {
                    Key = "sg_perf_maxActive",
                    DisplayName = "Max Active",
                    Description = "Maximum active mandalas for performance",
                    MinValue = 1,
                    MaxValue = 100,
                    DefaultValue = 20
                },
                new IntParameter
                {
                    Key = "sg_perf_maxSpawnsPerSecond",
                    DisplayName = "Max Spawns/Second",
                    Description = "Rate limit for spawning",
                    MinValue = 1,
                    MaxValue = 60,
                    DefaultValue = 10
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        if (effect is SacredGeometriesEffect sgEffect)
            return new SacredGeometriesSettingsControl(sgEffect);
        return null;
    }
}
