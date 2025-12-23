using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

public class FireworkContext
{
    public required ParticlePool Pool { get; init; }
    public required IFireworkStyle CurrentStyle { get; set; }
    public required Func<Vector4> GetRainbowColor { get; init; }
    public required Func<Vector4> GetRandomColor { get; init; }
    public required Func<Vector4> GetPrimaryColor { get; init; }
    public required Func<Vector4> GetSecondaryColor { get; init; }
    public required Func<int, int, int> GetRandomInt { get; init; }
    public required Func<float> GetRandomFloat { get; init; }

    public float Time { get; set; }
    public float DeltaTime { get; set; }
    public Vector2 ViewportSize { get; set; }

    // Settings
    public bool UseRandomColors { get; set; }
    public bool RainbowMode { get; set; }
    public float MinParticleSize { get; set; } = 3f;
    public float MaxParticleSize { get; set; } = 8f;
    public float ParticleLifespan { get; set; } = 2.5f;
    public float SpreadAngle { get; set; } = 360f;
    public bool EnableSecondaryExplosion { get; set; } = true;
    public float SecondaryExplosionDelay { get; set; } = 0.8f;
    public int SecondaryParticleCount { get; set; } = 20;
    public float SecondaryExplosionForce { get; set; } = 100f;

    public Vector4 GetExplosionColor()
    {
        if (RainbowMode) return GetRainbowColor();
        if (UseRandomColors) return GetRandomColor();
        return GetPrimaryColor();
    }

    public float GetRandomSize()
    {
        return MinParticleSize + GetRandomFloat() * (MaxParticleSize - MinParticleSize);
    }

    public float GetRandomLifespan()
    {
        return ParticleLifespan * (0.7f + GetRandomFloat() * 0.3f);
    }
}
