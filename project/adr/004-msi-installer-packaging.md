# ADR-004: MSI Installer with Self-Contained Deployment

## Status
Accepted

## Date
2025-11-01

## Context

Owlet needs to be easily installable by non-technical users on Windows machines. The installation must handle:

- .NET runtime dependencies without requiring users to install .NET separately
- Windows Service registration and configuration
- Firewall configuration for the embedded web server
- System tray application autostart registration
- Clean uninstallation with complete removal of all components
- Bundled dependencies (SQLite, Ollama for future AI features)

We need to choose between different Windows installer technologies and deployment models.

## Decision

We will use **MSI installer packages** built with **WiX Toolset** and **self-contained .NET deployment**:

### MSI Installer with WiX Toolset
- **Professional Installation Experience**: MSI provides standard Windows installation UI and behavior
- **Windows Integration**: Proper Add/Remove Programs registration, Windows Installer logging
- **Administrative Installation**: Supports both per-user and per-machine installation modes
- **Upgrade Logic**: Built-in support for version upgrades and patches
- **Component Management**: Precise control over file installation, registry entries, and service registration

### Self-Contained Deployment
- **No Runtime Dependencies**: Bundle .NET 9 runtime with the application
- **Offline Installation**: No internet connection required during installation
- **Version Isolation**: Eliminate conflicts with other .NET applications
- **Simplified Support**: No need to troubleshoot .NET installation issues

### Bundled Dependencies
- **Embedded SQLite**: No separate database installation required
- **Ollama Runtime**: Bundle AI runtime for future semantic search features
- **Language Models**: Include pre-trained models for offline operation

### Installation Components
```xml
<!-- WiX installer structure -->
<Product Id="*" Name="Owlet Document Indexer" Version="!(bind.FileVersion.OwletService.exe)">
  <Feature Id="CoreFeature" Title="Owlet Service" Level="1">
    <ComponentRef Id="ServiceExecutable"/>
    <ComponentRef Id="WindowsServiceRegistration"/>
    <ComponentRef Id="FirewallExemption"/>
    <ComponentRef Id="SystemTrayApplication"/>
  </Feature>
  
  <Feature Id="AIFeature" Title="AI Models" Level="1">
    <ComponentRef Id="OllamaRuntime"/>
    <ComponentRef Id="EmbeddingModels"/>
  </Feature>
</Product>
```

## Consequences

### Positive
- **Professional User Experience**: Standard Windows installation familiar to all users
- **Zero Configuration**: Users can install and run without technical knowledge
- **Reliable Dependencies**: Self-contained deployment eliminates environment issues
- **Enterprise Ready**: MSI supports corporate deployment scenarios (Group Policy, SCCM)
- **Complete Uninstall**: MSI ensures clean removal of all components
- **Upgrade Path**: Built-in support for version updates and patches

### Negative
- **Large Package Size**: Self-contained deployment increases installer size (≈150MB)
- **Build Complexity**: WiX requires XML configuration and advanced MSI knowledge
- **Platform Lock-in**: MSI is Windows-specific (though application code remains cross-platform)
- **Development Overhead**: Need for installer testing and signing infrastructure
- **Storage Requirements**: Bundled dependencies increase disk space usage

## Alternatives Considered

### Framework-Dependent Deployment with .NET Installer
- **Pros**: Smaller package size, shared runtime benefits
- **Cons**: Requires users to install .NET runtime, version compatibility issues, support complexity

### NSIS Installer
- **Pros**: Smaller learning curve, good customization options
- **Cons**: Less professional appearance, no built-in upgrade logic, limited enterprise features

### Inno Setup
- **Pros**: Simple scripting, good compression
- **Cons**: Limited enterprise features, no component-based installation model

### ClickOnce Deployment
- **Pros**: Automatic updates, easy deployment
- **Cons**: Requires internet connection, limited system integration, poor service support

### Chocolatey Package
- **Pros**: Excellent for technical users, good version management
- **Cons**: Not suitable for non-technical users, requires Chocolatey installation

### Windows Store Package (MSIX)
- **Pros**: Modern deployment model, automatic updates, sandboxing
- **Cons**: Limited system integration for services, Store approval process, requires Windows 10+

## Implementation Strategy

### Phase 1: Basic MSI
- Self-contained .NET deployment
- Windows Service registration
- Basic firewall configuration
- Add/Remove Programs integration

### Phase 2: Enhanced Features
- System tray application autostart
- Custom installation UI
- Upgrade logic and patching
- Digital signing for trust

### Phase 3: Enterprise Features
- Silent installation mode
- Configuration file customization
- Administrative installation
- Group Policy deployment support

### Phase 4: AI Integration
- Ollama runtime bundling
- Pre-trained model inclusion
- Model update mechanism
- Resource optimization

## Testing Strategy

- **Virtual Machine Testing**: Test installation on clean Windows VMs
- **Upgrade Testing**: Verify smooth upgrades between versions
- **Uninstall Testing**: Ensure complete removal of all components
- **Permission Testing**: Validate both admin and non-admin installation scenarios
- **Corporate Environment Testing**: Test in domain-joined environments with group policies