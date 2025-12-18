using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.InkBlot.UI;

namespace MouseEffects.Effects.InkBlot;

public sealed class InkBlotFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "inkblot",
        Name = "Ink Blot",
        Description = "Spreading ink and watercolor drops that bloom from clicks or cursor movement",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new InkBlotEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Drop settings
        config.Set("dropSize", 60f);
        config.Set("spreadSpeed", 50f);
        config.Set("edgeIrregularity", 0.3f);
        config.Set("opacity", 0.7f);
        config.Set("lifetime", 3.0f);

        // Color mode (0 = Ink, 1 = Watercolor)
        config.Set("colorMode", 1);

        // Ink colors (index: 0=Black, 1=Blue, 2=Red, 3=Sepia)
        config.Set("inkColorIndex", 0);

        // Watercolor colors (index: 0=Blue, 1=Pink, 2=Green, 3=Purple, 4=Yellow)
        config.Set("watercolorIndex", 0);

        // Random color per blot
        config.Set("randomColor", true);

        // Trigger settings
        config.Set("spawnOnClick", true);
        config.Set("spawnOnMove", false);
        config.Set("moveDistance", 80f);

        // Performance
        config.Set("maxBlots", 30);
        config.Set("maxBlotsPerSecond", 20);

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
                    Key = "dropSize",
                    DisplayName = "Drop Size",
                    Description = "Maximum size of ink blots in pixels",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 60f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "spreadSpeed",
                    DisplayName = "Spread Speed",
                    Description = "Speed at which blots expand (pixels per second)",
                    MinValue = 10f,
                    MaxValue = 200f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "edgeIrregularity",
                    DisplayName = "Edge Irregularity",
                    Description = "How organic and wavy the blot edges are (0 = smooth, 1 = very organic)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "opacity",
                    DisplayName = "Opacity",
                    Description = "Base opacity of ink blots",
                    MinValue = 0.1f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "lifetime",
                    DisplayName = "Lifetime",
                    Description = "How long blots remain visible (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 3.0f,
                    Step = 0.5f
                },
                new ChoiceParameter
                {
                    Key = "colorMode",
                    DisplayName = "Color Mode",
                    Description = "Ink mode (dark, saturated) or Watercolor mode (soft, pastel)",
                    Choices = ["Ink", "Watercolor"],
                    DefaultValue = "Watercolor"
                },
                new ChoiceParameter
                {
                    Key = "inkColorIndex",
                    DisplayName = "Ink Color",
                    Description = "Color for ink mode",
                    Choices = ["Black", "Blue", "Red", "Sepia"],
                    DefaultValue = "Black"
                },
                new ChoiceParameter
                {
                    Key = "watercolorIndex",
                    DisplayName = "Watercolor Color",
                    Description = "Color for watercolor mode",
                    Choices = ["Soft Blue", "Soft Pink", "Soft Green", "Soft Purple", "Soft Yellow"],
                    DefaultValue = "Soft Blue"
                },
                new BoolParameter
                {
                    Key = "randomColor",
                    DisplayName = "Random Color",
                    Description = "Use random colors from the selected palette for each blot",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "spawnOnClick",
                    DisplayName = "Spawn on Click",
                    Description = "Create blots when clicking",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "spawnOnMove",
                    DisplayName = "Spawn on Move",
                    Description = "Create blots when moving the mouse",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "moveDistance",
                    DisplayName = "Move Distance",
                    Description = "Distance mouse must move to spawn a blot (pixels)",
                    MinValue = 20f,
                    MaxValue = 200f,
                    DefaultValue = 80f,
                    Step = 10f
                },
                new IntParameter
                {
                    Key = "maxBlots",
                    DisplayName = "Max Active Blots",
                    Description = "Maximum number of blots that can be active at once",
                    MinValue = 10,
                    MaxValue = 100,
                    DefaultValue = 30
                },
                new IntParameter
                {
                    Key = "maxBlotsPerSecond",
                    DisplayName = "Max Blots/Second",
                    Description = "Rate limiting for blot spawning",
                    MinValue = 5,
                    MaxValue = 50,
                    DefaultValue = 20
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new InkBlotSettingsControl(effect);
}
