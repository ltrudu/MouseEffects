using System.Numerics;
using System.Windows;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.DNAHelix.UI;

namespace MouseEffects.Effects.DNAHelix;

public class DNAHelixFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "dnahelix",
        Name = "DNA Helix",
        Description = "Animated double helix DNA structure around the mouse cursor with base pairs and 3D rotation",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new DNAHelixEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Helix structure
        config.Set("helixHeight", 400f);
        config.Set("helixRadius", 50f);
        config.Set("twistRate", 0.03f);
        config.Set("rotationSpeed", 1.0f);
        config.Set("strandThickness", 4.0f);
        config.Set("basePairCount", 12);
        config.Set("glowIntensity", 0.8f);

        // Strand 1 color (Blue #4169E1)
        config.Set("strand1ColorR", 0.255f);
        config.Set("strand1ColorG", 0.412f);
        config.Set("strand1ColorB", 0.882f);

        // Strand 2 color (Red #DC143C)
        config.Set("strand2ColorR", 0.863f);
        config.Set("strand2ColorG", 0.078f);
        config.Set("strand2ColorB", 0.235f);

        // Base pair color 1 (Green #32CD32)
        config.Set("basePair1ColorR", 0.196f);
        config.Set("basePair1ColorG", 0.804f);
        config.Set("basePair1ColorB", 0.196f);

        // Base pair color 2 (Yellow #FFD700)
        config.Set("basePair2ColorR", 1.0f);
        config.Set("basePair2ColorG", 0.843f);
        config.Set("basePair2ColorB", 0.0f);

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters = []
        };
    }

    public object? CreateSettingsControl(IEffect effect) => effect is DNAHelixEffect dnaEffect ? new DNAHelixSettingsControl(dnaEffect) : null;
}
