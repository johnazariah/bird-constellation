# Epic and Story Creation with Task Breakdown - Universal Guide

> **⚠️ IMPORTANT: This guide has been split into three focused prompts for better usability:**
> 
> 1. **[analysis-to-epics.md](./analysis-to-epics.md)** - Convert analysis documents into Epic-level breakdown
> 2. **[epic-to-stories.md](./epic-to-stories.md)** - Convert Epic READMEs into Story-level breakdown
> 3. **[story-to-tasks.md](./story-to-tasks.md)** - Convert Story READMEs into Task-level breakdown
> 
> **Use those prompts for actual work.** This document remains as a comprehensive reference and historical context.

---

**Purpose:** Guide a GitHub Copilot agent to create comprehensive epic stories with task breakdowns for any software project, driven by analysis documents and adapted to project context.

**Key Principles:**
- **Analysis-Driven:** Story structure determined by analysis document, not fixed templates
- **Incremental:** Only create new work, skip existing epics/stories/tasks
- **Conditional Task Breakdown:** Break into tasks only when story >1000 lines or high complexity
- **Gap Analysis:** Only for legacy code migration (compare analysis vs existing implementation)
- **Generalized:** Works for any tech stack, project structure, domain

---

## Context Building Process

### Step 1: Read Analysis Document First

**CRITICAL: Always start here to understand scope and structure**

1. **Locate Analysis Document:**
   - Look in `.github/copilot/analysis/` or `docs/` or project root
   - Common names: `modernization_plan.md`, `vnext_analysis.md`, `requirements.md`, `epics.md`
   - If not found, ask user: "Where is the analysis/requirements document?"

### Step 3: Determine Story Sequence from Analysis

**When user says:** "Create stories for E0X [Epic Name]" or "do E05 Billing"

**Process:**

1. **Read Epic Section in Analysis Document:**
   - Find the epic's section (e.g., "E05: Billing & Subscriptions")
   - Extract listed stories and their descriptions
   - Note any special requirements (compliance, integrations, performance)

2. **Identify Story Pattern:**
   - **Full Domain Implementation:** Analysis → Design → Domain → Infrastructure → Application → API → Feature Stories → Testing → Security → Production → Gap Analysis (10-15 stories)
   - **Feature Addition:** Design → Implementation → Testing → Deployment (3-5 stories)
   - **Infrastructure Change:** Design → Infrastructure → Migration → Testing → Monitoring (4-6 stories)
   - **UI Development:** Design → Component Library → Pages/Views → State Management → API Integration → Testing → Accessibility (6-8 stories)

3. **Check What Already Exists:**
   ```bash
   ls -la .github/copilot/plan/E05-billing-subscriptions/
   # If shows S01, S02, S03 exist, only create S04-S15
   ```

4. **Only Create Missing Stories:**
   - Skip stories with existing `README.md` files
   - Continue numbering from last existing story

---

## Story Type Templates

### For Backend Domain Implementation (C#/.NET, Java, Python, Go)

1. **S01: Analysis** (800-1000 lines)
   - **Domain-focused:** User management, billing, content delivery → likely 8-12 stories (analysis, design, domain, infra, app, API, features, testing, security, production)
   - **Feature-focused:** Add 2FA, implement caching → likely 2-4 stories (design, implementation, testing, deployment)
   - **Infrastructure-focused:** Set up Kubernetes, migrate database → likely 3-6 stories (design, infrastructure, migration, testing, monitoring)
   - **UI-focused:** Build admin dashboard → likely 5-8 stories (design, components, state management, API integration, testing, accessibility)

4. **Identify Technology Context:**
   - Backend: C#/.NET, Java/Spring, Node.js/Express, Python/Django, Go, Rust, etc.
   - Frontend: React, Angular, Vue, Svelte, Next.js, etc.
   - Database: PostgreSQL, MongoDB, MySQL, Cosmos DB, etc.
   - Infrastructure: Azure, AWS, GCP, Kubernetes, Docker, etc.

### Step 2: Understand Project Structure

1. **Read Project Documentation:**
   - `.github/copilot-instructions.md` - coding standards, patterns, development process
   - `README.md` - project overview, setup instructions, architecture
   - `.github/copilot/SHARED_CONTEXT.md` (if exists) - common patterns across epics

2. **Identify Existing Work (Skip What's Done):**
   - List all directories in `.github/copilot/plan/`
   - For each epic directory, list story subdirectories
   - **Skip creating stories that already exist** (check for `README.md` or `*.md` files)
   - **Only create missing stories** identified in analysis but not yet in plan/

3. **Understand Legacy Code (for Migration Projects):**
   - Grep search for existing implementations if modernizing/refactoring
   - Example: `grep -r "class UserService" src/` to find existing services
   - Gap analysis needed: compare analysis document vs existing code structure

### Step 2: Epic Story Creation Process

**When user says:** "Create stories for E0X [Epic Name]" or "do E05"

**Follow this sequence (15 stories typical):**

1. **S01: Analysis** (800-1000 lines)
   - Business context (revenue impact ₹XL/month, user personas, current pain points)
   - Domain model (aggregates, entities, value objects with C# record examples)
   - Business rules (30-40 rules enumerated and numbered)
   - State machines (for aggregates with lifecycle: Pending → Active → Expired)
   - Integration points (events published/consumed via RabbitMQ)
   - Success metrics (KPIs, performance targets)

2. **S02: Design** (700-900 lines)
   - Architecture decisions (Clean Architecture, CQRS, microservices, event-driven)
8. **SXX: Gap Analysis** (conditional - only for legacy migration)
   - Compare analysis document with legacy codebase
   - Identify missing features, breaking changes, data migrations
   - Create task breakdown for implementation (12-20 tasks depending on scope)
   - **Skip if greenfield project (no legacy code to compare)**

### For Frontend Development (React, Angular, Vue, Svelte)

1. **S01: Design System & Architecture** (600-800 lines)
   - UI/UX requirements, design system tokens, component hierarchy
   - State management strategy (Redux, Zustand, Context API, etc.)
   - Routing architecture, API integration patterns
   - Accessibility requirements (WCAG 2.1 AA compliance)

2. **S02: Component Library** (800-1000 lines)
   - Atomic design components (atoms, molecules, organisms)
   - Storybook setup, component documentation
   - Theme provider, CSS-in-JS or utility classes (Tailwind, etc.)

3. **S03-S06: Feature Pages/Views** (600-800 lines each)
   - One story per major page/view (Dashboard, Settings, Reports, etc.)
   - Component composition, props, events
   - Local state management, form validation

4. **S07: API Integration** (700-900 lines)
   - API client setup (Axios, Fetch, React Query, SWR)
   - Authentication, authorization, token refresh
   - Error handling, loading states, optimistic updates

5. **S08: Testing** (800-1000 lines)
   - Unit tests (Jest, Vitest), component tests (React Testing Library)
   - E2E tests (Playwright, Cypress), visual regression tests (Chromatic)
   - Accessibility tests (axe-core, jest-axe)

6. **S09: Performance & Optimization** (600-800 lines)
   - Code splitting, lazy loading, bundle analysis
   - Memoization, virtual scrolling, image optimization
   - Lighthouse performance targets (>90 score)

7. **S10: Deployment & Monitoring** (500-700 lines)
   - Build pipeline (Vite, Webpack, Turbopack)
   - CDN deployment (Vercel, Netlify, Azure Static Web Apps)
   - Monitoring (Sentry, LogRocket, Google Analytics)

### For Infrastructure/DevOps

1. **S01: Infrastructure Design** (500-700 lines)
2. **S02: IaC Implementation** (Terraform, Bicep, CloudFormation)
3. **S03: CI/CD Pipeline** (GitHub Actions, Azure DevOps)
4. **S04: Monitoring & Alerting** (Prometheus, Grafana, Datadog)
5. **S05: Security Hardening** (secrets, IAM, network policies)

---

## Story Content Structure Templatee queues, webhooks)
   - Security considerations (authentication, authorization, data encryption)
   - Performance requirements (p95 <500ms, throughput targets)

3. **S03-S06: Core Implementation** (1200-1600 lines each)
   - **S03: Domain Model** - Aggregates as C# 13 records, value objects, factory methods, state transitions returning `Result<T>`
   - **S04: Infrastructure** - EF Core DbContext, entity configurations, repositories, payment gateway adapters
   - **S05: Application Layer** - MediatR command/query handlers, DTOs, FluentValidation, CQRS pipelines
   - **S06: API Layer** - ASP.NET Core controllers, webhooks, background jobs (Hangfire), middleware

4. **S07-S11: Feature Stories** (1200-1600 lines each)
   - Each feature story extends core implementation with specific functionality
   - Examples: "Subscription Plan Management", "Invoicing & Billing Cycles", "Payment Gateway Integration", "Discount & Coupon System", "Revenue Reporting & Analytics"
   - Include: Domain extensions (new methods on aggregates), infrastructure (new repositories/adapters), application layer (new commands/queries), API endpoints, background jobs

5. **S12: Integration Testing** (1200-1500 lines)
   - Test infrastructure (Testcontainers for PostgreSQL/Redis/RabbitMQ, IntegrationTestBase class)
   - Test data factories (Bogus library for realistic fake data)
   - Mock external services (payment gateways, email service)
   - E2E test suites (happy paths, edge cases, error scenarios for complete workflows)
   - Performance tests (k6 load test scripts with stages, thresholds)
   - CI/CD integration (GitHub Actions workflow with service containers)

6. **S13: Authorization & Security** (1400-1600 lines)
   - Permission system (RBAC with 20+ permissions across 4+ roles: Student, InstituteAdmin, FinanceTeam, SuperAdmin)
   - Role-permission mapping (BillingRoles dictionary)
   - Audit logging (immutable AuditLog aggregate with EF Core configuration)
   - Authorization service (HasPermissionAsync, CanAccessInstituteDataAsync)
   - MediatR pipeline behaviors (AuthorizationBehavior, AuditLoggingBehavior)
   - Data encryption (AES-256-GCM with IEncryptionService interface)
   - Sensitive data masking (EmailMasker, PhoneMasker value objects)
   - Rate limiting (ASP.NET Core 8 sliding window rate limiter per endpoint)
   - Security headers (CSP, HSTS, X-Frame-Options middleware)
   - Compliance requirements (PCI DSS, GDPR/DPDPA, GST 7-year retention)

7. **S14: Production Readiness** (1600-1800 lines)
   - **Observability:**
     - Structured logging (Serilog with console, file, Seq, Application Insights sinks)
     - OpenTelemetry metrics (custom BillingMetrics meter with counters/histograms)
     - Distributed tracing (HTTP, EF Core, Redis instrumentation)
     - Health checks (database, cache, message broker, payment gateways, disk, memory)
   - **Resilience:**
     - Circuit breakers (Polly for payment gateway: 50% failure, 60s break; database: 70% failure, 30s break)
     - Retry with exponential backoff (webhook retry: 30s, 1m, 5m, 15m, 1h)
     - Bulkhead isolation (50 concurrent payments, 10 concurrent PDF generations)
     - Timeout policies (30s payment gateway, 10s database)
   - **Deployment:**
     - Multi-stage Dockerfile (build, publish, runtime with non-root user)
     - Kubernetes manifests (Deployment with 3 replicas, Service ClusterIP, HPA 3-10 replicas based on CPU/memory)
     - CI/CD pipeline (GitHub Actions: build → test → docker push → staging deploy → production blue-green deploy)
     - Resource limits (512Mi-1Gi memory, 250m-1000m CPU)
     - Liveness/readiness/startup probes
   - **Disaster Recovery:**
     - PostgreSQL automated backups (Azure: daily, 7-day retention, geo-redundant, point-in-time restore with 5-min RPO)
     - Runbooks (payment gateway failover Stripe→Razorpay, database failure restart/restore)
   - **Monitoring:**
     - Prometheus alerts (error rate >5%, payment failures >10/min, slow responses p95>500ms, circuit breaker open, webhook lag)
     - Grafana dashboards (request rate, error rate, response time p50/p95/p99, payment success rate, revenue 24h, circuit breaker status)
     - SLA targets (99.9% uptime = 43.8 min downtime/month, p95<500ms, p99<1000ms)
     - Incident response (<15min acknowledgment, <1hr mitigation, 2 engineers on-call 24x7)

8. **S15: Gap Analysis** (see Task Breakdown section below)

### Step 3: Story Content Structure Template

Each story follows this template:

```markdown
# E0X SYY: [Story Title]

**Story:** [One sentence objective]
**Priority:** Critical/High/Medium/Low
**Effort:** X hours
**Status:** Not Started/In Progress/Complete

## Objective
[What this story accomplishes, why it matters, 2-3 paragraphs]

## Business Context
[Revenue impact (₹XL/month), user impact (X users), compliance requirements (GST, PCI DSS)]

### [Subsection - e.g., Security Requirements, Performance Targets]
- Specific requirements enumerated

## Domain/Infrastructure/Application/API Extensions

### 1. [Component Name - e.g., Permission System]

[Description paragraph]

**Implementation (C# 13 code example):**
```csharp
// Complete, runnable code with primary constructors, records, pattern matching
public record Example { }
```

### 2. [Next Component]

[Repeat pattern with complete code examples]

## Success Criteria
- ✅ Specific, testable acceptance criteria
- ✅ Each criterion maps to a deliverable artifact
- ✅ Test coverage targets (>80% unit, >85% integration)

## Next Steps
[What comes after this story, dependencies for implementation]

---
**Story Complete:** [One sentence summary of what was delivered]
```

### Step 4: Task Breakdown Process (S15 Gap Analysis)

**When to create task breakdown:**
- **Always for S15 Gap Analysis** - this is the implementation roadmap that executes all prior stories
- **Optionally for complex feature stories** (S07-S11) if user requests breakdown
- **Not needed for S01-S02** (analysis/design are research documents, not implementation)

**Task Breakdown Structure for S15:**

1. **Create `S15-gap-analysis/` directory with these files:**
   - `README.md` - Overview, 12-task summary table, dependencies graph, sprint plan (4 weeks), risk mitigation, success criteria
   - `SHARED_CONTEXT.md` - Project structure, C# 13 patterns, functional programming (Result<T>, pure functions, immutability), common patterns (repositories, MediatR, EF Core), NuGet packages per layer, testing standards, business constants
   - `TASK-01-domain-aggregates.md` through `TASK-12-integration-testing.md` - Individual task instructions

2. **README.md Structure:**
   ```markdown
   # E05 S15: Gap Analysis - Implementation Task Breakdown
   
   ## Overview
   [Epic context, total effort 160 hours, 2 engineers × 4 weeks]
   
   ## Task Summary
   ### Domain Layer (3 tasks, 32 hours)
   1. T01: Aggregates (14h)
   2. T02: Value Objects (10h)
   3. T03: Domain Events (8h)
   
   ### Infrastructure Layer (3 tasks, 38 hours)
   ...
   
   ## Dependencies
   [ASCII diagram showing task dependencies]
   
   ## Implementation Sequence
   ### Sprint 1: Foundation (Week 1-2, 70 hours)
   [Day-by-day breakdown of T01-T06]
   
   ## Risk Mitigation
   [High-risk tasks with mitigation strategies]
   
   ## Success Criteria
   [Technical + business acceptance criteria]
   ```

3. **SHARED_CONTEXT.md Structure:**
   - Project folder structure (Domain, Infrastructure, Application, API layers)
   - C# 13 features (primary constructors, records, collection expressions, pattern matching)
   - Functional programming patterns (Result<T>, monadic composition, pure functions, immutability)
   - Common patterns with code examples (repositories returning Result<T>, MediatR command/query handlers, EF Core configurations)
   - NuGet packages per layer (MediatR, AutoMapper, FluentValidation, Stripe.net, Hangfire, xUnit, Testcontainers)
   - Testing standards (unit test pattern with FluentAssertions, integration test pattern with IntegrationTestBase)
   - Business constants (GSTRates, SubscriptionDurations, MinimumAmounts)

4. **TASK-XX-[name].md Template:**
```markdown
# TASK XX: [Task Name]

**Effort:** X hours
**Priority:** Critical/High/Medium/Low
**Assignee:** Engineer 1/2
**Dependencies:** TXX, TYY (list blocking tasks)

## Objective
[What this task accomplishes in 1-2 sentences]

## Context
[Why this task matters, reference to S01-S14 stories, how it fits in the architecture]

## Acceptance Criteria
- [ ] Specific deliverable 1 (e.g., "Package aggregate with pricing tiers implemented")
- [ ] Specific deliverable 2 (e.g., "Unit tests for all state transitions written")
- [ ] Unit test coverage >80%
- [ ] Code compiles without errors
- [ ] All business rules from S01 enforced

## Implementation Guide

### 1. [Aggregate/Component Name]

**File:** `src/Notebook.School.Billing.Domain/Aggregates/Package.cs`

```csharp
// Complete, runnable code example (not pseudocode)
// Include: using statements, namespace, full class/record definition
// Use: primary constructors, records, Result<T> pattern, factory methods

using Notebook.School.Billing.Domain.ValueObjects;

namespace Notebook.School.Billing.Domain.Aggregates;

public record Package
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    // ... full property list
    
    public static Result<Package> Create(string name, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Package>.Failure("Name is required");
        
        return Result<Package>.Success(new Package { /* ... */ });
    }
    
    public Result<Package> UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            return Result<Package>.Failure("Price must be positive");
        
        return Result<Package>.Success(this with { Price = newPrice });
    }
}
```

### Step 6: Incremental Story Creation (Skip Existing Work)

**CRITICAL: Before creating any story, check if it already exists**

```bash
# Check existing stories for an epic
ls -la .github/copilot/plan/E05-billing-subscriptions/

# Output examples:
# S01-analysis/
# S02-design/
# S03-domain-model/
# S04-infrastructure/
# (S05-S15 missing)

# Agent decision: Create S05-S15 only, skip S01-S04
```

**Skip story creation if:**
- Directory exists: `.github/copilot/plan/E0X-epic-name/SYY-story-name/`
- Story README exists: `.github/copilot/plan/E0X-epic-name/SYY-story-name/README.md`
- User says: "Skip S01-S04, they're done"

**Create story if:**
- Story mentioned in analysis document but directory doesn't exist
- User says: "Create missing stories for E05"
- User says: "Continue from S05 onwards"

**Incremental Task Creation (within story):**

```bash
# Check existing tasks
ls -la .github/copilot/plan/E05-billing/S15-gap-analysis/

# Output examples:
# README.md
# SHARED_CONTEXT.md
# TASK-01-domain-aggregates.md
# TASK-02-value-objects.md
### Step 8: Quality Checklist (Generalized)

Before marking epic complete, verify:
- [ ] All stories from analysis document exist in `.github/copilot/plan/E0X-[epic-name]/`
- [ ] Each story has appropriate depth (varies: 500-2000 lines depending on complexity)
- [ ] Stories with >1000 lines have task breakdown (README + tasks or just tasks)
- [ ] Task files have complete code examples (not pseudocode or "TODO" comments)
- [ ] Business rules from analysis appear in domain/implementation stories
- [ ] Database schema from design appears in infrastructure/persistence stories
- [ ] API contracts from design appear in API/controller stories
- [ ] Test coverage requirements specified (varies by project: 70-90%)
- [ ] Dependencies between tasks/stories documented
- [ ] Effort estimates realistic (sum matches sprint capacity)
- [ ] All commits have detailed commit messages
- [ ] Technology-specific patterns followed (e.g., React hooks, .NET Result<T>, Go error handling)

**Project-Specific Quality Checks:**
**User:** "Create stories for E05 Billing Service" or "do E05"

**Agent Response Sequence:**

1. **Read analysis document:**
   ```bash
   # Find analysis document
   find .github/copilot/analysis/ -name "*.md" | grep -i "billing\|vnext\|plan"
   # Read it to understand E05 structure
   ```

2. **Check existing work:**
   ```bash
   ls -la .github/copilot/plan/E05-billing-subscriptions/
   # Output: S01-analysis/, S02-design/, S03-domain/ exist
   # Decision: Skip S01-S03, create S04 onwards
   ```

3. **Read project patterns:**
   - `.github/copilot-instructions.md` for coding standards
   - Existing stories (S01-S03) to match style/depth

4. **Identify legacy code (if migration):**
   ```bash
   grep -r "class SubscriptionBAL" src/Notebook.School.BAL/Business/
   # Found legacy code → Gap Analysis needed later
   ```

5. **Create missing stories** (determined by analysis, not template):
   - If analysis lists 15 stories: Create S04-S15
   - If analysis lists 8 stories: Create S04-S08
   - If analysis lists 20 stories: Create S04-S20

6. **Apply task breakdown conditionally:**
   - S04 Infrastructure (1580 lines) → Create tasks (>1000 lines)
   - S05 Application (450 lines) → No tasks (<1000 lines)
   - S08 Gap Analysis (any size) → Always create tasks (legacy comparison)

7. **Commit strategy:**
   - Commit after each story (or batch of 2-3 small stories)
   - For task breakdown: Commit in batches (T01-T03, T04-T06, etc.)

**Adaptation for React UI Epic:**

**User:** "Create stories for E10 Admin Dashboard (React)"

**Agent Response:**
1. Read analysis: E10 has 8 stories (Design, Component Library, Dashboard Page, Settings Page, Reports Page, API Integration, Testing, Deployment)
2. Check existing: E10 doesn't exist yet → Create all 8 stories
3. Read patterns: Check if React patterns documented (hooks, TypeScript, state management)
4. Create stories:
   - S01: Design System (600 lines) - Figma tokens, theme, typography
   - S02: Component Library (900 lines) - Buttons, Forms, Modals, Tables (Storybook examples)
   - S03: Dashboard Page (800 lines) - Charts (Recharts), KPI cards, real-time data
   - S04: Settings Page (700 lines) - Forms (React Hook Form), validation (Zod)
   - S05: Reports Page (850 lines) - Tables (TanStack Table), filtering, export CSV
   - S06: API Integration (600 lines) - React Query, auth, error handling
   - S07: Testing (750 lines) - Jest, React Testing Library, Playwright E2E
   - S08: Deployment (500 lines) - Vite build, Vercel deploy, environment vars
5. Task breakdown: Only S02 (900 lines, close to threshold) gets tasks
6. Commit each story individually4, E05 exist
# Analysis document now lists E06 (new)

# Agent decision: Create E06 only, skip E01-E05
```
        var price = new Money(1000, "INR");
        
        // Act
        var result = Package.Create(name, price);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
    }
    
    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Arrange, Act, Assert
        var result = Package.Create("", new Money(1000, "INR"));
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Name is required");
    }
}
```

## Recommendations for Future Agents

### Should all stories have task breakdowns?

**No - Task breakdown is conditional based on story size and complexity.**

**Break into tasks if:**
- Story is >1000 lines (too large for atomic execution)
- Story is Gap Analysis (mandatory - compares legacy vs new)
- Story spans multiple concerns (frontend + backend + database)
- Multiple engineers need to work in parallel
- User explicitly requests breakdown

**Skip task breakdown if:**
- Story is <1000 lines (already actionable)
- Story is analysis/design/research (no implementation)
- Story follows established pattern (can copy from similar story)
- Single engineer, sequential work (1-2 days effort)
---

## Template Parameterization for Any Project

To use this guide for a new project:

1. **Replace project-specific references:**
   - `EduJournal vNext` → `{{PROJECT_NAME}}`
   - `Notebook.School.*` → `{{NAMESPACE_PREFIX}}`
   - `.github/copilot/plan/` → `{{PLAN_DIRECTORY}}`
   - `.github/copilot/analysis/` → `{{ANALYSIS_DIRECTORY}}`

2. **Replace technology-specific patterns:**
   - `C# 13` → `{{PRIMARY_LANGUAGE}}`
   - `ASP.NET Core` → `{{BACKEND_FRAMEWORK}}`
   - `PostgreSQL` → `{{DATABASE}}`
   - `React` → `{{FRONTEND_FRAMEWORK}}`
   - `Azure` → `{{CLOUD_PROVIDER}}`

3. **Replace architecture patterns:**
   - `Clean Architecture` → `{{ARCHITECTURE_PATTERN}}`
   - `CQRS` → `{{APPLICATION_PATTERN}}`
   - `Result<T>` → `{{ERROR_HANDLING_PATTERN}}`
   - `MediatR` → `{{MEDIATOR_LIBRARY}}`
   - `EF Core` → `{{ORM}}`

4. **Adjust story templates:**
   - Read analysis document to determine story list
   - Not all projects have 15 stories per epic
   - Not all projects need gap analysis (only legacy migrations)
   - Frontend epics have different story structure than backend

5. **Adjust task breakdown thresholds:**
   - Default: >1000 lines triggers task breakdown
   - Adjust based on team size (1 engineer: 1500 lines, 3 engineers: 800 lines)
   - Adjust based on complexity (high complexity: 800 lines, low complexity: 1500 lines)

---

## Usage for Incremental Updates

**Scenario: Analysis document updated with new epic E10**

```bash
# 1. Read updated analysis
# Analysis now has E01-E10 (E10 is new: Admin Dashboard)

# 2. Check existing epics
ls .github/copilot/plan/
# Output: E01, E02, E03, E04, E05, E06, E07, E08, E09 (E10 missing)

# 3. Create only E10
# Don't recreate E01-E09

# 4. Follow standard process for E10
# Read E10 description from analysis
# Determine E10 story structure (Design, Components, Pages, Testing)
# Create E10 stories
```

**Scenario: New stories added to existing epic**

```bash
# Analysis updated: E05 now has S16-S17 (React UI for billing admin)

# Check existing
ls .github/copilot/plan/E05-billing/
# Output: S01-S15 exist

# Create only S16-S17
# Don't recreate S01-S15
```

---

**This generalized guide works for any software project (web, mobile, desktop, embedded) with any tech stack (C#, Java, Python, Go, Rust, JavaScript, TypeScript, etc.) and adapts to project-specific analysis documents, coding standards, and architectural patterns.**
- Redundancy: If story already has complete code examples, tasks are redundant
- Flexibility: Not all projects/stories fit 12-task pattern
- Analysis-driven: Story structure dictated by analysis document, not template
**Review Required:** Yes/No
```

### Step 5: Task Organization (12 Tasks Standard)

**Domain Layer (3 tasks, 32 hours):**
- **T01: Domain Aggregates** (14h) - Package, Subscription, Invoice, Payment, Discount as immutable records with state machines, factory methods, `Result<T>` pattern
- **T02: Value Objects** (10h) - Money (currency-aware arithmetic), Address (with encryption support), TaxBreakdown (GST calculation: CGST+SGST vs IGST)
- **T03: Domain Events** (8h) - 15+ event types (SubscriptionCreated, PaymentCompleted, InvoiceGenerated, etc.) as immutable records

**Infrastructure Layer (3 tasks, 38 hours):**
- **T04: EF Core Persistence** (16h) - BillingDbContext, 11 entity configurations (ToTable, HasKey, properties, owned entities, indexes), migrations (Add-Migration, Update-Database)
- **T05: Repository Pattern** (12h) - 6 repository interfaces (IPackageRepository, ISubscriptionRepository, etc.) + implementations returning `Result<T>`, `Result<PagedResult<T>>`
- **T06: Payment Gateway Integration** (10h) - IPaymentGateway interface, StripePaymentGateway adapter, RazorpayPaymentGateway adapter, webhook signature verification

**Application Layer (4 tasks, 58 hours):**
- **T07: Command Handlers** (20h) - 15+ command handlers (CreatePackage, CreateSubscription, ProcessPayment, ApplyDiscount, etc.) with MediatR, FluentValidation, Result<T>
- **T08: Query Handlers** (16h) - 10+ query handlers (GetPackages, GetSubscriptions, GetInvoices with pagination, filtering, authorization)
- **T09: DTOs & Mapping** (12h) - 20+ DTOs (PackageDto, SubscriptionDto, InvoiceDto, etc.), AutoMapper profiles with custom value resolvers
- **T10: Background Jobs** (10h) - 5 Hangfire jobs (RenewalReminderJob, RecurringBillingJob, WebhookRetryJob, PaymentReconciliationJob, MRRCalculationJob)

**API Layer (2 tasks, 32 hours):**
- **T11: REST Controllers** (18h) - 5 controllers (PackagesController, SubscriptionsController, InvoicesController, PaymentsController, DiscountsController), 27 endpoints total, JWT authentication, rate limiting (ASP.NET Core 8), authorization attributes
- **T12: Integration Testing** (14h) - IntegrationTestBase with Testcontainers (PostgreSQL, Redis, RabbitMQ), TestDataFactory with Bogus, E2E test suites (subscription lifecycle, payment flow, discount application), k6 performance tests

**Total:** 160 hours (2 engineers × 4 weeks)

### Step 6: When to Skip/Simplify Task Breakdown

**Skip detailed task breakdown for:**
- S01 Analysis - Research/documentation phase, no code
- S02 Design - Architecture decisions, schemas, contracts only
- Simple feature stories <800 lines that follow established patterns

**Always create detailed task breakdown for:**
- **S15 Gap Analysis** (implementation roadmap - this is mandatory)
- Net-new microservices (complete greenfield development)
- Complex integrations requiring multiple external systems

**If user requests task breakdown for S07-S11 feature stories:**
- Create 4-6 tasks (not 12) - group related work
- Example: S07 Subscription Plans could have 4 tasks:
  1. Domain extensions (proration logic, trial config)
  2. Application layer (commands/queries for plans)
  3. API endpoints (CRUD operations)
  4. Background job (trial conversion)

### Step 7: Commit Strategy

**During epic creation, commit after each story:**
```bash
git commit -m "feat: E0X SYY [Story Name] complete (XXXX lines)

[Story summary paragraph]
- [Key feature 1 with details]
- [Key feature 2 with details]
- [Key feature 3 with details]"
```

**For S15 Gap Analysis with 12 tasks, commit in batches:**
1. **Batch 1:** `git commit -m "feat: E05 S15 Gap Analysis - Part 1 (README, SHARED_CONTEXT, T01)"`
2. **Batch 2:** `git commit -m "feat: E05 S15 Gap Analysis - Part 2 (T02-T03 Domain Layer)"`
3. **Batch 3:** `git commit -m "feat: E05 S15 Gap Analysis - Part 3 (T04-T06 Infrastructure)"`
4. **Batch 4:** `git commit -m "feat: E05 S15 Gap Analysis - Part 4 (T07-T09 Application)"`
5. **Batch 5:** `git commit -m "feat: E05 S15 Gap Analysis - Part 5 (T10-T12 API, final)"`

### Step 8: Quality Checklist

Before marking epic complete, verify:
- [ ] All stories S01-S15 exist in `.github/copilot/plan/E0X-[epic-name]/`
- [ ] Each story 700-1800 lines (comprehensive with code examples)
- [ ] S15 has README + SHARED_CONTEXT + 12 task files (T01-T12)
- [ ] Each task file 500-1500 lines with complete code examples (not pseudocode)
- [ ] Business rules from S01 appear in S03 domain model and T01 aggregates
- [ ] Database schema from S02 appears in S04 infrastructure and T04 EF Core
- [ ] API contracts from S02 appear in S06 API layer and T11 controllers
- [ ] Test coverage requirements specified (>80% unit, >85% integration)
- [ ] Dependencies between tasks documented in README dependency graph
- [ ] Effort estimates realistic (T01-T12 sum to ~160 hours = 2 engineers × 4 weeks)
- [ ] All commits have detailed commit messages with bullet points

## Example Usage Walkthrough

**User:** "Create E05 Billing Service stories" or "do E05"

**Agent Response Sequence:**
1. Read `.github/copilot-instructions.md` for functional programming patterns (Result<T>, immutability, pure functions)
2. Grep search legacy billing code: `grep -r "subscription" src/Notebook.School.BAL/Business/` to understand current implementation
3. Create S01 Analysis (943 lines) - 5 aggregates, 34+ business rules, Money/Address value objects, state machines
4. Commit S01
5. Create S02 Design (701 lines) - PostgreSQL schema (10 tables), 27 REST endpoints, payment gateway patterns
6. Commit S02
7. Create S03-S06 (1290+1580+1487+453=4810 lines) - Core layers (domain, infrastructure, application, API)
8. Commit S03-S06 together
9. Create S07-S11 (1282+1417+1656+1512+1248=7115 lines) - Feature stories (subscription plans, invoicing, payment gateway, discounts, revenue reporting)
10. Commit each feature story individually
11. Create S12 Integration Testing (1389 lines) - Testcontainers, E2E tests, k6 performance tests
12. Commit S12
13. Create S13 Authorization & Security (1504 lines) - RBAC, audit logging, encryption, rate limiting
14. Commit S13
15. Create S14 Production Readiness (1686 lines) - Observability, resilience, deployment, monitoring
16. Commit S14
17. Create S15 Gap Analysis with 12 tasks (see breakdown below)
18. Commit S15 in 5 batches

**User:** "Break down S15 into tasks" or "OK the response has hit the length limit, so let's do the breakdown immediately"

**Agent Response Sequence:**
1. Create `S15-gap-analysis/README.md` (500 lines) - Overview, task summary table, dependency graph, sprint plan (4 weeks), risk mitigation
2. Create `S15-gap-analysis/SHARED_CONTEXT.md` (600 lines) - Project structure, C# 13 patterns, Result<T> pattern, repository/MediatR examples, NuGet packages, testing standards
3. Create `S15-gap-analysis/TASK-01-domain-aggregates.md` (800 lines) - Complete implementation of Package, Subscription, Invoice, Payment, Discount aggregates with factory methods, state transitions, unit tests
4. Commit Part 1 (README + SHARED_CONTEXT + T01)
5. Create `TASK-02-value-objects.md` (600 lines) - Money (currency arithmetic), Address (encryption), TaxBreakdown (GST calculation)
6. Create `TASK-03-domain-events.md` (400 lines) - 15+ event records (SubscriptionCreated, PaymentCompleted, etc.)
7. Commit Part 2 (T02-T03)
8. Create `TASK-04-ef-core-persistence.md` (700 lines) - BillingDbContext, 11 entity configurations, migrations
9. Create `TASK-05-repository-pattern.md` (600 lines) - 6 repository interfaces + implementations
10. Create `TASK-06-payment-gateways.md` (500 lines) - Stripe/Razorpay adapters with webhook handling
11. Commit Part 3 (T04-T06)
12. Create `TASK-07-command-handlers.md` (900 lines) - 15+ command handlers with validation
13. Create `TASK-08-query-handlers.md` (700 lines) - 10+ query handlers with pagination
14. Create `TASK-09-dtos-mapping.md` (600 lines) - 20+ DTOs with AutoMapper profiles
15. Commit Part 4 (T07-T09)
16. Create `TASK-10-background-jobs.md` (500 lines) - 5 Hangfire jobs
17. Create `TASK-11-rest-controllers.md` (800 lines) - 5 controllers, 27 endpoints, auth, rate limiting
18. Create `TASK-12-integration-testing.md` (700 lines) - IntegrationTestBase, Testcontainers, E2E tests
19. Commit Part 5 (T10-T12, final)
20. Update TODO list to mark E05 S15 complete

## Recommendations for Future Agents

### Should all stories have task breakdowns?

**No - Only S15 Gap Analysis needs detailed task breakdown by default.**

**Reasoning:**
- S01-S02: Analysis/design are documentation, not implementation - no tasks needed
- S03-S06: Core implementation stories already provide detailed code examples - tasks would be redundant
- S07-S11: Feature stories extend core patterns established in S03-S06 - tasks optional unless story >2000 lines
- S12-S14: Testing/security/production are typically sequential work by one engineer - tasks optional
- **S15 Gap Analysis: Always needs 12-task breakdown** - this is the implementation roadmap that translates all prior stories into executable work

### When user explicitly requests task breakdown for a non-S15 story:

**User:** "Break down S07 Subscription Plan Management into tasks"

**Agent Response:**
1. Create `S07-subscription-plan-management/` directory
2. Move existing story content to `S07-subscription-plan-management/README.md`
3. Add `SHARED_CONTEXT.md` if patterns differ from S15
4. Create **4-6 tasks** (not 12 - gap analysis is special because it covers entire epic):
   - Task 1: Domain extensions (proration logic on Package/Subscription aggregates)
   - Task 2: Infrastructure (new repository methods for package filtering)
   - Task 3: Application layer (CreatePackage, UpdatePackage, CalculateProration commands)
   - Task 4: API endpoints (POST /packages, PUT /packages/{id}, GET /packages/compare)
   - Task 5: Background job (TrialConversionJob)
   - Task 6: Unit tests for new business logic
5. Each task 8-16 hours (1-2 days), total 40-60 hours
6. Group related work (e.g., "All package CRUD commands" not separate task per command)

### Token Management Strategy

**Epic creation (S01-S14):**
- Typical token usage: 25-35K tokens for all 14 stories
- Can be done in single session if no interruptions
- Commit after each story to create checkpoints

**Gap analysis (S15 with 12 tasks):**
- Typical token usage: 40-50K tokens (README + SHARED_CONTEXT + 12 tasks)
- **Must split into 5 commits** to avoid length limits
- If hitting token limits (>900K used): commit current batch, user says "continue", agent resumes from context

**Conversation summary:**
- Triggered automatically if tokens >800K
- Agent can continue from summary - all context preserved
- Quality does not degrade after summarization

### Epic Completion Checklist

After S15 complete, verify:
- [ ] 15 stories (S01-S15) committed
- [ ] S15 has 12 task files (T01-T12)
- [ ] Total effort: S01-S14 documentation (40-60 hours to write), T01-T12 implementation (160 hours to execute)
- [ ] All commits have detailed messages
- [ ] TODO list updated to mark epic complete
- [ ] No placeholder/pseudocode - all examples are complete and runnable

---

**This comprehensive guide captures the E04 Institute Management and E05 Billing Service epic/story/task creation process, optimized for EduJournal vNext with C# 13, functional programming, Clean Architecture, and microservices patterns.**
