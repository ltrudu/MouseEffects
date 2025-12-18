using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Runes.UI;

namespace MouseEffects.Effects.Runes;

public sealed class RunesFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "runes",
        Name = "Runes",
        Description = "Floating magical runes and symbols that appear and fade around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new RunesEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        config.Set("rn_runeCount", 3);
        config.Set("rn_runeSize", 40f);
        config.Set("rn_lifetime", 3.0f);
        config.Set("rn_glowIntensity", 1.5f);
        config.Set("rn_rotationSpeed", 0.5f);
        config.Set("rn_floatDistance", 20f);
        config.Set("rn_rainbowMode", false);
        config.Set("rn_rainbowSpeed", 0.3f);
        config.Set("rn_fixedColor", new Vector4(1f, 0.84f, 0f, 1f));
        config.Set("rn_mouseMoveEnabled", true);
        config.Set("rn_moveDistanceThreshold", 60f);
        config.Set("rn_leftClickEnabled", true);
        config.Set("rn_leftClickBurstCount", 5);
        config.Set("rn_rightClickEnabled", true);
        config.Set("rn_rightClickBurstCount", 8);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new IntParameter
                {
                    Key = "rn_runeCount",
                    DisplayName = "Rune Count",
                    Description = "Number of runes spawned per trigger",
                    MinValue = 1,
                    MaxValue = 20,
                    DefaultValue = 3
                },

                new FloatParameter
                {
                    Key = "rn_runeSize",
                    DisplayName = "Rune Size",
                    Description = "Size of runes in pixels",
                    MinValue = 20f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },

                new FloatParameter
                {
                    Key = "rn_lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long runes exist before fading (seconds)",
                    MinValue = 1f,
                    MaxValue = 10f,
                    DefaultValue = 3.0f,
                    Step = 0.5f
                },

                new FloatParameter
                {
                    Key = "rn_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness intensity of rune glow",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                new FloatParameter
                {
                    Key = "rn_rotationSpeed",
                    DisplayName = "Rotation Speed",
                    Description = "Speed of rune rotation (radians/sec)",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                new FloatParameter
                {
                    Key = "rn_floatDistance",
                    DisplayName = "Float Distance",
                    Description = "Maximum distance of floating motion",
                    MinValue = 0f,
                    MaxValue = 50f,
                    DefaultValue = 20f,
                    Step = 5f
                },

                new BoolParameter
                {
                    Key = "rn_rainbowMode",
                    DisplayName = "Rainbow Mode",
                    Description = "Cycle through rainbow colors over time",
                    DefaultValue = false
                },

                new FloatParameter
                {
                    Key = "rn_rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                new ColorParameter
                {
                    Key = "rn_fixedColor",
                    DisplayName = "Rune Color",
                    Description = "Color when rainbow mode is disabled",
                    DefaultValue = new Vector4(1f, 0.84f, 0f, 1f),
                    SupportsAlpha = false
                },

                new BoolParameter
                {
                    Key = "rn_mouseMoveEnabled",
                    DisplayName = "Mouse Move Trigger",
                    Description = "Spawn runes when moving the mouse",
                    DefaultValue = true
                },

                new FloatParameter
                {
                    Key = "rn_moveDistanceThreshold",
                    DisplayName = "Move Distance",
                    Description = "Distance in pixels before spawning runes",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 60f,
                    Step = 10f
                },

                new BoolParameter
                {
                    Key = "rn_leftClickEnabled",
                    DisplayName = "Left Click Trigger",
                    Description = "Spawn rune burst on left click",
                    DefaultValue = true
                },

                new IntParameter
                {
                    Key = "rn_leftClickBurstCount",
                    DisplayName = "Left Click Burst",
                    Description = "Number of runes in left click burst",
                    MinValue = 1,
                    MaxValue = 15,
                    DefaultValue = 5
                },

                new BoolParameter
                {
                    Key = "rn_rightClickEnabled",
                    DisplayName = "Right Click Trigger",
                    Description = "Spawn rune burst on right click",
                    DefaultValue = true
                },

                new IntParameter
                {
                    Key = "rn_rightClickBurstCount",
                    DisplayName = "Right Click Burst",
                    Description = "Number of runes in right click burst",
                    MinValue = 1,
                    MaxValue = 20,
                    DefaultValue = 8
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new RunesSettingsControl(effect);
}
