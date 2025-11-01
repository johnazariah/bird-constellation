using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Owlet.Core.Extensions;
using Owlet.Core.Logging;
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
        // Early logger for startup errors
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.EventLog("Owlet", manageEventSource: true, formatProvider: CultureInfo.InvariantCulture)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Owlet service host");

            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

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

            // Add health checks
            builder.Services.AddHealthChecks()
                .AddOwletHealthChecks();

            // Add the Windows service host
            builder.Services.AddOwletWindowsService();

            IHost host = builder.Build();

            Log.Information("Service host configured, starting application");

            await host.StartAsync();
            await host.WaitForShutdownAsync();

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
