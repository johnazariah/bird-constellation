#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Build WiX installer for Owlet
.DESCRIPTION
    Builds the MSI installer using WiX Toolset with proper configuration
.PARAMETER Version
    Product version for the installer
.PARAMETER SourceDir
    Directory containing the service binaries
.PARAMETER OutputDir
    Directory for installer output
.EXAMPLE
    .\build-installer.ps1 -Version "1.0.0" -SourceDir "..\..\artifacts\service-package"
#>

param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$SourceDir,

    [Parameter()]
    [string]$OutputDir = "..\..\artifacts"
)

$ErrorActionPreference = "Stop"

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host ">> $Message" -ForegroundColor Green
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"

    # Check WiX toolset
    try {
        $wixVersion = & wix --version 2>&1
        Write-Step "✓ WiX Toolset version: $wixVersion"
    }
    catch {
        Write-Host "WiX Toolset not found. Attempting to install..." -ForegroundColor Yellow
        try {
            dotnet tool install --global wix --version 4.0.4
            Write-Step "✓ WiX Toolset installed"
        }
        catch {
            throw "Failed to install WiX Toolset. Please install manually: dotnet tool install --global wix"
        }
    }

    # Check source directory
    if (-not (Test-Path $SourceDir)) {
        throw "Source directory not found: $SourceDir"
    }

    $serviceExe = Join-Path $SourceDir "Owlet.Service.exe"
    if (-not (Test-Path $serviceExe)) {
        throw "Service executable not found: $serviceExe"
    }

    Write-Step "✓ Source files validated"
}

function Build-Installer {
    Write-Header "Building MSI Installer"

    $projectFile = "Owlet.Installer.wixproj"
    $outputPath = Join-Path $OutputDir "OwletInstaller-$Version.msi"

    # Ensure output directory exists
    $outputDirPath = Split-Path $outputPath
    if (-not (Test-Path $outputDirPath)) {
        New-Item -ItemType Directory -Force -Path $outputDirPath | Out-Null
    }

    # Build arguments
    $buildArgs = @(
        "build"
        $projectFile
        "-d", "SourceDir=$SourceDir"
        "-d", "ProductVersion=$Version"
        "-out", $outputPath
    )

    Write-Step "Building installer with WiX..."
    Write-Host "Command: wix $($buildArgs -join ' ')" -ForegroundColor Gray

    & wix @buildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "WiX build failed with exit code $LASTEXITCODE"
    }

    if (-not (Test-Path $outputPath)) {
        throw "Installer build failed - output file not created"
    }

    $fileInfo = Get-Item $outputPath
    Write-Step "✓ Installer built successfully: $($fileInfo.Name) ($([math]::Round($fileInfo.Length / 1MB, 2)) MB)"

    return $outputPath
}

function Main {
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        Test-Prerequisites
        $installerPath = Build-Installer

        $stopwatch.Stop()

        Write-Header "Build Completed Successfully"
        Write-Host "Installer: $installerPath" -ForegroundColor Green
        Write-Host "Build time: $($stopwatch.Elapsed.ToString('mm\:ss'))" -ForegroundColor Green
    }
    catch {
        Write-Host ""
        Write-Host "Build Failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Execute main function
Main
