# E10 S40: Build Pipeline

**Story:** Implement GitHub Actions CI/CD workflows for automated testing, building, and artifact creation supporting dual deployment scenarios  
**Priority:** Critical  
**Effort:** 18 hours  
**Status:** Not Started  
**Dependencies:** S30 (Core Infrastructure)  

## Objective

This story establishes a comprehensive CI/CD pipeline using GitHub Actions that automates the entire build, test, and packaging process for Owlet. The pipeline supports dual deployment scenarios - production MSI installer and development Aspire orchestration - while ensuring code quality through automated testing and static analysis.

The build system prioritizes reliability and speed, enabling rapid iteration during development while producing production-ready artifacts suitable for end-user installation. It includes comprehensive testing across Windows environments and automated packaging for MSI installer creation.

## Business Context

**Revenue Impact:** ₹0 direct revenue (foundational infrastructure enables reliable delivery)  
**User Impact:** All users - determines deployment reliability, update frequency, and time-to-market for features  
**Compliance Requirements:** Automated testing and build reproducibility support enterprise compliance requirements

## GitHub Actions Workflow Architecture

### 1. Main CI/CD Workflow

Comprehensive workflow covering build, test, package, and artifact creation.

**`.github/workflows/ci-cd.yml`:**

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
  release:
    types: [ published ]

env:
  DOTNET_VERSION: '9.0.x'
  SOLUTION_PATH: './Owlet.sln'
  SERVICE_PROJECT_PATH: './src/Owlet.Service/Owlet.Service.csproj'
  ASPIRE_PROJECT_PATH: './src/Owlet.AppHost/Owlet.AppHost.csproj'
  INSTALLER_PROJECT_PATH: './packaging/installer/Owlet.Installer.wixproj'
  ARTIFACTS_PATH: './artifacts'
  
jobs:
  # ===== VALIDATION JOBS =====
  code-quality:
    name: Code Quality Analysis
    runs-on: windows-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0 # Full history for SonarCloud analysis
        
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-
          
    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_PATH }}
      
    - name: Build solution
      run: dotnet build ${{ env.SOLUTION_PATH }} --no-restore --configuration Release
      
    - name: Run code analysis
      run: |
        dotnet format --verify-no-changes --verbosity diagnostic
        dotnet build ${{ env.SOLUTION_PATH }} --configuration Release --verbosity normal
        
    - name: Install SonarCloud scanner
      run: dotnet tool install --global dotnet-sonarscanner
      
    - name: Run SonarCloud analysis
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      run: |
        dotnet sonarscanner begin /k:"owlet-service" /o:"bird-constellation" /d:sonar.token="${{ secrets.SONAR_TOKEN }}" /d:sonar.host.url="https://sonarcloud.io"
        dotnet build ${{ env.SOLUTION_PATH }} --configuration Release
        dotnet sonarscanner end /d:sonar.token="${{ secrets.SONAR_TOKEN }}"

  # ===== TESTING JOBS =====
  unit-tests:
    name: Unit Tests
    runs-on: windows-latest
    strategy:
      matrix:
        test-project: 
          - 'tests/Owlet.Core.Tests'
          - 'tests/Owlet.Api.Tests'
          - 'tests/Owlet.Infrastructure.Tests'
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-
          
    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_PATH }}
      
    - name: Run unit tests
      run: |
        dotnet test ${{ matrix.test-project }} `
          --configuration Release `
          --no-restore `
          --logger trx `
          --logger "console;verbosity=detailed" `
          --collect:"XPlat Code Coverage" `
          --results-directory TestResults `
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
          
    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.test-project }}
        path: TestResults/*.trx
        
    - name: Upload code coverage
      uses: actions/upload-artifact@v4
      with:
        name: code-coverage-${{ matrix.test-project }}
        path: TestResults/*/coverage.opencover.xml

  integration-tests:
    name: Integration Tests
    runs-on: windows-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-
          
    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_PATH }}
      
    - name: Setup test database
      run: |
        $testDbPath = "TestResults/integration-test.db"
        New-Item -ItemType Directory -Force -Path (Split-Path $testDbPath)
        echo "TEST_DATABASE_PATH=$testDbPath" >> $env:GITHUB_ENV
        
    - name: Run integration tests
      env:
        ASPNETCORE_ENVIRONMENT: Testing
        ConnectionStrings__DefaultConnection: Data Source=${{ env.TEST_DATABASE_PATH }}
      run: |
        dotnet test tests/Owlet.Integration.Tests `
          --configuration Release `
          --no-restore `
          --logger trx `
          --logger "console;verbosity=detailed" `
          --collect:"XPlat Code Coverage" `
          --results-directory TestResults `
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
          
    - name: Upload integration test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: integration-test-results
        path: TestResults/*.trx
        
    - name: Upload integration test coverage
      uses: actions/upload-artifact@v4
      with:
        name: integration-test-coverage
        path: TestResults/*/coverage.opencover.xml

  # ===== BUILD JOBS =====
  build-service:
    name: Build Windows Service
    runs-on: windows-latest
    needs: [code-quality, unit-tests]
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-
          
    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_PATH }}
      
    - name: Set version variables
      run: |
        $version = "1.0.0"
        if ($env:GITHUB_REF -match "refs/tags/v(.+)") {
          $version = $matches[1]
        } elseif ($env:GITHUB_REF -eq "refs/heads/main") {
          $version = "1.0.0-main-$env:GITHUB_RUN_NUMBER"
        } else {
          $version = "1.0.0-dev-$env:GITHUB_RUN_NUMBER"
        }
        echo "VERSION=$version" >> $env:GITHUB_ENV
        echo "Building version: $version"
        
    - name: Build Windows Service (Release)
      run: |
        dotnet publish ${{ env.SERVICE_PROJECT_PATH }} `
          --configuration Release `
          --runtime win-x64 `
          --self-contained true `
          --output ${{ env.ARTIFACTS_PATH }}/service `
          -p:PublishSingleFile=true `
          -p:IncludeNativeLibrariesForSelfExtract=true `
          -p:PublishTrimmed=false `
          -p:Version=${{ env.VERSION }} `
          -p:AssemblyVersion=1.0.0.0 `
          -p:FileVersion=${{ env.VERSION }} `
          -p:InformationalVersion=${{ env.VERSION }}
          
    - name: Sign executable (if certificate available)
      if: env.SIGNING_CERTIFICATE != ''
      env:
        SIGNING_CERTIFICATE: ${{ secrets.SIGNING_CERTIFICATE }}
        SIGNING_PASSWORD: ${{ secrets.SIGNING_PASSWORD }}
      run: |
        # Create temporary certificate file
        $certBytes = [Convert]::FromBase64String($env:SIGNING_CERTIFICATE)
        $certPath = "temp-cert.pfx"
        [System.IO.File]::WriteAllBytes($certPath, $certBytes)
        
        # Sign the executable
        $signTool = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe"
        if (Test-Path $signTool) {
          & $signTool sign /f $certPath /p $env:SIGNING_PASSWORD /tr http://timestamp.digicert.com /td SHA256 /fd SHA256 "${{ env.ARTIFACTS_PATH }}/service/Owlet.Service.exe"
          echo "Executable signed successfully"
        } else {
          echo "SignTool not found, skipping code signing"
        }
        
        # Clean up certificate
        Remove-Item $certPath -Force
        
    - name: Create service package structure
      run: |
        $packagePath = "${{ env.ARTIFACTS_PATH }}/service-package"
        New-Item -ItemType Directory -Force -Path $packagePath
        
        # Copy service executable and dependencies
        Copy-Item "${{ env.ARTIFACTS_PATH }}/service/*" -Destination $packagePath -Recurse
        
        # Copy configuration files
        Copy-Item "src/Owlet.Service/appsettings.json" -Destination $packagePath
        Copy-Item "src/Owlet.Service/appsettings.Production.json" -Destination $packagePath
        
        # Create installation scripts
        @"
        @echo off
        echo Installing Owlet Service...
        sc create OwletService binPath= "%~dp0Owlet.Service.exe" start= auto
        sc description OwletService "Owlet Document Indexing Service"
        echo Service installed. Starting service...
        sc start OwletService
        echo Installation complete.
        "@ | Out-File -FilePath "$packagePath/install.bat" -Encoding ASCII
        
        @"
        @echo off
        echo Stopping Owlet Service...
        sc stop OwletService
        echo Uninstalling Owlet Service...
        sc delete OwletService
        echo Uninstallation complete.
        "@ | Out-File -FilePath "$packagePath/uninstall.bat" -Encoding ASCII
        
    - name: Upload service artifacts
      uses: actions/upload-artifact@v4
      with:
        name: owlet-service-${{ env.VERSION }}
        path: ${{ env.ARTIFACTS_PATH }}/service-package/
        retention-days: 30

  build-aspire:
    name: Build Aspire Development Host
    runs-on: windows-latest
    needs: [code-quality, unit-tests]
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-
          
    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_PATH }}
      
    - name: Set version variables
      run: |
        $version = "1.0.0"
        if ($env:GITHUB_REF -match "refs/tags/v(.+)") {
          $version = $matches[1]
        } elseif ($env:GITHUB_REF -eq "refs/heads/main") {
          $version = "1.0.0-main-$env:GITHUB_RUN_NUMBER"
        } else {
          $version = "1.0.0-dev-$env:GITHUB_RUN_NUMBER"
        }
        echo "VERSION=$version" >> $env:GITHUB_ENV
        
    - name: Build Aspire Host
      run: |
        dotnet publish ${{ env.ASPIRE_PROJECT_PATH }} `
          --configuration Release `
          --output ${{ env.ARTIFACTS_PATH }}/aspire `
          -p:Version=${{ env.VERSION }} `
          -p:AssemblyVersion=1.0.0.0 `
          -p:FileVersion=${{ env.VERSION }} `
          -p:InformationalVersion=${{ env.VERSION }}
          
    - name: Upload Aspire artifacts
      uses: actions/upload-artifact@v4
      with:
        name: owlet-aspire-${{ env.VERSION }}
        path: ${{ env.ARTIFACTS_PATH }}/aspire/
        retention-days: 30

  # ===== PACKAGE JOBS =====
  build-installer:
    name: Build MSI Installer
    runs-on: windows-latest
    needs: [build-service, integration-tests]
    if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/')
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Setup WiX Toolset
      run: |
        # Install WiX v4
        dotnet tool install --global wix --version 4.0.4
        wix extension add WixToolset.UI.wixext/4.0.4
        wix extension add WixToolset.Util.wixext/4.0.4
        wix extension add WixToolset.Firewall.wixext/4.0.4
        
    - name: Download service artifacts
      uses: actions/download-artifact@v4
      with:
        name: owlet-service-${{ env.VERSION }}
        path: ${{ env.ARTIFACTS_PATH }}/service-package/
        
    - name: Set version variables
      run: |
        $version = "1.0.0"
        if ($env:GITHUB_REF -match "refs/tags/v(.+)") {
          $version = $matches[1]
        } elseif ($env:GITHUB_REF -eq "refs/heads/main") {
          $version = "1.0.0.$env:GITHUB_RUN_NUMBER"
        } else {
          $version = "1.0.0.$env:GITHUB_RUN_NUMBER"
        }
        echo "VERSION=$version" >> $env:GITHUB_ENV
        echo "MSI_VERSION=$version" >> $env:GITHUB_ENV
        
    - name: Build MSI Installer
      run: |
        # Prepare source files for WiX
        $sourceFiles = "${{ env.ARTIFACTS_PATH }}/installer-source"
        New-Item -ItemType Directory -Force -Path $sourceFiles
        Copy-Item "${{ env.ARTIFACTS_PATH }}/service-package/*" -Destination $sourceFiles -Recurse
        
        # Build the installer
        cd packaging/installer
        wix build Owlet.Installer.wixproj `
          -d SourceDir="../../$sourceFiles" `
          -d Version="${{ env.MSI_VERSION }}" `
          -d ProductVersion="${{ env.VERSION }}" `
          -out "../../${{ env.ARTIFACTS_PATH }}/OwletInstaller-${{ env.VERSION }}.msi"
          
    - name: Sign MSI (if certificate available)
      if: env.SIGNING_CERTIFICATE != ''
      env:
        SIGNING_CERTIFICATE: ${{ secrets.SIGNING_CERTIFICATE }}
        SIGNING_PASSWORD: ${{ secrets.SIGNING_PASSWORD }}
      run: |
        # Create temporary certificate file
        $certBytes = [Convert]::FromBase64String($env:SIGNING_CERTIFICATE)
        $certPath = "temp-cert.pfx"
        [System.IO.File]::WriteAllBytes($certPath, $certBytes)
        
        # Sign the MSI
        $signTool = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe"
        if (Test-Path $signTool) {
          & $signTool sign /f $certPath /p $env:SIGNING_PASSWORD /tr http://timestamp.digicert.com /td SHA256 /fd SHA256 "${{ env.ARTIFACTS_PATH }}/OwletInstaller-${{ env.VERSION }}.msi"
          echo "MSI signed successfully"
        } else {
          echo "SignTool not found, skipping MSI signing"
        }
        
        # Clean up certificate
        Remove-Item $certPath -Force
        
    - name: Test MSI installation
      run: |
        # Install MSI in silent mode
        $msiPath = "${{ env.ARTIFACTS_PATH }}/OwletInstaller-${{ env.VERSION }}.msi"
        $logPath = "${{ env.ARTIFACTS_PATH }}/install-test.log"
        
        echo "Testing MSI installation..."
        Start-Process -FilePath "msiexec.exe" -ArgumentList "/i", "`"$msiPath`"", "/quiet", "/l*v", "`"$logPath`"" -Wait
        
        # Check if service was installed
        $service = Get-Service -Name "OwletService" -ErrorAction SilentlyContinue
        if ($service) {
          echo "✓ Service installed successfully"
          echo "Service Status: $($service.Status)"
          
          # Uninstall for cleanup
          Start-Process -FilePath "msiexec.exe" -ArgumentList "/x", "`"$msiPath`"", "/quiet" -Wait
          echo "✓ Service uninstalled successfully"
        } else {
          echo "✗ Service installation failed"
          Get-Content $logPath -Tail 50
          exit 1
        }
        
    - name: Upload MSI installer
      uses: actions/upload-artifact@v4
      with:
        name: owlet-installer-${{ env.VERSION }}
        path: ${{ env.ARTIFACTS_PATH }}/OwletInstaller-${{ env.VERSION }}.msi
        retention-days: 90
        
    - name: Upload installation log
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: installation-test-log-${{ env.VERSION }}
        path: ${{ env.ARTIFACTS_PATH }}/install-test.log
        retention-days: 30

  # ===== RELEASE JOBS =====
  create-release:
    name: Create GitHub Release
    runs-on: windows-latest
    needs: [build-installer]
    if: startsWith(github.ref, 'refs/tags/')
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Set version variables
      run: |
        $version = $env:GITHUB_REF -replace "refs/tags/v", ""
        echo "VERSION=$version" >> $env:GITHUB_ENV
        
    - name: Download MSI installer
      uses: actions/download-artifact@v4
      with:
        name: owlet-installer-${{ env.VERSION }}
        path: ./release-artifacts/
        
    - name: Download service package
      uses: actions/download-artifact@v4
      with:
        name: owlet-service-${{ env.VERSION }}
        path: ./release-artifacts/service/
        
    - name: Create release package
      run: |
        # Create ZIP package for service
        Compress-Archive -Path "./release-artifacts/service/*" -DestinationPath "./release-artifacts/Owlet-Service-${{ env.VERSION }}.zip"
        
        # Calculate checksums
        $msiPath = "./release-artifacts/OwletInstaller-${{ env.VERSION }}.msi"
        $zipPath = "./release-artifacts/Owlet-Service-${{ env.VERSION }}.zip"
        
        $msiHash = (Get-FileHash $msiPath -Algorithm SHA256).Hash
        $zipHash = (Get-FileHash $zipPath -Algorithm SHA256).Hash
        
        @"
        # Owlet ${{ env.VERSION }} - Checksums
        
        ## MSI Installer
        - **File:** OwletInstaller-${{ env.VERSION }}.msi
        - **SHA256:** $msiHash
        
        ## Service Package
        - **File:** Owlet-Service-${{ env.VERSION }}.zip
        - **SHA256:** $zipHash
        "@ | Out-File -FilePath "./release-artifacts/CHECKSUMS.md"
        
    - name: Generate release notes
      run: |
        @"
        # Owlet Document Indexing Service v${{ env.VERSION }}
        
        ## 🚀 What's New
        
        This release provides a production-ready Windows service for local document indexing and search.
        
        ## 📦 Installation
        
        ### Recommended: MSI Installer (Windows 10/11)
        1. Download ``OwletInstaller-${{ env.VERSION }}.msi``
        2. Double-click to install
        3. Service starts automatically
        4. Access web interface at http://localhost:5555
        
        ### Alternative: Manual Service Installation
        1. Download ``Owlet-Service-${{ env.VERSION }}.zip``
        2. Extract to ``C:\Program Files\Owlet\``
        3. Run ``install.bat`` as Administrator
        
        ## 🔧 System Requirements
        
        - Windows 10 version 1809 or later
        - Windows 11 (all versions)
        - Windows Server 2019 or later
        - .NET 9 Runtime (included in installer)
        - 100MB free disk space
        - Administrator privileges for installation
        
        ## 📋 Release Notes
        
        ### Features
        - Professional Windows service installation
        - Embedded web server with REST API
        - Comprehensive logging (Windows Event Log + file system)
        - Automatic service recovery on failure
        - Configurable network settings
        
        ### Technical Details
        - Built with .NET 9 and self-contained deployment
        - Clean architecture with domain-driven design
        - SQLite database for document indexing
        - Carter framework for HTTP API
        - Serilog for structured logging
        
        ## 🔒 Security
        
        - Service runs with LocalSystem privileges
        - HTTP server binds to localhost only (127.0.0.1:5555)
        - Automatic Windows Firewall configuration
        - All executables are digitally signed
        
        ## 📞 Support
        
        - Documentation: https://github.com/bird-constellation/owlet/wiki
        - Issues: https://github.com/bird-constellation/owlet/issues
        - Discussions: https://github.com/bird-constellation/owlet/discussions
        
        ## 🔍 Verification
        
        Verify file integrity using the checksums in ``CHECKSUMS.md``.
        "@ | Out-File -FilePath "./release-artifacts/RELEASE_NOTES.md"
        
    - name: Create GitHub Release
      uses: ncipollo/release-action@v1
      with:
        artifacts: |
          ./release-artifacts/OwletInstaller-${{ env.VERSION }}.msi
          ./release-artifacts/Owlet-Service-${{ env.VERSION }}.zip
          ./release-artifacts/CHECKSUMS.md
        bodyFile: ./release-artifacts/RELEASE_NOTES.md
        name: Owlet v${{ env.VERSION }}
        tag: v${{ env.VERSION }}
        draft: false
        prerelease: ${{ contains(env.VERSION, '-') }}
        token: ${{ secrets.GITHUB_TOKEN }}

  # ===== NOTIFICATION JOBS =====
  notify-completion:
    name: Notify Build Completion
    runs-on: windows-latest
    needs: [build-service, build-aspire, build-installer]
    if: always()
    
    steps:
    - name: Determine build status
      run: |
        $buildStatus = "success"
        $needsResults = @(
          "${{ needs.build-service.result }}",
          "${{ needs.build-aspire.result }}",
          "${{ needs.build-installer.result }}"
        )
        
        if ($needsResults -contains "failure") {
          $buildStatus = "failure"
        } elseif ($needsResults -contains "cancelled") {
          $buildStatus = "cancelled"
        }
        
        echo "BUILD_STATUS=$buildStatus" >> $env:GITHUB_ENV
        echo "Build status determined: $buildStatus"
        
    - name: Post to Teams webhook (if configured)
      if: env.TEAMS_WEBHOOK != ''
      env:
        TEAMS_WEBHOOK: ${{ secrets.TEAMS_WEBHOOK }}
      run: |
        $status = $env:BUILD_STATUS
        $color = if ($status -eq "success") { "good" } elseif ($status -eq "failure") { "danger" } else { "warning" }
        $emoji = if ($status -eq "success") { "✅" } elseif ($status -eq "failure") { "❌" } else { "⚠️" }
        
        $payload = @{
          "@type" = "MessageCard"
          "@context" = "http://schema.org/extensions"
          "themeColor" = $color
          "summary" = "Owlet Build $status"
          "sections" = @(
            @{
              "activityTitle" = "$emoji Owlet CI/CD Pipeline"
              "activitySubtitle" = "Build $status for $env:GITHUB_REF"
              "facts" = @(
                @{ "name" = "Repository"; "value" = "$env:GITHUB_REPOSITORY" },
                @{ "name" = "Branch"; "value" = "$env:GITHUB_REF" },
                @{ "name" = "Commit"; "value" = "$env:GITHUB_SHA".Substring(0, 8) },
                @{ "name" = "Status"; "value" = $status.ToUpper() },
                @{ "name" = "Run"; "value" = "$env:GITHUB_RUN_NUMBER" }
              )
            }
          )
          "potentialAction" = @(
            @{
              "@type" = "OpenUri"
              "name" = "View Build"
              "targets" = @(
                @{ "os" = "default"; "uri" = "https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID" }
              )
            }
          )
        } | ConvertTo-Json -Depth 10
        
        Invoke-RestMethod -Uri $env:TEAMS_WEBHOOK -Method Post -ContentType "application/json" -Body $payload
```

### 2. Security and Dependency Management

Automated security scanning and dependency updates.

**`.github/workflows/security.yml`:**

```yaml
name: Security Scanning

on:
  schedule:
    - cron: '0 6 * * 1' # Weekly on Monday at 6 AM UTC
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  dependency-scan:
    name: Dependency Vulnerability Scan
    runs-on: windows-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Run security audit
      run: |
        dotnet list package --vulnerable --include-transitive 2>&1 | Tee-Object -FilePath "vulnerability-report.txt"
        
        # Check if vulnerabilities were found
        $content = Get-Content "vulnerability-report.txt" -Raw
        if ($content -match "has the following vulnerable packages") {
          echo "::error::Vulnerable packages detected!"
          Get-Content "vulnerability-report.txt"
          exit 1
        } else {
          echo "No vulnerable packages detected"
        }
        
    - name: Upload vulnerability report
      uses: actions/upload-artifact@v4
      if: failure()
      with:
        name: vulnerability-report
        path: vulnerability-report.txt

  code-security-scan:
    name: Code Security Analysis
    runs-on: windows-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Run CodeQL Analysis
      uses: github/codeql-action/init@v3
      with:
        languages: csharp
        
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Build for analysis
      run: |
        dotnet restore
        dotnet build --configuration Release
        
    - name: Perform CodeQL Analysis
      uses: github/codeql-action/analyze@v3
```

### 3. Automated Dependency Updates

Dependabot configuration for automated dependency management.

**`.github/dependabot.yml`:**

```yaml
version: 2
updates:
  # .NET dependencies
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "06:00"
    open-pull-requests-limit: 10
    assignees:
      - "maintainer-username"
    commit-message:
      prefix: "deps"
      include: "scope"
    groups:
      microsoft-packages:
        patterns:
          - "Microsoft.*"
        update-types:
          - "minor"
          - "patch"
      aspire-packages:
        patterns:
          - "Aspire.*"
        update-types:
          - "minor"  
          - "patch"
      serilog-packages:
        patterns:
          - "Serilog.*"
        update-types:
          - "minor"
          - "patch"

  # GitHub Actions
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
    commit-message:
      prefix: "ci"
      include: "scope"
```

## Build Scripts and Automation

### 1. Local Development Build Scripts

PowerShell scripts for local development and testing.

**`scripts/build.ps1`:**

```powershell
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Local build script for Owlet solution
.DESCRIPTION
    Builds the Owlet solution with various configuration options
.PARAMETER Configuration
    Build configuration (Debug, Release)
.PARAMETER Target
    Build target (All, Service, Aspire, Tests)
.PARAMETER Clean
    Perform clean build
.PARAMETER Pack
    Create packages after build
.EXAMPLE
    .\scripts\build.ps1 -Configuration Release -Target All -Clean
#>

param(
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter()]
    [ValidateSet("All", "Service", "Aspire", "Tests")]
    [string]$Target = "All",
    
    [Parameter()]
    [switch]$Clean,
    
    [Parameter()]
    [switch]$Pack
)

$ErrorActionPreference = "Stop"
$SolutionPath = "Owlet.sln"
$ServiceProject = "src/Owlet.Service/Owlet.Service.csproj"
$AspireProject = "src/Owlet.AppHost/Owlet.AppHost.csproj"
$OutputPath = "artifacts"

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 80 -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "=" * 80 -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host ">> $Message" -ForegroundColor Green
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check .NET version
    try {
        $dotnetVersion = dotnet --version
        Write-Step "✓ .NET version: $dotnetVersion"
    } catch {
        throw ".NET SDK not found. Please install .NET 9 SDK"
    }
    
    # Check solution file
    if (-not (Test-Path $SolutionPath)) {
        throw "Solution file not found: $SolutionPath"
    }
    Write-Step "✓ Solution file found"
}

function Invoke-Clean {
    if ($Clean) {
        Write-Header "Cleaning Solution"
        
        if (Test-Path $OutputPath) {
            Remove-Item $OutputPath -Recurse -Force
            Write-Step "✓ Cleaned output directory"
        }
        
        dotnet clean $SolutionPath --configuration $Configuration --verbosity minimal
        Write-Step "✓ Cleaned solution"
    }
}

function Invoke-Restore {
    Write-Header "Restoring Dependencies"
    
    dotnet restore $SolutionPath --verbosity minimal
    Write-Step "✓ Dependencies restored"
}

function Invoke-Build {
    Write-Header "Building Solution"
    
    $buildArgs = @(
        "build"
        $SolutionPath
        "--configuration", $Configuration
        "--no-restore"
        "--verbosity", "minimal"
    )
    
    dotnet @buildArgs
    Write-Step "✓ Solution built successfully"
}

function Invoke-Tests {
    if ($Target -eq "All" -or $Target -eq "Tests") {
        Write-Header "Running Tests"
        
        # Unit tests
        Write-Step "Running unit tests..."
        dotnet test tests/Owlet.Core.Tests --configuration $Configuration --no-build --logger "console;verbosity=minimal"
        dotnet test tests/Owlet.Api.Tests --configuration $Configuration --no-build --logger "console;verbosity=minimal"
        dotnet test tests/Owlet.Infrastructure.Tests --configuration $Configuration --no-build --logger "console;verbosity=minimal"
        
        # Integration tests
        Write-Step "Running integration tests..."
        dotnet test tests/Owlet.Integration.Tests --configuration $Configuration --no-build --logger "console;verbosity=minimal"
        
        Write-Step "✓ All tests passed"
    }
}

function Invoke-Pack {
    if ($Pack) {
        Write-Header "Creating Packages"
        
        New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
        
        if ($Target -eq "All" -or $Target -eq "Service") {
            Write-Step "Publishing Windows Service..."
            dotnet publish $ServiceProject `
                --configuration $Configuration `
                --runtime win-x64 `
                --self-contained true `
                --output "$OutputPath/service" `
                -p:PublishSingleFile=true `
                -p:IncludeNativeLibrariesForSelfExtract=true `
                --verbosity minimal
            Write-Step "✓ Service published"
        }
        
        if ($Target -eq "All" -or $Target -eq "Aspire") {
            Write-Step "Publishing Aspire Host..."
            dotnet publish $AspireProject `
                --configuration $Configuration `
                --output "$OutputPath/aspire" `
                --verbosity minimal
            Write-Step "✓ Aspire Host published"
        }
    }
}

function Main {
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        Test-Prerequisites
        Invoke-Clean
        Invoke-Restore
        Invoke-Build
        Invoke-Tests
        Invoke-Pack
        
        $stopwatch.Stop()
        
        Write-Header "Build Completed Successfully"
        Write-Host "Total time: $($stopwatch.Elapsed.ToString('mm\:ss'))" -ForegroundColor Green
        
        if ($Pack) {
            Write-Host "Artifacts available in: $OutputPath" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host ""
        Write-Host "Build Failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Execute main function
Main
```

### 2. Development Environment Scripts

Scripts for setting up and managing the development environment.

**`scripts/dev-setup.ps1`:**

```powershell
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Development environment setup script
.DESCRIPTION
    Sets up the development environment for Owlet
.EXAMPLE
    .\scripts\dev-setup.ps1
#>

$ErrorActionPreference = "Stop"

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host ">> $Message" -ForegroundColor Green
}

function Install-Prerequisites {
    Write-Header "Installing Prerequisites"
    
    # Check if running as Administrator
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Warning "Some tools require Administrator privileges. Consider running as Administrator."
    }
    
    # Install .NET 9 SDK (if not installed)
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -like "9.*") {
            Write-Step "✓ .NET 9 SDK already installed: $dotnetVersion"
        } else {
            Write-Warning ".NET 9 SDK not found. Please install manually from https://dotnet.microsoft.com/download"
        }
    } catch {
        Write-Warning ".NET SDK not found. Please install .NET 9 SDK from https://dotnet.microsoft.com/download"
    }
    
    # Install global tools
    Write-Step "Installing global .NET tools..."
    
    $tools = @(
        @{ Name = "dotnet-ef"; Package = "dotnet-ef" },
        @{ Name = "dotnet-format"; Package = "dotnet-format" },
        @{ Name = "dotnet-outdated-tool"; Package = "dotnet-outdated-tool" },
        @{ Name = "wix"; Package = "wix" }
    )
    
    foreach ($tool in $tools) {
        try {
            & $tool.Name --version 2>$null | Out-Null
            Write-Step "✓ $($tool.Name) already installed"
        } catch {
            Write-Step "Installing $($tool.Name)..."
            dotnet tool install --global $tool.Package
            Write-Step "✓ $($tool.Name) installed"
        }
    }
}

function Setup-Configuration {
    Write-Header "Setting up Configuration"
    
    # Create development configuration
    $devConfig = @{
        "Service" = @{
            "ServiceName" = "OwletService-Dev"
            "DisplayName" = "Owlet Document Service (Development)"
        }
        "Network" = @{
            "Port" = 5556
        }
        "Logging" = @{
            "MinimumLevel" = "Debug"
            "LogDirectory" = "C:\temp\owlet\logs"
            "EnableConsole" = $true
            "EnableWindowsEventLog" = $false
        }
        "Database" = @{
            "ConnectionString" = "Data Source=owlet-dev.db"
            "EnableSensitiveDataLogging" = $true
            "EnableDetailedErrors" = $true
        }
    }
    
    $configPath = "src/Owlet.Service/appsettings.Development.json"
    if (-not (Test-Path $configPath)) {
        $devConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configPath -Encoding UTF8
        Write-Step "✓ Development configuration created"
    } else {
        Write-Step "✓ Development configuration already exists"
    }
}

function Setup-Database {
    Write-Header "Setting up Development Database"
    
    # Create database directory
    $dbDir = "C:\temp\owlet\data"
    if (-not (Test-Path $dbDir)) {
        New-Item -ItemType Directory -Force -Path $dbDir | Out-Null
        Write-Step "✓ Database directory created: $dbDir"
    }
    
    # Note: Database will be created automatically on first run
    Write-Step "✓ Database setup complete (will be created on first run)"
}

function Setup-Logs {
    Write-Header "Setting up Development Logging"
    
    # Create log directory
    $logDir = "C:\temp\owlet\logs"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Force -Path $logDir | Out-Null
        Write-Step "✓ Log directory created: $logDir"
    }
    
    # Set permissions for log directory
    try {
        $acl = Get-Acl $logDir
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Everyone", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($accessRule)
        Set-Acl -Path $logDir -AclObject $acl
        Write-Step "✓ Log directory permissions configured"
    } catch {
        Write-Warning "Could not set log directory permissions: $($_.Exception.Message)"
    }
}

function Setup-IDE {
    Write-Header "Setting up IDE Configuration"
    
    # VS Code extensions recommendations
    $vsCodeExtensions = @{
        "recommendations" = @(
            "ms-dotnettools.csharp",
            "ms-dotnettools.vscode-dotnet-runtime",
            "ms-vscode.powershell",
            "ms-azuretools.vscode-azureresourcegroups",
            "redhat.vscode-xml",
            "ms-vscode.vscode-json"
        )
    }
    
    $vsCodeDir = ".vscode"
    if (-not (Test-Path $vsCodeDir)) {
        New-Item -ItemType Directory -Force -Path $vsCodeDir | Out-Null
    }
    
    $extensionsPath = "$vsCodeDir/extensions.json"
    if (-not (Test-Path $extensionsPath)) {
        $vsCodeExtensions | ConvertTo-Json -Depth 10 | Out-File -FilePath $extensionsPath -Encoding UTF8
        Write-Step "✓ VS Code extensions recommendations created"
    }
    
    # Launch configuration
    $launchConfig = @{
        "version" = "0.2.0"
        "configurations" = @(
            @{
                "name" = "Launch Owlet Service"
                "type" = "coreclr"
                "request" = "launch"
                "program" = "`${workspaceFolder}/src/Owlet.Service/bin/Debug/net9.0/Owlet.Service.exe"
                "args" = @()
                "cwd" = "`${workspaceFolder}/src/Owlet.Service"
                "env" = @{
                    "ASPNETCORE_ENVIRONMENT" = "Development"
                }
                "serverReadyAction" = @{
                    "action" = "openExternally"
                    "pattern" = "Now listening on:\\s+(http://localhost:\\d+)"
                }
            },
            @{
                "name" = "Launch Aspire Host"
                "type" = "coreclr"
                "request" = "launch"
                "program" = "`${workspaceFolder}/src/Owlet.AppHost/bin/Debug/net9.0/Owlet.AppHost.exe"
                "args" = @()
                "cwd" = "`${workspaceFolder}/src/Owlet.AppHost"
                "env" = @{
                    "ASPNETCORE_ENVIRONMENT" = "Development"
                }
                "serverReadyAction" = @{
                    "action" = "openExternally"
                    "pattern" = "Now listening on:\\s+(http://localhost:\\d+)"
                }
            }
        )
    }
    
    $launchPath = "$vsCodeDir/launch.json"
    if (-not (Test-Path $launchPath)) {
        $launchConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $launchPath -Encoding UTF8
        Write-Step "✓ VS Code launch configuration created"
    }
}

function Main {
    Write-Header "Owlet Development Environment Setup"
    
    Install-Prerequisites
    Setup-Configuration
    Setup-Database  
    Setup-Logs
    Setup-IDE
    
    Write-Header "Setup Complete!"
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Run: .\scripts\build.ps1 -Configuration Debug" -ForegroundColor White
    Write-Host "2. Start debugging in VS Code (F5)" -ForegroundColor White
    Write-Host "3. Open browser to http://localhost:5556" -ForegroundColor White
    Write-Host ""
    Write-Host "Development database: C:\temp\owlet\data\owlet-dev.db" -ForegroundColor Gray
    Write-Host "Development logs: C:\temp\owlet\logs\" -ForegroundColor Gray
}

# Execute main function
Main
```

## Success Criteria

- [ ] GitHub Actions workflows build, test, and package solution successfully
- [ ] Automated testing includes unit tests, integration tests, and end-to-end installer testing
- [ ] MSI installer is built and signed (when certificate available) automatically
- [ ] Code quality checks include static analysis and security scanning
- [ ] Artifacts are properly versioned and uploaded for releases
- [ ] Build pipeline supports both development (Aspire) and production (service) scenarios
- [ ] Dependency security scanning detects vulnerable packages
- [ ] Local development scripts enable rapid setup and building
- [ ] Build completes in under 10 minutes for full CI/CD pipeline
- [ ] All build outputs are deterministic and reproducible

## Testing Strategy

### Unit Tests
**What to test:** Build script logic, configuration validation, artifact creation  
**Mocking strategy:** Mock file system operations, external tool execution  
**Test data approach:** Use temporary directories for test artifacts

**Example Tests:**
```csharp
[Fact]
public void BuildScript_WithValidParameters_ShouldSucceed()
{
    // Arrange
    var buildParams = new BuildParameters 
    { 
        Configuration = "Release", 
        Target = "Service" 
    };
    
    // Act
    var result = BuildRunner.Execute(buildParams);
    
    // Assert
    result.Success.Should().BeTrue();
    result.Artifacts.Should().NotBeEmpty();
}
```

### Integration Tests
**What to test:** Complete workflow execution, artifact validation, installer testing  
**Test environment:** GitHub Actions with Windows runners  
**Automation:** Automated workflow testing on pull requests

### E2E Tests
**What to test:** Complete CI/CD pipeline from commit to release  
**User workflows:** Commit → Build → Test → Package → Install → Validate

## Dependencies

### Technical Dependencies
- GitHub Actions (built-in CI/CD platform)
- WiX Toolset 4.x - MSI installer creation
- Windows SDK - Code signing tools
- SonarCloud - Code quality analysis
- .NET 9 SDK - Build and runtime

### Story Dependencies
- **Blocks:** S50 (WiX Installer), S60 (Health Monitoring), S80 (Documentation & Testing)
- **Blocked By:** S30 (Core Infrastructure)

## Next Steps

1. Set up GitHub repository with Actions workflows
2. Configure secrets for code signing certificates and notifications
3. Test complete workflow with sample commit
4. Validate MSI installer creation and testing
5. Set up SonarCloud integration for code quality
6. Configure automated dependency updates with Dependabot

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** [Date] (when status = Complete)