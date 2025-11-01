using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Owlet.Core.Configuration;

/// <summary>
/// Logging infrastructure configuration for multi-sink structured logging.
/// </summary>
public record LoggingConfiguration
{
    /// <summary>
    /// Minimum log level for all log sinks (Trace, Debug, Information, Warning, Error, Critical).
    /// </summary>
    [Required]
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Directory where log files will be written (must be writable by service account).
    /// </summary>
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string LogDirectory { get; init; } = @"C:\ProgramData\Owlet\Logs";

    /// <summary>
    /// Maximum size of a single log file before rolling to a new file (in bytes).
    /// </summary>
    [Range(1048576, 1073741824)] // 1MB to 1GB
    public long MaxLogFileSizeBytes { get; init; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Number of log files to retain before deleting oldest files.
    /// </summary>
    [Range(1, 100)]
    public int RetainedLogFiles { get; init; } = 10;

    /// <summary>
    /// Log file rolling interval (Infinite, Year, Month, Day, Hour, Minute).
    /// </summary>
    [Required]
    public LogRollingInterval RollingInterval { get; init; } = LogRollingInterval.Day;

    /// <summary>
    /// Whether to write critical service events to Windows Event Log.
    /// </summary>
    public bool EnableWindowsEventLog { get; init; } = true;

    /// <summary>
    /// Whether to write logs to console output (useful for debugging).
    /// </summary>
    public bool EnableConsole { get; init; } = false;

    /// <summary>
    /// Whether to write structured JSON logs for log aggregation systems.
    /// </summary>
    public bool EnableStructuredLogging { get; init; } = true;

    /// <summary>
    /// Whether to include detailed exception information in logs.
    /// </summary>
    public bool IncludeExceptionDetails { get; init; } = true;

    /// <summary>
    /// Whether to include scope information (correlation IDs, request paths) in logs.
    /// </summary>
    public bool IncludeScopes { get; init; } = true;

    /// <summary>
    /// Log level overrides for specific namespaces (e.g., "Microsoft" = Warning).
    /// </summary>
    public Dictionary<string, LogLevel> LogLevelOverrides { get; init; } = new()
    {
        ["Microsoft"] = LogLevel.Warning,
        ["System"] = LogLevel.Warning,
        ["Microsoft.Hosting.Lifetime"] = LogLevel.Information
    };
}

/// <summary>
/// Log file rolling interval options.
/// </summary>
public enum LogRollingInterval
{
    /// <summary>No rolling, single log file grows indefinitely.</summary>
    Infinite,

    /// <summary>Roll to new file each year.</summary>
    Year,

    /// <summary>Roll to new file each month.</summary>
    Month,

    /// <summary>Roll to new file each day.</summary>
    Day,

    /// <summary>Roll to new file each hour.</summary>
    Hour,

    /// <summary>Roll to new file each minute.</summary>
    Minute
}
