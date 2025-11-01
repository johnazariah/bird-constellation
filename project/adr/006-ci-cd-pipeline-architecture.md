# ADR-006: CI/CD Pipeline and Testing Strategy

## Status
Accepted

## Date
2025-11-01

## Context

Owlet's production-ready approach requires comprehensive CI/CD pipelines to ensure:
- Reliable automated builds across different environments
- Comprehensive testing including unit, integration, and end-to-end installation tests
- Professional packaging with digital signing and security scanning
- Virtual machine testing to validate the end-user installation experience
- Automated deployment and release management

The pipeline must support both development iteration and production release workflows while maintaining high quality standards.

## Decision

We will implement a **comprehensive CI/CD pipeline** using GitHub Actions with specialized workflows for different stages of the development and release process:

### Build Pipeline (Continuous Integration)
**Trigger**: Every push to main/develop branches and pull requests

**Stages**:
1. **Environment Setup**: .NET 9 SDK, WiX Toolset, signing certificates
2. **Code Quality**: Static analysis, security scanning, dependency validation
3. **Build**: Restore dependencies, compile solution, run code analysis
4. **Unit Testing**: Execute unit tests with coverage reporting
5. **Integration Testing**: Test API endpoints and service integration
6. **Package**: Create self-contained deployments for artifact storage

**Artifacts**: Build outputs, test results, coverage reports

### Packaging Pipeline (Release Preparation)
**Trigger**: Builds on main branch with successful tests

**Stages**:
1. **MSI Creation**: Build WiX installer with self-contained deployment
2. **Digital Signing**: Sign executables and MSI with code signing certificate
3. **Security Scanning**: Vulnerability assessment of packaged application
4. **Artifact Publishing**: Upload signed installer to secure storage

**Artifacts**: Signed MSI installer, security scan reports

### VM Testing Pipeline (Installation Validation)
**Trigger**: Daily schedule and on-demand for release candidates

**Test Matrix**:
- Windows 10 (versions 1909, 20H2, 21H2)
- Windows 11 (21H2, 22H2)
- Windows Server 2019, 2022

**Test Scenarios**:
1. **Fresh Installation**: Install on clean VM, verify service startup
2. **Functionality Testing**: Add folders, index files, perform searches
3. **Service Management**: Start/stop service, verify persistence
4. **Upgrade Testing**: Install previous version, upgrade to current
5. **Uninstall Testing**: Complete removal verification
6. **Error Scenarios**: Handle installation failures gracefully

### Release Pipeline (Production Deployment)
**Trigger**: Git tags matching version pattern (v*.*.*)

**Stages**:
1. **Release Validation**: Final security and quality checks
2. **GitHub Release**: Create release with changelog and artifacts
3. **Distribution**: Upload to download servers and update mechanisms
4. **Documentation**: Update installation guides and release notes
5. **Notification**: Alert stakeholders of new release availability

## Pipeline Configuration

### GitHub Actions Workflow Structure
```yaml
.github/workflows/
├── build.yml              # Continuous integration
├── package.yml             # MSI creation and signing
├── vm-tests.yml           # Installation testing
├── release.yml            # Production release
└── security-scan.yml      # Security analysis
```

### Key Pipeline Features
- **Matrix Testing**: Multiple Windows versions and configurations
- **Artifact Management**: Secure storage of build outputs and installers
- **Quality Gates**: Automated quality checks prevent bad releases
- **Security Integration**: Vulnerability scanning and dependency analysis
- **Performance Monitoring**: Track build times and test execution
- **Notification Integration**: Teams/Slack alerts for failures and releases

### Testing Strategy Integration

#### Unit Testing with Aspire
```csharp
// Aspire integration testing
[Collection("AspireTests")]
public class OwletServiceTests : IClassFixture<DistributedApplicationTestingBuilder>
{
    [Fact]
    public async Task Service_StartsSuccessfully_WithHealthChecks()
    {
        await using var app = await _builder.BuildAsync();
        await app.StartAsync();
        
        var httpClient = app.CreateHttpClient("owlet-service");
        var response = await httpClient.GetAsync("/health");
        
        response.Should().BeSuccessful();
    }
}
```

#### VM Testing Automation
```powershell
# Automated VM testing script
$testResults = @{
    InstallSuccess = Test-OwletInstallation
    ServiceHealth = Test-ServiceStatus
    SearchFunctionality = Test-SearchAPI
    UninstallCleanup = Test-UninstallProcess
}

Export-TestResults -Results $testResults -Format JUnit
```

## Consequences

### Positive
- **Quality Assurance**: Comprehensive testing prevents regressions and installation issues
- **Automation**: Reduces manual effort and human error in release process
- **Confidence**: VM testing provides high confidence in end-user experience
- **Security**: Integrated security scanning identifies vulnerabilities early
- **Traceability**: Complete audit trail from code changes to production releases
- **Efficiency**: Parallel testing reduces time from development to release

### Negative
- **Infrastructure Cost**: VM testing requires significant compute resources
- **Complexity**: Multiple pipelines require maintenance and monitoring
- **Initial Setup**: High upfront investment in pipeline configuration
- **Build Times**: Comprehensive testing increases feedback loop duration
- **Maintenance**: Pipeline updates required for new Windows versions or dependencies

## Alternatives Considered

### Simplified CI/CD with Basic Testing
- **Pros**: Lower infrastructure cost, faster setup
- **Cons**: Higher risk of installation failures, poor user experience

### Manual Testing Process
- **Pros**: Human judgment in testing scenarios
- **Cons**: Inconsistent testing, slow release cycles, human error prone

### Container-Only Testing
- **Pros**: Faster test execution, consistent environments
- **Cons**: Doesn't test actual Windows Service installation experience

### Third-Party CI/CD Platforms
- **Pros**: Specialized features, managed infrastructure
- **Cons**: Higher cost, vendor lock-in, less control over testing environment

## Implementation Phases

### Phase 1: Core CI/CD (Week 1)
- Basic build and test pipeline
- Unit test execution and reporting
- Artifact storage and management

### Phase 2: Packaging Integration (Week 2)
- MSI creation automation
- Code signing integration
- Security scanning implementation

### Phase 3: VM Testing (Week 3-4)
- VM provisioning and testing automation
- Installation scenario testing
- Performance and reliability testing

### Phase 4: Release Automation (Week 5-6)
- Automated release creation
- Distribution and notification
- Monitoring and alerting integration

## Quality Metrics

- **Build Success Rate**: > 95% successful builds on main branch
- **Test Coverage**: > 80% code coverage for core business logic
- **Installation Success**: > 95% successful installations across test matrix
- **Security Scan**: Zero critical vulnerabilities in releases
- **Performance**: Build and test cycle under 30 minutes
- **Reliability**: < 1% false positive test failures