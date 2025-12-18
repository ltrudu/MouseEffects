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
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new CircuitEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Trace settings (cir_ prefix for Circuit)
        config.Set("cir_traceCount", 12);
        config.Set("cir_growthSpeed", 150f);
        config.Set("cir_maxLength", 200f);
        config.Set("cir_branchProbability", 0.3f);

        // Appearance settings
        config.Set("cir_nodeSize", 4f);
        config.Set("cir_glowIntensity", 1.5f);
        config.Set("cir_lineThickness", 2.5f);

        // Lifetime and spawn
        config.Set("cir_traceLifetime", 1.5f);
        config.Set("cir_spawnThreshold", 50f);

        // Color settings
        config.Set("cir_colorPreset", 0);  // Classic Green
        config.Set("cir_customColor", new Vector4(0f, 1f, 0f, 1f));

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
                    DefaultValue = 4f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "cir_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the electronic glow",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cir_lineThickness",
                    DisplayName = "Line Thickness",
                    Description = "Thickness of circuit trace lines",
                    MinValue = 1f,
                    MaxValue = 6f,
                    DefaultValue = 2.5f,
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
                    DefaultValue = 1.5f,
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
                    DefaultValue = "Classic Green"
                },
                new ColorParameter
                {
                    Key = "cir_customColor",
                    DisplayName = "Custom Color",
                    Description = "Custom color for circuit traces",
                    DefaultValue = new Vector4(0f, 1f, 0f, 1f),
                    SupportsAlpha = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new CircuitSettingsControl(effect);
}
