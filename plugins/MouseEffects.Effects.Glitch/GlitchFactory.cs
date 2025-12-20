using MouseEffects.Core.Effects;
using MouseEffects.Effects.Glitch.UI;

namespace MouseEffects.Effects.Glitch;

public sealed class GlitchFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "glitch",
        Name = "Glitch",
        Description = "Creates digital corruption and distortion artifacts around the mouse cursor like a broken screen",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.VisualFilter
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new GlitchEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Effect parameters
        config.Set("radius", 300.0f);              // 100-500 pixels
        config.Set("intensity", 1.0f);             // 0.1-2.0
        config.Set("rgbSplitAmount", 0.02f);       // 0.0-0.05
        config.Set("scanLineFrequency", 8.0f);     // 1-20
        config.Set("blockSize", 23.5f);            // 5-50 pixels
        config.Set("noiseAmount", 0.0f);           // 0.0-1.0
        config.Set("glitchFrequency", 4.4f);       // 1-20 Hz

        // Effect toggles
        config.Set("movingBackgroundEnabled", false);
        config.Set("checkeredViewEnabled", false);
        config.Set("distortionEnabled", true);
        config.Set("rgbSplitEnabled", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new FloatParameter
                {
                    Key = "radius",
                    DisplayName = "Effect Radius",
                    Description = "Radius of the glitch effect around the cursor in pixels",
                    MinValue = 100f,
                    MaxValue = 500f,
                    DefaultValue = 300f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "intensity",
                    DisplayName = "Glitch Intensity",
                    Description = "Overall intensity of the glitch effect",
                    MinValue = 0.1f,
                    MaxValue = 2.0f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "rgbSplitAmount",
                    DisplayName = "RGB Split Amount",
                    Description = "Amount of chromatic aberration (RGB channel separation)",
                    MinValue = 0.0f,
                    MaxValue = 0.05f,
                    DefaultValue = 0.02f,
                    Step = 0.001f
                },
                new FloatParameter
                {
                    Key = "scanLineFrequency",
                    DisplayName = "Scan Line Frequency",
                    Description = "Frequency of horizontal scan line distortions",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "blockSize",
                    DisplayName = "Block Size",
                    Description = "Size of rectangular glitch blocks in pixels",
                    MinValue = 5f,
                    MaxValue = 50f,
                    DefaultValue = 23.5f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "noiseAmount",
                    DisplayName = "Noise Amount",
                    Description = "Amount of static/noise overlay",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "glitchFrequency",
                    DisplayName = "Glitch Frequency",
                    Description = "How often glitch effects change (Hz)",
                    MinValue = 1f,
                    MaxValue = 20f,
                    DefaultValue = 4.4f,
                    Step = 0.5f
                },
                new BoolParameter
                {
                    Key = "movingBackgroundEnabled",
                    DisplayName = "Moving Background",
                    Description = "Enable temporal flickering effect",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "checkeredViewEnabled",
                    DisplayName = "Checkered View",
                    Description = "Enable block color inversions and white pixel artifacts",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "distortionEnabled",
                    DisplayName = "Distortion",
                    Description = "Enable block and scan line distortion",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "rgbSplitEnabled",
                    DisplayName = "RGB Split",
                    Description = "Enable chromatic aberration effect",
                    DefaultValue = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new GlitchSettingsControl(effect);
}
