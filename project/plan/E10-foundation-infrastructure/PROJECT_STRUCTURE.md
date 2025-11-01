# E10 Foundation & Infrastructure - Project Structure

## Overview

This document defines the dual architecture for Owlet: a **pure Windows service** for production deployment and **.NET Aspire orchestration** for development scenarios. This separation ensures simple, reliable MSI installation for end users while maintaining excellent developer experience.

## Architecture Decision

**Reference:** ADR-007: Ship pure Windows service first, add Aspire constellation later  
**Priority:** Installer UX trumps architectural elegance

### Production Architecture
- Single self-contained executable (`Owlet.Service.exe`)
- No Aspire dependencies
- Runs as Windows service with embedded Kestrel
- MSI installer with all dependencies bundled

### Development Architecture
- Aspire AppHost orchestrates services
- Dashboard for monitoring and debugging
- Hot reload and rapid iteration
- Same shared components as production

## Solution Structure

```
Owlet.sln
├── src/
│   ├── Owlet.Service/              # Production Windows service entry point
│   │   ├── Program.cs              # Service host configuration
│   │   ├── OwletWindowsService.cs  # Windows service implementation
│   │   ├── appsettings.json        # Default configuration
│   │   ├── appsettings.Development.json
│   │   └── Owlet.Service.csproj    # Self-contained deployment
│   │
│   ├── Owlet.AppHost/              # Development orchestration (Aspire)
│   │   ├── Program.cs              # Aspire host entry point
│   │   └── Owlet.AppHost.csproj    # Aspire orchestration project
│   │
│   ├── Owlet.Core/                 # Shared domain logic
│   │   ├── Configuration/          # Configuration records & validators
│   │   ├── Installation/           # Service & firewall definitions
│   │   ├── Services/               # Domain services (future: indexing, search)
│   │   ├── Models/                 # Domain models (future: documents, indexes)
│   │   └── Owlet.Core.csproj       # Core business logic library
│   │
│   ├── Owlet.Api/                  # Shared HTTP API (Carter endpoints)
│   │   ├── Endpoints/              # API endpoints
│   │   ├── Middleware/             # HTTP middleware
│   │   ├── Models/                 # API DTOs
│   │   └── Owlet.Api.csproj        # HTTP API library
│   │
│   ├── Owlet.Infrastructure/       # External concerns
│   │   ├── Logging/                # Serilog configuration
│   │   ├── Database/               # EF Core (future: SQLite/PostgreSQL)
│   │   ├── FileSystem/             # File monitoring (future)
│   │   └── Owlet.Infrastructure.csproj
│   │
│   └── Owlet.ServiceDefaults/      # Aspire configuration extensions
│       ├── Extensions.cs           # Service registration helpers
│       └── Owlet.ServiceDefaults.csproj
│
├── tests/
│   ├── Owlet.Core.Tests/           # Unit tests for core logic
│   ├── Owlet.Api.Tests/            # API endpoint tests
│   ├── Owlet.Infrastructure.Tests/ # Infrastructure tests
│   └── Owlet.Service.Tests/        # Service integration tests
│
├── tools/
│   ├── Owlet.TrayApp/              # System tray application (future)
│   └── Owlet.Diagnostics/          # Health check & diagnostic CLI (future)
│
├── packaging/
│   ├── installer/                  # WiX installer project (S50)
│   │   ├── Product.wxs             # Main installer definition
│   │   ├── UI.wxs                  # Custom installer UI
│   │   └── Installer.wixproj       # WiX project file
│   ├── dependencies/               # Bundled runtime dependencies
│   └── scripts/                    # Installation PowerShell scripts
│
├── .github/
│   ├── workflows/                  # GitHub Actions CI/CD (S40)
│   │   ├── ci.yml                  # Continuous integration
│   │   ├── release.yml             # Release automation
│   │   └── security.yml            # Security scanning
│   ├── copilot-instructions.md     # GitHub Copilot guidance
│   └── coding-standards.md         # Project coding standards
│
├── project/
│   ├── spec/                       # Specifications and vision
│   ├── adr/                        # Architecture Decision Records
│   └── plan/                       # Epic and story breakdown
│
├── Directory.Build.props            # Shared MSBuild properties
├── Directory.Build.targets          # Shared MSBuild targets
├── Directory.Packages.props         # Central Package Management (CPM)
├── global.json                      # .NET SDK version pinning
└── nuget.config                     # NuGet feed configuration
```

## Project Responsibilities

### Owlet.Service (Production Entry Point)
**Purpose:** Windows service host for production deployment  
**Key Files:**
- `Program.cs` - Service configuration and startup
- `OwletWindowsService.cs` - Windows service lifecycle implementation
- `appsettings.json` - Production configuration defaults

**Key Dependencies:**
- Microsoft.Extensions.Hosting.WindowsServices
- Owlet.Core
- Owlet.Api
- Owlet.Infrastructure

**Deployment:**
- Self-contained executable (includes .NET runtime)
- Single-file publish for MSI packaging
- No Aspire dependencies

### Owlet.AppHost (Development Orchestration)
**Purpose:** Aspire-based orchestration for local development  
**Key Files:**
- `Program.cs` - Aspire host configuration

**Key Dependencies:**
- Aspire.Hosting
- Owlet.Service (as orchestrated project)
- Owlet.ServiceDefaults

**Usage:**
- `dotnet run --project src/Owlet.AppHost` - Start Aspire dashboard
- Provides service discovery, logging aggregation, health checks
- Not included in MSI installer

### Owlet.Core (Domain Logic)
**Purpose:** Platform-agnostic business logic and domain models  
**Responsibilities:**
- Configuration definitions and validation
- Domain services (future: document indexing, search algorithms)
- Domain models (future: documents, indexes, metadata)
- Installation definitions (service registry, firewall rules)

**Key Characteristics:**
- No external dependencies (except validation attributes)
- Pure C# 13 with records and pattern matching
- Shared by both Owlet.Service and Owlet.AppHost

### Owlet.Api (HTTP Endpoints)
**Purpose:** Carter-based HTTP API shared by service and Aspire  
**Responsibilities:**
- REST API endpoints (`/api/search`, `/api/folders`, `/api/files`)
- Health check endpoints (`/health`, `/health/ready`, `/health/live`)
- Event stream endpoints (`/events`)
- API middleware (exception handling, request logging)

**Key Dependencies:**
- Carter (functional API composition)
- Owlet.Core

**Key Characteristics:**
- Stateless endpoints (no storage dependencies in API layer)
- OpenAPI/Swagger documentation generation
- Shared by production service and development orchestration

### Owlet.Infrastructure (External Concerns)
**Purpose:** External system integrations and cross-cutting concerns  
**Responsibilities:**
- Serilog logging configuration
- Entity Framework Core (future: SQLite/PostgreSQL)
- File system monitoring (future: document discovery)
- External service adapters (future: OCR, ML models)

**Key Dependencies:**
- Serilog.Extensions.Hosting
- Serilog.Sinks.File
- Serilog.Sinks.EventLog
- Entity Framework Core (future)

### Owlet.ServiceDefaults (Aspire Extensions)
**Purpose:** Aspire-specific configuration extensions  
**Responsibilities:**
- Service registration helpers for Aspire
- Health check configuration
- OpenTelemetry configuration
- Service discovery setup

**Key Characteristics:**
- Only used in development (Aspire scenarios)
- Not included in production MSI
- Thin wrappers over ASP.NET Core registration

## Shared vs. Specific Components

### Shared Components (Both Architectures)
✅ Owlet.Core - Business logic  
✅ Owlet.Api - HTTP endpoints  
✅ Owlet.Infrastructure - External integrations  
✅ Configuration files (appsettings.json)

### Production-Only Components
🏭 Owlet.Service - Windows service host  
🏭 MSI installer (WiX)  
🏭 Windows service registration  
🏭 Firewall rule creation

### Development-Only Components
🔧 Owlet.AppHost - Aspire orchestration  
🔧 Owlet.ServiceDefaults - Aspire extensions  
🔧 Aspire dashboard  
🔧 Service discovery and telemetry

## Configuration Strategy

### Configuration Files
- `appsettings.json` - Default settings (checked into source control)
- `appsettings.Development.json` - Development overrides (Aspire)
- `appsettings.Production.json` - Production overrides (not in repo)
- Environment variables - Runtime overrides
- Command line arguments - Deployment-specific overrides

### Configuration Loading Priority
1. Command line arguments (highest priority)
2. Environment variables
3. `appsettings.{Environment}.json`
4. `appsettings.json`
5. Default values in code (lowest priority)

### Key Configuration Sections
```json
{
  "Service": {
    "ServiceName": "OwletService",
    "DisplayName": "Owlet Document Indexing Service",
    "StartupTimeout": "00:02:00"
  },
  "Network": {
    "Port": 5555,
    "BindAddress": "127.0.0.1",
    "EnableHttps": false
  },
  "Logging": {
    "MinimumLevel": "Information",
    "LogDirectory": "C:\\ProgramData\\Owlet\\Logs",
    "EnableWindowsEventLog": true,
    "EnableStructuredLogging": true
  }
}
```

## Build Strategy

### Development Build
```bash
# Build all projects
dotnet build

# Run with Aspire
dotnet run --project src/Owlet.AppHost

# Run tests
dotnet test
```

### Production Build
```bash
# Publish self-contained service
dotnet publish src/Owlet.Service \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output publish/

# Build MSI installer (S50)
dotnet build packaging/installer/Installer.wixproj \
  --configuration Release
```

### CI/CD Build (GitHub Actions - S40)
- Build and test on every PR
- Security scanning (CodeQL, dependency scanning)
- MSI packaging on release tags
- Automated GitHub releases with MSI artifacts

## Testing Strategy

### Unit Tests
**Projects:** `Owlet.Core.Tests`, `Owlet.Api.Tests`, `Owlet.Infrastructure.Tests`  
**Focus:** Business logic, validators, domain services  
**Coverage:** >80%

### Integration Tests
**Project:** `Owlet.Service.Tests`  
**Focus:** HTTP API endpoints, configuration loading, logging  
**Coverage:** >85%

### E2E Tests (S80)
**Focus:** MSI installation, service lifecycle, health checks  
**Platform:** Windows VM (GitHub Actions)

## Deployment Architecture

### Production Deployment
1. User downloads MSI installer from GitHub Releases
2. MSI installs:
   - Owlet.Service.exe to `C:\Program Files\Owlet\`
   - Registers Windows service
   - Creates firewall rule for port 5555
   - Creates `C:\ProgramData\Owlet\` directories
3. Service starts automatically
4. Health check available at `http://localhost:5555/health`

### Development Deployment
1. Clone repository
2. Run `dotnet restore`
3. Run `dotnet run --project src/Owlet.AppHost`
4. Access Aspire dashboard for monitoring
5. Service available at `http://localhost:5555`

## Migration Path (Future)

When Owlet Constellation features are needed:
1. Owlet.Service remains unchanged (pure service)
2. Owlet.AppHost adds constellation orchestration
3. Users choose:
   - **Simple:** Install MSI (pure service)
   - **Advanced:** Run Aspire (constellation with additional services)
4. Both use same Owlet.Core, Owlet.Api, Owlet.Infrastructure

## Next Steps

1. **S20:** Create solution and project files following this structure
2. **S30:** Implement Owlet.Service with Windows service lifecycle
3. **S40:** Set up GitHub Actions CI/CD pipeline
4. **S50:** Create WiX installer for MSI packaging
5. **S60:** Add health check endpoints
6. **S70:** Implement Owlet.AppHost for Aspire development
7. **S80:** Comprehensive testing and documentation

---

**Document Created:** November 1, 2025  
**Applies To:** E10 Foundation & Infrastructure  
**Status:** Living Document (updated as implementation progresses)
