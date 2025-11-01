# ADR-008: Index Deletion Policy

**Status**: Accepted  
**Date**: 2025-11-01  
**Deciders**: John Azariah  
**Context**: E10 Foundation Infrastructure - Preparing for E20 Core Service

## Context

When a file is deleted from disk, Owlet's search index needs a deletion policy that balances:

1. **Responsiveness**: Quick removal from search results
2. **Resilience**: Handling transient filesystem issues (network drives, OneDrive sync)
3. **Safety**: Not losing data when files temporarily disappear
4. **Simplicity**: Easy to understand and debug

Windows filesystem events can be unreliable:
- Service may be stopped when file is deleted
- Network/USB drives go offline temporarily
- OneDrive/SharePoint sync causes temporary "missing" state
- File renamed outside watched folder scope
- Multiple rapid events can be missed

## Decision

Implement a **three-tier deletion policy** with hard deletes as default behavior:

### Tier 1: Event-Driven Hard Delete (Default)

When `FileSystemWatcher` detects a `Deleted` or `Renamed` event:
- **Immediately hard delete** from index via `DeleteByPathAsync(path)`
- Fast, keeps index clean, matches user expectations
- Logged for audit trail

```csharp
watcher.Deleted += async (_, e) => 
    await _index.DeleteByPathAsync(e.FullPath, ct);
```

### Tier 2: Soft Delete (Tombstone) for Missed Events

When file is discovered missing during reconciliation:
- **Mark as missing** instead of immediate deletion
- Set `IsMissing = true`, `MissingSince = DateTimeOffset.UtcNow`
- Excluded from search by default (unless `includeMissing: true`)
- Allows recovery if file reappears (USB drive, network restore, sync completion)

Database schema additions:
```sql
ALTER TABLE Files ADD COLUMN IsMissing INTEGER DEFAULT 0;
ALTER TABLE Files ADD COLUMN MissingSince TEXT NULL;
```

### Tier 3: Periodic Reconciliation (Safety Net)

Background service runs every 6 hours:
1. Check `File.Exists()` for all indexed items
2. If missing and not already marked → set `IsMissing = true`
3. If marked missing for > 7 days → **hard delete** (configurable)

Configurable via `Owlet:Index:MissingRetentionDays` (default: 7)

## Implementation

### Updated Index Interface

```csharp
public interface IOwletIndex
{
    Task UpsertAsync(OwletItem item, CancellationToken ct = default);
    Task<IReadOnlyList<OwletItem>> SearchAsync(...);
    Task<OwletItem?> GetByIdAsync(string id, CancellationToken ct = default);
    
    // New deletion methods
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task DeleteByPathAsync(string path, CancellationToken ct = default);
    
    // Reconciliation
    Task ReconcileAsync(CancellationToken ct = default);
}
```

### Indexer Event Handlers

```csharp
private async Task HandleDeletedAsync(string path, CancellationToken ct)
{
    await _index.DeleteByPathAsync(path, ct);
    _logger.LogInformation("Removed {Path} from index", path);
}

private async Task HandleRenamedAsync(RenamedEventArgs e, CancellationToken ct)
{
    // Treat as delete + add
    await _index.DeleteByPathAsync(e.OldFullPath, ct);
    await ProcessFileAsync(e.FullPath, ct);
}
```

### Background Reconciliation Service

```csharp
public class IndexReconciler : BackgroundService
{
    private readonly IOwletIndex _index;
    private readonly ILogger<IndexReconciler> _logger;
    private readonly TimeSpan _interval;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _index.ReconcileAsync(ct);
                _logger.LogInformation("Index reconciliation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index reconciliation failed");
            }
            
            await Task.Delay(_interval, ct);
        }
    }
}
```

### Configuration

```json
{
  "Owlet": {
    "Index": {
      "MissingRetentionDays": 7,
      "ReconcileIntervalHours": 6
    }
  }
}
```

## Consequences

### Positive

- **Fast and clean**: Event-driven deletes keep index current
- **Resilient**: Soft deletes handle transient filesystem issues
- **Recoverable**: Files that reappear can be unmarked (future enhancement)
- **Auditable**: All deletions logged with reason
- **Configurable**: Retention period adjustable per user needs
- **No surprises**: Hard delete matches user expectations ("I deleted it, it's gone")

### Negative

- **Extra columns**: `IsMissing` and `MissingSince` add database complexity
- **Reconciliation overhead**: Periodic `File.Exists()` checks have I/O cost
- **Race conditions**: File could be deleted between reconciliation check and mark
- **Network drives**: Offline drives look deleted, then reappear (handled by soft delete)

### Edge Cases Handled

1. **Service stopped during deletion**: Reconciliation marks as missing, purges after 7 days
2. **Network/USB offline**: Marked missing, recovers when drive reconnects
3. **OneDrive sync lag**: Temporary missing state, hard delete after retention period
4. **Renamed outside scope**: Old path deleted, new path not indexed (user reindexes folder)
5. **Sensitive files**: Immediate hard delete via event, purged from index before reconciliation

### Not Addressed (Future Enhancements)

1. **Archive mode**: Keep extracted text after file deletion (requires `RetainText` flag)
2. **Un-delete recovery**: API to clear `IsMissing` when file reappears (E30)
3. **Manual reconciliation**: Tray app "Reindex folder" triggers reconciliation (E30)
4. **Selective purging**: User chooses which missing files to keep/delete (E30)

## Alternatives Considered

### Alternative 1: Always Soft Delete

**Rejected**: Users expect deleted files to disappear from search immediately. Soft delete should be safety net, not default.

### Alternative 2: No Reconciliation

**Rejected**: Without reconciliation, files deleted while service is stopped remain in index forever.

### Alternative 3: Immediate Hard Delete Always

**Rejected**: Too aggressive for network drives and sync folders. Users would lose index entries for temporary filesystem glitches.

### Alternative 4: User-Configurable Default Policy

**Rejected for v1**: Adds configuration complexity. Can add in E30 if users request it.

## Related ADRs

- ADR-005: Phased Implementation Approach - Reconciliation deferred to E20
- ADR-003: Functional Programming Patterns - Index operations return `Result<T>`

## References

- Windows `FileSystemWatcher` reliability issues: https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher
- SQLite soft delete patterns: Common in audit-trail systems
- Search index consistency: Elasticsearch uses tombstone records similarly

## Implementation Plan

- **E20 (Core Service)**: Implement event-driven hard delete, add database columns
- **E20**: Implement `DeleteAsync()` and `DeleteByPathAsync()` methods
- **E20**: Add `IsMissing` and `MissingSince` columns to schema
- **E20**: Wire up `FileSystemWatcher.Deleted` event handler
- **E30 (Advanced Features)**: Implement `IndexReconciler` background service
- **E30**: Add configuration for retention period and reconciliation interval
- **E30**: Add tray app "Reindex folder" command
- **Future**: API to recover soft-deleted items when file reappears
