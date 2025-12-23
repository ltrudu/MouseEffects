using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Random style - variety mode.
/// Each explosion randomly picks one of the other styles.
/// </summary>
public class RandomStyle : IFireworkStyle
{
    public string Name => "Random";
    public string Description => "Each explosion randomly selects from all available styles";
    public int StyleId => 5;

    // Cache of available styles (excluding Random itself)
    private readonly IFireworkStyle[] _availableStyles;
    private IFireworkStyle? _currentStyle;

    public RandomStyle()
    {
        _availableStyles = new IFireworkStyle[]
        {
            new ClassicBurstStyle(),
            new SpinnerStyle(),
            new WillowStyle(),
            new CracklingStyle(),
            new ChrysanthemumStyle(),
            new BrocadeStyle(),
            new CometStyle(),
            new CrossetteStyle(),
            new PalmStyle(),
            new PeonyStyle(),
            new PearlsStyle(),
            new FishStyle(),
            new GreenBeesStyle(),
            new PistilStyle(),
            new StarsStyle(),
            new TailStyle(),
            new StrobeStyle(),
            new GlitterStyle()
        };
    }

    /// <summary>
    /// Gets the style that was randomly selected for the current/last explosion.
    /// </summary>
    public IFireworkStyle? CurrentRandomStyle => _currentStyle;

    public void SpawnExplosion(FireworkContext ctx, Vector2 position, float force, Vector4 color, int particleCount, bool isSecondary)
    {
        // Pick a random style for each new explosion (not for secondary)
        if (!isSecondary)
        {
            int styleIndex = ctx.GetRandomInt(0, _availableStyles.Length);
            _currentStyle = _availableStyles[styleIndex];
        }

        // Use the selected style to spawn the explosion
        _currentStyle?.SpawnExplosion(ctx, position, force, color, particleCount, isSecondary);
    }

    public void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Delegate to the style that created this particle
        var style = GetStyleById(particle.StyleId);
        style?.UpdateParticle(ref particle, dt, time);
    }

    public void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        // Delegate to the style that created this particle
        var style = GetStyleById(particle.StyleId);
        style?.FillStyleData(ref gpu, in particle);
    }

    private IFireworkStyle? GetStyleById(int styleId)
    {
        return styleId switch
        {
            0 => _availableStyles[0],   // Classic Burst
            1 => _availableStyles[1],   // Spinner
            2 => _availableStyles[2],   // Willow
            3 => _availableStyles[3],   // Crackling
            4 => _availableStyles[4],   // Chrysanthemum
            6 => _availableStyles[5],   // Brocade
            7 => _availableStyles[6],   // Comet
            8 => _availableStyles[7],   // Crossette
            9 => _availableStyles[8],   // Palm
            10 => _availableStyles[9],  // Peony
            11 => _availableStyles[10], // Pearls
            12 => _availableStyles[11], // Fish
            13 => _availableStyles[12], // Green Bees
            14 => _availableStyles[13], // Pistil
            15 => _availableStyles[14], // Stars
            16 => _availableStyles[15], // Tail
            17 => _availableStyles[16], // Strobe
            18 => _availableStyles[17], // Glitter
            _ => _availableStyles[0]    // Default to Classic
        };
    }

    public StyleDefaults GetDefaults()
    {
        // Return balanced defaults that work reasonably for any style
        return new StyleDefaults
        {
            ParticleLifespan = 2.5f,
            Gravity = 150f,
            Drag = 0.98f,
            MinParticlesPerFirework = 60,
            MaxParticlesPerFirework = 120,
            ExplosionForce = 280f,
            MinParticleSize = 3f,
            MaxParticleSize = 7f,
            SpreadAngle = 360f,
            EnableSecondaryExplosion = false  // Disable to avoid complications
        };
    }

    public IEnumerable<StyleParameter> GetParameters()
    {
        // Random style has no parameters - it uses whatever the selected style needs
        yield break;
    }

    public void SetParameter(string key, object value)
    {
        // Forward to all styles so they're configured if selected
        foreach (var style in _availableStyles)
            style.SetParameter(key, value);
    }

    public object? GetParameter(string key)
    {
        // Return from current style if available
        return _currentStyle?.GetParameter(key);
    }

    /// <summary>
    /// Gets a reference to a specific style for configuration.
    /// </summary>
    public IFireworkStyle GetStyleInstance(int index)
    {
        if (index >= 0 && index < _availableStyles.Length)
            return _availableStyles[index];
        return _availableStyles[0];
    }

    // Trail particle spawning - delegate to current style
    public bool HasTrailParticles => _currentStyle?.HasTrailParticles ?? false;

    public bool ShouldSpawnTrail(ref FireworkParticle particle, float dt)
    {
        var style = GetStyleById(particle.StyleId);
        return style?.ShouldSpawnTrail(ref particle, dt) ?? false;
    }

    public FireworkParticle CreateTrailParticle(in FireworkParticle parent, FireworkContext ctx)
    {
        var style = GetStyleById(parent.StyleId);
        return style?.CreateTrailParticle(in parent, ctx) ?? new FireworkParticle();
    }
}
