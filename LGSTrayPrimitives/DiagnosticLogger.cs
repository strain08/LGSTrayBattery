using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LGSTrayPrimitives;

/// <summary>
/// Diagnostic logger for tracing device discovery and UI updates.
/// Writes to diagnostic.log file in the application directory.
/// Enable with --log command-line flag. Works in both Debug and Release builds.
/// </summary>
public static class DiagnosticLogger
{
    private static readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "diagnostic.log");

    private static bool _isEnabled = false;
    private static bool _isVerboseEnabled = false;

    /// <summary>
    /// Gets whether logging is enabled (--log flag).
    /// </summary>
    public static bool IsEnabled => _isEnabled;

    /// <summary>
    /// Gets whether verbose logging is enabled (--verbose flag).
    /// </summary>
    public static bool IsVerboseEnabled => _isVerboseEnabled;

    /// <summary>
    /// Initialize logging based on command-line arguments.
    /// Must be called before any Log() calls.
    /// </summary>
    /// <param name="enableLogging">Enable standard logging (--log)</param>
    /// <param name="enableVerbose">Enable verbose logging (--verbose)</param>
    public static void Initialize(bool enableLogging, bool enableVerbose)
    {
        _isEnabled = enableLogging;
        _isVerboseEnabled = enableVerbose;

        // If verbose is enabled, standard logging must also be enabled
        if (_isVerboseEnabled && !_isEnabled)
        {
            _isEnabled = true;
        }
    }

    /// <summary>
    /// Log an informational message with timestamp.
    /// </summary>
    public static void Log(string message, [CallerMemberName] string callerMember = "")
    {
        if (!_isEnabled) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formatted = $"[{timestamp}] [{callerMember}]: {message}";
        WriteToFile(formatted);
        WriteToConsole(formatted);
    }
    /// <summary>
    /// Log verbose message with timestamp.
    /// Requires --verbose flag.
    /// </summary>
    public static void Verbose(string message, [CallerMemberName] string callerMember = "")
    {
        if (!_isVerboseEnabled) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formatted = $"[{timestamp}] [VERBOSE] [{callerMember}]: {message}";
        WriteToFile(formatted);
        WriteToConsole(formatted);
    }



    /// <summary>
    /// Log a warning message with timestamp.
    /// </summary>
    public static void LogWarning(string message, [CallerMemberName] string callerMember = "")
    {
        if (!_isEnabled) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formatted = $"[{timestamp}] WARNING: {message}";
        WriteToFile(formatted);
        WriteToConsole(formatted);
    }

    /// <summary>
    /// Log an error message with timestamp.
    /// </summary>
    public static void LogError(string message, [CallerMemberName] string callerMember = "")
    {
        if (!_isEnabled) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formatted = $"[{timestamp}] ERROR: {message}";
        WriteToFile(formatted);
        WriteToConsole(formatted);
    }

    private static void WriteToFile(string message)
    {
        using var mutex = new Mutex(false, "LOG_WRITE");
        var hasHandle = false;
        try
        {
            hasHandle = mutex.WaitOne(Timeout.Infinite, false);

            File.AppendAllText(_logFilePath, message + Environment.NewLine);
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to write to diagnostic log file.");
        }
        finally
        {
            if (hasHandle)
                mutex.ReleaseMutex();
        }
    }

    private static void WriteToConsole(string formatted)
    {
        #if DEBUG
        Console.WriteLine(formatted);
        #endif
    }


    public static void ResetLog()
    {
        if (!_isEnabled) return;

        using var mutex = new Mutex(false, "LOG_WRITE");
        var hasHandle = false;
        try
        {
            hasHandle = mutex.WaitOne(Timeout.Infinite, false);

            File.WriteAllText(_logFilePath, string.Empty);
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to clear diagnostic log file.");
        }
        finally
        {
            if (hasHandle)
                mutex.ReleaseMutex();
        }
    }
}
