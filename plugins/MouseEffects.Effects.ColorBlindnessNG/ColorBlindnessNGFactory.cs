using MouseEffects.Core.Effects;
using MouseEffects.Effects.ColorBlindnessNG.UI;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Factory for creating ColorBlindnessNGEffect instances.
/// </summary>
public sealed class ColorBlindnessNGFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "color-blindness-ng",
        Name = "Color Blindness NG",
        Description = "Next-generation CVD simulation and correction. Simulation uses scientific Machado/Brettel matrices. Correction uses LUT-based color remapping for practical color enhancement.",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.VisualFilter
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create()
    {
        return new ColorBlindnessNGEffect();
    }

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Mode: 0=Simulation, 1=Correction
        config.Set("mode", 0);

        // Simulation settings
        config.Set("simulationAlgorithm", 0); // 0=Machado, 1=Strict
        config.Set("simulationFilterType", 0); // 0=None, 1-6=Machado filters, 7-12=Strict filters

        // Correction settings
        config.Set("correctionPreset", 0); // 0=Custom, 1-8=Presets
        config.Set("applicationMode", 0); // 0=Full Channel, 1=Dominant Only, 2=Threshold
        config.Set("gradientType", 0); // 0=Linear RGB, 1=Perceptual LAB, 2=HSL
        config.Set("threshold", 0.3f);

        // Red channel LUT
        config.Set("redEnabled", false);
        config.Set("redStrength", 1.0f);
        config.Set("redStartColor", "#FF0000");
        config.Set("redEndColor", "#00FFFF");

        // Green channel LUT
        config.Set("greenEnabled", false);
        config.Set("greenStrength", 1.0f);
        config.Set("greenStartColor", "#00FF00");
        config.Set("greenEndColor", "#00FFFF");

        // Blue channel LUT
        config.Set("blueEnabled", false);
        config.Set("blueStrength", 1.0f);
        config.Set("blueStartColor", "#0000FF");
        config.Set("blueEndColor", "#FFFF00");

        // Global intensity
        config.Set("intensity", 1.0f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new IntParameter
                {
                    Key = "mode",
                    DisplayName = "Mode",
                    Description = "0=Simulation (show what CVD people see), 1=Correction (help CVD users)",
                    MinValue = 0,
                    MaxValue = 1,
                    DefaultValue = 0
                },
                new IntParameter
                {
                    Key = "simulationAlgorithm",
                    DisplayName = "Simulation Algorithm",
                    Description = "0=Machado (RGB), 1=Strict (LMS)",
                    MinValue = 0,
                    MaxValue = 1,
                    DefaultValue = 0
                },
                new IntParameter
                {
                    Key = "simulationFilterType",
                    DisplayName = "Simulation Filter Type",
                    Description = "CVD type to simulate",
                    MinValue = 0,
                    MaxValue = 8,
                    DefaultValue = 0
                },
                new FloatParameter
                {
                    Key = "intensity",
                    DisplayName = "Intensity",
                    Description = "Overall effect intensity",
                    MinValue = 0.0f,
                    MaxValue = 1.0f,
                    DefaultValue = 1.0f,
                    Step = 0.05f
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new ColorBlindnessNGSettingsControl { DataContext = effect };
    }
}
