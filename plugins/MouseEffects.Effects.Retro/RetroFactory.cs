using MouseEffects.Core.Effects;
using MouseEffects.Effects.Retro.UI;

namespace MouseEffects.Effects.Retro;

/// <summary>
/// Factory for creating RetroEffect instances.
/// </summary>
public sealed class RetroFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "retro",
        Name = "Retro",
        Description = "Retro scaling filters (xSaI, Super Eagle, etc.) for pixel art style effects with CRT post-processing",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new RetroEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Global settings
        config.Set("filterType", 0);        // XSaI

        // Layout settings (same as ASCIIZer)
        config.Set("layoutMode", 0);        // Fullscreen
        config.Set("radius", 200f);
        config.Set("rectWidth", 400f);
        config.Set("rectHeight", 300f);
        config.Set("edgeSoftness", 20f);

        // xSaI-specific settings (xs_ prefix)
        config.Set("xs_mode", 0);           // 0=Enhancement, 1=Pixelate+Scale, 2=Downscale+Upscale
        config.Set("xs_pixelSize", 4f);     // Pixelation cell size (for mode 0)
        config.Set("xs_scaleFactor", 4);    // Scale factor for mode 1 (2, 4, 8, 16)
        config.Set("xs_strength", 1.0f);    // Effect strength/blend

        // TV Filter settings (tv_ prefix)
        config.Set("tv_phosphorWidth", 0.7f);   // Width of oval phosphors (0.3-0.9)
        config.Set("tv_phosphorHeight", 0.85f); // Height of oval phosphors (0.5-1.0)
        config.Set("tv_phosphorGap", 0.05f);    // Gap between phosphors (0.0-0.2)
        config.Set("tv_brightness", 2.0f);      // Phosphor brightness boost (1.0-3.0)

        // Toon Filter settings (toon_ prefix)
        config.Set("toon_edgeThreshold", 0.1f); // Edge detection sensitivity (0.01-0.5)
        config.Set("toon_edgeWidth", 1.5f);     // Outline thickness in pixels (1-5)
        config.Set("toon_colorLevels", 6f);     // Color quantization levels (2-16)
        config.Set("toon_saturation", 1.2f);    // Color saturation boost (0.5-2.0)

        // Shared Post-Effects
        config.Set("scanlines", false);
        config.Set("scanlineIntensity", 0.3f);
        config.Set("scanlineSpacing", 2);
        config.Set("crtCurvature", false);
        config.Set("crtAmount", 0.1f);
        config.Set("phosphorGlow", false);
        config.Set("phosphorIntensity", 0.5f);
        config.Set("chromatic", false);
        config.Set("chromaticOffset", 1f);
        config.Set("vignette", false);
        config.Set("vignetteIntensity", 0.3f);
        config.Set("vignetteRadius", 0.8f);
        config.Set("noise", false);
        config.Set("noiseAmount", 0.1f);
        config.Set("flicker", false);
        config.Set("flickerSpeed", 1f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Global
                new IntParameter
                {
                    Key = "filterType",
                    DisplayName = "Filter Type",
                    Description = "Retro scaling filter style",
                    MinValue = 0,
                    MaxValue = 2, // 0=XSaI, 1=TVFilter, 2=ToonFilter
                    DefaultValue = 0
                },

                // Layout
                new IntParameter
                {
                    Key = "layoutMode",
                    DisplayName = "Layout Mode",
                    Description = "Effect area: Fullscreen, Circle, or Rectangle",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "radius",
                    DisplayName = "Radius",
                    Description = "Circle radius in pixels",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "rectWidth",
                    DisplayName = "Rectangle Width",
                    Description = "Rectangle width in pixels",
                    MinValue = 100f,
                    MaxValue = 800f,
                    DefaultValue = 400f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "rectHeight",
                    DisplayName = "Rectangle Height",
                    Description = "Rectangle height in pixels",
                    MinValue = 100f,
                    MaxValue = 600f,
                    DefaultValue = 300f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "edgeSoftness",
                    DisplayName = "Edge Softness",
                    Description = "Edge blend for circle/rectangle modes",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },

                // xSaI-specific
                new IntParameter
                {
                    Key = "xs_mode",
                    DisplayName = "Scaling Mode",
                    Description = "0=Enhancement, 1=Pixelate+Scale, 2=Downscale+Upscale",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "xs_pixelSize",
                    DisplayName = "Pixel Size",
                    Description = "Size of pixelation cells",
                    MinValue = 2f,
                    MaxValue = 16f,
                    DefaultValue = 4f,
                    Step = 1f
                },
                new IntParameter
                {
                    Key = "xs_scaleFactor",
                    DisplayName = "Scale Factor",
                    Description = "Resolution divisor for Downscale+Upscale mode (2, 4, 8, 16)",
                    MinValue = 2,
                    MaxValue = 16,
                    DefaultValue = 4
                },
                new FloatParameter
                {
                    Key = "xs_strength",
                    DisplayName = "Effect Strength",
                    Description = "Blend strength of the effect",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 1f,
                    Step = 0.05f
                },

                // Post-Effects
                new BoolParameter
                {
                    Key = "scanlines",
                    DisplayName = "Scanlines",
                    Description = "Enable CRT scanline effect",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "scanlineIntensity",
                    DisplayName = "Scanline Intensity",
                    Description = "Scanline darkness",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new BoolParameter
                {
                    Key = "crtCurvature",
                    DisplayName = "CRT Curvature",
                    Description = "Enable CRT screen curve effect",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "crtAmount",
                    DisplayName = "Curvature Amount",
                    Description = "CRT curvature intensity",
                    MinValue = 0f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.1f,
                    Step = 0.01f
                },
                new BoolParameter
                {
                    Key = "vignette",
                    DisplayName = "Vignette",
                    Description = "Enable vignette darkening at edges",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "vignetteIntensity",
                    DisplayName = "Vignette Intensity",
                    Description = "Vignette darkness",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new RetroSettingsControl(effect);
    }
}
