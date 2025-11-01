#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Local CI validation script - Run before pushing to catch CI failures early
.DESCRIPTION
    Simulates the GitHub Actions CI pipeline locally:
    - Restores dependencies
    - Builds solution in Release mode
    - Runs all tests with coverage
    - Checks for vulnerable packages
    - Validates assembly versions
.EXAMPLE
    .\scripts\validate-build.ps1
.EXAMPLE
    .\scripts\validate-build.ps1 -SkipTests
#>

[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$SkipSecurity,
    [switch]$Fast
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors
function Write-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Failure { Write-Host "❌ $args" -ForegroundColor Red }
function Write-Step { Write-Host "`n🔹 $args" -ForegroundColor Cyan }
function Write-Warning { Write-Host "⚠️  $args" -ForegroundColor Yellow }

# Timer
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    Write-Host "`n🦉 Owlet Local CI Validation" -ForegroundColor Magenta
    Write-Host "============================`n" -ForegroundColor Magenta

    # Check prerequisites
    Write-Step "Checking prerequisites..."
    if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Failure ".NET SDK not found. Install from https://dotnet.microsoft.com"
        exit 1
    }
    
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK $dotnetVersion found"

    # Clean previous builds (optional)
    if (!$Fast) {
        Write-Step "Cleaning previous builds..."
        dotnet clean --configuration Release --verbosity quiet
        Write-Success "Cleaned"
    }

    # Restore dependencies
    Write-Step "Restoring dependencies..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Restore failed"
        exit $LASTEXITCODE
    }
    Write-Success "Dependencies restored"

    # Build solution (Release mode like CI)
    Write-Step "Building solution (Release)..."
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Build failed"
        exit $LASTEXITCODE
    }
    Write-Success "Build succeeded"

    # Run tests
    if (!$SkipTests) {
        Write-Step "Running tests..."
        
        $testProjects = @(
            "tests/Owlet.Core.Tests",
            "tests/Owlet.Api.Tests",
            "tests/Owlet.Infrastructure.Tests"
        )

        $testsFailed = $false
        foreach ($project in $testProjects) {
            Write-Host "  Testing: $project" -ForegroundColor Gray
            dotnet test $project `
                --configuration Release `
                --no-restore `
                --no-build `
                --logger "console;verbosity=minimal" `
                --collect:"XPlat Code Coverage" `
                -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
            
            if ($LASTEXITCODE -ne 0) {
                Write-Failure "  $project failed"
                $testsFailed = $true
            }
        }

        if ($testsFailed) {
            Write-Failure "Some tests failed"
            exit 1
        }
        Write-Success "All tests passed"
    } else {
        Write-Warning "Skipping tests (--SkipTests)"
    }

    # Security scan
    if (!$SkipSecurity) {
        Write-Step "Scanning for vulnerable packages..."
        $vulnerabilityReport = dotnet list package --vulnerable --include-transitive 2>&1 | Out-String
        
        if ($vulnerabilityReport -match "has the following vulnerable packages") {
            Write-Failure "Vulnerable packages detected!"
            Write-Host $vulnerabilityReport -ForegroundColor Red
            exit 1
        } else {
            Write-Success "No vulnerable packages found"
        }
    } else {
        Write-Warning "Skipping security scan (--SkipSecurity)"
    }

    # Simulate version string generation (like CI)
    Write-Step "Validating version strings..."
    $branchName = git rev-parse --abbrev-ref HEAD
    $runNumber = Get-Random -Minimum 1 -Maximum 9999
    $version = "1.0.0-$($branchName -replace '[^a-zA-Z0-9-]', '-').$runNumber"
    $assemblyVersion = "1.0.0.$runNumber"
    
    Write-Host "  Version: $version" -ForegroundColor Gray
    Write-Host "  AssemblyVersion: $assemblyVersion" -ForegroundColor Gray

    # Test publish (like CI build-service job)
    if (!$Fast) {
        Write-Step "Testing publish (simulating CI)..."
        $tempOutput = Join-Path $env:TEMP "owlet-validate-$(New-Guid)"
        New-Item -ItemType Directory -Force -Path $tempOutput | Out-Null
        
        dotnet publish src/Owlet.Service/Owlet.Service.csproj `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output $tempOutput `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:PublishTrimmed=false `
            -p:Version=$version `
            -p:AssemblyVersion=$assemblyVersion `
            -p:FileVersion=$assemblyVersion `
            -p:InformationalVersion=$version `
            --verbosity quiet
        
        if ($LASTEXITCODE -ne 0) {
            Write-Failure "Publish failed"
            exit $LASTEXITCODE
        }
        
        # Cleanup
        Remove-Item -Path $tempOutput -Recurse -Force
        Write-Success "Publish test succeeded"
    }

    # Success!
    $elapsed = $stopwatch.Elapsed
    Write-Host "`n" + ("=" * 50) -ForegroundColor Green
    Write-Success "All validations passed! ✨"
    Write-Host "Time elapsed: $($elapsed.ToString('mm\:ss'))" -ForegroundColor Gray
    Write-Host ("=" * 50) -ForegroundColor Green
    Write-Host "`n💡 Your code is ready to push to GitHub`n" -ForegroundColor Cyan

} catch {
    Write-Failure "Validation failed with error:"
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
} finally {
    $stopwatch.Stop()
}
