using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Circuit.UI;

namespace MouseEffects.Effects.Circuit;

public sealed class CircuitFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "circuit",
        Name = "Circuit",
        Description = "PCB-style circuit traces that grow from the mouse cursor like electronic veins",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Digital
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new CircuitEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Trace settings (cir_ prefix for Circuit)
        config.Set("cir_maxSegments", 512);
        config.Set("cir_traceCount", 12);
        config.Set("cir_growthSpeed", 150f);
        config.Set("cir_maxLength", 200f);
        config.Set("cir_branchProbability", 0.3f);

        // Appearance settings
        config.Set("cir_nodeSize", 2f);
        config.Set("cir_glowIntensity", 0.5f);
        config.Set("cir_glowAnimationEnabled", true);
        config.Set("cir_glowAnimationSpeed", 0.5f);
        config.Set("cir_glowMinIntensity", 0.3f);
        config.Set("cir_glowMaxIntensity", 1.0f);
        config.Set("cir_lineThickness", 1f);

        // Lifetime and spawn
        config.Set("cir_traceLifetime", 5f);
        config.Set("cir_spawnThreshold", 50f);

        // Color settings
        config.Set("cir_colorPreset", 5);  // Custom
        config.Set("cir_customColor", new Vector4(0f, 1f, 0f, 1f));
        config.Set("cir_rainbowEnabled", true);
        config.Set("cir_rainbowSpeed", 1.0f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Trace Settings
                new IntParameter
                {
                    Key = "cir_maxSegments",
                    DisplayName = "Max Segments",
                    Description = "Maximum number of circuit segments that can exist at once",
                    MinValue = 64,
                    MaxValue = 2048,
                    DefaultValue = 512
                },
                new IntParameter
                {
                    Key = "cir_traceCount",
                    DisplayName = "Trace Count",
                    Description = "Number of circuit traces spawned per event",
                    MinValue = 5,
                    MaxValue = 30,
                    DefaultValue = 12
                },
                new FloatParameter
                {
                    Key = "cir_growthSpeed",
                    DisplayName = "Growth Speed",
                    Description = "Speed at which traces grow (pixels/second)",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 150f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "cir_maxLength",
                    DisplayName = "Max Length",
                    Description = "Maximum length of each trace segment",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 200f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "cir_branchProbability",
                    DisplayName = "Branch Probability",
                    Description = "Probability of traces branching (0-1)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                // Appearance Settings
                new FloatParameter
                {
                    Key = "cir_nodeSize",
                    DisplayName = "Node Size",
                    Description = "Size of connection point nodes",
                    MinValue = 2f,
                    MaxValue = 10f,
                    DefaultValue = 2f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "cir_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the electronic glow",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "cir_glowAnimationEnabled",
                    DisplayName = "Animate Glow",
                    Description = "Enable glow intensity animation",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "cir_glowAnimationSpeed",
                    DisplayName = "Glow Animation Speed",
                    Description = "Speed of glow animation",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cir_glowMinIntensity",
                    DisplayName = "Min Glow",
                    Description = "Minimum glow intensity when animating",
                    MinValue = 0.1f,
                    MaxValue = 2.9f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cir_glowMaxIntensity",
                    DisplayName = "Max Glow",
                    Description = "Maximum glow intensity when animating",
                    MinValue = 0.2f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cir_lineThickness",
                    DisplayName = "Line Thickness",
                    Description = "Thickness of circuit trace lines",
                    MinValue = 1f,
                    MaxValue = 6f,
                    DefaultValue = 1f,
                    Step = 0.5f
                },

                // Timing Settings
                new FloatParameter
                {
                    Key = "cir_traceLifetime",
                    DisplayName = "Trace Lifetime",
                    Description = "How long traces persist (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cir_spawnThreshold",
                    DisplayName = "Spawn Threshold",
                    Description = "Distance mouse must move to spawn traces (pixels)",
                    MinValue = 20f,
                    MaxValue = 150f,
                    DefaultValue = 50f,
                    Step = 5f
                },

                // Color Settings
                new ChoiceParameter
                {
                    Key = "cir_colorPreset",
                    DisplayName = "Color Preset",
                    Description = "Choose a color theme for circuit traces",
                    Choices = ["Classic Green", "Cyan", "Gold PCB", "Orange", "Purple", "Custom"],
                    DefaultValue = "Custom"
                },
                new ColorParameter
                {
                    Key = "cir_customColor",
                    DisplayName = "Custom Color",
                    Description = "Custom color for circuit traces",
                    DefaultValue = new Vector4(0f, 1f, 0f, 1f),
                    SupportsAlpha = false
                },
                new BoolParameter
                {
                    Key = "cir_rainbowEnabled",
                    DisplayName = "Rainbow Mode",
                    Description = "Enable rainbow color cycling (Custom preset only)",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "cir_rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new CircuitSettingsControl(effect);
}
