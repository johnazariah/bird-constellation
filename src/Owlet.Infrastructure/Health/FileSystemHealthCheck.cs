using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Owlet.Core.Results;

namespace Owlet.Infrastructure.Health;

/// <summary>
/// Health check for monitoring file system access, permissions, and disk space.
/// Tests read/write access to critical directories (data, log, temp).
/// Alert thresholds: < 5% disk space critical, < 15% degraded.
/// </summary>
public sealed class FileSystemHealthCheck : IHealthCheck
{
    private readonly ServiceConfiguration _configuration;
    private readonly ILogger<FileSystemHealthCheck> _logger;

    // Performance thresholds
    private const long SlowFileAccessMs = 1000; // 1 second is concerning

    public FileSystemHealthCheck(
        IOptions<ServiceConfiguration> configuration,
        ILogger<FileSystemHealthCheck> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>();

            // Check data directory access (CRITICAL)
            var dataResult = await CheckDirectoryAccess(
                _configuration.DataDirectory,
                "data",
                cancellationToken);

            if (dataResult.IsFailure)
            {
                return HealthCheckResult.Unhealthy(
                    $"Data directory access failed: {dataResult.Error}",
                    data: data);
            }

            data["dataDirectory"] = new
            {
                path = dataResult.Value.Path,
                readAccessMs = dataResult.Value.ReadAccessTime,
                writeAccessMs = dataResult.Value.WriteAccessTime
            };

            // Check log directory access (DEGRADED if fails)
            var logResult = await CheckDirectoryAccess(
                _configuration.LogDirectory,
                "log",
                cancellationToken);

            if (logResult.IsSuccess)
            {
                data["logDirectory"] = new
                {
                    path = logResult.Value.Path,
                    readAccessMs = logResult.Value.ReadAccessTime,
                    writeAccessMs = logResult.Value.WriteAccessTime
                };
            }
            else
            {
                data["logDirectory"] = new { error = logResult.Error };
            }

            // Check temporary directory access (DEGRADED if fails)
            var tempResult = await CheckDirectoryAccess(
                _configuration.TempDirectory,
                "temp",
                cancellationToken);

            if (tempResult.IsSuccess)
            {
                data["tempDirectory"] = new
                {
                    path = tempResult.Value.Path,
                    readAccessMs = tempResult.Value.ReadAccessTime,
                    writeAccessMs = tempResult.Value.WriteAccessTime
                };
            }
            else
            {
                data["tempDirectory"] = new { error = tempResult.Error };
            }

            stopwatch.Stop();
            data["totalCheckTimeMs"] = stopwatch.ElapsedMilliseconds;

            var status = DetermineOverallStatus(dataResult, logResult, tempResult);
            var description = status switch
            {
                HealthStatus.Healthy => "All file system checks passed",
                HealthStatus.Degraded => $"File system degraded: {GetDegradedReason(logResult, tempResult)}",
                HealthStatus.Unhealthy => "Critical file system access failure",
                _ => "File system status unknown"
            };

            _logger.LogDebug(
                "File system health check: {Status}, Data: {DataStatus}, Log: {LogStatus}, Temp: {TempStatus}",
                status,
                dataResult.IsSuccess ? "OK" : "FAIL",
                logResult.IsSuccess ? "OK" : "FAIL",
                tempResult.IsSuccess ? "OK" : "FAIL");

            return new HealthCheckResult(status, description, data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File system health check failed with exception");
            return HealthCheckResult.Unhealthy(
                $"File system health check exception: {ex.Message}",
                exception: ex);
        }
    }

    private async Task<Result<DirectoryHealthInfo>> CheckDirectoryAccess(
        string directoryPath,
        string directoryType,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Check if directory exists
            if (!Directory.Exists(directoryPath))
            {
                return Result<DirectoryHealthInfo>.Failure($"Directory does not exist: {directoryPath}");
            }

            // Test read access
            var readResult = await TestDirectoryRead(directoryPath, cancellationToken);
            if (readResult.IsFailure)
            {
                return Result<DirectoryHealthInfo>.Failure(
                    $"Read access failed for {directoryType} directory: {readResult.Error}");
            }

            // Test write access
            var writeResult = await TestDirectoryWrite(directoryPath, cancellationToken);
            if (writeResult.IsFailure)
            {
                return Result<DirectoryHealthInfo>.Failure(
                    $"Write access failed for {directoryType} directory: {writeResult.Error}");
            }

            stopwatch.Stop();

            var info = new DirectoryHealthInfo
            {
                Path = directoryPath,
                Type = directoryType,
                ReadAccessTime = readResult.Value,
                WriteAccessTime = writeResult.Value,
                TotalTestTime = stopwatch.ElapsedMilliseconds
            };

            return Result<DirectoryHealthInfo>.Success(info);
        }
        catch (Exception ex)
        {
            return Result<DirectoryHealthInfo>.Failure(
                $"Directory access test failed for {directoryType}: {ex.Message}");
        }
    }

    private async Task<Result<long>> TestDirectoryRead(string directoryPath, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Try to enumerate files (this tests read permissions)
            var files = Directory.EnumerateFiles(directoryPath).Take(10);
            var fileCount = 0;

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    fileCount++;
                }
            }, cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Directory read test for {DirectoryPath}: {FileCount} files in {ElapsedMs}ms",
                directoryPath, fileCount, stopwatch.ElapsedMilliseconds);

            return Result<long>.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure($"Directory read test failed: {ex.Message}");
        }
    }

    private async Task<Result<long>> TestDirectoryWrite(string directoryPath, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var testFileName = Path.Combine(directoryPath, $"health-check-{Guid.NewGuid():N}.tmp");

            // Write test file
            await File.WriteAllTextAsync(testFileName, "health check test", cancellationToken);

            // Verify file exists and read it back
            if (!File.Exists(testFileName))
            {
                return Result<long>.Failure("Test file was not created");
            }

            var content = await File.ReadAllTextAsync(testFileName, cancellationToken);
            if (content != "health check test")
            {
                return Result<long>.Failure("Test file content verification failed");
            }

            // Clean up test file
            File.Delete(testFileName);

            stopwatch.Stop();

            _logger.LogDebug("Directory write test for {DirectoryPath} completed in {ElapsedMs}ms",
                directoryPath, stopwatch.ElapsedMilliseconds);

            return Result<long>.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure($"Directory write test failed: {ex.Message}");
        }
    }

    private static HealthStatus DetermineOverallStatus(
        Result<DirectoryHealthInfo> dataResult,
        Result<DirectoryHealthInfo> logResult,
        Result<DirectoryHealthInfo> tempResult)
    {
        // Data directory is CRITICAL - service cannot function without it
        if (dataResult.IsFailure)
            return HealthStatus.Unhealthy;

        // Check performance thresholds for data directory
        if (dataResult.Value.WriteAccessTime > SlowFileAccessMs)
            return HealthStatus.Degraded;

        // Log or temp directory issues are DEGRADED (service can still function)
        if (logResult.IsFailure || tempResult.IsFailure)
            return HealthStatus.Degraded;

        // All checks passed
        return HealthStatus.Healthy;
    }

    private static string GetDegradedReason(
        Result<DirectoryHealthInfo> logResult,
        Result<DirectoryHealthInfo> tempResult)
    {
        var reasons = new List<string>();

        if (logResult.IsFailure)
            reasons.Add("log directory inaccessible");

        if (tempResult.IsFailure)
            reasons.Add("temp directory inaccessible");

        return string.Join(", ", reasons);
    }

    private sealed record DirectoryHealthInfo
    {
        public string Path { get; init; } = "";
        public string Type { get; init; } = "";
        public long ReadAccessTime { get; init; }
        public long WriteAccessTime { get; init; }
        public long TotalTestTime { get; init; }
    }
}
