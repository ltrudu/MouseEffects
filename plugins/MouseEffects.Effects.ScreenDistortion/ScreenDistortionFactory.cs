using MouseEffects.Core.Effects;
using MouseEffects.Effects.ScreenDistortion.UI;

namespace MouseEffects.Effects.ScreenDistortion;

/// <summary>
/// Factory for creating ScreenDistortionEffect instances.
/// </summary>
public sealed class ScreenDistortionFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "screen-distortion",
        Name = "Screen Distortion",
        Description = "Creates a lens/ripple distortion effect around the mouse cursor by distorting the screen content",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create()
    {
        return new ScreenDistortionEffect();
    }

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        config.Set("distortionRadius", 150.0f);
        config.Set("distortionStrength", 0.3f);
        config.Set("rippleFrequency", 8.0f);
        config.Set("rippleSpeed", 3.0f);
        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new FloatParameter
                {
                    Key = "distortionRadius",
                    DisplayName = "Distortion Radius",
                    Description = "Size of the distortion effect around the mouse cursor (in pixels)",
                    MinValue = 50.0f,
                    MaxValue = 400.0f,
                    DefaultValue = 150.0f,
                    Step = 10.0f
                },
                new FloatParameter
                {
                    Key = "distortionStrength",
                    DisplayName = "Distortion Strength",
                    Description = "Intensity of the distortion effect",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "rippleFrequency",
                    DisplayName = "Ripple Frequency",
                    Description = "Number of ripple waves in the effect",
                    MinValue = 1.0f,
                    MaxValue = 20.0f,
                    DefaultValue = 8.0f,
                    Step = 1.0f
                },
                new FloatParameter
                {
                    Key = "rippleSpeed",
                    DisplayName = "Ripple Speed",
                    Description = "Speed of the ripple animation",
                    MinValue = 0.0f,
                    MaxValue = 10.0f,
                    DefaultValue = 3.0f,
                    Step = 0.5f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new ScreenDistortionSettingsControl(effect);
    }
}
