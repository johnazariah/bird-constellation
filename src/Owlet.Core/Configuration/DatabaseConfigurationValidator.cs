using Microsoft.Extensions.Options;

namespace Owlet.Core.Configuration;

/// <summary>
/// Validates database configuration settings.
/// </summary>
public class DatabaseConfigurationValidator : IValidateOptions<DatabaseConfiguration>
{
    public ValidateOptionsResult Validate(string? name, DatabaseConfiguration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add("ConnectionString cannot be empty");
        }
        else if (options.ConnectionString.Length > 2048)
        {
            failures.Add("ConnectionString cannot exceed 2048 characters");
        }

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add("Provider cannot be empty");
        }
        else if (!string.Equals(options.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"Provider '{options.Provider}' is not supported. Only 'Sqlite' is supported.");
        }

        if (options.CommandTimeoutSeconds < 1)
        {
            failures.Add("CommandTimeoutSeconds must be at least 1 second");
        }
        else if (options.CommandTimeoutSeconds > 3600)
        {
            failures.Add("CommandTimeoutSeconds cannot exceed 3600 seconds (1 hour)");
        }

        if (options.MaxRetryCount < 1)
        {
            failures.Add("MaxRetryCount must be at least 1");
        }
        else if (options.MaxRetryCount > 1000)
        {
            failures.Add("MaxRetryCount cannot exceed 1000");
        }

        if (options.RetryDelay < TimeSpan.FromSeconds(1))
        {
            failures.Add("RetryDelay must be at least 1 second");
        }
        else if (options.RetryDelay > TimeSpan.FromMinutes(1))
        {
            failures.Add("RetryDelay cannot exceed 1 minute");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
