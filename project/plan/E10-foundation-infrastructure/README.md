# E10: Foundation & Infrastructure

**Business Value:** Establish production-ready development and deployment pipeline enabling reliable Windows service delivery  
**Priority:** Critical  
**Dependencies:** None (foundational epic)  
**Estimated Effort:** 2 weeks  
**Status:** Not Started  

## Overview

This epic establishes the foundational infrastructure required for Owlet's production deployment as a Windows service. It focuses on creating a robust development-to-production pipeline that ensures reliable installation, monitoring, and operation of the document indexing service on end-user machines.

The foundation includes the complete build and packaging infrastructure, from solution structure and CI/CD pipelines to MSI installer creation and health monitoring systems. This work enables all subsequent development by providing the essential scaffolding for a production-ready Windows service.

## Business Context

### Current State
- Project exists as documentation and architectural decisions only
- No executable code or deployment infrastructure
- Development workflow undefined

### Desired State
- Complete solution structure with proper project organization
- Automated CI/CD pipeline for build, test, and packaging
- Professional MSI installer for seamless Windows service deployment
- Comprehensive logging and health monitoring infrastructure
- Validated installation process across Windows versions

### User Impact
- **End Users:** Reliable, professional installation experience with minimal technical knowledge required
- **Developers:** Standardized development workflow with automated testing and deployment
- **Operations:** Comprehensive logging and health monitoring for troubleshooting

## Scope

### In Scope
- **Solution Architecture:** Complete .NET 9 solution with proper project structure (Owlet.Service, Owlet.Core, Owlet.Api, etc.)
- **Build Pipeline:** GitHub Actions workflows for continuous integration, testing, and packaging
- **WiX Installer:** Professional MSI installer with Windows service registration, firewall configuration, and clean uninstall
- **Logging Infrastructure:** Serilog integration with structured logging, Windows Event Log, and file-based logs
- **Configuration System:** Strongly-typed configuration with validation and environment-specific overrides
- **Health Monitoring:** Health check endpoints for service monitoring and diagnostics
- **Development Environment:** Local development setup with Aspire orchestration

### Out of Scope
- File indexing functionality (E20: Core Service)
- Production hardening features (E30: Production Hardening)
- Advanced AI features (E40: Advanced Features)

## Domain Model

### Configuration Aggregates
- **ServiceConfiguration**: Windows service settings, ports, startup behavior
- **LoggingConfiguration**: Log levels, output targets, structured logging rules
- **HealthConfiguration**: Health check intervals, timeout settings, failure thresholds

### Value Objects
- **ServiceName**: Validated Windows service name ("OwletService")
- **Port**: Network port with validation (default: 5555)
- **LogLevel**: Structured logging levels (Debug, Information, Warning, Error, Critical)

## Technical Considerations

### Architecture
**Clean Architecture** with clear separation of concerns:
- Core business logic independent of infrastructure
- Dependency inversion for testability
- Configuration-driven behavior

### Technology Stack
- **Primary Language:** C# 13
- **Framework:** ASP.NET Core 8  
- **Architecture:** Clean Architecture with CQRS
- **Database:** SQLite (foundation for future PostgreSQL upgrade)
- **Cache:** In-memory (foundation for future Redis)
- **Containerization:** Docker (development and future deployment)
- **Cloud Provider:** Azure (future constellation deployment)

### Development Infrastructure
- **Build System:** .NET 9 SDK with self-contained deployment
- **CI/CD:** GitHub Actions with Windows runners
- **Package Management:** NuGet with central package management
- **Code Quality:** EditorConfig, analyzers, and automated formatting

### Installation Infrastructure
- **WiX Toolset 4.x:** Modern MSI installer creation
- **Code Signing:** Authenticode signing for installer trust
- **Service Registration:** Windows Service Control Manager integration
- **Firewall Configuration:** Automatic HTTP port exemption

### Performance Requirements
- **Build Time:** < 5 minutes for full CI/CD pipeline
- **Installation Time:** < 2 minutes from download to working service
- **Service Startup:** < 30 seconds from service start to healthy
- **Memory Footprint:** < 50MB for base service (no indexing)

### Security Considerations
- **Service Account:** LocalSystem with minimal required privileges
- **Code Signing:** All executables signed with trusted certificate
- **Network Security:** HTTP interface bound to localhost only
- **Input Validation:** All configuration inputs validated
- **Audit Logging:** Service lifecycle events logged to Windows Event Log

## Story Breakdown Preview

**Estimated Stories:** 8-10 stories

**Story Categories:**
1. **S10: Analysis & Requirements** - Foundation requirements analysis, Windows service best practices research
2. **S20: Solution Architecture** - Project structure, dependency organization, build configuration
3. **S30: Core Infrastructure** - Base Windows service host, configuration system, logging infrastructure
4. **S40: Build Pipeline** - GitHub Actions workflows, automated testing, artifact creation
5. **S50: WiX Installer** - MSI installer project, service registration, firewall configuration
6. **S60: Health Monitoring** - Health check endpoints, service status monitoring, diagnostics
7. **S70: Development Environment** - Aspire integration, local development setup, debugging configuration
8. **S80: Documentation & Testing** - Installation guides, CI/CD validation, cross-platform testing

## Success Criteria

- ✅ MSI installer successfully installs and starts Windows service on Windows 10/11
- ✅ Service responds to health checks within 30 seconds of startup
- ✅ Automated CI/CD pipeline builds, tests, and packages application
- ✅ Installation process completes in under 2 minutes
- ✅ Service logs structured events to Windows Event Log and file system
- ✅ Configuration system validates all settings on startup
- ✅ Clean uninstall removes all files and registry entries
- ✅ Health monitoring provides actionable service status information
- ✅ Development environment supports F5 debugging with Aspire dashboard

## Risks & Mitigations

### High Risk: Windows Service Registration Failures
**Impact:** Service fails to install or start on end-user machines  
**Mitigation:** 
- Comprehensive testing across Windows versions (10, 11, Server 2019/2022)
- Automated VM testing in CI/CD pipeline
- Detailed error logging and rollback procedures

### Medium Risk: Code Signing Certificate Issues
**Impact:** Windows SmartScreen warnings reduce user trust and adoption  
**Mitigation:**
- Establish code signing certificate early in development
- Test signing process in CI/CD pipeline
- Document certificate renewal procedures

### Medium Risk: WiX Toolset Learning Curve
**Impact:** Delays in installer development due to WiX complexity  
**Mitigation:**
- Start with simple installer and iterate
- Reference existing WiX examples and best practices
- Consider Windows Installer XML documentation

## Dependencies

### Must Complete First
- None (foundational epic)

### Blocks These Epics
- **E20: Core Service Implementation** - Requires service host and build infrastructure
- **E30: Production Hardening** - Requires base monitoring and logging infrastructure  
- **E40: Advanced Features** - Requires complete deployment pipeline

## Next Steps

1. Run `epic-to-stories` prompt to break down into detailed stories
2. Assign epic owner (platform/infrastructure engineer)
3. Set up initial GitHub repository structure
4. Establish development environment requirements
5. Create epic tracking board and milestone

---

**Epic Created:** November 1, 2025 by GitHub Copilot Agent  
**Last Updated:** November 1, 2025