using MouseEffects.Core.Effects;
using MouseEffects.Effects.Rain.UI;

namespace MouseEffects.Effects.Rain;

public sealed class RainFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "rain",
        Name = "Rain",
        Description = "Realistic raindrops falling around the mouse cursor with splash effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Nature
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new RainEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Rain Settings (rain_ prefix)
        config.Set("rain_intensity", 50);           // Drops per second
        config.Set("rain_fallSpeed", 800f);         // Fast falling
        config.Set("rain_windAngle", 15f);          // Wind angle in degrees
        config.Set("rain_minLength", 15f);          // Min streak length
        config.Set("rain_maxLength", 30f);          // Max streak length
        config.Set("rain_minSize", 1.5f);           // Min width
        config.Set("rain_maxSize", 3f);             // Max width
        config.Set("rain_splashEnabled", true);     // Enable splashes
        config.Set("rain_splashSize", 20f);         // Splash radius
        config.Set("rain_spawnRadius", 200f);       // Area around cursor
        config.Set("rain_fullScreen", false);       // Cursor-follow mode
        config.Set("rain_lifetime", 3f);            // Raindrop lifetime

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Rain Intensity
                new IntParameter
                {
                    Key = "rain_intensity",
                    DisplayName = "Rain Intensity",
                    Description = "Number of raindrops spawned per second",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 50
                },

                // Fall Speed
                new FloatParameter
                {
                    Key = "rain_fallSpeed",
                    DisplayName = "Fall Speed",
                    Description = "How fast raindrops fall (pixels per second)",
                    MinValue = 200f,
                    MaxValue = 1500f,
                    DefaultValue = 800f,
                    Step = 50f
                },

                // Wind Angle
                new FloatParameter
                {
                    Key = "rain_windAngle",
                    DisplayName = "Wind Angle",
                    Description = "Angle of wind affecting rain direction (degrees)",
                    MinValue = -45f,
                    MaxValue = 45f,
                    DefaultValue = 15f,
                    Step = 1f
                },

                // Min Length
                new FloatParameter
                {
                    Key = "rain_minLength",
                    DisplayName = "Min Streak Length",
                    Description = "Minimum length of raindrop streaks",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 1f
                },

                // Max Length
                new FloatParameter
                {
                    Key = "rain_maxLength",
                    DisplayName = "Max Streak Length",
                    Description = "Maximum length of raindrop streaks",
                    MinValue = 10f,
                    MaxValue = 80f,
                    DefaultValue = 30f,
                    Step = 1f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "rain_minSize",
                    DisplayName = "Min Drop Width",
                    Description = "Minimum width of raindrops",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "rain_maxSize",
                    DisplayName = "Max Drop Width",
                    Description = "Maximum width of raindrops",
                    MinValue = 1f,
                    MaxValue = 8f,
                    DefaultValue = 3f,
                    Step = 0.1f
                },

                // Splash Enabled
                new BoolParameter
                {
                    Key = "rain_splashEnabled",
                    DisplayName = "Enable Splashes",
                    Description = "Show splash effects when drops land",
                    DefaultValue = true
                },

                // Splash Size
                new FloatParameter
                {
                    Key = "rain_splashSize",
                    DisplayName = "Splash Size",
                    Description = "Maximum radius of splash effects",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 20f,
                    Step = 1f
                },

                // Spawn Radius
                new FloatParameter
                {
                    Key = "rain_spawnRadius",
                    DisplayName = "Spawn Radius",
                    Description = "Radius around cursor where rain spawns (cursor mode)",
                    MinValue = 100f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 10f
                },

                // Full Screen Mode
                new BoolParameter
                {
                    Key = "rain_fullScreen",
                    DisplayName = "Full Screen Mode",
                    Description = "Rain across entire screen instead of following cursor",
                    DefaultValue = false
                },

                // Raindrop Lifetime
                new FloatParameter
                {
                    Key = "rain_lifetime",
                    DisplayName = "Raindrop Lifetime",
                    Description = "How long raindrops exist before fading",
                    MinValue = 1f,
                    MaxValue = 10f,
                    DefaultValue = 3f,
                    Step = 0.5f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new RainSettingsControl(effect);
}
