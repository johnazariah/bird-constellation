#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Owlet development task runner
.DESCRIPTION
    Provides common development tasks in a simple command interface.
.EXAMPLE
    .\run.ps1 validate
    .\run.ps1 test
    .\run.ps1 build
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet(
        'validate',    # Run full local CI validation
        'validate-fast', # Run fast validation (skip publish test)
        'build',       # Build solution
        'test',        # Run tests
        'security',    # Run security scan
        'clean',       # Clean build artifacts
        'restore',     # Restore dependencies
        'aspire',      # Run Aspire AppHost
        'service',     # Run pure service
        'help'         # Show help
    )]
    [string]$Task = 'help'
)

$ErrorActionPreference = "Stop"

function Show-Help {
    Write-Host "`n🦉 Owlet Development Tasks" -ForegroundColor Magenta
    Write-Host "========================`n" -ForegroundColor Magenta
    Write-Host "Usage: .\run.ps1 <task>`n"
    Write-Host "Available tasks:" -ForegroundColor Cyan
    Write-Host "  validate       Run full local CI validation (recommended before push)" -ForegroundColor White
    Write-Host "  validate-fast  Quick validation (skip publish test)" -ForegroundColor White
    Write-Host "  build          Build solution in Release mode" -ForegroundColor White
    Write-Host "  test           Run all tests" -ForegroundColor White
    Write-Host "  security       Run security vulnerability scan" -ForegroundColor White
    Write-Host "  clean          Clean build artifacts" -ForegroundColor White
    Write-Host "  restore        Restore NuGet dependencies" -ForegroundColor White
    Write-Host "  aspire         Run Aspire AppHost (development mode)" -ForegroundColor White
    Write-Host "  service        Run pure Windows Service" -ForegroundColor White
    Write-Host "  help           Show this help message" -ForegroundColor White
    Write-Host ""
}

switch ($Task) {
    'validate' {
        & "$PSScriptRoot/scripts/validate-build.ps1"
    }
    'validate-fast' {
        & "$PSScriptRoot/scripts/validate-build.ps1" -Fast
    }
    'build' {
        dotnet build --configuration Release
    }
    'test' {
        dotnet test --configuration Release
    }
    'security' {
        Write-Host "`n🔒 Scanning for vulnerabilities..." -ForegroundColor Cyan
        dotnet list package --vulnerable --include-transitive
    }
    'clean' {
        dotnet clean --configuration Release
        Write-Host "✅ Cleaned" -ForegroundColor Green
    }
    'restore' {
        dotnet restore
        Write-Host "✅ Restored" -ForegroundColor Green
    }
    'aspire' {
        Write-Host "`n🚀 Starting Aspire AppHost..." -ForegroundColor Cyan
        dotnet run --project src/Owlet.AppHost
    }
    'service' {
        Write-Host "`n🦉 Starting Owlet Service..." -ForegroundColor Cyan
        dotnet run --project src/Owlet.Service
    }
    'help' {
        Show-Help
    }
}
