using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Owlet.Core.Results;
using Owlet.Infrastructure.Database;

namespace Owlet.Infrastructure.Health;

/// <summary>
/// Comprehensive database health check monitoring connectivity, query performance, and database metrics.
/// Test thresholds: connection < 5s, query < 2s (from performance-resource-planning.md).
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly OwletDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    // Performance thresholds from performance-resource-planning.md
    private const long ConnectionTimeoutMs = 5000; // 5 seconds - critical
    private const long ConnectionWarningMs = 1000; // 1 second - degraded
    private const long QueryTimeoutMs = 2000; // 2 seconds - degraded
    private const long QueryWarningMs = 500; // 500ms - warning threshold

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

            data["connectionTimeMs"] = connectionResult.Value;

            // Test query performance
            var queryResult = await TestQueryPerformance(cancellationToken);
            if (queryResult.IsFailure)
            {
                return HealthCheckResult.Degraded(
                    $"Database queries slow: {queryResult.Error}",
                    data: data);
            }

            data["queryTimeMs"] = queryResult.Value.QueryTimeMs;
            data["documentCount"] = queryResult.Value.DocumentCount;

            // Check database size and growth
            var sizeResult = await GetDatabaseMetrics(cancellationToken);
            if (sizeResult.IsSuccess)
            {
                data["databaseSizeMB"] = sizeResult.Value.SizeMB;
                data["tableCount"] = sizeResult.Value.TableCount;
                data["indexCount"] = sizeResult.Value.IndexCount;
            }

            stopwatch.Stop();
            data["totalCheckTimeMs"] = stopwatch.ElapsedMilliseconds;

            var status = GetHealthStatus(connectionResult.Value, queryResult.Value.QueryTimeMs);
            var description = status switch
            {
                HealthStatus.Healthy => $"Database is healthy (connection: {connectionResult.Value}ms, query: {queryResult.Value.QueryTimeMs}ms)",
                HealthStatus.Degraded => $"Database performance degraded (connection: {connectionResult.Value}ms, query: {queryResult.Value.QueryTimeMs}ms)",
                HealthStatus.Unhealthy => $"Database is unhealthy (connection: {connectionResult.Value}ms)",
                _ => $"Database status unknown"
            };

            _logger.LogDebug(
                "Database health check: {Status}, Connection: {ConnectionMs}ms, Query: {QueryMs}ms, Documents: {DocumentCount}",
                status, connectionResult.Value, queryResult.Value.QueryTimeMs, queryResult.Value.DocumentCount);

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

    private async Task<Result<QueryPerformanceResult>> TestQueryPerformance(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Simple count query to test performance
            var documentCount = await _dbContext.Documents
                .CountAsync(cancellationToken);

            stopwatch.Stop();

            var result = new QueryPerformanceResult
            {
                QueryTimeMs = stopwatch.ElapsedMilliseconds,
                DocumentCount = documentCount
            };

            _logger.LogDebug(
                "Database query performance test: {DocumentCount} documents in {ElapsedMs}ms",
                documentCount, stopwatch.ElapsedMilliseconds);

            return Result<QueryPerformanceResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<QueryPerformanceResult>.Failure($"Database query test failed: {ex.Message}");
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
            _logger.LogWarning(ex, "Database metrics retrieval failed (non-critical)");
            return Result<DatabaseMetrics>.Failure($"Database metrics retrieval failed: {ex.Message}");
        }
    }

    private static HealthStatus GetHealthStatus(long connectionTimeMs, long queryTimeMs)
    {
        // Connection time thresholds (critical for service operation)
        if (connectionTimeMs > ConnectionTimeoutMs) // > 5 seconds
            return HealthStatus.Unhealthy;

        if (connectionTimeMs > ConnectionWarningMs) // > 1 second
            return HealthStatus.Degraded;

        // Query time thresholds (affects user experience)
        if (queryTimeMs > QueryTimeoutMs) // > 2 seconds
            return HealthStatus.Degraded;

        // All thresholds passed
        return HealthStatus.Healthy;
    }

    private sealed record QueryPerformanceResult
    {
        public long QueryTimeMs { get; init; }
        public int DocumentCount { get; init; }
    }

    private sealed record DatabaseMetrics
    {
        public double SizeMB { get; init; }
        public int TableCount { get; init; }
        public int IndexCount { get; init; }
    }
}
