using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;

#pragma warning disable CA1848 // Use LoggerMessage delegates (future optimization)

namespace Owlet.Core.Validation;

/// <summary>
/// Validates all configuration sections at service startup.
/// </summary>
public class ConfigurationStartupValidator : IStartupValidator
{
    private readonly IOptionsMonitor<ServiceConfiguration> _serviceConfig;
    private readonly IOptionsMonitor<NetworkConfiguration> _networkConfig;
    private readonly IOptionsMonitor<LoggingConfiguration> _loggingConfig;
    private readonly IOptionsMonitor<DatabaseConfiguration> _databaseConfig;
    private readonly ILogger<ConfigurationStartupValidator> _logger;

    public ConfigurationStartupValidator(
        IOptionsMonitor<ServiceConfiguration> serviceConfig,
        IOptionsMonitor<NetworkConfiguration> networkConfig,
        IOptionsMonitor<LoggingConfiguration> loggingConfig,
        IOptionsMonitor<DatabaseConfiguration> databaseConfig,
        ILogger<ConfigurationStartupValidator> logger)
    {
        _serviceConfig = serviceConfig;
        _networkConfig = networkConfig;
        _loggingConfig = loggingConfig;
        _databaseConfig = databaseConfig;
        _logger = logger;
    }

    public Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate service configuration
        try
        {
            var serviceConfig = _serviceConfig.CurrentValue;
            _logger.LogDebug("Service configuration validated: {ServiceName}", serviceConfig.ServiceName);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Service configuration error: {f}"));
        }
        catch (Exception ex)
        {
            errors.Add($"Service configuration error: {ex.Message}");
        }

        // Validate network configuration
        try
        {
            var networkConfig = _networkConfig.CurrentValue;
            _logger.LogDebug("Network configuration validated: Port {Port}, Address {Address}",
                networkConfig.Port, networkConfig.BindAddress);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Network configuration error: {f}"));
        }
        catch (Exception ex)
        {
            errors.Add($"Network configuration error: {ex.Message}");
        }

        // Validate logging configuration
        try
        {
            var loggingConfig = _loggingConfig.CurrentValue;
            _logger.LogDebug("Logging configuration validated: Directory {Directory}",
                loggingConfig.LogDirectory);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Logging configuration error: {f}"));
        }
        catch (Exception ex)
        {
            errors.Add($"Logging configuration error: {ex.Message}");
        }

        // Validate database configuration
        try
        {
            var databaseConfig = _databaseConfig.CurrentValue;
            _logger.LogDebug("Database configuration validated: Provider {Provider}",
                databaseConfig.Provider);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Database configuration error: {f}"));
        }
        catch (Exception ex)
        {
            errors.Add($"Database configuration error: {ex.Message}");
        }

        var result = errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);

        if (!result.IsValid)
        {
            _logger.LogError("Configuration validation failed with {ErrorCount} errors: {Errors}",
                errors.Count, string.Join("; ", errors));
        }
        else
        {
            _logger.LogInformation("All configurations validated successfully");
        }

        return Task.FromResult(result);
    }
}
