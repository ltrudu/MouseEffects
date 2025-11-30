namespace MouseEffects.Core.Diagnostics;

/// <summary>
/// Simple file logger for debugging.
/// </summary>
public static class Logger
{
    private static readonly object _lock = new();
    private static string? _logPath;

    public static bool IsEnabled { get; set; } = true;

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

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        System.Diagnostics.Debug.WriteLine(line);
    }

    public static void Log(string message) => Log("App", message);

    public static void Error(string source, Exception ex)
    {
        Log(source, $"ERROR: {ex.GetType().Name}: {ex.Message}");
        Log(source, $"StackTrace: {ex.StackTrace}");
    }
}
