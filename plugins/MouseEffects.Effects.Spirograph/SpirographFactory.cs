using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Spirograph.UI;

namespace MouseEffects.Effects.Spirograph;

public sealed class SpirographFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "spirograph",
        Name = "Spirograph",
        Description = "Beautiful spirograph-like geometric patterns following the mouse cursor with intricate mathematical curves",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new SpirographEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Spirograph Parameters (sp_ prefix for Spirograph)
        config.Set("sp_innerRadius", 50f);
        config.Set("sp_outerRadius", 120f);
        config.Set("sp_penOffset", 80f);
        config.Set("sp_rotationSpeed", 1.0f);
        config.Set("sp_lineThickness", 2.0f);
        config.Set("sp_glowIntensity", 1.5f);
        config.Set("sp_numPetals", 12);
        config.Set("sp_trailFadeSpeed", 0.5f);
        config.Set("sp_colorCycleSpeed", 1.0f);
        config.Set("sp_colorMode", 0); // 0=rainbow, 1=fixed, 2=gradient

        // Colors
        config.Set("sp_primaryColor", new Vector4(1f, 0f, 0.5f, 1f));     // Pink
        config.Set("sp_secondaryColor", new Vector4(0f, 0.5f, 1f, 1f));   // Cyan
        config.Set("sp_tertiaryColor", new Vector4(0.5f, 1f, 0f, 1f));    // Green

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Spirograph Shape
                new FloatParameter
                {
                    Key = "sp_innerRadius",
                    DisplayName = "Inner Radius",
                    Description = "Radius of the inner rolling circle",
                    MinValue = 20f,
                    MaxValue = 150f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "sp_outerRadius",
                    DisplayName = "Outer Radius",
                    Description = "Radius of the outer fixed circle",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 120f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "sp_penOffset",
                    DisplayName = "Pen Offset",
                    Description = "Distance from center of rolling circle to the drawing point",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 80f,
                    Step = 5f
                },
                new IntParameter
                {
                    Key = "sp_numPetals",
                    DisplayName = "Number of Petals",
                    Description = "Number of petals/loops in the pattern",
                    MinValue = 3,
                    MaxValue = 24,
                    DefaultValue = 12
                },

                // Animation
                new FloatParameter
                {
                    Key = "sp_rotationSpeed",
                    DisplayName = "Rotation Speed",
                    Description = "Speed of pattern rotation and drawing",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sp_trailFadeSpeed",
                    DisplayName = "Trail Fade Speed",
                    Description = "How quickly the trail fades away",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Appearance
                new FloatParameter
                {
                    Key = "sp_lineThickness",
                    DisplayName = "Line Thickness",
                    Description = "Thickness of the spirograph curves",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "sp_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the glow effect around lines",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Color Settings
                new IntParameter
                {
                    Key = "sp_colorMode",
                    DisplayName = "Color Mode",
                    Description = "Color pattern: 0=Rainbow Cycle, 1=Fixed Color, 2=Gradient",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "sp_colorCycleSpeed",
                    DisplayName = "Color Cycle Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new ColorParameter
                {
                    Key = "sp_primaryColor",
                    DisplayName = "Primary Color",
                    Description = "Main color for fixed/gradient modes",
                    DefaultValue = new Vector4(1f, 0f, 0.5f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "sp_secondaryColor",
                    DisplayName = "Secondary Color",
                    Description = "Second color for gradient mode",
                    DefaultValue = new Vector4(0f, 0.5f, 1f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "sp_tertiaryColor",
                    DisplayName = "Tertiary Color",
                    Description = "Third color for gradient mode",
                    DefaultValue = new Vector4(0.5f, 1f, 0f, 1f),
                    SupportsAlpha = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new SpirographSettingsControl(effect);
}
