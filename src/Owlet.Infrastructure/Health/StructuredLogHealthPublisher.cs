using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Owlet.Infrastructure.Health;

/// <summary>
/// Publishes health check results to structured logs for monitoring and analysis.
/// Logs comprehensive health data with structured properties for easy querying.
/// </summary>
public sealed class StructuredLogHealthPublisher : IHealthCheckPublisher
{
    private readonly ILogger<StructuredLogHealthPublisher> _logger;

    public StructuredLogHealthPublisher(ILogger<StructuredLogHealthPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        try
        {
            // Create structured log scope with health check data
            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["HealthCheckTimestamp"] = DateTimeOffset.UtcNow,
                ["HealthCheckStatus"] = report.Status.ToString(),
                ["HealthCheckDurationMs"] = report.TotalDuration.TotalMilliseconds,
                ["ComponentCount"] = report.Entries.Count,
                ["HealthyComponents"] = report.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                ["DegradedComponents"] = report.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                ["UnhealthyComponents"] = report.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)
            });

            // Log overall status
            var logLevel = GetLogLevel(report.Status);
            _logger.Log(logLevel, "Health check completed: {Status} in {DurationMs}ms",
                report.Status, report.TotalDuration.TotalMilliseconds);

            // Log individual component details if not healthy
            foreach (var entry in report.Entries.Where(e => e.Value.Status != HealthStatus.Healthy))
            {
                var componentLogLevel = GetLogLevel(entry.Value.Status);
                var componentScope = new Dictionary<string, object>
                {
                    ["ComponentName"] = entry.Key,
                    ["ComponentStatus"] = entry.Value.Status.ToString(),
                    ["ComponentDurationMs"] = entry.Value.Duration.TotalMilliseconds
                };

                // Add component-specific data
                if (entry.Value.Data.Any())
                {
                    foreach (var kvp in entry.Value.Data)
                    {
                        componentScope[$"Component_{kvp.Key}"] = kvp.Value;
                    }
                }

                using (_logger.BeginScope(componentScope))
                {
                    if (entry.Value.Exception != null)
                    {
                        _logger.Log(componentLogLevel,
                            entry.Value.Exception,
                            "Component {ComponentName} is {Status}: {Description}",
                            entry.Key, entry.Value.Status, entry.Value.Description);
                    }
                    else
                    {
                        _logger.Log(componentLogLevel,
                            "Component {ComponentName} is {Status}: {Description}",
                            entry.Key, entry.Value.Status, entry.Value.Description);
                    }
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish health status to structured logs");
            return Task.CompletedTask;
        }
    }

    private static LogLevel GetLogLevel(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => LogLevel.Debug,
            HealthStatus.Degraded => LogLevel.Warning,
            HealthStatus.Unhealthy => LogLevel.Error,
            _ => LogLevel.Information
        };
    }
}
