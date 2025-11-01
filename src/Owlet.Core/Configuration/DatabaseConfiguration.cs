using System.ComponentModel.DataAnnotations;

namespace Owlet.Core.Configuration;

/// <summary>
/// Database configuration for Entity Framework Core with SQLite.
/// </summary>
public record DatabaseConfiguration
{
    /// <summary>
    /// Database connection string (SQLite format).
    /// </summary>
    [Required]
    [StringLength(2048, MinimumLength = 1)]
    public string ConnectionString { get; init; } = "Data Source=C:\\ProgramData\\Owlet\\owlet.db";

    /// <summary>
    /// Database provider name.
    /// </summary>
    [StringLength(50)]
    public string Provider { get; init; } = "Sqlite";

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    [Range(1, 3600)]
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// </summary>
    [Range(1, 1000)]
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>
    /// Delay between retry attempts.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Enable sensitive data logging (development only).
    /// </summary>
    public bool EnableSensitiveDataLogging { get; init; }

    /// <summary>
    /// Enable detailed error messages (development only).
    /// </summary>
    public bool EnableDetailedErrors { get; init; }
}
