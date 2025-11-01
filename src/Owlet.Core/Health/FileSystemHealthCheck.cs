using System.Text.RegularExpressions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;

#pragma warning disable CA1848 // Use LoggerMessage delegates (future optimization)

namespace Owlet.Core.Health;

/// <summary>
/// Health check for file system access (log and database directories).
/// </summary>
public class FileSystemHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<DatabaseConfiguration> _dbConfig;
    private readonly IOptionsMonitor<LoggingConfiguration> _loggingConfig;
    private readonly ILogger<FileSystemHealthCheck> _logger;

    public FileSystemHealthCheck(
        IOptionsMonitor<DatabaseConfiguration> dbConfig,
        IOptionsMonitor<LoggingConfiguration> loggingConfig,
        ILogger<FileSystemHealthCheck> logger)
    {
        _dbConfig = dbConfig;
        _loggingConfig = loggingConfig;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();
            var checks = new List<string>();

            // Check log directory
            var loggingConfig = _loggingConfig.CurrentValue;
            var logDirInfo = new DirectoryInfo(loggingConfig.LogDirectory);

            if (!logDirInfo.Exists)
            {
                try
                {
                    logDirInfo.Create();
                    checks.Add("Log directory created");
                }
                catch (Exception ex)
                {
                    data["LogDirectoryError"] = ex.Message;
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "Cannot create log directory",
                        ex,
                        data));
                }
            }
            else
            {
                checks.Add("Log directory exists");
            }

            data["LogDirectory"] = logDirInfo.FullName;
            data["LogDirectoryFreeSpace"] = GetFreeSpace(logDirInfo.FullName);

            // Check database directory
            var dbConfig = _dbConfig.CurrentValue;
            var dbPath = GetDatabasePath(dbConfig.ConnectionString);
            if (!string.IsNullOrEmpty(dbPath))
            {
                var dbDir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir))
                {
                    var dbDirInfo = new DirectoryInfo(dbDir);
                    if (!dbDirInfo.Exists)
                    {
                        try
                        {
                            dbDirInfo.Create();
                            checks.Add("Database directory created");
                        }
                        catch (Exception ex)
                        {
                            data["DatabaseDirectoryError"] = ex.Message;
                            return Task.FromResult(HealthCheckResult.Unhealthy(
                                "Cannot create database directory",
                                ex,
                                data));
                        }
                    }
                    else
                    {
                        checks.Add("Database directory exists");
                    }

                    data["DatabaseDirectory"] = dbDirInfo.FullName;
                    data["DatabaseDirectoryFreeSpace"] = GetFreeSpace(dbDirInfo.FullName);
                }
            }

            data["Checks"] = checks;
            data["CheckTime"] = DateTime.UtcNow;

            return Task.FromResult(HealthCheckResult.Healthy(
                "File system is accessible",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File system health check failed");

            return Task.FromResult(HealthCheckResult.Unhealthy(
                "File system health check failed",
                ex,
                new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["CheckTime"] = DateTime.UtcNow
                }));
        }
    }

    private static string? GetDatabasePath(string connectionString)
    {
        // Extract file path from SQLite connection string
        var match = Regex.Match(
            connectionString,
            @"Data Source=([^;]+)",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }

    private static long GetFreeSpace(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return -1;
            }

            var drive = new DriveInfo(root);
            return drive.AvailableFreeSpace;
        }
        catch
        {
            return -1;
        }
    }
}
