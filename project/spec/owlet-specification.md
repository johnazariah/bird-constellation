# 🦉 Owlet Technical Specification
*The Librarian - "Knows everything you keep"*

## Overview

Owlet is a production-ready, local-first document indexing and search application designed for seamless installation and operation on Windows machines. It runs as a Windows service with an embedded web UI, providing file discovery, content extraction, and semantic search capabilities while serving as the foundational knowledge layer for the Owlet Constellation ecosystem.

## Deployment Architecture

### Why Aspire + Windows Service = Optimal Approach

**Aspire works perfectly with Windows Services** and provides several critical advantages:

1. **Development-to-Production Consistency**: The same AppHost composition that works locally becomes your production deployment blueprint
2. **Built-in Observability**: OpenTelemetry, health checks, and service discovery come automatically
3. **Future-Proof Orchestration**: When you need to add Lumen, Cygnet, etc., they integrate seamlessly
4. **Flexible Deployment**: Aspire can deploy to containers, cloud services, or Windows Services
5. **Simplified Configuration**: Service defaults handle logging, metrics, and configuration patterns

### Dual Architecture: Pure Windows Service + Aspire Development

**End-User Deployment**: Pure Windows Service for reliable, minimal-dependency installation
**Development & Constellation**: Aspire orchestration for rich development experience and future ecosystem

### End-User Windows Service (Production)

```csharp
// Owlet.Service - Pure Windows Service for end-user installation
public static class ServiceHost
{
    public static async Task<int> Main(string[] args) =>
        await WebApplication.CreateBuilder(args)
            .ConfigureForWindowsService()
            .ConfigureForProduction()
            .Build()
            .RunAsync()
            .ContinueWith(_ => 0);

    private static WebApplicationBuilder ConfigureForWindowsService(this WebApplicationBuilder builder)
    {
        // Windows Service integration (no Aspire dependencies)
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "OwletService";
        });

        // Production content root and configuration
        builder.Host.UseContentRoot(AppContext.BaseDirectory);
        
        return builder;
    }

    private static WebApplicationBuilder ConfigureForProduction(this WebApplicationBuilder builder)
    {
        // Minimal production configuration
        builder.Services.AddOwletCore(builder.Configuration);
        builder.Services.AddOwletApi(builder.Configuration);
        builder.Services.AddOwletIndexer(builder.Configuration);
        builder.Services.AddHealthChecks();
        
        return builder;
    }
}
```

### Aspire Development Environment

```csharp
// Owlet.AppHost - Development orchestration and constellation blueprint
var builder = DistributedApplication.CreateBuilder(args);

var owletService = builder.AddProject<Projects.Owlet_Service>("owlet-service")
    .WithHttpEndpoint(port: 5555, name: "api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Future constellation members auto-discover owlet-service
var lumen = builder.AddProject<Projects.Lumen_Service>("lumen")
    .WithReference(owletService);

builder.Build().Run();
```
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "OwletService";
        });

        // Production considerations
        builder.Host.UseContentRoot(AppContext.BaseDirectory);
        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(AppContext.BaseDirectory)
                  .AddJsonFile("appsettings.json", optional: false)
                  .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                  .AddEnvironmentVariables("OWLET_");
        });

        return builder;
    }

    private static WebApplicationBuilder ConfigureForAspire(this WebApplicationBuilder builder)
    {
        // Aspire service defaults - provides observability, health checks, service discovery
        builder.AddServiceDefaults();
        
        // Core services with Aspire integrations
        builder.Services.AddSingleton<IFileWatcher, FileWatcherService>();
        builder.Services.AddHostedService<IndexingService>();
        
        // Database integration (future: could be PostgreSQL for constellation)
        builder.AddSqliteDatabase("owlet-db");
        
        return builder;
    }
}
```

### Deployment Models

| Environment | How It Works | Benefits |
|-------------|--------------|----------|
| **Development** | `dotnet run --project Owlet.AppHost` | Aspire Dashboard, hot reload, easy debugging |
| **Production** | Windows Service + Aspire manifest | Service reliability + observability patterns |
| **Constellation** | AppHost orchestrates all members | Automatic service discovery and health monitoring |

### Distribution Model
- **Single MSI installer** with self-contained .NET deployment
- **Windows service** registered during installation
- **Aspire AppHost** can be bundled for future constellation expansion
- **Embedded SQLite** with option to upgrade to PostgreSQL for multi-service scenarios

### Solution Structure (Dual Architecture)
```
Owlet.sln
├── src/
│   ├── Owlet.AppHost          # Aspire orchestration (dev + constellation)
│   ├── Owlet.Service          # Pure Windows service (production deployment)
│   ├── Owlet.Core             # Core business logic (shared)
│   ├── Owlet.Api              # Web API with Carter (shared)
│   ├── Owlet.Indexer          # File monitoring and processing (shared)
│   ├── Owlet.Extractors       # Content extraction pipeline (shared)
│   ├── Owlet.Infrastructure   # Data access and external concerns (shared)
│   └── Owlet.ServiceDefaults  # Aspire configuration (dev + constellation only)
├── tools/
│   ├── Owlet.TrayApp          # System tray application
│   └── Owlet.Diagnostics     # Health check and diagnostic tools
├── packaging/
│   ├── installer/             # WiX installer project (ships Owlet.Service)
│   ├── dependencies/          # Bundled runtime dependencies
│   └── scripts/               # Installation and service scripts
├── tests/
│   ├── Owlet.Tests.Unit       # Unit tests
│   ├── Owlet.Tests.Integration # Integration tests (both hosting models)
│   └── Owlet.Tests.E2E        # End-to-end installation tests
└── ci/
    ├── build/                 # Build pipeline definitions
    ├── test/                  # Test automation
    └── package/               # Packaging and signing
```

### Technology Stack (Updated)
- **.NET 9** with Aspire and self-contained deployment
- **Windows Service** using `Microsoft.Extensions.Hosting.WindowsServices`
- **Aspire Service Defaults** for observability and configuration
- **ASP.NET Core** with embedded Kestrel
- **Entity Framework Core** with SQLite (upgradeable to PostgreSQL)
- **Serilog** integrated with Aspire logging patterns
- **Carter** for functional API composition
- **WiX Toolset** for MSI packaging

## Project Specifications

### 1. Owlet.AppHost (Aspire Orchestration)
**Purpose**: Development orchestration and production deployment blueprint

```csharp
// Modern Aspire orchestration with service composition
var builder = DistributedApplication.CreateBuilder(args);

// Core Owlet service
var owletService = builder.AddProject<Projects.Owlet_Service>("owlet-service")
    .WithHttpEndpoint(port: 5555, name: "api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithHealthCheck("/health");

// Database - SQLite for standalone, PostgreSQL for constellation
var database = builder.Environment.IsDevelopment() 
    ? builder.AddSqliteDatabase("owlet-db") 
    : builder.AddPostgresDatabase("owlet-postgres");

owletService.WithReference(database);

// Future constellation members (commented for Phase 1)
// var lumen = builder.AddProject<Projects.Lumen_Service>("lumen")
//     .WithReference(owletService);
// 
// var cygnet = builder.AddProject<Projects.Cygnet_Service>("cygnet")
//     .WithReference(owletService);

builder.Build().Run();
```

**Responsibilities**:
- Local development orchestration with hot reload
- Service discovery and dependency injection
- Health monitoring and observability
- Production deployment manifest generation
- Future constellation member coordination

### 2. Owlet.Service (Windows Service + Aspire Integration)
**Purpose**: Production Windows service with Aspire observability patterns

```csharp
// Aspire-integrated Windows service with functional composition
public static class ServiceHost
{
    public static async Task<int> Main(string[] args) =>
        await CreateWebApplication(args)
            .RunAsync()
            .Map(_ => 0)
            .Recover(ex => ex.LogCriticalAndReturn(-1));

    private static WebApplication CreateWebApplication(string[] args) =>
        WebApplication.CreateBuilder(args)
            .ConfigureForProduction()
            .ConfigureAspireIntegration()
            .ConfigureFunctionalServices()
            .Build()
            .ConfigureMiddleware();

    private static WebApplicationBuilder ConfigureForProduction(this WebApplicationBuilder builder)
    {
        // Windows Service integration
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "OwletService";
        });

        // Production content root and configuration
        builder.Host.UseContentRoot(AppContext.BaseDirectory);
        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            var basePath = context.HostingEnvironment.IsDevelopment() 
                ? Directory.GetCurrentDirectory()
                : AppContext.BaseDirectory;

            config.SetBasePath(basePath)
                  .AddJsonFile("appsettings.json", optional: false)
                  .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                  .AddEnvironmentVariables("OWLET_");
        });

        return builder;
    }

    private static WebApplicationBuilder ConfigureAspireIntegration(this WebApplicationBuilder builder)
    {
        // Aspire service defaults - automatic observability
        builder.AddServiceDefaults();
        
        // Database integration with Aspire patterns
        if (builder.Environment.IsDevelopment())
        {
            builder.AddSqliteDatabase("owlet-db");
        }
        else
        {
            // Production can use same SQLite or upgrade to PostgreSQL for constellation
            builder.AddSqliteDatabase("owlet-db");
        }

        return builder;
    }

    private static WebApplicationBuilder ConfigureFunctionalServices(this WebApplicationBuilder builder) =>
        builder
            .ConfigureCore()
            .ConfigureIndexing()
            .ConfigureApi()
            .ConfigureExtractors();

    private static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        // Aspire default endpoints (health, metrics, etc.)
        app.MapDefaultEndpoints();
        
        // Carter API routes
        app.MapCarter();
        
        // Static files for web UI
        app.UseStaticFiles();
        app.UseDefaultFiles();

        return app;
    }
}

// Extension methods for clean service configuration
public static class ServiceConfigurationExtensions
{
    public static WebApplicationBuilder ConfigureCore(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOwletCore(builder.Configuration)
            .AddValidatedConfiguration<OwletConfiguration>(builder.Configuration);
        
        return builder;
    }

    public static WebApplicationBuilder ConfigureIndexing(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddSingleton<IFileWatcher, FileWatcherService>()
            .AddHostedService<IndexingService>()
            .AddSingleton<IFileProcessor, FileProcessor>();
            
        return builder;
    }

    public static WebApplicationBuilder ConfigureApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddCarter()
            .AddProblemDetails()
            .AddOwletApiServices();
            
        return builder;
    }

    public static WebApplicationBuilder ConfigureExtractors(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddTextExtractors()
            .AddImageExtractors()
            .AddPollyResiliencePolicies();
            
        return builder;
    }
}
```

### 3. Owlet.ServiceDefaults (Aspire Configuration)
**Purpose**: Shared Aspire service defaults for the constellation

```csharp
// Constellation-wide service defaults
public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddOwletServiceDefaults(this WebApplicationBuilder builder)
    {
        // Standard Aspire service defaults
        builder.AddServiceDefaults();
        
        // Owlet constellation specific defaults
        builder.Services.Configure<OpenTelemetryLoggerOptions>(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        });

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Constellation service discovery
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // Constellation event bus (future)
        // builder.AddEventBus();

        return builder;
    }

    public static WebApplication MapOwletDefaultEndpoints(this WebApplication app)
    {
        // Standard health and metrics endpoints
        app.MapHealthChecks("/health");
        
        // Constellation discovery endpoints
        app.MapGet("/constellation/info", () => new
        {
            Service = "owlet",
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            Capabilities = new[] { "search", "indexing", "file-monitoring", "events" },
            Endpoints = new
            {
                Search = "/api/search",
                Files = "/api/files",
                Folders = "/api/folders",
                Events = "/api/events",
                Health = "/health"
            }
        });

        app.MapGet("/constellation/capabilities", () => new[] { "search", "index", "events" });

        return app;
    }
}
```

### 4. Aspire Integration Benefits for Owlet

#### Development Experience
```csharp
// Local development with Aspire Dashboard
// dotnet run --project Owlet.AppHost
// - Automatic service startup
// - Real-time logs and metrics
// - Health check monitoring
// - Service dependency visualization
```

#### Production Deployment
```csharp
// Generate deployment manifest
// dotnet run --project Owlet.AppHost -- --publisher manifest --output-path deployment/
//
// This creates a manifest that can deploy to:
// - Windows Services (current requirement)
// - Docker containers (future scaling)
// - Azure Container Apps (cloud deployment)
// - Kubernetes (enterprise deployment)
```

#### Testing with Aspire
```csharp
// Integration tests with Aspire TestHost
public sealed class OwletIntegrationTests : IClassFixture<DistributedApplicationTestingBuilder>
{
    private readonly DistributedApplicationTestingBuilder _builder;

    public OwletIntegrationTests(DistributedApplicationTestingBuilder builder)
    {
        _builder = builder;
    }

    [Fact]
    public async Task SearchApi_ReturnsResults_WhenFilesIndexed()
    {
        // Arrange
        await using var app = await _builder.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("owlet-service");

        // Act
        var response = await httpClient.GetAsync("/api/search?q=test");

        // Assert
        response.Should().BeSuccessful();
    }
}
```

```csharp
// Immutable domain models using records
public sealed record IndexedFile(
    FileId Id,
    FilePath Path,
    FileName Name,
    FileExtension Extension,
    FileKind Kind,
    Option<ExtractedContent> Content,
    ImmutableList<Tag> Tags,
    DateTime ModifiedAt,
    DateTime IndexedAt
) : IValidatable<IndexedFile>;

// Functional search pipeline with monadic composition
public static class SearchPipeline
{
    public static Task<Result<SearchResults>> ExecuteAsync(
        SearchQuery query,
        ISearchRepository repository) =>
        query
            .Validate()
            .ToAsync()
            .Bind(ValidatedQuery => 
                repository.SearchAsync(ValidatedQuery))
            .Map(ApplyRelevanceRanking)
            .Map(ApplyResultLimits)
            .Bind(EnrichWithMetadata);
}

// Algebraic data types for domain modeling
public abstract record FileProcessingResult
{
    public sealed record Success(IndexedFile File) : FileProcessingResult;
    public sealed record Failure(FilePath Path, ProcessingError Error) : FileProcessingResult;
    public sealed record Skipped(FilePath Path, SkipReason Reason) : FileProcessingResult;
}
```

### 3. Owlet.Api (Embedded Web Interface)
**Purpose**: Self-hosted web API with Carter functional composition

```csharp
// Carter modules for functional API composition
public sealed class SearchModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var searchGroup = app.MapGroup("/api/search")
            .WithTags("Search")
            .WithOpenApi();

        searchGroup.MapGet("/", SearchFilesAsync)
            .Produces<SearchResults>()
            .ProducesValidationProblem();

        searchGroup.MapGet("/suggest", GetSearchSuggestionsAsync)
            .Produces<SearchSuggestions>();
    }

    private static async Task<IResult> SearchFilesAsync(
        [AsParameters] SearchRequest request,
        ISearchService searchService,
        CancellationToken cancellationToken) =>
        await SearchQuery.Create(request.Query, request.Filters)
            .ToAsync()
            .Bind(query => searchService.SearchAsync(query, cancellationToken))
            .Match(
                success => Results.Ok(success),
                failure => Results.BadRequest(failure.ToValidationProblem())
            );
}

// Events module for constellation integration
public sealed class EventsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var eventsGroup = app.MapGroup("/api/events")
            .WithTags("Events")
            .WithOpenApi();

        eventsGroup.MapGet("/", GetEventsAsync)
            .Produces<EventStream>()
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetEventsAsync(
        DateTime? since,
        int limit,
        IEventService eventService,
        CancellationToken cancellationToken) =>
        await eventService.GetEventsSince(since ?? DateTime.UtcNow.AddHours(-1), limit, cancellationToken)
            .Match(
                success => Results.Ok(success),
                failure => Results.BadRequest(failure.ToValidationProblem())
            );
}

// Tags module for file tagging
public sealed class TagsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var tagsGroup = app.MapGroup("/api/files")
            .WithTags("Tags")
            .WithOpenApi();

        tagsGroup.MapPost("/{id}/tags", AddFileTagsAsync)
            .Produces<TagOperationResult>()
            .ProducesValidationProblem();

        tagsGroup.MapDelete("/{id}/tags", RemoveFileTagsAsync)
            .Produces<TagOperationResult>()
            .ProducesValidationProblem();
    }

    private static async Task<IResult> AddFileTagsAsync(
        int id,
        TagRequest request,
        ITagService tagService,
        CancellationToken cancellationToken) =>
        await tagService.AddTagsToFileAsync(id, request.Tags, cancellationToken)
            .Match(
                success => Results.Ok(success),
                failure => Results.BadRequest(failure.ToValidationProblem())
            );

    private static async Task<IResult> RemoveFileTagsAsync(
        int id,
        TagRequest request,
        ITagService tagService,
        CancellationToken cancellationToken) =>
        await tagService.RemoveTagsFromFileAsync(id, request.Tags, cancellationToken)
            .Match(
                success => Results.Ok(success),
                failure => Results.BadRequest(failure.ToValidationProblem())
            );
}

// Functional validation pipeline
public static class SearchRequest
{
    public static Validation<SearchQuery> Create(string? query, SearchFilters? filters) =>
        (ValidateQuery(query), ValidateFilters(filters))
            .Apply((q, f) => new SearchQuery(q, f));

    private static Validation<NonEmptyText> ValidateQuery(string? query) =>
        NonEmptyText.Create(query)
            .ToValidation("Query cannot be empty");
}
```

### 4. Owlet.Indexer (Event-Driven File Processing)
**Purpose**: Reactive file monitoring with functional error handling

```csharp
// Simplified file monitoring for Phase 1
public sealed class FileWatcherService : BackgroundService
{
    private readonly IFileSystemWatcher _watcher;
    private readonly IFileProcessor _processor;
    private readonly ILogger<FileWatcherService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var change in _watcher.FileChanges.WithCancellation(stoppingToken))
        {
            if (ShouldProcessFile(change))
            {
                var result = await ProcessFileChange(change);
                LogProcessingResult(result);
            }
        }
    }

    private async Task<FileProcessingResult> ProcessFileChange(FileChangeEvent change) =>
        await change.EventType switch
        {
            FileEventType.Created => _processor.IndexFileAsync(change.Path),
            FileEventType.Modified => _processor.ReindexFileAsync(change.Path),
            FileEventType.Deleted => _processor.RemoveFileAsync(change.Path),
            FileEventType.Renamed => _processor.RenameFileAsync(change.OldPath, change.NewPath),
            _ => FileProcessingResult.Skipped(change.Path, SkipReason.UnsupportedEvent)
        };
}

// Simple file processor with Office file retry logic
public sealed class FileProcessor : IFileProcessor
{
    private readonly IExtractorService _extractorService;
    private readonly IIndexRepository _repository;
    private readonly IEventService _eventService;
    private readonly ILogger<FileProcessor> _logger;

    public async Task<FileProcessingResult> IndexFileAsync(FilePath path)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var content = await _extractorService.ExtractContentAsync(path);
                var indexedFile = await _repository.StoreFileAsync(path, content);
                
                // Publish event for constellation
                await _eventService.PublishAsync(new FileIndexedEvent(indexedFile));
                
                return FileProcessingResult.Success(indexedFile);
            }
            catch (IOException ex) when (IsFileLockException(ex) && attempt < maxRetries)
            {
                _logger.LogWarning("File {Path} is locked, attempt {Attempt}/{MaxRetries}", 
                    path, attempt, maxRetries);
                
                await Task.Delay(retryDelayMs * attempt); // Exponential backoff
                continue;
            }
            catch (IOException ex) when (IsFileLockException(ex))
            {
                _logger.LogWarning("File {Path} remains locked after {MaxRetries} attempts, marking as unreadable", 
                    path, maxRetries);
                
                var unreadableFile = await _repository.StoreUnreadableFileAsync(path, ex.Message);
                return FileProcessingResult.LockedFile(unreadableFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file {Path}", path);
                return FileProcessingResult.Failure(path, ProcessingError.From(ex));
            }
        }

        return FileProcessingResult.Failure(path, ProcessingError.UnexpectedRetryLoop);
    }

    private static bool IsFileLockException(IOException ex) =>
        ex.HResult == -2147024864 || // File is being used by another process
        ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase);
}

// Updated result type to include locked files
public abstract record FileProcessingResult
{
    public sealed record Success(IndexedFile File) : FileProcessingResult;
    public sealed record Failure(FilePath Path, ProcessingError Error) : FileProcessingResult;
    public sealed record Skipped(FilePath Path, SkipReason Reason) : FileProcessingResult;
    public sealed record LockedFile(UnreadableFile File) : FileProcessingResult;
}

    public async Task<FileProcessingResult> IndexFileAsync(FilePath path) =>
        await _retryPolicy.ExecuteAsync(async () =>
            await path
                .ValidateExists()
                .ToAsync()
                .Bind(ExtractContentAsync)
                .Bind(ClassifyFileAsync)
                .Bind(GenerateTagsAsync)
                .Bind(StoreInIndexAsync)
                .Match(
                    success => FileProcessingResult.Success(success),
                    failure => FileProcessingResult.Failure(path, failure)
                ));
}
```

### 5. Owlet.Infrastructure (External Concerns)
**Purpose**: Data access, logging, and external service integration

```csharp
// Repository pattern with functional error handling
public sealed class SqliteSearchRepository : ISearchRepository
{
    private readonly OwletDbContext _context;
    private readonly ILogger<SqliteSearchRepository> _logger;

    public async Task<Result<SearchResults>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default) =>
        await Try(async () =>
        {
            var baseQuery = _context.IndexedFiles
                .Where(f => f.Content != null);

            var filteredQuery = query.Filters.Kind switch
            {
                Some(kind) => baseQuery.Where(f => f.Kind == kind),
                None => baseQuery
            };

            var searchResults = await filteredQuery
                .Where(f => EF.Functions.Like(f.Content!, $"%{query.Text}%") ||
                           EF.Functions.Like(f.Name, $"%{query.Text}%"))
                .OrderByDescending(f => f.ModifiedAt)
                .Take(query.Limit)
                .ToListAsync(cancellationToken);

            return new SearchResults(
                searchResults.Select(MapToSearchResult).ToImmutableList(),
                searchResults.Count,
                query.Text
            );
        })
        .MapError(ex => new SearchError($"Database search failed: {ex.Message}", ex));

    private static async Task<Result<T>> Try<T>(Func<Task<T>> operation) =>
        await operation()
            .Map(Result.Success)
            .Recover(ex => Result.Failure<T>(ex));
}

// Configuration with strong typing and validation
public sealed record DatabaseConfiguration(
    ConnectionString ConnectionString,
    PositiveInteger CommandTimeout,
    PositiveInteger MaxRetryAttempts
) : IValidatable<DatabaseConfiguration>
{
    public static Validation<DatabaseConfiguration> Create(IConfiguration config) =>
        (
            ConnectionString.Create(config["ConnectionStrings:Default"]),
            PositiveInteger.Create(config.GetValue<int>("Database:CommandTimeout")),
            PositiveInteger.Create(config.GetValue<int>("Database:MaxRetryAttempts"))
        ).Apply((conn, timeout, retries) => 
            new DatabaseConfiguration(conn, timeout, retries));
}
```

## Installation & Packaging

### MSI Installer Package
```
Owlet-1.0.0-x64.msi
├── Program Files/Owlet/
│   ├── bin/                    # Self-contained .NET application
│   ├── wwwroot/               # Web UI assets
│   ├── data/                  # SQLite database location
│   ├── logs/                  # Application logs
│   ├── config/                # Configuration files
│   └── dependencies/
│       ├── ollama/            # Bundled Ollama for embeddings
│       └── models/            # Pre-installed language models
├── System Integration/
│   ├── Windows Service        # OwletService registration
│   ├── Windows Firewall       # HTTP port exemption
│   ├── Start Menu             # Owlet shortcuts
│   └── System Tray           # OwletTray.exe autostart
└── Uninstaller/              # Clean removal capability
```

### Installation Process
1. **Pre-flight checks**: Windows version, available disk space, admin rights
2. **Dependency installation**: Bundled runtimes, drivers, certificates
3. **Service registration**: Windows service installation and configuration
4. **Firewall configuration**: Allow local HTTP traffic for web UI
5. **User account setup**: Create service account with minimal privileges
6. **Initial configuration**: Default watched folders, security settings
7. **Service start**: Launch service and verify healthy startup
8. **User onboarding**: Open web UI with setup wizard

### WiX Installer Features
```xml
<!-- Product definition with upgrade logic -->
<Product Id="*" 
         Name="Owlet Document Indexer" 
         Version="!(bind.FileVersion.OwletService.exe)"
         Manufacturer="Owlet Team"
         UpgradeCode="{FIXED-GUID}">

  <!-- Self-contained deployment -->
  <Directory Id="TARGETDIR" Name="SourceDir">
    <Directory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLDIR" Name="Owlet">
        <Component Id="ServiceExecutable" Guid="*">
          <File Id="OwletService.exe" Source="$(var.PublishDir)OwletService.exe" KeyPath="yes"/>
          <ServiceInstall Id="OwletServiceInstall"
                         Name="OwletService"
                         DisplayName="Owlet Document Indexer"
                         Description="Indexes and searches your documents"
                         Type="ownProcess"
                         Start="auto"
                         Account="LocalSystem"
                         ErrorControl="normal"/>
          <ServiceControl Id="OwletServiceControl"
                         Name="OwletService"
                         Start="install"
                         Stop="both"
                         Remove="uninstall"/>
        </Component>
      </Directory>
    </Directory>
  </Directory>

  <!-- Feature organization -->
  <Feature Id="CoreFeature" Title="Core Service" Level="1">
    <ComponentRef Id="ServiceExecutable"/>
    <ComponentRef Id="WebAssets"/>
    <ComponentRef Id="DatabaseSchema"/>
  </Feature>
  
  <Feature Id="AIFeature" Title="AI Models" Level="1">
    <ComponentRef Id="OllamaRuntime"/>
    <ComponentRef Id="EmbeddingModels"/>
  </Feature>
</Product>
```

## Operational Architecture

### Logging & Observability
```csharp
// Structured logging with Serilog
public static class LoggingConfiguration
{
    public static IHostBuilder ConfigureSerilog(this IHostBuilder builder) =>
        builder.UseSerilog((context, config) =>
            config
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.File(
                    path: "logs/owlet-.log",
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 10_000_000,
                    retainedFileCountLimit: 30,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.EventLog(
                    source: "OwletService",
                    logName: "Application",
                    restrictedToMinimumLevel: LogEventLevel.Warning)
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information)
        );
}

// Application metrics and health checks
public sealed class OwletHealthCheck : IHealthCheck
{
    private readonly ISearchService _searchService;
    private readonly IIndexingService _indexingService;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var checks = await Task.WhenAll(
            CheckDatabaseConnectionAsync(cancellationToken),
            CheckFileSystemAccessAsync(cancellationToken),
            CheckIndexingServiceAsync(cancellationToken)
        );

        return checks.All(c => c.IsHealthy)
            ? HealthCheckResult.Healthy("All systems operational")
            : HealthCheckResult.Unhealthy("One or more subsystems failing",
                data: checks.Where(c => !c.IsHealthy).ToDictionary(c => c.Name, c => c.Error));
    }
}
```

### Configuration Management
```csharp
// Hierarchical configuration with validation
public sealed record OwletConfiguration(
    ServiceConfiguration Service,
    DatabaseConfiguration Database,
    IndexingConfiguration Indexing,
    ApiConfiguration Api,
    LoggingConfiguration Logging
) : IValidatable<OwletConfiguration>
{
    public static async Task<Result<OwletConfiguration>> LoadAsync(
        string configPath = "config/owlet.json") =>
        await File.ReadAllTextAsync(configPath)
            .Map(JsonSerializer.Deserialize<RawConfiguration>)
            .Bind(ValidateAndTransform)
            .MapError(ex => new ConfigurationError($"Failed to load configuration: {ex.Message}"));

    private static Validation<OwletConfiguration> ValidateAndTransform(RawConfiguration raw) =>
        (
            ServiceConfiguration.Create(raw.Service),
            DatabaseConfiguration.Create(raw.Database),
            IndexingConfiguration.Create(raw.Indexing),
            ApiConfiguration.Create(raw.Api),
            LoggingConfiguration.Create(raw.Logging)
        ).Apply((service, db, indexing, api, logging) =>
            new OwletConfiguration(service, db, indexing, api, logging));
}

// Environment-specific configuration
public static class ConfigurationSources
{
    public static IConfigurationBuilder AddOwletConfiguration(
        this IConfigurationBuilder builder) =>
        builder
            .AddJsonFile("config/owlet.json", optional: false)
            .AddJsonFile($"config/owlet.{Environment.GetEnvironmentVariable("OWLET_ENV")}.json", optional: true)
            .AddEnvironmentVariables("OWLET_")
            .AddUserSecrets<ServiceHost>(optional: true);
}
```

### Configuration Example

```json
// appsettings.json - Production configuration
{
  "ConnectionStrings": {
    "Default": "Data Source=data/owlet.db"
  },
  "Server": {
    "Port": 5555,
    "AllowedHosts": "localhost"
  },
  "Indexing": {
    "MaxFileSizeMB": 100,
    "SupportedExtensions": [
      ".txt", ".md", ".pdf", ".docx", 
      ".jpg", ".jpeg", ".png", ".gif", ".bmp"
    ],
    "ExcludedFolders": [
      "node_modules", ".git", "bin", "obj", ".vs", ".vscode"
    ],
    "RetryAttempts": 3,
    "RetryDelayMs": 1000
  },
  "Events": {
    "RetentionDays": 30,
    "MaxEventsInMemory": 1000
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    },
    "EventLog": {
      "LogLevel": {
        "Default": "Warning"
      }
    }
  }
}
```

### Database Schema with Events

```sql
-- SQLite Database: owlet.db

-- Watched Folders
CREATE TABLE WatchedFolders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Path TEXT NOT NULL UNIQUE,
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    AddedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexed Files
CREATE TABLE IndexedFiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Path TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Extension TEXT NOT NULL,
    Kind INTEGER NOT NULL, -- 0=Document, 1=Image, 2=Other
    Content TEXT NULL,     -- Extracted text content
    Tags TEXT NULL,        -- JSON array of tags
    ModifiedAt DATETIME NOT NULL,
    IndexedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsReadable BOOLEAN NOT NULL DEFAULT 1,
    ErrorMessage TEXT NULL -- For unreadable files
);

-- Events for constellation integration
CREATE TABLE Events (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventType TEXT NOT NULL, -- 'FileIndexed', 'FileDeleted', 'TagsAdded', etc.
    FileId INTEGER NULL,     -- Reference to IndexedFiles
    Payload TEXT NOT NULL,   -- JSON event data
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (FileId) REFERENCES IndexedFiles(Id)
);

-- Indexes for performance
CREATE INDEX IX_IndexedFiles_Kind ON IndexedFiles(Kind);
CREATE INDEX IX_IndexedFiles_Extension ON IndexedFiles(Extension);
CREATE INDEX IX_IndexedFiles_ModifiedAt ON IndexedFiles(ModifiedAt);
CREATE INDEX IX_IndexedFiles_Name ON IndexedFiles(Name);
CREATE INDEX IX_IndexedFiles_IsReadable ON IndexedFiles(IsReadable);
CREATE INDEX IX_Events_CreatedAt ON Events(CreatedAt);
CREATE INDEX IX_Events_EventType ON Events(EventType);

-- Full-text search (SQLite FTS5)
CREATE VIRTUAL TABLE IndexedFiles_FTS USING fts5(
    content='IndexedFiles',
    Name,
    Content,
    Tags
);
```

### Error Handling & Recovery
```csharp
// Global exception handling with recovery strategies
public sealed class OwletExceptionHandler : IExceptionHandler
{
    private readonly ILogger<OwletExceptionHandler> _logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (400, CreateValidationResponse(validationEx)),
            NotFoundException notFoundEx => (404, CreateNotFoundResponse(notFoundEx)),
            UnauthorizedException unauthorizedEx => (401, CreateUnauthorizedResponse(unauthorizedEx)),
            ServiceUnavailableException serviceEx => (503, CreateServiceUnavailableResponse(serviceEx)),
            _ => (500, CreateInternalErrorResponse(exception))
        };

        _logger.LogError(exception, "Unhandled exception in request pipeline");

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        
        return true;
    }
}

// Circuit breaker for external dependencies
public static class ResiliencePolicies
{
    public static IAsyncPolicy CreateDatabasePolicy() =>
        Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                    Log.Warning("Database operation retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds))
            .WrapAsync(Policy
                .Handle<SqlException>()
                .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1)));
}
```

## CI/CD Pipeline Architecture

### Build Pipeline
```yaml
# .github/workflows/build.yml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build solution
      run: dotnet build --configuration Release --no-restore
    
    - name: Run unit tests
      run: dotnet test tests/Owlet.Tests.Unit --configuration Release --logger trx
    
    - name: Run integration tests
      run: dotnet test tests/Owlet.Tests.Integration --configuration Release --logger trx
    
    - name: Publish self-contained
      run: dotnet publish src/Owlet.Service --configuration Release --self-contained --runtime win-x64 --output publish/
    
    - name: Upload build artifacts
      uses: actions/upload-artifact@v4
      with:
        name: owlet-build-${{ github.sha }}
        path: publish/

  package:
    needs: build
    runs-on: windows-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Build MSI installer
      run: |
        & "${env:WIX}bin\candle.exe" -arch x64 packaging/installer/Owlet.wxs
        & "${env:WIX}bin\light.exe" -ext WixUIExtension Owlet.wixobj -out Owlet-${{ github.run_number }}.msi
    
    - name: Sign installer
      run: signtool sign /f certificate.p12 /p ${{ secrets.CERT_PASSWORD }} Owlet-${{ github.run_number }}.msi
    
    - name: Upload installer
      uses: actions/upload-artifact@v4
      with:
        name: owlet-installer-${{ github.run_number }}
        path: Owlet-${{ github.run_number }}.msi
```

### Test Pipeline
```yaml
# .github/workflows/integration-tests.yml
name: Integration Tests

on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM
  workflow_dispatch:

jobs:
  vm-tests:
    strategy:
      matrix:
        os: [windows-2019, windows-2022]
        
    runs-on: ${{ matrix.os }}
    
    steps:
    - name: Download installer
      uses: actions/download-artifact@v4
      with:
        name: owlet-installer-latest
    
    - name: Install Owlet
      run: msiexec /i Owlet-latest.msi /quiet /l*v install.log
    
    - name: Wait for service startup
      run: |
        Start-Sleep -Seconds 30
        $service = Get-Service -Name "OwletService"
        if ($service.Status -ne "Running") {
          throw "Owlet service failed to start"
        }
    
    - name: Test web interface
      run: |
        $response = Invoke-WebRequest -Uri "http://localhost:5555" -UseBasicParsing
        if ($response.StatusCode -ne 200) {
          throw "Web interface not accessible"
        }
    
    - name: Test search API
      run: |
        $testFolder = "C:\TestDocuments"
        New-Item -ItemType Directory -Path $testFolder -Force
        "Test content" | Out-File -FilePath "$testFolder\test.txt"
        
        # Add folder to index
        Invoke-RestMethod -Uri "http://localhost:5555/api/folders" -Method POST -Body '{"path":"C:\\TestDocuments"}' -ContentType "application/json"
        
        # Wait for indexing
        Start-Sleep -Seconds 10
        
        # Search for content
        $searchResult = Invoke-RestMethod -Uri "http://localhost:5555/api/search?q=Test"
        if ($searchResult.files.Count -eq 0) {
          throw "Search failed to find indexed content"
        }
    
    - name: Uninstall Owlet
      run: msiexec /x Owlet-latest.msi /quiet /l*v uninstall.log
    
    - name: Upload logs
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-logs-${{ matrix.os }}
        path: |
          install.log
          uninstall.log
          C:\Program Files\Owlet\logs\*
```

### Release Pipeline
```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: windows-latest
    
    steps:
    - name: Create release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: ${{ github.ref }}
        release_name: Owlet ${{ github.ref }}
        body: |
          ## What's New
          - Automatic release from tag ${{ github.ref }}
          
          ## Installation
          Download and run the MSI installer as administrator.
          
          ## System Requirements
          - Windows 10/11 (64-bit)
          - 4GB RAM minimum
          - 500MB disk space
        draft: false
        prerelease: false
    
    - name: Upload release asset
      uses: actions/upload-release-asset@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        upload_url: ${{ steps.create_release.outputs.upload_url }}
        asset_path: Owlet-${{ github.run_number }}.msi
        asset_name: Owlet-Setup.msi
        asset_content_type: application/octet-stream
```

## Implementation Roadmap

### Phase 0: Foundation & Infrastructure (Weeks 1-2)
**Goal**: Establish production-ready development and deployment pipeline

**Deliverables**:
- [ ] **Solution structure** with projects and folder organization
- [ ] **CI/CD pipelines** for build, test, and packaging
- [ ] **WiX installer project** with basic Windows service registration
- [ ] **VM test environment** for installation validation
- [ ] **Logging infrastructure** with Serilog and Windows Event Log
- [ ] **Configuration system** with validation and environment support
- [ ] **Health check endpoints** for monitoring and diagnostics

**Acceptance Criteria**:
- MSI installer creates working Windows service
- Service starts successfully and responds to health checks
- Automated tests validate installation/uninstall process
- Logs are structured and accessible via Windows Event Viewer

### Phase 1: Core Service Implementation (Weeks 3-4)
**Goal**: Functional document indexing service

**Deliverables**:
- [ ] **Windows service host** with proper lifecycle management
- [ ] **SQLite database** with EF Core and migration support
- [ ] **File system watcher** with event-driven processing
- [ ] **Basic extractors** for text, markdown, and PDF files
- [ ] **Search API** with full-text search capabilities
- [ ] **Web UI** for search and folder management

**Acceptance Criteria**:
- Service automatically indexes documents in watched folders
- Search returns relevant results with sub-second response times
- Web interface is accessible and functional
- System handles 1,000+ files without performance degradation

### Phase 2: Production Hardening (Weeks 5-6)  
**Goal**: Enterprise-ready reliability and diagnostics

**Deliverables**:
- [ ] **Error handling** with circuit breakers and retry policies
- [ ] **Performance monitoring** with metrics and alerts
- [ ] **Security hardening** with minimal privileges and input validation
- [ ] **Backup and recovery** procedures for database
- [ ] **Update mechanism** for in-place service updates
- [ ] **Documentation** for operators and support teams

**Acceptance Criteria**:
- Service recovers gracefully from failures
- Performance metrics are collected and monitored
- Security audit shows no critical vulnerabilities
- Automated backups protect against data loss

### Phase 3: Advanced Features (Weeks 7-8)
**Goal**: AI-powered enhancements and ecosystem integration

**Deliverables**:
- [ ] **Ollama integration** for embeddings and semantic search
- [ ] **Image processing** with metadata extraction and tagging
- [ ] **Document classification** with ML-based categorization
- [ ] **API versioning** and OpenAPI documentation
- [ ] **Plugin architecture** for extensible extractors
- [ ] **Constellation protocol** for ecosystem integration

**Acceptance Criteria**:
- Semantic search provides relevant results beyond keyword matching
- Image files are properly indexed with metadata
- System serves as foundation for other Constellation applications
- API is well-documented and versioned for stability

## System Tray Application

### Owlet.TrayApp
**Purpose**: User-facing system tray interface for service interaction

```csharp
// Modern WinUI 3 system tray application
public sealed partial class OwletTrayApp : Application
{
    private OwletTrayIcon? _trayIcon;
    private readonly IServiceProvider _services;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _trayIcon = _services.GetRequiredService<OwletTrayIcon>();
        _trayIcon.Initialize();
    }
}

public sealed class OwletTrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IOwletApiClient _apiClient;
    private readonly ILogger<OwletTrayIcon> _logger;

    public void Initialize()
    {
        _notifyIcon.Icon = LoadOwletIcon();
        _notifyIcon.Text = "Owlet - Document Indexer";
        _notifyIcon.ContextMenuStrip = CreateContextMenu();
        _notifyIcon.DoubleClick += (s, e) => OpenWebInterface();
        _notifyIcon.Visible = true;
    }

    private ContextMenuStrip CreateContextMenu() => new()
    {
        Items =
        {
            new ToolStripMenuItem("Open Owlet", null, (s, e) => OpenWebInterface()),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Service Status", null, ShowServiceStatus),
            new ToolStripMenuItem("Recent Activity", null, ShowRecentActivity),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Settings", null, OpenSettings),
            new ToolStripMenuItem("Diagnostics", null, OpenDiagnostics),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication())
        }
    };

    private async void ShowServiceStatus(object? sender, EventArgs e)
    {
        var status = await _apiClient.GetServiceStatusAsync();
        var message = status.IsHealthy
            ? $"✅ Owlet is running\n📁 {status.WatchedFolders} folders\n📄 {status.IndexedFiles} files"
            : $"❌ Owlet service is not responding";
        
        ShowBalloonTip("Service Status", message, ToolTipIcon.Info);
    }
}
```

## Diagnostics & Support Tools

### Owlet.Diagnostics
**Purpose**: Comprehensive diagnostic and troubleshooting utilities

```csharp
// Diagnostic information collection
public sealed record DiagnosticReport(
    SystemInfo SystemInfo,
    ServiceStatus ServiceStatus,
    DatabaseStatus DatabaseStatus,
    ConfigurationStatus ConfigurationStatus,
    RecentErrors RecentErrors,
    PerformanceMetrics PerformanceMetrics
);

public static class DiagnosticCollector
{
    public static async Task<DiagnosticReport> CollectAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new[]
        {
            CollectSystemInfoAsync(cancellationToken),
            CollectServiceStatusAsync(cancellationToken),
            CollectDatabaseStatusAsync(cancellationToken),
            CollectConfigurationStatusAsync(cancellationToken),
            CollectRecentErrorsAsync(cancellationToken),
            CollectPerformanceMetricsAsync(cancellationToken)
        };

        var results = await Task.WhenAll(tasks);
        
        return new DiagnosticReport(
            (SystemInfo)results[0],
            (ServiceStatus)results[1],
            (DatabaseStatus)results[2],
            (ConfigurationStatus)results[3],
            (RecentErrors)results[4],
            (PerformanceMetrics)results[5]
        );
    }

    private static async Task<SystemInfo> CollectSystemInfoAsync(CancellationToken cancellationToken) =>
        new(
            OperatingSystem: Environment.OSVersion.ToString(),
            MachineName: Environment.MachineName,
            UserName: Environment.UserName,
            ProcessorCount: Environment.ProcessorCount,
            TotalMemoryGB: GC.GetTotalMemory(false) / (1024 * 1024 * 1024),
            DotNetVersion: Environment.Version.ToString(),
            OwletVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown"
        );
}

// Self-healing capabilities
public sealed class SelfHealingService : BackgroundService
{
    private readonly IServiceHealthChecker _healthChecker;
    private readonly IServiceRecovery _recovery;
    private readonly ILogger<SelfHealingService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var healthResult = await _healthChecker.CheckHealthAsync(stoppingToken);
                
                if (!healthResult.IsHealthy)
                {
                    _logger.LogWarning("Health check failed: {Issues}", healthResult.Issues);
                    await _recovery.AttemptRecoveryAsync(healthResult.Issues, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Self-healing service encountered an error");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
```

## Success Metrics & Monitoring

### Production Readiness Checklist
- [ ] **Installation Success Rate** > 95% across Windows 10/11
- [ ] **Service Uptime** > 99.9% over 30-day periods
- [ ] **Search Response Time** < 500ms for 95th percentile
- [ ] **Memory Usage** < 200MB steady state
- [ ] **CPU Usage** < 5% during normal operation
- [ ] **Crash Recovery** < 30 seconds automatic restart
- [ ] **Uninstall Cleanup** 100% removal of files and registry entries

### User Experience Metrics
- [ ] **Installation Time** < 2 minutes from download to working
- [ ] **First Search** available within 1 minute of adding folder
- [ ] **Search Relevance** > 90% user satisfaction in testing
- [ ] **Support Tickets** < 1% of installations require assistance
- [ ] **Documentation Coverage** All common scenarios documented

### Technical Quality Metrics
- [ ] **Code Coverage** > 80% for core business logic
- [ ] **Security Scan** Zero critical vulnerabilities
- [ ] **Performance Testing** Passes load testing with 10,000+ files
- [ ] **Compatibility Testing** Works on Windows 10 1909+ and Windows 11
- [ ] **Accessibility** Meets WCAG 2.1 AA standards for web interface

---

*This specification provides a comprehensive blueprint for building Owlet as a production-ready Windows service with enterprise-grade installation, monitoring, and support capabilities. The focus on functional programming concepts, modern C# idioms, and operational excellence ensures a robust foundation for the Owlet Constellation ecosystem.*