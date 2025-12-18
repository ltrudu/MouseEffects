using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.CrystalGrowth.UI;

namespace MouseEffects.Effects.CrystalGrowth;

public sealed class CrystalGrowthFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "crystal-growth",
        Name = "Crystal Growth",
        Description = "Ice/crystal structures that grow from mouse clicks with geometric, angular branches",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new CrystalGrowthEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Crystal settings (cg_ prefix for CrystalGrowth)
        config.Set("cg_crystalsPerClick", 3);
        config.Set("cg_growthSpeed", 120f);
        config.Set("cg_maxSize", 100f);
        config.Set("cg_branchProbability", 0.7f);
        config.Set("cg_maxGenerations", 3);

        // Appearance settings
        config.Set("cg_branchThickness", 1.5f);
        config.Set("cg_glowIntensity", 1.0f);
        config.Set("cg_sparkleIntensity", 1.2f);

        // Lifetime
        config.Set("cg_lifetime", 2.5f);

        // Color settings
        config.Set("cg_colorPreset", 0);  // Ice Blue
        config.Set("cg_customColor", new Vector4(0.53f, 0.81f, 0.92f, 1f));

        // Trigger settings
        config.Set("cg_leftClickEnabled", true);
        config.Set("cg_rightClickEnabled", true);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Crystal Settings
                new IntParameter
                {
                    Key = "cg_crystalsPerClick",
                    DisplayName = "Crystals Per Click",
                    Description = "Number of main crystal branches spawned per click",
                    MinValue = 2,
                    MaxValue = 8,
                    DefaultValue = 3
                },
                new FloatParameter
                {
                    Key = "cg_growthSpeed",
                    DisplayName = "Growth Speed",
                    Description = "Speed at which crystals grow (pixels/second)",
                    MinValue = 30f,
                    MaxValue = 300f,
                    DefaultValue = 120f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "cg_maxSize",
                    DisplayName = "Max Size",
                    Description = "Maximum length of crystal branches (pixels)",
                    MinValue = 30f,
                    MaxValue = 250f,
                    DefaultValue = 100f,
                    Step = 10f
                },
                new FloatParameter
                {
                    Key = "cg_branchProbability",
                    DisplayName = "Branch Probability",
                    Description = "Probability of crystals branching (0-1)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.7f,
                    Step = 0.1f
                },
                new IntParameter
                {
                    Key = "cg_maxGenerations",
                    DisplayName = "Max Generations",
                    Description = "Maximum number of branch generations",
                    MinValue = 1,
                    MaxValue = 5,
                    DefaultValue = 3
                },

                // Appearance Settings
                new FloatParameter
                {
                    Key = "cg_branchThickness",
                    DisplayName = "Branch Thickness",
                    Description = "Thickness of crystal branches",
                    MinValue = 0.5f,
                    MaxValue = 4f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cg_glowIntensity",
                    DisplayName = "Glow Intensity",
                    Description = "Intensity of the crystal glow",
                    MinValue = 0.2f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "cg_sparkleIntensity",
                    DisplayName = "Sparkle Intensity",
                    Description = "Intensity of light refraction sparkles",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.2f,
                    Step = 0.1f
                },

                // Timing Settings
                new FloatParameter
                {
                    Key = "cg_lifetime",
                    DisplayName = "Crystal Lifetime",
                    Description = "How long crystals persist after growth (seconds)",
                    MinValue = 0.5f,
                    MaxValue = 10f,
                    DefaultValue = 2.5f,
                    Step = 0.1f
                },

                // Color Settings
                new ChoiceParameter
                {
                    Key = "cg_colorPreset",
                    DisplayName = "Crystal Color",
                    Description = "Choose a color for the crystals",
                    Choices = ["Ice Blue", "Amethyst", "Emerald", "Diamond", "Custom"],
                    DefaultValue = "Ice Blue"
                },
                new ColorParameter
                {
                    Key = "cg_customColor",
                    DisplayName = "Custom Color",
                    Description = "Custom color for crystals",
                    DefaultValue = new Vector4(0.53f, 0.81f, 0.92f, 1f),
                    SupportsAlpha = false
                },

                // Trigger Settings
                new BoolParameter
                {
                    Key = "cg_leftClickEnabled",
                    DisplayName = "Left Click",
                    Description = "Spawn crystals on left click",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "cg_rightClickEnabled",
                    DisplayName = "Right Click",
                    Description = "Spawn crystals on right click",
                    DefaultValue = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new CrystalGrowthSettingsControl(effect);
}
