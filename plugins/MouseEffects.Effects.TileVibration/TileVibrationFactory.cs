using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.TileVibration.UI;

namespace MouseEffects.Effects.TileVibration;

/// <summary>
/// Factory for creating TileVibrationEffect instances.
/// </summary>
public sealed class TileVibrationFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "tile-vibration",
        Name = "Tile Vibration",
        Description = "Creates vibrating screen tiles that follow the mouse cursor and shrink over time",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new TileVibrationEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        config.Set("tileLifespan", 2.0f);
        config.Set("maxWidth", 100f);
        config.Set("maxHeight", 100f);
        config.Set("minWidth", 20f);
        config.Set("minHeight", 20f);
        config.Set("syncWidthHeight", true);
        config.Set("edgeStyle", 0); // Sharp
        config.Set("vibrationSpeed", 1.0f);
        config.Set("displacementEnabled", true);
        config.Set("displacementMax", 10f);
        config.Set("zoomEnabled", false);
        config.Set("zoomMin", 0.8f);
        config.Set("zoomMax", 1.2f);
        config.Set("rotationEnabled", false);
        config.Set("rotationAmplitude", 15f);
        config.Set("outlineEnabled", false);
        config.Set("outlineColor", new Vector4(1f, 1f, 1f, 1f));
        config.Set("outlineSize", 2f);
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
                    Key = "tileLifespan",
                    DisplayName = "Tile Lifespan",
                    Description = "Seconds before tile disappears",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "maxWidth",
                    DisplayName = "Max Width",
                    Description = "Width when tile is created (pixels)",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "maxHeight",
                    DisplayName = "Max Height",
                    Description = "Height when tile is created (pixels)",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "minWidth",
                    DisplayName = "Min Width",
                    Description = "Width at end of lifespan (pixels)",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "minHeight",
                    DisplayName = "Min Height",
                    Description = "Height at end of lifespan (pixels)",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },
                new BoolParameter
                {
                    Key = "syncWidthHeight",
                    DisplayName = "Sync Width/Height",
                    Description = "Keep tiles square",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "edgeStyle",
                    DisplayName = "Edge Style",
                    Description = "Sharp (0) or Soft (1) tile edges",
                    MinValue = 0,
                    MaxValue = 1,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "vibrationSpeed",
                    DisplayName = "Vibration Speed",
                    Description = "Speed multiplier for all vibrations",
                    MinValue = 0.1f,
                    MaxValue = 5.0f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "displacementEnabled",
                    DisplayName = "Displacement",
                    Description = "Enable random movement from center",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "displacementMax",
                    DisplayName = "Max Displacement",
                    Description = "Maximum pixels of displacement",
                    MinValue = 1f,
                    MaxValue = 50f,
                    DefaultValue = 10f,
                    Step = 1f
                },
                new BoolParameter
                {
                    Key = "zoomEnabled",
                    DisplayName = "Zoom",
                    Description = "Enable zoom in/out effect",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "zoomMin",
                    DisplayName = "Min Zoom",
                    Description = "Minimum zoom scale",
                    MinValue = 0.5f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.8f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "zoomMax",
                    DisplayName = "Max Zoom",
                    Description = "Maximum zoom scale",
                    MinValue = 1.0f,
                    MaxValue = 2.0f,
                    DefaultValue = 1.2f,
                    Step = 0.05f
                },
                new BoolParameter
                {
                    Key = "rotationEnabled",
                    DisplayName = "Rotation",
                    Description = "Enable rotational vibration",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "rotationAmplitude",
                    DisplayName = "Rotation Amplitude",
                    Description = "Maximum rotation in degrees",
                    MinValue = 1f,
                    MaxValue = 45f,
                    DefaultValue = 15f,
                    Step = 1f
                },
                new BoolParameter
                {
                    Key = "outlineEnabled",
                    DisplayName = "Outline",
                    Description = "Enable tile outline",
                    DefaultValue = false
                },
                new ColorParameter
                {
                    Key = "outlineColor",
                    DisplayName = "Outline Color",
                    Description = "Color of the tile outline",
                    DefaultValue = new Vector4(1f, 1f, 1f, 1f),
                    SupportsAlpha = true
                },
                new FloatParameter
                {
                    Key = "outlineSize",
                    DisplayName = "Outline Size",
                    Description = "Thickness of the outline in pixels",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 2f,
                    Step = 1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new TileVibrationSettingsControl(effect);
    }
}
