using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.BlackHole.UI;

namespace MouseEffects.Effects.BlackHole;

public sealed class BlackHoleFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "black-hole",
        Name = "Black Hole",
        Description = "Creates gravitational lensing distortion around the mouse cursor, warping the screen like a real black hole",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new BlackHoleEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Black hole parameters
        config.Set("radius", 200.0f);
        config.Set("distortionStrength", 1.0f);
        config.Set("eventHorizonSize", 0.3f);

        // Accretion disk
        config.Set("accretionDiskEnabled", true);
        config.Set("accretionDiskColor", new Vector4(1.0f, 0.6f, 0.2f, 1.0f));
        config.Set("rotationSpeed", 0.5f);
        config.Set("glowIntensity", 1.0f);

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
                    Description = "Radius of the black hole effect in pixels",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "distortionStrength",
                    DisplayName = "Distortion Strength",
                    Description = "Strength of the gravitational lensing distortion",
                    MinValue = 0.1f,
                    MaxValue = 2.0f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "eventHorizonSize",
                    DisplayName = "Event Horizon Size",
                    Description = "Size of the dark center as percentage of radius (0-1)",
                    MinValue = 0.0f,
                    MaxValue = 0.8f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new BoolParameter
                {
                    Key = "accretionDiskEnabled",
                    DisplayName = "Accretion Disk Enabled",
                    Description = "Show the glowing accretion disk around the black hole",
                    DefaultValue = true
                },
                new ColorParameter
                {
                    Key = "accretionDiskColor",
                    DisplayName = "Accretion Disk Color",
                    Description = "Color of the accretion disk glow",
                    DefaultValue = new Vector4(1.0f, 0.6f, 0.2f, 1.0f),
                    SupportsAlpha = false
                },
                new FloatParameter
                {
                    Key = "rotationSpeed",
                    DisplayName = "Rotation Speed",
                    Description = "Speed of accretion disk rotation",
                    MinValue = 0.0f,
                    MaxValue = 2.0f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of the accretion disk glow",
                    MinValue = 0.0f,
                    MaxValue = 3.0f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new BlackHoleSettingsControl(effect);
}
