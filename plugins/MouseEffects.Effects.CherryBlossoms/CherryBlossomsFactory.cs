using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.CherryBlossoms.UI;

namespace MouseEffects.Effects.CherryBlossoms;

public sealed class CherryBlossomsFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "cherry-blossoms",
        Name = "Cherry Blossoms",
        Description = "Beautiful sakura petals floating gently around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new CherryBlossomsEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Cherry Blossom Settings (cb_ prefix)
        config.Set("cb_petalCount", 30);
        config.Set("cb_fallSpeed", 60f);
        config.Set("cb_swayAmount", 40f);
        config.Set("cb_swayFrequency", 0.8f);
        config.Set("cb_minSize", 10f);
        config.Set("cb_maxSize", 18f);
        config.Set("cb_spinSpeed", 1.5f);
        config.Set("cb_glowIntensity", 0.8f);
        config.Set("cb_spawnRadius", 180f);
        config.Set("cb_lifetime", 10f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Petal Count
                new IntParameter
                {
                    Key = "cb_petalCount",
                    DisplayName = "Petal Count",
                    Description = "Number of petals spawned per second",
                    MinValue = 10,
                    MaxValue = 100,
                    DefaultValue = 30
                },

                // Fall Speed
                new FloatParameter
                {
                    Key = "cb_fallSpeed",
                    DisplayName = "Fall Speed",
                    Description = "Downward falling speed of petals",
                    MinValue = 20f,
                    MaxValue = 150f,
                    DefaultValue = 60f,
                    Step = 5f
                },

                // Sway Amount
                new FloatParameter
                {
                    Key = "cb_swayAmount",
                    DisplayName = "Sway Amount",
                    Description = "Strength of side-to-side swaying motion",
                    MinValue = 0f,
                    MaxValue = 100f,
                    DefaultValue = 40f,
                    Step = 5f
                },

                // Sway Frequency
                new FloatParameter
                {
                    Key = "cb_swayFrequency",
                    DisplayName = "Sway Frequency",
                    Description = "How quickly the petals sway back and forth",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "cb_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum petal size in pixels",
                    MinValue = 5f,
                    MaxValue = 25f,
                    DefaultValue = 10f,
                    Step = 1f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "cb_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum petal size in pixels",
                    MinValue = 8f,
                    MaxValue = 40f,
                    DefaultValue = 18f,
                    Step = 1f
                },

                // Spin Speed
                new FloatParameter
                {
                    Key = "cb_spinSpeed",
                    DisplayName = "Spin Speed",
                    Description = "Speed at which petals tumble and spin",
                    MinValue = 0f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Glow Intensity
                new FloatParameter
                {
                    Key = "cb_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of petal soft glow",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },

                // Spawn Radius
                new FloatParameter
                {
                    Key = "cb_spawnRadius",
                    DisplayName = "Spawn Radius",
                    Description = "Radius around cursor where petals spawn",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 180f,
                    Step = 10f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "cb_lifetime",
                    DisplayName = "Petal Lifetime",
                    Description = "How long petals exist before fading",
                    MinValue = 3f,
                    MaxValue = 20f,
                    DefaultValue = 10f,
                    Step = 1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new CherryBlossomsSettingsControl(effect);
}
