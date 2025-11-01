using System.Reflection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

namespace Owlet.Infrastructure.Logging;

/// <summary>
/// Creates and configures Serilog logger with multiple sinks based on configuration.
/// </summary>
public static class LoggerFactory
{
    /// <summary>
    /// Creates a fully configured Serilog logger instance.
    /// </summary>
    /// <param name="config">Logging configuration from appsettings</param>
    /// <returns>Configured Serilog ILogger instance</returns>
    public static Serilog.ILogger CreateLogger(Core.Configuration.LoggingConfiguration config)
    {
        // Ensure log directory exists
        if (!Directory.Exists(config.LogDirectory))
        {
            Directory.CreateDirectory(config.LogDirectory);
        }

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(ConvertLogLevel(config.MinimumLevel))
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Owlet")
            .Enrich.WithProperty("Version", GetApplicationVersion())
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();

        // Apply namespace-specific log level overrides
        foreach (var (ns, level) in config.LogLevelOverrides)
        {
            loggerConfig.MinimumLevel.Override(ns, ConvertLogLevel(level));
        }

        // File logging with rolling
        loggerConfig.WriteTo.File(
            path: Path.Combine(config.LogDirectory, "owlet-.log"),
            rollingInterval: ConvertRollingInterval(config.RollingInterval),
            retainedFileCountLimit: config.RetainedLogFiles,
            fileSizeLimitBytes: config.MaxLogFileSizeBytes,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        // Structured JSON logging for log aggregation
        if (config.EnableStructuredLogging)
        {
            loggerConfig.WriteTo.File(
                new JsonFormatter(),
                path: Path.Combine(config.LogDirectory, "owlet-structured-.json"),
                rollingInterval: ConvertRollingInterval(config.RollingInterval),
                retainedFileCountLimit: config.RetainedLogFiles,
                fileSizeLimitBytes: config.MaxLogFileSizeBytes,
                rollOnFileSizeLimit: true);
        }

        // Windows Event Log (critical events only)
        if (config.EnableWindowsEventLog)
        {
            try
            {
                loggerConfig.WriteTo.EventLog(
                    source: "Owlet Service",
                    logName: "Application",
                    restrictedToMinimumLevel: LogEventLevel.Warning,
                    manageEventSource: false); // Must be created during installation
            }
            catch (Exception ex)
            {
                // Event log source may not exist yet, log to console if enabled
                if (config.EnableConsole)
                {
                    Console.WriteLine($"Warning: Could not configure Windows Event Log sink: {ex.Message}");
                }
            }
        }

        // Console output (primarily for development)
        if (config.EnableConsole)
        {
            loggerConfig.WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        return loggerConfig.CreateLogger();
    }

    /// <summary>
    /// Converts Microsoft.Extensions.Logging.LogLevel to Serilog LogEventLevel.
    /// </summary>
    private static LogEventLevel ConvertLogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        LogLevel.None => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };

    /// <summary>
    /// Converts LogRollingInterval to Serilog RollingInterval.
    /// </summary>
    private static RollingInterval ConvertRollingInterval(Core.Configuration.LogRollingInterval interval) => interval switch
    {
        Core.Configuration.LogRollingInterval.Infinite => RollingInterval.Infinite,
        Core.Configuration.LogRollingInterval.Year => RollingInterval.Year,
        Core.Configuration.LogRollingInterval.Month => RollingInterval.Month,
        Core.Configuration.LogRollingInterval.Day => RollingInterval.Day,
        Core.Configuration.LogRollingInterval.Hour => RollingInterval.Hour,
        Core.Configuration.LogRollingInterval.Minute => RollingInterval.Minute,
        _ => RollingInterval.Day
    };

    /// <summary>
    /// Gets the application version from assembly metadata.
    /// </summary>
    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly.GetName().Version?.ToString()
               ?? "Unknown";
    }
}
