# ADR-007: Pure Windows Service for End-User Installation

## Status
Accepted

## Date
2025-11-01

## Context

During specification development, we designed a dual architecture approach with both pure Windows Service and Aspire orchestration capabilities. The question arose about how to package and deliver these two hosting models to different audiences:

1. **End Users**: Want simple document search that "just works" after installation
2. **Developers/Power Users**: Want rich development experience and constellation capabilities
3. **Constellation Users**: Want to run multiple services (Owlet + Lumen + Raven + etc.)

Initial consideration was to provide installation options or bundle both approaches in a single installer. However, this creates significant user experience and maintenance problems.

## Decision

We will **ship the pure Windows Service as the default and only MSI installer**, with Aspire constellation support provided as a separate developer-focused option.

### End-User Installation (MSI Installer)
**Target Audience**: Non-technical users who want document search

**Contents**:
- Owlet.Service (pure Windows Service, minimal dependencies)
- System tray application  
- Self-contained .NET deployment
- SQLite database
- All necessary runtime dependencies

**Installation Experience**:
1. User downloads `Owlet-Setup.msi`
2. Runs installer (single question: "Install Owlet?")
3. Service starts automatically
4. Tray opens `http://localhost:5555` (or configured port)
5. User immediately has working document search

**No Choices. No Composition Dialog. No "What is Aspire?"**

### Developer/Constellation Experience (Separate)
**Target Audience**: Developers, power users, constellation scenarios

**Access Methods**:
- **Repository**: `dotnet run --project src/Owlet.AppHost`
- **Constellation Pack**: Separate download with Docker Compose or Aspire manifest
- **Documentation**: Clear instructions for running full constellation

**Capabilities**:
- Rich Aspire development experience
- Service discovery and orchestration
- Integration with Lumen, Cygnet, Eaglet, Raven
- PostgreSQL for multi-service scenarios
- Full observability and debugging tools

## Consequences

### Positive
- **Simplified User Experience**: Installer answers exactly one question for normal users
- **Reduced Support Burden**: No need to explain, patch, or support Aspire in production installs
- **Smaller MSI Package**: Pure service installation without Aspire dependencies
- **Clear Audience Separation**: End users get reliability, developers get sophistication
- **Future Flexibility**: Can add constellation features later without changing core installation
- **Reduced Testing Matrix**: Focus testing on one production deployment model
- **Professional Appearance**: Clean, single-purpose installer builds user confidence

### Negative
- **Two Maintenance Paths**: Need to maintain both hosting approaches (though they share core libraries)
- **Documentation Split**: Need separate documentation for end-user vs. developer scenarios
- **Feature Discovery**: Power users may not discover constellation capabilities initially

## Alternatives Considered

### Single Installer with Options
- **Pros**: One download covers all scenarios
- **Cons**: Confuses end users, increases support complexity, larger package

### Aspire-Only Distribution
- **Pros**: Unified development experience, rich orchestration
- **Cons**: Not suitable for end-user installation, requires technical knowledge

### Docker-Only Distribution
- **Pros**: Consistent across platforms, easy constellation setup
- **Cons**: Requires Docker Desktop, unfamiliar to Windows users, resource overhead

### Multiple MSI Packages
- **Pros**: Choice without in-installer complexity
- **Cons**: Confusing download page, support matrix complexity

## Implementation Strategy

### Phase 1: Pure Service Foundation
- Build and perfect Owlet.Service as standalone Windows Service
- Create professional MSI installer with WiX
- Implement tray application and local web UI
- Validate installation experience on clean Windows VMs

### Phase 2: Aspire Development Environment
- Create Owlet.AppHost for development scenarios
- Document constellation development workflow
- Provide sample configurations for multi-service scenarios

### Phase 3: Constellation Distribution
- Create "Constellation Pack" with Docker Compose or Aspire manifests
- Document integration patterns for Lumen, Raven, etc.
- Provide migration path from single service to constellation

## Success Criteria

### End-User Success
- [ ] Installation completes in under 2 minutes with zero user decisions
- [ ] Service starts automatically and responds within 30 seconds
- [ ] Web UI opens automatically and is immediately functional
- [ ] Zero support tickets asking "what is Aspire?" or installation confusion

### Developer Success
- [ ] `dotnet run --project src/Owlet.AppHost` provides rich development experience
- [ ] Clear documentation for adding constellation services
- [ ] Service discovery and orchestration work seamlessly in development
- [ ] Easy transition from development to production deployment

## User Journey Examples

### End User (Primary Path)
1. Downloads `Owlet-Setup.msi` from website
2. Double-clicks to install (UAC prompt, clicks Yes)
3. Installer runs silently, completes in ~1 minute
4. Tray icon appears, opens browser to working search interface
5. Adds document folders, sees immediate indexing results
6. **Never thinks about services, ports, or technical details**

### Developer (Advanced Path)
1. Clones repository or downloads constellation pack
2. Runs `dotnet run --project src/Owlet.AppHost`
3. Aspire Dashboard opens with rich service topology
4. Adds Lumen service to AppHost configuration
5. Services discover each other automatically
6. **Full constellation development experience**

## Rationale Summary

The installer should make **one promise** to end users: "Install this and get working document search." 

Everything else - orchestration, constellation, development tools - is for people who understand what they're asking for and how to get it.

This creates a clean separation between "software that users install" and "platform that developers build on."