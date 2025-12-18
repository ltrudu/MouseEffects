using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Butterflies.UI;

namespace MouseEffects.Effects.Butterflies;

public sealed class ButterfliesFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "butterflies",
        Name = "Butterflies",
        Description = "Beautiful animated butterflies that flutter around and follow the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new ButterfliesEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Butterfly count and size
        config.Set("bf_butterflyCount", 8);
        config.Set("bf_minSize", 15f);
        config.Set("bf_maxSize", 30f);

        // Wing animation
        config.Set("bf_wingFlapSpeed", 8f);

        // Following behavior
        config.Set("bf_followDistance", 100f);
        config.Set("bf_followStrength", 0.3f);
        config.Set("bf_wanderStrength", 50f);

        // Visual properties
        config.Set("bf_glowIntensity", 1.0f);
        config.Set("bf_colorMode", 0); // 0=Rainbow, 1=Pastel, 2=Nature
        config.Set("bf_rainbowSpeed", 0.3f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Butterfly Count and Size
                new IntParameter
                {
                    Key = "bf_butterflyCount",
                    DisplayName = "Butterfly Count",
                    Description = "Number of butterflies to display",
                    MinValue = 1,
                    MaxValue = 20,
                    DefaultValue = 8
                },
                new FloatParameter
                {
                    Key = "bf_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum butterfly size",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "bf_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum butterfly size",
                    MinValue = 10f,
                    MaxValue = 80f,
                    DefaultValue = 30f,
                    Step = 1f
                },

                // Wing Animation
                new FloatParameter
                {
                    Key = "bf_wingFlapSpeed",
                    DisplayName = "Wing Flap Speed",
                    Description = "How fast the wings flap",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 0.5f
                },

                // Following Behavior
                new FloatParameter
                {
                    Key = "bf_followDistance",
                    DisplayName = "Follow Distance",
                    Description = "How close butterflies stay to cursor (pixels)",
                    MinValue = 20f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "bf_followStrength",
                    DisplayName = "Follow Strength",
                    Description = "How quickly butterflies move toward cursor",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "bf_wanderStrength",
                    DisplayName = "Wander Strength",
                    Description = "How much butterflies wander randomly",
                    MinValue = 0f,
                    MaxValue = 150f,
                    DefaultValue = 50f,
                    Step = 10f
                },

                // Visual Properties
                new FloatParameter
                {
                    Key = "bf_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of butterfly glow",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new ChoiceParameter
                {
                    Key = "bf_colorMode",
                    DisplayName = "Color Mode",
                    Description = "Color scheme for butterflies",
                    Choices = ["Rainbow", "Pastel", "Nature"],
                    DefaultValue = "Rainbow"
                },
                new FloatParameter
                {
                    Key = "bf_rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling (when in Rainbow mode)",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new ButterfliesSettingsControl(effect);
}
