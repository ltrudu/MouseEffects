using MouseEffects.Core.Effects;
using MouseEffects.Effects.FireTrail.UI;

namespace MouseEffects.Effects.FireTrail;

/// <summary>
/// Factory for creating FireTrailEffect instances.
/// </summary>
public sealed class FireTrailFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "firetrail",
        Name = "Fire Trail",
        Description = "Creates realistic fire and flames that trail behind the mouse cursor with particle effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new FireTrailEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // General settings
        config.Set("ft_enabled", true);
        config.Set("ft_intensity", 1.0f);
        config.Set("ft_particleLifetime", 1.5f);

        // Flame appearance
        config.Set("ft_flameHeight", 80f);
        config.Set("ft_flameWidth", 40f);
        config.Set("ft_turbulenceAmount", 0.5f);
        config.Set("ft_flickerSpeed", 15f);
        config.Set("ft_glowIntensity", 1.2f);
        config.Set("ft_colorSaturation", 1.0f);

        // Fire style
        config.Set("ft_fireStyle", 0); // 0=Campfire, 1=Torch, 2=Inferno

        // Particle types
        config.Set("ft_smokeAmount", 0.3f);
        config.Set("ft_emberAmount", 0.2f);

        // Speed settings
        config.Set("ft_minSpeed", 20f);
        config.Set("ft_maxSpeed", 60f);

        // HDR settings
        config.Set("ft_hdrEnabled", true);
        config.Set("ft_hdrBrightness", 2.0f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // General
                new BoolParameter
                {
                    Key = "ft_enabled",
                    DisplayName = "Enabled",
                    Description = "Enable or disable the fire trail effect",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "ft_intensity",
                    DisplayName = "Intensity",
                    Description = "Overall intensity of the fire effect",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ft_particleLifetime",
                    DisplayName = "Particle Lifetime",
                    Description = "How long particles last (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Flame Appearance
                new FloatParameter
                {
                    Key = "ft_flameHeight",
                    DisplayName = "Flame Height",
                    Description = "How high the flames rise",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 80f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "ft_flameWidth",
                    DisplayName = "Flame Width",
                    Description = "Width of the fire trail",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "ft_turbulenceAmount",
                    DisplayName = "Turbulence",
                    Description = "Amount of flame movement and chaos",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.5f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "ft_flickerSpeed",
                    DisplayName = "Flicker Speed",
                    Description = "Speed of flame flickering",
                    MinValue = 1f,
                    MaxValue = 30f,
                    DefaultValue = 15f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ft_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of fire glow",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ft_colorSaturation",
                    DisplayName = "Color Saturation",
                    Description = "Vibrancy of fire colors",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Fire Style
                new IntParameter
                {
                    Key = "ft_fireStyle",
                    DisplayName = "Fire Style",
                    Description = "0=Campfire, 1=Torch, 2=Inferno",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 0
                },

                // Particle Types
                new FloatParameter
                {
                    Key = "ft_smokeAmount",
                    DisplayName = "Smoke Amount",
                    Description = "Amount of smoke particles",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "ft_emberAmount",
                    DisplayName = "Ember Amount",
                    Description = "Amount of ember/spark particles",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.2f,
                    Step = 0.05f
                },

                // HDR
                new BoolParameter
                {
                    Key = "ft_hdrEnabled",
                    DisplayName = "HDR Enabled",
                    Description = "Enable HDR brightness for fire",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "ft_hdrBrightness",
                    DisplayName = "HDR Brightness",
                    Description = "Peak brightness for HDR displays",
                    MinValue = 1f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new FireTrailSettingsControl(effect);
    }
}
