# ADR-003: Functional Programming and Modern C# Patterns

## Status
Accepted

## Date
2025-11-01

## Context

The initial specification suggested that code should be "clean and well-commented so a non-nerd can follow the Api project." This led to discussion about code complexity and whether to use simple, procedural patterns or more advanced C# language features.

Upon clarification, we learned that:
- The code itself will not be read by non-technical users
- The application needs to be installable and usable by non-technical users
- The codebase should use modern, sophisticated C# patterns for maintainability and robustness
- Professional software engineering practices are preferred over simplified code

## Decision

We will embrace modern C# functional programming patterns and sophisticated language features throughout the codebase:

### Functional Programming Patterns
- **Immutable Records**: Use record types for domain models and data transfer objects
- **Option/Maybe Types**: Use `Option<T>` for nullable values to eliminate null reference exceptions
- **Result Types**: Use `Result<T>` for error handling without exceptions in business logic
- **Monadic Composition**: Chain operations using `Map`, `Bind`, and `Match` for clean error handling
- **Pure Functions**: Prefer pure functions with clear inputs and outputs where possible

### Modern C# Language Features
- **Pattern Matching**: Use switch expressions and pattern matching for control flow
- **Algebraic Data Types**: Model domain concepts with discriminated unions using abstract records
- **Async/Await**: Async-first design throughout the application
- **LINQ and Functional Collections**: Use immutable collections and functional composition
- **Generic Constraints**: Leverage advanced generics for type safety

### Error Handling Strategy
- **Railway-Oriented Programming**: Use Result types for error handling in business logic
- **Polly for Resilience**: Circuit breakers, retry policies, and timeout handling
- **Structured Exceptions**: Clear exception hierarchy for unrecoverable errors
- **Validation Types**: Strong typing with validation constraints (e.g., `NonEmptyString`, `PositiveInteger`)

### Code Organization
- **Functional Composition**: Small, composable functions over large classes
- **Carter for APIs**: Functional HTTP API composition
- **Dependency Injection**: Constructor injection with functional service composition
- **Extension Methods**: Fluent APIs for configuration and service registration

## Consequences

### Positive
- **Robustness**: Functional patterns reduce bugs and improve reliability
- **Maintainability**: Immutable data structures and pure functions are easier to reason about
- **Type Safety**: Strong typing with validation prevents runtime errors
- **Testability**: Pure functions and dependency injection make testing straightforward
- **Modern Codebase**: Attracts skilled developers and demonstrates technical excellence
- **Error Handling**: Explicit error handling improves reliability and debugging

### Negative
- **Learning Curve**: Developers unfamiliar with functional patterns need training
- **Complexity**: More abstract patterns compared to simple object-oriented code
- **Performance**: Some functional patterns may have slight performance overhead
- **Debugging**: Functional composition can be harder to debug with traditional tools

## Alternatives Considered

### Simple Object-Oriented Patterns
- **Pros**: Familiar to most C# developers, easier to understand for beginners
- **Cons**: More prone to null reference exceptions, mutable state bugs, less robust error handling

### Traditional Exception-Based Error Handling
- **Pros**: Standard .NET pattern, familiar to all developers
- **Cons**: Hidden control flow, difficult to track error paths, poor composability

### Minimal Functional Patterns
- **Pros**: Gradual adoption, easier migration from traditional patterns
- **Cons**: Inconsistent patterns, misses benefits of full functional approach

## Implementation Examples

### Domain Modeling
```csharp
// Immutable domain model with validation
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

// Algebraic data type for processing results
public abstract record FileProcessingResult
{
    public sealed record Success(IndexedFile File) : FileProcessingResult;
    public sealed record Failure(FilePath Path, ProcessingError Error) : FileProcessingResult;
    public sealed record Skipped(FilePath Path, SkipReason Reason) : FileProcessingResult;
}
```

### Error Handling
```csharp
// Railway-oriented programming for search pipeline
public static Task<Result<SearchResults>> ExecuteAsync(
    SearchQuery query,
    ISearchRepository repository) =>
    query
        .Validate()
        .ToAsync()
        .Bind(validatedQuery => repository.SearchAsync(validatedQuery))
        .Map(ApplyRelevanceRanking)
        .Map(ApplyResultLimits)
        .Bind(EnrichWithMetadata);
```

### API Composition
```csharp
// Functional API composition with Carter
public sealed class SearchModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", SearchFilesAsync)
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

## Quality Standards

- Use nullable reference types throughout the codebase
- Implement comprehensive unit tests for all business logic
- Use static analysis tools to enforce functional patterns
- Document complex functional compositions with XML comments
- Provide extension methods for common monadic operations