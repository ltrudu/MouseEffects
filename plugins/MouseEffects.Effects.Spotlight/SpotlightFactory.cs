using MouseEffects.Core.Effects;
using MouseEffects.Effects.Spotlight.UI;

namespace MouseEffects.Effects.Spotlight;

public sealed class SpotlightFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "spotlight",
        Name = "Spotlight",
        Description = "Creates a dramatic theater spotlight effect centered on the mouse cursor, darkening everything outside the lit area",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new SpotlightEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Spotlight parameters
        config.Set("spotlightRadius", 200.0f);
        config.Set("edgeSoftness", 100.0f);
        config.Set("darknessLevel", 0.1f);
        config.Set("colorTemperature", 1); // 0=warm, 1=neutral, 2=cool
        config.Set("brightnessBoost", 1.2f);

        // Dust particles
        config.Set("dustParticlesEnabled", true);
        config.Set("dustDensity", 0.5f);

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
                    Key = "spotlightRadius",
                    DisplayName = "Spotlight Radius",
                    Description = "Radius of the spotlight in pixels",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 200f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "edgeSoftness",
                    DisplayName = "Edge Softness",
                    Description = "Softness of the spotlight edge",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "darknessLevel",
                    DisplayName = "Darkness Level",
                    Description = "How dark the area outside the spotlight should be (0=black, 1=full brightness)",
                    MinValue = 0.0f,
                    MaxValue = 0.8f,
                    DefaultValue = 0.1f,
                    Step = 0.05f
                },
                new IntParameter
                {
                    Key = "colorTemperature",
                    DisplayName = "Color Temperature",
                    Description = "Color temperature of the light (0=warm, 1=neutral, 2=cool)",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 1
                },
                new FloatParameter
                {
                    Key = "brightnessBoost",
                    DisplayName = "Brightness Boost",
                    Description = "Brightness multiplier inside the spotlight",
                    MinValue = 1.0f,
                    MaxValue = 2.0f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "dustParticlesEnabled",
                    DisplayName = "Dust Particles Enabled",
                    Description = "Show floating dust particles in the light beam",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "dustDensity",
                    DisplayName = "Dust Density",
                    Description = "Density of dust particles (0=none, 1=heavy)",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new SpotlightSettingsControl(effect);
}
