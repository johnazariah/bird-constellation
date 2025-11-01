using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;

namespace Owlet.Infrastructure.Health;

/// <summary>
/// Health check for monitoring disk space on the data drive.
/// Alerts when free space falls below critical thresholds (< 5% critical, < 15% degraded).
/// </summary>
public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ServiceConfiguration _configuration;
    private readonly ILogger<DiskSpaceHealthCheck> _logger;

    // Disk space thresholds from performance-resource-planning.md
    private const double CriticalFreePercentThreshold = 5.0; // < 5% is critical
    private const double DegradedFreePercentThreshold = 15.0; // < 15% is degraded
    private const long MinimumFreeSpaceGB = 1; // At least 1 GB free

    public DiskSpaceHealthCheck(
        IOptions<ServiceConfiguration> configuration,
        ILogger<DiskSpaceHealthCheck> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get disk info for the data directory drive
            var dataDirectoryPath = _configuration.DataDirectory;
            var driveInfo = GetDriveInfo(dataDirectoryPath);

            if (driveInfo == null)
            {
                return Task.FromResult(
                    HealthCheckResult.Degraded(
                        $"Could not determine drive information for path: {dataDirectoryPath}"));
            }

            var totalGB = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0), 2);
            var freeGB = Math.Round(driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
            var usedGB = Math.Round((driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (1024.0 * 1024.0 * 1024.0), 2);
            var freePercent = Math.Round((double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100, 1);

            var data = new Dictionary<string, object>
            {
                ["drive"] = driveInfo.Name,
                ["driveFormat"] = driveInfo.DriveFormat,
                ["driveType"] = driveInfo.DriveType.ToString(),
                ["totalGB"] = totalGB,
                ["freeGB"] = freeGB,
                ["usedGB"] = usedGB,
                ["freePercent"] = freePercent,
                ["volumeLabel"] = driveInfo.VolumeLabel,
                ["isReady"] = driveInfo.IsReady
            };

            // Determine health status
            var status = GetHealthStatus(freeGB, freePercent);
            var description = status switch
            {
                HealthStatus.Healthy => $"Disk space is healthy: {freeGB} GB free ({freePercent}%)",
                HealthStatus.Degraded => $"Disk space is low: {freeGB} GB free ({freePercent}%) - consider cleanup",
                HealthStatus.Unhealthy => $"Disk space is critical: {freeGB} GB free ({freePercent}%) - immediate action required",
                _ => $"Disk space status unknown: {freeGB} GB free ({freePercent}%)"
            };

            _logger.LogDebug(
                "Disk space health check: {Status}, Drive: {Drive}, Free: {FreeGB} GB ({FreePercent}%)",
                status, driveInfo.Name, freeGB, freePercent);

            return Task.FromResult(new HealthCheckResult(status, description, data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed with exception");
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    $"Disk space health check exception: {ex.Message}",
                    exception: ex));
        }
    }

    private DriveInfo? GetDriveInfo(string path)
    {
        try
        {
            var rootPath = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(rootPath))
            {
                _logger.LogWarning("Could not determine root path for: {Path}", path);
                return null;
            }

            return new DriveInfo(rootPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get drive info for path: {Path}", path);
            return null;
        }
    }

    private static HealthStatus GetHealthStatus(double freeGB, double freePercent)
    {
        // Critical: Less than 5% free OR less than 1 GB absolute
        if (freePercent < CriticalFreePercentThreshold || freeGB < MinimumFreeSpaceGB)
        {
            return HealthStatus.Unhealthy;
        }

        // Degraded: Less than 15% free
        if (freePercent < DegradedFreePercentThreshold)
        {
            return HealthStatus.Degraded;
        }

        // Healthy: Above thresholds
        return HealthStatus.Healthy;
    }
}
