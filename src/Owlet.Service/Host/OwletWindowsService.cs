using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Owlet.Core.Logging;
using Owlet.Core.Validation;

#pragma warning disable CA1848 // Use LoggerMessage delegates (future optimization)

namespace Owlet.Service.Host;

/// <summary>
/// Main Windows service background worker that manages service lifecycle.
/// </summary>
public class OwletWindowsService : BackgroundService
{
    private readonly ILogger<OwletWindowsService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ServiceConfiguration> _serviceConfig;
    private readonly Core.Validation.IStartupValidator _startupValidator;
    private readonly ILoggingContext _loggingContext;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public OwletWindowsService(
        ILogger<OwletWindowsService> logger,
        IServiceProvider serviceProvider,
        IOptionsMonitor<ServiceConfiguration> serviceConfig,
        Core.Validation.IStartupValidator startupValidator,
        ILoggingContext loggingContext,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _serviceConfig = serviceConfig;
        _startupValidator = startupValidator;
        _loggingContext = loggingContext;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = _loggingContext.BeginScope("ServiceStartup");

        try
        {
            _logger.LogInformation("Owlet Windows Service starting...");

            // Validate configuration
            var validationResult = await _startupValidator.ValidateAsync(stoppingToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(Environment.NewLine, validationResult.Errors);
                _logger.LogCritical("Service startup failed due to configuration errors:{NewLine}{Errors}",
                    Environment.NewLine, errors);

                _applicationLifetime.StopApplication();
                return;
            }

            _loggingContext.LogServiceEvent("ConfigurationValidated");

            // Start all hosted services
            await StartHostedServicesAsync(stoppingToken);

            _loggingContext.LogServiceEvent("ServiceStarted", new
            {
                ServiceName = _serviceConfig.CurrentValue.ServiceName,
                StartupTime = DateTime.UtcNow
            });

            _logger.LogInformation("Owlet Windows Service started successfully");

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Owlet Windows Service shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Owlet Windows Service failed to start");
            _applicationLifetime.StopApplication();
        }
        finally
        {
            _loggingContext.LogServiceEvent("ServiceStopping");
            _logger.LogInformation("Owlet Windows Service stopping...");
        }
    }

    private async Task StartHostedServicesAsync(CancellationToken cancellationToken)
    {
        var hostedServices = _serviceProvider.GetServices<IHostedService>()
            .Where(s => s != this) // Exclude self to avoid recursion
            .ToArray();

        _logger.LogInformation("Starting {HostedServiceCount} hosted services", hostedServices.Length);

        foreach (var service in hostedServices)
        {
            var serviceType = service.GetType().Name;

            try
            {
                using var scope = _loggingContext.BeginScope("HostedServiceStartup", new { ServiceType = serviceType });

                var stopwatch = Stopwatch.StartNew();
                await service.StartAsync(cancellationToken);
                stopwatch.Stop();

                _loggingContext.LogPerformanceMetric($"HostedService.{serviceType}.Startup",
                    stopwatch.Elapsed, new { ServiceType = serviceType });

                _logger.LogInformation("Started hosted service: {ServiceType}", serviceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start hosted service: {ServiceType}", serviceType);
                throw;
            }
        }

        _logger.LogInformation("All hosted services started successfully");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        using var _ = _loggingContext.BeginScope("ServiceShutdown");

        _logger.LogInformation("Owlet Windows Service stopping gracefully...");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await base.StopAsync(cancellationToken);

            stopwatch.Stop();
            _loggingContext.LogPerformanceMetric("ServiceShutdown", stopwatch.Elapsed);

            _loggingContext.LogServiceEvent("ServiceStopped", new
            {
                ShutdownTime = DateTime.UtcNow,
                ShutdownDuration = stopwatch.Elapsed
            });

            _logger.LogInformation("Owlet Windows Service stopped gracefully in {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service shutdown");
            throw;
        }
    }
}
