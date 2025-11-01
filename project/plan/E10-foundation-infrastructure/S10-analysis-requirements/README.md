# E10 S10: Analysis & Requirements

**Story:** Analyze foundation requirements, Windows service best practices, and configuration system design for production-ready service deployment  
**Priority:** Critical  
**Effort:** 16 hours  
**Status:** Complete  
**Dependencies:** None  

## Objective

This story establishes the foundational requirements and design principles for Owlet's Windows service architecture. It focuses on understanding Windows service best practices, defining configuration requirements, and establishing the architectural patterns that will guide all subsequent development.

The analysis ensures that Owlet follows production-ready Windows service patterns from the start, avoiding architectural debt and ensuring reliable deployment across diverse Windows environments. This work directly enables the professional installation experience that is critical to user adoption.

## Business Context

**Revenue Impact:** ₹0 direct revenue (foundational work)  
**User Impact:** All users impacted - this determines installation reliability and first-run experience  
**Compliance Requirements:** None specific, but establishes foundation for future compliance (Windows certification, enterprise security)

## Windows Service Architecture Analysis

### 1. Service Host Requirements

Understanding the optimal Windows service hosting patterns for .NET 9 applications with embedded web servers.

**Key Research Areas:**
- **Service Lifetime Management:** How to properly handle service start, stop, pause, and continue operations
- **Embedded Web Server:** Best practices for hosting Kestrel within a Windows service
- **Configuration Hot Reload:** Whether and how to support configuration changes without service restart
- **Error Recovery:** Service restart policies and failure handling
- **Security Context:** Appropriate service account selection (LocalSystem vs NetworkService vs custom)

**Requirements Analysis:**

```csharp
// Service host configuration requirements
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Owlet.Service.Host;

public record ServiceConfiguration
{
    public required string ServiceName { get; init; } = "OwletService";
    public required string DisplayName { get; init; } = "Owlet Document Indexing Service";
    public required string Description { get; init; } = "Indexes and searches local documents";
    public required ServiceStartMode StartMode { get; init; } = ServiceStartMode.Automatic;
    public required ServiceAccount ServiceAccount { get; init; } = ServiceAccount.LocalSystem;
    public required TimeSpan StartupTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public required bool CanPauseAndContinue { get; init; } = false;
    public required bool CanShutdown { get; init; } = true;
    public required bool CanStop { get; init; } = true;
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
```

### 2. Configuration System Architecture

Defining a robust configuration system that supports validation, environment-specific overrides, and hot reload capabilities.

**Configuration Sources Priority:**
1. Command line arguments (highest priority)
2. Environment variables
3. appsettings.{Environment}.json
4. appsettings.json
5. Default values (lowest priority)

**Configuration Validation Requirements:**

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Owlet.Core.Configuration;

[OptionsValidator]
public partial class ServiceConfigurationValidator : IValidateOptions<ServiceConfiguration>
{
    public ValidateOptionsResult Validate(string name, ServiceConfiguration options)
    {
        var builder = new ValidateOptionsResultBuilder();
        
        if (string.IsNullOrWhiteSpace(options.ServiceName))
            builder.AddError("ServiceName is required");
            
        if (options.Port < 1024 || options.Port > 65535)
            builder.AddError("Port must be between 1024 and 65535");
            
        if (options.StartupTimeout < TimeSpan.FromSeconds(10))
            builder.AddError("StartupTimeout must be at least 10 seconds");
            
        return builder.Build();
    }
}

public record NetworkConfiguration
{
    [Required]
    [Range(1024, 65535)]
    public int Port { get; init; } = 5555;
    
    [Required]
    public string BindAddress { get; init; } = "127.0.0.1";
    
    [Required]
    public bool EnableHttps { get; init; } = false;
    
    public string? CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }
}

public record LoggingConfiguration
{
    [Required]
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
    
    [Required]
    public string LogDirectory { get; init; } = @"C:\ProgramData\Owlet\Logs";
    
    [Required]
    public long MaxLogFileSizeBytes { get; init; } = 100 * 1024 * 1024; // 100MB
    
    [Required]
    public int RetainedLogFiles { get; init; } = 10;
    
    public bool EnableWindowsEventLog { get; init; } = true;
    public bool EnableConsole { get; init; } = false;
    public bool EnableStructuredLogging { get; init; } = true;
}
```

### 3. Logging Infrastructure Requirements

Establishing comprehensive logging that integrates with Windows service patterns and provides excellent troubleshooting capabilities.

**Logging Destinations:**
- **Windows Event Log:** Critical service lifecycle events
- **File System:** Detailed application logs with rotation
- **Structured Logging:** JSON format for log aggregation
- **Console Output:** Development and debugging scenarios

**Serilog Configuration Requirements:**

```csharp
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Owlet.Infrastructure.Logging;

public static class LoggingConfiguration
{
    public static ILogger CreateLogger(Core.Configuration.LoggingConfiguration config)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(ConvertLogLevel(config.MinimumLevel))
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Owlet")
            .Enrich.WithProperty("Version", GetApplicationVersion())
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();

        // File logging with rolling
        loggerConfig.WriteTo.File(
            path: Path.Combine(config.LogDirectory, "owlet-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: config.RetainedLogFiles,
            fileSizeLimitBytes: config.MaxLogFileSizeBytes,
            rollOnFileSizeLimit: true,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        // Structured JSON logging
        if (config.EnableStructuredLogging)
        {
            loggerConfig.WriteTo.File(
                new JsonFormatter(),
                path: Path.Combine(config.LogDirectory, "owlet-structured-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: config.RetainedLogFiles);
        }

        // Windows Event Log
        if (config.EnableWindowsEventLog)
        {
            loggerConfig.WriteTo.EventLog(
                source: "Owlet Service",
                logName: "Application",
                restrictedToMinimumLevel: LogEventLevel.Warning);
        }

        // Console (development)
        if (config.EnableConsole)
        {
            loggerConfig.WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        return loggerConfig.CreateLogger();
    }

    private static LogEventLevel ConvertLogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
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
```

## Service Registration Requirements

### 1. Windows Service Installation

Understanding the requirements for professional Windows service registration that works reliably across Windows versions.

**Service Registration Parameters:**
- **Service Name:** OwletService (unique, no conflicts with existing services)
- **Display Name:** Owlet Document Indexing Service
- **Description:** Indexes and searches local documents for fast retrieval
- **Start Type:** Automatic (starts with Windows)
- **Service Account:** LocalSystem (sufficient privileges, no password management)
- **Dependencies:** None (self-contained service)
- **Recovery Actions:** Restart service on failure (up to 3 attempts)

**Registry Requirements:**

```csharp
// Service registration will create these registry entries
// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OwletService

public record ServiceRegistryEntries
{
    public string ServiceName => "OwletService";
    public string DisplayName => "Owlet Document Indexing Service";
    public string Description => "Indexes and searches local documents";
    public string ImagePath => @"C:\Program Files\Owlet\Owlet.Service.exe";
    public ServiceType Type => ServiceType.Win32OwnProcess;
    public ServiceStartMode Start => ServiceStartMode.AutoStart;
    public ServiceErrorControl ErrorControl => ServiceErrorControl.Normal;
    public string ObjectName => "LocalSystem";
    
    // Recovery actions on failure
    public FailureActions FailureActions => new()
    {
        ResetPeriod = TimeSpan.FromDays(1),
        Actions = new[]
        {
            new ServiceAction(ServiceActionType.Restart, TimeSpan.FromMinutes(1)),
            new ServiceAction(ServiceActionType.Restart, TimeSpan.FromMinutes(2)),
            new ServiceAction(ServiceActionType.Restart, TimeSpan.FromMinutes(5))
        }
    };
}
```

### 2. Firewall Configuration

Automatic firewall rule creation for HTTP port access, ensuring the service can receive local requests without manual configuration.

**Firewall Rule Requirements:**

```csharp
public record FirewallRule
{
    public string Name => "Owlet Document Service - HTTP";
    public string Description => "Allow HTTP access to Owlet document indexing service";
    public FirewallDirection Direction => FirewallDirection.Inbound;
    public FirewallAction Action => FirewallAction.Allow;
    public FirewallProtocol Protocol => FirewallProtocol.TCP;
    public int Port { get; init; } = 5555;
    public string LocalAddresses => "127.0.0.1";
    public FirewallProfiles Profiles => FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public;
    public bool Enabled => true;
}
```

## Development Environment Requirements

### 1. Aspire Integration

Understanding how to integrate .NET Aspire for development scenarios while maintaining the pure Windows service approach for production.

**Dual Architecture Requirements:**
- **Production:** Owlet.Service.exe runs as pure Windows service
- **Development:** Owlet.AppHost.exe orchestrates services with Aspire dashboard
- **Shared Components:** Core business logic, API endpoints, configuration

**Project Structure Analysis:**

```
src/
├── Owlet.Service/              # Production Windows service host
│   ├── Program.cs              # Windows service entry point
│   ├── ServiceHost.cs          # Service implementation
│   └── Owlet.Service.csproj    # Self-contained deployment
├── Owlet.AppHost/              # Development orchestration
│   ├── Program.cs              # Aspire host entry point
│   └── Owlet.AppHost.csproj    # Aspire orchestration
├── Owlet.Core/                 # Shared business logic
│   ├── Services/               # Domain services
│   ├── Models/                 # Domain models
│   └── Owlet.Core.csproj       # Core domain library
├── Owlet.Api/                  # Shared HTTP API
│   ├── Endpoints/              # Carter endpoints
│   ├── Middleware/             # HTTP middleware
│   └── Owlet.Api.csproj        # HTTP API library
└── Owlet.ServiceDefaults/      # Shared Aspire configuration
    ├── Extensions.cs           # Service registration extensions
    └── Owlet.ServiceDefaults.csproj
```

### 2. Build System Requirements

Establishing build requirements that support both development (Aspire) and production (MSI) scenarios.

**Build Outputs:**
- **Development:** Aspire dashboard with service orchestration
- **Production:** Self-contained executable for Windows service
- **Installer:** MSI package with all dependencies included

**MSBuild Configuration:**

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>false</PublishTrimmed>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" />
    <PackageReference Include="Serilog.Extensions.Hosting" />
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.EventLog" />
  </ItemGroup>
</Project>
```

## Performance Requirements

### 1. Service Startup Performance

**Requirements:**
- **Cold Start:** Service must reach healthy state within 30 seconds
- **Configuration Load:** All configuration validation must complete within 5 seconds
- **Port Binding:** HTTP server must bind and start listening within 10 seconds
- **Health Check Response:** Initial health check must respond within 5 seconds

**Measurement Strategy:**

```csharp
public class ServiceStartupMetrics
{
    public TimeSpan ConfigurationLoadTime { get; init; }
    public TimeSpan DependencyRegistrationTime { get; init; }
    public TimeSpan WebServerStartupTime { get; init; }
    public TimeSpan HealthCheckInitializationTime { get; init; }
    public TimeSpan TotalStartupTime { get; init; }
    
    public bool MeetsPerformanceRequirements =>
        TotalStartupTime < TimeSpan.FromSeconds(30) &&
        ConfigurationLoadTime < TimeSpan.FromSeconds(5) &&
        WebServerStartupTime < TimeSpan.FromSeconds(10);
}
```

### 2. Memory Usage Requirements

**Requirements:**
- **Base Service:** < 50MB memory usage with no indexing activity
- **Configuration:** < 5MB for all configuration and logging infrastructure
- **HTTP Server:** < 10MB for Kestrel and Carter API infrastructure
- **Growth Pattern:** Linear memory growth with indexed document count (addressed in E20)

## Security Requirements

### 1. Service Account Security

**Requirements:**
- **Minimal Privileges:** Service runs with least required privileges
- **Network Access:** No outbound network access required (local-first architecture)
- **File System Access:** Read/write access to ProgramData\Owlet directory only
- **Registry Access:** Read access to service configuration keys only

### 2. Network Security

**Requirements:**
- **Local Only:** HTTP server binds to 127.0.0.1 (no external access)
- **No HTTPS Required:** Local communication over HTTP acceptable
- **Firewall Integration:** Automatic firewall rule creation for required port
- **Port Configuration:** Configurable port with validation (default 5555)

## Success Criteria

- [ ] Windows service hosting pattern defined with proper lifecycle management
- [ ] Configuration system supports validation, environment overrides, and hot reload
- [ ] Logging infrastructure integrates Windows Event Log, file system, and structured logging
- [ ] Service registration requirements documented for reliable installation
- [ ] Firewall rule creation process defined for automatic port configuration
- [ ] Dual architecture (service/Aspire) requirements clearly separated
- [ ] Build system requirements support both development and production scenarios
- [ ] Performance requirements established with measurement strategy
- [ ] Security requirements defined for minimal privilege operation
- [ ] All requirements validated against Windows 10/11 compatibility

## Testing Strategy

### Requirements Validation
**What to test:** All requirement definitions against real Windows environments
**Validation approach:** 
- Research current Windows service best practices
- Validate configuration patterns with .NET 9 hosting
- Test logging integration with Windows Event Log
- Verify firewall rule creation across Windows versions

**Example Validation:**
```csharp
[Fact]
public void ServiceConfiguration_ShouldValidateRequiredFields()
{
    // Arrange
    var config = new ServiceConfiguration
    {
        ServiceName = "",  // Invalid
        Port = 80,         // Invalid (below 1024)
        StartupTimeout = TimeSpan.FromSeconds(5)  // Invalid (below 10)
    };
    
    var validator = new ServiceConfigurationValidator();
    
    // Act
    var result = validator.Validate("test", config);
    
    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("ServiceName"));
    result.Failures.Should().Contain(f => f.Contains("Port"));
    result.Failures.Should().Contain(f => f.Contains("StartupTimeout"));
}
```

### Integration Tests
**What to test:** Windows service registration and configuration loading
**Test environment:** Windows VM with clean state
**Automation:** PowerShell scripts for service installation testing

### E2E Tests
**What to test:** Complete service lifecycle from installation to health check
**User workflows:** Install → Start → Configure → Health Check → Stop → Uninstall

## Dependencies

### Technical Dependencies
- Microsoft.Extensions.Hosting.WindowsServices 9.0.0 - Windows service hosting
- Serilog.Extensions.Hosting 8.0.0 - Logging infrastructure
- System.ServiceProcess.ServiceController 9.0.0 - Service management

### Story Dependencies
- **Blocks:** All subsequent stories (S20-S80) depend on these requirements
- **Blocked By:** None (foundational analysis)

## Next Steps

1. Complete requirements analysis and validation
2. Begin S20: Solution Architecture with project structure design
3. Validate Windows service patterns with prototype implementation
4. Research WiX installer requirements for S50

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** November 1, 2025