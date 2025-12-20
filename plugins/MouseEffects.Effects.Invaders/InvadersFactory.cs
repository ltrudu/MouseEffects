using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Invaders.UI;

namespace MouseEffects.Effects.Invaders;

public sealed class InvadersFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "invaders",
        Name = "Space Invaders",
        Description = "Defend against waves of neon space invaders with rockets from your cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Interactive
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new InvadersEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Rocket settings
        config.Set("spawnOnLeftClick", true);
        config.Set("spawnOnRightClick", false);
        config.Set("spawnOnMove", true);
        config.Set("moveSpawnDistance", 80f);
        config.Set("rocketSpeed", 810f);
        config.Set("rocketSize", 8f);
        config.Set("rocketRainbowMode", true);
        config.Set("rocketRainbowSpeed", 0.5f);
        config.Set("rocketColor", new Vector4(0f, 1f, 0.5f, 1f));

        // Invader settings
        config.Set("invaderSpawnRate", 0.53f);
        config.Set("invaderMinSpeed", 83f);
        config.Set("invaderMaxSpeed", 406f);
        config.Set("invaderBigSize", 48f);
        config.Set("invaderMediumSizePercent", 0.5f);
        config.Set("invaderSmallSizePercent", 0.25f);
        config.Set("maxActiveInvaders", 20);
        config.Set("invaderDescentSpeed", 30f);
        config.Set("invaderSmallColor", new Vector4(1f, 0.2f, 0.8f, 1f));
        config.Set("invaderMediumColor", new Vector4(0.58f, 1f, 0.2f, 1f));
        config.Set("invaderBigColor", new Vector4(0f, 0.25f, 0.5f, 1f));

        // Explosion settings
        config.Set("explosionParticleCount", 30);
        config.Set("explosionForce", 200f);
        config.Set("explosionLifespan", 1.0f);
        config.Set("explosionParticleSize", 6f);
        config.Set("explosionGlowIntensity", 1.5f);

        // Visual settings
        config.Set("glowIntensity", 1.2f);
        config.Set("neonIntensity", 1.0f);
        config.Set("enableTrails", true);
        config.Set("trailLength", 0.4f);
        config.Set("animSpeed", 2.0f);

        // Scoring
        config.Set("scoreSmall", 200);
        config.Set("scoreMedium", 100);
        config.Set("scoreBig", 50);

        // Score overlay
        config.Set("showScoreOverlay", true);
        config.Set("scoreOverlaySize", 32f);
        config.Set("scoreOverlaySpacing", 1.3f);
        config.Set("scoreOverlayMargin", 20f);
        config.Set("scoreOverlayBgOpacity", 0.7f);
        config.Set("scoreOverlayColor", new Vector4(0f, 1f, 0f, 1f));
        config.Set("scoreOverlayX", 70f);
        config.Set("scoreOverlayY", 49f);

        // Timer
        config.Set("timerDuration", 90f);

        // Reset hotkey
        config.Set("enableResetHotkey", false);

        // Default high scores (stored as JSON, not shown in settings UI)
        config.Set("highScoresJson", "[{\"PointsPerMinute\":2000,\"Date\":\"04/12/2025\"},{\"PointsPerMinute\":1500,\"Date\":\"04/12/2025\"},{\"PointsPerMinute\":1000,\"Date\":\"04/12/2025\"},{\"PointsPerMinute\":500,\"Date\":\"04/12/2025\"},{\"PointsPerMinute\":200,\"Date\":\"04/12/2025\"}]");

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // ========== ROCKET SETTINGS ==========
                new BoolParameter
                {
                    Key = "spawnOnLeftClick",
                    DisplayName = "Spawn on Left Click",
                    Description = "Launch rockets when left mouse button is clicked",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "spawnOnRightClick",
                    DisplayName = "Spawn on Right Click",
                    Description = "Launch rockets when right mouse button is clicked",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "spawnOnMove",
                    DisplayName = "Spawn on Move",
                    Description = "Launch rockets while moving the mouse",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "moveSpawnDistance",
                    DisplayName = "Move Spawn Distance",
                    Description = "Distance to travel before spawning a rocket when moving (px)",
                    MinValue = 20f,
                    MaxValue = 300f,
                    DefaultValue = 80f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "rocketSpeed",
                    DisplayName = "Rocket Speed",
                    Description = "Speed of rockets (pixels/sec)",
                    MinValue = 200f,
                    MaxValue = 1500f,
                    DefaultValue = 810f,
                    Step = 50f
                },
                new FloatParameter
                {
                    Key = "rocketSize",
                    DisplayName = "Rocket Size",
                    Description = "Size of rockets (px)",
                    MinValue = 4f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new BoolParameter
                {
                    Key = "rocketRainbowMode",
                    DisplayName = "Rocket Rainbow Mode",
                    Description = "Cycle through rainbow colors for rockets",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "rocketRainbowSpeed",
                    DisplayName = "Rocket Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new ColorParameter
                {
                    Key = "rocketColor",
                    DisplayName = "Rocket Color",
                    Description = "Color of rockets (when rainbow mode is off)",
                    DefaultValue = new Vector4(0f, 1f, 0.5f, 1f),
                    SupportsAlpha = false
                },

                // ========== INVADER SETTINGS ==========
                new FloatParameter
                {
                    Key = "invaderSpawnRate",
                    DisplayName = "Invader Spawn Rate",
                    Description = "Seconds between invader spawns",
                    MinValue = 0.2f,
                    MaxValue = 5f,
                    DefaultValue = 0.53f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "invaderMinSpeed",
                    DisplayName = "Invader Min Speed",
                    Description = "Minimum horizontal speed of invaders (px/sec)",
                    MinValue = 10f,
                    MaxValue = 300f,
                    DefaultValue = 83f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "invaderMaxSpeed",
                    DisplayName = "Invader Max Speed",
                    Description = "Maximum horizontal speed of invaders (px/sec)",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 406f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "invaderBigSize",
                    DisplayName = "Invader Big Size",
                    Description = "Size of large invaders (px)",
                    MinValue = 20f,
                    MaxValue = 100f,
                    DefaultValue = 48f,
                    Step = 2f
                },
                new FloatParameter
                {
                    Key = "invaderMediumSizePercent",
                    DisplayName = "Medium Size (%)",
                    Description = "Medium invader size as percentage of big",
                    MinValue = 0.3f,
                    MaxValue = 0.8f,
                    DefaultValue = 0.5f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "invaderSmallSizePercent",
                    DisplayName = "Small Size (%)",
                    Description = "Small invader size as percentage of big",
                    MinValue = 0.1f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.25f,
                    Step = 0.05f
                },
                new IntParameter
                {
                    Key = "maxActiveInvaders",
                    DisplayName = "Max Active Invaders",
                    Description = "Maximum number of invaders on screen",
                    MinValue = 5,
                    MaxValue = 100,
                    DefaultValue = 20
                },
                new FloatParameter
                {
                    Key = "invaderDescentSpeed",
                    DisplayName = "Invader Descent Speed",
                    Description = "Speed at which invaders descend (px/sec)",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },
                new ColorParameter
                {
                    Key = "invaderSmallColor",
                    DisplayName = "Small Invader Color",
                    Description = "Color of small (squid) invaders",
                    DefaultValue = new Vector4(1f, 0.2f, 0.8f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "invaderMediumColor",
                    DisplayName = "Medium Invader Color",
                    Description = "Color of medium (crab) invaders",
                    DefaultValue = new Vector4(0.58f, 1f, 0.2f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "invaderBigColor",
                    DisplayName = "Big Invader Color",
                    Description = "Color of big (octopus) invaders",
                    DefaultValue = new Vector4(0f, 0.25f, 0.5f, 1f),
                    SupportsAlpha = false
                },

                // ========== EXPLOSION SETTINGS ==========
                new IntParameter
                {
                    Key = "explosionParticleCount",
                    DisplayName = "Explosion Particles",
                    Description = "Number of particles per explosion",
                    MinValue = 10,
                    MaxValue = 100,
                    DefaultValue = 30
                },
                new FloatParameter
                {
                    Key = "explosionForce",
                    DisplayName = "Explosion Force",
                    Description = "Force of explosion particles",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 25f
                },
                new FloatParameter
                {
                    Key = "explosionLifespan",
                    DisplayName = "Explosion Lifespan",
                    Description = "How long explosion particles live (sec)",
                    MinValue = 0.3f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "explosionParticleSize",
                    DisplayName = "Explosion Particle Size",
                    Description = "Size of explosion particles (px)",
                    MinValue = 2f,
                    MaxValue = 20f,
                    DefaultValue = 6f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "explosionGlowIntensity",
                    DisplayName = "Explosion Glow",
                    Description = "Glow intensity of explosions",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // ========== VISUAL SETTINGS ==========
                new FloatParameter
                {
                    Key = "glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Overall glow intensity",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "neonIntensity",
                    DisplayName = "Neon Intensity",
                    Description = "Neon edge glow intensity",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "enableTrails",
                    DisplayName = "Enable Trails",
                    Description = "Show trail particles behind rockets",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "trailLength",
                    DisplayName = "Trail Length",
                    Description = "Length of rocket trails",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.4f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "animSpeed",
                    DisplayName = "Animation Speed",
                    Description = "Speed of invader animations",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.25f
                },

                // ========== SCORING ==========
                new IntParameter
                {
                    Key = "scoreSmall",
                    DisplayName = "Small Score",
                    Description = "Points for destroying small invaders",
                    MinValue = 50,
                    MaxValue = 500,
                    DefaultValue = 200
                },
                new IntParameter
                {
                    Key = "scoreMedium",
                    DisplayName = "Medium Score",
                    Description = "Points for destroying medium invaders",
                    MinValue = 25,
                    MaxValue = 300,
                    DefaultValue = 100
                },
                new IntParameter
                {
                    Key = "scoreBig",
                    DisplayName = "Big Score",
                    Description = "Points for destroying big invaders",
                    MinValue = 10,
                    MaxValue = 200,
                    DefaultValue = 50
                },

                // ========== SCORE OVERLAY ==========
                new BoolParameter
                {
                    Key = "showScoreOverlay",
                    DisplayName = "Show Score Overlay",
                    Description = "Display score on screen during gameplay",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "scoreOverlaySize",
                    DisplayName = "Score Size",
                    Description = "Size of score digits on screen (px)",
                    MinValue = 16f,
                    MaxValue = 100f,
                    DefaultValue = 32f,
                    Step = 4f
                },
                new FloatParameter
                {
                    Key = "scoreOverlaySpacing",
                    DisplayName = "Digit Spacing",
                    Description = "Spacing between score digits (multiplier)",
                    MinValue = 1.0f,
                    MaxValue = 2.5f,
                    DefaultValue = 1.3f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "scoreOverlayMargin",
                    DisplayName = "Label Margin",
                    Description = "Margin between labels and values (px)",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "scoreOverlayBgOpacity",
                    DisplayName = "Background Opacity",
                    Description = "Opacity of score overlay background (0=transparent, 1=opaque)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.1f
                },
                new ColorParameter
                {
                    Key = "scoreOverlayColor",
                    DisplayName = "Score Color",
                    Description = "Color of score overlay",
                    DefaultValue = new Vector4(0f, 1f, 0f, 1f),
                    SupportsAlpha = false
                },
                new FloatParameter
                {
                    Key = "scoreOverlayX",
                    DisplayName = "Score X Position",
                    Description = "Horizontal position of score (px from left)",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 70f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "scoreOverlayY",
                    DisplayName = "Score Y Position",
                    Description = "Vertical position of score (px from top)",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 49f,
                    Step = 10f
                },

                // ========== TIMER ==========
                new FloatParameter
                {
                    Key = "timerDuration",
                    DisplayName = "Game Duration",
                    Description = "Duration of the game in seconds",
                    MinValue = 30f,
                    MaxValue = 300f,
                    DefaultValue = 90f,
                    Step = 15f
                },

                // ========== HOTKEYS ==========
                new BoolParameter
                {
                    Key = "enableResetHotkey",
                    DisplayName = "Reset Hotkey (Alt+Shift+R)",
                    Description = "Enable Alt+Shift+R to reset and restart the game",
                    DefaultValue = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new InvadersSettingsControl(effect);
}
