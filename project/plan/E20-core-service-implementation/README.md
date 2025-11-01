# E20: Core Service Implementation

**Business Value:** Deliver functional document indexing and search capabilities as the foundation of Owlet's value proposition  
**Priority:** Critical  
**Dependencies:** E10 (Foundation & Infrastructure)  
**Estimated Effort:** 2 weeks  
**Status:** Not Started  

## Overview

This epic implements the core document indexing and search functionality that defines Owlet's primary value proposition. It transforms Owlet from a deployment infrastructure into a working document indexing service capable of monitoring file systems, extracting content, and providing fast search capabilities through an embedded web interface.

The implementation focuses on reliable file monitoring, content extraction from common document formats, full-text indexing with SQLite, and a responsive web UI for search and folder management. This creates the foundation for all advanced features while ensuring the service can handle typical desktop document collections (1,000+ files) with sub-second search response times.

## Business Context

### Current State
- Infrastructure and deployment pipeline established (E10)
- No document processing or search capabilities
- Service exists but provides no end-user value

### Desired State
- Automatic file discovery and indexing in watched folders
- Content extraction from text, markdown, PDF, and Office documents
- Fast full-text search with relevance ranking
- Intuitive web interface accessible at localhost:5555
- Real-time file monitoring with immediate index updates

### User Impact
- **Knowledge Workers:** Instant search across all personal documents regardless of location or format
- **Researchers:** Quick discovery of relevant content across large document collections
- **Content Creators:** Effortless organization and retrieval of reference materials
- **Business Impact:** Reduces time spent searching for information by 70% (typical knowledge worker saves 30 minutes daily)

## Scope

### In Scope
- **Windows Service Host:** Complete service implementation with proper lifecycle management and graceful shutdown
- **SQLite Database:** Schema design, Entity Framework Core integration, migration support, and full-text search
- **File System Monitoring:** Real-time file watching with event-driven processing and change detection
- **Content Extraction Pipeline:** Text, Markdown, PDF, and basic Office document (.docx, .xlsx, .pptx) content extraction
- **Search API:** RESTful search endpoints with query parsing, relevance scoring, and pagination
- **Web User Interface:** Responsive single-page application for search, folder management, and service status
- **File Classification:** Basic file type detection and metadata extraction (size, dates, encoding)

### Out of Scope
- Advanced error handling and resilience patterns (E30: Production Hardening)
- Semantic search and AI-powered features (E40: Advanced Features)
- Multi-user support and advanced security (E30: Production Hardening)
- Performance optimization beyond basic requirements (E30: Production Hardening)

## Domain Model

### Core Aggregates
- **IndexedFile:** File metadata, content, tags, indexing status, modification tracking
- **WatchedFolder:** Folder path, monitoring status, inclusion/exclusion rules, last scan timestamp
- **SearchQuery:** Query text, filters, pagination parameters, execution context

### Value Objects
- **FilePath:** Validated, normalized file system path with existence checking
- **FileContent:** Extracted text content with encoding detection and length limits
- **FileMetadata:** Size, creation/modification dates, file type classification
- **SearchResults:** Result collection with relevance scores, total count, and execution time

### Domain Events
- **FileIndexed:** Triggered when file successfully indexed with content
- **FileUpdated:** Triggered when existing file content changes
- **FileDeleted:** Triggered when indexed file removed from file system
- **FolderAdded:** Triggered when new folder added to watch list

## Technical Considerations

### Architecture
**Event-Driven Processing** with clean separation:
- Domain layer with pure business logic and validation
- Infrastructure layer handling file system and database operations
- Application layer orchestrating workflows and use cases
- API layer providing HTTP endpoints and web interface

### Technology Stack
- **Primary Language:** C# 13
- **Framework:** ASP.NET Core 8 with embedded Kestrel
- **Database:** SQLite with Entity Framework Core and FTS5 full-text search
- **File Monitoring:** System.IO.FileSystemWatcher with event aggregation
- **Content Extraction:** Custom extractors with fallback strategies
- **Web Framework:** Carter for functional API composition
- **Frontend:** HTML5/CSS3/JavaScript (no framework dependencies)

### Database Design
**SQLite with Full-Text Search:**
```sql
-- Core Tables
WatchedFolders (Id, Path, IsActive, AddedAt, Settings)
IndexedFiles (Id, Path, Name, Extension, Kind, ModifiedAt, IndexedAt, FileSize, IsReadable)
FileContent (FileId, ExtractedText, ContentHash, ExtractionMethod)

-- Full-Text Search
CREATE VIRTUAL TABLE FileContentFTS USING fts5(
    content='FileContent', 
    ExtractedText, 
    content_rowid=FileId
);
```

### Content Extraction Strategy
**Multi-Stage Pipeline:**
1. **File Type Detection:** MIME type and extension-based classification
2. **Content Extraction:** Format-specific extractors (text, PDF, Office)
3. **Text Processing:** Encoding detection, normalization, length limits
4. **Indexing:** SQLite FTS5 tokenization and index creation

### Performance Requirements
- **Search Response Time:** < 500ms for 95th percentile queries
- **Indexing Throughput:** 100+ files per minute for typical document sizes
- **Memory Usage:** < 200MB steady state with 10,000 indexed files
- **File System Monitoring:** < 1 second latency for file change detection
- **Database Performance:** Support for 100,000+ files with linear search performance

### Security Considerations
- **File System Access:** Restricted to explicitly watched folders only
- **Content Processing:** Memory limits to prevent resource exhaustion
- **Web Interface:** Localhost-only binding with CSRF protection
- **Input Validation:** All file paths and search queries validated
- **Error Handling:** Graceful degradation for corrupt or locked files

## Story Breakdown Preview

**Estimated Stories:** 12-15 stories

**Story Categories:**
1. **S10: Analysis & Domain Design** - File indexing requirements, search behavior analysis, domain modeling
2. **S20: Database Architecture** - SQLite schema design, Entity Framework setup, migration framework
3. **S30: Windows Service Host** - Service lifecycle, configuration loading, graceful shutdown handling
4. **S40: File System Monitoring** - FileSystemWatcher implementation, event aggregation, change detection
5. **S50: Content Extraction Pipeline** - Text extractors, PDF processing, Office document handling
6. **S60: Search Engine** - Full-text search implementation, query parsing, relevance scoring
7. **S70: Search API** - RESTful endpoints, request/response models, validation
8. **S80: Web User Interface** - Search interface, folder management, responsive design
9. **S90: File Management API** - Folder watch endpoints, file metadata API, status monitoring
10. **S100: Integration & Testing** - End-to-end testing, performance validation, edge case handling

## Success Criteria

- ✅ Service automatically discovers and indexes files in watched folders within 1 minute
- ✅ Search returns relevant results in under 500ms for typical queries
- ✅ Web interface is accessible at http://localhost:5555 and fully functional
- ✅ System handles 1,000+ files without performance degradation
- ✅ Content extraction works for text, markdown, PDF, and basic Office formats
- ✅ File changes trigger immediate re-indexing within 1 second
- ✅ Search supports basic query operators (quotes, exclusions, wildcards)
- ✅ Database grows gracefully and maintains search performance
- ✅ Service memory usage remains under 200MB during normal operation
- ✅ All file operations handle locked files gracefully without crashing

## Risks & Mitigations

### High Risk: File Locking Issues with Office Documents
**Impact:** Service crashes or hangs when trying to index open Office files  
**Mitigation:**
- Implement retry logic with exponential backoff for locked files
- Use read-only file access patterns
- Mark files as "temporarily unavailable" rather than failing completely

### High Risk: Search Performance Degradation
**Impact:** Search becomes unusably slow with large document collections  
**Mitigation:**
- Implement SQLite FTS5 with proper indexing strategy
- Add query timeout and pagination limits
- Profile search performance with realistic data sets

### Medium Risk: Content Extraction Reliability
**Impact:** Service fails to extract content from various document formats  
**Mitigation:**
- Implement fallback extraction strategies
- Graceful degradation to file metadata when content extraction fails
- Comprehensive testing with real-world document samples

### Medium Risk: Memory Usage with Large Files
**Impact:** Service consumes excessive memory when processing large documents  
**Mitigation:**
- Implement content size limits (100MB default)
- Stream processing for large files
- Memory usage monitoring and alerts

## Dependencies

### Must Complete First
- **E10: Foundation & Infrastructure** - Service host, configuration, and deployment pipeline

### Blocks These Epics
- **E30: Production Hardening** - Requires working service for performance optimization and monitoring
- **E40: Advanced Features** - Requires base indexing and search for AI enhancements

## Next Steps

1. Run `epic-to-stories` prompt to break down into detailed implementation stories
2. Assign epic owner (backend engineer with file system expertise)
3. Set up development database and test document collection
4. Create API specification and web interface mockups
5. Establish performance benchmarking framework

---

**Epic Created:** November 1, 2025 by GitHub Copilot Agent  
**Last Updated:** November 1, 2025