# E10 Foundation & Infrastructure - Analysis & Requirements Summary

## Document Purpose

This is the comprehensive analysis output from **S10: Analysis & Requirements**. All subsequent stories in E10 should reference this document for foundational decisions, requirements, and implementation patterns.

## Table of Contents

1. [Configuration System](#configuration-system)
2. [Logging Infrastructure](#logging-infrastructure)
3. [Windows Service Installation](#windows-service-installation)
4. [Network Security](#network-security)
5. [Project Structure](#project-structure)
6. [Build System](#build-system)
7. [Performance Requirements](#performance-requirements)
8. [Security Requirements](#security-requirements)
9. [Testing Strategy](#testing-strategy)

---

## Configuration System

### Configuration Records

**Location:** `src/Owlet.Core/Configuration/`

#### ServiceConfiguration
- Windows service lifecycle settings
- Service name, display name, description
- Start mode (Automatic, Manual, Disabled)
- Service account (LocalSystem, NetworkService, LocalService, User)
- Startup timeout (10 seconds to 5 minutes)
- Failure restart configuration

#### NetworkConfiguration
- HTTP server port (1024-65535, default: 5555)
- Bind address (default: 127.0.0.1 for localhost-only)
- HTTPS configuration (optional, with certificate path/password)
- Request body size limits and timeouts
- Compression and error detail settings

#### LoggingConfiguration
- Minimum log level (Trace, Debug, Information, Warning, Error, Critical)
- Log directory path (default: `C:\ProgramData\Owlet\Logs`)
- File size limits and rotation settings
- Retained log file count
- Multiple sinks: Windows Event Log, file system, structured JSON, console

### Configuration Validation

**Location:** `src/Owlet.Core/Configuration/ConfigurationValidator.cs`

All configuration validated at startup using `IValidateOptions<T>` pattern:
- `ServiceConfigurationValidator` - Service lifecycle settings
- `NetworkConfigurationValidator` - Network and HTTP settings
- `LoggingConfigurationValidator` - Logging infrastructure settings

**Validation ensures:**
- Required fields present
- Numeric ranges valid
- File paths exist (when required)
- HTTPS certificate available when enabled
- Startup timeouts reasonable
- Port numbers valid and non-privileged

### Configuration Loading Priority

1. Command line arguments (highest priority)
2. Environment variables
3. `appsettings.{Environment}.json`
4. `appsettings.json`
5. Default values in code (lowest priority)

---

## Logging Infrastructure

**Location:** `src/Owlet.Infrastructure/Logging/LoggerFactory.cs`

### Logging Sinks

#### File System Logs
- **Human-readable:** `owlet-YYYYMMDD.log`
- **Structured JSON:** `owlet-structured-YYYYMMDD.json`
- Rolling interval: Day, Hour, Minute (configurable)
- Size-based rolling with retention limits
- Location: `C:\ProgramData\Owlet\Logs`

#### Windows Event Log
- Source: "Owlet Service"
- Log Name: Application
- Minimum level: Warning (only critical events)
- Requires event source creation during installation

#### Console Output
- Development and debugging only
- ANSI color themes for readability
- Disabled in production by default

### Log Enrichment

All logs enriched with:
- **Application:** "Owlet"
- **Version:** Assembly version
- **MachineName:** Windows computer name
- **ProcessId:** Service process ID
- **ThreadId:** Logging thread ID
- **SourceContext:** Logging class/namespace

### Log Level Overrides

Default overrides:
- `Microsoft.*` → Warning
- `System.*` → Warning
- `Microsoft.Hosting.Lifetime` → Information

Customizable via configuration.

---

## Windows Service Installation

**Location:** `src/Owlet.Core/Installation/ServiceRegistryEntries.cs`

### Service Registry Configuration

Registry location: `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OwletService`

**Key entries:**
- **ServiceName:** "OwletService"
- **DisplayName:** "Owlet Document Indexing Service"
- **Description:** "Indexes and searches local documents for fast retrieval"
- **ImagePath:** `C:\Program Files\Owlet\Owlet.Service.exe`
- **Type:** Win32OwnProcess (standalone executable)
- **Start:** AutoStart (starts with Windows)
- **ErrorControl:** Normal (log and continue)
- **ObjectName:** LocalSystem (service account)

### Failure Recovery Actions

**Recovery policy:**
1. First failure: Restart after 1 minute
2. Second failure: Restart after 2 minutes
3. Third failure: Restart after 5 minutes
4. Reset counter: After 1 day

**Implementation:** WiX installer (S50) configures these via `ServiceControl` and `FailureActions` elements.

---

## Network Security

**Location:** `src/Owlet.Core/Installation/FirewallRule.cs`

### Firewall Rule Configuration

**Rule name:** "Owlet Document Service - HTTP"

**Properties:**
- **Direction:** Inbound
- **Action:** Allow
- **Protocol:** TCP
- **Port:** 5555 (default, configurable)
- **Local addresses:** 127.0.0.1 (localhost only)
- **Remote addresses:** LocalSubnet
- **Profiles:** Domain + Private + Public
- **Edge traversal:** Block (most secure)

**Security rationale:**
- Localhost binding prevents external network access
- LocalSubnet allows discovery on same network
- Port configurable to avoid conflicts
- Rule created/removed automatically by MSI installer

### HTTPS Configuration (Optional)

**When to enable:**
- Service bound to non-localhost addresses
- Corporate security policies require encryption
- Integration with external monitoring tools

**Certificate requirements:**
- X.509 certificate with private key
- `.pfx` format supported
- Password optional (environment variable recommended)
- Location: `C:\ProgramData\Owlet\Certificates\`

---

## Project Structure

**Location:** `project/plan/E10-foundation-infrastructure/PROJECT_STRUCTURE.md`

### Dual Architecture

**Production (MSI Installer):**
```
Owlet.Service.exe (self-contained)
├── Owlet.Core.dll
├── Owlet.Api.dll
├── Owlet.Infrastructure.dll
└── .NET 9 runtime (embedded)
```

**Development (Aspire Orchestration):**
```
Owlet.AppHost.exe
├── Owlet.Service (orchestrated project)
├── Aspire Dashboard
├── Service Discovery
└── Telemetry & Logging
```

### Project Responsibilities

| Project | Purpose | Dependencies |
|---------|---------|--------------|
| **Owlet.Service** | Production Windows service | Core, Api, Infrastructure, WindowsServices |
| **Owlet.AppHost** | Development orchestration | Aspire.Hosting, ServiceDefaults |
| **Owlet.Core** | Domain logic & models | None (pure logic) |
| **Owlet.Api** | HTTP endpoints (Carter) | Core, Carter |
| **Owlet.Infrastructure** | External integrations | Core, Serilog, EF Core |
| **Owlet.ServiceDefaults** | Aspire extensions | Aspire.Hosting |

### Directory Structure

```
Owlet.sln
├── src/
│   ├── Owlet.Service/           # Production entry point
│   ├── Owlet.AppHost/            # Development entry point
│   ├── Owlet.Core/               # Domain logic
│   ├── Owlet.Api/                # HTTP API
│   ├── Owlet.Infrastructure/     # External concerns
│   └── Owlet.ServiceDefaults/    # Aspire extensions
├── tests/
│   ├── Owlet.Core.Tests/
│   ├── Owlet.Api.Tests/
│   ├── Owlet.Infrastructure.Tests/
│   └── Owlet.Service.Tests/
├── tools/
│   ├── Owlet.TrayApp/            # System tray (future)
│   └── Owlet.Diagnostics/        # Health check CLI (future)
├── packaging/
│   ├── installer/                # WiX project (S50)
│   ├── dependencies/
│   └── scripts/
└── .github/
    └── workflows/                # CI/CD (S40)
```

---

## Build System

**Location:** `Directory.Build.props`

### Common Properties

**All projects:**
- TargetFramework: `net9.0`
- LangVersion: `13.0` (C# 13)
- Nullable: `enable` (nullable reference types)
- TreatWarningsAsErrors: `true`
- ImplicitUsings: `enable`

**Implicit usings:**
- System, System.Collections.Generic, System.Linq
- System.Threading, System.Threading.Tasks
- Microsoft.Extensions.Logging, Configuration, DependencyInjection

### Release Configuration

**Self-contained publishing (win-x64):**
- `PublishSingleFile: true` - Single executable
- `SelfContained: true` - Includes .NET runtime
- `IncludeNativeLibrariesForSelfExtract: true`
- `PublishTrimmed: false` - Disabled for safety
- `PublishReadyToRun: true` - AOT compilation

**MSI packaging:**
- Output: `Owlet.Service.exe` (~60MB with runtime)
- Location: `publish/` directory
- WiX consumes published output (S50)

---

## Performance Requirements

**Location:** `src/Owlet.Core/Diagnostics/ServiceMetrics.cs`

### Startup Performance

| Metric | Requirement | Tracking |
|--------|-------------|----------|
| Configuration Load | <5 seconds | `ConfigurationLoadTime` |
| Dependency Registration | <5 seconds | `DependencyRegistrationTime` |
| Web Server Startup | <10 seconds | `WebServerStartupTime` |
| Health Check Init | <5 seconds | `HealthCheckInitializationTime` |
| **Total Startup** | **<30 seconds** | `TotalStartupTime` |

### Memory Requirements

| Metric | Requirement | Tracking |
|--------|-------------|----------|
| Base Service | <50MB | `BaseServiceMemoryBytes` |
| Configuration | <5MB | `ConfigurationMemoryBytes` |
| HTTP Server | <10MB | `HttpServerMemoryBytes` |

### Metrics Capture

**Implementation:**
- `ServiceStartupMetrics` - Startup phase tracking
- `ServiceMemoryMetrics` - Memory consumption tracking
- `MetricsCapture` - Helper for capturing metrics
- `StartupMetricsBuilder` - Fluent builder for metrics

**Usage in S30 (Core Infrastructure):**
```csharp
var metricsBuilder = MetricsCapture.CreateStartupMetricsBuilder();
// ... startup phases ...
var metrics = metricsBuilder.Build();
logger.LogInformation("{Status}", metrics.PerformanceStatus);
```

---

## Security Requirements

**Location:** `project/plan/E10-foundation-infrastructure/SECURITY_REQUIREMENTS.md`

### Security Principles

1. **Defense in Depth:** Multiple layers of security
2. **Least Privilege:** Minimal permissions required
3. **Local-First:** No external network dependencies
4. **Transparent:** Clear security boundaries

### Service Account Security

**Primary:** LocalSystem
- Sufficient privileges for file/network operations
- No password management
- Standard for document indexing services
- Simplifies installation

**File System Access:**
- ✅ Read/write: `C:\ProgramData\Owlet\`
- ✅ Read: User-specified document directories
- ❌ No write to user documents
- ❌ No system-protected directories

**Network Access:**
- ✅ Localhost HTTP server (127.0.0.1)
- ❌ No outbound internet access
- ❌ No external network exposure

### Data Protection

**At Rest:**
- SQLite database in ProgramData (ACL-protected)
- No encryption by default (local-only access)
- Optional SQLCipher for encryption (future)

**In Transit:**
- HTTP acceptable for localhost
- HTTPS optional for non-localhost scenarios
- TLS 1.2+ only (no SSL, TLS 1.0, TLS 1.1)

### Threat Model

**Mitigated:**
- ✅ Unauthorized network access (localhost binding, firewall)
- ✅ Unauthorized file access (ACLs, ProgramData isolation)
- ✅ Service tampering (registry ACLs)
- ✅ Configuration tampering (file ACLs)

**Not Mitigated (Out of Scope):**
- ❌ Local administrator attacks (OS-level security)
- ❌ Physical access (full disk encryption)
- ❌ Supply chain attacks (dependency scanning in S40)

---

## Testing Strategy

**Location:** `tests/Owlet.Core.Tests/`

### Unit Tests

**Validators (Implemented):**
- `ServiceConfigurationValidatorTests` - 12 test cases
- `NetworkConfigurationValidatorTests` - 14 test cases
- `LoggingConfigurationValidatorTests` - 10 test cases

**Coverage target:** >80%

**Test patterns:**
- FluentAssertions for readable assertions
- Theory/InlineData for parameterized tests
- Arrange-Act-Assert structure
- Edge cases and error conditions

### Integration Tests (S30)

**Service lifecycle:**
- Configuration loading and validation
- Logging initialization
- HTTP server startup
- Health check endpoints

**Coverage target:** >85%

### E2E Tests (S80)

**MSI installation:**
- Service registration
- Firewall rule creation
- Directory creation and ACLs
- Service startup and health check

**Platform:** Windows VM (GitHub Actions)

---

## Key Decisions & Trade-offs

### Decision: LocalSystem Service Account

**Rationale:**
- Simplifies installation (no credential prompts)
- Sufficient privileges for document indexing
- Standard practice for similar services
- Can be changed to NetworkService in future if needed

**Trade-off:** Higher privileges than strictly necessary, but acceptable for local-first architecture.

### Decision: Localhost-Only Binding

**Rationale:**
- Maximum security (no external exposure)
- Suitable for single-machine deployment
- Simplifies firewall configuration
- Reduces attack surface

**Trade-off:** Cannot access service from network without explicit configuration change.

### Decision: No Database Encryption by Default

**Rationale:**
- Local-only access with file system ACLs
- Performance overhead of encryption
- Complexity of key management
- Optional SQLCipher for users who need it

**Trade-off:** Data readable by local administrators, but this is acceptable for v1.

### Decision: Dual Architecture (Service + Aspire)

**Rationale:**
- Simple MSI installer for end users (pure service)
- Excellent developer experience (Aspire dashboard)
- Shared components minimize duplication
- Future-proof for constellation scenarios

**Trade-off:** Two entry points to maintain, but both use same core components.

---

## Implementation Roadmap

### S20: Solution Architecture (Next)
- Create Visual Studio solution file
- Create all project files (.csproj)
- Configure project references
- Add NuGet package references
- Verify build succeeds

### S30: Core Infrastructure
- Implement `OwletWindowsService` (service lifecycle)
- Configure Serilog in service startup
- Implement health check endpoints
- Add metrics capture
- Verify service runs locally

### S40: Build Pipeline
- GitHub Actions workflows (CI/CD)
- Security scanning (CodeQL, Dependabot)
- MSI build automation
- Release automation

### S50: WiX Installer
- Service registration (uses ServiceRegistryEntries)
- Firewall rule creation (uses FirewallRule)
- Directory creation and ACLs
- MSI build and testing

### S60: Health Monitoring
- Database health check
- File system health check
- Diagnostic CLI tool
- Event log publishing

### S70: Development Environment
- Aspire AppHost implementation
- VS Code launch configurations
- Development scripts
- Local development guide

### S80: Documentation & Testing
- Installation guides
- Cross-platform tests
- CI/CD validation
- Performance testing

---

## Files Created in S10

### Configuration
- `src/Owlet.Core/Configuration/ServiceConfiguration.cs`
- `src/Owlet.Core/Configuration/NetworkConfiguration.cs`
- `src/Owlet.Core/Configuration/LoggingConfiguration.cs`
- `src/Owlet.Core/Configuration/ConfigurationValidator.cs`

### Infrastructure
- `src/Owlet.Infrastructure/Logging/LoggerFactory.cs`

### Installation
- `src/Owlet.Core/Installation/ServiceRegistryEntries.cs`
- `src/Owlet.Core/Installation/FirewallRule.cs`

### Diagnostics
- `src/Owlet.Core/Diagnostics/ServiceMetrics.cs`

### Build System
- `Directory.Build.props`

### Documentation
- `project/plan/E10-foundation-infrastructure/PROJECT_STRUCTURE.md`
- `project/plan/E10-foundation-infrastructure/SECURITY_REQUIREMENTS.md`
- `project/plan/E10-foundation-infrastructure/ANALYSIS_SUMMARY.md` (this file)

### Tests
- `tests/Owlet.Core.Tests/Owlet.Core.Tests.csproj`
- `tests/Owlet.Core.Tests/Configuration/ServiceConfigurationValidatorTests.cs`
- `tests/Owlet.Core.Tests/Configuration/NetworkConfigurationValidatorTests.cs`
- `tests/Owlet.Core.Tests/Configuration/LoggingConfigurationValidatorTests.cs`

**Total:** 16 files created

---

## Next Steps

1. **Proceed to S20:** Create solution and project files
2. **Reference this document:** All S20-S80 stories should reference this analysis
3. **Validate assumptions:** Test configurations on real Windows environments
4. **Update as needed:** Living document, update as requirements evolve

---

**Document Created:** November 1, 2025  
**Story:** S10 - Analysis & Requirements  
**Status:** Complete  
**Author:** GitHub Copilot Agent
