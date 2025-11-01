using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Owlet.Core.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates (future optimization)

namespace Owlet.Core.ErrorHandling;

/// <summary>
/// Handles global unhandled exceptions with comprehensive logging.
/// </summary>
public class GlobalExceptionHandler : IHostedService
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly ILoggingContext _loggingContext;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, ILoggingContext loggingContext)
    {
        _logger = logger;
        _loggingContext = loggingContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger.LogInformation("Global exception handlers registered");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        _logger.LogInformation("Global exception handlers unregistered");
        return Task.CompletedTask;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _loggingContext.LogServiceEvent("UnhandledException", new
            {
                IsTerminating = e.IsTerminating,
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            });

            _logger.LogCritical(ex, "Unhandled exception occurred. Terminating: {IsTerminating}",
                e.IsTerminating);
        }
        else
        {
            _logger.LogCritical("Unhandled non-exception object: {ExceptionObject}", e.ExceptionObject);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _loggingContext.LogServiceEvent("UnobservedTaskException", new
        {
            ExceptionType = e.Exception.GetType().Name,
            Message = e.Exception.Message,
            InnerExceptionCount = e.Exception.InnerExceptions.Count
        });

        _logger.LogError(e.Exception, "Unobserved task exception occurred");

        // Mark as observed to prevent application termination
        e.SetObserved();
    }
}
