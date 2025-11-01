#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Development environment setup script
.DESCRIPTION
    Sets up the development environment for Owlet including prerequisites,
    configuration, database, logging, and IDE configuration
.EXAMPLE
    .\scripts\dev-setup.ps1
#>

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

function Write-Warning-Message {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Install-Prerequisites {
    Write-Header "Checking Prerequisites"

    # Check if running as Administrator
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    if (-not $isAdmin) {
        Write-Warning-Message "Some setup steps may require Administrator privileges."
        Write-Host "Consider running this script as Administrator for full setup." -ForegroundColor Gray
    }

    # Check .NET 9 SDK
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -like "9.*") {
            Write-Step "✓ .NET 9 SDK installed: $dotnetVersion"
        }
        else {
            Write-Warning-Message ".NET 9 SDK not found (found: $dotnetVersion)"
            Write-Host "Please install .NET 9 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Gray
        }
    }
    catch {
        Write-Warning-Message ".NET SDK not found"
        Write-Host "Please install .NET 9 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Gray
    }

    # Install global tools
    Write-Step "Installing global .NET tools..."

    $tools = @(
        @{ Name = "dotnet-ef"; Package = "dotnet-ef" },
        @{ Name = "dotnet-format"; Package = "dotnet-format" },
        @{ Name = "dotnet-outdated-tool"; Package = "dotnet-outdated-tool" }
    )

    foreach ($tool in $tools) {
        try {
            & $tool.Name --version 2>$null | Out-Null
            Write-Step "✓ $($tool.Name) already installed"
        }
        catch {
            Write-Step "Installing $($tool.Name)..."
            dotnet tool install --global $tool.Package 2>&1 | Out-Null
            Write-Step "✓ $($tool.Name) installed"
        }
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
    else {
        Write-Step "✓ Database directory already exists"
    }

    Write-Host "  Database will be created automatically on first run" -ForegroundColor Gray
}

function Setup-Logs {
    Write-Header "Setting up Development Logging"

    # Create log directory
    $logDir = "C:\temp\owlet\logs"
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Force -Path $logDir | Out-Null
        Write-Step "✓ Log directory created: $logDir"
    }
    else {
        Write-Step "✓ Log directory already exists"
    }

    # Set permissions for log directory
    try {
        $acl = Get-Acl $logDir
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Everyone", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.SetAccessRule($accessRule)
        Set-Acl -Path $logDir -AclObject $acl
        Write-Step "✓ Log directory permissions configured"
    }
    catch {
        Write-Warning-Message "Could not set log directory permissions: $($_.Exception.Message)"
    }
}

function Setup-IDE {
    Write-Header "Setting up IDE Configuration"

    # Create .vscode directory
    $vsCodeDir = ".vscode"
    if (-not (Test-Path $vsCodeDir)) {
        New-Item -ItemType Directory -Force -Path $vsCodeDir | Out-Null
    }

    # VS Code extensions recommendations
    $vsCodeExtensions = @{
        "recommendations" = @(
            "ms-dotnettools.csharp",
            "ms-dotnettools.vscode-dotnet-runtime",
            "ms-vscode.powershell",
            "ms-azuretools.vscode-azureresourcegroups"
        )
    }

    $extensionsPath = "$vsCodeDir/extensions.json"
    if (-not (Test-Path $extensionsPath)) {
        $vsCodeExtensions | ConvertTo-Json -Depth 10 | Out-File -FilePath $extensionsPath -Encoding UTF8
        Write-Step "✓ VS Code extensions recommendations created"
    }
    else {
        Write-Step "✓ VS Code extensions recommendations already exist"
    }

    # Launch configuration
    $launchConfig = @{
        "version"        = "0.2.0"
        "configurations" = @(
            @{
                "name"              = "Launch Owlet Service"
                "type"              = "coreclr"
                "request"           = "launch"
                "program"           = "`${workspaceFolder}/src/Owlet.Service/bin/Debug/net9.0/Owlet.Service.exe"
                "args"              = @()
                "cwd"               = "`${workspaceFolder}/src/Owlet.Service"
                "env"               = @{
                    "ASPNETCORE_ENVIRONMENT" = "Development"
                }
                "serverReadyAction" = @{
                    "action"  = "openExternally"
                    "pattern" = "Now listening on:\\s+(http://localhost:\\d+)"
                }
            },
            @{
                "name"              = "Launch Aspire Host"
                "type"              = "coreclr"
                "request"           = "launch"
                "program"           = "`${workspaceFolder}/src/Owlet.AppHost/bin/Debug/net9.0/Owlet.AppHost.exe"
                "args"              = @()
                "cwd"               = "`${workspaceFolder}/src/Owlet.AppHost"
                "env"               = @{
                    "ASPNETCORE_ENVIRONMENT" = "Development"
                }
                "serverReadyAction" = @{
                    "action"  = "openExternally"
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
    else {
        Write-Step "✓ VS Code launch configuration already exists"
    }
}

function Main {
    Write-Header "Owlet Development Environment Setup"

    Install-Prerequisites
    Setup-Database
    Setup-Logs
    Setup-IDE

    Write-Header "Setup Complete!"
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Run: .\scripts\build.ps1 -Configuration Debug" -ForegroundColor White
    Write-Host "  2. Start debugging in VS Code (F5)" -ForegroundColor White
    Write-Host "  3. Open browser to http://localhost:5556" -ForegroundColor White
    Write-Host ""
    Write-Host "Development paths:" -ForegroundColor Cyan
    Write-Host "  Database: C:\temp\owlet\data\owlet-dev.db" -ForegroundColor Gray
    Write-Host "  Logs:     C:\temp\owlet\logs\" -ForegroundColor Gray
    Write-Host ""
}

# Execute main function
Main
