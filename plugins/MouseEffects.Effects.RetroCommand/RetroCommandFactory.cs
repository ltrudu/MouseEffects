using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.RetroCommand.UI;

namespace MouseEffects.Effects.RetroCommand;

public sealed class RetroCommandFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "retrocommand",
        Name = "Retro Command",
        Description = "Defend cities from incoming missiles! Click to launch counter-missiles that explode and destroy enemy missiles.",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Interactive
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new RetroCommandEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Render style
        config.Set("rc_renderStyle", 0); // 0=Modern, 1=Retro

        // Explosion mode
        config.Set("rc_explosionMode", 1); // 0=Instant, 1=Counter-missile (DEFAULT)
        config.Set("rc_counterMissileSpeed", 400f);

        // Fire rate mode
        config.Set("rc_fireRateMode", 1); // 0=Unlimited, 1=MaxActive (DEFAULT), 2=Cooldown
        config.Set("rc_maxActiveExplosions", 5);
        config.Set("rc_fireCooldown", 0.3f);

        // City settings
        config.Set("rc_cityCount", 6);
        config.Set("rc_citySize", 40f);
        config.Set("rc_cityColor", new Vector4(0f, 0.8f, 1f, 1f)); // cyan

        // Base settings
        config.Set("rc_baseSize", 50f);
        config.Set("rc_baseColor", new Vector4(0f, 1f, 0.5f, 1f)); // green

        // Enemy missile settings
        config.Set("rc_enemyMissileSpeed", 100f);
        config.Set("rc_enemyMissileSpeedIncrease", 15f);
        config.Set("rc_enemyMissilesPerWave", 10);
        config.Set("rc_enemyMissileSize", 8f);
        config.Set("rc_enemyMissileColor", new Vector4(1f, 0.2f, 0.2f, 1f)); // red

        // Explosion settings
        config.Set("rc_explosionMaxRadius", 80f);
        config.Set("rc_explosionExpandSpeed", 200f);
        config.Set("rc_explosionShrinkSpeed", 150f);
        config.Set("rc_explosionDuration", 1.5f);
        config.Set("rc_explosionColor", new Vector4(1f, 0.8f, 0.2f, 1f)); // orange-yellow

        // Visual settings
        config.Set("rc_glowIntensity", 1.2f);
        config.Set("rc_neonIntensity", 1.0f);
        config.Set("rc_showTrails", true);

        // Wave settings
        config.Set("rc_wavePauseDuration", 2.0f);

        // Scoring
        config.Set("rc_scoreWaveBonus", 100);
        config.Set("rc_scoreMissile", 25);
        config.Set("rc_scoreCityBonus", 500);

        // Score overlay
        config.Set("rc_showScoreOverlay", true);
        config.Set("rc_scoreOverlaySize", 32f);
        config.Set("rc_scoreOverlaySpacing", 1.3f);
        config.Set("rc_scoreOverlayMargin", 20f);
        config.Set("rc_scoreOverlayBgOpacity", 0.7f);
        config.Set("rc_scoreOverlayColor", new Vector4(0f, 1f, 0f, 1f)); // green
        config.Set("rc_scoreOverlayX", 70f);
        config.Set("rc_scoreOverlayY", 49f);

        // Timer
        config.Set("rc_timerDuration", 90f);

        // Hotkey
        config.Set("rc_enableResetHotkey", false);

        // Default high scores (stored as JSON, not shown in settings UI)
        config.Set("rc_highScoresJson", "[{\"PointsPerMinute\":2000,\"Date\":\"21/12/2025\"},{\"PointsPerMinute\":1500,\"Date\":\"21/12/2025\"},{\"PointsPerMinute\":1000,\"Date\":\"21/12/2025\"},{\"PointsPerMinute\":500,\"Date\":\"21/12/2025\"},{\"PointsPerMinute\":200,\"Date\":\"21/12/2025\"}]");

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // ========== RENDER STYLE ==========
                new IntParameter
                {
                    Key = "rc_renderStyle",
                    DisplayName = "Render Style",
                    Description = "Visual style: 0=Modern (neon glow), 1=Retro (pixelated arcade)",
                    MinValue = 0,
                    MaxValue = 1,
                    DefaultValue = 0
                },

                // ========== EXPLOSION MODE ==========
                new IntParameter
                {
                    Key = "rc_explosionMode",
                    DisplayName = "Explosion Mode",
                    Description = "0=Instant at click, 1=Counter-missile launches first",
                    MinValue = 0,
                    MaxValue = 1,
                    DefaultValue = 1
                },
                new FloatParameter
                {
                    Key = "rc_counterMissileSpeed",
                    DisplayName = "Counter-Missile Speed",
                    Description = "Speed of defensive counter-missiles (px/sec)",
                    MinValue = 100f,
                    MaxValue = 1000f,
                    DefaultValue = 400f,
                    Step = 50f
                },

                // ========== FIRE RATE MODE ==========
                new IntParameter
                {
                    Key = "rc_fireRateMode",
                    DisplayName = "Fire Rate Mode",
                    Description = "0=Unlimited, 1=Max Active Explosions, 2=Cooldown",
                    MinValue = 0,
                    MaxValue = 2,
                    DefaultValue = 1
                },
                new IntParameter
                {
                    Key = "rc_maxActiveExplosions",
                    DisplayName = "Max Active Explosions",
                    Description = "Maximum number of simultaneous explosions (mode 1)",
                    MinValue = 1,
                    MaxValue = 20,
                    DefaultValue = 5
                },
                new FloatParameter
                {
                    Key = "rc_fireCooldown",
                    DisplayName = "Fire Cooldown",
                    Description = "Cooldown between shots in seconds (mode 2)",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },

                // ========== CITY SETTINGS ==========
                new IntParameter
                {
                    Key = "rc_cityCount",
                    DisplayName = "City Count",
                    Description = "Number of cities to defend",
                    MinValue = 1,
                    MaxValue = 10,
                    DefaultValue = 6
                },
                new FloatParameter
                {
                    Key = "rc_citySize",
                    DisplayName = "City Size",
                    Description = "Size of city structures (px)",
                    MinValue = 20f,
                    MaxValue = 80f,
                    DefaultValue = 40f,
                    Step = 5f
                },
                new ColorParameter
                {
                    Key = "rc_cityColor",
                    DisplayName = "City Color",
                    Description = "Color of cities",
                    DefaultValue = new Vector4(0f, 0.8f, 1f, 1f),
                    SupportsAlpha = false
                },

                // ========== BASE SETTINGS ==========
                new FloatParameter
                {
                    Key = "rc_baseSize",
                    DisplayName = "Base Size",
                    Description = "Size of missile base (px)",
                    MinValue = 30f,
                    MaxValue = 100f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new ColorParameter
                {
                    Key = "rc_baseColor",
                    DisplayName = "Base Color",
                    Description = "Color of missile base",
                    DefaultValue = new Vector4(0f, 1f, 0.5f, 1f),
                    SupportsAlpha = false
                },

                // ========== ENEMY MISSILE SETTINGS ==========
                new FloatParameter
                {
                    Key = "rc_enemyMissileSpeed",
                    DisplayName = "Enemy Missile Speed",
                    Description = "Initial speed of enemy missiles (px/sec)",
                    MinValue = 20f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "rc_enemyMissileSpeedIncrease",
                    DisplayName = "Speed Increase Per Wave",
                    Description = "Speed increase for each new wave (px/sec)",
                    MinValue = 0f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 5f
                },
                new IntParameter
                {
                    Key = "rc_enemyMissilesPerWave",
                    DisplayName = "Missiles Per Wave",
                    Description = "Number of enemy missiles per wave",
                    MinValue = 1,
                    MaxValue = 30,
                    DefaultValue = 10
                },
                new FloatParameter
                {
                    Key = "rc_enemyMissileSize",
                    DisplayName = "Enemy Missile Size",
                    Description = "Size of enemy missiles (px)",
                    MinValue = 4f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new ColorParameter
                {
                    Key = "rc_enemyMissileColor",
                    DisplayName = "Enemy Missile Color",
                    Description = "Color of enemy missiles",
                    DefaultValue = new Vector4(1f, 0.2f, 0.2f, 1f),
                    SupportsAlpha = false
                },

                // ========== EXPLOSION SETTINGS ==========
                new FloatParameter
                {
                    Key = "rc_explosionMaxRadius",
                    DisplayName = "Explosion Max Radius",
                    Description = "Maximum radius of explosions (px)",
                    MinValue = 30f,
                    MaxValue = 200f,
                    DefaultValue = 80f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "rc_explosionExpandSpeed",
                    DisplayName = "Explosion Expand Speed",
                    Description = "How fast explosions expand (px/sec)",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 25f
                },
                new FloatParameter
                {
                    Key = "rc_explosionShrinkSpeed",
                    DisplayName = "Explosion Shrink Speed",
                    Description = "How fast explosions shrink (px/sec)",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 150f,
                    Step = 25f
                },
                new FloatParameter
                {
                    Key = "rc_explosionDuration",
                    DisplayName = "Explosion Duration",
                    Description = "How long explosions last (sec)",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.25f
                },
                new ColorParameter
                {
                    Key = "rc_explosionColor",
                    DisplayName = "Explosion Color",
                    Description = "Color of explosions",
                    DefaultValue = new Vector4(1f, 0.8f, 0.2f, 1f),
                    SupportsAlpha = false
                },

                // ========== VISUAL SETTINGS ==========
                new FloatParameter
                {
                    Key = "rc_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Overall glow intensity",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "rc_neonIntensity",
                    DisplayName = "Neon Intensity",
                    Description = "Neon edge glow intensity",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "rc_showTrails",
                    DisplayName = "Show Trails",
                    Description = "Show missile trails",
                    DefaultValue = true
                },

                // ========== WAVE SETTINGS ==========
                new FloatParameter
                {
                    Key = "rc_wavePauseDuration",
                    DisplayName = "Wave Pause Duration",
                    Description = "Pause between waves (sec)",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.0f,
                    Step = 0.5f
                },

                // ========== SCORING ==========
                new IntParameter
                {
                    Key = "rc_scoreWaveBonus",
                    DisplayName = "Wave Bonus",
                    Description = "Points for completing a wave",
                    MinValue = 0,
                    MaxValue = 1000,
                    DefaultValue = 100
                },
                new IntParameter
                {
                    Key = "rc_scoreMissile",
                    DisplayName = "Missile Score",
                    Description = "Points for destroying an enemy missile",
                    MinValue = 0,
                    MaxValue = 100,
                    DefaultValue = 25
                },
                new IntParameter
                {
                    Key = "rc_scoreCityBonus",
                    DisplayName = "City Bonus",
                    Description = "Points for each city surviving a wave",
                    MinValue = 0,
                    MaxValue = 1000,
                    DefaultValue = 500
                },

                // ========== SCORE OVERLAY ==========
                new BoolParameter
                {
                    Key = "rc_showScoreOverlay",
                    DisplayName = "Show Score Overlay",
                    Description = "Display score on screen during gameplay",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "rc_scoreOverlaySize",
                    DisplayName = "Score Size",
                    Description = "Size of score digits on screen (px)",
                    MinValue = 16f,
                    MaxValue = 100f,
                    DefaultValue = 32f,
                    Step = 4f
                },
                new FloatParameter
                {
                    Key = "rc_scoreOverlaySpacing",
                    DisplayName = "Digit Spacing",
                    Description = "Spacing between score digits (multiplier)",
                    MinValue = 1.0f,
                    MaxValue = 2.5f,
                    DefaultValue = 1.3f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "rc_scoreOverlayMargin",
                    DisplayName = "Label Margin",
                    Description = "Margin between labels and values (px)",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "rc_scoreOverlayBgOpacity",
                    DisplayName = "Background Opacity",
                    Description = "Opacity of score overlay background (0=transparent, 1=opaque)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.1f
                },
                new ColorParameter
                {
                    Key = "rc_scoreOverlayColor",
                    DisplayName = "Score Color",
                    Description = "Color of score overlay",
                    DefaultValue = new Vector4(0f, 1f, 0f, 1f),
                    SupportsAlpha = false
                },
                new FloatParameter
                {
                    Key = "rc_scoreOverlayX",
                    DisplayName = "Score X Position",
                    Description = "Horizontal position of score (px from left)",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 70f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "rc_scoreOverlayY",
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
                    Key = "rc_timerDuration",
                    DisplayName = "Game Duration",
                    Description = "Duration of the game in seconds",
                    MinValue = 30f,
                    MaxValue = 300f,
                    DefaultValue = 90f,
                    Step = 15f
                },

                // ========== HOTKEY ==========
                new BoolParameter
                {
                    Key = "rc_enableResetHotkey",
                    DisplayName = "Enable Reset Hotkey",
                    Description = "Enable R key to reset the game",
                    DefaultValue = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new RetroCommandSettingsControl(effect);
}
