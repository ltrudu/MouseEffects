using MouseEffects.Core.Effects;
using MouseEffects.Effects.Kaleidoscope.UI;

namespace MouseEffects.Effects.Kaleidoscope;

public sealed class KaleidoscopeFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "kaleidoscope",
        Name = "Kaleidoscope",
        Description = "Creates real-time kaleidoscopic mirroring of the screen around the mouse cursor with radial symmetry",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new KaleidoscopeEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Kaleidoscope parameters
        config.Set("radius", 300.0f);
        config.Set("segments", 8);
        config.Set("rotationSpeed", 0.5f);
        config.Set("rotationOffset", 0.0f);
        config.Set("edgeSoftness", 0.2f);
        config.Set("zoomFactor", 1.0f);

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
                    Description = "Radius of the kaleidoscope effect in pixels",
                    MinValue = 100f,
                    MaxValue = 600f,
                    DefaultValue = 300f,
                    Step = 10f
                },
                new IntParameter
                {
                    Key = "segments",
                    DisplayName = "Segment Count",
                    Description = "Number of mirror segments (higher = more symmetry)",
                    MinValue = 4,
                    MaxValue = 16,
                    DefaultValue = 8
                },
                new FloatParameter
                {
                    Key = "rotationSpeed",
                    DisplayName = "Rotation Speed",
                    Description = "Speed of kaleidoscope rotation",
                    MinValue = 0.0f,
                    MaxValue = 2.0f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "rotationOffset",
                    DisplayName = "Rotation Offset",
                    Description = "Static rotation offset in radians",
                    MinValue = 0.0f,
                    MaxValue = 6.28f,
                    DefaultValue = 0.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "edgeSoftness",
                    DisplayName = "Edge Softness",
                    Description = "Softness of the effect edge blend",
                    MinValue = 0.0f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.2f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "zoomFactor",
                    DisplayName = "Zoom Factor",
                    Description = "Zoom level of the kaleidoscope (1.0 = normal)",
                    MinValue = 0.5f,
                    MaxValue = 2.0f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new KaleidoscopeSettingsControl(effect);
}
