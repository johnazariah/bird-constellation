namespace Owlet.Core.Diagnostics;

/// <summary>
/// Tracks service startup performance metrics to ensure requirements are met.
/// </summary>
public record ServiceStartupMetrics
{
    /// <summary>
    /// Time spent loading and validating configuration (requirement: &lt;5 seconds).
    /// </summary>
    public TimeSpan ConfigurationLoadTime { get; init; }

    /// <summary>
    /// Time spent registering services in dependency injection container.
    /// </summary>
    public TimeSpan DependencyRegistrationTime { get; init; }

    /// <summary>
    /// Time spent starting the embedded web server (requirement: &lt;10 seconds).
    /// </summary>
    public TimeSpan WebServerStartupTime { get; init; }

    /// <summary>
    /// Time spent initializing health check system.
    /// </summary>
    public TimeSpan HealthCheckInitializationTime { get; init; }

    /// <summary>
    /// Total time from service start to fully operational (requirement: &lt;30 seconds).
    /// </summary>
    public TimeSpan TotalStartupTime { get; init; }

    /// <summary>
    /// Timestamp when service startup began.
    /// </summary>
    public DateTime StartupBeginTime { get; init; }

    /// <summary>
    /// Timestamp when service reached operational state.
    /// </summary>
    public DateTime StartupCompleteTime { get; init; }

    /// <summary>
    /// Whether all performance requirements were met.
    /// </summary>
    public bool MeetsPerformanceRequirements =>
        ConfigurationLoadTime < TimeSpan.FromSeconds(5) &&
        WebServerStartupTime < TimeSpan.FromSeconds(10) &&
        TotalStartupTime < TimeSpan.FromSeconds(30);

    /// <summary>
    /// Performance status message for logging and diagnostics.
    /// </summary>
    public string PerformanceStatus =>
        MeetsPerformanceRequirements
            ? $"✅ Startup performance OK: {TotalStartupTime.TotalSeconds:F2}s total"
            : $"⚠️ Startup performance slow: {TotalStartupTime.TotalSeconds:F2}s total (target: <30s)";
}

/// <summary>
/// Tracks service memory usage to ensure efficient resource consumption.
/// </summary>
public record ServiceMemoryMetrics
{
    /// <summary>
    /// Base service memory usage with no activity (requirement: &lt;50MB).
    /// </summary>
    public long BaseServiceMemoryBytes { get; init; }

    /// <summary>
    /// Memory used by configuration and logging infrastructure (requirement: &lt;5MB).
    /// </summary>
    public long ConfigurationMemoryBytes { get; init; }

    /// <summary>
    /// Memory used by HTTP server infrastructure (requirement: &lt;10MB).
    /// </summary>
    public long HttpServerMemoryBytes { get; init; }

    /// <summary>
    /// Current working set memory (all service memory).
    /// </summary>
    public long WorkingSetBytes { get; init; }

    /// <summary>
    /// Peak working set memory since service start.
    /// </summary>
    public long PeakWorkingSetBytes { get; init; }

    /// <summary>
    /// GC heap size (managed memory).
    /// </summary>
    public long ManagedHeapBytes { get; init; }

    /// <summary>
    /// Timestamp when metrics were captured.
    /// </summary>
    public DateTime CapturedAt { get; init; }

    /// <summary>
    /// Whether memory usage meets requirements.
    /// </summary>
    public bool MeetsMemoryRequirements =>
        BaseServiceMemoryBytes < 50 * 1024 * 1024; // 50MB

    /// <summary>
    /// Human-readable memory status.
    /// </summary>
    public string MemoryStatus =>
        MeetsMemoryRequirements
            ? $"✅ Memory usage OK: {BaseServiceMemoryBytes / (1024.0 * 1024.0):F1} MB"
            : $"⚠️ Memory usage high: {BaseServiceMemoryBytes / (1024.0 * 1024.0):F1} MB (target: <50 MB)";
}

/// <summary>
/// Helper to capture service performance metrics.
/// </summary>
public static class MetricsCapture
{
    /// <summary>
    /// Creates a new startup metrics builder for tracking service startup phases.
    /// </summary>
    public static StartupMetricsBuilder CreateStartupMetricsBuilder()
        => new();

    /// <summary>
    /// Captures current memory metrics for the service process.
    /// </summary>
    public static ServiceMemoryMetrics CaptureMemoryMetrics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        
        return new ServiceMemoryMetrics
        {
            WorkingSetBytes = process.WorkingSet64,
            PeakWorkingSetBytes = process.PeakWorkingSet64,
            ManagedHeapBytes = GC.GetTotalMemory(forceFullCollection: false),
            BaseServiceMemoryBytes = process.WorkingSet64, // Simplified for now
            ConfigurationMemoryBytes = 0, // Detailed tracking added later
            HttpServerMemoryBytes = 0,    // Detailed tracking added later
            CapturedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Builder for collecting startup metrics across multiple phases.
/// </summary>
public class StartupMetricsBuilder
{
    private DateTime _startupBegin;
    private TimeSpan _configurationLoadTime;
    private TimeSpan _dependencyRegistrationTime;
    private TimeSpan _webServerStartupTime;
    private TimeSpan _healthCheckInitializationTime;

    public StartupMetricsBuilder()
    {
        _startupBegin = DateTime.UtcNow;
    }

    public StartupMetricsBuilder WithConfigurationLoadTime(TimeSpan time)
    {
        _configurationLoadTime = time;
        return this;
    }

    public StartupMetricsBuilder WithDependencyRegistrationTime(TimeSpan time)
    {
        _dependencyRegistrationTime = time;
        return this;
    }

    public StartupMetricsBuilder WithWebServerStartupTime(TimeSpan time)
    {
        _webServerStartupTime = time;
        return this;
    }

    public StartupMetricsBuilder WithHealthCheckInitializationTime(TimeSpan time)
    {
        _healthCheckInitializationTime = time;
        return this;
    }

    public ServiceStartupMetrics Build()
    {
        var completeTime = DateTime.UtcNow;
        var totalTime = completeTime - _startupBegin;

        return new ServiceStartupMetrics
        {
            ConfigurationLoadTime = _configurationLoadTime,
            DependencyRegistrationTime = _dependencyRegistrationTime,
            WebServerStartupTime = _webServerStartupTime,
            HealthCheckInitializationTime = _healthCheckInitializationTime,
            TotalStartupTime = totalTime,
            StartupBeginTime = _startupBegin,
            StartupCompleteTime = completeTime
        };
    }
}
