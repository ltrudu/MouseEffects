using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Retropede.UI;

namespace MouseEffects.Effects.Retropede;

public sealed class RetropedeFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "retropede",
        Name = "Retropede",
        Description = "Classic arcade Retropede game - shoot the retropede, avoid the spider!",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Interactive
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new RetropedeEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Cannon/Laser settings
        config.Set("mp_spawnOnLeftClick", true);
        config.Set("mp_spawnOnMove", true);
        config.Set("mp_moveFireThreshold", 50f);
        config.Set("mp_laserSpeed", 1000f);
        config.Set("mp_laserSize", 6f);
        config.Set("mp_cannonSize", 24f);
        config.Set("mp_playerZoneHeight", 150f);

        // Retropede settings
        config.Set("mp_baseSpeed", 100f);
        config.Set("mp_speedIncrement", 5f);
        config.Set("mp_startingSegments", 25);
        config.Set("mp_segmentSize", 20f);
        config.Set("mp_headColor", new Vector4(1f, 0.2f, 0.4f, 1f));
        config.Set("mp_bodyColor", new Vector4(1f, 0.4f, 0.6f, 1f));

        // Mushroom settings
        config.Set("mp_mushroomSize", 16f);
        config.Set("mp_mushroomHealth", 3);
        config.Set("mp_initialMushroomCount", 50);
        config.Set("mp_mushroomFreeZoneHeight", 130f);
        config.Set("mp_mushroomColor", new Vector4(0.4f, 1f, 0.4f, 1f));

        // Spider settings
        config.Set("mp_spiderEnabled", true);
        config.Set("mp_spiderSpawnRate", 8f);
        config.Set("mp_spiderSpeed", 120f);
        config.Set("mp_spiderSize", 28f);
        config.Set("mp_spiderColor", new Vector4(1f, 1f, 0.2f, 1f));

        // DDT settings
        config.Set("mp_ddtEnabled", true);
        config.Set("mp_ddtMaxOnField", 3);
        config.Set("mp_ddtExplosionRadius", 80f);
        config.Set("mp_ddtExplosionDuration", 1.5f);
        config.Set("mp_ddtColor", new Vector4(1f, 0f, 1f, 1f));
        config.Set("mp_ddtGasColor", new Vector4(0f, 1f, 0f, 1f));

        // Scoring settings
        config.Set("mp_scoreHead", 100);
        config.Set("mp_scoreBody", 10);
        config.Set("mp_scoreMushroom", 1);
        config.Set("mp_scoreSpiderClose", 900);
        config.Set("mp_scoreSpiderMedium", 600);
        config.Set("mp_scoreSpiderFar", 300);
        config.Set("mp_scoreDDTKill", 200);

        // Visual settings
        config.Set("mp_renderStyle", 1);
        config.Set("mp_glowIntensity", 1.5f);
        config.Set("mp_neonIntensity", 1.0f);
        config.Set("mp_retroScanlines", 0.3f);
        config.Set("mp_retroPixelScale", 2f);
        config.Set("mp_animSpeed", 1.5f);

        // Explosion settings
        config.Set("mp_explosionParticleCount", 30);
        config.Set("mp_explosionForce", 200f);
        config.Set("mp_explosionLifespan", 1.0f);

        // Score overlay settings
        config.Set("mp_showScoreOverlay", true);
        config.Set("mp_scoreOverlaySize", 32f);
        config.Set("mp_scoreOverlayX", 70f);
        config.Set("mp_scoreOverlayY", 50f);
        config.Set("mp_timerDuration", 30f);

        // Default high scores (stored as JSON, not shown in settings UI)
        config.Set("mp_highScoresJson", "[{\"PointsPerMinute\":2000,\"Date\":\"20/12/2025\"},{\"PointsPerMinute\":1500,\"Date\":\"20/12/2025\"},{\"PointsPerMinute\":1000,\"Date\":\"20/12/2025\"},{\"PointsPerMinute\":500,\"Date\":\"20/12/2025\"},{\"PointsPerMinute\":200,\"Date\":\"20/12/2025\"}]");

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // ========== CANNON/LASER SETTINGS ==========
                new BoolParameter
                {
                    Key = "mp_spawnOnLeftClick",
                    DisplayName = "Fire on Left Click",
                    Description = "Fire laser when left mouse button is clicked",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "mp_spawnOnMove",
                    DisplayName = "Fire While Moving",
                    Description = "Fire laser while moving the mouse",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "mp_moveFireThreshold",
                    DisplayName = "Move Fire Threshold",
                    Description = "Distance to travel before auto-firing when moving (px)",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 50f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "mp_laserSpeed",
                    DisplayName = "Laser Speed",
                    Description = "Speed of laser projectiles (pixels/sec)",
                    MinValue = 400f,
                    MaxValue = 1500f,
                    DefaultValue = 800f,
                    Step = 50f
                },
                new FloatParameter
                {
                    Key = "mp_laserSize",
                    DisplayName = "Laser Size",
                    Description = "Visual size of lasers (px)",
                    MinValue = 4f,
                    MaxValue = 12f,
                    DefaultValue = 6f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "mp_cannonSize",
                    DisplayName = "Cannon Size",
                    Description = "Visual size of cannon (px)",
                    MinValue = 16f,
                    MaxValue = 48f,
                    DefaultValue = 24f,
                    Step = 2f
                },
                new FloatParameter
                {
                    Key = "mp_playerZoneHeight",
                    DisplayName = "Player Zone Height",
                    Description = "Height of player zone from bottom (px)",
                    MinValue = 100f,
                    MaxValue = 400f,
                    DefaultValue = 200f,
                    Step = 25f
                },

                // ========== RETROPEDE SETTINGS ==========
                new FloatParameter
                {
                    Key = "mp_baseSpeed",
                    DisplayName = "Base Retropede Speed",
                    Description = "Starting speed of retropede (px/sec)",
                    MinValue = 30f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "mp_speedIncrement",
                    DisplayName = "Speed Increment",
                    Description = "Speed increase per wave (px/sec)",
                    MinValue = 0f,
                    MaxValue = 20f,
                    DefaultValue = 5f,
                    Step = 1f
                },
                new IntParameter
                {
                    Key = "mp_startingSegments",
                    DisplayName = "Starting Segments",
                    Description = "Number of segments in first wave",
                    MinValue = 5,
                    MaxValue = 50,
                    DefaultValue = 25
                },
                new FloatParameter
                {
                    Key = "mp_segmentSize",
                    DisplayName = "Segment Size",
                    Description = "Visual size of segments (px)",
                    MinValue = 12f,
                    MaxValue = 32f,
                    DefaultValue = 20f,
                    Step = 2f
                },
                new ColorParameter
                {
                    Key = "mp_headColor",
                    DisplayName = "Head Color",
                    Description = "Color of retropede head segment",
                    DefaultValue = new Vector4(1f, 0.2f, 0.4f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "mp_bodyColor",
                    DisplayName = "Body Color",
                    Description = "Color of retropede body segments",
                    DefaultValue = new Vector4(1f, 0.4f, 0.6f, 1f),
                    SupportsAlpha = false
                },

                // ========== MUSHROOM SETTINGS ==========
                new FloatParameter
                {
                    Key = "mp_mushroomSize",
                    DisplayName = "Mushroom Size",
                    Description = "Visual size of mushrooms (px)",
                    MinValue = 12f,
                    MaxValue = 24f,
                    DefaultValue = 16f,
                    Step = 1f
                },
                new IntParameter
                {
                    Key = "mp_mushroomHealth",
                    DisplayName = "Mushroom Health",
                    Description = "Hits required to destroy a mushroom",
                    MinValue = 1,
                    MaxValue = 4,
                    DefaultValue = 3
                },
                new IntParameter
                {
                    Key = "mp_initialMushroomCount",
                    DisplayName = "Initial Mushroom Count",
                    Description = "Number of random mushrooms at start",
                    MinValue = 10,
                    MaxValue = 100,
                    DefaultValue = 50
                },
                new FloatParameter
                {
                    Key = "mp_mushroomFreeZoneHeight",
                    DisplayName = "Mushroom Free Zone",
                    Description = "Top zone height where initial mushrooms won't spawn (px)",
                    MinValue = 0f,
                    MaxValue = 200f,
                    DefaultValue = 130f,
                    Step = 10f
                },
                new ColorParameter
                {
                    Key = "mp_mushroomColor",
                    DisplayName = "Mushroom Color",
                    Description = "Color of mushrooms",
                    DefaultValue = new Vector4(0.4f, 1f, 0.4f, 1f),
                    SupportsAlpha = false
                },

                // ========== SPIDER SETTINGS ==========
                new BoolParameter
                {
                    Key = "mp_spiderEnabled",
                    DisplayName = "Enable Spider",
                    Description = "Enable spider enemy",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "mp_spiderSpawnRate",
                    DisplayName = "Spider Spawn Rate",
                    Description = "Seconds between spider spawns",
                    MinValue = 3f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "mp_spiderSpeed",
                    DisplayName = "Spider Speed",
                    Description = "Movement speed of spider (px/sec)",
                    MinValue = 50f,
                    MaxValue = 200f,
                    DefaultValue = 120f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "mp_spiderSize",
                    DisplayName = "Spider Size",
                    Description = "Visual size of spider (px)",
                    MinValue = 20f,
                    MaxValue = 40f,
                    DefaultValue = 28f,
                    Step = 2f
                },
                new ColorParameter
                {
                    Key = "mp_spiderColor",
                    DisplayName = "Spider Color",
                    Description = "Color of spider",
                    DefaultValue = new Vector4(1f, 1f, 0.2f, 1f),
                    SupportsAlpha = false
                },

                // ========== DDT SETTINGS ==========
                new BoolParameter
                {
                    Key = "mp_ddtEnabled",
                    DisplayName = "Enable DDT Bombs",
                    Description = "Enable DDT bomb power-up",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "mp_ddtMaxOnField",
                    DisplayName = "Max DDT Bombs",
                    Description = "Maximum DDT bombs on screen",
                    MinValue = 1,
                    MaxValue = 6,
                    DefaultValue = 3
                },
                new FloatParameter
                {
                    Key = "mp_ddtExplosionRadius",
                    DisplayName = "DDT Explosion Radius",
                    Description = "Radius of DDT gas cloud (px)",
                    MinValue = 40f,
                    MaxValue = 150f,
                    DefaultValue = 80f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "mp_ddtExplosionDuration",
                    DisplayName = "DDT Gas Duration",
                    Description = "How long gas cloud persists (sec)",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.25f
                },
                new ColorParameter
                {
                    Key = "mp_ddtColor",
                    DisplayName = "DDT Bomb Color",
                    Description = "Color of DDT bomb",
                    DefaultValue = new Vector4(1f, 0f, 1f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "mp_ddtGasColor",
                    DisplayName = "DDT Gas Color",
                    Description = "Color of DDT gas cloud",
                    DefaultValue = new Vector4(0f, 1f, 0f, 1f),
                    SupportsAlpha = true
                },

                // ========== SCORING SETTINGS ==========
                new IntParameter
                {
                    Key = "mp_scoreHead",
                    DisplayName = "Head Score",
                    Description = "Points for retropede head",
                    MinValue = 50,
                    MaxValue = 500,
                    DefaultValue = 100
                },
                new IntParameter
                {
                    Key = "mp_scoreBody",
                    DisplayName = "Body Score",
                    Description = "Points for body segment",
                    MinValue = 5,
                    MaxValue = 50,
                    DefaultValue = 10
                },
                new IntParameter
                {
                    Key = "mp_scoreMushroom",
                    DisplayName = "Mushroom Score",
                    Description = "Points for mushroom",
                    MinValue = 1,
                    MaxValue = 10,
                    DefaultValue = 1
                },
                new IntParameter
                {
                    Key = "mp_scoreSpiderClose",
                    DisplayName = "Spider Close Range Score",
                    Description = "Points for spider kill at close range",
                    MinValue = 500,
                    MaxValue = 1500,
                    DefaultValue = 900
                },
                new IntParameter
                {
                    Key = "mp_scoreSpiderMedium",
                    DisplayName = "Spider Medium Range Score",
                    Description = "Points for spider kill at medium range",
                    MinValue = 300,
                    MaxValue = 900,
                    DefaultValue = 600
                },
                new IntParameter
                {
                    Key = "mp_scoreSpiderFar",
                    DisplayName = "Spider Far Range Score",
                    Description = "Points for spider kill at far range",
                    MinValue = 100,
                    MaxValue = 500,
                    DefaultValue = 300
                },
                new IntParameter
                {
                    Key = "mp_scoreDDTKill",
                    DisplayName = "DDT Kill Score",
                    Description = "Points per enemy killed by DDT",
                    MinValue = 100,
                    MaxValue = 500,
                    DefaultValue = 200
                },

                // ========== VISUAL SETTINGS ==========
                new IntParameter
                {
                    Key = "mp_renderStyle",
                    DisplayName = "Render Style",
                    Description = "0=Modern (glow), 1=Retro (scanlines)",
                    MinValue = 0,
                    MaxValue = 1,
                    DefaultValue = 1
                },
                new FloatParameter
                {
                    Key = "mp_glowIntensity",
                    DisplayName = "Glow Intensity (Modern)",
                    Description = "Glow intensity for modern style",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "mp_neonIntensity",
                    DisplayName = "Neon Intensity (Modern)",
                    Description = "Neon edge glow for modern style",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "mp_retroScanlines",
                    DisplayName = "Scanlines (Retro)",
                    Description = "Scanline intensity for retro style",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "mp_retroPixelScale",
                    DisplayName = "Pixel Scale (Retro)",
                    Description = "Pixel scaling for retro style",
                    MinValue = 1f,
                    MaxValue = 4f,
                    DefaultValue = 2f,
                    Step = 0.25f
                },
                new FloatParameter
                {
                    Key = "mp_animSpeed",
                    DisplayName = "Animation Speed",
                    Description = "Speed of animations",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.25f
                },

                // ========== EXPLOSION SETTINGS ==========
                new IntParameter
                {
                    Key = "mp_explosionParticleCount",
                    DisplayName = "Explosion Particles",
                    Description = "Number of particles per explosion",
                    MinValue = 10,
                    MaxValue = 100,
                    DefaultValue = 30
                },
                new FloatParameter
                {
                    Key = "mp_explosionForce",
                    DisplayName = "Explosion Force",
                    Description = "Force of explosion particles",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 200f,
                    Step = 25f
                },
                new FloatParameter
                {
                    Key = "mp_explosionLifespan",
                    DisplayName = "Explosion Lifespan",
                    Description = "How long explosion particles live (sec)",
                    MinValue = 0.3f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // ========== SCORE OVERLAY ==========
                new BoolParameter
                {
                    Key = "mp_showScoreOverlay",
                    DisplayName = "Show Score Overlay",
                    Description = "Display score on screen during gameplay",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "mp_scoreOverlaySize",
                    DisplayName = "Score Size",
                    Description = "Size of score digits on screen (px)",
                    MinValue = 16f,
                    MaxValue = 100f,
                    DefaultValue = 32f,
                    Step = 4f
                },
                new FloatParameter
                {
                    Key = "mp_scoreOverlayX",
                    DisplayName = "Score X Position",
                    Description = "Horizontal position of score (px from left)",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 70f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "mp_scoreOverlayY",
                    DisplayName = "Score Y Position",
                    Description = "Vertical position of score (px from top)",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 50f,
                    Step = 10f
                },

                // ========== TIMER ==========
                new FloatParameter
                {
                    Key = "mp_timerDuration",
                    DisplayName = "Game Duration",
                    Description = "Duration of the game in seconds",
                    MinValue = 30f,
                    MaxValue = 300f,
                    DefaultValue = 30f,
                    Step = 15f
                },

            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new RetropedeSettingsControl(effect);
}
