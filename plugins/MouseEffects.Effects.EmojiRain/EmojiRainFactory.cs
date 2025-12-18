using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.EmojiRain.UI;

namespace MouseEffects.Effects.EmojiRain;

public sealed class EmojiRainFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "emojirain",
        Name = "Emoji Rain",
        Description = "Falling emoji faces from the mouse cursor with rotation and tumble",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new EmojiRainEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Emoji Settings (er_ prefix)
        config.Set("er_emojiCount", 15);
        config.Set("er_fallSpeed", 100f);
        config.Set("er_minSize", 20f);
        config.Set("er_maxSize", 40f);
        config.Set("er_rotationAmount", 2.0f);
        config.Set("er_lifetime", 6f);
        config.Set("er_enableHappy", true);
        config.Set("er_enableSad", true);
        config.Set("er_enableWink", true);
        config.Set("er_enableHeartEyes", true);
        config.Set("er_enableStarEyes", true);
        config.Set("er_enableSurprised", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Emoji Count
                new IntParameter
                {
                    Key = "er_emojiCount",
                    DisplayName = "Emoji Count",
                    Description = "Number of emojis spawned per second while moving",
                    MinValue = 5,
                    MaxValue = 50,
                    DefaultValue = 15
                },

                // Fall Speed
                new FloatParameter
                {
                    Key = "er_fallSpeed",
                    DisplayName = "Fall Speed",
                    Description = "Downward falling speed of emojis",
                    MinValue = 30f,
                    MaxValue = 200f,
                    DefaultValue = 100f,
                    Step = 10f
                },

                // Min Size
                new FloatParameter
                {
                    Key = "er_minSize",
                    DisplayName = "Min Size",
                    Description = "Minimum emoji size in pixels",
                    MinValue = 10f,
                    MaxValue = 40f,
                    DefaultValue = 20f,
                    Step = 2f
                },

                // Max Size
                new FloatParameter
                {
                    Key = "er_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum emoji size in pixels",
                    MinValue = 20f,
                    MaxValue = 80f,
                    DefaultValue = 40f,
                    Step = 2f
                },

                // Rotation Amount
                new FloatParameter
                {
                    Key = "er_rotationAmount",
                    DisplayName = "Rotation Amount",
                    Description = "Amount of tumble rotation while falling",
                    MinValue = 0f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.5f
                },

                // Lifetime
                new FloatParameter
                {
                    Key = "er_lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long emojis fall before fading (seconds)",
                    MinValue = 3f,
                    MaxValue = 12f,
                    DefaultValue = 6f,
                    Step = 1f
                },

                // Emoji Type Toggles
                new BoolParameter
                {
                    Key = "er_enableHappy",
                    DisplayName = "Enable Happy Face",
                    Description = "Show happy face emojis",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "er_enableSad",
                    DisplayName = "Enable Sad Face",
                    Description = "Show sad face emojis",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "er_enableWink",
                    DisplayName = "Enable Wink Face",
                    Description = "Show wink face emojis",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "er_enableHeartEyes",
                    DisplayName = "Enable Heart Eyes",
                    Description = "Show heart eyes emojis",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "er_enableStarEyes",
                    DisplayName = "Enable Star Eyes",
                    Description = "Show star eyes emojis",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "er_enableSurprised",
                    DisplayName = "Enable Surprised Face",
                    Description = "Show surprised face emojis",
                    DefaultValue = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new EmojiRainSettingsControl(effect);
}
