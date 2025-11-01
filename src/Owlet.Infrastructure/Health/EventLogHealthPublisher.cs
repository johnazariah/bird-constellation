using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Owlet.Infrastructure.Health;

/// <summary>
/// Publishes health check results to Windows Event Log.
/// Only logs on status changes or critical issues to avoid event log spam.
/// </summary>
public sealed class EventLogHealthPublisher : IHealthCheckPublisher
{
    private readonly ILogger<EventLogHealthPublisher> _logger;
    private readonly EventLog? _eventLog;

    // Simple in-memory status tracking (could be enhanced with persistent storage)
    private static HealthStatus? _previousStatus;
    private static int _healthCheckCount;

    public EventLogHealthPublisher(ILogger<EventLogHealthPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            // Ensure event source exists (should be created during installation)
            if (!EventLog.SourceExists("Owlet Service"))
            {
                EventLog.CreateEventSource("Owlet Service", "Application");
                _logger.LogInformation("Created Event Log source: Owlet Service");
            }

            _eventLog = new EventLog("Application", ".", "Owlet Service");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize Event Log for health check publishing. Event logging will be disabled.");
            _eventLog = null;
        }
    }

    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        try
        {
            if (_eventLog == null)
                return;

            await Task.Yield(); // Make async for interface compliance

            var previousStatus = GetPreviousHealthStatus();
            var currentStatus = report.Status;

            // Only log on status changes or critical issues
            if (ShouldLogHealthStatus(previousStatus, currentStatus, report))
            {
                var eventType = GetEventLogEntryType(currentStatus);
                var message = FormatHealthMessage(report);
                var eventId = GetEventId(currentStatus);

                _eventLog.WriteEntry(message, eventType, eventId);

                _logger.LogInformation("Health status published to Event Log: {Status}", currentStatus);
            }

            // Update stored status
            SetPreviousHealthStatus(currentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish health status to Event Log");
        }
    }

    private static bool ShouldLogHealthStatus(
        HealthStatus? previousStatus,
        HealthStatus currentStatus,
        HealthReport report)
    {
        // Always log status changes
        if (previousStatus != currentStatus)
            return true;

        // Always log unhealthy status (every check)
        if (currentStatus == HealthStatus.Unhealthy)
            return true;

        // Log degraded status periodically (every 10th check)
        if (currentStatus == HealthStatus.Degraded)
        {
            var checkCount = GetHealthCheckCount();
            return checkCount % 10 == 0;
        }

        // Don't log healthy status unless it's a recovery
        return false;
    }

    private static EventLogEntryType GetEventLogEntryType(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => EventLogEntryType.Information,
            HealthStatus.Degraded => EventLogEntryType.Warning,
            HealthStatus.Unhealthy => EventLogEntryType.Error,
            _ => EventLogEntryType.Information
        };
    }

    private static int GetEventId(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => 1001,
            HealthStatus.Degraded => 1002,
            HealthStatus.Unhealthy => 1003,
            _ => 1000
        };
    }

    private static string FormatHealthMessage(HealthReport report)
    {
        var message = $"Owlet Service Health Status: {report.Status}\n";
        message += $"Check Duration: {report.TotalDuration.TotalMilliseconds:F0}ms\n";
        message += $"Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n\n";

        if (report.Entries.Any())
        {
            message += "Component Status:\n";
            foreach (var entry in report.Entries)
            {
                message += $"  {entry.Key}: {entry.Value.Status}";

                if (entry.Value.Status != HealthStatus.Healthy && !string.IsNullOrEmpty(entry.Value.Description))
                {
                    message += $" - {entry.Value.Description}";
                }

                if (entry.Value.Exception != null)
                {
                    message += $" (Exception: {entry.Value.Exception.Message})";
                }

                message += "\n";
            }
        }

        return message;
    }

    private static HealthStatus? GetPreviousHealthStatus() => _previousStatus;
    private static void SetPreviousHealthStatus(HealthStatus status) => _previousStatus = status;
    private static int GetHealthCheckCount() => Interlocked.Increment(ref _healthCheckCount);

    public void Dispose()
    {
        _eventLog?.Dispose();
    }
}
