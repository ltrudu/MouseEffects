using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.RadialDithering.UI;

namespace MouseEffects.Effects.RadialDithering;

/// <summary>
/// Factory for creating RadialDitheringEffect instances.
/// </summary>
public sealed class RadialDitheringFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "radial-dithering",
        Name = "Radial Dithering",
        Description = "Creates a Bayer-pattern dithering effect in a circular area around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.VisualFilter
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create()
    {
        return new RadialDitheringEffect();
    }

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        config.Set("radius", 200.0f);
        config.Set("intensity", 0.5f);
        config.Set("patternScale", 2.0f);
        config.Set("animationSpeed", 1.0f);
        config.Set("edgeSoftness", 0.3f);
        config.Set("enableAnimation", false);
        config.Set("invertPattern", false);
        config.Set("color1", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        config.Set("color2", new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        config.Set("falloffType", 1);
        config.Set("ringWidth", 0.3f);
        config.Set("enableGlow", false);
        config.Set("glowIntensity", 0.3f);
        config.Set("glowColor", new Vector4(0.3f, 0.5f, 1.0f, 1.0f));
        config.Set("colorBlendMode", 0);
        config.Set("threshold", 0.0f);
        config.Set("enableNoise", false);
        config.Set("noiseAmount", 0.2f);
        config.Set("alpha", 1.0f);
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
                    Key = "radius",
                    DisplayName = "Effect Radius",
                    Description = "Size of the dithering effect area around the mouse cursor (in pixels)",
                    MinValue = 0.0f,
                    MaxValue = 1920.0f,
                    DefaultValue = 200.0f,
                    Step = 10.0f
                },
                new FloatParameter
                {
                    Key = "intensity",
                    DisplayName = "Dither Intensity",
                    Description = "How strong the dithering effect appears",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.5f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "patternScale",
                    DisplayName = "Pattern Scale",
                    Description = "Size of the dither pattern pixels",
                    MinValue = 1.0f,
                    MaxValue = 8.0f,
                    DefaultValue = 2.0f,
                    Step = 1.0f
                },
                new FloatParameter
                {
                    Key = "edgeSoftness",
                    DisplayName = "Edge Softness",
                    Description = "How soft the edge of the effect appears",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "threshold",
                    DisplayName = "Threshold",
                    Description = "Adjust the dither threshold",
                    MinValue = -0.5f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.0f,
                    Step = 0.05f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new RadialDitheringSettingsControl(effect);
    }
}
