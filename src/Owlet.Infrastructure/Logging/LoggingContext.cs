using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates (future optimization)

namespace Owlet.Infrastructure.Logging;

/// <summary>
/// Implementation of structured logging context for service operations.
/// </summary>
public class LoggingContext : Core.Logging.ILoggingContext
{
    private readonly ILogger<LoggingContext> _logger;

    public LoggingContext(ILogger<LoggingContext> logger)
    {
        _logger = logger;
    }

    public IDisposable BeginScope(string operationName)
    {
        return _logger.BeginScope("Operation: {OperationName}", operationName) ?? throw new InvalidOperationException("Failed to create logging scope");
    }

    public IDisposable BeginScope(string operationName, object parameters)
    {
        return _logger.BeginScope("Operation: {OperationName} with {@Parameters}", operationName, parameters) ?? throw new InvalidOperationException("Failed to create logging scope");
    }

    public void LogServiceEvent(string eventName, object? data = null)
    {
        if (data != null)
        {
            _logger.LogInformation("Service Event: {EventName} {@EventData}", eventName, data);
        }
        else
        {
            _logger.LogInformation("Service Event: {EventName}", eventName);
        }
    }

    public void LogPerformanceMetric(string metricName, TimeSpan duration, object? context = null)
    {
        if (context != null)
        {
            _logger.LogInformation("Performance Metric: {MetricName} took {Duration}ms {@Context}",
                metricName, duration.TotalMilliseconds, context);
        }
        else
        {
            _logger.LogInformation("Performance Metric: {MetricName} took {Duration}ms",
                metricName, duration.TotalMilliseconds);
        }
    }
}
