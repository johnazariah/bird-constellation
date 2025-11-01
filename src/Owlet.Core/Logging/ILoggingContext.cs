namespace Owlet.Core.Logging;

/// <summary>
/// Provides structured logging context for service operations and events.
/// </summary>
public interface ILoggingContext
{
    /// <summary>
    /// Begins a logging scope for an operation.
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Disposable scope</returns>
    IDisposable BeginScope(string operationName);

    /// <summary>
    /// Begins a logging scope for an operation with parameters.
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="parameters">Operation parameters</param>
    /// <returns>Disposable scope</returns>
    IDisposable BeginScope(string operationName, object parameters);

    /// <summary>
    /// Logs a service event with optional data.
    /// </summary>
    /// <param name="eventName">Name of the event</param>
    /// <param name="data">Optional event data</param>
    void LogServiceEvent(string eventName, object? data = null);

    /// <summary>
    /// Logs a performance metric with duration and context.
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="duration">Duration of the operation</param>
    /// <param name="context">Optional context information</param>
    void LogPerformanceMetric(string metricName, TimeSpan duration, object? context = null);
}
