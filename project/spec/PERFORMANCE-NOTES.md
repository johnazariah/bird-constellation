# Performance & Resource Management - Implementation Notes

**Date**: November 1, 2025  
**Context**: E10 Foundation Infrastructure - Performance Planning  
**Status**: Planning Complete - Ready for Implementation

---

## Summary

This document captures the performance and resource management analysis for Owlet, establishing concrete targets and implementation strategies for the local-first document indexing service.

## Key Deliverables

### 1. Performance & Resource Planning Document
**Location**: `project/spec/performance-resource-planning.md`

Comprehensive 500+ line analysis covering:
- **Resource control knobs**: What you ingest, when, whether embeddings enabled
- **Performance envelopes**: Small/medium/large library resource profiles
- **Operational modes**: Baseline (idle), Busy (crawl), Fancy (embeddings), Image handling
- **Storage breakdown**: Database growth model and optimization strategies
- **CPU/memory optimization**: Throttling, scheduling, memory management
- **Performance targets**: Production readiness metrics
- **Testing strategy**: Synthetic workloads and test scenarios
- **Hardware requirements**: Minimum, recommended, constellation specs
- **Comparison to alternatives**: Why this beats Windows Recall
- **Implementation checklist**: Phased rollout across E10, E20, E30

### 2. Specification Updates
**Location**: `project/spec/owlet-specification.md`

Added to appsettings.json configuration:
```jsonc
"Indexing": {
  "MaxFileSizeMB": 100,
  "MaxTextSizeKB": 200,                    // NEW: Prevent bloat from huge docs
  "MaxParallelWorkers": 2,                 // NEW: Throttle during normal operation
  "MaxParallelWorkersWhenIdle": 4,         // NEW: Ramp up when system idle
  "BatchWriteSize": 100,                   // NEW: Batch DB writes for efficiency
  "AggressiveIndexingWhenIdle": true,      // NEW: Smart background processing
  "PauseWhenBatteryLow": true,             // NEW: Battery awareness
  "QuietHours": {                          // NEW: User-configurable scheduling
    "Enabled": false,
    "Start": "09:00",
    "End": "17:00",
    "BehaviorDuringQuietHours": "ThrottleOnly"
  }
}
```

### 3. Epic Updates
**Location**: `project/plan/E10-foundation-infrastructure/README.md`

Updated performance requirements section with:
- Idle memory target: < 200MB (was 50MB - more realistic)
- CPU usage target: < 5% during normal operation
- Search response time: < 500ms p95
- Reference to comprehensive performance planning document

---

## Performance Targets at a Glance

| Scenario | CPU | Memory | Storage | Notes |
|----------|-----|--------|---------|-------|
| **Idle** | < 1% | 150-200 MB | - | File watching, ready to serve |
| **Light indexing** (10 files/min) | 5-10% | 200-300 MB | +10 MB/hr | Background updates |
| **Heavy indexing** (100 files/min) | 50-90% | 300-500 MB | +100 MB/hr | First-time crawl |
| **Semantic search** (with embeddings) | Variable | +50-250 MB | +4 KB/doc | Deferred batch processing |
| **Image analysis** (with vision) | Variable | +100-300 MB | +2 KB/doc | Background job, opt-in |

---

## Implementation Phases

### E10 (Foundation) - Must Build Now
- [x] Configuration schema for performance settings (appsettings.json updated)
- [ ] Health check endpoints (track memory usage, CPU %)
- [ ] Performance metrics collection infrastructure
- [ ] Memory pressure relief (periodic GC hints)

### E20 (Core Service) - Critical Features
- [ ] Indexing throttle with configurable worker limit
- [ ] Large file detection and metadata-only fallback
- [ ] Batch write optimization (commit every N files)
- [ ] Background queue for deferred processing
- [ ] System idle detection (GetLastInputInfo)
- [ ] Battery status monitoring (SystemInformation.PowerStatus)

### E30 (Production Hardening) - Optimization
- [ ] Indexing schedule (quiet hours, idle detection)
- [ ] Periodic SQLite vacuum (weekly maintenance)
- [ ] Performance test suite with synthetic workloads
- [ ] Adaptive throttling based on system load

### Phase 3+ (Advanced Features) - Optional
- [ ] Embeddings batch processing
- [ ] Vision model integration for images
- [ ] GPU acceleration detection and utilization
- [ ] Incremental text extraction (hash-based change detection)

---

## Key Insights from Analysis

### 1. "Everything on your machine" is feasible
- **Idle**: ~150-200 MB RAM, < 1% CPU → Nearly invisible
- **Busy**: 2-10 minutes for typical library → Acceptable with UI feedback
- **Storage**: 150 MB for 5k docs → Very reasonable for personal/office use

### 2. Throttling is essential
- Default 2 parallel workers prevents overwhelming systems
- Adaptive throttling (4 workers when idle) balances UX and performance
- "Pause indexing" button provides user control

### 3. Embeddings are optional, but expensive
- 4-5 KB per doc storage overhead → Manageable
- CPU cost for generation → Heavy, requires deferred batch processing
- Should be Phase 3 feature, not foundation requirement

### 4. Smart scheduling improves UX
- Aggressive indexing when system idle (no file changes, no UI requests)
- Battery awareness prevents drain on laptops
- Quiet hours support work/business hour restrictions

### 5. This beats Windows Recall decisively
- **Data source**: Actual files vs screenshots
- **Storage**: 150 MB vs 25 GB
- **CPU (idle)**: < 1% vs 5-15%
- **Privacy**: Local-only, no telemetry vs cloud-optional
- **Accuracy**: Native content vs OCR errors

**Tagline**: "No screenshots, no cloud, just your files."

---

## Technical Debt Avoided

By planning performance upfront, we avoid:

1. **Runaway resource usage**: Clear limits on workers, file sizes, text storage
2. **Poor UX on low-end hardware**: Throttling and scheduling built-in from start
3. **Storage bloat**: Configurable text limits, periodic vacuum, large file detection
4. **Battery drain**: Power status monitoring, pause on low battery
5. **User frustration**: "Pause indexing" control, progress visibility

---

## Documentation Strategy

This analysis will inform:
- **User documentation**: "Owlet is designed to run efficiently on modest hardware"
- **Installation guide**: Hardware requirements table (min/recommended)
- **FAQ**: "How much disk space will Owlet use?"
- **Troubleshooting**: "If indexing is slow, try reducing MaxParallelWorkers"
- **Marketing**: Performance comparison to Windows Recall

---

## Next Steps

1. **Immediate**: Continue with S60 (Health Monitoring & Diagnostics)
   - Implement health check endpoints with memory/CPU tracking
   - Add performance metrics collection
   
2. **E20 Implementation**: Build throttling and resource management features
   - Configurable worker pools
   - System idle detection
   - Battery status monitoring
   - Batch write optimization

3. **E30 Validation**: Performance testing with synthetic workloads
   - Small corpus (100 files) - CI/CD smoke tests
   - Medium corpus (5k files) - local testing baseline
   - Large corpus (50k files) - stress testing

4. **Documentation**: Update user-facing docs with performance expectations
   - Installation guide hardware requirements
   - FAQ on resource usage
   - Comparison to alternatives

---

## References

- [Performance & Resource Planning](../spec/performance-resource-planning.md) - Full analysis
- [Owlet Technical Specification](../spec/owlet-specification.md) - Updated configuration
- [E10 Epic README](../plan/E10-foundation-infrastructure/README.md) - Performance requirements

---

*This planning ensures Owlet delivers on its "local-first" promise with predictable, efficient resource usage that scales with document libraries, not vendor infrastructure.*
