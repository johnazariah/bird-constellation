using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;

#pragma warning disable CA1848 // Use LoggerMessage delegates (future optimization)

namespace Owlet.Core.Health;

/// <summary>
/// Health check for service configuration and network availability.
/// </summary>
public class ServiceHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<ServiceConfiguration> _serviceConfig;
    private readonly IOptionsMonitor<NetworkConfiguration> _networkConfig;
    private readonly ILogger<ServiceHealthCheck> _logger;

    public ServiceHealthCheck(
        IOptionsMonitor<ServiceConfiguration> serviceConfig,
        IOptionsMonitor<NetworkConfiguration> networkConfig,
        ILogger<ServiceHealthCheck> logger)
    {
        _serviceConfig = serviceConfig;
        _networkConfig = networkConfig;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var checks = new List<string>();
            var data = new Dictionary<string, object>();

            // Check service configuration
            var serviceConfig = _serviceConfig.CurrentValue;
            data["ServiceName"] = serviceConfig.ServiceName;
            data["StartMode"] = serviceConfig.StartMode.ToString();
            checks.Add("Service configuration loaded");

            // Check network configuration
            var networkConfig = _networkConfig.CurrentValue;
            data["Port"] = networkConfig.Port;
            data["BindAddress"] = networkConfig.BindAddress;
            checks.Add("Network configuration loaded");

            // Check if port is available
            if (IsPortAvailable(networkConfig.Port))
            {
                checks.Add("Network port available");
                data["PortStatus"] = "Available";
            }
            else
            {
                data["PortStatus"] = "In Use";
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Port {networkConfig.Port} is already in use",
                    data: data));
            }

            data["Checks"] = checks;
            data["CheckTime"] = DateTime.UtcNow;

            return Task.FromResult(HealthCheckResult.Healthy(
                "Service is healthy and ready",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Health check failed with exception",
                ex,
                new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["CheckTime"] = DateTime.UtcNow
                }));
        }
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var tcpListener = new TcpListener(IPAddress.Loopback, port);
            tcpListener.Start();
            tcpListener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
