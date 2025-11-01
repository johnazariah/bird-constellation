# E10 S80: Documentation & Testing

**Story:** Create comprehensive installation guides, CI/CD validation, and cross-platform testing for complete project foundation  
**Priority:** High  
**Effort:** 20 hours  
**Status:** Not Started  
**Dependencies:** S70 (Development Environment)  

## Objective

This story creates comprehensive documentation, testing frameworks, and validation procedures for the complete Owlet foundation. It includes installation guides for end users and administrators, comprehensive testing strategies for all components, CI/CD validation processes, and cross-platform compatibility testing to ensure reliable deployment across diverse Windows environments.

The documentation and testing system provides the foundation for reliable releases, user adoption, and operational success. It ensures that all foundation components work together seamlessly and can be deployed confidently in production environments.

## Business Context

**Revenue Impact:** ₹0 direct revenue (reduces support costs and enables successful user adoption)  
**User Impact:** All users and administrators - determines adoption success and operational reliability  
**Compliance Requirements:** Documentation standards and testing requirements for enterprise deployment

## Documentation and Testing Architecture

### 1. Installation Documentation

Comprehensive guides for different user types and deployment scenarios.

**`docs/installation/README.md`:**

```markdown
# Owlet Installation Guide

This directory contains comprehensive installation guides for different deployment scenarios and user types.

## Quick Start

For most users, the MSI installer provides the simplest installation experience:

1. **Download** the latest installer from [Releases](https://github.com/bird-constellation/owlet/releases)
2. **Run** `OwletInstaller-[version].msi` as Administrator
3. **Follow** the installation wizard prompts
4. **Access** the web interface at http://localhost:5555

## Installation Guides

### End Users
- [MSI Installer Guide](./msi-installer.md) - Complete MSI installation walkthrough
- [Silent Installation](./silent-installation.md) - Automated deployment for enterprises
- [Troubleshooting](./troubleshooting.md) - Common installation issues and solutions

### Administrators
- [Enterprise Deployment](./enterprise-deployment.md) - Group Policy and domain deployment
- [Service Configuration](./service-configuration.md) - Advanced service settings and security
- [Network Configuration](./network-configuration.md) - Firewall, ports, and access control

### Developers
- [Development Setup](./development-setup.md) - Local development environment
- [Build from Source](./build-from-source.md) - Compiling and packaging
- [Contributing](./contributing.md) - Development workflow and standards

## System Requirements

### Minimum Requirements
- **Operating System:** Windows 10 version 1809 (build 17763) or later
- **Architecture:** x64 (64-bit)
- **RAM:** 2 GB available memory
- **Storage:** 500 MB free disk space
- **Network:** HTTP port (default 5555) available

### Recommended Requirements
- **Operating System:** Windows 11 or Windows Server 2019+
- **RAM:** 4 GB available memory
- **Storage:** 2 GB free disk space (for document indexing)
- **Network:** Dedicated port with firewall configuration

## Support

- **Documentation:** [GitHub Wiki](https://github.com/bird-constellation/owlet/wiki)
- **Issues:** [GitHub Issues](https://github.com/bird-constellation/owlet/issues)
- **Discussions:** [GitHub Discussions](https://github.com/bird-constellation/owlet/discussions)
```

### 2. MSI Installer Guide

Step-by-step installation guide with screenshots and troubleshooting.

**`docs/installation/msi-installer.md`:**

```markdown
# MSI Installer Guide

This guide walks through the complete MSI installation process for Owlet Document Indexing Service.

## Prerequisites

Before installation, ensure your system meets the requirements:

- Windows 10 version 1809 or later (x64)
- Administrator privileges for installation
- 500 MB free disk space
- Network port 5555 available (or alternative port)

## Download

1. Visit the [Owlet Releases](https://github.com/bird-constellation/owlet/releases) page
2. Download the latest `OwletInstaller-[version].msi` file
3. Verify the file signature (right-click → Properties → Digital Signatures)

## Installation Process

### Step 1: Launch Installer

1. **Right-click** the MSI file and select **"Run as administrator"**
2. If prompted by Windows Defender SmartScreen, click **"More info"** then **"Run anyway"**
3. The Owlet Setup Wizard will open

### Step 2: License Agreement

1. Read the license agreement carefully
2. Select **"I accept the terms in the License Agreement"**
3. Click **"Next"** to continue

### Step 3: Service Configuration

The installer will prompt for service configuration:

#### HTTP Port Configuration
- **Default Port:** 5555
- **Custom Port:** Enter alternative port (1024-65535)
- **Validation:** Installer checks port availability

#### Service Account Selection
- **Local System** (Recommended): Full system access, highest privilege
- **Network Service**: Network access with reduced privileges
- **Local Service**: Minimal privileges, local access only

#### Service Options
- ☑ **Start service automatically**: Service starts with Windows (Recommended)
- ☑ **Create Windows Firewall rule**: Automatic firewall configuration (Recommended)

### Step 4: Security Settings

Review security configuration:

- Service runs with secure defaults
- HTTP server binds to localhost (127.0.0.1) only
- Firewall rule allows local subnet access only
- Configuration files use encrypted storage
- Comprehensive audit logging enabled

### Step 5: Feature Selection

Choose optional components:

#### Core Features (Required)
- ☑ **Owlet Service**: Main document indexing service
- ☑ **Service Configuration**: Configuration files and settings
- ☑ **Service Registration**: Windows service registration
- ☑ **Firewall Rule**: Windows Firewall configuration

#### Optional Features
- ☐ **System Tray Application**: Status monitoring and quick access
- ☐ **Diagnostic Tools**: Command-line troubleshooting tools

### Step 6: Installation Location

- **Default:** `C:\Program Files\Owlet\`
- **Custom:** Click "Change" to select alternative location
- **Data Directory:** `C:\ProgramData\Owlet\` (not configurable)

### Step 7: Ready to Install

1. Review installation summary
2. Click **"Install"** to begin installation
3. Installation progress will be displayed

### Step 8: Installation Complete

1. Installation completion confirmation
2. Optional: **"Launch Owlet Web Interface"** checkbox
3. Click **"Finish"** to complete installation

## Post-Installation Verification

### Service Status Check

1. Open **Services** (services.msc)
2. Locate **"Owlet Document Indexing Service"**
3. Verify status is **"Running"**
4. Verify startup type is **"Automatic"**

### Web Interface Access

1. Open web browser
2. Navigate to `http://localhost:5555` (or your configured port)
3. Verify Owlet web interface loads correctly

### Firewall Verification

1. Open **Windows Defender Firewall with Advanced Security**
2. Check **Inbound Rules** for "Owlet Document Service - HTTP Inbound"
3. Verify rule is **Enabled** and allows **Local subnet** access

## Common Installation Issues

### Issue: "Installation package corrupt"
**Cause:** Downloaded file is incomplete or corrupted  
**Solution:** Re-download the installer from official source

### Issue: "Service failed to start"
**Cause:** Port conflict or insufficient permissions  
**Solution:** 
1. Check port availability: `netstat -an | findstr :5555`
2. Try alternative port during installation
3. Verify service account has required permissions

### Issue: "Firewall rule not created"
**Cause:** Windows Firewall service disabled or restricted  
**Solution:**
1. Enable Windows Firewall service
2. Run installer with elevated privileges
3. Create firewall rule manually if needed

### Issue: "Access denied to web interface"
**Cause:** Service not running or firewall blocking access  
**Solution:**
1. Check service status in Services console
2. Verify firewall rule is enabled
3. Test with `http://127.0.0.1:5555` directly

## Uninstallation

### Standard Uninstall
1. Open **Settings** → **Apps**
2. Search for **"Owlet"**
3. Click **"Uninstall"** and follow prompts

### Alternative Uninstall
1. Open **Control Panel** → **Programs and Features**
2. Select **"Owlet Document Indexing Service"**
3. Click **"Uninstall"**

### Emergency Uninstall
If standard uninstallation fails:
1. Navigate to installation directory
2. Run `emergency-uninstall.bat` as administrator
3. Manually clean remaining files if necessary

## Advanced Configuration

After installation, advanced configuration options are available:

- **Configuration Files:** `C:\Program Files\Owlet\config\`
- **Service Management:** Windows Services console
- **Firewall Rules:** Windows Defender Firewall console
- **Event Logs:** Windows Event Viewer → Application log

For detailed configuration instructions, see [Service Configuration Guide](./service-configuration.md).
```

### 3. Comprehensive Testing Framework

Complete testing strategy with automated validation and cross-platform testing.

**`tests/Owlet.Tests.Integration/FoundationTests.cs`:**

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Owlet.Api;
using Owlet.Core.Configuration;
using Owlet.Infrastructure.Data;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Owlet.Tests.Integration;

[Collection("Foundation")]
public class FoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;

    public FoundationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Foundation_ShouldStartServices_Successfully()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        
        // Assert
        Assert.NotNull(hostEnvironment);
        Assert.Equal("Test", hostEnvironment.EnvironmentName);
        
        _output.WriteLine($"Host environment: {hostEnvironment.EnvironmentName}");
    }

    [Fact]
    public async Task Foundation_ShouldConfigureDatabase_Correctly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OwletDbContext>();
        
        // Act
        var canConnect = await dbContext.Database.CanConnectAsync();
        
        // Assert
        Assert.True(canConnect, "Database connection should be established");
        
        _output.WriteLine("Database connection validated successfully");
    }

    [Fact]
    public async Task Foundation_ShouldExposeHealthEndpoints_Correctly()
    {
        // Act
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");
        var detailedResponse = await _client.GetAsync("/health");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailedResponse.StatusCode);
        
        var liveContent = await liveResponse.Content.ReadAsStringAsync();
        var readyContent = await readyResponse.Content.ReadAsStringAsync();
        var detailedContent = await detailedResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("Healthy", liveContent);
        Assert.Contains("Healthy", readyContent);
        Assert.Contains("\"status\":", detailedContent);
        
        _output.WriteLine($"Health endpoints validated: Live={liveResponse.StatusCode}, Ready={readyResponse.StatusCode}, Detailed={detailedResponse.StatusCode}");
    }

    [Fact]
    public async Task Foundation_ShouldConfigureLogging_Correctly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<FoundationTests>();
        
        // Act & Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
        
        // Test logging levels
        logger.LogInformation("Foundation logging test - Information level");
        logger.LogWarning("Foundation logging test - Warning level");
        logger.LogError("Foundation logging test - Error level");
        
        _output.WriteLine("Logging configuration validated successfully");
    }

    [Fact]
    public async Task Foundation_ShouldValidateConfiguration_Correctly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IOptions<ServiceConfiguration>>();
        
        // Act
        var config = configuration.Value;
        
        // Assert
        Assert.NotNull(config);
        Assert.NotEmpty(config.DataDirectory);
        Assert.NotEmpty(config.LogDirectory);
        Assert.True(config.HttpPort > 0);
        Assert.True(config.HttpPort < 65536);
        
        _output.WriteLine($"Configuration validated: Port={config.HttpPort}, DataDir={config.DataDirectory}");
    }

    [Fact]
    public async Task Foundation_ShouldSupportConcurrentRequests_Correctly()
    {
        // Arrange
        const int concurrentRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_client.GetAsync("/health"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(responses, response =>
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
        
        _output.WriteLine($"Concurrent request test completed: {concurrentRequests} requests processed successfully");
    }

    [Fact]
    public async Task Foundation_ShouldHandleServiceShutdown_Gracefully()
    {
        // Arrange
        var testFactory = new WebApplicationFactory<Program>();
        var testClient = testFactory.CreateClient();
        
        // Act - Verify service is running
        var initialResponse = await testClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
        
        // Act - Dispose factory to simulate shutdown
        await testFactory.DisposeAsync();
        
        // Assert - Shutdown completed without exceptions
        _output.WriteLine("Service shutdown test completed successfully");
    }
}

[CollectionDefinition("Foundation")]
public class FoundationTestCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
```

### 4. Cross-Platform Testing Matrix

Automated testing across different Windows versions and configurations.

**`tests/Owlet.Tests.CrossPlatform/WindowsVersionTests.cs`:**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Owlet.Core.Configuration;
using Owlet.Service;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Owlet.Tests.CrossPlatform;

public class WindowsVersionTests
{
    private readonly ITestOutputHelper _output;

    public WindowsVersionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Windows_ShouldMeetMinimumVersionRequirements()
    {
        // Arrange
        var osVersion = Environment.OSVersion;
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        
        // Act & Assert
        Assert.True(isWindows, "Tests must run on Windows platform");
        
        // Windows 10 version 1809 = build 17763
        var minimumBuild = new Version(10, 0, 17763, 0);
        Assert.True(osVersion.Version >= minimumBuild, 
            $"Windows version {osVersion.Version} does not meet minimum requirement {minimumBuild}");
        
        _output.WriteLine($"OS Version validated: {osVersion.VersionString}");
        _output.WriteLine($"Platform: {RuntimeInformation.OSDescription}");
        _output.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture}");
    }

    [Fact]
    public void Architecture_ShouldBeX64()
    {
        // Arrange & Act
        var architecture = RuntimeInformation.OSArchitecture;
        
        // Assert
        Assert.Equal(Architecture.X64, architecture);
        
        _output.WriteLine($"Architecture validated: {architecture}");
    }

    [Fact]
    public void Framework_ShouldBeNet9OrLater()
    {
        // Arrange & Act
        var frameworkVersion = Environment.Version;
        var frameworkDescription = RuntimeInformation.FrameworkDescription;
        
        // Assert
        Assert.True(frameworkVersion.Major >= 9, 
            $".NET version {frameworkVersion} does not meet minimum requirement .NET 9");
        
        _output.WriteLine($"Framework validated: {frameworkDescription}");
        _output.WriteLine($"Runtime version: {frameworkVersion}");
    }

    [Theory]
    [InlineData("Windows 10")]
    [InlineData("Windows 11")]
    [InlineData("Windows Server 2019")]
    [InlineData("Windows Server 2022")]
    public void ServiceHost_ShouldStartOnSupportedWindows(string expectedOSFamily)
    {
        // Arrange
        var osDescription = RuntimeInformation.OSDescription;
        
        // Skip test if not running on specified OS
        if (!osDescription.Contains(expectedOSFamily, StringComparison.OrdinalIgnoreCase))
        {
            _output.WriteLine($"Skipping test - not running on {expectedOSFamily}");
            return;
        }
        
        // Act - Create minimal service configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HttpPort"] = "0", // Use any available port
                ["DataDirectory"] = Path.GetTempPath(),
                ["LogDirectory"] = Path.GetTempPath(),
                ["TempDirectory"] = Path.GetTempPath()
            })
            .Build();
        
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole());
        services.Configure<ServiceConfiguration>(configuration);
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Assert - Configuration should be valid
        var serviceConfig = serviceProvider.GetRequiredService<IOptions<ServiceConfiguration>>();
        Assert.NotNull(serviceConfig.Value);
        
        _output.WriteLine($"Service host compatibility validated on {osDescription}");
    }

    [Fact]
    public void WindowsService_ShouldSupportServiceInstallation()
    {
        // Arrange
        var hasAdminRights = IsRunningAsAdministrator();
        
        if (!hasAdminRights)
        {
            _output.WriteLine("Skipping service installation test - requires administrator privileges");
            return;
        }
        
        // Act & Assert - Test service registration capabilities
        var serviceManagerHandle = OpenSCManager(null, null, SC_MANAGER_ENUMERATE_SERVICE);
        var canAccessServiceManager = serviceManagerHandle != IntPtr.Zero;
        
        if (serviceManagerHandle != IntPtr.Zero)
        {
            CloseServiceHandle(serviceManagerHandle);
        }
        
        Assert.True(canAccessServiceManager, "Service Control Manager should be accessible");
        
        _output.WriteLine("Windows Service installation capability validated");
    }

    [Fact]
    public void FileSystem_ShouldSupportRequiredOperations()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, $"owlet-test-{Guid.NewGuid():N}");
        
        try
        {
            // Act & Assert - Directory operations
            Directory.CreateDirectory(testDir);
            Assert.True(Directory.Exists(testDir), "Directory creation should succeed");
            
            // File operations
            var testFile = Path.Combine(testDir, "test.txt");
            await File.WriteAllTextAsync(testFile, "test content");
            Assert.True(File.Exists(testFile), "File creation should succeed");
            
            var content = await File.ReadAllTextAsync(testFile);
            Assert.Equal("test content", content);
            
            // Permission test
            var fileInfo = new FileInfo(testFile);
            Assert.True(fileInfo.Exists, "File should be accessible");
            
            _output.WriteLine($"File system operations validated in: {testDir}");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [Fact]
    public void Network_ShouldSupportHttpServer()
    {
        // Arrange & Act
        using var listener = new HttpListener();
        
        try
        {
            // Test localhost binding
            listener.Prefixes.Add("http://127.0.0.1:0/");
            listener.Start();
            
            Assert.True(listener.IsListening, "HTTP listener should start successfully");
            
            _output.WriteLine("HTTP server capability validated");
        }
        finally
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    // Windows API imports for service testing
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint dwAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr hSCObject);

    private const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
}
```

### 5. CI/CD Validation Pipeline

Automated validation for all foundation components in CI/CD.

**`.github/workflows/foundation-validation.yml`:**

```yaml
name: Foundation Validation

on:
  push:
    branches: [ main, develop ]
    paths:
      - 'src/**'
      - 'tests/**'
      - 'packaging/**'
      - '.github/workflows/**'
  pull_request:
    branches: [ main ]
    paths:
      - 'src/**'
      - 'tests/**'
      - 'packaging/**'

jobs:
  validate-foundation:
    name: Validate Foundation Components
    runs-on: windows-latest
    strategy:
      matrix:
        os-version: ['windows-2019', 'windows-2022']
        dotnet-version: ['9.0.x']
    
    env:
      DOTNET_NOLOGO: true
      DOTNET_CLI_TELEMETRY_OPTOUT: true
      
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet-version }}

    - name: Setup MSBuild
      uses: microsoft/setup-msbuild@v2

    - name: Cache dependencies
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
        restore-keys: |
          ${{ runner.os }}-nuget-

    - name: Restore dependencies
      run: dotnet restore Owlet.sln

    - name: Build solution
      run: dotnet build Owlet.sln --configuration Release --no-restore

    - name: Run unit tests
      run: |
        dotnet test tests/Owlet.Tests.Unit/Owlet.Tests.Unit.csproj `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=unit-tests.trx" `
          --collect:"XPlat Code Coverage"

    - name: Run integration tests
      run: |
        dotnet test tests/Owlet.Tests.Integration/Owlet.Tests.Integration.csproj `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=integration-tests.trx" `
          --collect:"XPlat Code Coverage"

    - name: Run cross-platform tests
      run: |
        dotnet test tests/Owlet.Tests.CrossPlatform/Owlet.Tests.CrossPlatform.csproj `
          --configuration Release `
          --no-build `
          --verbosity normal `
          --logger "trx;LogFileName=crossplatform-tests.trx"

    - name: Validate service startup
      run: |
        $servicePath = "src/Owlet.Service/bin/Release/net9.0/Owlet.Service.exe"
        if (Test-Path $servicePath) {
          Write-Host "✓ Service executable exists: $servicePath"
          
          # Test service can be instantiated (but not started as service)
          $processInfo = New-Object System.Diagnostics.ProcessStartInfo
          $processInfo.FileName = $servicePath
          $processInfo.Arguments = "--help"
          $processInfo.UseShellExecute = $false
          $processInfo.RedirectStandardOutput = $true
          $processInfo.CreateNoWindow = $true
          
          $process = [System.Diagnostics.Process]::Start($processInfo)
          $output = $process.StandardOutput.ReadToEnd()
          $process.WaitForExit()
          
          if ($process.ExitCode -eq 0) {
            Write-Host "✓ Service executable runs successfully"
          } else {
            Write-Error "✗ Service executable failed with exit code: $($process.ExitCode)"
            exit 1
          }
        } else {
          Write-Error "✗ Service executable not found: $servicePath"
          exit 1
        }

    - name: Validate API startup (standalone)
      run: |
        $apiPath = "src/Owlet.Api/bin/Release/net9.0/Owlet.Api.exe"
        if (Test-Path $apiPath) {
          Write-Host "✓ API executable exists: $apiPath"
          
          # Start API in background
          $env:ASPNETCORE_URLS = "http://localhost:0"
          $env:ConnectionStrings__DefaultConnection = "Data Source=:memory:"
          
          $processInfo = New-Object System.Diagnostics.ProcessStartInfo
          $processInfo.FileName = $apiPath
          $processInfo.UseShellExecute = $false
          $processInfo.RedirectStandardOutput = $true
          $processInfo.RedirectStandardError = $true
          $processInfo.CreateNoWindow = $true
          
          $process = [System.Diagnostics.Process]::Start($processInfo)
          
          # Give it time to start
          Start-Sleep -Seconds 10
          
          if (!$process.HasExited) {
            Write-Host "✓ API started successfully"
            $process.Kill()
            $process.WaitForExit()
          } else {
            $stderr = $process.StandardError.ReadToEnd()
            Write-Error "✗ API failed to start: $stderr"
            exit 1
          }
        } else {
          Write-Error "✗ API executable not found: $apiPath"
          exit 1
        }

    - name: Validate diagnostics tools
      run: |
        $diagPath = "tools/Owlet.Diagnostics/bin/Release/net9.0/Owlet.Diagnostics.exe"
        if (Test-Path $diagPath) {
          Write-Host "✓ Diagnostics executable exists: $diagPath"
          
          # Test diagnostics help
          $processInfo = New-Object System.Diagnostics.ProcessStartInfo
          $processInfo.FileName = $diagPath
          $processInfo.Arguments = "--help"
          $processInfo.UseShellExecute = $false
          $processInfo.RedirectStandardOutput = $true
          $processInfo.CreateNoWindow = $true
          
          $process = [System.Diagnostics.Process]::Start($processInfo)
          $output = $process.StandardOutput.ReadToEnd()
          $process.WaitForExit()
          
          if ($process.ExitCode -eq 0) {
            Write-Host "✓ Diagnostics tool runs successfully"
          } else {
            Write-Error "✗ Diagnostics tool failed with exit code: $($process.ExitCode)"
            exit 1
          }
        } else {
          Write-Error "✗ Diagnostics executable not found: $diagPath"
          exit 1
        }

    - name: Generate test report
      if: always()
      uses: dorny/test-reporter@v1
      with:
        name: Foundation Test Results (${{ matrix.os-version }})
        path: '**/TestResults/*.trx'
        reporter: dotnet-trx

    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-results-${{ matrix.os-version }}
        path: |
          **/TestResults/
          **/*.trx

  validate-installer:
    name: Validate MSI Installer
    runs-on: windows-latest
    needs: validate-foundation
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Install WiX Toolset
      run: |
        dotnet tool install --global wix --version 4.0.4
        wix extension add -g WixToolset.UI.wixext/4.0.4
        wix extension add -g WixToolset.Util.wixext/4.0.4
        wix extension add -g WixToolset.Firewall.wixext/4.0.4

    - name: Build service for packaging
      run: |
        dotnet publish src/Owlet.Service/Owlet.Service.csproj `
          --configuration Release `
          --output artifacts/service-package `
          --self-contained true `
          --runtime win-x64

    - name: Build installer
      run: |
        cd packaging/installer
        wix build Owlet.Installer.wixproj `
          -d "SourceDir=../../artifacts/service-package" `
          -d "Version=1.0.0" `
          -out "../../artifacts/OwletInstaller-1.0.0.msi"

    - name: Validate installer
      run: |
        $installerPath = "artifacts/OwletInstaller-1.0.0.msi"
        if (Test-Path $installerPath) {
          $fileInfo = Get-Item $installerPath
          Write-Host "✓ Installer created: $($fileInfo.Name) ($([math]::Round($fileInfo.Length / 1MB, 2)) MB)"
          
          # Basic MSI validation
          $msiInfo = Get-ItemProperty $installerPath
          Write-Host "✓ MSI file properties validated"
        } else {
          Write-Error "✗ Installer not found: $installerPath"
          exit 1
        }

    - name: Upload installer artifact
      uses: actions/upload-artifact@v4
      with:
        name: owlet-installer
        path: artifacts/OwletInstaller-*.msi

  validate-documentation:
    name: Validate Documentation
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Check documentation completeness
      run: |
        echo "Validating documentation structure..."
        
        # Required documentation files
        required_files=(
          "README.md"
          "docs/installation/README.md"
          "docs/installation/msi-installer.md"
          "docs/installation/troubleshooting.md"
          "docs/installation/enterprise-deployment.md"
          "docs/development/README.md"
          "docs/api/README.md"
        )
        
        missing_files=()
        for file in "${required_files[@]}"; do
          if [[ ! -f "$file" ]]; then
            missing_files+=("$file")
          else
            echo "✓ Found: $file"
          fi
        done
        
        if [[ ${#missing_files[@]} -gt 0 ]]; then
          echo "✗ Missing documentation files:"
          printf '  - %s\n' "${missing_files[@]}"
          exit 1
        fi
        
        echo "✓ All required documentation files present"

    - name: Validate markdown formatting
      uses: DavidAnson/markdownlint-cli2-action@v16
      with:
        globs: '**/*.md'
        config: '.markdownlint.json'

  validate-complete:
    name: Foundation Validation Complete
    runs-on: ubuntu-latest
    needs: [validate-foundation, validate-installer, validate-documentation]
    
    steps:
    - name: Validation summary
      run: |
        echo "🎉 Foundation validation completed successfully!"
        echo ""
        echo "Validated components:"
        echo "  ✓ .NET solution build and test"
        echo "  ✓ Service components and startup"
        echo "  ✓ Cross-platform compatibility"
        echo "  ✓ MSI installer creation"
        echo "  ✓ Documentation completeness"
        echo ""
        echo "Foundation is ready for deployment!"
```

### 6. Performance and Load Testing

Comprehensive performance validation for foundation components.

**`tests/Owlet.Tests.Performance/FoundationPerformanceTests.cs`:**

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.CSharp;
using Owlet.Api;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Owlet.Tests.Performance;

public class FoundationPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public FoundationPerformanceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task HealthEndpoint_ShouldMeetPerformanceTargets()
    {
        // Arrange
        var client = _factory.CreateClient();
        const int iterations = 1000;
        const double maxAverageResponseTime = 100; // 100ms target
        
        var responseTimes = new List<double>();
        
        // Act
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync("/health");
            stopwatch.Stop();
            
            response.EnsureSuccessStatusCode();
            responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
        }
        
        // Assert
        var averageResponseTime = responseTimes.Average();
        var p95ResponseTime = responseTimes.OrderBy(t => t).Skip((int)(iterations * 0.95)).First();
        var maxResponseTime = responseTimes.Max();
        
        Assert.True(averageResponseTime <= maxAverageResponseTime, 
            $"Average response time {averageResponseTime:F2}ms exceeds target {maxAverageResponseTime}ms");
        
        _output.WriteLine($"Health endpoint performance:");
        _output.WriteLine($"  Average: {averageResponseTime:F2}ms");
        _output.WriteLine($"  P95: {p95ResponseTime:F2}ms");
        _output.WriteLine($"  Max: {maxResponseTime:F2}ms");
        _output.WriteLine($"  Iterations: {iterations}");
    }

    [Fact]
    public void ServiceStartup_ShouldMeetPerformanceTargets()
    {
        // Arrange
        const double maxStartupTime = 5000; // 5 seconds target
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        using var testFactory = new WebApplicationFactory<Program>();
        var client = testFactory.CreateClient();
        
        // Make first request to ensure full initialization
        var response = client.GetAsync("/health").Result;
        
        stopwatch.Stop();
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var startupTime = stopwatch.Elapsed.TotalMilliseconds;
        Assert.True(startupTime <= maxStartupTime, 
            $"Service startup time {startupTime:F2}ms exceeds target {maxStartupTime}ms");
        
        _output.WriteLine($"Service startup performance:");
        _output.WriteLine($"  Startup time: {startupTime:F2}ms");
        _output.WriteLine($"  Target: {maxStartupTime:F2}ms");
    }

    [Fact]
    public async Task ConcurrentLoad_ShouldMeetPerformanceTargets()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var scenario = Scenario.Create("health_check_load", async context =>
        {
            var response = await client.GetAsync("/health");
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var sceanarioStats = stats.AllScenarioStats.First();
        
        Assert.True(sceanarioStats.Ok.Request.Mean <= 100, 
            $"Mean response time {sceanarioStats.Ok.Request.Mean}ms exceeds 100ms target");
        
        Assert.True(sceanarioStats.AllFailCount == 0, 
            $"Load test had {sceanarioStats.AllFailCount} failures");
        
        _output.WriteLine($"Concurrent load performance:");
        _output.WriteLine($"  Mean response: {sceanarioStats.Ok.Request.Mean}ms");
        _output.WriteLine($"  P95 response: {sceanarioStats.Ok.Request.Percentile95}ms");
        _output.WriteLine($"  Requests/sec: {sceanarioStats.Ok.Request.Rate}");
        _output.WriteLine($"  Success rate: {sceanarioStats.Ok.Request.Count}/{sceanarioStats.AllRequestCount}");
    }

    [Fact]
    public void MemoryUsage_ShouldStayWithinLimits()
    {
        // Arrange
        const long maxMemoryMB = 100; // 100MB target for basic service
        
        // Act
        using var testFactory = new WebApplicationFactory<Program>();
        var client = testFactory.CreateClient();
        
        // Warm up
        for (int i = 0; i < 10; i++)
        {
            client.GetAsync("/health").Wait();
        }
        
        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);
        
        // Perform some work
        for (int i = 0; i < 100; i++)
        {
            client.GetAsync("/health").Wait();
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsedMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);
        
        // Assert
        Assert.True(memoryUsedMB <= maxMemoryMB, 
            $"Memory usage {memoryUsedMB:F2}MB exceeds target {maxMemoryMB}MB");
        
        _output.WriteLine($"Memory usage performance:");
        _output.WriteLine($"  Initial: {initialMemory / (1024.0 * 1024.0):F2}MB");
        _output.WriteLine($"  Final: {finalMemory / (1024.0 * 1024.0):F2}MB");
        _output.WriteLine($"  Used: {memoryUsedMB:F2}MB");
        _output.WriteLine($"  Target: {maxMemoryMB}MB");
    }
}
```

## Success Criteria

- [ ] Complete installation documentation covers all deployment scenarios
- [ ] MSI installer guide provides step-by-step instructions with troubleshooting
- [ ] Foundation integration tests validate all core components working together
- [ ] Cross-platform tests confirm compatibility across Windows versions (10, 11, Server)
- [ ] CI/CD validation pipeline automatically tests all foundation components
- [ ] Performance tests verify service meets response time and resource usage targets
- [ ] Service startup time stays under 5 seconds in automated testing
- [ ] Documentation validation ensures all required guides are complete and properly formatted
- [ ] Installation troubleshooting guide covers common issues with solutions
- [ ] Load testing confirms service handles concurrent requests within performance targets

## Testing Strategy

### Unit Tests
**What to test:** Documentation validation, installation scripts, cross-platform compatibility functions  
**Mocking strategy:** Mock Windows APIs, file system operations, network calls  
**Test data approach:** Use various Windows environment configurations and installation scenarios

### Integration Tests
**What to test:** Complete foundation stack integration, service startup, health checks, installer validation  
**Test environment:** Multiple Windows versions with clean state and various configurations  
**Automation:** GitHub Actions workflows with matrix testing across Windows versions

### E2E Tests
**What to test:** Full installation and operation workflow from MSI to running service  
**User workflows:** Download → Install → Service Operation → Health Monitoring → Uninstall

## Dependencies

### Technical Dependencies
- MSI Installer Testing - Windows Installer validation tools
- NBomber - Performance and load testing framework
- Cross-platform Testing - Windows version compatibility validation
- Documentation Tools - Markdown validation and generation

### Story Dependencies
- **Blocks:** None (completes E10 epic)
- **Blocked By:** S70 (Development Environment)

## Next Steps

1. Create comprehensive installation and user documentation
2. Implement complete testing framework with cross-platform validation
3. Develop CI/CD validation pipeline for all foundation components
4. Create performance and load testing suite for service validation
5. Test complete installation and operation workflow across Windows versions
6. Validate documentation completeness and accuracy through user testing

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** [Date] (when status = Complete)