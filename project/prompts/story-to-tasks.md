# Story-to-Tasks Prompt

**Purpose:** Convert a Story README.md into Task-level breakdown. Each Task represents an atomic, executable unit of work (typically 8-16 hours) with complete implementation instructions, code examples, and acceptance criteria.

**Invocation:** "Break down {epicPrefix}05 {storyPrefix}150 into tasks" or "Create tasks for Story {storyPrefix}40 Infrastructure"

---

## Step 0: Load Project Parameters

**CRITICAL: Read project parameters first to configure this prompt for the current project.**

```bash
# Read project-specific configuration
cat .github/copilot/prompts/project-parameters.json
```

**Extract and use these parameters throughout:**
- `paths.plan` - Where story folders exist
- `paths.sourceCode` - Source code location
- `paths.tests` - Test code location
- `naming.taskPrefix` - Task prefix (default: `T`)
- `naming.numberingGap` - Numbering gap (default: `10`)
- `naming.taskFormat` - Task file format
- `technology.*` - Tech stack for code examples
- `patterns.*` - Coding patterns to follow
- `namespace.*` - Namespace structure
- `taskBreakdown.lineThreshold` - When to create task breakdown (default: 1000 lines)
- `taskBreakdown.taskEffortRange` - Effort range per task
- `testingStandards.*` - Testing requirements
- `commitConventions.taskFormat` - Commit message format

**If project-parameters.json not found, use defaults.**

---

## Prerequisites

Before starting, the agent must have:
1. **Project parameters loaded** (from project-parameters.json)
2. Path to story README.md (from `{paths.plan}/{epicPrefix}XX-epic/{storyPrefix}YY-story/README.md`)
3. Project coding standards (from `{paths.instructions}` parameter)
4. Understanding of layers/modules (from `namespace.*` parameters)

---

## Execution Sequence

### Step 1: Context Gathering

**Read these files in this order:**

1. **Story README.md** (REQUIRED)
   - Location: `.github/copilot/plan/E0X-epic/SYY-story/README.md`
   - Extract: Objective, components to implement, code examples, success criteria
   - Determine: If story naturally breaks into tasks (>1000 lines OR high complexity OR S150 Gap Analysis)

2. **Project Instructions** (REQUIRED)
   - Location: `.github/copilot-instructions.md`
   - Extract: Coding patterns, testing standards, file organization, layer structure
   - Understand: How to organize task files, what patterns to reference

3. **Epic README.md** (CONTEXT)
   - Location: `.github/copilot/plan/E0X-epic/README.md`
   - Extract: Epic-level domain model, business rules, technical stack
   - Understand: Broader context for tasks

4. **Existing Tasks** (CHECK FOR IDEMPOTENCY)
   ```bash
   ls .github/copilot/plan/E05-billing/S40-infrastructure/
   ```
   - Output: README.md, SHARED_CONTEXT.md, T10-repositories.md, T20-ef-core.md, etc.
   - Decision: **Skip tasks that already exist**, only create missing ones

5. **Related Story Files** (OPTIONAL - for SHARED_CONTEXT)
   ```bash
   # Check if earlier stories have SHARED_CONTEXT that can be referenced
   ls .github/copilot/plan/E05-billing/S30-domain-layer/SHARED_CONTEXT.md
   ```

### Step 2: Determine If Task Breakdown Needed

**Create task breakdown if:**
- ✅ Story README.md exceeds `{taskBreakdown.lineThreshold}` lines (default: 1000 lines - too large for atomic execution)
- ✅ Story is {storyPrefix}150 Gap Analysis (if `{taskBreakdown.gapAnalysisAlwaysBreakdown}` is true)
- ✅ Story spans multiple concerns (frontend + backend + database)
- ✅ Multiple engineers need to work in parallel
- ✅ User explicitly requests: "Break down {storyPrefix}YY into tasks"

**Skip task breakdown if:**
- ❌ Story is <1000 lines and follows established patterns
- ❌ Story is S10 Analysis or S20 Design (research/documentation, not implementation)
- ❌ Story is simple CRUD following template (can execute directly from story README)
- ❌ Single engineer, sequential work, 1-2 days effort

**If task breakdown not needed:**
```
Agent: "S40 Infrastructure Layer is 850 lines with established patterns (EF Core, repositories).
Task breakdown not required - story README provides sufficient implementation guidance.
Developers can execute directly from story README.

If you still want task breakdown, confirm: 'Yes, create tasks for S40'"
```

### Step 3: Determine Task Pattern

**Task patterns vary by story type:**

#### S150 Gap Analysis (Legacy Migration) - ALWAYS 10-15 tasks
1. **Domain Layer Tasks (T10-T30):** Aggregates, Value Objects, Domain Events
2. **Infrastructure Layer Tasks (T40-T60):** EF Core, Repositories, External Adapters
3. **Application Layer Tasks (T70-T90):** Command Handlers, Query Handlers, DTOs
4. **API Layer Tasks (T100-T120):** Controllers, Background Jobs, Integration Tests

#### S30-S60 Core Implementation Stories - 4-8 tasks (if breakdown needed)
**Example: S40 Infrastructure Layer (1580 lines) breaks into:**
1. **T10: EF Core DbContext & Configurations** (12h) - DbContext, 11 entity configurations
2. **T20: Repository Pattern** (10h) - 6 repository interfaces + implementations
3. **T30: Payment Gateway Adapters** (8h) - Stripe + Razorpay adapters
4. **T40: Caching Layer** (6h) - Redis cache for read-heavy queries

#### S70-S110 Feature Stories - 3-6 tasks (if breakdown needed)
**Example: S80 Invoicing & Billing Cycles (1417 lines) breaks into:**
1. **T10: Domain Extensions** (10h) - Invoice aggregate methods, billing cycle logic
2. **T20: Infrastructure** (8h) - Invoice repository, PDF generation service
3. **T30: Application Layer** (12h) - GenerateInvoice, SendInvoice commands
4. **T40: Background Jobs** (10h) - RecurringBillingJob, InvoiceReminderJob
5. **T50: API & Testing** (8h) - Invoice endpoints, E2E tests

#### S120-S140 Quality Stories - 3-5 tasks (if breakdown needed)
**Example: S120 Integration Testing (1389 lines) breaks into:**
1. **T10: Test Infrastructure** (8h) - Testcontainers, IntegrationTestBase
2. **T20: Workflow Tests** (10h) - Subscription lifecycle, payment flow E2E tests
3. **T30: Performance Tests** (8h) - k6 load tests with stages and thresholds

#### Frontend Stories - 4-6 tasks (if breakdown needed)
**Example: S30 Dashboard Page (800 lines) breaks into:**
1. **T10: Layout & Navigation** (6h) - Dashboard shell, sidebar, header
2. **T20: KPI Cards** (8h) - Metric display components with real-time updates
3. **T30: Charts & Visualizations** (10h) - Revenue chart, user growth chart (Recharts)
4. **T40: Data Fetching & State** (8h) - React Query hooks, loading/error states

### Step 4: Check Existing Tasks (Idempotency)

**For each task in the determined pattern:**

```bash
# Check if task file already exists
ls .github/copilot/plan/E05-billing/S40-infrastructure/T10-ef-core.md

# If file exists -> SKIP this task
# If file missing -> CREATE this task
```

**Numbering rules:**
- Use semantic numbering with gaps of 10: T10, T20, T30, T40...
- If adding new tasks later: Find highest existing number (e.g., T80), add new as T90, T100...
- This allows interleaving (if T20 needs splitting, add T21, T22 between T20 and T30)

**Skip task if:**
- Task file exists: `.github/copilot/plan/E05-billing/S40-infrastructure/T10-ef-core.md`
- Task status is "Complete" in file header

### Step 5: Create SHARED_CONTEXT.md (if needed)

**SHARED_CONTEXT.md provides common patterns/code that all tasks reference.**

**Create if:**
- Multiple tasks need same imports, base classes, configuration
- Project has specific patterns not obvious from code (Result<T>, MediatR, etc.)
- Layer-specific guidance needed (Domain purity, Infrastructure I/O, etc.)

**Skip if:**
- Tasks are simple and self-contained
- Project patterns already well-documented in `.github/copilot-instructions.md`
- Adding to existing story that already has SHARED_CONTEXT

**SHARED_CONTEXT.md structure:**

```markdown
# Shared Context: E05 S40 Infrastructure Layer

**Purpose:** Common patterns, imports, and guidance for all Infrastructure Layer tasks (T10-T40).

## Project Structure

```
src/Notebook.School.Billing.Infrastructure/
├── Persistence/
│   ├── BillingDbContext.cs
│   ├── Configurations/
│   │   ├── PackageConfiguration.cs
│   │   ├── SubscriptionConfiguration.cs
│   │   └── ...
│   └── Migrations/
├── Repositories/
│   ├── PackageRepository.cs
│   ├── SubscriptionRepository.cs
│   └── ...
└── Adapters/
    ├── StripePaymentGateway.cs
    └── RazorpayPaymentGateway.cs
```

## Common Imports

```csharp
// All EF Core configuration classes
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notebook.School.Billing.Domain.Aggregates;
using Notebook.School.Billing.Domain.ValueObjects;

// All repository implementations
using Microsoft.EntityFrameworkCore;
using Notebook.School.Billing.Application.Interfaces;
using Notebook.School.Billing.Domain.Aggregates;
using Notebook.School.Common.Results;
```

## Common Patterns

### Repository Method Returning Result<T>

```csharp
public async Task<Result<Package>> GetByIdAsync(Guid id, CancellationToken ct)
{
    try
    {
        var package = await _context.Packages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        
        return package is null
            ? Result<Package>.Failure($"Package {id} not found")
            : Result<Package>.Success(package);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve package {PackageId}", id);
        return Result<Package>.Failure("Database error retrieving package");
    }
}
```

### Repository Method Returning Result<PagedResult<T>>

```csharp
public async Task<Result<PagedResult<Package>>> GetPagedAsync(
    int page, int pageSize, CancellationToken ct)
{
    try
    {
        var totalCount = await _context.Packages.CountAsync(ct);
        var items = await _context.Packages
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        var pagedResult = new PagedResult<Package>(items, totalCount, page, pageSize);
        return Result<PagedResult<Package>>.Success(pagedResult);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve paged packages");
        return Result<PagedResult<Package>>.Failure("Database error retrieving packages");
    }
}
```

### EF Core Entity Configuration Pattern

```csharp
public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("Packages");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        // Owned entity for value object
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            price.Property(m => m.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });
        
        builder.HasIndex(p => p.Name);
    }
}
```

## NuGet Packages (Infrastructure Layer)

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  <PackageReference Include="Stripe.net" Version="43.0.0" />
  <PackageReference Include="Razorpay" Version="3.1.0" />
</ItemGroup>
```

## Testing Standards

### Repository Unit Test Pattern

```csharp
public class PackageRepositoryTests : IAsyncLifetime
{
    private readonly BillingDbContext _context;
    private readonly PackageRepository _repository;
    
    public PackageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _context = new BillingDbContext(options);
        _repository = new PackageRepository(_context);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithExistingPackage_ShouldReturnSuccess()
    {
        // Arrange
        var package = new Package { Id = Guid.NewGuid(), Name = "Premium" };
        await _context.Packages.AddAsync(package);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByIdAsync(package.Id, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(package.Id);
    }
    
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _context.DisposeAsync().AsTask();
}
```

## Business Constants

```csharp
// From Domain Layer - reference in Infrastructure queries
public static class BillingConstants
{
    public const int MaxPackagesPerPage = 50;
    public const int DefaultPageSize = 20;
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;
}
```

---
**Context Last Updated:** [Date]
```

### Step 6: Task File Structure

**Each TASK-XX-[name].md contains:**

```markdown
# E0X SYY TZZ: [Task Name]

**Effort:** X hours
**Priority:** Critical/High/Medium/Low
**Assignee:** Engineer 1/Engineer 2 (optional, can be assigned during sprint planning)
**Dependencies:** TXX, TYY (list tasks that must complete first) or None
**Status:** Not Started/In Progress/Complete

## Objective

[1-2 sentences: What this task accomplishes]

## Context

[2-3 paragraphs: Why this task matters, how it fits in the story/epic, architectural considerations]

**References:**
- **Story:** [Link or path to SYY-story/README.md]
- **Epic:** [Link or path to E0X-epic/README.md]
- **Shared Context:** [Link to SHARED_CONTEXT.md if exists]

## Acceptance Criteria

- [ ] [Specific, testable deliverable 1 with file path]
- [ ] [Specific, testable deliverable 2 with verification method]
- [ ] [Specific, testable deliverable 3 with success metric]
- [ ] Unit test coverage >80% for all new code
- [ ] Code compiles without errors or warnings
- [ ] All linting/formatting rules pass
- [ ] Code review completed (if team process requires)

## Implementation Guide

### 1. [Component Name - e.g., PackageRepository, BillingDbContext, StripeAdapter]

**File:** `src/Notebook.School.Billing.Infrastructure/Repositories/PackageRepository.cs`

**Purpose:** [What this component does, its responsibilities, why it's structured this way]

**Dependencies:**
- `Notebook.School.Billing.Domain.Aggregates` (Package aggregate)
- `Notebook.School.Billing.Application.Interfaces` (IPackageRepository interface)
- `Notebook.School.Common.Results` (Result<T> pattern)

**Implementation:**

```csharp
// COMPLETE, RUNNABLE CODE
// Include:
// - All using statements
// - Namespace
// - Full class definition with constructor (primary or traditional)
// - All methods with complete implementation (not pseudocode, not "TODO")
// - XML documentation comments for public methods
// - Following project patterns (Result<T>, async/await, cancellation tokens)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notebook.School.Billing.Application.Interfaces;
using Notebook.School.Billing.Domain.Aggregates;
using Notebook.School.Common.Results;

namespace Notebook.School.Billing.Infrastructure.Repositories;

/// <summary>
/// Repository for Package aggregate providing persistence operations.
/// </summary>
public class PackageRepository(BillingDbContext context, ILogger<PackageRepository> logger) 
    : IPackageRepository
{
    /// <summary>
    /// Retrieves a package by its unique identifier.
    /// </summary>
    public async Task<Result<Package>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var package = await context.Packages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);
            
            return package is null
                ? Result<Package>.Failure($"Package {id} not found")
                : Result<Package>.Success(package);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve package {PackageId}", id);
            return Result<Package>.Failure("Database error retrieving package");
        }
    }
    
    /// <summary>
    /// Retrieves packages with pagination support.
    /// </summary>
    public async Task<Result<PagedResult<Package>>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            
            var totalCount = await context.Packages.CountAsync(ct);
            var items = await context.Packages
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            
            var pagedResult = new PagedResult<Package>(items, totalCount, page, pageSize);
            return Result<PagedResult<Package>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve paged packages");
            return Result<PagedResult<Package>>.Failure("Database error");
        }
    }
    
    /// <summary>
    /// Adds a new package to the database.
    /// </summary>
    public async Task<Result<Package>> AddAsync(Package package, CancellationToken ct)
    {
        try
        {
            await context.Packages.AddAsync(package, ct);
            await context.SaveChangesAsync(ct);
            return Result<Package>.Success(package);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to add package {PackageId}", package.Id);
            return Result<Package>.Failure("Failed to save package");
        }
    }
    
    /// <summary>
    /// Updates an existing package in the database.
    /// </summary>
    public async Task<Result<Package>> UpdateAsync(Package package, CancellationToken ct)
    {
        try
        {
            context.Packages.Update(package);
            await context.SaveChangesAsync(ct);
            return Result<Package>.Success(package);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency conflict updating package {PackageId}", package.Id);
            return Result<Package>.Failure("Package was modified by another user");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to update package {PackageId}", package.Id);
            return Result<Package>.Failure("Failed to update package");
        }
    }
}
```

### 2. [Next Component]

**File:** [Path]

**Purpose:** [Description]

**Implementation:**

```csharp
// Repeat pattern with complete code
```

### 3. [Additional Components as Needed]

[Continue pattern for all components in this task]

## Unit Tests

**File:** `tests/Notebook.School.Billing.Infrastructure.Tests/Repositories/PackageRepositoryTests.cs`

**Purpose:** Verify repository operations with in-memory database.

**Implementation:**

```csharp
// COMPLETE TEST CLASS
// Include:
// - All using statements
// - Test class with IAsyncLifetime for setup/teardown
// - Arrange-Act-Assert pattern
// - FluentAssertions for readable assertions
// - Cover happy paths, edge cases, error scenarios

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Notebook.School.Billing.Domain.Aggregates;
using Notebook.School.Billing.Infrastructure.Persistence;
using Notebook.School.Billing.Infrastructure.Repositories;

namespace Notebook.School.Billing.Infrastructure.Tests.Repositories;

public sealed class PackageRepositoryTests : IAsyncLifetime
{
    private readonly BillingDbContext _context;
    private readonly PackageRepository _repository;
    
    public PackageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _context = new BillingDbContext(options);
        _repository = new PackageRepository(_context, NullLogger<PackageRepository>.Instance);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithExistingPackage_ShouldReturnSuccess()
    {
        // Arrange
        var package = new Package 
        { 
            Id = Guid.NewGuid(), 
            Name = "Premium Plan",
            Price = new Money(1000, "INR")
        };
        await _context.Packages.AddAsync(package);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByIdAsync(package.Id, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(package.Id);
        result.Value.Name.Should().Be("Premium Plan");
    }
    
    [Fact]
    public async Task GetByIdAsync_WithNonExistentPackage_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
    
    [Fact]
    public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResults()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            await _context.Packages.AddAsync(new Package 
            { 
                Id = Guid.NewGuid(), 
                Name = $"Package {i}",
                Price = new Money(100 * i, "INR")
            });
        }
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetPagedAsync(page: 2, pageSize: 10, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }
    
    [Fact]
    public async Task AddAsync_WithValidPackage_ShouldPersist()
    {
        // Arrange
        var package = new Package 
        { 
            Id = Guid.NewGuid(), 
            Name = "New Package",
            Price = new Money(500, "INR")
        };
        
        // Act
        var result = await _repository.AddAsync(package, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var retrieved = await _context.Packages.FindAsync(package.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("New Package");
    }
    
    public Task InitializeAsync() => Task.CompletedTask;
    
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
```

## Dependencies

### NuGet Packages (if new packages needed)
```xml
<PackageReference Include="PackageName" Version="X.Y.Z" />
```

### Task Dependencies
- **Must complete first:** T10 (needs DbContext), T20 (needs interfaces)
- **Can work in parallel with:** T30, T40
- **Blocks these tasks:** T50 (needs repositories to test)

## Verification Steps

1. **Build the project:**
   ```bash
   dotnet build src/Notebook.School.Billing.Infrastructure/
   # Should compile without errors
   ```

2. **Run unit tests:**
   ```bash
   dotnet test tests/Notebook.School.Billing.Infrastructure.Tests/ --filter "PackageRepositoryTests"
   # All tests should pass
   ```

3. **Check test coverage:**
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   # Coverage should be >80% for PackageRepository.cs
   ```

4. **Verify code quality:**
   ```bash
   dotnet format --verify-no-changes
   # No formatting issues
   ```

## Troubleshooting

### Common Issues

**Issue:** "Cannot resolve type 'BillingDbContext'"
**Solution:** Ensure T10 (EF Core DbContext) is completed first. Add project reference to Infrastructure project.

**Issue:** "Result<T> type not found"
**Solution:** Add reference to `Notebook.School.Common` project. Verify Result<T> class exists in Common/Results/.

**Issue:** "Test fails with 'Database error'"
**Solution:** Check if in-memory database is properly configured. Verify test constructor creates fresh DbContext per test.

## Next Steps

- After completing this task, proceed to T30 (Payment Gateway Adapters)
- Update SHARED_CONTEXT.md if new patterns emerged
- Update Story README if scope changed
- Mark task status as "Complete" in this file header

---

**Task Created:** [Date] by [GitHub Copilot Agent]
**Task Completed:** [Date] (when status = Complete)
**Completed By:** [Engineer Name] (when done)
```

### Step 7: Create Task Overview in Story README (Update)

**After creating all task files, update story README.md:**

Add this section near the end (before Success Criteria):

```markdown
## Task Breakdown

**Total Tasks:** 4
**Total Effort:** 36 hours

### Overview Table

| Task | Name | Effort | Priority | Status | Dependencies |
|------|------|--------|----------|--------|--------------|
| T10 | EF Core DbContext & Configurations | 12h | Critical | Not Started | None |
| T20 | Repository Pattern | 10h | Critical | Not Started | T10 |
| T30 | Payment Gateway Adapters | 8h | High | Not Started | T10 |
| T40 | Caching Layer | 6h | Medium | Not Started | T20 |

### Dependency Graph

```
T10 (EF Core)
 ├── T20 (Repositories)
 │    └── T40 (Caching)
 └── T30 (Payment Gateways)
```

### Implementation Sequence

**Sprint 1 - Week 1 (22 hours):**
- Day 1-2: T10 EF Core DbContext & Configurations (12h)
- Day 3: T20 Repository Pattern - Part 1 (5h)

**Sprint 1 - Week 2 (14 hours):**
- Day 4: T20 Repository Pattern - Part 2 (5h)
- Day 5: T30 Payment Gateway Adapters (8h)

**Sprint 2 - Week 3 (6 hours):**
- Day 6: T40 Caching Layer (6h)

### Files

- `README.md` - This overview document
- `SHARED_CONTEXT.md` - Common patterns for all tasks
- `T10-ef-core-configurations.md` - EF Core implementation
- `T20-repository-pattern.md` - Repository implementation
- `T30-payment-gateway-adapters.md` - External API adapters
- `T40-caching-layer.md` - Redis caching implementation
```

### Step 8: Create Task Files

**For each task:**

```bash
# Create task files in story directory
cd .github/copilot/plan/E05-billing/S40-infrastructure/

# Create SHARED_CONTEXT.md (once per story)
# Write content from Step 5

# Create task files
# T10-ef-core-configurations.md
# T20-repository-pattern.md
# T30-payment-gateway-adapters.md
# T40-caching-layer.md

# Commit in batches
git add SHARED_CONTEXT.md T10-ef-core-configurations.md
git commit -m "feat: E05 S40 task breakdown - Part 1 (SHARED_CONTEXT + T10)"

git add T20-repository-pattern.md T30-payment-gateway-adapters.md
git commit -m "feat: E05 S40 task breakdown - Part 2 (T20-T30)"

git add T40-caching-layer.md
git commit -m "feat: E05 S40 task breakdown - Part 3 (T40, complete)"
```

### Step 9: Idempotency Verification

**Before completing, verify:**

```bash
# Check what was created
ls .github/copilot/plan/E05-billing/S40-infrastructure/

# Expected output:
# README.md (updated with task breakdown section)
# SHARED_CONTEXT.md
# T10-ef-core-configurations.md
# T20-repository-pattern.md
# T30-payment-gateway-adapters.md
# T40-caching-layer.md
```

**Idempotency rules:**
- If task file exists -> **DO NOT recreate or modify**
- If task in story but file missing -> **CREATE**
- If task marked "Complete" in file -> **DO NOT modify**
- If adding new tasks, number them T50, T60... (jump by 10 from last existing)

**If re-running prompt on same story:**
```bash
# First run: Created T10-T40 (4 tasks)
ls .github/copilot/plan/E05-billing/S40-infrastructure/
# Output: README.md, SHARED_CONTEXT.md, T10, T20, T30, T40

# Second run (same story):
# Agent checks existence, finds all 4 tasks exist
# Agent: "All tasks for S40 already exist. No new tasks created."
```

**If story scope expanded, new tasks added:**
```bash
# Original: T10-T40 exist
# Story README updated: Add "Audit Logging" component

# Run prompt again:
# Agent checks: T10-T40 exist, new task missing
# Agent creates: T50-audit-logging.md (next semantic number)
# Agent updates: Story README task breakdown table with T50
# Agent does NOT modify T10-T40
```

### Step 10: Quality Checklist

Before marking complete, verify:
- [ ] Story README.md updated with task breakdown section (table, dependency graph, sequence)
- [ ] SHARED_CONTEXT.md created with common patterns, imports, examples (if needed)
- [ ] All task files created with semantic numbering (T10, T20, T30...)
- [ ] Each task file 500-1500 lines with complete code examples (not pseudocode)
- [ ] Each task has clear objective, context, acceptance criteria, implementation guide
- [ ] Each task has complete unit tests with FluentAssertions
- [ ] Dependencies between tasks documented (must complete first, blocks)
- [ ] Effort estimates realistic (8-16 hours per task, sum matches story effort)
- [ ] Task files reference SHARED_CONTEXT for common patterns
- [ ] Existing tasks NOT modified (idempotency)
- [ ] All commits have detailed commit messages
- [ ] Technology-specific patterns followed (e.g., React hooks, .NET Result<T>, Go error handling)
- [ ] Verification steps included (build, test, coverage commands)
- [ ] Troubleshooting section with common issues and solutions

---

## Example Execution

**User:** "Break down E05 S40 Infrastructure Layer into tasks"

**Agent Response:**

1. **Read story README:**
   ```bash
   cat .github/copilot/plan/E05-billing/S40-infrastructure/README.md
   # Extract: 1580 lines, EF Core, repositories, payment gateways, caching
   ```

2. **Determine if breakdown needed:**
   - Story size: 1580 lines (>1000 threshold) ✅
   - Complexity: Multiple concerns (EF Core + repositories + adapters + caching) ✅
   - Decision: **Create task breakdown**

3. **Determine task pattern:**
   - Story type: Infrastructure Layer (backend, C#/.NET)
   - Components: EF Core (12h), Repositories (10h), Payment Gateways (8h), Caching (6h)
   - Pattern: 4 tasks (T10-T40)

4. **Check existing tasks:**
   ```bash
   ls .github/copilot/plan/E05-billing/S40-infrastructure/
   # Output: README.md only (no tasks yet)
   # Decision: Create all 4 tasks
   ```

5. **Create SHARED_CONTEXT.md:**
   - Content: 650 lines (project structure, common imports, repository pattern, EF Core config, testing)
   - Includes: Result<T> pattern, PagedResult<T> pattern, primary constructor pattern

6. **Create T10 EF Core:**
   - File: `T10-ef-core-configurations.md`
   - Content: 980 lines (BillingDbContext, 11 entity configurations, migrations, unit tests)
   - Commit: `feat: E05 S40 task breakdown - Part 1 (SHARED_CONTEXT + T10)`

7. **Create T20 Repository Pattern:**
   - File: `T20-repository-pattern.md`
   - Content: 1120 lines (6 repository interfaces + implementations, unit tests)

8. **Create T30 Payment Gateways:**
   - File: `T30-payment-gateway-adapters.md`
   - Content: 890 lines (Stripe + Razorpay adapters, webhook verification, unit tests)
   - Commit: `feat: E05 S40 task breakdown - Part 2 (T20-T30)`

9. **Create T40 Caching Layer:**
   - File: `T40-caching-layer.md`
   - Content: 720 lines (Redis cache wrapper, cache-aside pattern, unit tests)
   - Commit: `feat: E05 S40 task breakdown - Part 3 (T40, complete)`

10. **Update Story README:**
    - Add task breakdown section with table, dependency graph, implementation sequence
    - Total effort: 36 hours (matches T10-T40 sum)

11. **Final verification:**
    ```bash
    ls .github/copilot/plan/E05-billing/S40-infrastructure/
    # Output: README.md, SHARED_CONTEXT.md, T10, T20, T30, T40 (6 files)
    ```

12. **Report to user:**
    ```
    ✅ Story-to-Tasks Complete
    
    📊 Tasks Created: 4
    - T10: EF Core DbContext & Configurations (12h, 980 lines)
    - T20: Repository Pattern (10h, 1120 lines)
    - T30: Payment Gateway Adapters (8h, 890 lines)
    - T40: Caching Layer (6h, 720 lines)
    
    📄 Additional Files:
    - SHARED_CONTEXT.md (650 lines, common patterns)
    - Story README.md updated with task breakdown section
    
    📂 Location: .github/copilot/plan/E05-billing/S40-infrastructure/
    
    📋 Implementation Sequence:
    Sprint 1 Week 1: T10 (12h)
    Sprint 1 Week 2: T20 (10h) + T30 (8h)
    Sprint 2 Week 3: T40 (6h)
    
    🔗 Dependencies:
    T10 → T20 → T40
    T10 → T30
    
    ⏱️ Total Effort: 36 hours (2 engineers × 2.25 weeks)
    
    🚀 Next Step:
    Assign tasks to engineers and start implementation from T10.
    All task files have complete code examples and unit tests ready to execute.
    ```

---

## Technology-Specific Adaptations

### Backend (C#/.NET, Java, Python, Go)
- **Task size:** 8-16 hours (one component per task: repository, service, adapter)
- **Code examples:** Complete classes with all methods, error handling, logging
- **Testing:** Unit tests with mocking (NSubstitute, Mockito, unittest.mock, testify)

### Frontend (React, Angular, Vue, Svelte)
- **Task size:** 6-12 hours (one page/feature per task)
- **Code examples:** Complete components with hooks, state, effects, event handlers
- **Testing:** Component tests (React Testing Library, Vue Test Utils) + E2E (Playwright)

### Infrastructure (Terraform, Kubernetes, Ansible)
- **Task size:** 4-10 hours (one module or manifest per task)
- **Code examples:** Complete IaC with variables, outputs, dependencies
- **Testing:** terraform plan, kubectl dry-run, ansible-lint

### Data (Spark, Airflow, dbt)
- **Task size:** 6-12 hours (one transformation or DAG per task)
- **Code examples:** Complete SQL models, PySpark jobs, Airflow DAGs
- **Testing:** dbt tests, Great Expectations, unit tests for transformations

---

## Prompt Chaining

**This prompt is typically the last in the chain:**
- `analysis-to-epics` creates epics
- `epic-to-stories` creates stories
- `story-to-tasks` creates tasks (this prompt)

**After this prompt, implementation begins using task files as roadmap.**

---

## Idempotency Examples

**Scenario 1: Re-running on same story**
```bash
# First run: Created T10-T40 (4 tasks)
ls .github/copilot/plan/E05-billing/S40-infrastructure/
# Output: README.md, SHARED_CONTEXT.md, T10, T20, T30, T40

# Second run (same story):
# Agent checks existence, finds all 4 tasks exist
# Agent: "All tasks for S40 already exist. No new tasks created."
```

**Scenario 2: Story scope expanded, new tasks added**
```bash
# Original: T10-T40 exist
# Story README updated: Add "Audit Logging" component

# Run prompt again:
# Agent checks: T10-T40 exist, new task missing
# Agent creates: T50-audit-logging.md (next semantic number)
# Agent updates: Story README task breakdown table with T50
# Agent does NOT modify T10-T40
```

**Scenario 3: Task marked complete**
```bash
# T20-repository-pattern.md has:
# **Status:** Complete

# Run prompt again (even if T20 scope changes):
# Agent: "T20 marked Complete, skipping modification"
# Agent: Does not overwrite completed work
# If new work needed: Create T21 or T50 (new task)
```

**Scenario 4: Task split due to complexity**
```bash
# Original: T20 Repository Pattern (1500 lines, too large)

# User: "Split T20 into smaller tasks"
# Agent:
# - Renames T20 to T20-read-repositories.md (700 lines, GET operations)
# - Creates T21-write-repositories.md (800 lines, POST/PUT/DELETE operations)
# - Updates story README task table with T20 + T21
```

---

## Error Handling

**Story README not found:**
```
Agent: "Cannot find story README at .github/copilot/plan/E05-billing/S40-infrastructure/README.md
Please verify story directory or run `epic-to-stories` prompt first."
```

**Story too small for task breakdown:**
```
Agent: "Story S40 Infrastructure Layer is 680 lines with established patterns.
Task breakdown not recommended - story README provides sufficient implementation guidance.

If you still want task breakdown, confirm: 'Yes, create tasks for S40'"
```

**Unclear task pattern from story:**
```
Agent: "Story README does not clearly indicate components to break down.
Please update story README with component sections (e.g., ## 1. Repositories, ## 2. Adapters)
Then re-run this prompt."
```

---

## Output Summary Template

**After completion, agent reports:**

```
✅ Story-to-Tasks Complete

📊 Tasks Created: [Number]
- T10: [Name] (Xh, YYY lines)
- T20: [Name] (Xh, YYY lines)
- T30: [Name] (Xh, YYY lines)

📄 Additional Files:
- SHARED_CONTEXT.md (XXX lines, common patterns)
- Story README.md updated with task breakdown section

📂 Location: .github/copilot/plan/E0X-epic/SYY-story/

📋 Implementation Sequence:
[Sprint/week breakdown with hours per period]

🔗 Dependencies:
[ASCII diagram showing task dependencies]

⏱️ Total Effort: [Hours] ([Engineers] × [Weeks])

🚀 Next Step:
Assign tasks to engineers and start implementation.
All task files have complete code examples and unit tests ready to execute.
```

---

**This prompt is self-contained, idempotent, and ready for a fresh GitHub Copilot agent with no prior context.**
