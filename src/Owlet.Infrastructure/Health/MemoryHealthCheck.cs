using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Owlet.Infrastructure.Health;

/// <summary>
/// Health check for monitoring service memory usage.
/// Tracks GC memory allocation against performance targets (< 200MB idle, < 500MB indexing).
/// </summary>
public sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;

    // Performance targets from performance-resource-planning.md
    private const long IdleMemoryThresholdBytes = 200L * 1024 * 1024; // 200 MB
    private const long BusyMemoryThresholdBytes = 500L * 1024 * 1024; // 500 MB
    private const long CriticalMemoryThresholdBytes = 800L * 1024 * 1024; // 800 MB

    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var totalMemory = GC.GetTotalMemory(forceFullCollection: false);
            var workingSet = Environment.WorkingSet;

            var totalMemoryMB = Math.Round(totalMemory / (1024.0 * 1024.0), 2);
            var workingSetMB = Math.Round(workingSet / (1024.0 * 1024.0), 2);
            var heapSizeMB = Math.Round(gcMemoryInfo.HeapSizeBytes / (1024.0 * 1024.0), 2);
            var fragmentedMB = Math.Round(gcMemoryInfo.FragmentedBytes / (1024.0 * 1024.0), 2);

            var data = new Dictionary<string, object>
            {
                ["totalMemoryMB"] = totalMemoryMB,
                ["workingSetMB"] = workingSetMB,
                ["heapSizeMB"] = heapSizeMB,
                ["fragmentedMB"] = fragmentedMB,
                ["generation0Collections"] = GC.CollectionCount(0),
                ["generation1Collections"] = GC.CollectionCount(1),
                ["generation2Collections"] = GC.CollectionCount(2),
                ["gcPauseTimePercentage"] = gcMemoryInfo.PauseTimePercentage,
                ["memoryLoadBytes"] = gcMemoryInfo.MemoryLoadBytes
            };

            // Determine health status based on memory usage
            var status = GetHealthStatus(totalMemory, totalMemoryMB);
            var description = status switch
            {
                HealthStatus.Healthy => $"Memory usage is healthy: {totalMemoryMB} MB",
                HealthStatus.Degraded => $"Memory usage is elevated: {totalMemoryMB} MB (target < {BusyMemoryThresholdBytes / 1024 / 1024} MB)",
                HealthStatus.Unhealthy => $"Memory usage is critical: {totalMemoryMB} MB (limit: {CriticalMemoryThresholdBytes / 1024 / 1024} MB)",
                _ => $"Memory status unknown: {totalMemoryMB} MB"
            };

            _logger.LogDebug(
                "Memory health check: {Status}, Total: {TotalMB} MB, Working Set: {WorkingSetMB} MB, Heap: {HeapSizeMB} MB",
                status, totalMemoryMB, workingSetMB, heapSizeMB);

            return Task.FromResult(new HealthCheckResult(status, description, data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed with exception");
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    $"Memory health check exception: {ex.Message}",
                    exception: ex));
        }
    }

    private static HealthStatus GetHealthStatus(long totalMemoryBytes, double totalMemoryMB)
    {
        // Critical: Over 800 MB (system is under severe pressure)
        if (totalMemoryBytes > CriticalMemoryThresholdBytes)
        {
            return HealthStatus.Unhealthy;
        }

        // Degraded: Over 500 MB (exceeds busy threshold)
        if (totalMemoryBytes > BusyMemoryThresholdBytes)
        {
            return HealthStatus.Degraded;
        }

        // Healthy: Under 500 MB
        return HealthStatus.Healthy;
    }
}
