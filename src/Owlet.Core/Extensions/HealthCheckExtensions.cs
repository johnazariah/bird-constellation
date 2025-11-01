using Microsoft.Extensions.Diagnostics.HealthChecks;
using Owlet.Core.Health;

#pragma warning disable CA1861 // Prefer static readonly fields (minimal impact in extension methods)

namespace Owlet.Core.Extensions;

/// <summary>
/// Extension methods for health check services.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds Owlet health checks.
    /// </summary>
    public static IHealthChecksBuilder AddOwletHealthChecks(this IHealthChecksBuilder builder)
    {
        builder.AddCheck<ServiceHealthCheck>(
            "service",
            HealthStatus.Degraded,
            new[] { "service", "ready" });

        builder.AddCheck<FileSystemHealthCheck>(
            "filesystem",
            HealthStatus.Degraded,
            new[] { "filesystem", "ready" });

        return builder;
    }
}
