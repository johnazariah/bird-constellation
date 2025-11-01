# Owlet Project - Copilot Instructions

## Project Overview

**Owlet** is a production-ready, local-first document indexing and search application designed for seamless installation and operation on Windows machines. It runs as a Windows service with an embedded web UI, providing file discovery, content extraction, and semantic search capabilities while serving as the foundational knowledge layer for the Owlet Constellation ecosystem.

## Essential Reading - Source of Truth Documents

**IMPORTANT**: Always read these authoritative documents before working on the project. Do not rely solely on this instruction file for technical details.

### Primary Documentation
- **📋 Technical Specification**: `c:\code\owlet\project\spec\owlet-specification.md`
  - Complete technical architecture and implementation details
  - API definitions, configuration, database schema
  - Technology stack and deployment architecture
  - Always read this first for comprehensive project understanding

- **🏗️ Architecture Decision Records**: `c:\code\owlet\project\adr\*.md`
  - Contains all architectural decisions and their rationale
  - Read `c:\code\owlet\project\adr\README.md` for the complete index
  - Always consult relevant ADRs to understand why decisions were made

### Supporting Documentation
- **🎯 Vision Document**: `c:\code\owlet\spec\00-vision.md`
  - Project vision and high-level goals
- **📝 Backlog**: `c:\code\owlet\spec\backlog_1.md`
  - Feature backlog and priorities
- **💻 Coding Standards**: `c:\code\owlet\.github\coding-standards.md`
  - Functional programming patterns, error handling, API design
  - Testing patterns and performance considerations

## Key Architectural Principles

### Strategic Decisions (from ADRs)
1. **Pure Windows Service First**: Ship simple MSI installer with pure Windows Service before adding Aspire constellation complexity
2. **Installer UX Trumps Everything**: Prioritize simple, reliable installation over architectural elegance
3. **Dual Architecture**: Pure service for end users, Aspire for developers and constellation scenarios
4. **Functional Programming Patterns**: Modern C# with Result types, immutable records, monadic composition

### Core Technology Stack
- **.NET 9** with Aspire and self-contained deployment
- **Windows Service** using `Microsoft.Extensions.Hosting.WindowsServices`
- **ASP.NET Core** with embedded Kestrel web server
- **Carter** for functional API composition
- **Entity Framework Core** with SQLite (upgradeable to PostgreSQL)
- **Serilog** integrated with structured logging
- **WiX Toolset** for MSI packaging

## Solution Structure

```
Owlet.sln
├── src/
│   ├── Owlet.AppHost          # Aspire orchestration (dev + constellation)
│   ├── Owlet.Service          # Pure Windows service (production deployment)
│   ├── Owlet.Core             # Core business logic (shared)
│   ├── Owlet.Api              # Web API with Carter (shared)
│   ├── Owlet.Indexer          # File monitoring and processing (shared)
│   ├── Owlet.Extractors       # Content extraction pipeline (shared)
│   ├── Owlet.Infrastructure   # Data access and external concerns (shared)
│   └── Owlet.ServiceDefaults  # Aspire configuration (dev + constellation only)
├── tools/
│   ├── Owlet.TrayApp          # System tray application
│   └── Owlet.Diagnostics     # Health check and diagnostic tools
├── packaging/
│   ├── installer/             # WiX installer project
│   ├── dependencies/          # Bundled runtime dependencies
│   └── scripts/               # Installation and service scripts
```

## Development Workflow

### Getting Started
1. Clone repository
2. Run `dotnet restore` to restore packages
3. Use `dotnet run --project src/Owlet.AppHost` for development
4. Access Aspire dashboard for service monitoring

### Before Making Changes
1. **Read the specification**: `c:\code\owlet\project\spec\owlet-specification.md`
2. **Check relevant ADRs**: `c:\code\owlet\project\adr\*.md` for architectural context
3. **Follow coding standards**: `c:\code\owlet\.github\coding-standards.md`
4. **Review existing implementation** to understand patterns

### Making Changes
- **Functional composition**: Use Result types and monadic patterns
- **Immutable records**: Prefer records over mutable classes
- **Dependency injection**: Constructor injection with interfaces
- **Configuration**: Strongly-typed configuration with validation
- **Testing**: Follow established patterns in existing tests

### Implementation Guidelines
- **Extension Methods**: If referenced but not found (`ConfigureForAspire`, `AddOwletCore`, etc.), create in `Owlet.ServiceDefaults` as thin wrapper over ASP.NET Core registration
- **Database Choice**: Always prefer SQLite unless explicit environment variable selects PostgreSQL
- **Reactive Processing**: If event-stream processing too complex for single task, implement simpler BackgroundService first
- **WiX Installer**: Generate stub components instead of packaging actual model files
- **API Routes**: DO NOT change established routes: `/api/search`, `/api/folders`, `/api/files`, `/health`, `/events`, `/tags`

### Documentation Maintenance
- Update ADRs for significant architectural decisions
- Keep API documentation current with OpenAPI
- Update specification for major changes
- Update coding standards for new patterns

---

## Quick Reference

### Essential Files
- **📋 Specification**: `c:\code\owlet\project\spec\owlet-specification.md`
- **🏗️ ADRs**: `c:\code\owlet\project\adr\README.md` (index of all decisions)
- **💻 Coding Standards**: `c:\code\owlet\.github\coding-standards.md`
- **🎯 Vision**: `c:\code\owlet\spec\00-vision.md`
- **📝 Backlog**: `c:\code\owlet\spec\backlog_1.md`

### Key Implementation Principles
- **Ship pure service first**, add Aspire constellation later
- **Installer UX is paramount** - simple, reliable installation
- **Local-first architecture** - no cloud dependencies
- **Functional programming patterns** for maintainable code

### When in Doubt
- Architecture questions: Reference ADRs for rationale
- Implementation guidance: Follow specification patterns
- Code patterns: Follow coding standards document
- Testing strategy: Use established test pyramid approach
- Performance concerns: Monitor against targets in specification

---

*Last updated: November 1, 2025*
*Version: 2.0 - Refactored to follow DRY principles with authoritative source references*
