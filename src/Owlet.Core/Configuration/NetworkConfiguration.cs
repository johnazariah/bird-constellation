using System.ComponentModel.DataAnnotations;

namespace Owlet.Core.Configuration;

/// <summary>
/// Network and HTTP server configuration for the embedded web server.
/// </summary>
public record NetworkConfiguration
{
    /// <summary>
    /// HTTP port for the embedded web server (must be between 1024-65535).
    /// </summary>
    [Required]
    [Range(1024, 65535)]
    public int Port { get; init; } = 5555;

    /// <summary>
    /// IP address to bind the HTTP server to (127.0.0.1 for local-only access).
    /// </summary>
    [Required]
    [RegularExpression(@"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$")]
    public string BindAddress { get; init; } = "127.0.0.1";

    /// <summary>
    /// Whether to enable HTTPS (requires certificate configuration).
    /// </summary>
    public bool EnableHttps { get; init; }

    /// <summary>
    /// Path to X.509 certificate file for HTTPS (required if EnableHttps is true).
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Password for the certificate file (if certificate is password-protected).
    /// </summary>
    public string? CertificatePassword { get; init; }

    /// <summary>
    /// Maximum request body size in bytes (prevents memory exhaustion attacks).
    /// </summary>
    [Range(1024, 1073741824)] // 1KB to 1GB
    public long MaxRequestBodySize { get; init; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Request timeout for HTTP operations.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Whether to enable detailed HTTP error responses (disable in production).
    /// </summary>
    public bool EnableDetailedErrors { get; init; }

    /// <summary>
    /// Whether to enable response compression (gzip/brotli).
    /// </summary>
    public bool EnableCompression { get; init; } = true;
}
