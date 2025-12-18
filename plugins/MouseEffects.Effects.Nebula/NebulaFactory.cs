using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Nebula.UI;

namespace MouseEffects.Effects.Nebula;

public sealed class NebulaFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "nebula",
        Name = "Nebula",
        Description = "Colorful cosmic gas clouds trailing the mouse cursor with volumetric feel and twinkling stars",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new NebulaEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Nebula appearance (nb_ prefix for Nebula)
        config.Set("nb_cloudDensity", 0.7f);
        config.Set("nb_swirlSpeed", 0.5f);
        config.Set("nb_layerCount", 4);
        config.Set("nb_glowIntensity", 1.5f);
        config.Set("nb_starDensity", 0.3f);
        config.Set("nb_effectRadius", 400f);
        config.Set("nb_noiseScale", 1.2f);
        config.Set("nb_colorVariation", 0.5f);
        config.Set("nb_cloudSpeed", 0.3f);

        // Color palette (0=Orion, 1=Carina, 2=Eagle, 3=Custom)
        config.Set("nb_colorPalette", 0);

        // Custom colors (Purple, Blue, Pink)
        config.Set("nb_customColor1", new Vector4(0.545f, 0f, 0.545f, 1f));
        config.Set("nb_customColor2", new Vector4(0.25f, 0.41f, 0.88f, 1f));
        config.Set("nb_customColor3", new Vector4(1f, 0.41f, 0.71f, 1f));

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Cloud Properties
                new FloatParameter
                {
                    Key = "nb_cloudDensity",
                    DisplayName = "Cloud Density",
                    Description = "How dense and opaque the nebula clouds appear",
                    MinValue = 0.1f,
                    MaxValue = 1.5f,
                    DefaultValue = 0.7f,
                    Step = 0.05f
                },
                new IntParameter
                {
                    Key = "nb_layerCount",
                    DisplayName = "Number of Layers",
                    Description = "Number of overlapping cloud layers for depth",
                    MinValue = 2,
                    MaxValue = 5,
                    DefaultValue = 4
                },
                new FloatParameter
                {
                    Key = "nb_effectRadius",
                    DisplayName = "Effect Radius",
                    Description = "Size of the nebula cloud area around cursor",
                    MinValue = 200f,
                    MaxValue = 800f,
                    DefaultValue = 400f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "nb_noiseScale",
                    DisplayName = "Noise Scale",
                    Description = "Scale of the cloud turbulence patterns",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },

                // Animation
                new FloatParameter
                {
                    Key = "nb_swirlSpeed",
                    DisplayName = "Swirl Speed",
                    Description = "Speed of the swirling cloud motion",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "nb_cloudSpeed",
                    DisplayName = "Cloud Drift Speed",
                    Description = "Speed of cloud drifting movement",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },

                // Visual Effects
                new FloatParameter
                {
                    Key = "nb_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Strength of the ethereal glow effect",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "nb_starDensity",
                    DisplayName = "Star Density",
                    Description = "Amount of twinkling stars within the nebula",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "nb_colorVariation",
                    DisplayName = "Color Variation",
                    Description = "How much colors vary across the nebula",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.5f,
                    Step = 0.05f
                },

                // Color Palette
                new IntParameter
                {
                    Key = "nb_colorPalette",
                    DisplayName = "Color Palette",
                    Description = "Nebula color scheme (0=Orion, 1=Carina, 2=Eagle, 3=Custom)",
                    MinValue = 0,
                    MaxValue = 3,
                    DefaultValue = 0
                },

                // Custom Colors
                new ColorParameter
                {
                    Key = "nb_customColor1",
                    DisplayName = "Custom Color 1",
                    Description = "First custom nebula color",
                    DefaultValue = new Vector4(0.545f, 0f, 0.545f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "nb_customColor2",
                    DisplayName = "Custom Color 2",
                    Description = "Second custom nebula color",
                    DefaultValue = new Vector4(0.25f, 0.41f, 0.88f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "nb_customColor3",
                    DisplayName = "Custom Color 3",
                    Description = "Third custom nebula color",
                    DefaultValue = new Vector4(1f, 0.41f, 0.71f, 1f),
                    SupportsAlpha = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new NebulaSettingsControl(effect);
}
