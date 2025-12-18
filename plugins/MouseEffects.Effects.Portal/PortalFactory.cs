using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Portal.UI;

namespace MouseEffects.Effects.Portal;

public sealed class PortalFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "portal",
        Name = "Portal",
        Description = "Swirling dimensional vortex/portal effect at the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new PortalEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Portal dimensions
        config.Set("portalRadius", 150f);

        // Animation
        config.Set("rotationSpeed", 1.0f);
        config.Set("spiralTightness", 1.0f);
        config.Set("spiralArms", 4);

        // Visual effects
        config.Set("glowIntensity", 1.2f);
        config.Set("depthStrength", 0.7f);
        config.Set("innerDarkness", 0.2f);
        config.Set("distortionStrength", 1.0f);

        // Rim particles
        config.Set("rimParticlesEnabled", true);
        config.Set("particleSpeed", 1.5f);

        // Colors
        config.Set("portalColor", new Vector4(0.4f, 0.7f, 1f, 1f));
        config.Set("rimColor", new Vector4(0.8f, 0.9f, 1f, 1f));
        config.Set("colorTheme", 0); // 0=Blue, 1=Purple, 2=Orange, 3=Rainbow

        // HDR
        config.Set("hdrMultiplier", 0.5f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Portal Dimensions
                new FloatParameter
                {
                    Key = "portalRadius",
                    DisplayName = "Portal Radius",
                    Description = "Size of the portal in pixels",
                    MinValue = 50f,
                    MaxValue = 300f,
                    DefaultValue = 150f,
                    Step = 5f
                },

                // Spiral Pattern
                new IntParameter
                {
                    Key = "spiralArms",
                    DisplayName = "Spiral Arms",
                    Description = "Number of spiral arms in the vortex",
                    MinValue = 2,
                    MaxValue = 8,
                    DefaultValue = 4
                },
                new FloatParameter
                {
                    Key = "spiralTightness",
                    DisplayName = "Spiral Tightness",
                    Description = "How tightly wound the spiral is",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Animation
                new FloatParameter
                {
                    Key = "rotationSpeed",
                    DisplayName = "Rotation Speed",
                    Description = "Speed of portal rotation",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Visual Effects
                new FloatParameter
                {
                    Key = "glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Overall glow brightness",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "depthStrength",
                    DisplayName = "Depth Effect",
                    Description = "Strength of the receding center depth illusion",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "innerDarkness",
                    DisplayName = "Center Darkness",
                    Description = "How dark the center of the portal appears",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.2f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "distortionStrength",
                    DisplayName = "Swirl Distortion",
                    Description = "Strength of the swirling distortion effect",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Rim Particles
                new BoolParameter
                {
                    Key = "rimParticlesEnabled",
                    DisplayName = "Rim Particles",
                    Description = "Enable sparkling particles around the portal rim",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "particleSpeed",
                    DisplayName = "Particle Speed",
                    Description = "Speed of particles rotating around the rim",
                    MinValue = 0f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Color Theme
                new ChoiceParameter
                {
                    Key = "colorTheme",
                    DisplayName = "Color Theme",
                    Description = "Portal color theme",
                    Choices = ["Blue Portal", "Purple Portal", "Orange Portal", "Rainbow Portal"],
                    DefaultValue = "Blue Portal"
                },
                new ColorParameter
                {
                    Key = "portalColor",
                    DisplayName = "Portal Color",
                    Description = "Custom portal color (only when not using theme presets)",
                    DefaultValue = new Vector4(0.4f, 0.7f, 1f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "rimColor",
                    DisplayName = "Rim Color",
                    Description = "Color of the outer rim glow",
                    DefaultValue = new Vector4(0.8f, 0.9f, 1f, 1f),
                    SupportsAlpha = false
                },

                // HDR
                new FloatParameter
                {
                    Key = "hdrMultiplier",
                    DisplayName = "HDR Brightness",
                    Description = "Extra brightness for HDR displays",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new PortalSettingsControl(effect);
}
