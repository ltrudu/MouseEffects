using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.ASCIIZer.UI;

namespace MouseEffects.Effects.ASCIIZer;

/// <summary>
/// Factory for creating ASCIIZerEffect instances.
/// </summary>
public sealed class ASCIIZerFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "asciizer",
        Name = "ASCIIZer",
        Description = "Renders the screen as ASCII art with multiple filter styles and extensive customization options",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.VisualFilter
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new ASCIIZerEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Global settings
        config.Set("filterType", 0);        // ASCIIClassic
        config.Set("advancedMode", false);

        // Basic settings
        config.Set("layoutMode", 0);        // Fullscreen
        config.Set("radius", 200f);
        config.Set("rectWidth", 400f);
        config.Set("rectHeight", 300f);
        config.Set("cellWidth", 8f);
        config.Set("cellHeight", 16f);
        config.Set("charsetPreset", 0);     // Standard
        config.Set("colorMode", 0);         // Colored
        config.Set("foreground", new Vector4(0f, 1f, 0f, 1f));   // Green
        config.Set("background", new Vector4(0f, 0f, 0f, 1f));   // Black

        // Advanced: Character settings
        config.Set("customCharset", "");
        config.Set("fontFamily", 0);        // Consolas
        config.Set("fontWeight", 0);        // Normal
        config.Set("charSpacing", 0f);
        config.Set("lineSpacing", 0f);
        config.Set("aspectCorrection", true);

        // Advanced: Color settings
        config.Set("paletteType", 0);       // None
        config.Set("saturation", 1.0f);
        config.Set("quantizeLevels", 256);
        config.Set("preserveLuminance", false);

        // Advanced: Brightness & Contrast
        config.Set("brightness", 0f);
        config.Set("contrast", 1.0f);
        config.Set("gamma", 1.0f);
        config.Set("invert", false);
        config.Set("autoContrast", false);

        // Advanced: Cell Sampling
        config.Set("lockAspect", true);
        config.Set("sampleMode", 0);        // Center
        config.Set("sampleRadius", 0.5f);

        // Advanced: Visual Effects
        config.Set("scanlines", false);
        config.Set("scanlineIntensity", 0.3f);
        config.Set("scanlineSpacing", 2);
        config.Set("crtCurvature", false);
        config.Set("crtAmount", 0.1f);
        config.Set("phosphorGlow", false);
        config.Set("phosphorRadius", 2f);
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

        // Advanced: Character Rendering
        config.Set("antialiasing", 0);      // None
        config.Set("charShadow", false);
        config.Set("shadowOffset", new Vector2(1f, 1f));
        config.Set("shadowColor", new Vector4(0f, 0f, 0f, 0.5f));
        config.Set("charOutline", false);
        config.Set("outlineThickness", 1f);
        config.Set("outlineColor", new Vector4(0f, 0f, 0f, 1f));
        config.Set("glowOnBright", false);
        config.Set("glowThreshold", 0.8f);
        config.Set("glowRadius", 3f);
        config.Set("gridLines", false);
        config.Set("gridThickness", 1f);
        config.Set("gridColor", new Vector4(0.2f, 0.2f, 0.2f, 1f));

        // Advanced: Edge & Shape
        config.Set("edgeSoftness", 20f);
        config.Set("shapeFeather", 0f);
        config.Set("innerGlow", false);
        config.Set("innerGlowColor", new Vector4(1f, 1f, 1f, 0.3f));
        config.Set("innerGlowSize", 10f);

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
                    Description = "ASCII art filter style",
                    MinValue = 0,
                    MaxValue = 5,
                    DefaultValue = 0
                },
                new BoolParameter
                {
                    Key = "advancedMode",
                    DisplayName = "Advanced Mode",
                    Description = "Show advanced settings",
                    DefaultValue = false
                },

                // Basic
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
                    Key = "cellWidth",
                    DisplayName = "Cell Width",
                    Description = "Character cell width in pixels",
                    MinValue = 4f,
                    MaxValue = 32f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "cellHeight",
                    DisplayName = "Cell Height",
                    Description = "Character cell height in pixels",
                    MinValue = 8f,
                    MaxValue = 48f,
                    DefaultValue = 16f,
                    Step = 1f
                },
                new IntParameter
                {
                    Key = "charsetPreset",
                    DisplayName = "Character Set",
                    Description = "Predefined character set",
                    MinValue = 0,
                    MaxValue = 4,
                    DefaultValue = 0
                },
                new IntParameter
                {
                    Key = "colorMode",
                    DisplayName = "Color Mode",
                    Description = "Colored, Monochrome, or Palette",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },
                new ColorParameter
                {
                    Key = "foreground",
                    DisplayName = "Foreground Color",
                    Description = "Monochrome text color",
                    DefaultValue = new Vector4(0f, 1f, 0f, 1f),
                    SupportsAlpha = true
                },
                new ColorParameter
                {
                    Key = "background",
                    DisplayName = "Background Color",
                    Description = "Monochrome background color",
                    DefaultValue = new Vector4(0f, 0f, 0f, 1f),
                    SupportsAlpha = true
                },

                // Advanced: Brightness
                new FloatParameter
                {
                    Key = "brightness",
                    DisplayName = "Brightness",
                    Description = "Brightness adjustment",
                    MinValue = -1f,
                    MaxValue = 1f,
                    DefaultValue = 0f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "contrast",
                    DisplayName = "Contrast",
                    Description = "Contrast multiplier",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "gamma",
                    DisplayName = "Gamma",
                    Description = "Gamma correction",
                    MinValue = 0.5f,
                    MaxValue = 2.5f,
                    DefaultValue = 1f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "invert",
                    DisplayName = "Invert",
                    Description = "Invert brightness mapping",
                    DefaultValue = false
                },

                // Advanced: Visual Effects
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
                },

                // Edge & Shape
                new FloatParameter
                {
                    Key = "edgeSoftness",
                    DisplayName = "Edge Softness",
                    Description = "Edge blend for circle/rectangle modes",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new ASCIIZerSettingsControl(effect);
    }
}
