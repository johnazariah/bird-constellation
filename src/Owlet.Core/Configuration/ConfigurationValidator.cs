using Microsoft.Extensions.Options;

namespace Owlet.Core.Configuration;

/// <summary>
/// Validates ServiceConfiguration at startup to ensure all required settings are present and valid.
/// </summary>
public class ServiceConfigurationValidator : IValidateOptions<ServiceConfiguration>
{
    public ValidateOptionsResult Validate(string? name, ServiceConfiguration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            failures.Add("ServiceName is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(options.DisplayName))
            failures.Add("DisplayName is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(options.Description))
            failures.Add("Description is required and cannot be empty.");

        if (options.StartupTimeout < TimeSpan.FromSeconds(10))
            failures.Add("StartupTimeout must be at least 10 seconds.");

        if (options.StartupTimeout > TimeSpan.FromMinutes(5))
            failures.Add("StartupTimeout cannot exceed 5 minutes.");

        if (options.FailureRestartDelay < TimeSpan.FromSeconds(30))
            failures.Add("FailureRestartDelay must be at least 30 seconds.");

        if (options.MaxFailureRestarts < 0 || options.MaxFailureRestarts > 10)
            failures.Add("MaxFailureRestarts must be between 0 and 10.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validates NetworkConfiguration to ensure safe network settings.
/// </summary>
public class NetworkConfigurationValidator : IValidateOptions<NetworkConfiguration>
{
    public ValidateOptionsResult Validate(string? name, NetworkConfiguration options)
    {
        var failures = new List<string>();

        if (options.Port < 1024 || options.Port > 65535)
            failures.Add("Port must be between 1024 and 65535.");

        if (string.IsNullOrWhiteSpace(options.BindAddress))
            failures.Add("BindAddress is required and cannot be empty.");

        if (options.EnableHttps)
        {
            if (string.IsNullOrWhiteSpace(options.CertificatePath))
                failures.Add("CertificatePath is required when EnableHttps is true.");

            if (!string.IsNullOrWhiteSpace(options.CertificatePath) && !File.Exists(options.CertificatePath))
                failures.Add($"Certificate file not found at path: {options.CertificatePath}");
        }

        if (options.MaxRequestBodySize < 1024)
            failures.Add("MaxRequestBodySize must be at least 1024 bytes (1KB).");

        if (options.RequestTimeout < TimeSpan.FromSeconds(1))
            failures.Add("RequestTimeout must be at least 1 second.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Validates LoggingConfiguration to ensure proper log file settings.
/// </summary>
public class LoggingConfigurationValidator : IValidateOptions<LoggingConfiguration>
{
    public ValidateOptionsResult Validate(string? name, LoggingConfiguration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.LogDirectory))
            failures.Add("LogDirectory is required and cannot be empty.");

        if (options.MaxLogFileSizeBytes < 1024 * 1024) // 1MB minimum
            failures.Add("MaxLogFileSizeBytes must be at least 1MB (1048576 bytes).");

        if (options.RetainedLogFiles < 1)
            failures.Add("RetainedLogFiles must be at least 1.");

        if (options.RetainedLogFiles > 100)
            failures.Add("RetainedLogFiles cannot exceed 100 to prevent excessive disk usage.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
