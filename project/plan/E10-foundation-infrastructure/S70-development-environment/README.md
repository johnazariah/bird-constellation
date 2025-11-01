# E10 S70: Development Environment

**Story:** Complete Aspire integration, local development setup, and debugging configuration for streamlined developer experience  
**Priority:** High  
**Effort:** 16 hours  
**Status:** Not Started  
**Dependencies:** S30 (Core Infrastructure)  

## Objective

This story creates a comprehensive development environment using .NET Aspire orchestration, local development tools, and debugging configurations. It enables developers to run the complete Owlet ecosystem locally with hot reload, detailed debugging, and integrated development tools while maintaining the production Windows service deployment model.

The development environment provides the foundation for the dual architecture strategy: simple production deployment via Windows service and rich development experience via Aspire orchestration. It includes container support, service discovery, telemetry integration, and comprehensive debugging capabilities.

## Business Context

**Revenue Impact:** ₹0 direct revenue (accelerates development velocity and reduces development costs)  
**User Impact:** Developers and contributors - enables efficient local development and debugging  
**Compliance Requirements:** Development environment isolation and secure credential management

## Development Environment Architecture

### 1. Aspire App Host Configuration

Central orchestration for all Owlet services and dependencies in development.

**`src/Owlet.AppHost/Program.cs`:**

```csharp
using Microsoft.Extensions.Hosting;
using Owlet.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

// Development-specific configuration
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);
builder.Configuration.AddUserSecrets<Program>();

// Database - SQLite for development, PostgreSQL for advanced scenarios
var database = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("owletdb");

// Alternative SQLite configuration for simpler development
var sqliteDb = builder.AddConnectionString("DefaultConnection", 
    "Data Source=../../../data/owlet-dev.db;Cache=Shared");

// Redis for caching (optional in development)
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

// Owlet Core API Service
var apiService = builder.AddProject<Projects.Owlet_Api>("owlet-api")
    .WithReference(database)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithHttpEndpoint(port: 5555, name: "http")
    .WithHttpsEndpoint(port: 5556, name: "https");

// Owlet Indexer Service (Background processing)
var indexerService = builder.AddProject<Projects.Owlet_Indexer>("owlet-indexer")
    .WithReference(database)
    .WithReference(redis)
    .WithReference(apiService)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Owlet Extractors Service (Content processing)
var extractorService = builder.AddProject<Projects.Owlet_Extractors>("owlet-extractors")
    .WithReference(database)
    .WithReference(redis)
    .WithReference(apiService)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Development Tools
var trayApp = builder.AddProject<Projects.Owlet_TrayApp>("owlet-tray")
    .WithReference(apiService)
    .WithEnvironment("OWLET_API_URL", "https://localhost:5556");

var diagnostics = builder.AddProject<Projects.Owlet_Diagnostics>("owlet-diagnostics")
    .WithReference(apiService)
    .WithReference(database);

// Web UI (if available as separate project)
if (builder.Environment.IsDevelopment())
{
    var webUI = builder.AddNpmApp("owlet-ui", "../ui")
        .WithReference(apiService)
        .WithHttpEndpoint(port: 3000, env: "PORT")
        .WithEnvironment("VITE_API_URL", apiService.GetEndpoint("https"));
        
    // Proxy configuration for development
    webUI.WithEnvironment("VITE_PROXY_TARGET", apiService.GetEndpoint("https"));
}

// Development-specific extensions
if (builder.Environment.IsDevelopment())
{
    // Enable detailed logging
    builder.Services.Configure<LoggerConfiguration>(config =>
    {
        config.MinimumLevel.Debug();
        config.WriteTo.Console();
        config.WriteTo.Debug();
    });

    // Add development middleware
    builder.Services.AddDeveloperExceptionPage();
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

var app = builder.Build();

await app.RunAsync();
```

### 2. Service Defaults for Development

Shared configuration and services for consistent development experience.

**`src/Owlet.ServiceDefaults/Extensions.cs`:**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;

namespace Owlet.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.ConfigureSerilog();
        
        // Development-specific configuration
        if (builder.Environment.IsDevelopment())
        {
            builder.AddDevelopmentServices();
        }

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSqlClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Development-specific exporters
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddConsoleExporter())
                .WithMetrics(metrics => metrics.AddConsoleExporter());
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static IHostApplicationBuilder ConfigureSerilog(this IHostApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

            // Development-specific enrichment
            if (context.HostingEnvironment.IsDevelopment())
            {
                loggerConfig
                    .WriteTo.Console(outputTemplate: 
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {NewLine}{Exception}")
                    .WriteTo.Debug()
                    .MinimumLevel.Debug();
            }
        });

        return builder;
    }

    public static IHostApplicationBuilder AddDevelopmentServices(this IHostApplicationBuilder builder)
    {
        // Enhanced development services
        builder.Services.AddDeveloperExceptionPage();
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Development-specific HTTP client configuration
        builder.Services.AddHttpClient("development")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

        // Development middleware configuration
        builder.Services.Configure<DevelopmentConfiguration>(
            builder.Configuration.GetSection("Development"));

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health check endpoints
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        // Development-specific endpoints
        if (app.Environment.IsDevelopment())
        {
            app.MapDevelopmentEndpoints();
        }

        return app;
    }

    public static WebApplication MapDevelopmentEndpoints(this WebApplication app)
    {
        // Configuration endpoint for debugging
        app.MapGet("/dev/config", (IConfiguration config) =>
        {
            var configData = new Dictionary<string, object>();
            
            foreach (var kvp in config.AsEnumerable())
            {
                // Mask sensitive values
                var value = kvp.Value;
                if (kvp.Key.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains("Key", StringComparison.OrdinalIgnoreCase))
                {
                    value = "***MASKED***";
                }
                
                configData[kvp.Key] = value ?? "";
            }
            
            return Results.Json(configData);
        })
        .WithTags("Development")
        .ExcludeFromDescription();

        // Environment info endpoint
        app.MapGet("/dev/environment", (IHostEnvironment env) =>
        {
            return Results.Json(new
            {
                ApplicationName = env.ApplicationName,
                EnvironmentName = env.EnvironmentName,
                ContentRootPath = env.ContentRootPath,
                WebRootPath = env.WebRootPath,
                IsDevelopment = env.IsDevelopment(),
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                UserName = Environment.UserName,
                WorkingSet = Environment.WorkingSet,
                Version = Environment.Version.ToString()
            });
        })
        .WithTags("Development")
        .ExcludeFromDescription();

        // Service dependencies endpoint
        app.MapGet("/dev/dependencies", (IServiceProvider services) =>
        {
            var serviceDescriptors = services.GetService<IServiceCollection>();
            
            var dependencies = serviceDescriptors?
                .Select(sd => new
                {
                    ServiceType = sd.ServiceType.Name,
                    ImplementationType = sd.ImplementationType?.Name,
                    Lifetime = sd.Lifetime.ToString()
                })
                .OrderBy(d => d.ServiceType)
                .ToList() ?? new List<object>();
            
            return Results.Json(dependencies);
        })
        .WithTags("Development")
        .ExcludeFromDescription();

        return app;
    }
}

public class DevelopmentConfiguration
{
    public bool EnableDetailedLogging { get; set; } = true;
    public bool EnableHotReload { get; set; } = true;
    public bool EnableBrowserRefresh { get; set; } = true;
    public string[]? CorsOrigins { get; set; }
    public bool TrustDevelopmentCertificates { get; set; } = true;
}
```

### 3. Launch Configurations for Debugging

Comprehensive Visual Studio Code and Visual Studio launch configurations.

**`.vscode/launch.json`:**

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Owlet AppHost (Aspire)",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/Owlet.AppHost/bin/Debug/net9.0/Owlet.AppHost.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/Owlet.AppHost",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "DOTNET_ENVIRONMENT": "Development",
                "ASPNETCORE_URLS": "https://localhost:7777;http://localhost:7778"
            },
            "sourceFileMap": {
                "/Views": "${workspaceFolder}/Views"
            },
            "preLaunchTask": "build-apphost",
            "postDebugTask": "cleanup-development",
            "console": "integratedTerminal",
            "internalConsoleOptions": "openOnSessionStart"
        },
        {
            "name": "Owlet Service (Production Mode)",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/Owlet.Service/bin/Debug/net9.0/Owlet.Service.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/Owlet.Service",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "DOTNET_ENVIRONMENT": "Development",
                "ASPNETCORE_URLS": "https://localhost:5556;http://localhost:5555"
            },
            "preLaunchTask": "build-service",
            "console": "integratedTerminal"
        },
        {
            "name": "Owlet API (Standalone)",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/Owlet.Api/bin/Debug/net9.0/Owlet.Api.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/Owlet.Api",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "ASPNETCORE_URLS": "https://localhost:5556;http://localhost:5555",
                "ConnectionStrings__DefaultConnection": "Data Source=../../../data/owlet-dev.db"
            },
            "preLaunchTask": "build-api",
            "console": "integratedTerminal"
        },
        {
            "name": "Owlet Diagnostics",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/tools/Owlet.Diagnostics/bin/Debug/net9.0/Owlet.Diagnostics.dll",
            "args": ["status", "--detailed"],
            "cwd": "${workspaceFolder}/tools/Owlet.Diagnostics",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            },
            "preLaunchTask": "build-diagnostics",
            "console": "integratedTerminal"
        },
        {
            "name": "Owlet Tray App",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/tools/Owlet.TrayApp/bin/Debug/net9.0-windows/Owlet.TrayApp.dll",
            "args": [],
            "cwd": "${workspaceFolder}/tools/Owlet.TrayApp",
            "stopAtEntry": false,
            "env": {
                "DOTNET_ENVIRONMENT": "Development",
                "OWLET_API_URL": "https://localhost:5556"
            },
            "preLaunchTask": "build-trayapp",
            "console": "none"
        },
        {
            "name": "Attach to Owlet Service",
            "type": "coreclr",
            "request": "attach",
            "processName": "Owlet.Service"
        }
    ],
    "compounds": [
        {
            "name": "Full Owlet Development",
            "configurations": [
                "Owlet AppHost (Aspire)"
            ],
            "stopAll": true
        },
        {
            "name": "Service + Diagnostics",
            "configurations": [
                "Owlet Service (Production Mode)",
                "Owlet Diagnostics"
            ]
        }
    ]
}
```

### 4. Development Tasks and Build Scripts

Automated tasks for building and running development environment.

**`.vscode/tasks.json`:**

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build-apphost",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "build",
                "${workspaceFolder}/src/Owlet.AppHost/Owlet.AppHost.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary;ForceNoAlign"
            ],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "silent",
                "focus": false,
                "panel": "shared",
                "showReuseMessage": true,
                "clear": false
            },
            "problemMatcher": "$msCompile"
        },
        {
            "label": "build-service",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "build",
                "${workspaceFolder}/src/Owlet.Service/Owlet.Service.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary;ForceNoAlign"
            ],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "silent",
                "focus": false,
                "panel": "shared"
            },
            "problemMatcher": "$msCompile"
        },
        {
            "label": "build-api",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "build",
                "${workspaceFolder}/src/Owlet.Api/Owlet.Api.csproj"
            ],
            "group": "build",
            "problemMatcher": "$msCompile"
        },
        {
            "label": "build-diagnostics",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "build",
                "${workspaceFolder}/tools/Owlet.Diagnostics/Owlet.Diagnostics.csproj"
            ],
            "group": "build",
            "problemMatcher": "$msCompile"
        },
        {
            "label": "build-trayapp",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "build",
                "${workspaceFolder}/tools/Owlet.TrayApp/Owlet.TrayApp.csproj"
            ],
            "group": "build",
            "problemMatcher": "$msCompile"
        },
        {
            "label": "restore-all",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "restore",
                "${workspaceFolder}/Owlet.sln"
            ],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "shared"
            },
            "problemMatcher": []
        },
        {
            "label": "clean-all",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "clean",
                "${workspaceFolder}/Owlet.sln"
            ],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": false,
                "panel": "shared"
            }
        },
        {
            "label": "setup-development-environment",
            "type": "shell",
            "command": "pwsh",
            "args": [
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                "${workspaceFolder}/scripts/setup-development.ps1"
            ],
            "group": "build",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": true,
                "panel": "new"
            },
            "problemMatcher": []
        },
        {
            "label": "run-tests-all",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "test",
                "${workspaceFolder}/Owlet.sln",
                "--logger:console;verbosity=detailed",
                "--collect:\"XPlat Code Coverage\""
            ],
            "group": "test",
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": true,
                "panel": "new"
            },
            "problemMatcher": "$msCompile"
        },
        {
            "label": "cleanup-development",
            "type": "shell",
            "command": "pwsh",
            "args": [
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                "${workspaceFolder}/scripts/cleanup-development.ps1"
            ],
            "presentation": {
                "echo": true,
                "reveal": "silent",
                "focus": false,
                "panel": "shared"
            },
            "problemMatcher": []
        },
        {
            "label": "generate-certificates",
            "type": "shell",
            "command": "dotnet",
            "args": [
                "dev-certs",
                "https",
                "--trust"
            ],
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": true,
                "panel": "new"
            },
            "problemMatcher": []
        }
    ]
}
```

### 5. Development Setup Script

Automated development environment configuration and dependency setup.

**`scripts/setup-development.ps1`:**

```powershell
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Setup development environment for Owlet
.DESCRIPTION
    Configures the complete development environment including certificates, databases, and development tools
.PARAMETER Force
    Force recreation of existing configuration
.PARAMETER SkipCertificates
    Skip HTTPS certificate generation
.PARAMETER DatabaseType
    Database type to configure (SQLite, PostgreSQL)
.EXAMPLE
    .\setup-development.ps1 -Force
#>

param(
    [Parameter()]
    [switch]$Force,
    
    [Parameter()]
    [switch]$SkipCertificates,
    
    [Parameter()]
    [ValidateSet("SQLite", "PostgreSQL")]
    [string]$DatabaseType = "SQLite"
)

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

function Write-Warning {
    param([string]$Message)
    Write-Host "WARNING: $Message" -ForegroundColor Yellow
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Step "✓ .NET SDK version: $dotnetVersion"
        
        if ([Version]$dotnetVersion -lt [Version]"9.0.0") {
            throw ".NET 9.0 or later is required"
        }
    } catch {
        throw ".NET SDK not found or version too old. Please install .NET 9.0 SDK"
    }
    
    # Check Aspire workload
    try {
        $workloads = dotnet workload list
        if ($workloads -notcontains "aspire") {
            Write-Step "Installing .NET Aspire workload..."
            dotnet workload install aspire
        }
        Write-Step "✓ .NET Aspire workload installed"
    } catch {
        Write-Warning "Could not verify Aspire workload: $($_.Exception.Message)"
    }
    
    # Check PowerShell version
    $psVersion = $PSVersionTable.PSVersion
    Write-Step "✓ PowerShell version: $psVersion"
    
    # Check Git
    try {
        $gitVersion = git --version
        Write-Step "✓ Git: $gitVersion"
    } catch {
        Write-Warning "Git not found - version control features may not work"
    }
}

function Setup-Directories {
    Write-Header "Setting Up Directories"
    
    $directories = @(
        "data",
        "logs",
        "temp",
        "artifacts",
        "artifacts/service",
        "artifacts/installer",
        "certificates"
    )
    
    foreach ($dir in $directories) {
        $fullPath = Join-Path $PSScriptRoot ".." $dir
        if (-not (Test-Path $fullPath) -or $Force) {
            New-Item -ItemType Directory -Force -Path $fullPath | Out-Null
            Write-Step "✓ Created directory: $dir"
        } else {
            Write-Step "✓ Directory exists: $dir"
        }
    }
}

function Setup-Certificates {
    if ($SkipCertificates) {
        Write-Step "Skipping certificate setup (requested)"
        return
    }
    
    Write-Header "Setting Up Development Certificates"
    
    try {
        # Generate HTTPS development certificate
        Write-Step "Generating development HTTPS certificate..."
        dotnet dev-certs https --clean
        dotnet dev-certs https --trust
        
        Write-Step "✓ HTTPS development certificate configured"
        
        # Export certificate for other tools
        $certPath = Join-Path $PSScriptRoot ".." "certificates" "aspnetcore-https-dev.pfx"
        dotnet dev-certs https --export-path $certPath --format Pfx --no-password
        
        Write-Step "✓ Certificate exported to: certificates/aspnetcore-https-dev.pfx"
    } catch {
        Write-Warning "Certificate setup failed: $($_.Exception.Message)"
        Write-Warning "You may need to run with administrator privileges"
    }
}

function Setup-Database {
    Write-Header "Setting Up Development Database"
    
    $dataDir = Join-Path $PSScriptRoot ".." "data"
    
    if ($DatabaseType -eq "SQLite") {
        Write-Step "Configuring SQLite database..."
        
        $dbPath = Join-Path $dataDir "owlet-dev.db"
        
        # Create connection string configuration
        $connectionString = "Data Source=$dbPath;Cache=Shared"
        
        # Create appsettings.Development.json if it doesn't exist
        $configPath = Join-Path $PSScriptRoot ".." "src" "Owlet.Api" "appsettings.Development.json"
        
        $config = @{
            "ConnectionStrings" = @{
                "DefaultConnection" = $connectionString
            }
            "Logging" = @{
                "LogLevel" = @{
                    "Default" = "Debug"
                    "Microsoft.AspNetCore" = "Information"
                    "Microsoft.EntityFrameworkCore" = "Information"
                }
            }
            "Development" = @{
                "EnableDetailedLogging" = $true
                "EnableHotReload" = $true
                "EnableBrowserRefresh" = $true
                "TrustDevelopmentCertificates" = $true
            }
        }
        
        $configJson = $config | ConvertTo-Json -Depth 10
        Set-Content -Path $configPath -Value $configJson -Force
        
        Write-Step "✓ SQLite database configuration created"
        Write-Step "Database path: $dbPath"
    }
    elseif ($DatabaseType -eq "PostgreSQL") {
        Write-Step "PostgreSQL will be configured via Aspire orchestration"
        Write-Step "Database will be available at: postgres://localhost:5432/owletdb"
    }
}

function Setup-UserSecrets {
    Write-Header "Setting Up User Secrets"
    
    $projects = @(
        "src/Owlet.AppHost",
        "src/Owlet.Api",
        "src/Owlet.Service",
        "tools/Owlet.Diagnostics"
    )
    
    foreach ($project in $projects) {
        $projectPath = Join-Path $PSScriptRoot ".." $project
        if (Test-Path $projectPath) {
            Write-Step "Initializing user secrets for $project..."
            
            Push-Location $projectPath
            try {
                dotnet user-secrets init --force
                
                # Set common development secrets
                dotnet user-secrets set "Development:EnableDetailedLogging" "true"
                dotnet user-secrets set "Development:DatabaseType" $DatabaseType
                
                if ($DatabaseType -eq "PostgreSQL") {
                    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=owletdb;Username=postgres;Password=devpassword"
                }
                
                Write-Step "✓ User secrets configured for $project"
            }
            finally {
                Pop-Location
            }
        }
    }
}

function Setup-VSCodeConfiguration {
    Write-Header "Setting Up VS Code Configuration"
    
    $vscodeDir = Join-Path $PSScriptRoot ".." ".vscode"
    
    # VS Code settings
    $settings = @{
        "dotnet.completion.showCompletionItemsFromUnimportedNamespaces" = $true
        "dotnet.server.useOmnisharp" = $false
        "dotnet.backgroundAnalysis.analyzerDiagnosticsScope" = "fullSolution"
        "omnisharp.enableAsyncCompletion" = $true
        "omnisharp.enableImportCompletion" = $true
        "omnisharp.enableRoslynAnalyzers" = $true
        "files.exclude" = @{
            "**/bin" = $true
            "**/obj" = $true
            "**/.vs" = $true
        }
        "search.exclude" = @{
            "**/bin" = $true
            "**/obj" = $true
            "**/node_modules" = $true
        }
        "terminal.integrated.defaultProfile.windows" = "PowerShell"
        "terminal.integrated.profiles.windows" = @{
            "PowerShell" = @{
                "source" = "PowerShell"
                "icon" = "terminal-powershell"
            }
        }
    }
    
    $settingsPath = Join-Path $vscodeDir "settings.json"
    $settingsJson = $settings | ConvertTo-Json -Depth 10
    Set-Content -Path $settingsPath -Value $settingsJson -Force
    
    Write-Step "✓ VS Code settings configured"
    
    # VS Code extensions recommendations
    $extensions = @{
        "recommendations" = @(
            "ms-dotnettools.csharp",
            "ms-dotnettools.csdevkit",
            "ms-dotnettools.vscodeintellicode-csharp",
            "ms-vscode.powershell",
            "ms-vscode.vscode-json",
            "redhat.vscode-yaml",
            "esbenp.prettier-vscode",
            "ms-vscode.vscode-typescript-next",
            "bradlc.vscode-tailwindcss"
        )
    }
    
    $extensionsPath = Join-Path $vscodeDir "extensions.json"
    $extensionsJson = $extensions | ConvertTo-Json -Depth 10
    Set-Content -Path $extensionsPath -Value $extensionsJson -Force
    
    Write-Step "✓ VS Code extensions configured"
}

function Restore-And-Build {
    Write-Header "Restoring and Building Solution"
    
    $solutionPath = Join-Path $PSScriptRoot ".." "Owlet.sln"
    
    Write-Step "Restoring NuGet packages..."
    dotnet restore $solutionPath
    
    Write-Step "Building solution..."
    dotnet build $solutionPath --configuration Debug --no-restore
    
    Write-Step "✓ Solution built successfully"
}

function Test-Environment {
    Write-Header "Testing Development Environment"
    
    try {
        # Test basic compilation
        Write-Step "Testing compilation..."
        $solutionPath = Join-Path $PSScriptRoot ".." "Owlet.sln"
        dotnet build $solutionPath --configuration Debug --verbosity quiet
        Write-Step "✓ Compilation successful"
        
        # Test certificate
        if (-not $SkipCertificates) {
            Write-Step "Testing HTTPS certificate..."
            $certInfo = dotnet dev-certs https --check --trust
            Write-Step "✓ HTTPS certificate valid"
        }
        
        # Test database connection (SQLite only)
        if ($DatabaseType -eq "SQLite") {
            Write-Step "Testing database connection..."
            $dbPath = Join-Path $PSScriptRoot ".." "data" "owlet-dev.db"
            # Basic test - just check if we can create the file
            if (Test-Path $dbPath) {
                Remove-Item $dbPath -Force
            }
            "" | Out-File -FilePath $dbPath
            Remove-Item $dbPath -Force
            Write-Step "✓ Database path accessible"
        }
        
        Write-Step "✓ Development environment test completed successfully"
    } catch {
        Write-Warning "Environment test failed: $($_.Exception.Message)"
    }
}

function Show-Summary {
    Write-Header "Development Environment Setup Complete"
    
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Green
    Write-Host "1. Open VS Code: code ." -ForegroundColor White
    Write-Host "2. Install recommended extensions (VS Code will prompt)" -ForegroundColor White
    Write-Host "3. Run development environment:" -ForegroundColor White
    Write-Host "   dotnet run --project src/Owlet.AppHost" -ForegroundColor Cyan
    Write-Host "4. Open Aspire dashboard: https://localhost:7777" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Available Commands:" -ForegroundColor Green
    Write-Host "• Build solution: dotnet build" -ForegroundColor White
    Write-Host "• Run tests: dotnet test" -ForegroundColor White
    Write-Host "• Run service standalone: dotnet run --project src/Owlet.Service" -ForegroundColor White
    Write-Host "• Run diagnostics: dotnet run --project tools/Owlet.Diagnostics" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Database Configuration:" -ForegroundColor Green
    Write-Host "• Type: $DatabaseType" -ForegroundColor White
    if ($DatabaseType -eq "SQLite") {
        $dbPath = Join-Path $PSScriptRoot ".." "data" "owlet-dev.db"
        Write-Host "• Path: $dbPath" -ForegroundColor White
    } else {
        Write-Host "• Connection: postgres://localhost:5432/owletdb" -ForegroundColor White
    }
    Write-Host ""
    
    Write-Host "Troubleshooting:" -ForegroundColor Green
    Write-Host "• Check logs in: logs/" -ForegroundColor White
    Write-Host "• Run diagnostics: ./scripts/diagnose-environment.ps1" -ForegroundColor White
    Write-Host "• Reset environment: ./scripts/setup-development.ps1 -Force" -ForegroundColor White
}

# Main execution
function Main {
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        Test-Prerequisites
        Setup-Directories
        Setup-Certificates
        Setup-Database
        Setup-UserSecrets
        Setup-VSCodeConfiguration
        Restore-And-Build
        Test-Environment
        
        $stopwatch.Stop()
        
        Show-Summary
        Write-Host "Setup completed in $($stopwatch.Elapsed.ToString('mm\:ss'))" -ForegroundColor Green
    }
    catch {
        Write-Host ""
        Write-Host "Setup Failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please check the error above and retry with appropriate permissions" -ForegroundColor Red
        exit 1
    }
}

# Execute main function
Main
```

### 6. Development Cleanup Script

Automated cleanup for development environment reset and maintenance.

**`scripts/cleanup-development.ps1`:**

```powershell
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Cleanup development environment
.DESCRIPTION
    Removes temporary files, logs, and development artifacts
.PARAMETER Full
    Perform full cleanup including databases and certificates
.PARAMETER Logs
    Only clean log files
.EXAMPLE
    .\cleanup-development.ps1 -Full
#>

param(
    [Parameter()]
    [switch]$Full,
    
    [Parameter()]
    [switch]$Logs
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ">> $Message" -ForegroundColor Green
}

function Cleanup-BuildArtifacts {
    Write-Step "Cleaning build artifacts..."
    
    $patterns = @("bin", "obj")
    
    foreach ($pattern in $patterns) {
        Get-ChildItem -Path (Join-Path $PSScriptRoot "..") -Recurse -Directory -Name $pattern | ForEach-Object {
            $fullPath = Join-Path $PSScriptRoot ".." $_
            if (Test-Path $fullPath) {
                Remove-Item -Recurse -Force $fullPath
                Write-Host "  Removed: $_" -ForegroundColor Gray
            }
        }
    }
}

function Cleanup-Logs {
    Write-Step "Cleaning log files..."
    
    $logDir = Join-Path $PSScriptRoot ".." "logs"
    if (Test-Path $logDir) {
        Get-ChildItem -Path $logDir -Recurse -File | ForEach-Object {
            Remove-Item -Force $_.FullName
            Write-Host "  Removed: $($_.Name)" -ForegroundColor Gray
        }
    }
}

function Cleanup-TempFiles {
    Write-Step "Cleaning temporary files..."
    
    $tempDir = Join-Path $PSScriptRoot ".." "temp"
    if (Test-Path $tempDir) {
        Get-ChildItem -Path $tempDir -Recurse -File | ForEach-Object {
            Remove-Item -Force $_.FullName
            Write-Host "  Removed: $($_.Name)" -ForegroundColor Gray
        }
    }
}

function Cleanup-Database {
    if (-not $Full) { return }
    
    Write-Step "Cleaning development database..."
    
    $dbPath = Join-Path $PSScriptRoot ".." "data" "owlet-dev.db"
    if (Test-Path $dbPath) {
        Remove-Item -Force $dbPath
        Write-Host "  Removed: owlet-dev.db" -ForegroundColor Gray
    }
    
    # Clean database backup files
    Get-ChildItem -Path (Join-Path $PSScriptRoot ".." "data") -Filter "*.db-*" | ForEach-Object {
        Remove-Item -Force $_.FullName
        Write-Host "  Removed: $($_.Name)" -ForegroundColor Gray
    }
}

function Cleanup-Certificates {
    if (-not $Full) { return }
    
    Write-Step "Cleaning development certificates..."
    
    $certDir = Join-Path $PSScriptRoot ".." "certificates"
    if (Test-Path $certDir) {
        Get-ChildItem -Path $certDir -Recurse -File | ForEach-Object {
            Remove-Item -Force $_.FullName
            Write-Host "  Removed: $($_.Name)" -ForegroundColor Gray
        }
    }
}

# Main execution
try {
    if ($Logs) {
        Cleanup-Logs
    } else {
        Cleanup-BuildArtifacts
        Cleanup-Logs
        Cleanup-TempFiles
        
        if ($Full) {
            Cleanup-Database
            Cleanup-Certificates
        }
    }
    
    Write-Step "✓ Cleanup completed successfully"
}
catch {
    Write-Host "Cleanup failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
```

## Success Criteria

- [ ] Aspire AppHost successfully orchestrates all Owlet services in development
- [ ] Visual Studio Code debugging configurations work for all service components
- [ ] Hot reload functionality works for code changes during development
- [ ] Development database setup (SQLite/PostgreSQL) completes without errors
- [ ] HTTPS certificates are generated and trusted for local development
- [ ] Telemetry and logging integration provides comprehensive development visibility
- [ ] Development environment setup script runs successfully on clean Windows machines
- [ ] Service discovery and inter-service communication works in Aspire orchestration
- [ ] Development configurations properly isolate from production settings
- [ ] Cleanup scripts maintain development environment hygiene without data loss

## Testing Strategy

### Unit Tests
**What to test:** Service defaults configuration, development extensions, Aspire integration  
**Mocking strategy:** Mock external dependencies, configuration providers  
**Test data approach:** Use test configuration and isolated service containers

**Example Tests:**
```csharp
[Fact]
public void ServiceDefaults_ShouldConfigureOpenTelemetry_InDevelopment()
{
    // Arrange
    var builder = WebApplication.CreateBuilder();
    builder.Environment.EnvironmentName = "Development";
    
    // Act
    builder.AddServiceDefaults();
    
    // Assert
    var services = builder.Services;
    services.Should().ContainSingle(s => s.ServiceType == typeof(IOpenTelemetryBuilder));
}
```

### Integration Tests
**What to test:** Complete Aspire orchestration, service startup and communication  
**Test environment:** Containerized test environment with full service stack  
**Automation:** Automated Aspire application testing with health checks

### E2E Tests
**What to test:** Full development workflow from setup to debugging  
**User workflows:** Environment Setup → Service Start → Development → Debugging → Cleanup

## Dependencies

### Technical Dependencies
- .NET Aspire - Development orchestration and service discovery
- OpenTelemetry - Distributed tracing and metrics collection
- Serilog - Structured logging with development enhancements
- Visual Studio Code - Primary development environment

### Story Dependencies
- **Blocks:** S80 (Documentation & Testing)
- **Blocked By:** S30 (Core Infrastructure)

## Next Steps

1. Configure Aspire AppHost with complete service orchestration
2. Create comprehensive debugging configurations for Visual Studio Code
3. Implement development service defaults with telemetry integration
4. Develop automated development environment setup and cleanup scripts
5. Test development environment across different Windows configurations
6. Create development documentation and troubleshooting guides

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** [Date] (when status = Complete)