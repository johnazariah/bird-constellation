# E10 S60: Health Monitoring

**Story:** Implement comprehensive health check endpoints, service status monitoring, and diagnostics system for operational visibility  
**Priority:** High  
**Effort:** 18 hours  
**Status:** Not Started  
**Dependencies:** S30 (Core Infrastructure)  

## Objective

This story implements a comprehensive health monitoring and diagnostics system that provides operational visibility into the Owlet service. It includes health check endpoints for service monitoring, detailed service status reporting, diagnostic tools for troubleshooting, and integration with Windows Event Log and structured logging for comprehensive observability.

The health monitoring system enables proactive service management, automated monitoring integration, and rapid troubleshooting of service issues. It provides both HTTP endpoints for programmatic access and command-line tools for administrative tasks.

## Business Context

**Revenue Impact:** ₹0 direct revenue (reduces operational costs through automated monitoring and faster issue resolution)  
**User Impact:** System administrators and support teams - enables proactive monitoring and rapid troubleshooting  
**Compliance Requirements:** Audit logging and service availability monitoring for enterprise deployment

## Health Monitoring Architecture

### 1. Health Check System

Comprehensive health checks covering all service components and dependencies.

**`src/Owlet.Infrastructure/Health/HealthCheckExtensions.cs`:**

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Owlet.Infrastructure.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddOwletHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "database" })
            .AddCheck<FileSystemHealthCheck>("filesystem", tags: new[] { "ready", "filesystem" })
            .AddCheck<IndexerHealthCheck>("indexer", tags: new[] { "ready", "indexer" })
            .AddCheck<ExtractorHealthCheck>("extractor", tags: new[] { "ready", "extractor" })
            .AddCheck<MemoryHealthCheck>("memory", tags: new[] { "live", "memory" })
            .AddCheck<DiskSpaceHealthCheck>("disk", tags: new[] { "live", "disk" })
            .AddCheck<NetworkHealthCheck>("network", tags: new[] { "ready", "network" });

        services.AddSingleton<IHealthCheckPublisher, EventLogHealthPublisher>();
        services.AddSingleton<IHealthCheckPublisher, StructuredLogHealthPublisher>();
        
        return services;
    }

    public static IApplicationBuilder UseOwletHealthChecks(this IApplicationBuilder app)
    {
        // Liveness probe - basic service health
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        // Readiness probe - full service readiness
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

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = result.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = result.TotalDuration,
            version = GetServiceVersion()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = result.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = result.TotalDuration,
            version = GetServiceVersion(),
            checks = result.Entries.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.Status.ToString(),
                duration = kvp.Value.Duration,
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

    private static string GetServiceVersion()
    {
        return typeof(HealthCheckExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Unknown";
    }
}
```

### 2. Database Health Check

Comprehensive database connectivity and performance health monitoring.

**`src/Owlet.Infrastructure/Health/DatabaseHealthCheck.cs`:**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Owlet.Core.Results;
using Owlet.Infrastructure.Data;
using System.Diagnostics;

namespace Owlet.Infrastructure.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly OwletDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(OwletDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>();

            // Test basic connectivity
            var connectionResult = await TestDatabaseConnection(cancellationToken);
            if (connectionResult.IsFailure)
            {
                return HealthCheckResult.Unhealthy(
                    $"Database connection failed: {connectionResult.Error}",
                    data: data);
            }

            data["connectionTime"] = connectionResult.Value;

            // Test query performance
            var queryResult = await TestQueryPerformance(cancellationToken);
            if (queryResult.IsFailure)
            {
                return HealthCheckResult.Degraded(
                    $"Database queries slow: {queryResult.Error}",
                    data: data);
            }

            data["queryTime"] = queryResult.Value;

            // Check database size and growth
            var sizeResult = await GetDatabaseMetrics(cancellationToken);
            if (sizeResult.IsSuccess)
            {
                data["databaseSize"] = sizeResult.Value.SizeMB;
                data["tableCount"] = sizeResult.Value.TableCount;
                data["indexCount"] = sizeResult.Value.IndexCount;
            }

            stopwatch.Stop();
            data["totalCheckTime"] = stopwatch.ElapsedMilliseconds;

            var status = GetHealthStatus(connectionResult.Value, queryResult.Value);
            var description = $"Database health check completed in {stopwatch.ElapsedMilliseconds}ms";

            return new HealthCheckResult(status, description, data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return HealthCheckResult.Unhealthy(
                $"Database health check exception: {ex.Message}",
                exception: ex);
        }
    }

    private async Task<Result<long>> TestDatabaseConnection(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            
            stopwatch.Stop();

            if (!canConnect)
            {
                return Result<long>.Failure("Cannot connect to database");
            }

            return Result<long>.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure($"Database connection test failed: {ex.Message}");
        }
    }

    private async Task<Result<long>> TestQueryPerformance(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simple count query to test performance
            var documentCount = await _dbContext.Documents
                .CountAsync(cancellationToken);
            
            stopwatch.Stop();

            _logger.LogDebug("Database query performance test: {DocumentCount} documents in {ElapsedMs}ms",
                documentCount, stopwatch.ElapsedMilliseconds);

            return Result<long>.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure($"Database query test failed: {ex.Message}");
        }
    }

    private async Task<Result<DatabaseMetrics>> GetDatabaseMetrics(CancellationToken cancellationToken)
    {
        try
        {
            // Get database size (SQLite specific)
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    page_count * page_size as size_bytes,
                    (SELECT COUNT(*) FROM sqlite_master WHERE type='table') as table_count,
                    (SELECT COUNT(*) FROM sqlite_master WHERE type='index') as index_count
                FROM pragma_page_count(), pragma_page_size()";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (await reader.ReadAsync(cancellationToken))
            {
                var sizeBytes = reader.GetInt64(0);
                var tableCount = reader.GetInt32(1);
                var indexCount = reader.GetInt32(2);

                var metrics = new DatabaseMetrics
                {
                    SizeMB = Math.Round(sizeBytes / (1024.0 * 1024.0), 2),
                    TableCount = tableCount,
                    IndexCount = indexCount
                };

                return Result<DatabaseMetrics>.Success(metrics);
            }

            return Result<DatabaseMetrics>.Failure("Could not retrieve database metrics");
        }
        catch (Exception ex)
        {
            return Result<DatabaseMetrics>.Failure($"Database metrics retrieval failed: {ex.Message}");
        }
    }

    private static HealthStatus GetHealthStatus(long connectionTime, long queryTime)
    {
        // Connection time thresholds
        if (connectionTime > 5000) // 5 seconds
            return HealthStatus.Unhealthy;
        
        if (connectionTime > 1000) // 1 second
            return HealthStatus.Degraded;

        // Query time thresholds
        if (queryTime > 2000) // 2 seconds
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }

    public record DatabaseMetrics
    {
        public double SizeMB { get; init; }
        public int TableCount { get; init; }
        public int IndexCount { get; init; }
    }
}
```

### 3. File System Health Check

Monitor file system access, permissions, and disk space.

**`src/Owlet.Infrastructure/Health/FileSystemHealthCheck.cs`:**

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Owlet.Core.Results;
using System.Diagnostics;

namespace Owlet.Infrastructure.Health;

public class FileSystemHealthCheck : IHealthCheck
{
    private readonly ServiceConfiguration _configuration;
    private readonly ILogger<FileSystemHealthCheck> _logger;

    public FileSystemHealthCheck(
        IOptions<ServiceConfiguration> configuration,
        ILogger<FileSystemHealthCheck> logger)
    {
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var data = new Dictionary<string, object>();

            // Check data directory access
            var dataResult = await CheckDirectoryAccess(
                _configuration.DataDirectory, 
                "data", 
                cancellationToken);
            
            if (dataResult.IsFailure)
            {
                return HealthCheckResult.Unhealthy(
                    $"Data directory access failed: {dataResult.Error}",
                    data: data);
            }

            data["dataDirectory"] = dataResult.Value;

            // Check log directory access
            var logResult = await CheckDirectoryAccess(
                _configuration.LogDirectory, 
                "log", 
                cancellationToken);
            
            if (logResult.IsFailure)
            {
                return HealthCheckResult.Degraded(
                    $"Log directory access issues: {logResult.Error}",
                    data: data);
            }

            data["logDirectory"] = logResult.Value;

            // Check temporary directory access
            var tempResult = await CheckDirectoryAccess(
                _configuration.TempDirectory, 
                "temp", 
                cancellationToken);
            
            if (tempResult.IsFailure)
            {
                return HealthCheckResult.Degraded(
                    $"Temp directory access issues: {tempResult.Error}",
                    data: data);
            }

            data["tempDirectory"] = tempResult.Value;

            // Check disk space
            var diskResult = await CheckDiskSpace(_configuration.DataDirectory, cancellationToken);
            if (diskResult.IsSuccess)
            {
                data["diskSpace"] = diskResult.Value;
            }

            stopwatch.Stop();
            data["totalCheckTime"] = stopwatch.ElapsedMilliseconds;

            var status = DetermineOverallStatus(dataResult, logResult, tempResult, diskResult);
            var description = $"File system health check completed in {stopwatch.ElapsedMilliseconds}ms";

            return new HealthCheckResult(status, description, data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File system health check failed with exception");
            return HealthCheckResult.Unhealthy(
                $"File system health check exception: {ex.Message}",
                exception: ex);
        }
    }

    private async Task<Result<DirectoryHealthInfo>> CheckDirectoryAccess(
        string directoryPath, 
        string directoryType,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Check if directory exists
            if (!Directory.Exists(directoryPath))
            {
                return Result<DirectoryHealthInfo>.Failure($"Directory does not exist: {directoryPath}");
            }

            // Test read access
            var readResult = await TestDirectoryRead(directoryPath, cancellationToken);
            if (readResult.IsFailure)
            {
                return Result<DirectoryHealthInfo>.Failure(
                    $"Read access failed for {directoryType} directory: {readResult.Error}");
            }

            // Test write access
            var writeResult = await TestDirectoryWrite(directoryPath, cancellationToken);
            if (writeResult.IsFailure)
            {
                return Result<DirectoryHealthInfo>.Failure(
                    $"Write access failed for {directoryType} directory: {writeResult.Error}");
            }

            stopwatch.Stop();

            var info = new DirectoryHealthInfo
            {
                Path = directoryPath,
                Type = directoryType,
                ReadAccessTime = readResult.Value,
                WriteAccessTime = writeResult.Value,
                TotalTestTime = stopwatch.ElapsedMilliseconds
            };

            return Result<DirectoryHealthInfo>.Success(info);
        }
        catch (Exception ex)
        {
            return Result<DirectoryHealthInfo>.Failure(
                $"Directory access test failed for {directoryType}: {ex.Message}");
        }
    }

    private async Task<Result<long>> TestDirectoryRead(string directoryPath, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Try to enumerate files (this tests read permissions)
            var files = Directory.EnumerateFiles(directoryPath).Take(10);
            var fileCount = 0;
            
            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    fileCount++;
                }
            }, cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogDebug("Directory read test for {DirectoryPath}: {FileCount} files in {ElapsedMs}ms",
                directoryPath, fileCount, stopwatch.ElapsedMilliseconds);
            
            return Result<long>.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure($"Directory read test failed: {ex.Message}");
        }
    }

    private async Task<Result<long>> TestDirectoryWrite(string directoryPath, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var testFileName = Path.Combine(directoryPath, $"health-check-{Guid.NewGuid():N}.tmp");
            
            // Write test file
            await File.WriteAllTextAsync(testFileName, "health check test", cancellationToken);
            
            // Verify file exists and read it back
            if (!File.Exists(testFileName))
            {
                return Result<long>.Failure("Test file was not created");
            }
            
            var content = await File.ReadAllTextAsync(testFileName, cancellationToken);
            if (content != "health check test")
            {
                return Result<long>.Failure("Test file content verification failed");
            }
            
            // Clean up test file
            File.Delete(testFileName);
            
            stopwatch.Stop();
            
            _logger.LogDebug("Directory write test for {DirectoryPath} completed in {ElapsedMs}ms",
                directoryPath, stopwatch.ElapsedMilliseconds);
            
            return Result<long>.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return Result<long>.Failure($"Directory write test failed: {ex.Message}");
        }
    }

    private async Task<Result<DiskSpaceInfo>> CheckDiskSpace(string directoryPath, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Yield(); // Make async for consistency
            
            var driveInfo = new DriveInfo(Path.GetPathRoot(directoryPath)!);
            
            var totalGB = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0), 2);
            var freeGB = Math.Round(driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
            var usedGB = Math.Round((driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (1024.0 * 1024.0 * 1024.0), 2);
            var freePercent = Math.Round((double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize * 100, 1);

            var info = new DiskSpaceInfo
            {
                Drive = driveInfo.Name,
                TotalGB = totalGB,
                FreeGB = freeGB,
                UsedGB = usedGB,
                FreePercent = freePercent
            };

            return Result<DiskSpaceInfo>.Success(info);
        }
        catch (Exception ex)
        {
            return Result<DiskSpaceInfo>.Failure($"Disk space check failed: {ex.Message}");
        }
    }

    private static HealthStatus DetermineOverallStatus(
        Result<DirectoryHealthInfo> dataResult,
        Result<DirectoryHealthInfo> logResult,
        Result<DirectoryHealthInfo> tempResult,
        Result<DiskSpaceInfo> diskResult)
    {
        // Data directory is critical
        if (dataResult.IsFailure)
            return HealthStatus.Unhealthy;

        // Check disk space if available
        if (diskResult.IsSuccess && diskResult.Value.FreePercent < 5)
            return HealthStatus.Unhealthy;
        
        if (diskResult.IsSuccess && diskResult.Value.FreePercent < 15)
            return HealthStatus.Degraded;

        // Log or temp directory issues are degraded
        if (logResult.IsFailure || tempResult.IsFailure)
            return HealthStatus.Degraded;

        // Check performance thresholds
        if (dataResult.Value.WriteAccessTime > 1000) // 1 second
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }

    public record DirectoryHealthInfo
    {
        public string Path { get; init; } = "";
        public string Type { get; init; } = "";
        public long ReadAccessTime { get; init; }
        public long WriteAccessTime { get; init; }
        public long TotalTestTime { get; init; }
    }

    public record DiskSpaceInfo
    {
        public string Drive { get; init; } = "";
        public double TotalGB { get; init; }
        public double FreeGB { get; init; }
        public double UsedGB { get; init; }
        public double FreePercent { get; init; }
    }
}
```

### 4. Service Diagnostics System

Comprehensive diagnostic tools for service troubleshooting and status reporting.

**`tools/Owlet.Diagnostics/Program.cs`:**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Owlet.Diagnostics.Commands;
using Owlet.Diagnostics.Services;
using System.CommandLine;

namespace Owlet.Diagnostics;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var rootCommand = CreateRootCommand();
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Owlet Diagnostics Tool")
        {
            Name = "owlet-diagnostics"
        };

        // Service status command
        var statusCommand = new Command("status", "Check service status and health")
        {
            new Option<bool>("--detailed", "Show detailed health information"),
            new Option<bool>("--json", "Output in JSON format"),
            new Option<string>("--output", "Output file path")
        };
        statusCommand.SetHandler(StatusCommand.Handle);

        // Service logs command
        var logsCommand = new Command("logs", "View and analyze service logs")
        {
            new Option<int>("--lines", () => 100, "Number of recent log lines to show"),
            new Option<string>("--level", "Filter by log level (Debug, Info, Warning, Error)"),
            new Option<DateTime?>("--since", "Show logs since specific date/time"),
            new Option<bool>("--follow", "Follow live log output"),
            new Option<string>("--output", "Output file path")
        };
        logsCommand.SetHandler(LogsCommand.Handle);

        // Network diagnostics command
        var networkCommand = new Command("network", "Test network connectivity and ports")
        {
            new Option<string>("--host", () => "localhost", "Target host to test"),
            new Option<int>("--port", () => 5555, "Target port to test"),
            new Option<bool>("--firewall", "Check Windows Firewall rules"),
            new Option<bool>("--http", "Test HTTP endpoint connectivity")
        };
        networkCommand.SetHandler(NetworkCommand.Handle);

        // Performance monitoring command
        var perfCommand = new Command("performance", "Monitor service performance metrics")
        {
            new Option<int>("--duration", () => 60, "Monitoring duration in seconds"),
            new Option<int>("--interval", () => 5, "Sample interval in seconds"),
            new Option<bool>("--cpu", "Monitor CPU usage"),
            new Option<bool>("--memory", "Monitor memory usage"),
            new Option<bool>("--disk", "Monitor disk I/O"),
            new Option<string>("--output", "Output file path")
        };
        perfCommand.SetHandler(PerformanceCommand.Handle);

        // Database diagnostics command
        var dbCommand = new Command("database", "Diagnose database issues")
        {
            new Option<bool>("--integrity", "Check database integrity"),
            new Option<bool>("--size", "Show database size and statistics"),
            new Option<bool>("--optimize", "Optimize database (vacuum, reindex)"),
            new Option<bool>("--backup", "Create database backup"),
            new Option<string>("--backup-path", "Backup file path")
        };
        dbCommand.SetHandler(DatabaseCommand.Handle);

        // Configuration validation command
        var configCommand = new Command("config", "Validate and display configuration")
        {
            new Option<bool>("--validate", "Validate configuration"),
            new Option<bool>("--show", "Display current configuration"),
            new Option<bool>("--masked", "Mask sensitive values"),
            new Option<string>("--environment", "Target environment")
        };
        configCommand.SetHandler(ConfigCommand.Handle);

        // Repair command
        var repairCommand = new Command("repair", "Attempt automatic repairs")
        {
            new Option<bool>("--service", "Repair service registration"),
            new Option<bool>("--firewall", "Repair firewall rules"),
            new Option<bool>("--permissions", "Repair file permissions"),
            new Option<bool>("--database", "Repair database issues"),
            new Option<bool>("--all", "Attempt all repairs")
        };
        repairCommand.SetHandler(RepairCommand.Handle);

        rootCommand.AddCommand(statusCommand);
        rootCommand.AddCommand(logsCommand);
        rootCommand.AddCommand(networkCommand);
        rootCommand.AddCommand(perfCommand);
        rootCommand.AddCommand(dbCommand);
        rootCommand.AddCommand(configCommand);
        rootCommand.AddCommand(repairCommand);

        return rootCommand;
    }
}
```

### 5. Health Check Publishers

Structured logging and event log publishing for health check results.

**`src/Owlet.Infrastructure/Health/EventLogHealthPublisher.cs`:**

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Owlet.Infrastructure.Health;

public class EventLogHealthPublisher : IHealthCheckPublisher
{
    private readonly ILogger<EventLogHealthPublisher> _logger;
    private readonly EventLog _eventLog;

    public EventLogHealthPublisher(ILogger<EventLogHealthPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        try
        {
            if (!EventLog.SourceExists("Owlet Service"))
            {
                EventLog.CreateEventSource("Owlet Service", "Application");
            }
            _eventLog = new EventLog("Application", ".", "Owlet Service");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize Event Log for health check publishing");
            _eventLog = null!;
        }
    }

    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        try
        {
            if (_eventLog == null)
                return;

            await Task.Yield(); // Make async for interface compliance

            var previousStatus = GetPreviousHealthStatus();
            var currentStatus = report.Status;

            // Only log on status changes or critical issues
            if (ShouldLogHealthStatus(previousStatus, currentStatus, report))
            {
                var eventType = GetEventLogEntryType(currentStatus);
                var message = FormatHealthMessage(report);
                var eventId = GetEventId(currentStatus);

                _eventLog.WriteEntry(message, eventType, eventId);
                
                _logger.LogInformation("Health status published to Event Log: {Status}", currentStatus);
            }

            // Update stored status
            SetPreviousHealthStatus(currentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish health status to Event Log");
        }
    }

    private static bool ShouldLogHealthStatus(
        HealthStatus? previousStatus, 
        HealthStatus currentStatus, 
        HealthReport report)
    {
        // Always log status changes
        if (previousStatus != currentStatus)
            return true;

        // Always log unhealthy status (every check)
        if (currentStatus == HealthStatus.Unhealthy)
            return true;

        // Log degraded status periodically (every 10th check)
        if (currentStatus == HealthStatus.Degraded)
        {
            var checkCount = GetHealthCheckCount();
            return checkCount % 10 == 0;
        }

        // Don't log healthy status unless it's a recovery
        return false;
    }

    private static EventLogEntryType GetEventLogEntryType(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => EventLogEntryType.Information,
            HealthStatus.Degraded => EventLogEntryType.Warning,
            HealthStatus.Unhealthy => EventLogEntryType.Error,
            _ => EventLogEntryType.Information
        };
    }

    private static int GetEventId(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => 1001,
            HealthStatus.Degraded => 1002,
            HealthStatus.Unhealthy => 1003,
            _ => 1000
        };
    }

    private static string FormatHealthMessage(HealthReport report)
    {
        var message = $"Owlet Service Health Status: {report.Status}\n";
        message += $"Check Duration: {report.TotalDuration.TotalMilliseconds:F0}ms\n";
        message += $"Timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\n\n";

        if (report.Entries.Any())
        {
            message += "Component Status:\n";
            foreach (var entry in report.Entries)
            {
                message += $"  {entry.Key}: {entry.Value.Status}";
                
                if (entry.Value.Status != HealthStatus.Healthy && !string.IsNullOrEmpty(entry.Value.Description))
                {
                    message += $" - {entry.Value.Description}";
                }
                
                if (entry.Value.Exception != null)
                {
                    message += $" (Exception: {entry.Value.Exception.Message})";
                }
                
                message += "\n";
            }
        }

        return message;
    }

    // Simple in-memory status tracking (could be enhanced with persistent storage)
    private static HealthStatus? _previousStatus;
    private static int _healthCheckCount;

    private static HealthStatus? GetPreviousHealthStatus() => _previousStatus;
    private static void SetPreviousHealthStatus(HealthStatus status) => _previousStatus = status;
    private static int GetHealthCheckCount() => Interlocked.Increment(ref _healthCheckCount);

    public void Dispose()
    {
        _eventLog?.Dispose();
    }
}
```

### 6. Service Status Monitor

Background service for continuous health monitoring and alerting.

**`src/Owlet.Infrastructure/Health/HealthMonitorService.cs`:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;

namespace Owlet.Infrastructure.Health;

public class HealthMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly HealthMonitorConfiguration _configuration;

    public HealthMonitorService(
        IServiceProvider serviceProvider,
        ILogger<HealthMonitorService> logger,
        IOptions<HealthMonitorConfiguration> configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health monitor service starting with interval {IntervalSeconds}s", 
            _configuration.CheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheck(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_configuration.CheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health monitoring cycle");
                
                // Wait before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Health monitor service stopped");
    }

    private async Task PerformHealthCheck(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

        try
        {
            var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);
            
            _logger.LogDebug("Health check completed: {Status} in {Duration}ms",
                healthReport.Status, 
                healthReport.TotalDuration.TotalMilliseconds);

            // Analyze health trends
            await AnalyzeHealthTrends(healthReport, cancellationToken);

            // Check for degradation patterns
            await CheckDegradationPatterns(healthReport, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check execution failed");
        }
    }

    private async Task AnalyzeHealthTrends(HealthReport report, CancellationToken cancellationToken)
    {
        // Store health check results for trend analysis
        var healthRecord = new HealthCheckRecord
        {
            Timestamp = DateTimeOffset.UtcNow,
            Status = report.Status,
            Duration = report.TotalDuration,
            ComponentResults = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new ComponentHealthRecord
                {
                    Status = kvp.Value.Status,
                    Duration = kvp.Value.Duration,
                    Description = kvp.Value.Description
                })
        };

        // TODO: Persist to database for trend analysis
        // For now, just log significant changes
        await LogHealthTrends(healthRecord, cancellationToken);
    }

    private async Task CheckDegradationPatterns(HealthReport report, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make async for future enhancements

        // Check for concerning patterns
        var concerningComponents = report.Entries
            .Where(kvp => kvp.Value.Status == HealthStatus.Degraded)
            .Select(kvp => kvp.Key)
            .ToList();

        if (concerningComponents.Any())
        {
            _logger.LogWarning("Components showing degraded performance: {Components}",
                string.Join(", ", concerningComponents));
        }

        // Check for slow response times
        var slowComponents = report.Entries
            .Where(kvp => kvp.Value.Duration.TotalMilliseconds > 1000)
            .Select(kvp => new { Component = kvp.Key, Duration = kvp.Value.Duration.TotalMilliseconds })
            .ToList();

        if (slowComponents.Any())
        {
            _logger.LogWarning("Components with slow response times: {SlowComponents}",
                string.Join(", ", slowComponents.Select(c => $"{c.Component}({c.Duration:F0}ms)")));
        }
    }

    private async Task LogHealthTrends(HealthCheckRecord record, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make async for future database operations

        // Log structured data for analysis
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["HealthCheckTimestamp"] = record.Timestamp,
            ["HealthCheckStatus"] = record.Status.ToString(),
            ["HealthCheckDuration"] = record.Duration.TotalMilliseconds,
            ["ComponentCount"] = record.ComponentResults.Count,
            ["HealthyComponents"] = record.ComponentResults.Count(c => c.Value.Status == HealthStatus.Healthy),
            ["DegradedComponents"] = record.ComponentResults.Count(c => c.Value.Status == HealthStatus.Degraded),
            ["UnhealthyComponents"] = record.ComponentResults.Count(c => c.Value.Status == HealthStatus.Unhealthy)
        });

        _logger.LogInformation("Health check trend data recorded");
    }

    private record HealthCheckRecord
    {
        public DateTimeOffset Timestamp { get; init; }
        public HealthStatus Status { get; init; }
        public TimeSpan Duration { get; init; }
        public Dictionary<string, ComponentHealthRecord> ComponentResults { get; init; } = new();
    }

    private record ComponentHealthRecord
    {
        public HealthStatus Status { get; init; }
        public TimeSpan Duration { get; init; }
        public string? Description { get; init; }
    }
}

public class HealthMonitorConfiguration
{
    public int CheckIntervalSeconds { get; set; } = 60;
    public bool EnableTrendAnalysis { get; set; } = true;
    public bool EnableEventLogPublishing { get; set; } = true;
    public bool EnableStructuredLogPublishing { get; set; } = true;
}
```

## Success Criteria

- [ ] Health check endpoints respond correctly for all service components
- [ ] Database health checks validate connectivity and query performance under 2 seconds
- [ ] File system health checks verify read/write access to all critical directories
- [ ] Service status is published to Windows Event Log on status changes
- [ ] Diagnostic tools provide comprehensive service status and troubleshooting information
- [ ] Health monitoring service runs continuously and tracks service health trends
- [ ] Memory and disk space monitoring provides early warning of resource constraints
- [ ] Network connectivity checks validate HTTP endpoint accessibility
- [ ] Health check responses include detailed component status and performance metrics
- [ ] Emergency repair functionality can automatically fix common service issues

## Testing Strategy

### Unit Tests
**What to test:** Health check logic, status determination, diagnostic functions  
**Mocking strategy:** Mock database connections, file system operations, Windows services  
**Test data approach:** Use test data for health check responses and diagnostic scenarios

**Example Tests:**
```csharp
[Fact]
public async Task DatabaseHealthCheck_ShouldReturnHealthy_WhenConnectionIsGood()
{
    // Arrange
    var mockContext = CreateMockDbContext();
    var healthCheck = new DatabaseHealthCheck(mockContext, Mock.Of<ILogger<DatabaseHealthCheck>>());
    
    // Act
    var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
    
    // Assert
    result.Status.Should().Be(HealthStatus.Healthy);
    result.Data.Should().ContainKey("connectionTime");
}
```

### Integration Tests
**What to test:** Complete health check pipeline, diagnostic tool execution  
**Test environment:** Test service instance with controlled failure scenarios  
**Automation:** PowerShell scripts for automated health check validation

### E2E Tests
**What to test:** Full health monitoring workflow from service startup to status reporting  
**User workflows:** Service Start → Health Checks → Status Reporting → Diagnostic Analysis

## Dependencies

### Technical Dependencies
- ASP.NET Core Health Checks - Built-in health check infrastructure
- System.Diagnostics - Windows Event Log and performance monitoring
- System.CommandLine - Command-line diagnostic tools
- Entity Framework Core - Database health validation

### Story Dependencies
- **Blocks:** S80 (Documentation & Testing)
- **Blocked By:** S30 (Core Infrastructure)

## Next Steps

1. Implement comprehensive health check system with all component checks
2. Create diagnostic tools with command-line interface for troubleshooting
3. Develop health monitoring service with trend analysis and alerting
4. Integrate health checks with service startup and API endpoints
5. Create emergency repair functionality for common service issues
6. Test health monitoring across various failure scenarios and recovery patterns

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** [Date] (when status = Complete)