# ADR-001: Production-Ready Windows Service Architecture

## Status
Accepted

## Date
2025-11-01

## Context

The initial prompt suggested building Owlet as an Aspire development application, but we identified several production deployment challenges:

- End users need a simple installation experience without requiring .NET runtime knowledge
- The application must run reliably as a background service without user interaction
- System integration requirements include Windows Service registration, firewall configuration, and autostart capabilities
- Long-term vision includes potential scaling to enterprise environments
- Non-technical users need professional installation and diagnostic capabilities

The choice was between:
1. Aspire development-focused application requiring manual setup
2. Traditional console application with manual service registration
3. Professional Windows Service with enterprise-grade installation

## Decision

We will build Owlet as a production-ready Windows Service application with the following characteristics:

- **Windows Service Host**: Using `Microsoft.Extensions.Hosting.WindowsServices` for proper service lifecycle management
- **Self-Contained Deployment**: Bundle .NET runtime to eliminate user environment dependencies
- **MSI Installer Package**: Professional installation experience with WiX Toolset
- **System Integration**: Automatic service registration, firewall configuration, and startup registration
- **Enterprise Patterns**: Structured logging, health monitoring, configuration validation, and diagnostic capabilities

The service will embed an ASP.NET Core web server (Kestrel) to provide the user interface and API endpoints, accessible via `http://localhost:5555`.

## Consequences

### Positive
- **Professional User Experience**: Non-technical users can install and use Owlet without configuration
- **Reliable Operation**: Windows Service lifecycle management provides automatic startup, crash recovery, and clean shutdown
- **Enterprise Ready**: Supports corporate environments with proper logging, monitoring, and diagnostic capabilities
- **Security Model**: Runs with appropriate service account permissions and security boundaries
- **Maintenance**: Built-in update mechanisms and troubleshooting tools for support scenarios

### Negative
- **Increased Complexity**: More infrastructure code required compared to simple console application
- **Platform Lock-in**: Windows-specific deployment model (though code remains cross-platform)
- **Installation Requirements**: Requires administrator privileges for service installation
- **Development Overhead**: Need for installer creation, service testing, and deployment pipeline complexity

## Alternatives Considered

### Aspire Development Application
- **Pros**: Rapid development, excellent local debugging experience
- **Cons**: Not suitable for end-user deployment, requires technical knowledge to install and run

### Console Application with Manual Setup
- **Pros**: Simple implementation, minimal infrastructure requirements
- **Cons**: Poor user experience, no automatic startup, difficult troubleshooting

### Docker Container Deployment
- **Pros**: Consistent deployment model, easy scaling
- **Cons**: Requires Docker Desktop installation, not familiar to typical Windows users, overhead for single-service deployment

### Electron/Tauri Desktop Application
- **Pros**: Rich UI capabilities, familiar application model
- **Cons**: Resource overhead, doesn't provide the "always-on" background service model required for file monitoring

## Implementation Notes

- Use `Microsoft.Extensions.Hosting.WindowsServices` for service integration
- Implement proper service lifecycle events (OnStart, OnStop, OnShutdown)
- Embed Kestrel web server for API and UI endpoints
- Include comprehensive logging to Windows Event Log and local files
- Design for graceful degradation when web UI is not accessible