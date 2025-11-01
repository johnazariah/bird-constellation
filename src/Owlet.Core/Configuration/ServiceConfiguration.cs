using System.ComponentModel.DataAnnotations;

namespace Owlet.Core.Configuration;

/// <summary>
/// Windows service host configuration including lifecycle management and deployment settings.
/// </summary>
public record ServiceConfiguration
{
    /// <summary>
    /// Windows service name (must be unique across all installed services).
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string ServiceName { get; init; } = "OwletService";

    /// <summary>
    /// Display name shown in Windows Services management console.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string DisplayName { get; init; } = "Owlet Document Indexing Service";

    /// <summary>
    /// Service description shown in Windows Services properties.
    /// </summary>
    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string Description { get; init; } = "Indexes and searches local documents for fast retrieval";

    /// <summary>
    /// Service start mode (Automatic, Manual, or Disabled).
    /// </summary>
    [Required]
    public ServiceStartMode StartMode { get; init; } = ServiceStartMode.Automatic;

    /// <summary>
    /// Service account under which the service runs.
    /// </summary>
    [Required]
    public ServiceAccount ServiceAccount { get; init; } = ServiceAccount.LocalSystem;

    /// <summary>
    /// Maximum time allowed for service startup before Windows considers it failed.
    /// </summary>
    [Required]
    [Range(typeof(TimeSpan), "00:00:10", "00:05:00")]
    public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Whether the service supports pause and continue operations.
    /// </summary>
    public bool CanPauseAndContinue { get; init; }

    /// <summary>
    /// Whether the service can be stopped after starting.
    /// </summary>
    public bool CanStop { get; init; } = true;

    /// <summary>
    /// Whether the service responds to system shutdown notifications.
    /// </summary>
    public bool CanShutdown { get; init; } = true;

    /// <summary>
    /// Delay before first restart attempt on service failure.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:30", "00:10:00")]
    public TimeSpan FailureRestartDelay { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum number of automatic restart attempts on failure.
    /// </summary>
    [Range(0, 10)]
    public int MaxFailureRestarts { get; init; } = 3;
}

/// <summary>
/// Service start mode options.
/// </summary>
public enum ServiceStartMode
{
    /// <summary>Service starts automatically at system boot.</summary>
    Automatic,

    /// <summary>Service must be started manually.</summary>
    Manual,

    /// <summary>Service is disabled and cannot be started.</summary>
    Disabled
}

/// <summary>
/// Service account options for Windows service execution context.
/// </summary>
public enum ServiceAccount
{
    /// <summary>Highest privileges, no password required.</summary>
    LocalSystem,

    /// <summary>Network service account with limited local privileges.</summary>
    NetworkService,

    /// <summary>Local service account with minimal privileges.</summary>
    LocalService,

    /// <summary>Custom user account (requires username/password).</summary>
    User
}
