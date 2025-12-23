using System.Numerics;
using System.Runtime.InteropServices;

namespace MouseEffects.Effects.Firework.Core;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct ParticleGPU
{
    public Vector2 Position;      // 8
    public Vector2 Velocity;      // 8
    public Vector4 Color;         // 16 = 32
    public float Size;            // 4
    public float Life;            // 4
    public float MaxLife;         // 4
    public float StyleData1;      // 4 = 48 (angular velocity / flash phase)
    public float StyleData2;      // 4 (spin radius / flash frequency)
    public float StyleData3;      // 4 (spawn time / jitter seed)
    public uint StyleFlags;       // 4 (style ID in low bits, flags in high bits)
    public float Padding;         // 4 = 64
}
