using MouseEffects.Core.Effects;
using MouseEffects.Effects.Bubbles.UI;

namespace MouseEffects.Effects.Bubbles;

public sealed class BubblesFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "bubbles",
        Name = "Bubbles",
        Description = "Floating soap bubbles with rainbow iridescence following the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new BubblesEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Bubble Settings (b_ prefix)
        config.Set("b_bubbleCount", 10);
        config.Set("b_minSize", 15f);
        config.Set("b_maxSize", 35f);
        config.Set("b_floatSpeed", 25f);
        config.Set("b_wobbleAmount", 15f);
        config.Set("b_wobbleFrequency", 1.5f);
        config.Set("b_driftSpeed", 20f);
        config.Set("b_iridescenceIntensity", 1.0f);
        config.Set("b_iridescenceSpeed", 0.5f);
        config.Set("b_lifetime", 12f);
        config.Set("b_popEnabled", true);
        config.Set("b_popDuration", 0.3f);
        config.Set("b_transparency", 0.7f);
        config.Set("b_rimThickness", 0.08f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Bubble Count
                new IntParameter
                {
                    Key = "b_bubbleCount",
                    DisplayName = "Bubble Count",
                    Description = "Number of bubbles spawned per second while moving",
                    MinValue = 5,
                    MaxValue = 50,
                    DefaultValue = 10
                },

                // Min Size
                new FloatParameter
                {
                    Key = "b_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum bubble radius in pixels",
                    MinValue = 5f,
                    MaxValue = 40f,
                    DefaultValue = 15f,
                    Step = 1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "b_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum bubble radius in pixels",
                    MinValue = 10f,
                    MaxValue = 80f,
                    DefaultValue = 35f,
                    Step = 1f
                },

                // Float Speed
                new FloatParameter
                {
                    Key = "b_floatSpeed",
                    DisplayName = "Float Speed",
                    Description = "Upward floating speed of bubbles",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 25f,
                    Step = 5f
                },

                // Wobble Amount
                new FloatParameter
                {
                    Key = "b_wobbleAmount",
                    DisplayName = "Wobble Amount",
                    Description = "Strength of wobble movement",
                    MinValue = 0f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 1f
                },

                // Wobble Frequency
                new FloatParameter
                {
                    Key = "b_wobbleFrequency",
                    DisplayName = "Wobble Speed",
                    Description = "Speed of wobble oscillation",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Drift Speed
                new FloatParameter
                {
                    Key = "b_driftSpeed",
                    DisplayName = "Drift Speed",
                    Description = "Horizontal drift speed",
                    MinValue = 0f,
                    MaxValue = 60f,
                    DefaultValue = 20f,
                    Step = 5f
                },

                // Iridescence Intensity
                new FloatParameter
                {
                    Key = "b_iridescenceIntensity",
                    DisplayName = "Iridescence Intensity",
                    Description = "Rainbow shimmer intensity",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Iridescence Speed
                new FloatParameter
                {
                    Key = "b_iridescenceSpeed",
                    DisplayName = "Iridescence Speed",
                    Description = "How fast colors shift",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "b_lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long bubbles float before popping (seconds)",
                    MinValue = 5f,
                    MaxValue = 30f,
                    DefaultValue = 12f,
                    Step = 1f
                },

                // Pop Duration
                new FloatParameter
                {
                    Key = "b_popDuration",
                    DisplayName = "Pop Duration",
                    Description = "Duration of pop animation (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },

                // Transparency
                new FloatParameter
                {
                    Key = "b_transparency",
                    DisplayName = "Transparency",
                    Description = "Overall bubble transparency",
                    MinValue = 0.3f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.05f
                },

                // Rim Thickness
                new FloatParameter
                {
                    Key = "b_rimThickness",
                    DisplayName = "Rim Thickness",
                    Description = "Thickness of bubble outline",
                    MinValue = 0.02f,
                    MaxValue = 0.2f,
                    DefaultValue = 0.08f,
                    Step = 0.01f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new BubblesSettingsControl(effect);
}
