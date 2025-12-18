using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Aurora.UI;

namespace MouseEffects.Effects.Aurora;

public sealed class AuroraFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "aurora",
        Name = "Aurora",
        Description = "Beautiful northern lights ribbons following the mouse cursor with flowing colors and organic motion",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new AuroraEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Aurora appearance (au_ prefix for Aurora)
        config.Set("au_height", 400f);
        config.Set("au_horizontalSpread", 300f);
        config.Set("au_waveSpeed", 1.0f);
        config.Set("au_waveFrequency", 2.0f);
        config.Set("au_numLayers", 3);
        config.Set("au_colorIntensity", 1.5f);
        config.Set("au_glowStrength", 2.0f);

        // Aurora colors - classic northern lights
        config.Set("au_primaryColor", new Vector4(0f, 1f, 0.5f, 1f));  // Green #00FF7F
        config.Set("au_secondaryColor", new Vector4(0f, 1f, 1f, 1f));   // Cyan #00FFFF
        config.Set("au_tertiaryColor", new Vector4(0.545f, 0f, 1f, 1f)); // Purple #8B00FF
        config.Set("au_accentColor", new Vector4(1f, 0.078f, 0.576f, 1f)); // Pink #FF1493

        // Animation
        config.Set("au_noiseScale", 1.5f);
        config.Set("au_noiseStrength", 0.3f);
        config.Set("au_verticalFlow", 0.5f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Aurora Appearance
                new FloatParameter
                {
                    Key = "au_height",
                    DisplayName = "Aurora Height",
                    Description = "How tall the aurora ribbons extend vertically",
                    MinValue = 100f,
                    MaxValue = 800f,
                    DefaultValue = 400f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "au_horizontalSpread",
                    DisplayName = "Horizontal Spread",
                    Description = "Width of the aurora effect horizontally",
                    MinValue = 100f,
                    MaxValue = 600f,
                    DefaultValue = 300f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "au_waveSpeed",
                    DisplayName = "Wave Speed",
                    Description = "Speed of aurora wave animation",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "au_waveFrequency",
                    DisplayName = "Wave Frequency",
                    Description = "Frequency of vertical waves",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new IntParameter
                {
                    Key = "au_numLayers",
                    DisplayName = "Number of Layers",
                    Description = "Number of overlapping aurora layers",
                    MinValue = 1,
                    MaxValue = 5,
                    DefaultValue = 3
                },
                new FloatParameter
                {
                    Key = "au_colorIntensity",
                    DisplayName = "Color Intensity",
                    Description = "Brightness of aurora colors",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "au_glowStrength",
                    DisplayName = "Glow Strength",
                    Description = "Strength of the ethereal glow effect",
                    MinValue = 0.5f,
                    MaxValue = 4f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },

                // Aurora Colors
                new ColorParameter
                {
                    Key = "au_primaryColor",
                    DisplayName = "Primary Color",
                    Description = "Main aurora color (typically green)",
                    DefaultValue = new Vector4(0f, 1f, 0.5f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "au_secondaryColor",
                    DisplayName = "Secondary Color",
                    Description = "Second aurora color (typically cyan)",
                    DefaultValue = new Vector4(0f, 1f, 1f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "au_tertiaryColor",
                    DisplayName = "Tertiary Color",
                    Description = "Third aurora color (typically purple)",
                    DefaultValue = new Vector4(0.545f, 0f, 1f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "au_accentColor",
                    DisplayName = "Accent Color",
                    Description = "Accent color for highlights (typically pink)",
                    DefaultValue = new Vector4(1f, 0.078f, 0.576f, 1f),
                    SupportsAlpha = false
                },

                // Animation
                new FloatParameter
                {
                    Key = "au_noiseScale",
                    DisplayName = "Noise Scale",
                    Description = "Scale of organic noise distortion",
                    MinValue = 0.5f,
                    MaxValue = 4f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "au_noiseStrength",
                    DisplayName = "Noise Strength",
                    Description = "Strength of organic movement distortion",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "au_verticalFlow",
                    DisplayName = "Vertical Flow",
                    Description = "Speed of vertical flowing movement",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new AuroraSettingsControl(effect);
}
