using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Firework.UI;

namespace MouseEffects.Effects.Firework;

public sealed class FireworkFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "firework",
        Name = "Firework",
        Description = "Creates stunning firework explosions with colorful particles and trails",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Other
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new FireworkEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // General
        config.Set("maxParticles", 5500);
        config.Set("maxFireworks", 15);
        config.Set("particleLifespan", 3.3f);
        config.Set("spawnOnLeftClick", true);
        config.Set("spawnOnRightClick", true);
        config.Set("minParticlesPerFirework", 20);
        config.Set("maxParticlesPerFirework", 40);
        config.Set("clickExplosionForce", 359f);
        config.Set("spawnOnMove", true);
        config.Set("moveSpawnDistance", 101f);
        config.Set("moveExplosionForce", 289f);
        config.Set("minParticleSize", 3.6f);
        config.Set("maxParticleSize", 12f);
        config.Set("glowIntensity", 0.13f);
        config.Set("enableTrails", true);
        config.Set("trailLength", 1.36f);
        config.Set("gravity", 33f);
        config.Set("drag", 0.983f);
        config.Set("spreadAngle", 360f);

        // Firework colors
        config.Set("rainbowMode", true);
        config.Set("rainbowSpeed", 0.5f);
        config.Set("primaryColor", new Vector4(1f, 0.3f, 0.1f, 1f));
        config.Set("secondaryColor", new Vector4(1f, 0.8f, 0.2f, 1f));
        config.Set("useRandomColors", true);

        // Secondary explosion
        config.Set("enableSecondaryExplosion", true);
        config.Set("secondaryExplosionDelay", 0.82f);
        config.Set("secondaryParticleCount", 40);
        config.Set("secondaryExplosionForce", 100f);

        // Rocket
        config.Set("enableRocketMode", true);
        config.Set("rocketSpeed", 354f);
        config.Set("rocketMinAltitude", 0.1f);
        config.Set("rocketMaxAltitude", 0.3f);
        config.Set("rocketMaxFuseTime", 3.0f);
        config.Set("rocketSize", 8f);
        config.Set("rocketRainbowMode", true);
        config.Set("rocketRainbowSpeed", 0.5f);
        config.Set("rocketPrimaryColor", new Vector4(1f, 0.8f, 0.2f, 1f));
        config.Set("rocketSecondaryColor", new Vector4(1f, 0.4f, 0.1f, 1f));
        config.Set("rocketUseRandomColors", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // General Section
                new IntParameter
                {
                    Key = "maxParticles",
                    DisplayName = "Max Particles",
                    Description = "Maximum number of particles in the system",
                    MinValue = 1000,
                    MaxValue = 15000,
                    DefaultValue = 5000
                },
                new IntParameter
                {
                    Key = "maxFireworks",
                    DisplayName = "Max Fireworks",
                    Description = "Maximum number of simultaneous firework explosions",
                    MinValue = 1,
                    MaxValue = 200,
                    DefaultValue = 50
                },
                new FloatParameter
                {
                    Key = "particleLifespan",
                    DisplayName = "Particle Lifespan",
                    Description = "How long particles live (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.5f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "spawnOnLeftClick",
                    DisplayName = "Spawn on Left Click",
                    Description = "Create firework when left mouse button is clicked",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "spawnOnRightClick",
                    DisplayName = "Spawn on Right Click",
                    Description = "Create firework when right mouse button is clicked",
                    DefaultValue = false
                },
                new IntParameter
                {
                    Key = "minParticlesPerFirework",
                    DisplayName = "Min Particles/Firework",
                    Description = "Minimum number of particles per firework explosion",
                    MinValue = 10,
                    MaxValue = 500,
                    DefaultValue = 50
                },
                new IntParameter
                {
                    Key = "maxParticlesPerFirework",
                    DisplayName = "Max Particles/Firework",
                    Description = "Maximum number of particles per firework explosion",
                    MinValue = 10,
                    MaxValue = 500,
                    DefaultValue = 150
                },
                new FloatParameter
                {
                    Key = "clickExplosionForce",
                    DisplayName = "Explosion Force",
                    Description = "Initial velocity of particles (pixels/sec)",
                    MinValue = 50f,
                    MaxValue = 1000f,
                    DefaultValue = 300f,
                    Step = 10f
                },
                new BoolParameter
                {
                    Key = "spawnOnMove",
                    DisplayName = "Spawn on Mouse Move",
                    Description = "Create fireworks as the mouse moves",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "moveSpawnDistance",
                    DisplayName = "Move Spawn Distance",
                    Description = "Distance mouse must move before spawning firework (pixels)",
                    MinValue = 20f,
                    MaxValue = 500f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "moveExplosionForce",
                    DisplayName = "Move Explosion Force",
                    Description = "Initial velocity of particles on move (pixels/sec)",
                    MinValue = 30f,
                    MaxValue = 500f,
                    DefaultValue = 150f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "minParticleSize",
                    DisplayName = "Min Particle Size",
                    Description = "Minimum particle size in pixels",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 3f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "maxParticleSize",
                    DisplayName = "Max Particle Size",
                    Description = "Maximum particle size in pixels",
                    MinValue = 2f,
                    MaxValue = 50f,
                    DefaultValue = 8f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the glow effect around particles",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "enableTrails",
                    DisplayName = "Enable Trails",
                    Description = "Elongate particles in direction of movement",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "trailLength",
                    DisplayName = "Trail Length",
                    Description = "Length of particle trails",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "gravity",
                    DisplayName = "Gravity",
                    Description = "Downward acceleration (pixels/sec^2)",
                    MinValue = 0f,
                    MaxValue = 500f,
                    DefaultValue = 150f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "drag",
                    DisplayName = "Air Drag",
                    Description = "Velocity damping per frame (0.9 = heavy drag, 1.0 = no drag)",
                    MinValue = 0.9f,
                    MaxValue = 1f,
                    DefaultValue = 0.98f,
                    Step = 0.01f
                },
                new FloatParameter
                {
                    Key = "spreadAngle",
                    DisplayName = "Spread Angle",
                    Description = "Angular spread of explosion (360 = full circle)",
                    MinValue = 30f,
                    MaxValue = 360f,
                    DefaultValue = 360f,
                    Step = 15f
                },

                // Firework Colors Section
                new BoolParameter
                {
                    Key = "rainbowMode",
                    DisplayName = "Rainbow Mode",
                    Description = "Cycle through rainbow colors over time",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new ColorParameter
                {
                    Key = "primaryColor",
                    DisplayName = "Primary Color",
                    Description = "Main firework color (when not in rainbow mode)",
                    DefaultValue = new Vector4(1f, 0.3f, 0.1f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "secondaryColor",
                    DisplayName = "Secondary Color",
                    Description = "Secondary color for mixing",
                    DefaultValue = new Vector4(1f, 0.8f, 0.2f, 1f),
                    SupportsAlpha = false
                },
                new BoolParameter
                {
                    Key = "useRandomColors",
                    DisplayName = "Use Random Colors",
                    Description = "Randomize colors for each firework",
                    DefaultValue = true
                },

                // Secondary Explosion Section
                new BoolParameter
                {
                    Key = "enableSecondaryExplosion",
                    DisplayName = "Enable Secondary Explosion",
                    Description = "Particles explode into smaller particles",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "secondaryExplosionDelay",
                    DisplayName = "Secondary Delay",
                    Description = "Time before secondary explosion (seconds)",
                    MinValue = 0.2f,
                    MaxValue = 3f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },
                new IntParameter
                {
                    Key = "secondaryParticleCount",
                    DisplayName = "Secondary Particle Count",
                    Description = "Number of particles in secondary explosion",
                    MinValue = 5,
                    MaxValue = 100,
                    DefaultValue = 20
                },
                new FloatParameter
                {
                    Key = "secondaryExplosionForce",
                    DisplayName = "Secondary Explosion Force",
                    Description = "Force of secondary explosion (pixels/sec)",
                    MinValue = 20f,
                    MaxValue = 300f,
                    DefaultValue = 100f,
                    Step = 10f
                },

                // Rocket Mode Section
                new BoolParameter
                {
                    Key = "enableRocketMode",
                    DisplayName = "Enable Rocket Mode",
                    Description = "Particles launch upward then explode",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "rocketSpeed",
                    DisplayName = "Rocket Speed",
                    Description = "Upward launch speed of rockets (pixels/sec)",
                    MinValue = 100f,
                    MaxValue = 1500f,
                    DefaultValue = 500f,
                    Step = 50f
                },
                new FloatParameter
                {
                    Key = "rocketMinAltitude",
                    DisplayName = "Min Explosion Altitude",
                    Description = "Minimum altitude from top where rockets explode (% of screen)",
                    MinValue = 0.05f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.1f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "rocketMaxAltitude",
                    DisplayName = "Max Explosion Altitude",
                    Description = "Maximum altitude from top where rockets explode (% of screen)",
                    MinValue = 0.1f,
                    MaxValue = 0.8f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "rocketMaxFuseTime",
                    DisplayName = "Max Fuse Time",
                    Description = "Maximum time before forced explosion (safety fallback)",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 3.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "rocketSize",
                    DisplayName = "Rocket Size",
                    Description = "Size of the rocket particle",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 12f,
                    Step = 1f
                },
                new BoolParameter
                {
                    Key = "rocketRainbowMode",
                    DisplayName = "Rocket Rainbow Mode",
                    Description = "Cycle rocket colors through rainbow",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "rocketRainbowSpeed",
                    DisplayName = "Rocket Rainbow Speed",
                    Description = "Speed of rocket rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new ColorParameter
                {
                    Key = "rocketPrimaryColor",
                    DisplayName = "Rocket Primary Color",
                    Description = "Main rocket color (when not in rainbow mode)",
                    DefaultValue = new Vector4(1f, 0.8f, 0.2f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "rocketSecondaryColor",
                    DisplayName = "Rocket Secondary Color",
                    Description = "Secondary rocket color for mixing",
                    DefaultValue = new Vector4(1f, 0.4f, 0.1f, 1f),
                    SupportsAlpha = false
                },
                new BoolParameter
                {
                    Key = "rocketUseRandomColors",
                    DisplayName = "Rocket Random Colors",
                    Description = "Randomize colors for each rocket",
                    DefaultValue = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new FireworkSettingsControl(effect);
}
