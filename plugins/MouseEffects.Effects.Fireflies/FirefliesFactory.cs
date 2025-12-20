using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Fireflies.UI;

namespace MouseEffects.Effects.Fireflies;

public sealed class FirefliesFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "fireflies",
        Name = "Fireflies",
        Description = "Glowing fireflies that swarm around the mouse cursor with pulsing bioluminescence",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Nature
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new FirefliesEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // General settings (ff_ prefix for fireflies)
        config.Set("ff_fireflyCount", 49);
        config.Set("ff_glowSize", 15f);
        config.Set("ff_glowColor", new Vector4(0.8f, 1.0f, 0.3f, 1f)); // Warm yellow-green

        // Pulse settings
        config.Set("ff_pulseSpeed", 3.0f);
        config.Set("ff_pulseRandomness", 0.5f);
        config.Set("ff_minBrightness", 0.2f);
        config.Set("ff_maxBrightness", 0.6f);

        // Movement settings
        config.Set("ff_attractionStrength", 1.1f);
        config.Set("ff_wanderStrength", 30f);
        config.Set("ff_maxSpeed", 140f);
        config.Set("ff_wanderChangeRate", 2.0f);

        // HDR settings
        config.Set("ff_hdrMultiplier", 1.5f);

        // Explosion settings
        config.Set("ff_explosionEnabled", true);
        config.Set("ff_explosionStrength", 1500f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // General
                new IntParameter
                {
                    Key = "ff_fireflyCount",
                    DisplayName = "Firefly Count",
                    Description = "Number of fireflies to display",
                    MinValue = 5,
                    MaxValue = 500,
                    DefaultValue = 49
                },
                new FloatParameter
                {
                    Key = "ff_glowSize",
                    DisplayName = "Glow Size",
                    Description = "Size of the firefly glow in pixels",
                    MinValue = 10f,
                    MaxValue = 40f,
                    DefaultValue = 15f,
                    Step = 1f
                },
                new ColorParameter
                {
                    Key = "ff_glowColor",
                    DisplayName = "Glow Color",
                    Description = "Color of the firefly glow",
                    DefaultValue = new Vector4(0.8f, 1.0f, 0.3f, 1f),
                    SupportsAlpha = false
                },

                // Pulse settings
                new FloatParameter
                {
                    Key = "ff_pulseSpeed",
                    DisplayName = "Pulse Speed",
                    Description = "How fast the fireflies pulse on/off",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 3.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ff_pulseRandomness",
                    DisplayName = "Pulse Randomness",
                    Description = "Variation in pulse timing between fireflies",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ff_minBrightness",
                    DisplayName = "Min Brightness",
                    Description = "Minimum brightness when pulsing off",
                    MinValue = 0f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.2f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "ff_maxBrightness",
                    DisplayName = "Max Brightness",
                    Description = "Maximum brightness when pulsing on",
                    MinValue = 0.5f,
                    MaxValue = 2f,
                    DefaultValue = 0.6f,
                    Step = 0.1f
                },

                // Movement
                new FloatParameter
                {
                    Key = "ff_attractionStrength",
                    DisplayName = "Attraction Strength",
                    Description = "How strongly fireflies are attracted to the cursor",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 1.1f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ff_wanderStrength",
                    DisplayName = "Wander Strength",
                    Description = "How much fireflies wander randomly",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "ff_maxSpeed",
                    DisplayName = "Max Speed",
                    Description = "Maximum movement speed of fireflies",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 140f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "ff_wanderChangeRate",
                    DisplayName = "Wander Change Rate",
                    Description = "How often fireflies change wander direction",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },

                // HDR
                new FloatParameter
                {
                    Key = "ff_hdrMultiplier",
                    DisplayName = "HDR Multiplier",
                    Description = "Brightness multiplier for HDR displays",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Explosion
                new BoolParameter
                {
                    Key = "ff_explosionEnabled",
                    DisplayName = "Explosion on Click",
                    Description = "Explode fireflies outward when clicking",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "ff_explosionStrength",
                    DisplayName = "Explosion Strength",
                    Description = "How strongly fireflies are pushed when exploding",
                    MinValue = 100f,
                    MaxValue = 2000f,
                    DefaultValue = 1500f,
                    Step = 50f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new FirefliesSettingsControl(effect);
}
