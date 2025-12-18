using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.MagneticField.UI;

namespace MouseEffects.Effects.MagneticField;

public sealed class MagneticFieldFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "magneticfield",
        Name = "Magnetic Field",
        Description = "Visualization of magnetic field lines emanating from the mouse cursor with dipole pattern",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Physics
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new MagneticFieldEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Field parameters
        config.Set("mf_lineCount", 16);
        config.Set("mf_fieldStrength", 1.0f);
        config.Set("mf_fieldCurvature", 1.5f);
        config.Set("mf_effectRadius", 300f);

        // Animation
        config.Set("mf_animationSpeed", 1.0f);
        config.Set("mf_flowScale", 0.05f);
        config.Set("mf_flowSpeed", 1.0f);

        // Visual effects
        config.Set("mf_lineThickness", 2.0f);
        config.Set("mf_glowIntensity", 1.5f);

        // Dual pole mode
        config.Set("mf_dualPoleMode", false);
        config.Set("mf_poleSeparation", 200f);

        // Colors
        config.Set("mf_colorMode", 0); // 0=NorthSouth, 1=Unified, 2=Custom
        config.Set("mf_northColor", new Vector4(0.255f, 0.412f, 0.882f, 1f)); // #4169E1 Blue
        config.Set("mf_southColor", new Vector4(0.863f, 0.078f, 0.235f, 1f)); // #DC143C Red
        config.Set("mf_unifiedColor", new Vector4(0f, 1f, 1f, 1f)); // #00FFFF Cyan

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Field Parameters
                new IntParameter
                {
                    Key = "mf_lineCount",
                    DisplayName = "Field Line Count",
                    Description = "Number of magnetic field lines radiating from cursor",
                    MinValue = 8,
                    MaxValue = 32,
                    DefaultValue = 16
                },
                new FloatParameter
                {
                    Key = "mf_fieldStrength",
                    DisplayName = "Field Strength",
                    Description = "Strength of magnetic field affecting line curvature",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "mf_fieldCurvature",
                    DisplayName = "Field Curvature",
                    Description = "How much field lines curve (dipole effect)",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "mf_effectRadius",
                    DisplayName = "Effect Radius",
                    Description = "Maximum radius of magnetic field effect in pixels",
                    MinValue = 100f,
                    MaxValue = 500f,
                    DefaultValue = 300f,
                    Step = 10f
                },

                // Animation
                new FloatParameter
                {
                    Key = "mf_animationSpeed",
                    DisplayName = "Animation Speed",
                    Description = "Speed of field line flow animation",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "mf_flowScale",
                    DisplayName = "Flow Pattern Scale",
                    Description = "Scale of the flowing pattern along field lines",
                    MinValue = 0.01f,
                    MaxValue = 0.2f,
                    DefaultValue = 0.05f,
                    Step = 0.01f
                },
                new FloatParameter
                {
                    Key = "mf_flowSpeed",
                    DisplayName = "Flow Speed",
                    Description = "Speed of energy flow along field lines",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Visual Effects
                new FloatParameter
                {
                    Key = "mf_lineThickness",
                    DisplayName = "Line Thickness",
                    Description = "Thickness of field lines",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "mf_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of field line glow",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Dual Pole Mode
                new BoolParameter
                {
                    Key = "mf_dualPoleMode",
                    DisplayName = "Dual Pole Mode",
                    Description = "Enable second magnetic pole (N-S dipole)",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "mf_poleSeparation",
                    DisplayName = "Pole Separation",
                    Description = "Distance between North and South poles in dual mode",
                    MinValue = 50f,
                    MaxValue = 400f,
                    DefaultValue = 200f,
                    Step = 10f
                },

                // Colors
                new ChoiceParameter
                {
                    Key = "mf_colorMode",
                    DisplayName = "Color Mode",
                    Description = "Field line color scheme",
                    Choices = ["North/South Colors", "Unified Color", "Custom Colors"],
                    DefaultValue = "North/South Colors"
                },
                new ColorParameter
                {
                    Key = "mf_northColor",
                    DisplayName = "North Pole Color",
                    Description = "Color for north magnetic pole field lines",
                    DefaultValue = new Vector4(0.255f, 0.412f, 0.882f, 1f), // #4169E1 Blue
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "mf_southColor",
                    DisplayName = "South Pole Color",
                    Description = "Color for south magnetic pole field lines",
                    DefaultValue = new Vector4(0.863f, 0.078f, 0.235f, 1f), // #DC143C Red
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "mf_unifiedColor",
                    DisplayName = "Unified Color",
                    Description = "Single color for all field lines (Unified mode)",
                    DefaultValue = new Vector4(0f, 1f, 1f, 1f), // #00FFFF Cyan
                    SupportsAlpha = false
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new MagneticFieldSettingsControl(effect);
}
