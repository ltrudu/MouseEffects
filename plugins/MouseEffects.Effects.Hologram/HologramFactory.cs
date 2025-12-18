using MouseEffects.Core.Effects;
using MouseEffects.Effects.Hologram.UI;

namespace MouseEffects.Effects.Hologram;

public sealed class HologramFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "hologram",
        Name = "Hologram",
        Description = "Sci-fi holographic projection effect with scan lines and flickering around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new HologramEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Effect parameters
        config.Set("radius", 250.0f);                   // 100-500 pixels
        config.Set("scanLineDensity", 150.0f);          // 50-300
        config.Set("scanLineSpeed", 2.0f);              // 0.5-5.0
        config.Set("flickerIntensity", 0.15f);          // 0.0-0.5
        config.Set("colorTint", 0);                     // 0=Cyan, 1=Blue, 2=Green, 3=Purple
        config.Set("edgeGlowStrength", 0.8f);           // 0.0-2.0
        config.Set("noiseAmount", 0.2f);                // 0.0-1.0
        config.Set("chromaticAberration", 0.008f);      // 0.0-0.03
        config.Set("tintStrength", 0.6f);               // 0.0-1.0

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
                    Description = "Radius of the hologram effect around the cursor in pixels",
                    MinValue = 100f,
                    MaxValue = 500f,
                    DefaultValue = 250f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "scanLineDensity",
                    DisplayName = "Scan Line Density",
                    Description = "Density of horizontal holographic scan lines",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 150f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "scanLineSpeed",
                    DisplayName = "Scan Line Speed",
                    Description = "Speed of scan line movement",
                    MinValue = 0.5f,
                    MaxValue = 5.0f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "flickerIntensity",
                    DisplayName = "Flicker Intensity",
                    Description = "Intensity of holographic brightness flickering",
                    MinValue = 0.0f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.15f,
                    Step = 0.05f
                },
                new ChoiceParameter
                {
                    Key = "colorTint",
                    DisplayName = "Color Tint",
                    Description = "Holographic color theme",
                    Choices = ["Cyan", "Blue", "Green", "Purple"],
                    DefaultValue = "Cyan"
                },
                new FloatParameter
                {
                    Key = "tintStrength",
                    DisplayName = "Tint Strength",
                    Description = "Strength of color tinting applied to hologram",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.6f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "edgeGlowStrength",
                    DisplayName = "Edge Glow Strength",
                    Description = "Strength of glowing edge effect",
                    MinValue = 0.0f,
                    MaxValue = 2.0f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "noiseAmount",
                    DisplayName = "Noise Amount",
                    Description = "Amount of static/noise overlay",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.2f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "chromaticAberration",
                    DisplayName = "Chromatic Aberration",
                    Description = "Amount of RGB channel separation for holographic effect",
                    MinValue = 0.0f,
                    MaxValue = 0.03f,
                    DefaultValue = 0.008f,
                    Step = 0.001f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new HologramSettingsControl(effect);
}
