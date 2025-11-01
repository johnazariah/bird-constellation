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
.EXAMPLE
    .\scripts\build.ps1 -Configuration Debug -Pack
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
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host ">> $Message" -ForegroundColor Green
}

function Write-Error-Message {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"

    # Check .NET version
    try {
        $dotnetVersion = dotnet --version
        Write-Step "✓ .NET version: $dotnetVersion"
    }
    catch {
        throw ".NET SDK not found. Please install .NET 9 SDK from https://dotnet.microsoft.com/download"
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
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Step "✓ Solution built successfully"
}

function Invoke-Tests {
    if ($Target -eq "All" -or $Target -eq "Tests") {
        Write-Header "Running Tests"

        # Unit tests
        Write-Step "Running unit tests..."
        $testProjects = @(
            "tests/Owlet.Core.Tests"
            "tests/Owlet.Api.Tests"
            "tests/Owlet.Infrastructure.Tests"
        )

        foreach ($project in $testProjects) {
            Write-Host "  Testing $project..." -ForegroundColor Gray
            dotnet test $project --configuration $Configuration --no-build --logger "console;verbosity=minimal"
            if ($LASTEXITCODE -ne 0) {
                throw "Tests failed for $project"
            }
        }

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

            if ($LASTEXITCODE -ne 0) {
                throw "Service publish failed"
            }
            Write-Step "✓ Service published to $OutputPath/service"
        }

        if ($Target -eq "All" -or $Target -eq "Aspire") {
            Write-Step "Publishing Aspire Host..."
            dotnet publish $AspireProject `
                --configuration $Configuration `
                --output "$OutputPath/aspire" `
                --verbosity minimal

            if ($LASTEXITCODE -ne 0) {
                throw "Aspire publish failed"
            }
            Write-Step "✓ Aspire Host published to $OutputPath/aspire"
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
            Write-Host ""
            Write-Host "Artifacts available in: $OutputPath" -ForegroundColor Yellow
            Write-Host "  Service: $OutputPath/service/Owlet.Service.exe" -ForegroundColor Gray
            if ($Target -eq "All" -or $Target -eq "Aspire") {
                Write-Host "  Aspire:  $OutputPath/aspire/" -ForegroundColor Gray
            }
        }
    }
    catch {
        Write-Host ""
        Write-Error-Message "Build Failed: $($_.Exception.Message)"
        exit 1
    }
}

# Execute main function
Main
