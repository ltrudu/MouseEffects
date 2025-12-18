using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.PaintSplatter.UI;

namespace MouseEffects.Effects.PaintSplatter;

public sealed class PaintSplatterFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "paintsplatter",
        Name = "Paint Splatter",
        Description = "Artistic paint drops that splatter on clicks like Jackson Pollock",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new PaintSplatterEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        config.Set("ps_splatSize", 60f);
        config.Set("ps_dropletCount", 15);
        config.Set("ps_enableDrips", true);
        config.Set("ps_dripLength", 40f);
        config.Set("ps_opacity", 0.85f);
        config.Set("ps_lifetime", 5.0f);
        config.Set("ps_spreadRadius", 25f);
        config.Set("ps_colorMode", 0); // Single color
        config.Set("ps_paletteIndex", 0); // Primary palette
        config.Set("ps_singleColor", new Vector4(0.9f, 0.1f, 0.1f, 1f)); // Bright red
        config.Set("ps_clickEnabled", true);
        config.Set("ps_maxSplats", 50);
        config.Set("ps_edgeNoisiness", 0.3f);

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
                    Key = "ps_splatSize",
                    DisplayName = "Splat Size",
                    Description = "Size of the main paint splatter (pixels)",
                    MinValue = 20f,
                    MaxValue = 150f,
                    DefaultValue = 60f,
                    Step = 5f
                },
                new IntParameter
                {
                    Key = "ps_dropletCount",
                    DisplayName = "Droplet Count",
                    Description = "Number of small droplets around main splat",
                    MinValue = 5,
                    MaxValue = 30,
                    DefaultValue = 15
                },
                new BoolParameter
                {
                    Key = "ps_enableDrips",
                    DisplayName = "Enable Drips",
                    Description = "Show drip trails running down",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "ps_dripLength",
                    DisplayName = "Drip Length",
                    Description = "Length of drip trails (pixels)",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "ps_opacity",
                    DisplayName = "Opacity",
                    Description = "Overall transparency of paint splats",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.85f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "ps_lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long splats remain visible (seconds)",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 5.0f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "ps_spreadRadius",
                    DisplayName = "Spread Radius",
                    Description = "How far droplets spread from center (pixels)",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 25f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "ps_edgeNoisiness",
                    DisplayName = "Edge Noisiness",
                    Description = "Amount of irregularity in splat edges",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new IntParameter
                {
                    Key = "ps_colorMode",
                    DisplayName = "Color Mode",
                    Description = "0=Single, 1=Random, 2=Palette",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },
                new IntParameter
                {
                    Key = "ps_paletteIndex",
                    DisplayName = "Palette",
                    Description = "0=Primary, 1=Neon, 2=Earth, 3=Pastel",
                    MinValue = 0,
                    MaxValue = 3,
                    DefaultValue = 0
                },
                new BoolParameter
                {
                    Key = "ps_clickEnabled",
                    DisplayName = "Click to Splat",
                    Description = "Create splat on mouse click",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "ps_maxSplats",
                    DisplayName = "Max Splats",
                    Description = "Maximum number of splats on screen",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 50
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new PaintSplatterSettingsControl(effect);
}
