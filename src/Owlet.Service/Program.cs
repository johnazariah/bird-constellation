using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Owlet.Core.Extensions;
using Owlet.Core.Logging;
using Owlet.Infrastructure.Health;
using Owlet.Infrastructure.Logging;
using Owlet.Service.Extensions;
using Serilog;

#pragma warning disable CA1848 // Use LoggerMessage delegates (future optimization)
#pragma warning disable CA1416 // Owlet is Windows-only

namespace Owlet.Service;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Early logger for startup errors (console only during development)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Owlet service host");

            // Use WebApplication for HTTP endpoints (health checks, API)
            var builder = WebApplication.CreateBuilder(args);

            // Add Aspire service defaults (OpenTelemetry, service discovery, resilience)
            builder.AddServiceDefaults();

            // Configure Windows Service integration
            builder.Host.UseWindowsService(options => 
            {
                options.ServiceName = "OwletService";
            });

            // Add logging context
            builder.Services.AddSingleton<ILoggingContext, LoggingContext>();

            // Add Owlet configuration with validation
            builder.Services.AddOwletConfiguration();

            // Configure Serilog using our factory after configuration is available
            builder.Logging.ClearProviders();
            builder.Services.AddSerilog((services, _) =>
            {
                var loggingConfig = services.GetRequiredService<IOptionsMonitor<LoggingConfiguration>>().CurrentValue;
                Log.Logger = Infrastructure.Logging.LoggerFactory.CreateLogger(loggingConfig);
            });

            // Add error handling
            builder.Services.AddOwletErrorHandling();

            // Add comprehensive health checks
            builder.Services.AddOwletHealthChecks();

            // Add the Windows service background worker
            builder.Services.AddOwletWindowsService();

            var app = builder.Build();

            // Configure Aspire default endpoints (/health, /alive in development)
            app.MapDefaultEndpoints();

            // Configure health check HTTP endpoints
            app.UseOwletHealthChecks();

            Log.Information("Service host configured, starting application");

            await app.RunAsync();

            Log.Information("Service host stopped normally");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Service host terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
