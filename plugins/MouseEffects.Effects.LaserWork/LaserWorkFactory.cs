using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.LaserWork.UI;

namespace MouseEffects.Effects.LaserWork;

/// <summary>
/// Factory for creating LaserWork effect instances.
/// </summary>
public sealed class LaserWorkFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "laser-work",
        Name = "Laser Work",
        Description = "Shoots glowing lasers from the mouse pointer that bounce off screen edges",
        Version = new Version(1, 0, 0),
        Author = "MouseEffects",
        Category = EffectCategory.Light
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new LaserWorkEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Emission rate
        config.Set("lasersPerSecond", 20f);

        // Size settings (random between min and max)
        config.Set("minLaserLength", 80.47f);
        config.Set("maxLaserLength", 153.23f);
        config.Set("minLaserWidth", 2f);
        config.Set("maxLaserWidth", 6f);
        config.Set("autoShrink", true);

        // Physics settings
        config.Set("laserSpeed", 400f);
        config.Set("laserLifespan", 7.44f);

        // Alpha settings
        config.Set("minAlpha", 0.1f);
        config.Set("maxAlpha", 1.0f);

        // Visual settings
        config.Set("glowIntensity", 0.5f);
        config.Set("laserColor", new Vector4(1f, 0.2f, 0.2f, 1f)); // Red by default

        // Rainbow settings
        config.Set("rainbowMode", true);
        config.Set("rainbowSpeed", 1f);

        // Direction toggles
        config.Set("shootForward", true);
        config.Set("shootBackward", true);
        config.Set("shootLeft", true);
        config.Set("shootRight", true);

        // Collision explosion settings
        config.Set("enableCollisionExplosion", true);
        config.Set("explosionLaserCount", 8f);
        config.Set("explosionLifespanMultiplier", 0.5f);
        config.Set("explosionLasersCanCollide", false);
        config.Set("maxCollisionCount", 3f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Emission rate
                new FloatParameter
                {
                    Key = "lasersPerSecond",
                    DisplayName = "Lasers Per Second",
                    Description = "How many lasers are shot per second when the mouse is moved",
                    MinValue = 1f,
                    MaxValue = 100f,
                    DefaultValue = 20f
                },

                // Size parameters
                new FloatParameter
                {
                    Key = "minLaserLength",
                    DisplayName = "Min Laser Length",
                    Description = "Minimum length of laser beams in pixels",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 30f
                },
                new FloatParameter
                {
                    Key = "maxLaserLength",
                    DisplayName = "Max Laser Length",
                    Description = "Maximum length of laser beams in pixels",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 70f
                },
                new FloatParameter
                {
                    Key = "minLaserWidth",
                    DisplayName = "Min Laser Width",
                    Description = "Minimum width of laser beams in pixels",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 2f
                },
                new FloatParameter
                {
                    Key = "maxLaserWidth",
                    DisplayName = "Max Laser Width",
                    Description = "Maximum width of laser beams in pixels",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 6f
                },
                new BoolParameter
                {
                    Key = "autoShrink",
                    DisplayName = "Auto Shrink",
                    Description = "Lasers shrink from original size to 1 pixel over their lifespan"
                },

                // Physics parameters
                new FloatParameter
                {
                    Key = "laserSpeed",
                    DisplayName = "Speed",
                    Description = "Speed of laser movement in pixels per second",
                    MinValue = 50f,
                    MaxValue = 1000f,
                    DefaultValue = 400f
                },
                new FloatParameter
                {
                    Key = "laserLifespan",
                    DisplayName = "Lifespan",
                    Description = "How long lasers live in seconds",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 3f
                },

                // Alpha parameters
                new FloatParameter
                {
                    Key = "minAlpha",
                    DisplayName = "Min Alpha",
                    Description = "Minimum opacity at end of lifespan",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.1f
                },
                new FloatParameter
                {
                    Key = "maxAlpha",
                    DisplayName = "Max Alpha",
                    Description = "Maximum opacity at start of lifespan",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 1f
                },

                // Glow
                new FloatParameter
                {
                    Key = "glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the laser glow effect",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f
                },

                // Color
                new ColorParameter
                {
                    Key = "laserColor",
                    DisplayName = "Laser Color",
                    Description = "Color of the laser beams"
                },

                // Rainbow
                new BoolParameter
                {
                    Key = "rainbowMode",
                    DisplayName = "Rainbow Mode",
                    Description = "Cycle through rainbow colors"
                },
                new FloatParameter
                {
                    Key = "rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1f
                },

                // Directions
                new BoolParameter
                {
                    Key = "shootForward",
                    DisplayName = "Shoot Forward",
                    Description = "Shoot lasers in movement direction"
                },
                new BoolParameter
                {
                    Key = "shootBackward",
                    DisplayName = "Shoot Backward",
                    Description = "Shoot lasers opposite to movement direction"
                },
                new BoolParameter
                {
                    Key = "shootLeft",
                    DisplayName = "Shoot Left",
                    Description = "Shoot lasers perpendicular left"
                },
                new BoolParameter
                {
                    Key = "shootRight",
                    DisplayName = "Shoot Right",
                    Description = "Shoot lasers perpendicular right"
                },

                // Collision explosion
                new BoolParameter
                {
                    Key = "enableCollisionExplosion",
                    DisplayName = "Enable Collision Explosion",
                    Description = "Lasers explode when they collide with each other"
                },
                new FloatParameter
                {
                    Key = "explosionLaserCount",
                    DisplayName = "Explosion Laser Count",
                    Description = "Number of lasers spawned in explosion",
                    MinValue = 2f,
                    MaxValue = 24f,
                    DefaultValue = 8f
                },
                new FloatParameter
                {
                    Key = "explosionLifespanMultiplier",
                    DisplayName = "Explosion Lifespan",
                    Description = "Lifespan multiplier for explosion lasers (relative to normal lifespan)",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.5f
                },
                new BoolParameter
                {
                    Key = "explosionLasersCanCollide",
                    DisplayName = "Collide Always",
                    Description = "When checked, explosion lasers can also collide with other lasers"
                },
                new FloatParameter
                {
                    Key = "maxCollisionCount",
                    DisplayName = "Max Collisions",
                    Description = "Maximum number of collisions a laser can participate in",
                    MinValue = 1f,
                    MaxValue = 10f,
                    DefaultValue = 3f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new LaserWorkSettingsControl(effect);
    }
}
