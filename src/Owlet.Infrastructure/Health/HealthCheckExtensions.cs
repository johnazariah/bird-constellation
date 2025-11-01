using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Owlet.Infrastructure.Health;

/// <summary>
/// Extension methods for configuring health checks in the Owlet service.
/// Provides registration, endpoint configuration, and response formatting for health monitoring.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers all Owlet health checks with the service collection.
    /// Includes database, file system, indexer, memory, and disk space checks.
    /// </summary>
    public static IServiceCollection AddOwletHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "database" })
            .AddCheck<FileSystemHealthCheck>("filesystem", tags: new[] { "ready", "filesystem" })
            .AddCheck<MemoryHealthCheck>("memory", tags: new[] { "live", "memory" })
            .AddCheck<DiskSpaceHealthCheck>("disk", tags: new[] { "live", "disk" });

        // Register health check publishers
        services.AddSingleton<IHealthCheckPublisher, EventLogHealthPublisher>();
        services.AddSingleton<IHealthCheckPublisher, StructuredLogHealthPublisher>();
        
        return services;
    }

    /// <summary>
    /// Configures health check HTTP endpoints for the application.
    /// Creates three endpoints:
    /// - /health: Detailed health with all checks
    /// - /health/live: Liveness probe (memory, disk)
    /// - /health/ready: Readiness probe (database, filesystem)
    /// </summary>
    public static IApplicationBuilder UseOwletHealthChecks(this IApplicationBuilder app)
    {
        // Liveness probe - basic service health (is the process alive?)
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        // Readiness probe - full service readiness (is the service ready to accept requests?)
        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        // Detailed health endpoint with all checks
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthCheckResponse,
            AllowCachingResponses = false
        });

        return app;
    }

    /// <summary>
    /// Writes a simple health check response with overall status and timing.
    /// Used for liveness and readiness probes.
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = result.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = result.TotalDuration.TotalMilliseconds,
            version = GetServiceVersion()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    /// <summary>
    /// Writes a detailed health check response including all component results.
    /// Used for the main /health endpoint.
    /// </summary>
    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = result.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = result.TotalDuration.TotalMilliseconds,
            version = GetServiceVersion(),
            checks = result.Entries.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.Status.ToString(),
                duration = kvp.Value.Duration.TotalMilliseconds,
                description = kvp.Value.Description,
                data = kvp.Value.Data,
                exception = kvp.Value.Exception?.Message,
                tags = kvp.Value.Tags
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    /// <summary>
    /// Gets the service version from the assembly informational version attribute.
    /// </summary>
    private static string GetServiceVersion()
    {
        return typeof(HealthCheckExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Unknown";
    }
}
