# Owlet Coding Standards

## Overview

This document defines the coding standards and patterns for the Owlet project. These standards ensure consistency, maintainability, and alignment with the project's functional programming approach.

## Functional Programming Patterns

### Result Types for Error Handling
Use Result types instead of exceptions for expected error conditions:

```csharp
// ✅ Good - Explicit error handling
public static async Task<Result<SearchResults>> SearchAsync(SearchQuery query) =>
    await query
        .Validate()
        .ToAsync()
        .Bind(ValidatedQuery => repository.SearchAsync(ValidatedQuery))
        .Map(ApplyRelevanceRanking)
        .Bind(EnrichWithMetadata);

// ❌ Avoid - Exception-based flow control
public async Task<SearchResults> SearchAsync(SearchQuery query)
{
    if (string.IsNullOrEmpty(query.Text))
        throw new ArgumentException("Query cannot be empty");
    
    return await repository.SearchAsync(query);
}
```

### Immutable Domain Models
Use records for domain models with immutable properties:

```csharp
// ✅ Good - Immutable record with validation
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

// ❌ Avoid - Mutable class
public class IndexedFile
{
    public int Id { get; set; }
    public string Path { get; set; }
    public List<string> Tags { get; set; }
}
```

### Algebraic Data Types
Use discriminated unions for modeling domain states:

```csharp
// ✅ Good - Explicit state modeling
public abstract record FileProcessingResult
{
    public sealed record Success(IndexedFile File) : FileProcessingResult;
    public sealed record Failure(FilePath Path, ProcessingError Error) : FileProcessingResult;
    public sealed record Skipped(FilePath Path, SkipReason Reason) : FileProcessingResult;
    public sealed record LockedFile(UnreadableFile File) : FileProcessingResult;
}

// Pattern matching usage
var result = await processor.ProcessFileAsync(path);
return result switch
{
    FileProcessingResult.Success(var file) => Ok(file),
    FileProcessingResult.Failure(_, var error) => BadRequest(error),
    FileProcessingResult.Skipped(_, var reason) => Accepted(reason),
    FileProcessingResult.LockedFile(var unreadable) => Conflict(unreadable)
};
```

### Monadic Composition
Chain operations using Bind and Map for clean composition:

```csharp
// ✅ Good - Monadic pipeline
public async Task<Result<ProcessedFile>> ProcessFileAsync(FilePath path) =>
    await path
        .ValidateExists()
        .ToAsync()
        .Bind(ExtractContentAsync)
        .Bind(ClassifyFileAsync)
        .Map(ApplyTags)
        .Bind(StoreInIndexAsync);

// ❌ Avoid - Nested try-catch
public async Task<ProcessedFile> ProcessFileAsync(string path)
{
    try
    {
        var content = await ExtractContentAsync(path);
        try
        {
            var classified = await ClassifyFileAsync(content);
            return await StoreInIndexAsync(classified);
        }
        catch (ClassificationException ex)
        {
            // Handle classification error
        }
    }
    catch (ExtractionException ex)
    {
        // Handle extraction error
    }
}
```

## API Design Patterns

### Carter Module Organization
Organize API endpoints using Carter modules with clear responsibilities:

```csharp
// ✅ Good - Focused module with functional composition
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
```

### Configuration Patterns
Use strongly-typed configuration with validation:

```csharp
// ✅ Good - Validated configuration record
public sealed record IndexingConfiguration(
    PositiveInteger MaxFileSizeMB,
    NonEmptyList<FileExtension> SupportedExtensions,
    NonEmptyList<FolderName> ExcludedFolders,
    PositiveInteger RetryAttempts,
    PositiveInteger RetryDelayMs
) : IValidatable<IndexingConfiguration>
{
    public static Validation<IndexingConfiguration> Create(IConfiguration config) =>
        (
            PositiveInteger.Create(config.GetValue<int>("MaxFileSizeMB")),
            NonEmptyList<FileExtension>.Create(config.GetSection("SupportedExtensions")),
            NonEmptyList<FolderName>.Create(config.GetSection("ExcludedFolders")),
            PositiveInteger.Create(config.GetValue<int>("RetryAttempts")),
            PositiveInteger.Create(config.GetValue<int>("RetryDelayMs"))
        ).Apply((max, ext, excl, retry, delay) => 
            new IndexingConfiguration(max, ext, excl, retry, delay));
}
```

## Error Handling Standards

### Structured Error Types
Define specific error types for different failure modes:

```csharp
// ✅ Good - Specific error types
public abstract record ProcessingError
{
    public sealed record FileNotFound(FilePath Path) : ProcessingError;
    public sealed record ExtractionFailed(FilePath Path, string Reason) : ProcessingError;
    public sealed record ValidationFailed(ValidationErrors Errors) : ProcessingError;
    public sealed record DatabaseError(string Message, Exception? Inner = null) : ProcessingError;
}

// Extension method for problem details
public static class ErrorExtensions
{
    public static ValidationProblem ToValidationProblem(this ProcessingError error) =>
        error switch
        {
            ProcessingError.FileNotFound(var path) => 
                new ValidationProblem("File not found", $"The file {path} does not exist"),
            ProcessingError.ExtractionFailed(var path, var reason) => 
                new ValidationProblem("Extraction failed", $"Could not extract content from {path}: {reason}"),
            ProcessingError.ValidationFailed(var errors) => 
                new ValidationProblem("Validation failed", errors.ToString()),
            ProcessingError.DatabaseError(var message, _) => 
                new ValidationProblem("Database error", message),
            _ => new ValidationProblem("Unknown error", "An unexpected error occurred")
        };
}
```

### Retry Patterns
Implement retry logic for transient failures:

```csharp
// ✅ Good - Explicit retry with backoff
public async Task<FileProcessingResult> IndexFileAsync(FilePath path)
{
    const int maxRetries = 3;
    const int baseDelayMs = 1000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var content = await _extractorService.ExtractContentAsync(path);
            var indexedFile = await _repository.StoreFileAsync(path, content);
            
            await _eventService.PublishAsync(new FileIndexedEvent(indexedFile));
            
            return FileProcessingResult.Success(indexedFile);
        }
        catch (IOException ex) when (IsFileLockException(ex) && attempt < maxRetries)
        {
            _logger.LogWarning("File {Path} is locked, attempt {Attempt}/{MaxRetries}", 
                path, attempt, maxRetries);
            
            await Task.Delay(baseDelayMs * attempt, _cancellationToken);
            continue;
        }
        catch (IOException ex) when (IsFileLockException(ex))
        {
            return FileProcessingResult.LockedFile(
                await _repository.StoreUnreadableFileAsync(path, ex.Message));
        }
    }

    return FileProcessingResult.Failure(path, ProcessingError.UnexpectedRetryLoop);
}
```

## Service Registration Patterns

### Extension Methods for Clean Configuration
Use extension methods to organize service registration:

```csharp
// ✅ Good - Organized extension methods
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOwletCore(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddValidatedConfiguration<OwletConfiguration>(configuration)
            .AddSingleton<IClock, SystemClock>()
            .AddSingleton<IFileIdGenerator, GuidFileIdGenerator>();

    public static IServiceCollection AddOwletIndexing(
        this IServiceCollection services) =>
        services
            .AddSingleton<IFileWatcher, FileWatcherService>()
            .AddSingleton<IFileProcessor, FileProcessor>()
            .AddHostedService<IndexingService>();

    public static IServiceCollection AddOwletApi(
        this IServiceCollection services) =>
        services
            .AddCarter()
            .AddProblemDetails()
            .Configure<ProblemDetailsOptions>(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                };
            });
}
```

## Logging Standards

### Structured Logging with Context
Use structured logging with meaningful context:

```csharp
// ✅ Good - Structured logging with context
public async Task<FileProcessingResult> ProcessFileAsync(FilePath path)
{
    using var activity = _logger.BeginScope(new Dictionary<string, object>
    {
        ["FilePath"] = path.Value,
        ["Operation"] = "ProcessFile"
    });

    _logger.LogInformation("Starting file processing for {FilePath}", path);

    try
    {
        var result = await ProcessFileInternal(path);
        
        _logger.LogInformation("File processing completed with result {ResultType}", 
            result.GetType().Name);
        
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "File processing failed for {FilePath}", path);
        return FileProcessingResult.Failure(path, ProcessingError.From(ex));
    }
}

// ❌ Avoid - String interpolation in logs
_logger.LogInformation($"Processing file: {path}");
```

## Testing Patterns

### Functional Test Organization
Organize tests around behavior and outcomes:

```csharp
// ✅ Good - Behavior-focused test
[Fact]
public async Task SearchAsync_WithValidQuery_ReturnsMatchingFiles()
{
    // Arrange
    var query = SearchQuery.Create("test document", SearchFilters.None).Value;
    var expectedFiles = new[] { CreateTestFile("test-document.txt") };
    
    _mockRepository
        .Setup(r => r.SearchAsync(query, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Success(new SearchResults(expectedFiles, 1, "test document")));

    // Act
    var result = await _searchService.SearchAsync(query);

    // Assert
    result.Should().BeSuccess()
        .Which.Files.Should().HaveCount(1)
        .And.Contain(f => f.Name.Contains("test-document"));
}

// ✅ Good - Property-based testing for validation
[Property]
public void SearchQuery_Create_WithEmptyText_ReturnsFailure(NonEmptyString text)
{
    // Act
    var result = SearchQuery.Create("", SearchFilters.None);

    // Assert
    result.Should().BeFailure()
        .Which.Should().BeOfType<ValidationError>();
}
```

## File Organization

### Project Structure Standards
Follow the established solution structure:

```
src/
├── Owlet.Core/           # Domain models, interfaces, business logic
│   ├── Domain/           # Domain entities and value objects
│   ├── Services/         # Business services interfaces
│   └── Extensions/       # Core extension methods
├── Owlet.Api/            # Carter modules and API concerns
│   ├── Modules/          # Carter modules grouped by feature
│   ├── Validation/       # Request validation
│   └── Extensions/       # API-specific extensions
├── Owlet.Infrastructure/ # External concerns and implementations
│   ├── Data/             # EF Core contexts and repositories
│   ├── Services/         # Service implementations
│   └── Configuration/    # Configuration and DI setup
```

### Naming Conventions
- **Interfaces**: Start with `I` (e.g., `ISearchService`)
- **Records**: Use noun phrases (e.g., `IndexedFile`, `SearchQuery`)
- **Result types**: Use descriptive names (e.g., `FileProcessingResult`)
- **Extension methods**: Group in static classes ending with `Extensions`
- **Carter modules**: End with `Module` (e.g., `SearchModule`)

## Performance Considerations

### Async Patterns
Use async/await properly with cancellation tokens:

```csharp
// ✅ Good - Proper async with cancellation
public async Task<Result<SearchResults>> SearchAsync(
    SearchQuery query,
    CancellationToken cancellationToken = default) =>
    await _repository
        .SearchAsync(query, cancellationToken)
        .ConfigureAwait(false);

// ❌ Avoid - Blocking async calls
public SearchResults Search(SearchQuery query) =>
    SearchAsync(query).Result;
```

### Memory Management
Use appropriate collection types and dispose patterns:

```csharp
// ✅ Good - Immutable collections for domain
public sealed record SearchResults(
    ImmutableList<SearchResultItem> Files,
    int TotalCount,
    string Query
);

// ✅ Good - Proper disposal
public sealed class FileProcessor : IFileProcessor, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}
```

---

## Reference Documents

For additional context, always consult:
- **Technical Specification**: `c:\code\owlet\project\spec\owlet-specification.md`
- **Architecture Decisions**: `c:\code\owlet\project\adr\*.md`
- **API Documentation**: Generated OpenAPI docs from Carter modules

---

*Last updated: November 1, 2025*
*Follows principles established in ADR-003: Functional Programming and Modern C# Patterns*