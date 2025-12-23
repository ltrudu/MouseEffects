using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

public interface IFireworkStyle
{
    string Name { get; }
    string Description { get; }
    int StyleId { get; }  // Unique ID for shader

    // Spawn particles for an explosion
    void SpawnExplosion(FireworkContext ctx, Vector2 position, float force, Vector4 color, int particleCount, bool isSecondary);

    // Update a particle each frame (style-specific behavior)
    void UpdateParticle(ref FireworkParticle particle, float dt, float time);

    // Get style-specific shader data for a particle
    void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle);

    // Style-specific defaults
    StyleDefaults GetDefaults();

    // Style-specific UI parameters (for settings panel)
    IEnumerable<StyleParameter> GetParameters();

    // Apply a parameter value from UI
    void SetParameter(string key, object value);

    // Get current parameter value
    object? GetParameter(string key);

    // Trail particle spawning - returns true if style spawns trail particles
    bool HasTrailParticles { get; }

    // Check if a trail particle should spawn for this particle
    bool ShouldSpawnTrail(ref FireworkParticle particle, float dt);

    // Create a trail particle from a parent particle
    FireworkParticle CreateTrailParticle(in FireworkParticle parent, FireworkContext ctx);
}
