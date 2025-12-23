using System.Numerics;

namespace MouseEffects.Effects.Firework.Core;

public struct FireworkParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector4 Color;
    public float Size;
    public float Life;
    public float MaxLife;
    public bool CanExplode;
    public bool HasExploded;

    // Style-specific data
    public float StyleData1;  // Angular velocity (Spinner), flash phase (Crackling)
    public float StyleData2;  // Spin radius (Spinner), flash frequency (Crackling)
    public float StyleData3;  // Spawn time, jitter seed
    public int StyleId;       // Which style created this particle
}
