using System.Collections.Concurrent;

namespace MouseEffects.Core.Diagnostics;

/// <summary>
/// Simple file logger for debugging with queue-based async writing.
/// </summary>
public static class Logger
{
    private static readonly ConcurrentQueue<string> _logQueue = new();
    private static readonly Timer _flushTimer;
    private static string? _logPath;

    public static bool IsEnabled { get; set; } = true;

    static Logger()
    {
        // Flush queue every 100ms to balance responsiveness and I/O efficiency
        _flushTimer = new Timer(FlushQueue, null, 100, 100);
    }

    public static void Initialize(string logPath)
    {
        _logPath = logPath;

        // Clear previous log
        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }
    }

    public static void Log(string source, string message)
    {
        if (!IsEnabled || _logPath == null) return;

        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{source}] {message}";
        _logQueue.Enqueue(line);

        System.Diagnostics.Debug.WriteLine(line);
    }

    public static void Log(string message) => Log("App", message);

    public static void Error(string source, Exception ex)
    {
        Log(source, $"ERROR: {ex.GetType().Name}: {ex.Message}");
        Log(source, $"StackTrace: {ex.StackTrace}");
    }

    /// <summary>
    /// Forces immediate flush of queued log entries.
    /// Call before app shutdown to ensure all logs are written.
    /// </summary>
    public static void Flush()
    {
        FlushQueue(null);
    }

    private static void FlushQueue(object? state)
    {
        if (_logPath == null || _logQueue.IsEmpty) return;

        try
        {
            // Batch dequeue and write to minimize I/O operations
            var lines = new List<string>();
            while (_logQueue.TryDequeue(out var line))
            {
                lines.Add(line);
            }

            if (lines.Count > 0)
            {
                File.AppendAllLines(_logPath, lines);
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
