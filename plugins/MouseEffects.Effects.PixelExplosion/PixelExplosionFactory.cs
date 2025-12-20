using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.PixelExplosion.UI;

namespace MouseEffects.Effects.PixelExplosion;

public sealed class PixelExplosionFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "pixel-explosion",
        Name = "Pixel Explosion",
        Description = "Retro 8-bit style pixel explosions on clicks with gravity physics",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new PixelExplosionEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        config.Set("maxPixels", 5000);
        config.Set("pixelCountMin", 30);
        config.Set("pixelCountMax", 49);
        config.Set("pixelSizeMin", 3f);
        config.Set("pixelSizeMax", 8f);
        config.Set("explosionForce", 400f);
        config.Set("gravity", 250f);
        config.Set("lifetime", 2.0f);
        config.Set("colorPalette", 5); // Animated Rainbow
        config.Set("rainbowSpeed", 3.07f);
        config.Set("spawnOnLeftClick", true);
        config.Set("spawnOnRightClick", false);
        config.Set("spawnOnMouseMove", true);
        config.Set("mouseThreshold", 150f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new IntParameter
                {
                    Key = "maxPixels",
                    DisplayName = "Max Pixels",
                    Description = "Maximum number of pixels in the system",
                    MinValue = 100,
                    MaxValue = 10000,
                    DefaultValue = 5000
                },
                new IntParameter
                {
                    Key = "pixelCountMin",
                    DisplayName = "Min Pixels per Explosion",
                    Description = "Minimum number of pixels spawned per click",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 20
                },
                new IntParameter
                {
                    Key = "pixelCountMax",
                    DisplayName = "Max Pixels per Explosion",
                    Description = "Maximum number of pixels spawned per click",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 60
                },
                new FloatParameter
                {
                    Key = "pixelSizeMin",
                    DisplayName = "Min Pixel Size",
                    Description = "Minimum size of pixels in screen pixels",
                    MinValue = 2f,
                    MaxValue = 20f,
                    DefaultValue = 3f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "pixelSizeMax",
                    DisplayName = "Max Pixel Size",
                    Description = "Maximum size of pixels in screen pixels",
                    MinValue = 2f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "explosionForce",
                    DisplayName = "Explosion Force",
                    Description = "Initial velocity of pixels (pixels/sec)",
                    MinValue = 100f,
                    MaxValue = 1000f,
                    DefaultValue = 400f,
                    Step = 50f
                },
                new FloatParameter
                {
                    Key = "gravity",
                    DisplayName = "Gravity",
                    Description = "Downward acceleration (pixels/sec^2)",
                    MinValue = 0f,
                    MaxValue = 1000f,
                    DefaultValue = 250f,
                    Step = 25f
                },
                new FloatParameter
                {
                    Key = "lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long pixels live (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.0f,
                    Step = 0.5f
                },
                new IntParameter
                {
                    Key = "colorPalette",
                    DisplayName = "Color Palette",
                    Description = "0=Fire, 1=Ice, 2=Rainbow, 3=Retro, 4=Neon, 5=Animated Rainbow",
                    MinValue = 0,
                    MaxValue = 5,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling (for Animated Rainbow palette)",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "spawnOnLeftClick",
                    DisplayName = "Spawn on Left Click",
                    Description = "Create explosion when left mouse button is clicked",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "spawnOnRightClick",
                    DisplayName = "Spawn on Right Click",
                    Description = "Create explosion when right mouse button is clicked",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "spawnOnMouseMove",
                    DisplayName = "Spawn on Mouse Move",
                    Description = "Create explosions while moving the mouse",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "mouseThreshold",
                    DisplayName = "Mouse Threshold",
                    Description = "Distance in pixels before spawning explosion on mouse move",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 50f,
                    Step = 10f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new PixelExplosionSettingsControl(effect);
}
