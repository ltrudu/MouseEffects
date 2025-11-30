namespace MouseEffects.Core.Time;

/// <summary>
/// Represents timing information for a single frame.
/// </summary>
public readonly struct GameTime
{
    /// <summary>Total time elapsed since application start.</summary>
    public TimeSpan TotalTime { get; init; }

    /// <summary>Time elapsed since the last frame.</summary>
    public TimeSpan DeltaTime { get; init; }

    /// <summary>Delta time in seconds (convenience property).</summary>
    public float DeltaSeconds => (float)DeltaTime.TotalSeconds;

    /// <summary>Total time in seconds (convenience property).</summary>
    public float TotalSeconds => (float)TotalTime.TotalSeconds;

    public GameTime(TimeSpan totalTime, TimeSpan deltaTime)
    {
        TotalTime = totalTime;
        DeltaTime = deltaTime;
    }
}
