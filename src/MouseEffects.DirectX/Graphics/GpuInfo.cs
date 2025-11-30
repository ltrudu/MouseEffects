namespace MouseEffects.DirectX.Graphics;

/// <summary>
/// Information about a GPU adapter.
/// </summary>
public class GpuInfo
{
    public required string Name { get; init; }
    public int DedicatedMemoryMB { get; init; }
    public int Index { get; init; }

    public override string ToString() => $"{Name} ({DedicatedMemoryMB}MB)";
}
