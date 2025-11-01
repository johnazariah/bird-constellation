# 🎯 Owlet Performance & Resource Planning

**Purpose**: Define performance envelopes, resource requirements, and optimization strategies for Owlet's local-first architecture.

**Last Updated**: November 1, 2025  
**Status**: Foundation - to be validated during implementation

---

## Executive Summary

Owlet is designed to run efficiently on modest hardware with **minimal idle resource usage** and **predictable peak behavior**. The "everything on your machine" philosophy means careful attention to CPU, memory, and disk usage across three operational modes:

- **Baseline (idle)**: Service running, watching folders, no active indexing
- **Busy (crawl)**: First-time indexing of large document collections  
- **Fancy (embeddings)**: Optional semantic search with ML model integration

**Key Takeaway**: For the non-ML, non-embeddings version (text/md/pdf/docx metadata + full-text), Owlet is **I/O bound rather than CPU bound**. It only gets resource-intensive when parsing lots of PDFs at once or storing embeddings.

---

## Resource Control Knobs

Owlet's resource profile depends on three primary factors:

### 1. What You Ingest

| Content Type | Resource Impact | Notes |
|-------------|-----------------|-------|
| Plain text / Markdown | Trivial | ~1-3 KB metadata, fast text extraction |
| Office (docx/xlsx) | Light | 5-20 KB per doc, moderate CPU for parsing |
| PDFs | Expensive | 50-200ms CPU per doc, 10-100 KB storage |
| Images | Mixed | Cheap to store (metadata only), expensive if vision analysis enabled |

### 2. When You Ingest

| Scenario | Resource Spike | Duration |
|----------|----------------|----------|
| Cold start (index 30GB Documents folder) | High CPU, heavy I/O | 2-10 minutes |
| Hot updates (new file saved) | Minimal | < 1 second per file |
| Background catch-up | Throttled | Spread over hours |

### 3. Whether You Store Embeddings

| Mode | Storage Overhead | CPU Impact |
|------|------------------|------------|
| Off (metadata + text only) | ~1-5 KB per doc | Minimal |
| On (semantic vectors) | +4-5 KB per doc (~768 floats) | Heavy during generation |

---

## Performance Envelopes

### Small Library (1-2k documents)

**Profile**: Personal documents, sermon notes, typical church office staff member

| Metric | Value |
|--------|-------|
| CPU (idle) | 0-1% |
| CPU (indexing) | 20-40% of 1 core |
| RAM (steady state) | 150 MB |
| Disk (no embeddings) | 50-150 MB |
| First index duration | < 1 minute |

**User Experience**: Nearly invisible - service starts quickly, indexing completes before user notices.

### Medium Library (10-20k documents)

**Profile**: Church office shared drive, pastor's complete sermon archive, organizational documents

| Metric | Value |
|--------|-------|
| CPU (idle) | 0-1% |
| CPU (indexing) | 30-70% of 1 core |
| RAM (steady state) | 200-300 MB |
| Disk (no embeddings) | 200-400 MB |
| First index duration | 2-5 minutes |

**User Experience**: Noticeable background activity during first crawl, then imperceptible. Throttling recommended.

### Large Library (50-100k documents)

**Profile**: Multi-user organization, decade+ of archived content, extensive document repository

| Metric | Value |
|--------|-------|
| CPU (idle) | 0-1% |
| CPU (indexing) | 50-90% of 1-2 cores |
| RAM (steady state) | 300-500 MB |
| Disk (no embeddings) | 0.5-1.5 GB |
| First index duration | 10-30 minutes |

**User Experience**: Requires "pause indexing" control and background scheduling. CPU spike noticeable on older hardware.

**Critical**: At this scale, **throttling and scheduling** are essential for good UX.

---

## Operational Modes

### 1. Baseline Mode (Typical Steady State)

**Scenario**: Owlet running as Windows service, watching Documents + Downloads, indexing txt/md/docx/pdf (text-only), no embeddings.

#### Resource Profile
- **Idle CPU**: ~0-1% (file watchers are cheap, wake up on change)
- **Idle RAM**: 120-200 MB (ASP.NET + DI + SQLite + minimal cache)
- **Network**: 0 (fully local, no cloud calls)

#### Per-File Costs (Text/Markdown)
- **CPU**: Trivial (< 5ms)
- **Disk (metadata row)**: 1-3 KB (path + name + kind + timestamps)
- **Disk (full text in SQLite)**: +2-10 KB (typical note)

#### Per-File Costs (PDF)
- **CPU**: 50-200ms per small PDF (text extraction via PdfPig or similar)
- **Disk**: 10-100 KB (extracted text + metadata)

#### Storage Math (5,000 mixed docs)
```
Metadata:     ~25 MB
Full text:    ~100 MB (avg 20 KB/doc)
SQLite overhead: 10-30 MB
─────────────────────────
Total:        ~150 MB
```

**Verdict**: Very reasonable for a typical personal/office library.

### 2. Busy Mode (First-Time Crawl)

**Scenario**: First install, user adds `C:\Users\You\Documents` (15-20k files).

#### What Happens
1. File watcher immediately dumps a **ton** of "created" events
2. Indexer starts processing in a loop (throttled to N workers)
3. ASP.NET continues serving UI (health checks, search UI)

#### Resource Profile
- **CPU**: Spikes to **30-90% of one core** while parsing PDFs/DOCX
  - Should throttle to 2-4 parallel workers (configurable)
  - Default: 2 workers to avoid overwhelming system
- **Disk I/O**: Mostly reads, SQLite writes are small but frequent
  - Better to **batch writes** (every N files or every few seconds)
- **Duration**: 2-10 minutes for typical medium library

#### Mitigations to Build
- **"Indexing in progress" status** in tray and health checks
- **Max concurrency setting** (default: 2, max: 8)
- **Low-priority background thread** (don't starve UI)
- **"Pause indexing" button** in tray app

**User Experience**: System feels busy for a few minutes, then drops back to idle. This is expected and acceptable with proper UI feedback.

### 3. Fancy Mode (Embeddings Enabled)

**Scenario**: User enables semantic search, requires vector embeddings for each document.

#### Vector Storage Overhead
```
Vector size:  768-1024 floats (typical embedding dimension)
Per document: 768 floats × 4 bytes = ~3 KB
With metadata: ~4-5 KB per doc

10,000 docs:  ~50 MB extra
50,000 docs:  ~250 MB extra
```

**Verdict**: Storage overhead is manageable, but **generating embeddings is expensive**.

#### CPU Impact
- **Local Ollama**: Calling model for each new file → **heavy CPU spike**
- **GPU Available**: Much faster, but not guaranteed on target hardware
- **Recommendation**: **Deferred batch processing**
  - Don't generate embeddings inline during file discovery
  - Queue files for embedding generation
  - Process in background (e.g., once per hour, when system idle)

#### User Experience
- Normal users **don't pay** for this unless they opt in
- Embeddings = **Phase 3 / optional pack** (per spec)
- Allow configuration: "Generate embeddings when on AC power only"

### 4. Image Handling

**Scenario**: User has photos, screenshots, diagrams in indexed folders.

#### Storage (Cheap)
- **Index**: Filename, folder, timestamps, EXIF metadata
- **Per image**: ~2-5 KB metadata
- **No storage**: Actual image bytes (just paths)

#### Understanding (Expensive)
- **Vision model** (e.g., Florence-2, CLIP) required for tag generation
- **Recommendation**: 
  - Index metadata immediately (cheap)
  - Defer vision analysis to **background job** (once per hour)
  - Allow user to disable: "Don't run vision when on battery"

**Verdict**: Start with metadata-only indexing, add vision as optional enhancement.

---

## Storage Breakdown

### Database Growth Model

| Documents | Metadata | Full Text | Embeddings (if enabled) | Total |
|-----------|----------|-----------|-------------------------|-------|
| 1,000 | 5 MB | 20 MB | +5 MB | 30 MB |
| 10,000 | 50 MB | 200 MB | +50 MB | 300 MB |
| 50,000 | 250 MB | 1 GB | +250 MB | 1.5 GB |
| 100,000 | 500 MB | 2 GB | +500 MB | 3 GB |

**SQLite Overhead**: ~10-30 MB for indexes and internal structures.

### Storage Optimizations

1. **Configurable Text Limit**
   - Store only first N KB of text in database (e.g., 200 KB limit)
   - Prevents bloat from huge documents
   - Search quality unaffected (first 200 KB captures most relevant content)

2. **Periodic Vacuum**
   - SQLite can bloat with many small updates
   - Run `VACUUM` command weekly (during low-usage hours)
   - Reclaims unused space from deletions

3. **Large File Threshold**
   - If file > 50 MB, **index metadata only** (no text extraction)
   - Avoids pathological cases (e.g., huge PDFs, database backups)

---

## CPU & Memory Optimization Strategies

### Indexing Throttle

**Problem**: Unrestricted parallel processing overwhelms system during crawls.

**Solution**: Configurable worker limit

```jsonc
{
  "Indexing": {
    "MaxParallelWorkers": 2,           // Default: 2 (safe for all hardware)
    "MaxParallelWorkersWhenIdle": 4    // Increase when no user activity
  }
}
```

**Heuristics**:
- **CPU cores = 4**: Default 2 workers
- **CPU cores >= 8**: Can use 4 workers
- **System idle** (no mouse/keyboard input): Increase by 2x

### Indexing Schedule

**Problem**: Indexing during active work hours annoys users.

**Solution**: Smart background processing

```jsonc
{
  "Indexing": {
    "AggressiveIndexingWhenIdle": true,  // Ramp up when no file changes
    "PauseWhenBatteryLow": true,         // Pause below 20% battery
    "QuietHours": {
      "Enabled": true,
      "Start": "09:00",                  // Business hours
      "End": "17:00",
      "BehaviorDuringQuietHours": "ThrottleOnly"  // Don't pause, just throttle
    }
  }
}
```

**Implementation Notes**:
- Detect system idle via `GetLastInputInfo()` (Win32)
- Detect battery via `System.Windows.Forms.SystemInformation.PowerStatus`
- Allow manual override: "Index now" button in tray

### Memory Management

**Problem**: .NET GC can be lazy about releasing memory.

**Solution**: Periodic pressure relief

```csharp
// After every 100 documents indexed
if (indexedCount % 100 == 0)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
}
```

**Justification**: We control the indexing loop, so we can hint to GC when it's safe to clean up. This prevents memory creep during long indexing sessions.

---

## Performance Targets

### Production Readiness Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Memory (idle)** | < 200 MB | Task Manager / Performance Monitor |
| **Memory (indexing)** | < 500 MB | Peak during large crawl |
| **CPU (idle)** | < 5% | Average over 5 minutes |
| **CPU (indexing)** | < 90% of 1 core | Default throttle setting |
| **Search latency (p95)** | < 500ms | For 10k document corpus |
| **Search latency (p99)** | < 1s | Worst case for complex queries |
| **Indexing throughput** | > 10 files/sec | For text/markdown files |
| **Indexing throughput (PDF)** | > 2 files/sec | For typical PDFs (< 5 MB) |

### Installation & Startup

| Metric | Target | Notes |
|--------|--------|-------|
| **MSI size** | < 50 MB | Self-contained .NET 9 runtime |
| **Installation time** | < 2 minutes | Including service registration |
| **Service startup** | < 30 seconds | From service start to healthy |
| **First search ready** | < 1 minute | After adding first folder |

### Storage Efficiency

| Metric | Target | Notes |
|--------|--------|-------|
| **Metadata overhead** | 1-5 KB/doc | Paths, timestamps, file info |
| **Full-text overhead** | 0.5-2x file size | Extracted text in SQLite |
| **Embedding overhead** | 4-5 KB/doc | 768-dim float vector |
| **Index overhead** | 10-30 MB | SQLite indexes and structures |

---

## Testing Strategy

### Synthetic Workloads

Create test document sets for validation:

#### Small Corpus (for CI/CD)
- **Size**: 100 files
- **Mix**: 50% text, 30% PDF, 20% office
- **Total**: ~10 MB
- **Purpose**: Fast smoke tests, CI pipeline validation

#### Medium Corpus (for local testing)
- **Size**: 5,000 files
- **Mix**: 40% text, 30% PDF, 20% office, 10% images
- **Total**: ~500 MB
- **Purpose**: Performance baseline, typical user scenario

#### Large Corpus (for stress testing)
- **Size**: 50,000 files
- **Mix**: Realistic distribution from sample org
- **Total**: ~5 GB
- **Purpose**: Validate throttling, memory limits, indexing duration

### Performance Test Scenarios

1. **Cold Start Indexing**
   - Measure: Time to index all files, peak CPU/memory
   - Assert: Completes within 2x estimated time, no OOM crashes

2. **Incremental Updates**
   - Measure: Time to detect + index new file
   - Assert: < 5 seconds from file save to searchable

3. **Search Latency**
   - Measure: p50, p95, p99 query times
   - Assert: p95 < 500ms, p99 < 1s

4. **Concurrent Operations**
   - Scenario: Search queries during active indexing
   - Assert: Search latency < 2x baseline, UI remains responsive

5. **Memory Stability**
   - Scenario: 24-hour run with periodic file additions
   - Assert: Memory usage stable (no unbounded growth), < 500 MB peak

---

## Hardware Requirements

### Minimum (Supported)

| Component | Requirement | Notes |
|-----------|-------------|-------|
| **OS** | Windows 10 (1909+) | 64-bit only |
| **CPU** | Intel Core i3 / AMD Ryzen 3 | 2+ cores, 2.0+ GHz |
| **RAM** | 4 GB | 2 GB free for Owlet |
| **Disk** | 500 MB free | For application + small corpus |
| **Storage** | HDD (5400 RPM) | Indexing slower, but functional |

**Use Case**: Single-user, small document library (< 1k files), occasional searches.

### Recommended (Optimal Experience)

| Component | Requirement | Notes |
|-----------|-------------|-------|
| **OS** | Windows 11 | Latest updates |
| **CPU** | Intel Core i5 / AMD Ryzen 5 | 4+ cores, 3.0+ GHz |
| **RAM** | 8 GB | 4 GB free for Owlet + embeddings |
| **Disk** | 2 GB free | For application + medium corpus + embeddings |
| **Storage** | SSD (SATA or NVMe) | 10x faster indexing |

**Use Case**: Multi-user shared library, frequent searches, semantic search enabled.

### Constellation (Future Multi-Service)

| Component | Requirement | Notes |
|-----------|-------------|-------|
| **RAM** | 16 GB | Owlet + Lumen + Cygnet |
| **CPU** | Intel Core i7 / AMD Ryzen 7 | 6+ cores for parallel services |
| **Disk** | 10 GB free | All services + shared data |

---

## Comparison to Alternatives

### Why This Beats Windows Recall

| Feature | Owlet | Windows Recall |
|---------|-------|----------------|
| **Data Source** | Your actual files | Screenshots of screen |
| **Privacy** | Local-only, no telemetry | Cloud-optional, telemetry risk |
| **Storage** | 150-500 MB for 10k docs | 25 GB for 3 months screenshots |
| **CPU (idle)** | < 1% | 5-15% (constant screen capture) |
| **Searchable** | Structured text, metadata | OCR of pixel data |
| **Accuracy** | Native file content | OCR errors, UI artifacts |

**Tagline**: "No screenshots, no cloud, just your files."

---

## Implementation Checklist

### Must Build in E10 (Foundation)

- [x] Health check endpoints (track memory usage, CPU %)
- [ ] Configuration: `MaxParallelWorkers`, `MaxTextSizeKB`
- [ ] Performance metrics collection (indexing rate, search latency)
- [ ] Memory pressure relief (periodic GC hints)

### Must Build in E20 (Core Service)

- [ ] Indexing throttle with configurable worker limit
- [ ] Large file detection and metadata-only fallback
- [ ] Batch write optimization (commit every N files)
- [ ] Background queue for deferred processing

### Must Build in E30 (Production Hardening)

- [ ] Indexing schedule (quiet hours, idle detection)
- [ ] Battery awareness (pause on low battery)
- [ ] Periodic SQLite vacuum
- [ ] Performance test suite with synthetic workloads

### Optional Enhancements (Phase 3+)

- [ ] Embeddings batch processing
- [ ] Vision model integration for images
- [ ] GPU acceleration detection and utilization
- [ ] Adaptive throttling based on system load

---

## Monitoring & Observability

### Key Metrics to Track

**Service Health**:
- Process memory (working set, private bytes)
- CPU usage (% of one core)
- Thread count
- Handle count (detect resource leaks)

**Indexing Performance**:
- Files indexed per second
- Average time per file type
- Queue depth (pending files)
- Errors per 1000 files

**Search Performance**:
- Query latency (p50, p95, p99)
- Queries per minute
- Cache hit rate

**Storage**:
- Database file size
- Table row counts
- Index size
- Free disk space

### Health Check Endpoints

```http
GET /health
GET /health/startup   # Called once during startup
GET /health/ready     # Called by monitoring tools
GET /health/live      # Heartbeat check
```

**Response Example**:
```json
{
  "status": "Healthy",
  "memoryMB": 187,
  "cpuPercent": 2.3,
  "indexingActive": false,
  "databaseSizeMB": 243,
  "documentCount": 12847,
  "lastIndexed": "2025-11-01T14:32:18Z"
}
```

---

## Future Optimizations (Phase 3+)

### 1. Incremental Text Extraction
**Current**: Re-extract full text on every file change  
**Future**: Detect file changes (hash-based), skip unchanged content

### 2. Distributed Indexing
**Current**: Single-threaded per file  
**Future**: Farm out heavy work (PDF parsing, embeddings) to worker pool

### 3. Tiered Storage
**Current**: All data in SQLite  
**Future**: Hot data in SQLite, cold data in parquet/blob storage

### 4. GPU Acceleration
**Current**: CPU-only for embeddings and vision  
**Future**: Detect CUDA/DirectML, offload to GPU if available

---

## Summary: Performance at a Glance

| Scenario | CPU | Memory | Storage | Notes |
|----------|-----|--------|---------|-------|
| **Idle** | < 1% | 150-200 MB | - | File watching, ready to serve |
| **Light indexing** (10 files/min) | 5-10% | 200-300 MB | +10 MB/hr | Background updates |
| **Heavy indexing** (100 files/min) | 50-90% | 300-500 MB | +100 MB/hr | First-time crawl |
| **Semantic search** (with embeddings) | Variable | +50-250 MB | +4 KB/doc | Deferred batch processing |
| **Image analysis** (with vision) | Variable | +100-300 MB | +2 KB/doc | Background job, opt-in |

**Bottom Line**: Owlet is designed to be **invisible when idle** and **respectful when busy**. The local-first architecture means no cloud dependencies, no telemetry, and predictable resource usage that scales with your document library—not with vendor infrastructure costs.

---

*This document will be updated as implementation progresses and real-world performance data is collected. Initial targets are conservative; we expect to beat these numbers in practice.*
