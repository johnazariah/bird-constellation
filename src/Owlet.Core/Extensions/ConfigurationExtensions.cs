using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Owlet.Core.ErrorHandling;
using Owlet.Core.Validation;

namespace Owlet.Core.Extensions;

/// <summary>
/// Extension methods for configuration services.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Owlet configuration with validation.
    /// </summary>
    public static IServiceCollection AddOwletConfiguration(this IServiceCollection services)
    {
        // Register options with data annotation validation
        services.AddOptions<ServiceConfiguration>()
            .BindConfiguration(nameof(ServiceConfiguration))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<NetworkConfiguration>()
            .BindConfiguration(nameof(NetworkConfiguration))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LoggingConfiguration>()
            .BindConfiguration(nameof(LoggingConfiguration))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseConfiguration>()
            .BindConfiguration(nameof(DatabaseConfiguration))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register configuration validators
        services.AddSingleton<IValidateOptions<ServiceConfiguration>, ServiceConfigurationValidator>();
        services.AddSingleton<IValidateOptions<NetworkConfiguration>, NetworkConfigurationValidator>();
        services.AddSingleton<IValidateOptions<LoggingConfiguration>, LoggingConfigurationValidator>();
        services.AddSingleton<IValidateOptions<DatabaseConfiguration>, DatabaseConfigurationValidator>();

        // Register startup validator
        services.AddSingleton<Core.Validation.IStartupValidator, ConfigurationStartupValidator>();

        return services;
    }
}
