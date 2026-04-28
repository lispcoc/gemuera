// Gemuera — Simple file-based error logger
// Writes startup, initialization, and runtime errors to gemuera_runtime.log
// next to the project binary (or a writable fallback path).
using System;
using System.IO;
using System.Text;

namespace Gemuera;

/// <summary>
/// Thread-safe, synchronous file logger.
/// Call GemueraLogger.Init() once at startup, then use
/// GemueraLogger.Log / LogWarn / LogError / LogException.
/// </summary>
public static class GemueraLogger
{
    private static string _logPath = "";
    private static readonly object _lock = new();

    // ----------------------------------------------------------------
    // Init
    // ----------------------------------------------------------------

    /// <summary>
    /// Opens (or creates) the log file.
    /// Must be called before any other methods.
    /// </summary>
    public static void Init(string logFilePath)
    {
        _logPath = logFilePath;
        try
        {
            string? dir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Write header
            File.WriteAllText(logFilePath,
                $"=== Gemuera Runtime Log ===\n" +
                $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"----------------------------------------\n",
                Encoding.UTF8);
        }
        catch (Exception ex)
        {
            // If we can't open the log file, fall back silently
            Console.Error.WriteLine($"[GemueraLogger] Cannot create log file: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------
    // Logging methods
    // ----------------------------------------------------------------

    public static void Log(string message)     => Write("INFO ", message);
    public static void LogWarn(string message) => Write("WARN ", message);
    public static void LogError(string message) => Write("ERROR", message);

    public static void LogException(string context, Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"EXCEPTION in [{context}]");
        sb.AppendLine($"  Type   : {ex.GetType().FullName}");
        sb.AppendLine($"  Message: {ex.Message}");

        // Inner exceptions
        var inner = ex.InnerException;
        int depth = 0;
        while (inner != null && depth < 5)
        {
            sb.AppendLine($"  Inner[{depth}]: {inner.GetType().Name}: {inner.Message}");
            inner = inner.InnerException;
            depth++;
        }

        // Stack trace
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            sb.AppendLine("  StackTrace:");
            foreach (var line in ex.StackTrace.Split('\n'))
                sb.AppendLine("    " + line.TrimEnd());
        }

        Write("EXCPT", sb.ToString());
    }

    public static void Flush() { /* synchronous writes are already flushed */ }

    // ----------------------------------------------------------------
    // Internal
    // ----------------------------------------------------------------

    private static void Write(string level, string message)
    {
        if (string.IsNullOrEmpty(_logPath)) return;
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}\n";
        lock (_lock)
        {
            try { File.AppendAllText(_logPath, line, Encoding.UTF8); }
            catch { /* ignore write failures */ }
        }
    }
}
