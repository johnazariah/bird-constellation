using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Owlet.Service.Host;

namespace Owlet.Service.Extensions;

/// <summary>
/// Extension methods for Windows service hosting.
/// </summary>
public static class ServiceHostExtensions
{
    /// <summary>
    /// Adds Windows service hosting with Owlet configuration.
    /// </summary>
    public static IServiceCollection AddOwletWindowsService(this IServiceCollection services)
    {
        // Register the main service host
        services.AddHostedService<OwletWindowsService>();

        return services;
    }
}
