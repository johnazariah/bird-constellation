# E10 S20: Solution Architecture

**Story:** Design complete .NET 9 solution structure with clean architecture, project organization, and build configuration for dual deployment scenarios  
**Priority:** Critical  
**Effort:** 20 hours  
**Status:** Not Started  
**Dependencies:** S10 (Analysis & Requirements)  

## Objective

This story establishes the complete solution architecture for Owlet, implementing clean architecture principles with clear separation of concerns and proper dependency management. The design supports dual deployment scenarios: pure Windows service for production and Aspire orchestration for development, ensuring maintainable code that serves both contexts effectively.

The architecture follows the principle that the installer UX trumps everything - the production deployment must be simple and reliable, while the development experience can leverage sophisticated tooling like Aspire for enhanced productivity.

## Business Context

**Revenue Impact:** ₹0 direct revenue (foundational work enables all future revenue)  
**User Impact:** All users - determines code maintainability, development velocity, and deployment reliability  
**Compliance Requirements:** Establishes foundation for future enterprise compliance requirements

## Clean Architecture Design

### 1. Solution Structure

Complete solution organization following clean architecture with proper dependency flow and deployment separation.

**Solution Organization:**

```
Owlet.sln
├── src/
│   ├── Owlet.AppHost/              # Aspire orchestration (development only)
│   ├── Owlet.Service/              # Windows service host (production deployment)
│   ├── Owlet.Core/                 # Domain logic and business rules
│   ├── Owlet.Api/                  # HTTP API with Carter endpoints
│   ├── Owlet.Indexer/              # File monitoring and processing
│   ├── Owlet.Extractors/           # Content extraction pipeline
│   ├── Owlet.Infrastructure/       # Data access and external concerns
│   └── Owlet.ServiceDefaults/      # Shared Aspire configuration
├── tools/
│   ├── Owlet.TrayApp/              # System tray application
│   └── Owlet.Diagnostics/         # Health check and diagnostic tools
├── packaging/
│   ├── installer/                  # WiX installer project
│   ├── dependencies/              # Bundled runtime dependencies
│   └── scripts/                    # Installation and service scripts
├── tests/
│   ├── Owlet.Core.Tests/           # Unit tests for domain logic
│   ├── Owlet.Api.Tests/            # API integration tests
│   ├── Owlet.Infrastructure.Tests/ # Infrastructure integration tests
│   └── Owlet.Integration.Tests/    # End-to-end service tests
└── docs/
    ├── architecture/               # Architecture documentation
    ├── deployment/                 # Deployment guides
    └── development/                # Development setup guides
```

### 2. Project Dependencies

Dependency flow ensuring clean architecture with domain at the center and infrastructure at the edges.

**Dependency Graph:**

```csharp
// Core Domain (center) - no external dependencies
Owlet.Core
├── No project references
└── Framework: .NET 9.0 Standard

// Application Layer - depends only on Core
Owlet.Indexer
├── References: Owlet.Core
└── Purpose: File monitoring and indexing orchestration

Owlet.Extractors  
├── References: Owlet.Core
└── Purpose: Content extraction from various file types

// API Layer - depends on Core, uses Infrastructure via DI
Owlet.Api
├── References: Owlet.Core
└── Purpose: HTTP endpoints with Carter

// Infrastructure Layer - depends on Core, implements interfaces
Owlet.Infrastructure
├── References: Owlet.Core
└── Purpose: Database, logging, external service adapters

// Composition Roots - orchestrate all layers
Owlet.Service (Production)
├── References: Owlet.Core, Owlet.Api, Owlet.Indexer, Owlet.Extractors, Owlet.Infrastructure
└── Purpose: Windows service host

Owlet.AppHost (Development)
├── References: All project references + Aspire.Hosting
└── Purpose: Development orchestration with Aspire dashboard

// Shared Configuration
Owlet.ServiceDefaults
├── References: Aspire.Hosting
└── Purpose: Common service registration for Aspire scenarios
```

## Project Configurations

### 1. Core Domain Project (Owlet.Core)

Pure domain logic with no external dependencies, containing business rules and domain models.

**Owlet.Core.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
    <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Owlet.Core.Tests" />
    <InternalsVisibleTo Include="Owlet.Api.Tests" />
    <InternalsVisibleTo Include="Owlet.Infrastructure.Tests" />
  </ItemGroup>

</Project>
```

**Core Domain Structure:**
```csharp
namespace Owlet.Core;

// Domain Models
public record Document
{
    public required Guid Id { get; init; }
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required DateTime LastModified { get; init; }
    public required DocumentStatus Status { get; init; }
    public string? Content { get; init; }
    public string? ExtractedText { get; init; }
    public DocumentMetadata? Metadata { get; init; }
    
    public static Result<Document> Create(string filePath, FileInfo fileInfo)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Result<Document>.Failure("File path cannot be empty");
            
        if (!fileInfo.Exists)
            return Result<Document>.Failure($"File does not exist: {filePath}");
            
        return Result<Document>.Success(new Document
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            LastModified = fileInfo.LastWriteTime,
            Status = DocumentStatus.Discovered
        });
    }
}

public enum DocumentStatus
{
    Discovered,
    Processing,
    Indexed,
    Failed,
    Deleted
}

public record DocumentMetadata
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? MimeType { get; init; }
    public DateTime? CreatedDate { get; init; }
    public Dictionary<string, object> CustomProperties { get; init; } = new();
}

// Result Type for Error Handling
public record Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

// Domain Services Interface
public interface IDocumentService
{
    Task<Result<Document>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Document>>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<Result<Document>> IndexDocumentAsync(string filePath, CancellationToken cancellationToken = default);
    Task<Result<Unit>> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
}

// Repository Interfaces (implemented in Infrastructure)
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Document?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> SearchByContentAsync(string query, CancellationToken cancellationToken = default);
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 2. HTTP API Project (Owlet.Api)

Carter-based HTTP API providing RESTful endpoints for document search and management.

**Owlet.Api.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Owlet.Core\Owlet.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Carter" Version="8.2.1" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.1" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="9.0.0" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.0.1" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Owlet.Api.Tests" />
  </ItemGroup>

</Project>
```

**API Structure:**
```csharp
namespace Owlet.Api.Endpoints;

public class DocumentEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .WithOpenApi();

        group.MapGet("/", GetDocuments)
            .WithName("GetDocuments")
            .WithSummary("Get all indexed documents")
            .Produces<IEnumerable<DocumentResponse>>();

        group.MapGet("/{id:guid}", GetDocument)
            .WithName("GetDocument")
            .WithSummary("Get document by ID")
            .Produces<DocumentResponse>()
            .ProducesValidationProblem()
            .Produces(404);

        group.MapPost("/search", SearchDocuments)
            .WithName("SearchDocuments")
            .WithSummary("Search documents by content")
            .Produces<IEnumerable<DocumentResponse>>()
            .ProducesValidationProblem();

        group.MapPost("/index", IndexDocument)
            .WithName("IndexDocument")
            .WithSummary("Index a specific document")
            .Produces<DocumentResponse>(201)
            .ProducesValidationProblem()
            .Produces(409);

        group.MapDelete("/{id:guid}", DeleteDocument)
            .WithName("DeleteDocument")
            .WithSummary("Delete document from index")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetDocuments(
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var result = await documentService.GetAllAsync(cancellationToken);
        return result.Match(
            documents => Results.Ok(documents.Select(DocumentResponse.FromDocument)),
            error => Results.Problem(error, statusCode: 500));
    }

    private static async Task<IResult> GetDocument(
        Guid id,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var result = await documentService.GetByIdAsync(id, cancellationToken);
        return result.Match(
            document => Results.Ok(DocumentResponse.FromDocument(document)),
            error => Results.NotFound(error));
    }

    private static async Task<IResult> SearchDocuments(
        SearchRequest request,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return Results.BadRequest("Search query cannot be empty");

        var result = await documentService.SearchAsync(request.Query, cancellationToken);
        return result.Match(
            documents => Results.Ok(documents.Select(DocumentResponse.FromDocument)),
            error => Results.Problem(error, statusCode: 500));
    }

    private static async Task<IResult> IndexDocument(
        IndexDocumentRequest request,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return Results.BadRequest("File path cannot be empty");

        var result = await documentService.IndexDocumentAsync(request.FilePath, cancellationToken);
        return result.Match(
            document => Results.Created($"/api/documents/{document.Id}", DocumentResponse.FromDocument(document)),
            error => Results.Conflict(error));
    }

    private static async Task<IResult> DeleteDocument(
        Guid id,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var result = await documentService.DeleteDocumentAsync(id, cancellationToken);
        return result.Match(
            _ => Results.NoContent(),
            error => Results.NotFound(error));
    }
}

// DTOs
public record DocumentResponse
{
    public required Guid Id { get; init; }
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required DateTime LastModified { get; init; }
    public required string Status { get; init; }
    public string? ExtractedText { get; init; }
    public DocumentMetadataResponse? Metadata { get; init; }

    public static DocumentResponse FromDocument(Document document) => new()
    {
        Id = document.Id,
        FilePath = document.FilePath,
        FileName = document.FileName,
        FileSize = document.FileSize,
        LastModified = document.LastModified,
        Status = document.Status.ToString(),
        ExtractedText = document.ExtractedText,
        Metadata = document.Metadata != null ? DocumentMetadataResponse.FromMetadata(document.Metadata) : null
    };
}

public record DocumentMetadataResponse
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? MimeType { get; init; }
    public DateTime? CreatedDate { get; init; }
    public Dictionary<string, object> CustomProperties { get; init; } = new();

    public static DocumentMetadataResponse FromMetadata(DocumentMetadata metadata) => new()
    {
        Title = metadata.Title,
        Author = metadata.Author,
        MimeType = metadata.MimeType,
        CreatedDate = metadata.CreatedDate,
        CustomProperties = metadata.CustomProperties
    };
}

public record SearchRequest
{
    public required string Query { get; init; }
    public int? Limit { get; init; }
    public int? Offset { get; init; }
}

public record IndexDocumentRequest
{
    public required string FilePath { get; init; }
}
```

### 3. Infrastructure Project (Owlet.Infrastructure)

Data access layer with Entity Framework Core, implementing repository patterns and external service adapters.

**Owlet.Infrastructure.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Owlet.Core\Owlet.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageReference Include="Serilog.Sinks.EventLog" Version="4.0.0" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Owlet.Infrastructure.Tests" />
  </ItemGroup>

</Project>
```

**Infrastructure Implementation:**
```csharp
namespace Owlet.Infrastructure.Data;

public class OwletDbContext : DbContext
{
    public OwletDbContext(DbContextOptions<OwletDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OwletDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public class DocumentEntity
{
    public Guid Id { get; set; }
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public DocumentStatus Status { get; set; }
    public string? Content { get; set; }
    public string? ExtractedText { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DocumentEntityConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.ToTable("Documents");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(2048);
            
        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
            
        builder.Property(e => e.ExtractedText)
            .HasColumnType("TEXT");
            
        builder.Property(e => e.MetadataJson)
            .HasColumnType("TEXT");
            
        builder.HasIndex(e => e.FilePath)
            .IsUnique();
            
        builder.HasIndex(e => e.Status);
        
        // Full-text search index on extracted text (SQLite FTS5)
        builder.HasIndex(e => e.ExtractedText);
    }
}

public class DocumentRepository : IDocumentRepository
{
    private readonly OwletDbContext _context;
    private readonly ILogger<DocumentRepository> _logger;

    public DocumentRepository(OwletDbContext context, ILogger<DocumentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            
        return entity?.ToDomainModel();
    }

    public async Task<Document?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .FirstOrDefaultAsync(d => d.FilePath == filePath, cancellationToken);
            
        return entity?.ToDomainModel();
    }

    public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Documents
            .ToListAsync(cancellationToken);
            
        return entities.Select(e => e.ToDomainModel());
    }

    public async Task<IEnumerable<Document>> SearchByContentAsync(string query, CancellationToken cancellationToken = default)
    {
        // SQLite FTS5 full-text search
        var entities = await _context.Documents
            .Where(d => d.ExtractedText != null && EF.Functions.Like(d.ExtractedText, $"%{query}%"))
            .ToListAsync(cancellationToken);
            
        return entities.Select(e => e.ToDomainModel());
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        var entity = DocumentEntity.FromDomainModel(document);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        
        _context.Documents.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Added document {DocumentId} for file {FilePath}", entity.Id, entity.FilePath);
        
        return entity.ToDomainModel();
    }

    public async Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == document.Id, cancellationToken);
            
        if (entity == null)
            throw new InvalidOperationException($"Document {document.Id} not found");
            
        entity.FilePath = document.FilePath;
        entity.FileName = document.FileName;
        entity.FileSize = document.FileSize;
        entity.LastModified = document.LastModified;
        entity.Status = document.Status;
        entity.ExtractedText = document.ExtractedText;
        entity.MetadataJson = document.Metadata != null ? JsonSerializer.Serialize(document.Metadata) : null;
        entity.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated document {DocumentId}", entity.Id);
        
        return entity.ToDomainModel();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            
        if (entity != null)
        {
            _context.Documents.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Deleted document {DocumentId}", id);
        }
    }
}

// Extension methods for entity/domain mapping
public static class DocumentEntityExtensions
{
    public static Document ToDomainModel(this DocumentEntity entity)
    {
        var metadata = !string.IsNullOrEmpty(entity.MetadataJson)
            ? JsonSerializer.Deserialize<DocumentMetadata>(entity.MetadataJson)
            : null;

        return new Document
        {
            Id = entity.Id,
            FilePath = entity.FilePath,
            FileName = entity.FileName,
            FileSize = entity.FileSize,
            LastModified = entity.LastModified,
            Status = entity.Status,
            Content = entity.Content,
            ExtractedText = entity.ExtractedText,
            Metadata = metadata
        };
    }

    public static DocumentEntity FromDomainModel(Document document)
    {
        return new DocumentEntity
        {
            Id = document.Id,
            FilePath = document.FilePath,
            FileName = document.FileName,
            FileSize = document.FileSize,
            LastModified = document.LastModified,
            Status = document.Status,
            Content = document.Content,
            ExtractedText = document.ExtractedText,
            MetadataJson = document.Metadata != null ? JsonSerializer.Serialize(document.Metadata) : null
        };
    }
}
```

### 4. Windows Service Host (Owlet.Service)

Production Windows service host that orchestrates all components with proper lifecycle management.

**Owlet.Service.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseAppHost>true</UseAppHost>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <!-- Production deployment configuration -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>false</PublishTrimmed>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Owlet.Core\Owlet.Core.csproj" />
    <ProjectReference Include="..\Owlet.Api\Owlet.Api.csproj" />
    <ProjectReference Include="..\Owlet.Indexer\Owlet.Indexer.csproj" />
    <ProjectReference Include="..\Owlet.Extractors\Owlet.Extractors.csproj" />
    <ProjectReference Include="..\Owlet.Infrastructure\Owlet.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Hosting" Version="2.2.7" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
    <Content Include="appsettings.Production.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

**Service Host Implementation:**
```csharp
namespace Owlet.Service;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.EventLog("Owlet Service", "Application", restrictedToMinimumLevel: LogEventLevel.Warning)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Owlet Document Service host");

            var builder = Host.CreateApplicationBuilder(args);
            
            // Configure as Windows Service
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "OwletService";
            });

            // Add configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables("OWLET_");
            builder.Configuration.AddCommandLine(args);

            // Add logging
            builder.Services.AddSerilog((services, lc) => lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Owlet", "Logs", "owlet-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10)
                .WriteTo.EventLog("Owlet Service", "Application", restrictedToMinimumLevel: LogEventLevel.Warning));

            // Add core services
            builder.Services.AddOwletCore(builder.Configuration);
            builder.Services.AddOwletInfrastructure(builder.Configuration);
            builder.Services.AddOwletApi();
            builder.Services.AddOwletIndexer(builder.Configuration);
            builder.Services.AddOwletExtractors();

            // Add health checks
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<OwletDbContext>()
                .AddCheck<FileSystemHealthCheck>("filesystem")
                .AddCheck<ServiceHealthCheck>("service");

            // Add HTTP services
            builder.Services.AddHttpContextAccessor();

            // Configure Kestrel
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                var port = builder.Configuration.GetValue<int>("Server:Port", 5555);
                var bindAddress = builder.Configuration.GetValue<string>("Server:BindAddress", "127.0.0.1");
                
                options.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1;
                });
            });

            // Add hosted services
            builder.Services.AddHostedService<WebServerService>();
            builder.Services.AddHostedService<DocumentIndexingService>();

            var host = builder.Build();

            // Ensure database is created
            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OwletDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }

            Log.Information("Owlet Document Service configured and ready to start");

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Owlet Document Service terminated unexpectedly");
            Environment.Exit(1);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}

public class WebServerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebServerService> _logger;
    private readonly IConfiguration _configuration;
    private WebApplication? _webApp;

    public WebServerService(
        IServiceProvider serviceProvider,
        ILogger<WebServerService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var builder = WebApplication.CreateBuilder();
            
            // Copy configuration
            builder.Configuration.AddConfiguration(_configuration);
            
            // Copy services from DI container
            foreach (var service in _serviceProvider.GetServices<ServiceDescriptor>())
            {
                builder.Services.Add(service);
            }

            // Add Carter
            builder.Services.AddCarter();

            _webApp = builder.Build();

            // Configure pipeline
            _webApp.UseRouting();
            _webApp.MapCarter();
            _webApp.MapHealthChecks("/health");

            var port = _configuration.GetValue<int>("Server:Port", 5555);
            _logger.LogInformation("Starting web server on port {Port}", port);

            await _webApp.RunAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running web server");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping web server");
        
        if (_webApp != null)
        {
            await _webApp.StopAsync(cancellationToken);
            await _webApp.DisposeAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}

public class DocumentIndexingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentIndexingService> _logger;

    public DocumentIndexingService(IServiceProvider serviceProvider, ILogger<DocumentIndexingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document indexing service started");
        
        while (!stoppingToken.IsCancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var indexer = scope.ServiceProvider.GetRequiredService<IDocumentIndexer>();
                
                await indexer.ProcessPendingDocumentsAsync(stoppingToken);
                
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in document indexing service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        
        _logger.LogInformation("Document indexing service stopped");
    }
}
```

### 5. Aspire Host Project (Owlet.AppHost)

Development orchestration with Aspire dashboard for enhanced development experience.

**Owlet.AppHost.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Owlet.ServiceDefaults\Owlet.ServiceDefaults.csproj" />
    <ProjectReference Include="..\Owlet.Api\Owlet.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.0.0" />
  </ItemGroup>

</Project>
```

**Aspire Host Implementation:**
```csharp
namespace Owlet.AppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Add SQLite database (file-based for development)
        var database = builder.AddSqlite("owletdb")
            .WithDataVolume();

        // Add the main API service
        var api = builder.AddProject<Projects.Owlet_Api>("owlet-api")
            .WithReference(database)
            .WithExternalHttpEndpoints();

        // Add health checks dashboard
        builder.AddProject<Projects.Owlet_Diagnostics>("owlet-diagnostics")
            .WithReference(api);

        builder.Build().Run();
    }
}
```

### 6. Service Defaults (Owlet.ServiceDefaults)

Shared Aspire configuration and service registration extensions.

**Owlet.ServiceDefaults.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
  </ItemGroup>

</Project>
```

**Service Registration Extensions:**
```csharp
namespace Owlet.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

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
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
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

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
```

## Build Configuration

### 1. Solution-Level Configuration

Central package management and common build properties.

**Directory.Build.props:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Missing XML comment -->
    
    <!-- Assembly information -->
    <Product>Owlet Document Indexing Service</Product>
    <Company>Owlet</Company>
    <Copyright>© 2025 Owlet. All rights reserved.</Copyright>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <InformationalVersion>1.0.0-dev</InformationalVersion>
  </PropertyGroup>

  <!-- Production deployment settings -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release' AND '$(MSBuildProjectName)' == 'Owlet.Service'">
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishTrimmed>false</PublishTrimmed>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <DebugType>embedded</DebugType>
    <DebugSymbols>true</DebugSymbols>
  </PropertyGroup>

  <!-- Code analysis -->
  <PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <!-- Package references for all projects -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  </ItemGroup>

</Project>
```

**Directory.Packages.props:**
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Microsoft packages -->
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="9.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
    
    <!-- Third-party packages -->
    <PackageVersion Include="Carter" Version="8.2.1" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.EventLog" Version="4.0.0" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="6.8.1" />
    
    <!-- Aspire packages -->
    <PackageVersion Include="Aspire.Hosting.AppHost" Version="9.0.0" />
    <PackageVersion Include="Aspire.Hosting" Version="9.0.0" />
    
    <!-- Testing packages -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
    <PackageVersion Include="Testcontainers" Version="3.10.0" />
  </ItemGroup>

</Project>
```

### 2. EditorConfig

Code style and formatting rules.

**.editorconfig:**
```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx,vb,vbx}]
indent_style = space
indent_size = 4

[*.{json,js,yml,yaml}]
indent_style = space
indent_size = 2

[*.md]
trim_trailing_whitespace = false

# C# code style rules
[*.cs]
# Organize usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# this. preferences
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:silent
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:silent

# Modifier preferences
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion

# Expression-level preferences
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_auto_properties = true:silent
dotnet_style_prefer_conditional_expression_over_assignment = true:silent
dotnet_style_prefer_conditional_expression_over_return = true:silent

# C# preferences
csharp_prefer_simple_using_statement = true:suggestion
csharp_prefer_braces = true:silent
csharp_style_namespace_declarations = file_scoped:suggestion
csharp_style_prefer_method_group_conversion = true:silent
csharp_style_prefer_top_level_statements = false:silent
csharp_style_expression_bodied_methods = false:silent
csharp_style_expression_bodied_constructors = false:silent
csharp_style_expression_bodied_operators = false:silent
csharp_style_expression_bodied_properties = true:silent
csharp_style_expression_bodied_indexers = true:silent
csharp_style_expression_bodied_accessors = true:silent
csharp_style_expression_bodied_lambdas = true:silent
csharp_style_expression_bodied_local_functions = false:silent

# Pattern matching preferences
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion

# Null-checking preferences
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:suggestion

# Modifier preferences
csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async:suggestion

# Code-block preferences
csharp_prefer_simple_default_expression = true:suggestion
csharp_style_deconstructed_variable_declaration = true:suggestion
csharp_style_pattern_local_over_anonymous_function = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion

# New line preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_parentheses = false
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_binary_operators = before_and_after
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false

# Wrapping preferences
csharp_preserve_single_line_statements = true
csharp_preserve_single_line_blocks = true

# Naming conventions
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = suggestion
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = prefix_interface_with_i

dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case

dotnet_naming_rule.non_field_members_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.non_field_members_should_be_pascal_case.symbols = non_field_members
dotnet_naming_rule.non_field_members_should_be_pascal_case.style = pascal_case

# Symbol specifications
dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected

dotnet_naming_symbols.types.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.types.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected

dotnet_naming_symbols.non_field_members.applicable_kinds = property, event, method
dotnet_naming_symbols.non_field_members.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected

# Naming styles
dotnet_naming_style.pascal_case.capitalization = pascal_case

dotnet_naming_style.prefix_interface_with_i.capitalization = pascal_case
dotnet_naming_style.prefix_interface_with_i.required_prefix = I
```

## Success Criteria

- [ ] Complete solution structure created with all projects properly configured
- [ ] Clean architecture dependency flow implemented (Core → Application → Infrastructure → Composition)
- [ ] Dual deployment scenarios supported (Windows service and Aspire development)
- [ ] Entity Framework Core with SQLite integration configured
- [ ] Carter-based HTTP API with OpenAPI documentation
- [ ] Comprehensive logging with Serilog integration
- [ ] Configuration system with validation and environment overrides
- [ ] Health checks implemented for all components
- [ ] Build system supports both development and production deployment
- [ ] All projects compile without errors or warnings
- [ ] EditorConfig enforces consistent code style
- [ ] Central package management configured

## Testing Strategy

### Unit Tests
**What to test:** Domain logic, configuration validation, API endpoints  
**Mocking strategy:** NSubstitute for external dependencies  
**Test data approach:** Builder pattern for complex domain objects

**Example Architecture Test:**
```csharp
[Fact]
public void Core_ShouldNotDependOnInfrastructure()
{
    // Arrange
    var coreAssembly = typeof(Document).Assembly;
    var infrastructureAssembly = typeof(OwletDbContext).Assembly;
    
    // Act
    var dependencies = coreAssembly.GetReferencedAssemblies()
        .Select(a => a.Name);
    
    // Assert
    dependencies.Should().NotContain(infrastructureAssembly.GetName().Name,
        "Core should not depend on Infrastructure");
}

[Fact]
public void Api_ShouldOnlyDependOnCore()
{
    // Arrange
    var apiAssembly = typeof(DocumentEndpoints).Assembly;
    var infrastructureAssembly = typeof(OwletDbContext).Assembly;
    
    // Act
    var dependencies = apiAssembly.GetReferencedAssemblies()
        .Select(a => a.Name);
    
    // Assert
    dependencies.Should().NotContain(infrastructureAssembly.GetName().Name,
        "API should not directly depend on Infrastructure");
}
```

### Integration Tests
**What to test:** Service composition, database integration, HTTP API endpoints  
**Test environment:** In-memory database for fast tests  
**Automation:** TestServer for HTTP API testing

### E2E Tests
**What to test:** Complete service startup and health check response  
**User workflows:** Service start → Configuration load → HTTP endpoint availability → Health check

## Dependencies

### Technical Dependencies
- Microsoft.Extensions.Hosting.WindowsServices 9.0.0 - Windows service hosting
- Carter 8.2.1 - HTTP API framework
- Microsoft.EntityFrameworkCore.Sqlite 9.0.0 - Database access
- Serilog.Extensions.Hosting 8.0.0 - Logging framework
- Aspire.Hosting.AppHost 9.0.0 - Development orchestration

### Story Dependencies
- **Blocks:** S30 (Core Infrastructure), S40 (Build Pipeline), S50 (WiX Installer)
- **Blocked By:** S10 (Analysis & Requirements)

## Next Steps

1. Create all project files and establish solution structure
2. Implement core domain models and interfaces
3. Set up Entity Framework Core with initial migration
4. Implement Carter API endpoints with health checks
5. Configure dual hosting scenarios (service and Aspire)
6. Validate build system produces correct outputs for both scenarios

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** [Date] (when status = Complete)