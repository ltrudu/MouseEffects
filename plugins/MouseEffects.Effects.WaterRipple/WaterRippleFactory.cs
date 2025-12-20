using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.WaterRipple.UI;

namespace MouseEffects.Effects.WaterRipple;

/// <summary>
/// Factory for creating WaterRippleEffect instances.
/// </summary>
public sealed class WaterRippleFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "water-ripple",
        Name = "Water Ripple",
        Description = "Creates expanding water ripples on click that distort the screen with realistic wave interference",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.VisualFilter
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new WaterRippleEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        // General settings
        config.Set("maxRipples", 100);
        config.Set("rippleLifespan", 4.1f);
        config.Set("waveSpeed", 887f);
        config.Set("wavelength", 92f);
        config.Set("damping", 5.1f);
        // Click settings
        config.Set("spawnOnLeftClick", true);
        config.Set("spawnOnRightClick", false);
        config.Set("clickMinAmplitude", 71f);
        config.Set("clickMaxAmplitude", 269f);
        // Mouse move settings
        config.Set("spawnOnMove", true);
        config.Set("moveSpawnDistance", 27f);
        config.Set("moveMinAmplitude", 4f);
        config.Set("moveMaxAmplitude", 33f);
        config.Set("moveRippleLifespan", 2.0f);
        config.Set("moveWaveSpeed", 300f);
        config.Set("moveWavelength", 30f);
        config.Set("moveDamping", 3.0f);
        // Grid settings
        config.Set("enableGrid", false);
        config.Set("gridSpacing", 20f);
        config.Set("gridThickness", 2.0f);
        config.Set("gridColor", new Vector4(0.0f, 1.0f, 0.5f, 0.8f));
        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // General settings
                new IntParameter
                {
                    Key = "maxRipples",
                    DisplayName = "Max Ripples",
                    Description = "Maximum number of ripples at the same time",
                    MinValue = 1,
                    MaxValue = 200,
                    DefaultValue = 50
                },
                new FloatParameter
                {
                    Key = "rippleLifespan",
                    DisplayName = "Ripple Lifespan",
                    Description = "Seconds before ripple fades out",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 3.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "waveSpeed",
                    DisplayName = "Wave Speed",
                    Description = "Speed of ripple expansion (pixels/second)",
                    MinValue = 50f,
                    MaxValue = 1000f,
                    DefaultValue = 200f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "wavelength",
                    DisplayName = "Wavelength",
                    Description = "Distance between wave peaks (pixels)",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "damping",
                    DisplayName = "Damping",
                    Description = "How quickly waves lose energy with distance",
                    MinValue = 0.1f,
                    MaxValue = 10f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                // Click settings
                new BoolParameter
                {
                    Key = "spawnOnLeftClick",
                    DisplayName = "Spawn on Left Click",
                    Description = "Create ripple when left mouse button is clicked",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "spawnOnRightClick",
                    DisplayName = "Spawn on Right Click",
                    Description = "Create ripple when right mouse button is clicked",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "clickMinAmplitude",
                    DisplayName = "Click Min Amplitude",
                    Description = "Minimum wave height for click ripples (pixels)",
                    MinValue = 1f,
                    MaxValue = 100f,
                    DefaultValue = 5f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "clickMaxAmplitude",
                    DisplayName = "Click Max Amplitude",
                    Description = "Maximum wave height for click ripples (pixels)",
                    MinValue = 5f,
                    MaxValue = 300f,
                    DefaultValue = 20f,
                    Step = 1f
                },
                // Mouse move settings
                new BoolParameter
                {
                    Key = "spawnOnMove",
                    DisplayName = "Spawn on Mouse Move",
                    Description = "Create ripples as the mouse moves",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "moveSpawnDistance",
                    DisplayName = "Move Spawn Distance",
                    Description = "Minimum mouse movement before spawning a new ripple (pixels)",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "moveMinAmplitude",
                    DisplayName = "Move Min Amplitude",
                    Description = "Minimum wave height for movement ripples (pixels)",
                    MinValue = 1f,
                    MaxValue = 50f,
                    DefaultValue = 3f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "moveMaxAmplitude",
                    DisplayName = "Move Max Amplitude",
                    Description = "Maximum wave height for movement ripples (pixels)",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 10f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "moveRippleLifespan",
                    DisplayName = "Move Ripple Lifespan",
                    Description = "Seconds before movement ripple fades out",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "moveWaveSpeed",
                    DisplayName = "Move Wave Speed",
                    Description = "Speed of movement ripple expansion (pixels/second)",
                    MinValue = 50f,
                    MaxValue = 1000f,
                    DefaultValue = 300f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "moveWavelength",
                    DisplayName = "Move Wavelength",
                    Description = "Distance between wave peaks for movement ripples (pixels)",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "moveDamping",
                    DisplayName = "Move Damping",
                    Description = "How quickly movement waves lose energy with distance",
                    MinValue = 0.1f,
                    MaxValue = 10f,
                    DefaultValue = 3.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "enableGrid",
                    DisplayName = "Show Grid Overlay",
                    Description = "Display a grid to visualize the distortion",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "gridSpacing",
                    DisplayName = "Grid Spacing",
                    Description = "Distance between grid lines (pixels)",
                    MinValue = 10f,
                    MaxValue = 100f,
                    DefaultValue = 30f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "gridThickness",
                    DisplayName = "Grid Thickness",
                    Description = "Thickness of grid lines (pixels)",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.5f
                },
                new ColorParameter
                {
                    Key = "gridColor",
                    DisplayName = "Grid Color",
                    Description = "Color of the grid overlay",
                    DefaultValue = new Vector4(0.0f, 1.0f, 0.5f, 0.8f),
                    SupportsAlpha = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new WaterRippleSettingsControl(effect);
    }
}
