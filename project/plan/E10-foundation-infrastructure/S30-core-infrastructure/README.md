# E10 S30: Core Infrastructure

**Story:** Implement base Windows service host, configuration system, and Serilog logging infrastructure for production-ready service operation  
**Priority:** Critical  
**Effort:** 24 hours  
**Status:** Not Started  
**Dependencies:** S20 (Solution Architecture)  

## Objective

This story implements the core infrastructure foundation that enables Owlet to run as a professional Windows service. It establishes the service host lifecycle management, comprehensive configuration system with validation, and production-grade logging infrastructure that integrates with Windows Event Log and file system.

The implementation focuses on reliability and troubleshooting capabilities, ensuring that service failures are properly logged and diagnosable, while configuration errors are caught early with clear validation messages.

## Business Context

**Revenue Impact:** ₹0 direct revenue (foundational infrastructure enables all business value)  
**User Impact:** All users - determines service reliability, troubleshooting capability, and professional operation  
**Compliance Requirements:** Windows Event Log integration provides audit trail for enterprise compliance

## Configuration System Implementation

### 1. Strongly-Typed Configuration

Complete configuration system with validation, environment overrides, and clear error messages.

**Configuration Models:**

```csharp
namespace Owlet.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

[OptionsValidator]
public partial class ServiceConfigurationValidator : IValidateOptions<ServiceConfiguration>
{
    public ValidateOptionsResult Validate(string? name, ServiceConfiguration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
            failures.Add("ServiceName cannot be empty");
        else if (options.ServiceName.Length > 256)
            failures.Add("ServiceName cannot exceed 256 characters");
        else if (!IsValidServiceName(options.ServiceName))
            failures.Add("ServiceName contains invalid characters. Use only letters, numbers, spaces, and hyphens");

        if (string.IsNullOrWhiteSpace(options.DisplayName))
            failures.Add("DisplayName cannot be empty");
        else if (options.DisplayName.Length > 256)
            failures.Add("DisplayName cannot exceed 256 characters");

        if (string.IsNullOrWhiteSpace(options.Description))
            failures.Add("Description cannot be empty");
        else if (options.Description.Length > 1024)
            failures.Add("Description cannot exceed 1024 characters");

        if (options.StartupTimeout < TimeSpan.FromSeconds(10))
            failures.Add("StartupTimeout must be at least 10 seconds");
        else if (options.StartupTimeout > TimeSpan.FromMinutes(10))
            failures.Add("StartupTimeout cannot exceed 10 minutes");

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidServiceName(string serviceName)
    {
        return serviceName.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_');
    }
}

public record ServiceConfiguration
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string ServiceName { get; init; } = "OwletService";

    [Required] 
    [StringLength(256, MinimumLength = 1)]
    public string DisplayName { get; init; } = "Owlet Document Indexing Service";

    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string Description { get; init; } = "Indexes and searches local documents for fast retrieval";

    public ServiceStartMode StartMode { get; init; } = ServiceStartMode.Automatic;
    public ServiceAccount ServiceAccount { get; init; } = ServiceAccount.LocalSystem;
    
    [Range(typeof(TimeSpan), "00:00:10", "00:10:00")]
    public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromMinutes(2);
    
    public bool CanPauseAndContinue { get; init; } = false;
    public bool CanShutdown { get; init; } = true;
    public bool CanStop { get; init; } = true;
}

public enum ServiceStartMode
{
    Automatic,
    Manual,
    Disabled
}

public enum ServiceAccount
{
    LocalSystem,
    NetworkService,
    LocalService,
    User
}

[OptionsValidator]
public partial class NetworkConfigurationValidator : IValidateOptions<NetworkConfiguration>
{
    public ValidateOptionsResult Validate(string? name, NetworkConfiguration options)
    {
        var failures = new List<string>();

        if (options.Port < 1024)
            failures.Add("Port must be 1024 or higher (reserved ports not allowed)");
        else if (options.Port > 65535)
            failures.Add("Port must be 65535 or lower");

        if (string.IsNullOrWhiteSpace(options.BindAddress))
            failures.Add("BindAddress cannot be empty");
        else if (!IsValidIpAddress(options.BindAddress))
            failures.Add($"BindAddress '{options.BindAddress}' is not a valid IP address");

        if (options.EnableHttps)
        {
            if (string.IsNullOrWhiteSpace(options.CertificatePath))
                failures.Add("CertificatePath is required when HTTPS is enabled");
            else if (!File.Exists(options.CertificatePath))
                failures.Add($"Certificate file not found: {options.CertificatePath}");

            if (string.IsNullOrWhiteSpace(options.CertificatePassword))
                failures.Add("CertificatePassword is required when HTTPS is enabled");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }
}

public record NetworkConfiguration
{
    [Required]
    [Range(1024, 65535)]
    public int Port { get; init; } = 5555;

    [Required]
    [StringLength(45)] // IPv6 max length
    public string BindAddress { get; init; } = "127.0.0.1";

    public bool EnableHttps { get; init; } = false;

    public string? CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }

    [Range(1, 3600)] // 1 second to 1 hour
    public int RequestTimeoutSeconds { get; init; } = 30;

    [Range(1, 1000)]
    public int MaxConcurrentConnections { get; init; } = 100;
}

[OptionsValidator]
public partial class LoggingConfigurationValidator : IValidateOptions<LoggingConfiguration>
{
    public ValidateOptionsResult Validate(string? name, LoggingConfiguration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.LogDirectory))
            failures.Add("LogDirectory cannot be empty");
        else
        {
            try
            {
                var directory = new DirectoryInfo(options.LogDirectory);
                if (!directory.Exists)
                {
                    directory.Create(); // Attempt to create directory
                }
            }
            catch (Exception ex)
            {
                failures.Add($"Cannot create log directory '{options.LogDirectory}': {ex.Message}");
            }
        }

        if (options.MaxLogFileSizeBytes < 1024 * 1024) // 1MB minimum
            failures.Add("MaxLogFileSizeBytes must be at least 1MB");
        else if (options.MaxLogFileSizeBytes > 10L * 1024 * 1024 * 1024) // 10GB maximum
            failures.Add("MaxLogFileSizeBytes cannot exceed 10GB");

        if (options.RetainedLogFiles < 1)
            failures.Add("RetainedLogFiles must be at least 1");
        else if (options.RetainedLogFiles > 1000)
            failures.Add("RetainedLogFiles cannot exceed 1000");

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

public record LoggingConfiguration
{
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    [Required]
    [StringLength(2048)]
    public string LogDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Owlet", "Logs");

    [Range(1024 * 1024, 10L * 1024 * 1024 * 1024)] // 1MB to 10GB
    public long MaxLogFileSizeBytes { get; init; } = 100 * 1024 * 1024; // 100MB

    [Range(1, 1000)]
    public int RetainedLogFiles { get; init; } = 10;

    public bool EnableWindowsEventLog { get; init; } = true;
    public bool EnableConsole { get; init; } = false;
    public bool EnableStructuredLogging { get; init; } = true;
    public bool EnableFileLogging { get; init; } = true;

    [StringLength(100)]
    public string EventLogSource { get; init; } = "Owlet Service";

    [StringLength(100)]
    public string EventLogName { get; init; } = "Application";

    public LogEventLevel WindowsEventLogMinimumLevel { get; init; } = LogEventLevel.Warning;
}

public record DatabaseConfiguration
{
    [Required]
    [StringLength(2048)]
    public string ConnectionString { get; init; } = "Data Source=owlet.db";

    public string Provider { get; init; } = "Sqlite";

    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; init; } = 30;

    [Range(1, 1000)]
    public int MaxRetryCount { get; init; } = 3;

    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);

    public bool EnableSensitiveDataLogging { get; init; } = false;
    public bool EnableDetailedErrors { get; init; } = false;
}
```

### 2. Configuration Registration and Validation

Service registration with comprehensive validation and clear error reporting.

**Configuration Extensions:**

```csharp
namespace Owlet.Core.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddOwletConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Service configuration
        services.Configure<ServiceConfiguration>(configuration.GetSection("Service"));
        services.AddSingleton<IValidateOptions<ServiceConfiguration>, ServiceConfigurationValidator>();

        // Network configuration  
        services.Configure<NetworkConfiguration>(configuration.GetSection("Network"));
        services.AddSingleton<IValidateOptions<NetworkConfiguration>, NetworkConfigurationValidator>();

        // Logging configuration
        services.Configure<LoggingConfiguration>(configuration.GetSection("Logging"));
        services.AddSingleton<IValidateOptions<LoggingConfiguration>, LoggingConfigurationValidator>();

        // Database configuration
        services.Configure<DatabaseConfiguration>(configuration.GetSection("Database"));
        services.AddSingleton<IValidateOptions<DatabaseConfiguration>, DatabaseConfigurationValidator>();

        // Validate all configurations at startup
        services.AddSingleton<IStartupValidator, ConfigurationStartupValidator>();

        return services;
    }
}

public interface IStartupValidator
{
    Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    
    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(IEnumerable<string> errors) => new() 
    { 
        IsValid = false, 
        Errors = errors.ToArray() 
    };
}

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

        try
        {
            // Validate service configuration
            var serviceConfig = _serviceConfig.CurrentValue;
            _logger.LogDebug("Service configuration validated: {ServiceName}", serviceConfig.ServiceName);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Service configuration error: {f}"));
        }

        try
        {
            // Validate network configuration
            var networkConfig = _networkConfig.CurrentValue;
            _logger.LogDebug("Network configuration validated: Port {Port}, Address {Address}", 
                networkConfig.Port, networkConfig.BindAddress);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Network configuration error: {f}"));
        }

        try
        {
            // Validate logging configuration
            var loggingConfig = _loggingConfig.CurrentValue;
            _logger.LogDebug("Logging configuration validated: Directory {Directory}", 
                loggingConfig.LogDirectory);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Logging configuration error: {f}"));
        }

        try
        {
            // Validate database configuration
            var databaseConfig = _databaseConfig.CurrentValue;
            _logger.LogDebug("Database configuration validated: Provider {Provider}", 
                databaseConfig.Provider);
        }
        catch (OptionsValidationException ex)
        {
            errors.AddRange(ex.Failures.Select(f => $"Database configuration error: {f}"));
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
```

## Logging Infrastructure Implementation

### 1. Serilog Configuration and Setup

Production-grade logging with multiple sinks and structured logging support.

**Logging Infrastructure:**

```csharp
namespace Owlet.Infrastructure.Logging;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

public static class LoggingConfigurator
{
    public static ILogger CreateLogger(LoggingConfiguration config, IServiceProvider? serviceProvider = null)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(ConvertLogLevel(config.MinimumLevel))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Owlet")
            .Enrich.WithProperty("Version", GetApplicationVersion())
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.With<ServiceContextEnricher>();

        // File logging with rolling
        if (config.EnableFileLogging)
        {
            ConfigureFileLogging(loggerConfig, config);
        }

        // Structured JSON logging
        if (config.EnableStructuredLogging)
        {
            ConfigureStructuredLogging(loggerConfig, config);
        }

        // Windows Event Log
        if (config.EnableWindowsEventLog)
        {
            ConfigureWindowsEventLog(loggerConfig, config);
        }

        // Console logging (development scenarios)
        if (config.EnableConsole)
        {
            ConfigureConsoleLogging(loggerConfig, config);
        }

        return loggerConfig.CreateLogger();
    }

    private static void ConfigureFileLogging(LoggerConfiguration loggerConfig, LoggingConfiguration config)
    {
        var logPath = Path.Combine(config.LogDirectory, "owlet-.log");
        
        loggerConfig.WriteTo.File(
            path: logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: config.RetainedLogFiles,
            fileSizeLimitBytes: config.MaxLogFileSizeBytes,
            rollOnFileSizeLimit: true,
            shared: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext:l}] [{MachineName}] [{ProcessId}] {Message:lj}{NewLine}{Exception}");
    }

    private static void ConfigureStructuredLogging(LoggerConfiguration loggerConfig, LoggingConfiguration config)
    {
        var structuredLogPath = Path.Combine(config.LogDirectory, "owlet-structured-.json");
        
        loggerConfig.WriteTo.File(
            new JsonFormatter(),
            path: structuredLogPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: config.RetainedLogFiles,
            fileSizeLimitBytes: config.MaxLogFileSizeBytes,
            rollOnFileSizeLimit: true,
            shared: true);
    }

    private static void ConfigureWindowsEventLog(LoggerConfiguration loggerConfig, LoggingConfiguration config)
    {
        try
        {
            loggerConfig.WriteTo.EventLog(
                source: config.EventLogSource,
                logName: config.EventLogName,
                restrictedToMinimumLevel: config.WindowsEventLogMinimumLevel,
                outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}");
        }
        catch (Exception ex)
        {
            // If Windows Event Log fails (permissions, etc.), log to console instead
            Console.WriteLine($"Failed to configure Windows Event Log: {ex.Message}");
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] EventLog Fallback: {Message:lj}{NewLine}{Exception}");
        }
    }

    private static void ConfigureConsoleLogging(LoggerConfiguration loggerConfig, LoggingConfiguration config)
    {
        loggerConfig.WriteTo.Console(
            theme: AnsiConsoleTheme.Code,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
    }

    private static LogEventLevel ConvertLogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        LogLevel.None => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
               ?? assembly.GetName().Version?.ToString() 
               ?? "Unknown";
    }
}

public class ServiceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ServiceName", "OwletService"));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", 
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"));
    }
}

public static class LoggingExtensions
{
    public static IServiceCollection AddOwletLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var loggingConfig = configuration.GetSection("Logging").Get<LoggingConfiguration>() 
                           ?? new LoggingConfiguration();

        // Create the logger
        var logger = LoggingConfigurator.CreateLogger(loggingConfig);
        
        // Register Serilog
        services.AddSerilog(logger);
        
        // Add structured logging context
        services.AddScoped<ILoggingContext, LoggingContext>();
        
        return services;
    }
}

public interface ILoggingContext
{
    IDisposable BeginScope(string operationName);
    IDisposable BeginScope(string operationName, object parameters);
    void LogServiceEvent(string eventName, object? data = null);
    void LogPerformanceMetric(string metricName, TimeSpan duration, object? context = null);
}

public class LoggingContext : ILoggingContext
{
    private readonly ILogger<LoggingContext> _logger;

    public LoggingContext(ILogger<LoggingContext> logger)
    {
        _logger = logger;
    }

    public IDisposable BeginScope(string operationName)
    {
        return _logger.BeginScope("Operation: {OperationName}", operationName);
    }

    public IDisposable BeginScope(string operationName, object parameters)
    {
        return _logger.BeginScope("Operation: {OperationName} with {@Parameters}", operationName, parameters);
    }

    public void LogServiceEvent(string eventName, object? data = null)
    {
        if (data != null)
        {
            _logger.LogInformation("Service Event: {EventName} {@EventData}", eventName, data);
        }
        else
        {
            _logger.LogInformation("Service Event: {EventName}", eventName);
        }
    }

    public void LogPerformanceMetric(string metricName, TimeSpan duration, object? context = null)
    {
        _logger.LogInformation("Performance Metric: {MetricName} took {Duration}ms {@Context}",
            metricName, duration.TotalMilliseconds, context);
    }
}
```

## Windows Service Host Implementation

### 1. Service Host with Proper Lifecycle Management

Production-ready Windows service host with graceful startup and shutdown.

**Service Host Implementation:**

```csharp
namespace Owlet.Service.Host;

using Microsoft.Extensions.Hosting.WindowsServices;

public class OwletWindowsService : BackgroundService
{
    private readonly ILogger<OwletWindowsService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ServiceConfiguration> _serviceConfig;
    private readonly IStartupValidator _startupValidator;
    private readonly ILoggingContext _loggingContext;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public OwletWindowsService(
        ILogger<OwletWindowsService> logger,
        IServiceProvider serviceProvider,
        IOptionsMonitor<ServiceConfiguration> serviceConfig,
        IStartupValidator startupValidator,
        ILoggingContext loggingContext,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _serviceConfig = serviceConfig;
        _startupValidator = startupValidator;
        _loggingContext = loggingContext;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = _loggingContext.BeginScope("ServiceStartup");
        
        try
        {
            _logger.LogInformation("Owlet Windows Service starting...");
            
            // Validate configuration
            var validationResult = await _startupValidator.ValidateAsync(stoppingToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(Environment.NewLine, validationResult.Errors);
                _logger.LogCritical("Service startup failed due to configuration errors:{NewLine}{Errors}", 
                    Environment.NewLine, errors);
                    
                _applicationLifetime.StopApplication();
                return;
            }

            _loggingContext.LogServiceEvent("ConfigurationValidated");

            // Start all hosted services
            await StartHostedServicesAsync(stoppingToken);

            _loggingContext.LogServiceEvent("ServiceStarted", new 
            {
                ServiceName = _serviceConfig.CurrentValue.ServiceName,
                StartupTime = DateTime.UtcNow
            });

            _logger.LogInformation("Owlet Windows Service started successfully");

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Owlet Windows Service shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Owlet Windows Service failed to start");
            _applicationLifetime.StopApplication();
        }
        finally
        {
            _loggingContext.LogServiceEvent("ServiceStopping");
            _logger.LogInformation("Owlet Windows Service stopping...");
        }
    }

    private async Task StartHostedServicesAsync(CancellationToken cancellationToken)
    {
        var hostedServices = _serviceProvider.GetServices<IHostedService>()
            .Where(s => s != this) // Exclude self to avoid recursion
            .ToArray();

        _logger.LogInformation("Starting {HostedServiceCount} hosted services", hostedServices.Length);

        foreach (var service in hostedServices)
        {
            var serviceType = service.GetType().Name;
            
            try
            {
                using var scope = _loggingContext.BeginScope("HostedServiceStartup", new { ServiceType = serviceType });
                
                var stopwatch = Stopwatch.StartNew();
                await service.StartAsync(cancellationToken);
                stopwatch.Stop();

                _loggingContext.LogPerformanceMetric($"HostedService.{serviceType}.Startup", 
                    stopwatch.Elapsed, new { ServiceType = serviceType });

                _logger.LogInformation("Started hosted service: {ServiceType}", serviceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start hosted service: {ServiceType}", serviceType);
                throw;
            }
        }

        _logger.LogInformation("All hosted services started successfully");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        using var _ = _loggingContext.BeginScope("ServiceShutdown");
        
        _logger.LogInformation("Owlet Windows Service stopping gracefully...");

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await base.StopAsync(cancellationToken);
            
            stopwatch.Stop();
            _loggingContext.LogPerformanceMetric("ServiceShutdown", stopwatch.Elapsed);
            
            _loggingContext.LogServiceEvent("ServiceStopped", new
            {
                ShutdownTime = DateTime.UtcNow,
                ShutdownDuration = stopwatch.Elapsed
            });

            _logger.LogInformation("Owlet Windows Service stopped gracefully in {Duration}ms", 
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service shutdown");
            throw;
        }
    }
}

public static class ServiceHostExtensions
{
    public static IServiceCollection AddOwletWindowsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add configuration
        services.AddOwletConfiguration(configuration);
        
        // Add logging
        services.AddOwletLogging(configuration);
        
        // Add Windows service support
        services.AddWindowsService(options =>
        {
            var serviceConfig = configuration.GetSection("Service").Get<ServiceConfiguration>() 
                               ?? new ServiceConfiguration();
            options.ServiceName = serviceConfig.ServiceName;
        });

        // Add the main service
        services.AddHostedService<OwletWindowsService>();

        return services;
    }
}
```

### 2. Application Configuration Files

Complete configuration files with comprehensive settings and documentation.

**appsettings.json:**

```json
{
  "Service": {
    "ServiceName": "OwletService",
    "DisplayName": "Owlet Document Indexing Service",
    "Description": "Indexes and searches local documents for fast retrieval",
    "StartMode": "Automatic",
    "ServiceAccount": "LocalSystem",
    "StartupTimeout": "00:02:00",
    "CanPauseAndContinue": false,
    "CanShutdown": true,
    "CanStop": true
  },
  "Network": {
    "Port": 5555,
    "BindAddress": "127.0.0.1",
    "EnableHttps": false,
    "RequestTimeoutSeconds": 30,
    "MaxConcurrentConnections": 100
  },
  "Logging": {
    "MinimumLevel": "Information",
    "LogDirectory": "C:\\ProgramData\\Owlet\\Logs",
    "MaxLogFileSizeBytes": 104857600,
    "RetainedLogFiles": 10,
    "EnableWindowsEventLog": true,
    "EnableConsole": false,
    "EnableStructuredLogging": true,
    "EnableFileLogging": true,
    "EventLogSource": "Owlet Service",
    "EventLogName": "Application",
    "WindowsEventLogMinimumLevel": "Warning"
  },
  "Database": {
    "ConnectionString": "Data Source=C:\\ProgramData\\Owlet\\owlet.db",
    "Provider": "Sqlite",
    "CommandTimeoutSeconds": 30,
    "MaxRetryCount": 3,
    "RetryDelay": "00:00:05",
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false
  }
}
```

**appsettings.Production.json:**

```json
{
  "Logging": {
    "MinimumLevel": "Warning",
    "EnableConsole": false,
    "EnableStructuredLogging": true,
    "EnableDetailedErrors": false
  },
  "Database": {
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false
  }
}
```

**appsettings.Development.json:**

```json
{
  "Logging": {
    "MinimumLevel": "Debug",
    "LogDirectory": "C:\\temp\\owlet\\logs",
    "EnableConsole": true,
    "EnableWindowsEventLog": false,
    "WindowsEventLogMinimumLevel": "Error"
  },
  "Database": {
    "ConnectionString": "Data Source=owlet-dev.db",
    "EnableSensitiveDataLogging": true,
    "EnableDetailedErrors": true
  },
  "Network": {
    "Port": 5556
  }
}
```

## Error Handling and Resilience

### 1. Global Exception Handling

Comprehensive error handling with proper logging and recovery strategies.

**Error Handling Infrastructure:**

```csharp
namespace Owlet.Core.ErrorHandling;

public class GlobalExceptionHandler : IHostedService
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly ILoggingContext _loggingContext;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, ILoggingContext loggingContext)
    {
        _logger = logger;
        _loggingContext = loggingContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        _logger.LogInformation("Global exception handlers registered");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        
        _logger.LogInformation("Global exception handlers unregistered");
        return Task.CompletedTask;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _loggingContext.LogServiceEvent("UnhandledException", new 
            {
                IsTerminating = e.IsTerminating,
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            });

            _logger.LogCritical(ex, "Unhandled exception occurred. Terminating: {IsTerminating}", 
                e.IsTerminating);
        }
        else
        {
            _logger.LogCritical("Unhandled non-exception object: {ExceptionObject}", e.ExceptionObject);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _loggingContext.LogServiceEvent("UnobservedTaskException", new 
        {
            ExceptionType = e.Exception.GetType().Name,
            Message = e.Exception.Message,
            InnerExceptionCount = e.Exception.InnerExceptions.Count
        });

        _logger.LogError(e.Exception, "Unobserved task exception occurred");
        
        // Mark as observed to prevent application termination
        e.SetObserved();
    }
}

public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddOwletErrorHandling(this IServiceCollection services)
    {
        services.AddHostedService<GlobalExceptionHandler>();
        return services;
    }
}
```

### 2. Startup Validation and Health Checks

Comprehensive startup validation ensuring service health before accepting requests.

**Startup Validation:**

```csharp
namespace Owlet.Core.Health;

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

            // Check log directory
            var loggingConfig = _serviceConfig.CurrentValue;
            if (Directory.Exists(Path.GetDirectoryName("C:\\ProgramData\\Owlet\\Logs")))
            {
                checks.Add("Log directory accessible");
                data["LogDirectory"] = "Accessible";
            }
            else
            {
                data["LogDirectory"] = "Inaccessible";
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Log directory is not accessible",
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
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}

public class FileSystemHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<DatabaseConfiguration> _dbConfig;
    private readonly IOptionsMonitor<LoggingConfiguration> _loggingConfig;
    private readonly ILogger<FileSystemHealthCheck> _logger;

    public FileSystemHealthCheck(
        IOptionsMonitor<DatabaseConfiguration> dbConfig,
        IOptionsMonitor<LoggingConfiguration> loggingConfig,
        ILogger<FileSystemHealthCheck> logger)
    {
        _dbConfig = dbConfig;
        _loggingConfig = loggingConfig;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();
            var checks = new List<string>();

            // Check log directory
            var loggingConfig = _loggingConfig.CurrentValue;
            var logDirInfo = new DirectoryInfo(loggingConfig.LogDirectory);
            
            if (!logDirInfo.Exists)
            {
                try
                {
                    logDirInfo.Create();
                    checks.Add("Log directory created");
                }
                catch (Exception ex)
                {
                    data["LogDirectoryError"] = ex.Message;
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "Cannot create log directory",
                        ex,
                        data));
                }
            }
            else
            {
                checks.Add("Log directory exists");
            }

            data["LogDirectory"] = logDirInfo.FullName;
            data["LogDirectoryFreeSpace"] = GetFreeSpace(logDirInfo.FullName);

            // Check database directory
            var dbConfig = _dbConfig.CurrentValue;
            var dbPath = GetDatabasePath(dbConfig.ConnectionString);
            if (!string.IsNullOrEmpty(dbPath))
            {
                var dbDirInfo = new DirectoryInfo(Path.GetDirectoryName(dbPath)!);
                if (!dbDirInfo.Exists)
                {
                    try
                    {
                        dbDirInfo.Create();
                        checks.Add("Database directory created");
                    }
                    catch (Exception ex)
                    {
                        data["DatabaseDirectoryError"] = ex.Message;
                        return Task.FromResult(HealthCheckResult.Unhealthy(
                            "Cannot create database directory",
                            ex,
                            data));
                    }
                }
                else
                {
                    checks.Add("Database directory exists");
                }

                data["DatabaseDirectory"] = dbDirInfo.FullName;
                data["DatabaseDirectoryFreeSpace"] = GetFreeSpace(dbDirInfo.FullName);
            }

            data["Checks"] = checks;
            data["CheckTime"] = DateTime.UtcNow;

            return Task.FromResult(HealthCheckResult.Healthy(
                "File system is accessible",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File system health check failed");
            
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "File system health check failed",
                ex,
                new Dictionary<string, object>
                {
                    ["Exception"] = ex.Message,
                    ["CheckTime"] = DateTime.UtcNow
                }));
        }
    }

    private static string? GetDatabasePath(string connectionString)
    {
        // Extract file path from SQLite connection string
        var match = System.Text.RegularExpressions.Regex.Match(
            connectionString, 
            @"Data Source=([^;]+)", 
            RegexOptions.IgnoreCase);
            
        return match.Success ? match.Groups[1].Value : null;
    }

    private static long GetFreeSpace(string path)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(path)!);
            return drive.AvailableFreeSpace;
        }
        catch
        {
            return -1;
        }
    }
}

public static class HealthCheckExtensions
{
    public static IServiceCollection AddOwletHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ServiceHealthCheck>("service", tags: new[] { "service", "ready" })
            .AddCheck<FileSystemHealthCheck>("filesystem", tags: new[] { "filesystem", "ready" });

        return services;
    }
}
```

## Success Criteria

- [ ] Configuration system validates all settings at startup with clear error messages
- [ ] Serilog logging infrastructure writes to file system, Windows Event Log, and structured JSON
- [ ] Windows service host manages lifecycle properly with graceful startup and shutdown
- [ ] Global exception handlers catch and log unhandled exceptions
- [ ] Health checks validate service readiness before accepting requests
- [ ] All configuration supports environment-specific overrides
- [ ] Service starts successfully within 30 seconds on Windows 10/11
- [ ] Logging provides comprehensive troubleshooting information
- [ ] Error scenarios are properly handled and logged
- [ ] Configuration validation prevents invalid service states

## Testing Strategy

### Unit Tests
**What to test:** Configuration validation, logging setup, health check logic  
**Mocking strategy:** Mock file system operations, Windows Event Log  
**Test data approach:** Use temporary directories and in-memory configuration

**Example Tests:**
```csharp
[Fact]
public void ServiceConfiguration_WithInvalidPort_ShouldFailValidation()
{
    // Arrange
    var config = new NetworkConfiguration { Port = 80 }; // Below 1024
    var validator = new NetworkConfigurationValidator();
    
    // Act
    var result = validator.Validate("test", config);
    
    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("Port must be 1024 or higher"));
}

[Fact]
public void LoggingConfigurator_ShouldCreateValidLogger()
{
    // Arrange
    var config = new LoggingConfiguration
    {
        LogDirectory = Path.GetTempPath(),
        EnableFileLogging = true,
        EnableConsole = false
    };
    
    // Act
    var logger = LoggingConfigurator.CreateLogger(config);
    
    // Assert
    logger.Should().NotBeNull();
    logger.Information("Test message");
}
```

### Integration Tests
**What to test:** Service startup, configuration loading, Windows Event Log integration  
**Test environment:** Windows test environment with service registration  
**Automation:** PowerShell scripts for service lifecycle testing

### E2E Tests
**What to test:** Complete service installation, startup, health check, and log file creation  
**User workflows:** Install → Start → Check Health → Stop → Verify Logs

## Dependencies

### Technical Dependencies
- Microsoft.Extensions.Hosting.WindowsServices 9.0.0 - Windows service hosting
- Serilog.Extensions.Hosting 8.0.0 - Logging infrastructure
- Serilog.Sinks.File 6.0.0 - File logging
- Serilog.Sinks.EventLog 4.0.0 - Windows Event Log integration
- Microsoft.Extensions.Options.DataAnnotations 9.0.0 - Configuration validation

### Story Dependencies
- **Blocks:** S40 (Build Pipeline), S50 (WiX Installer), S60 (Health Monitoring)
- **Blocked By:** S20 (Solution Architecture)

## Next Steps

1. Implement configuration validation with comprehensive error messages
2. Set up Serilog with multiple sinks and structured logging
3. Create Windows service host with proper lifecycle management
4. Add health checks for service readiness validation
5. Test configuration loading and validation scenarios
6. Validate logging output across all configured sinks

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** [Date] (when status = Complete)