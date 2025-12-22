using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.ProceduralSigil.UI;

namespace MouseEffects.Effects.ProceduralSigil;

public sealed class ProceduralSigilFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "procedural-sigil",
        Name = "Procedural Sigil",
        Description = "Magical sigil with procedural geometry, runes, and glowing energy",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new ProceduralSigilEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Style
        config.Set("sigilStyle", 2); // Moon

        // General
        config.Set("sigilAlpha", 0.65f);

        // Position
        config.Set("positionMode", 0); // FollowCursor
        config.Set("sigilRadius", 383f);
        config.Set("fadeDuration", 5.0f);

        // Appearance
        config.Set("lineThickness", 2.26f);
        config.Set("glowIntensity", 1.42f);
        config.Set("colorPreset", 6); // Custom

        // Colors (user's custom colors)
        config.Set("coreColor", new Vector4(0f, 0.502f, 0.251f, 1.0f));
        config.Set("midColor", new Vector4(0.463f, 0.502f, 0f, 1.0f));
        config.Set("edgeColor", new Vector4(1f, 0.302f, 0f, 1.0f));

        // Layers (all enabled by default)
        config.Set("layerFlags", 31u); // All layers

        // Animation
        config.Set("animationFlags", 7u); // All animations
        config.Set("rotationSpeed", 0.5f);
        config.Set("counterRotateLayers", true);
        config.Set("pulseSpeed", 1.0f);
        config.Set("pulseAmplitude", 0.3f);
        config.Set("morphAmount", 1.0f);
        config.Set("runeScrollSpeed", 0.16f);

        // Triangle Mandala specific
        config.Set("triangleLayers", 1);
        config.Set("zoomSpeed", 0.45f);
        config.Set("zoomAmount", 0.3f);
        config.Set("innerTriangles", 4);
        config.Set("fractalDepth", 3.06f);

        // Moon style specific
        config.Set("moonPhaseRotationSpeed", 0.21f);
        config.Set("zodiacRotationSpeed", -0.21f);
        config.Set("moonPhaseOffset", 0f);
        config.Set("treeOfLifeScale", 0.6f);
        config.Set("starfieldDensity", 0.5f);
        config.Set("cosmicGlowIntensity", 1.0f);

        // Energy particles
        config.Set("particleIntensity", 0.71f);
        config.Set("particleSpeed", 2.54f);
        config.Set("particleType", 3); // Mixed
        config.Set("particleEntropy", 0.92f);
        config.Set("particleSize", 0.41f);
        config.Set("fireRiseHeight", 1.79f);
        config.Set("electricitySpread", 1.34f);

        // Wind simulation
        config.Set("windEnabled", true);
        config.Set("windStrength", 0.53f);
        config.Set("windTurbulence", 0.52f);

        // Fire particle pool
        config.Set("fireParticleEnabled", true);
        config.Set("fireSpawnLocation", 1); // RuneBand
        config.Set("fireRenderOrder", 0); // BehindSigil
        config.Set("fireColorPalette", 1); // Vibrant Fire
        config.Set("fireCustomCoreColor", new Vector4(1.0f, 0.502f, 0.251f, 1.0f));
        config.Set("fireCustomMidColor", new Vector4(1.0f, 0.4f, 0.0f, 1.0f));
        config.Set("fireCustomEdgeColor", new Vector4(1.0f, 0f, 0f, 1.0f));
        config.Set("fireParticleAlpha", 0.68f);
        config.Set("fireParticleCount", 300);
        config.Set("fireSpawnRate", 30f);
        config.Set("fireParticleSize", 11.2f);
        config.Set("fireLifetime", 4.0f);
        config.Set("fireRiseSpeed", 124f);
        config.Set("fireTurbulence", 0.71f);
        config.Set("fireWindEnabled", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Style
                new ChoiceParameter
                {
                    Key = "sigilStyle",
                    DisplayName = "Sigil Style",
                    Description = "The visual style of the sigil",
                    Choices = ["Arcane Circle", "Triangle Mandala", "Moon"],
                    DefaultValue = "Moon"
                },

                // General
                new FloatParameter
                {
                    Key = "sigilAlpha",
                    DisplayName = "Sigil Alpha",
                    Description = "Overall opacity of the sigil",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.65f,
                    Step = 0.05f
                },

                // Position
                new ChoiceParameter
                {
                    Key = "positionMode",
                    DisplayName = "Position Mode",
                    Description = "How the sigil is positioned on screen",
                    Choices = ["Follow Cursor", "Screen Center", "Click to Summon", "Click at Cursor"],
                    DefaultValue = "Follow Cursor"
                },
                new FloatParameter
                {
                    Key = "sigilRadius",
                    DisplayName = "Sigil Size",
                    Description = "Radius of the sigil in pixels",
                    MinValue = 100f,
                    MaxValue = 800f,
                    DefaultValue = 383f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "fadeDuration",
                    DisplayName = "Fade Duration",
                    Description = "Duration before sigil fades out (for click modes)",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 5.0f,
                    Step = 0.1f
                },

                // Appearance
                new FloatParameter
                {
                    Key = "lineThickness",
                    DisplayName = "Line Thickness",
                    Description = "Thickness of sigil lines",
                    MinValue = 1f,
                    MaxValue = 5f,
                    DefaultValue = 2.26f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Brightness of the glow effect",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.42f,
                    Step = 0.1f
                },
                new ChoiceParameter
                {
                    Key = "colorPreset",
                    DisplayName = "Color Preset",
                    Description = "Preset color schemes for the sigil",
                    Choices = ["Shield of Fire", "Arcane Blue", "Dark Magic", "Holy Light", "Void", "Nature", "Custom"],
                    DefaultValue = "Custom"
                },
                new ColorParameter
                {
                    Key = "coreColor",
                    DisplayName = "Core Color",
                    Description = "Color at the center of the sigil",
                    DefaultValue = new Vector4(0f, 0.502f, 0.251f, 1.0f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "midColor",
                    DisplayName = "Mid Color",
                    Description = "Color in the middle rings",
                    DefaultValue = new Vector4(0.463f, 0.502f, 0f, 1.0f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "edgeColor",
                    DisplayName = "Edge Color",
                    Description = "Color at the outer edge",
                    DefaultValue = new Vector4(1f, 0.302f, 0f, 1.0f),
                    SupportsAlpha = false
                },

                // Layers
                new BoolParameter
                {
                    Key = "layerCenter",
                    DisplayName = "Center Geometry",
                    Description = "Show center geometric patterns",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "layerInner",
                    DisplayName = "Inner Lattice",
                    Description = "Show inner triangular lattice",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "layerMiddle",
                    DisplayName = "Middle Rings",
                    Description = "Show middle concentric rings",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "layerRunes",
                    DisplayName = "Rune Band",
                    Description = "Show outer rune symbols",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "layerGlow",
                    DisplayName = "Outer Glow",
                    Description = "Enable glow effect",
                    DefaultValue = true
                },

                // Animation
                new FloatParameter
                {
                    Key = "rotationSpeed",
                    DisplayName = "Rotation Speed",
                    Description = "Speed of layer rotation",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "counterRotateLayers",
                    DisplayName = "Counter-Rotate Layers",
                    Description = "Rotate layers in opposite directions",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "enablePulse",
                    DisplayName = "Pulse Effect",
                    Description = "Enable pulsing glow animation",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "pulseSpeed",
                    DisplayName = "Pulse Speed",
                    Description = "Speed of pulse animation",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "enableMorph",
                    DisplayName = "Morph Patterns",
                    Description = "Enable pattern morphing animation",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "runeScrollSpeed",
                    DisplayName = "Rune Scroll Speed",
                    Description = "Speed of rune scrolling",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.16f,
                    Step = 0.05f
                },

                // Triangle Mandala specific
                new IntParameter
                {
                    Key = "triangleLayers",
                    DisplayName = "Triangle Layers",
                    Description = "Number of nested triangle layers (Triangle Mandala style)",
                    MinValue = 1,
                    MaxValue = 5,
                    DefaultValue = 1
                },
                new FloatParameter
                {
                    Key = "zoomSpeed",
                    DisplayName = "Zoom Speed",
                    Description = "Speed of zooming animation (Triangle Mandala style)",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.45f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "zoomAmount",
                    DisplayName = "Zoom Amount",
                    Description = "Intensity of zooming effect (Triangle Mandala style)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new IntParameter
                {
                    Key = "innerTriangles",
                    DisplayName = "Inner Triangles",
                    Description = "Number of triangles in inner ring (Triangle Mandala style)",
                    MinValue = 2,
                    MaxValue = 8,
                    DefaultValue = 4
                },
                new FloatParameter
                {
                    Key = "fractalDepth",
                    DisplayName = "Fractal Depth",
                    Description = "Depth of fractal triangle patterns (Triangle Mandala style)",
                    MinValue = 1f,
                    MaxValue = 5f,
                    DefaultValue = 3.06f,
                    Step = 0.5f
                },

                // Moon style specific
                new FloatParameter
                {
                    Key = "moonPhaseRotationSpeed",
                    DisplayName = "Moon Phase Rotation",
                    Description = "Rotation speed of moon phases ring (Moon style)",
                    MinValue = -1f,
                    MaxValue = 1f,
                    DefaultValue = 0.21f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "zodiacRotationSpeed",
                    DisplayName = "Zodiac Rotation",
                    Description = "Rotation speed of zodiac ring (Moon style)",
                    MinValue = -1f,
                    MaxValue = 1f,
                    DefaultValue = -0.21f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "treeOfLifeScale",
                    DisplayName = "Tree of Life Size",
                    Description = "Scale of the Tree of Life at center (Moon style)",
                    MinValue = 0.2f,
                    MaxValue = 0.6f,
                    DefaultValue = 0.6f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "cosmicGlowIntensity",
                    DisplayName = "Cosmic Glow",
                    Description = "Intensity of cosmic glow effect (Moon style)",
                    MinValue = 0.5f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Energy particles
                new ChoiceParameter
                {
                    Key = "particleType",
                    DisplayName = "Particle Type",
                    Description = "Type of energy particles along sigil edges",
                    Choices = ["None", "Fire", "Electricity", "Mixed"],
                    DefaultValue = "Mixed"
                },
                new FloatParameter
                {
                    Key = "particleIntensity",
                    DisplayName = "Particle Intensity",
                    Description = "Intensity of energy particles (0 = off)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.71f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "particleSpeed",
                    DisplayName = "Particle Speed",
                    Description = "Animation speed of particles",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 2.54f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "particleEntropy",
                    DisplayName = "Particle Entropy",
                    Description = "Chaos and movement of particles (0 = static, 1 = chaotic)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.92f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "particleSize",
                    DisplayName = "Particle Size",
                    Description = "Size of energy particles",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 0.41f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "fireRiseHeight",
                    DisplayName = "Fire Rise Height",
                    Description = "How high fire particles rise before fading",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 1.79f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "electricitySpread",
                    DisplayName = "Electricity Spread",
                    Description = "How far electricity spreads from sigil edges",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1.34f,
                    Step = 0.1f
                },

                // Wind simulation
                new BoolParameter
                {
                    Key = "windEnabled",
                    DisplayName = "Wind Simulation",
                    Description = "Enable wind affecting fire particles",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "windStrength",
                    DisplayName = "Wind Strength",
                    Description = "How strongly wind pushes particles horizontally",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 0.53f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "windTurbulence",
                    DisplayName = "Wind Turbulence",
                    Description = "Randomness and gusts in the wind",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.52f,
                    Step = 0.1f
                },

                // Fire particle pool
                new BoolParameter
                {
                    Key = "fireParticleEnabled",
                    DisplayName = "Enable Fire Particles",
                    Description = "Enable fire particles rising from the sigil",
                    DefaultValue = true
                },
                new ChoiceParameter
                {
                    Key = "fireSpawnLocation",
                    DisplayName = "Spawn Location",
                    Description = "Where fire particles spawn from",
                    Choices = ["Inner Ring", "Rune Band"],
                    DefaultValue = "Rune Band"
                },
                new ChoiceParameter
                {
                    Key = "fireRenderOrder",
                    DisplayName = "Render Order",
                    Description = "Whether particles render behind or on top of sigil",
                    Choices = ["Behind Sigil", "On Top of Sigil"],
                    DefaultValue = "Behind Sigil"
                },
                new ChoiceParameter
                {
                    Key = "fireColorPalette",
                    DisplayName = "Color Palette",
                    Description = "Color scheme for fire particles",
                    Choices = ["Sigil Colors", "Vibrant Fire", "Ethereal", "Mystical Blue", "Magical Pink", "Poison Green", "Deep Crimson", "Custom"],
                    DefaultValue = "Vibrant Fire"
                },
                new ColorParameter
                {
                    Key = "fireCustomCoreColor",
                    DisplayName = "Custom Core Color",
                    Description = "Hot center color for custom palette",
                    DefaultValue = new Vector4(1.0f, 0.502f, 0.251f, 1.0f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "fireCustomMidColor",
                    DisplayName = "Custom Mid Color",
                    Description = "Middle color for custom palette",
                    DefaultValue = new Vector4(1.0f, 0.4f, 0.0f, 1.0f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "fireCustomEdgeColor",
                    DisplayName = "Custom Edge Color",
                    Description = "Cold edge color for custom palette",
                    DefaultValue = new Vector4(1.0f, 0f, 0f, 1.0f),
                    SupportsAlpha = false
                },
                new FloatParameter
                {
                    Key = "fireParticleAlpha",
                    DisplayName = "Particle Alpha",
                    Description = "Opacity of fire particles",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.68f,
                    Step = 0.05f
                },
                new IntParameter
                {
                    Key = "fireParticleCount",
                    DisplayName = "Fire Particle Count",
                    Description = "Maximum number of fire particles",
                    MinValue = 100,
                    MaxValue = 1000,
                    DefaultValue = 300
                },
                new FloatParameter
                {
                    Key = "fireSpawnRate",
                    DisplayName = "Fire Spawn Rate",
                    Description = "Particles spawned per second",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "fireParticleSize",
                    DisplayName = "Fire Particle Size",
                    Description = "Base size of fire particles in pixels",
                    MinValue = 2f,
                    MaxValue = 20f,
                    DefaultValue = 11.2f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "fireLifetime",
                    DisplayName = "Fire Lifetime",
                    Description = "How long fire particles live in seconds",
                    MinValue = 0.5f,
                    MaxValue = 4f,
                    DefaultValue = 4.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "fireRiseSpeed",
                    DisplayName = "Fire Rise Speed",
                    Description = "How fast fire particles rise in pixels per second",
                    MinValue = 30f,
                    MaxValue = 150f,
                    DefaultValue = 124f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "fireTurbulence",
                    DisplayName = "Fire Turbulence",
                    Description = "Horizontal randomness of fire particles",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.71f,
                    Step = 0.05f
                },
                new BoolParameter
                {
                    Key = "fireWindEnabled",
                    DisplayName = "Fire Uses Wind",
                    Description = "Whether fire particles are affected by wind settings",
                    DefaultValue = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new ProceduralSigilSettingsControl(effect);
}
