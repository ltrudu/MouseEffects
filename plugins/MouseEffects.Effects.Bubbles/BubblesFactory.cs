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

        // Animation Settings - Appears (0=None, 1=FadeIn, 2=ZoomIn)
        config.Set("b_appearsAnimation", 2); // Zoom In
        config.Set("b_fadeInSpeed", 0.5f);
        config.Set("b_fadeInStartAlpha", 0f);
        config.Set("b_fadeInEndAlpha", 1f);
        config.Set("b_zoomInSpeed", 0.5f);
        config.Set("b_zoomInStartScale", 0f);
        config.Set("b_zoomInEndScale", 1.1f);

        // Animation Settings - Disappears (0=None, 1=FadeOut, 2=ZoomOut, 3=PopOut)
        config.Set("b_disappearsAnimation", 3); // Pop Out
        config.Set("b_fadeOutSpeed", 0.5f);
        config.Set("b_fadeOutStartAlpha", 1f);
        config.Set("b_fadeOutEndAlpha", 0f);
        config.Set("b_zoomOutSpeed", 0.5f);
        config.Set("b_zoomOutStartScale", 1f);
        config.Set("b_zoomOutEndScale", 0f);
        config.Set("b_popDuration", 0.24f);

        // Bubble Settings (b_ prefix)
        config.Set("b_maxBubbles", 150);
        config.Set("b_bubbleCount", 10);
        config.Set("b_minSize", 15f);
        config.Set("b_maxSize", 35f);
        config.Set("b_floatSpeed", 27f);
        config.Set("b_wobbleAmount", 15f);
        config.Set("b_wobbleFrequency", 1.36f);
        config.Set("b_driftSpeed", 20f);
        config.Set("b_iridescenceIntensity", 1.3f);
        config.Set("b_iridescenceSpeed", 0.5f);
        config.Set("b_lifetime", 15f);
        config.Set("b_transparency", 1.0f);
        config.Set("b_rimThickness", 0.088f);

        // Diffraction settings
        config.Set("b_diffractionEnabled", true);
        config.Set("b_diffractionStrength", 0.4f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Animation - Appears
                new ChoiceParameter
                {
                    Key = "b_appearsAnimation",
                    DisplayName = "Appears Animation",
                    Description = "Animation when bubble first appears",
                    Choices = ["No Animation", "Fade In", "Zoom In"],
                    DefaultValue = "No Animation"
                },
                new FloatParameter
                {
                    Key = "b_fadeInSpeed",
                    DisplayName = "Fade In Speed",
                    Description = "Duration of fade in animation (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "b_fadeInStartAlpha",
                    DisplayName = "Fade In Start Alpha",
                    Description = "Starting opacity for fade in",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "b_fadeInEndAlpha",
                    DisplayName = "Fade In End Alpha",
                    Description = "Ending opacity for fade in",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 1f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "b_zoomInSpeed",
                    DisplayName = "Zoom In Speed",
                    Description = "Duration of zoom in animation (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "b_zoomInStartScale",
                    DisplayName = "Zoom In Start Scale",
                    Description = "Starting scale for zoom in",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "b_zoomInEndScale",
                    DisplayName = "Zoom In End Scale",
                    Description = "Ending scale for zoom in",
                    MinValue = 0.5f,
                    MaxValue = 2f,
                    DefaultValue = 1f,
                    Step = 0.05f
                },

                // Animation - Disappears
                new ChoiceParameter
                {
                    Key = "b_disappearsAnimation",
                    DisplayName = "Disappears Animation",
                    Description = "Animation when bubble disappears",
                    Choices = ["No Animation", "Fade Out", "Zoom Out", "Pop Out"],
                    DefaultValue = "Pop Out"
                },
                new FloatParameter
                {
                    Key = "b_fadeOutSpeed",
                    DisplayName = "Fade Out Speed",
                    Description = "Duration of fade out animation (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "b_fadeOutStartAlpha",
                    DisplayName = "Fade Out Start Alpha",
                    Description = "Starting opacity for fade out",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 1f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "b_fadeOutEndAlpha",
                    DisplayName = "Fade Out End Alpha",
                    Description = "Ending opacity for fade out",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "b_zoomOutSpeed",
                    DisplayName = "Zoom Out Speed",
                    Description = "Duration of zoom out animation (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "b_zoomOutStartScale",
                    DisplayName = "Zoom Out Start Scale",
                    Description = "Starting scale for zoom out",
                    MinValue = 0.5f,
                    MaxValue = 2f,
                    DefaultValue = 1f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "b_zoomOutEndScale",
                    DisplayName = "Zoom Out End Scale",
                    Description = "Ending scale for zoom out",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0f,
                    Step = 0.05f
                },
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

                // Max Bubbles
                new IntParameter
                {
                    Key = "b_maxBubbles",
                    DisplayName = "Max Bubbles",
                    Description = "Maximum number of bubbles on screen at once",
                    MinValue = 1,
                    MaxValue = 500,
                    DefaultValue = 150
                },

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
                    Description = "How long bubbles float before disappearing (seconds)",
                    MinValue = 5f,
                    MaxValue = 30f,
                    DefaultValue = 12f,
                    Step = 1f
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
                },

                // Diffraction Effect
                new BoolParameter
                {
                    Key = "b_diffractionEnabled",
                    DisplayName = "Diffraction Effect",
                    Description = "Enable screen refraction through bubbles",
                    DefaultValue = false
                },

                // Diffraction Strength
                new FloatParameter
                {
                    Key = "b_diffractionStrength",
                    DisplayName = "Diffraction Strength",
                    Description = "Intensity of lens distortion effect",
                    MinValue = 0.05f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new BubblesSettingsControl(effect);
}
