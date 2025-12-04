using MouseEffects.Core.Effects;
using MouseEffects.Effects.ColorBlindness.UI;

namespace MouseEffects.Effects.ColorBlindness;

/// <summary>
/// Factory for creating ColorBlindnessEffect instances.
/// </summary>
public sealed class ColorBlindnessFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "color-blindness",
        Name = "Color Blindness",
        Description = "Simulates color blindness conditions with RGB curve adjustment. Apply to circular, rectangular, or fullscreen areas.",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create()
    {
        return new ColorBlindnessEffect();
    }

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        config.Set("radius", 300.0f);
        config.Set("rectWidth", 400.0f);
        config.Set("rectHeight", 300.0f);
        config.Set("shapeMode", 2); // Fullscreen
        config.Set("filterType", 0); // None (inside shape default)
        config.Set("outsideFilterType", 0); // None (outside shape default)
        config.Set("intensity", 1.0f);
        config.Set("colorBoost", 1.0f);
        config.Set("edgeSoftness", 0.2f);
        config.Set("enableCurves", false);
        config.Set("curveStrength", 1.0f);
        // Curves default to linear (handled by CurveData.CreateLinear())
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
                    Key = "shapeMode",
                    DisplayName = "Shape Mode",
                    Description = "How the effect is applied: 0=Circle, 1=Rectangle, 2=Fullscreen",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "radius",
                    DisplayName = "Radius",
                    Description = "Radius of the circular effect area (in pixels)",
                    MinValue = 50.0f,
                    MaxValue = 1920.0f,
                    DefaultValue = 300.0f,
                    Step = 10.0f
                },
                new FloatParameter
                {
                    Key = "rectWidth",
                    DisplayName = "Rectangle Width",
                    Description = "Width of the rectangular effect area (in pixels)",
                    MinValue = 100.0f,
                    MaxValue = 1920.0f,
                    DefaultValue = 400.0f,
                    Step = 10.0f
                },
                new FloatParameter
                {
                    Key = "rectHeight",
                    DisplayName = "Rectangle Height",
                    Description = "Height of the rectangular effect area (in pixels)",
                    MinValue = 100.0f,
                    MaxValue = 1080.0f,
                    DefaultValue = 300.0f,
                    Step = 10.0f
                },
                new IntParameter
                {
                    Key = "filterType",
                    DisplayName = "Inside Shape Filter Type",
                    Description = "Filter applied inside the shape (circle/rectangle) or as the only filter in fullscreen mode: 0=None, 1=Deuteranopia, 2=Protanopia, 3=Tritanopia, 4=Grayscale, 5=Grayscale Inverted, 6=Inverted",
                    MinValue = 0,
                    MaxValue = 6,
                    DefaultValue = 4
                },
                new IntParameter
                {
                    Key = "outsideFilterType",
                    DisplayName = "Outside Shape Filter Type",
                    Description = "Filter applied outside the shape (circle/rectangle). Only used when shape mode is not fullscreen: 0=None, 1=Deuteranopia, 2=Protanopia, 3=Tritanopia, 4=Grayscale, 5=Grayscale Inverted, 6=Inverted",
                    MinValue = 0,
                    MaxValue = 6,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "intensity",
                    DisplayName = "Intensity",
                    Description = "Strength of the color blindness filter",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 1.0f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "colorBoost",
                    DisplayName = "Color Boost",
                    Description = "Saturation adjustment (1.0 = normal, >1 = more saturated, <1 = less saturated)",
                    MinValue = 0.0f,
                    MaxValue = 2.0f,
                    DefaultValue = 1.0f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "edgeSoftness",
                    DisplayName = "Edge Softness",
                    Description = "How soft the edge of the effect area appears",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.2f,
                    Step = 0.05f
                },
                new BoolParameter
                {
                    Key = "enableCurves",
                    DisplayName = "Enable RGB Curves",
                    Description = "Enable RGB curve color adjustment",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "curveStrength",
                    DisplayName = "Curve Strength",
                    Description = "Strength of the RGB curve adjustment",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 1.0f,
                    Step = 0.05f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new ColorBlindnessSettingsControl(effect);
    }
}
