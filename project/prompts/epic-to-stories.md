# Epic-to-Stories Prompt

**Purpose:** Convert an Epic README.md into Story-level breakdown. Each Story represents a cohesive unit of work (analysis, design, implementation, testing, etc.) that delivers specific technical or business value.

**Invocation:** "Break down {epicPrefix}05 into stories" or "Create stories for Epic {epicPrefix}05 Billing"

---

## Step 0: Load Project Parameters

**CRITICAL: Read project parameters first to configure this prompt for the current project.**

```bash
# Read project-specific configuration
cat .github/copilot/prompts/project-parameters.json
```

**Extract and use these parameters throughout:**
- `paths.plan` - Where epic folders exist
- `paths.instructions` - Project coding standards
- `naming.storyPrefix` - Story prefix (default: `S`)
- `naming.numberingGap` - Numbering gap (default: `10`)
- `naming.storyFormat` - Story folder format
- `technology.*` - Tech stack for code examples
- `patterns.*` - Coding patterns to follow
- `namespace.*` - Namespace for code examples
- `storyPatterns.*` - Story templates by project type
- `taskBreakdown.lineThreshold` - When to break stories into tasks
- `testingStandards.*` - Testing requirements
- `commitConventions.storyFormat` - Commit message format

**If project-parameters.json not found, use defaults.**

---

## Prerequisites

Before starting, the agent must have:
1. **Project parameters loaded** (from project-parameters.json)
2. Path to epic README.md (from `{paths.plan}/{epicPrefix}XX-epic-name/README.md`)
3. Project coding standards (from `{paths.instructions}` parameter)
4. Understanding of project structure (from `project.type` parameter)

---

## Execution Sequence

### Step 1: Context Gathering

**Read these files in this order:**

1. **Epic README.md** (REQUIRED)
   - Location: `.github/copilot/plan/E0X-epic-name/README.md`
   - Extract: Business value, scope, domain model, technical stack, success criteria
   - Understand: What stories are needed to deliver this epic

2. **Project Instructions** (REQUIRED)
   - Location: `.github/copilot-instructions.md`
   - Extract: Coding paradigms (functional/OOP), testing standards, architecture patterns
   - Understand: How to structure code examples in stories

3. **Existing Stories** (CHECK FOR IDEMPOTENCY)
   ```bash
   ls -la .github/copilot/plan/E05-billing-subscriptions/
   ```
   - Output: List of existing story directories (S01-analysis/, S02-design/, etc.)
   - Decision: **Skip stories that already exist**, only create missing ones

4. **Similar Epics' Stories** (OPTIONAL - for pattern matching)
   ```bash
   ls .github/copilot/plan/E03-institute-management/
   # See what story structure was used for similar backend epic
   ```

### Step 2: Determine Story Pattern

**Story patterns vary by epic type. Use `storyPatterns.*` from project parameters.**

**Read epic to determine which pattern applies:**
- Check `project.type` parameter (e.g., "backend-microservices", "frontend-spa", "infrastructure", "data-pipeline")
- Match to `storyPatterns.{type}` in parameters
- Use the defined story list, line counts, and effort estimates

#### Example: Backend Domain Implementation ({technology.primaryLanguage}, {technology.framework})
**When epic focuses on:** Microservices, business logic, APIs, database persistence

**Use `storyPatterns.backend-domain-implementation` from parameters:**
**Typical 10-15 stories (from parameters):**
1. **{storyPrefix}10: Analysis** (800-1000 lines) - Domain model, business rules, state machines
2. **{storyPrefix}20: Design** (700-900 lines) - Architecture, database schema, API contracts
3. **S30: Domain Layer** (1200-1600 lines) - Aggregates, value objects, domain events
4. **S40: Infrastructure Layer** (1200-1600 lines) - EF Core, repositories, external adapters
5. **S50: Application Layer** (1200-1600 lines) - Command/query handlers, DTOs, validation
6. **S60: API Layer** (1200-1600 lines) - Controllers, webhooks, middleware
7. **S70-S110: Feature Stories** (1200-1600 lines each, 3-5 stories) - Specific features extending core
8. **S120: Integration Testing** (1200-1500 lines) - Testcontainers, E2E tests, performance tests
9. **S130: Authorization & Security** (1400-1600 lines) - RBAC, audit logging, encryption
10. **S140: Production Readiness** (1600-1800 lines) - Observability, resilience, deployment
11. **S150: Gap Analysis** (conditional) - Legacy migration tasks if applicable

#### Frontend Development (React, Angular, Vue)
**When epic focuses on:** UI/UX, component library, state management

**Typical 7-10 stories:**
1. **S10: Design System & Architecture** (600-800 lines) - UI requirements, component hierarchy
2. **S20: Component Library** (800-1000 lines) - Atoms, molecules, organisms, Storybook
3. **S30-S60: Feature Pages** (600-800 lines each, 3-4 stories) - Dashboard, Settings, Reports
4. **S70: API Integration** (700-900 lines) - API client, auth, error handling
5. **S80: Testing** (800-1000 lines) - Unit, component, E2E, accessibility tests
6. **S90: Performance & Optimization** (600-800 lines) - Code splitting, lazy loading
7. **S100: Deployment & Monitoring** (500-700 lines) - Build pipeline, CDN, monitoring

#### Infrastructure/DevOps (Kubernetes, CI/CD, Monitoring)
**When epic focuses on:** IaC, deployment pipelines, observability

**Typical 5-7 stories:**
1. **S10: Infrastructure Design** (500-700 lines) - Architecture, resource requirements
2. **S20: IaC Implementation** (800-1000 lines) - Terraform/Bicep, modules
3. **S30: CI/CD Pipeline** (700-900 lines) - GitHub Actions, Azure DevOps
4. **S40: Monitoring & Alerting** (600-800 lines) - Prometheus, Grafana, alerts
5. **S50: Security Hardening** (700-900 lines) - Secrets, IAM, network policies
6. **S60: Disaster Recovery** (600-800 lines) - Backups, restore procedures
7. **S70: Runbooks** (500-700 lines) - Incident response, troubleshooting

#### Data Engineering (ETL, Analytics, ML)
**When epic focuses on:** Data pipelines, transformations, analytics

**Typical 6-9 stories:**
1. **S10: Data Model & Sources** (600-800 lines) - Schema, data sources, lineage
2. **S20: Ingestion Pipeline** (800-1000 lines) - Extract from sources, connectors
3. **S30: Transformation Logic** (900-1200 lines) - Cleansing, enrichment, business rules
4. **S40: Storage Layer** (700-900 lines) - Data lake, warehouse, partitioning
5. **S50: Query API** (800-1000 lines) - REST/GraphQL for data access
6. **S60: Visualization** (700-900 lines) - Dashboards, reports, alerts
7. **S70: Data Quality** (600-800 lines) - Validation, anomaly detection
8. **S80: Performance Tuning** (600-800 lines) - Indexing, caching, optimization
9. **S90: Orchestration** (500-700 lines) - Airflow/Prefect DAGs, scheduling

### Step 3: Check Existing Stories (Idempotency)

**For each story in the determined pattern:**

```bash
# Check if story already exists
ls .github/copilot/plan/E05-billing-subscriptions/S10-analysis/

# If directory exists with README.md -> SKIP this story
# If directory missing -> CREATE this story
```

**Numbering rules:**
- Use semantic numbering with gaps of 10: S10, S20, S30, S40...
- If adding new stories later: Find highest existing number (e.g., S140), add new as S150, S160...
- This allows interleaving (if S20 needs splitting, add S21, S22 between S20 and S30)

**Skip story if:**
- Directory exists: `.github/copilot/plan/E05-billing/S10-analysis/`
- Story README exists: `.github/copilot/plan/E05-billing/S10-analysis/README.md`
- Story status is "Complete" in README.md

### Step 4: Story Content Structure

**Each story README.md contains:**

```markdown
# E0X SYY: [Story Title]

**Story:** [One sentence objective]
**Priority:** Critical/High/Medium/Low
**Effort:** X hours
**Status:** Not Started/In Progress/Complete
**Dependencies:** [SXX, SYY] or None

## Objective

[2-3 paragraphs explaining what this story accomplishes, why it matters, how it fits in the epic]

## Business Context

[Revenue impact, user impact, compliance requirements - quantify where possible]
- Revenue: ₹XL/month enabled by this story
- Users: X users impacted
- Compliance: GDPR, PCI DSS requirements

## [Domain-Specific Sections]

### For Backend Stories (Domain/Infrastructure/Application/API):

#### 1. [Component Name - e.g., Aggregates, Repositories, Command Handlers, Controllers]

[Description paragraph explaining purpose, responsibilities, patterns used]

**Implementation (Complete Code Example):**

```csharp
// Full, runnable code with:
// - using statements
// - namespace (using {namespace.*} from parameters)
// - complete class/record definition
// - all methods with logic (not pseudocode)
// - following project patterns from parameters ({patterns.errorHandling}, {patterns.domainModeling}, etc.)

using {namespace.root}.Billing{namespace.domainSuffix}.ValueObjects;

namespace {namespace.root}.Billing{namespace.domainSuffix}.Aggregates;

// Example with parameters substituted:
// using Notebook.School.Billing.Domain.ValueObjects;
// namespace Notebook.School.Billing.Domain.Aggregates;

public record Package
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Money Price { get; init; }
    public bool IsActive { get; init; } = true;
    
    // Factory method with Result<T> pattern
    public static Result<Package> Create(string name, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Package>.Failure("Package name is required");
        
        if (price.Amount <= 0)
            return Result<Package>.Failure("Package price must be positive");
        
        return Result<Package>.Success(new Package
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            IsActive = true
        });
    }
    
    // Business logic methods
    public Result<Package> Deactivate()
    {
        if (!IsActive)
            return Result<Package>.Failure("Package already inactive");
        
        return Result<Package>.Success(this with { IsActive = false });
    }
}
```

#### 2. [Next Component]

[Repeat pattern with complete code examples]

### For Frontend Stories (Components/Pages/State Management):

#### 1. [Component Name - e.g., Button, Form, Dashboard]

[Description of component purpose, props, state, behavior]

**Implementation (Complete Code Example):**

```typescript
// Full component with:
// - imports
// - TypeScript types
// - hooks (useState, useEffect, custom hooks)
// - event handlers
// - JSX structure

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useToast } from '@/hooks/use-toast';

interface SubscriptionFormProps {
  packages: Package[];
  onSubmit: (data: SubscriptionData) => Promise<void>;
}

export const SubscriptionForm: React.FC<SubscriptionFormProps> = ({ packages, onSubmit }) => {
  const [selectedPackage, setSelectedPackage] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { toast } = useToast();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    
    try {
      await onSubmit({ packageId: selectedPackage });
      toast({ title: 'Success', description: 'Subscription created' });
    } catch (error) {
      toast({ title: 'Error', description: error.message, variant: 'destructive' });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* Full JSX structure */}
    </form>
  );
};
```

### For Infrastructure Stories (IaC/Deployment/Monitoring):

#### 1. [Resource Name - e.g., Kubernetes Deployment, Terraform Module]

[Description of infrastructure resource, configuration, dependencies]

**Implementation (Complete Configuration):**

```yaml
# Full Kubernetes manifest with:
# - apiVersion, kind, metadata
# - Complete spec with all fields
# - Resource limits, probes, env vars

apiVersion: apps/v1
kind: Deployment
metadata:
  name: billing-service
  namespace: production
  labels:
    app: billing-service
    tier: backend
spec:
  replicas: 3
  selector:
    matchLabels:
      app: billing-service
  template:
    metadata:
      labels:
        app: billing-service
    spec:
      containers:
      - name: billing-api
        image: acr.io/billing-service:1.0.0
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
```

## Success Criteria

- [ ] [Specific, testable acceptance criterion 1]
- [ ] [Specific, testable acceptance criterion 2]
- [ ] [Specific, testable acceptance criterion 3]
- [ ] Unit test coverage >80% (for code stories)
- [ ] Integration test coverage >85% (for integration stories)
- [ ] Code compiles without errors
- [ ] All linting/formatting rules pass
- [ ] Performance benchmarks met (if applicable)
- [ ] Security review completed (if applicable)

## Testing Strategy (for implementation stories)

### Unit Tests
[What to test, mocking strategy, test data approach]

**Example Test:**
```csharp
public class PackageTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "Premium Plan";
        var price = new Money(1000, "INR");
        
        // Act
        var result = Package.Create(name, price);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Price.Should().Be(price);
    }
    
    [Fact]
    public void Create_WithInvalidName_ShouldFail()
    {
        // Arrange, Act
        var result = Package.Create("", new Money(1000, "INR"));
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("name is required");
    }
}
```

### Integration Tests (if applicable)
[What integration points to test, test data setup, cleanup strategy]

### E2E Tests (if applicable)
[User workflows to test, test environment setup]

## Dependencies

### Technical Dependencies
- [NuGet package/npm package/library with version]
- [External API/service]

### Story Dependencies
- **Blocks:** S40, S50 (cannot start until this completes)
- **Blocked By:** S10, S20 (must complete before starting this)

## Next Steps

[What comes after this story - next story in sequence, testing required, deployment actions]

---

**Story Created:** [Date] by [GitHub Copilot Agent]
**Story Completed:** [Date] (when status = Complete)
```

### Step 5: Context Length Management

**If story README.md approaches 1000 lines during writing:**

1. **Stop writing immediately**
2. **Complete current section, then close the story**
3. **Immediately invoke `story-to-tasks` prompt:**
   - User: "Break down E05 S40 into tasks"
   - Or agent auto-invokes if context allows

**Alternative approach (split story):**
- If story has natural split (e.g., S40 Infrastructure is 1500 lines)
- Split into: S40 (Repositories, 700 lines) and S41 (External Adapters, 800 lines)
- Adjust story numbers: Original S50 becomes S50 (keep numbering consistent)

**Why this matters:**
- Stories >1000 lines are hard to execute atomically
- Breaking into tasks provides better granularity
- Prevents overwhelming context for implementation agent

### Step 6: Create Story Directories and Files

**For each new story:**

```bash
# Create directory
mkdir -p .github/copilot/plan/E05-billing-subscriptions/S30-domain-layer

# Create README.md with story content
# (Write content from Step 4 template)

# Commit after each story (or batch 2-3 if small)
git add .github/copilot/plan/E05-billing-subscriptions/S30-domain-layer/
git commit -m "feat: E05 S30 Domain Layer story (1420 lines)

Implements core domain model for billing & subscriptions.
- Package aggregate with pricing tiers and trial config
- Subscription aggregate with lifecycle states and renewal logic
- Invoice aggregate with line items and tax calculation
- Value objects: Money (currency-aware), Address (encryption support)
- Domain events: 12 events for aggregate state changes"
```

**Commit strategy:**
- **Small stories (<600 lines):** Batch 2-3 stories per commit
- **Medium stories (600-1000 lines):** 1 story per commit
- **Large stories (>1000 lines):** Immediately break into tasks (see Step 5)

### Step 7: Idempotency Verification

**Before completing, verify:**

```bash
# Check what was created
ls .github/copilot/plan/E05-billing-subscriptions/

# Expected output for new stories only:
# S70-subscription-plans/ (new)
# S80-invoicing/ (new)
# S90-payment-gateway/ (new)

# Existing stories untouched:
# S10-analysis/ (unchanged)
# S20-design/ (unchanged)
# S30-domain-layer/ (unchanged)
```

**Idempotency rules:**
- If story directory exists with README.md -> **DO NOT recreate or modify**
- If story in epic pattern but missing from epic directory -> **CREATE**
- If story marked "Complete" in README.md -> **DO NOT modify**
- If adding new stories, number them S150, S160... (jump by 10 from last existing)

### Step 8: Quality Checklist

Before marking complete, verify:
- [ ] All stories for epic pattern created (10-15 for backend, 7-10 for frontend, etc.)
- [ ] Each story has appropriate depth (varies: 500-1800 lines based on complexity)
- [ ] Stories >1000 lines have path to task breakdown (noted in Next Steps or auto-invoke)
- [ ] Code examples are complete and runnable (not pseudocode or "TODO" comments)
- [ ] Business rules from S10 Analysis appear in implementation stories (S30-S60)
- [ ] Database schema from S20 Design appears in S40 Infrastructure
- [ ] API contracts from S20 Design appear in S60 API Layer
- [ ] Test coverage requirements specified (>80% unit, >85% integration)
- [ ] Dependencies between stories documented (blocks/blocked by)
- [ ] Effort estimates realistic (sum matches epic estimate)
- [ ] Story numbers jump by 10 (S10, S20, S30...) for future interleaving
- [ ] Existing stories NOT modified (idempotency)
- [ ] All commits have detailed commit messages with bullet points
- [ ] Technology-specific patterns followed (e.g., React hooks, .NET Result<T>, Go error handling)

---

## Example Execution

**User:** "Break down E05 Billing & Subscriptions into stories"

**Agent Response:**

1. **Read epic README:**
   ```bash
   # Read epic context
   cat .github/copilot/plan/E05-billing-subscriptions/README.md
   # Extract: Backend microservice, domain-driven design, 12-15 stories estimated
   ```

2. **Determine story pattern:**
   - Epic type: Backend domain implementation (C#/.NET)
   - Pattern: Analysis, Design, Domain, Infrastructure, Application, API, 5 Feature Stories, Testing, Security, Production, Gap Analysis (15 stories total)

3. **Check existing stories:**
   ```bash
   ls .github/copilot/plan/E05-billing-subscriptions/
   # Output: S10-analysis/, S20-design/, S30-domain-layer/ (3 stories exist)
   # Decision: Create S40-S150 (12 stories)
   ```

4. **Create S40 Infrastructure Layer:**
   - Directory: `.github/copilot/plan/E05-billing-subscriptions/S40-infrastructure-layer/`
   - Content: 1580 lines (EF Core DbContext, 11 entity configurations, 6 repositories, payment gateway adapters)
   - Commit: `feat: E05 S40 Infrastructure Layer story (1580 lines)`

5. **Create S50 Application Layer:**
   - Directory: `.github/copilot/plan/E05-billing-subscriptions/S50-application-layer/`
   - Content: 1487 lines (15 command handlers, 10 query handlers, DTOs, validation)
   - Commit: `feat: E05 S50 Application Layer story (1487 lines)`

6. **Create S60 API Layer:**
   - Directory: `.github/copilot/plan/E05-billing-subscriptions/S60-api-layer/`
   - Content: 453 lines (5 controllers, 27 endpoints, JWT auth, rate limiting)
   - Commit: `feat: E05 S60 API Layer story (453 lines)`

7. **Create S70-S110 Feature Stories** (5 stories, 1200-1600 lines each):
   - S70: Subscription Plan Management
   - S80: Invoicing & Billing Cycles
   - S90: Payment Gateway Integration
   - S100: Discount & Coupon System
   - S110: Revenue Reporting & Analytics
   - Commit each individually

8. **Create S120 Integration Testing:**
   - Content: 1389 lines (Testcontainers, E2E workflows, k6 performance tests)
   - Commit: `feat: E05 S120 Integration Testing story (1389 lines)`

9. **Create S130 Authorization & Security:**
   - Content: 1504 lines (RBAC, audit logging, encryption, rate limiting)
   - Commit: `feat: E05 S130 Authorization & Security story (1504 lines)`

10. **Create S140 Production Readiness:**
    - Content: 1686 lines (Observability, resilience, K8s deployment, monitoring)
    - Commit: `feat: E05 S140 Production Readiness story (1686 lines)`

11. **Create S150 Gap Analysis:**
    - Content: 800 lines (Compare legacy vs new, identify missing features)
    - Note: "S150 requires task breakdown - run `story-to-tasks` prompt next"
    - Commit: `feat: E05 S150 Gap Analysis story (800 lines)`

12. **Final verification:**
    ```bash
    ls .github/copilot/plan/E05-billing-subscriptions/
    # Output: S10, S20, S30, S40, S50, S60, S70, S80, S90, S100, S110, S120, S130, S140, S150 (15 stories)
    ```

13. **Report to user:**
    ```
    ✅ E05 Billing & Subscriptions Stories Created
    
    📊 Stories Created: 12 new stories (S40-S150)
    - S40: Infrastructure Layer (1580 lines, EF Core + repositories)
    - S50: Application Layer (1487 lines, CQRS handlers)
    - S60: API Layer (453 lines, REST endpoints)
    - S70-S110: 5 Feature Stories (6557 lines total)
    - S120: Integration Testing (1389 lines, Testcontainers)
    - S130: Authorization & Security (1504 lines, RBAC)
    - S140: Production Readiness (1686 lines, K8s + monitoring)
    - S150: Gap Analysis (800 lines, legacy comparison)
    
    📊 Stories Skipped: 3 (already exist)
    - S10: Analysis ✅
    - S20: Design ✅
    - S30: Domain Layer ✅
    
    📂 Location: .github/copilot/plan/E05-billing-subscriptions/
    
    🔄 Next Step:
    Break down S150 into tasks (gap analysis requires implementation roadmap):
    "Break down E05 S150 into tasks"
    
    Or start implementation from S40 onwards using existing stories.
    
    ⏱️ Total Effort: ~160 hours (2 engineers × 4 weeks)
    ```

---

## Technology-Specific Adaptations

### Backend (C#/.NET, Java, Python, Go)
- **Code examples:** Use language-specific idioms (C# records, Java records, Python dataclasses, Go structs)
- **Patterns:** Result<T>/Option<T> (C#), Try/Either (Scala), Result (Rust/Go)
- **Testing:** xUnit/NUnit (C#), JUnit (Java), pytest (Python), testing package (Go)

### Frontend (React, Angular, Vue, Svelte)
- **Code examples:** TypeScript with framework-specific patterns
- **Hooks:** useState, useEffect, custom hooks (React), signals (Angular), composables (Vue)
- **State Management:** Context API, Zustand, Redux (React), NgRx (Angular), Pinia (Vue)

### Infrastructure (Terraform, Kubernetes, Ansible)
- **Code examples:** HCL for Terraform, YAML for K8s/Ansible
- **Modules:** Reusable modules with variables, outputs, dependencies
- **Security:** Secrets management, IAM roles, network policies

### Data (Spark, Airflow, dbt)
- **Code examples:** SQL, Python, YAML (dbt models, Airflow DAGs)
- **Testing:** dbt tests, Great Expectations, data quality checks

---

## Prompt Chaining

**After this prompt completes, user typically runs:**
- `story-to-tasks` for complex stories (>1000 lines or S150 Gap Analysis): "Break down E05 S150 into tasks"
- Or starts implementation using stories as-is if <1000 lines

**Agent can auto-invoke `story-to-tasks` if:**
- Story exceeds 1000 lines during writing
- Story is S150 Gap Analysis (always needs task breakdown)
- User says: "Create stories and break down complex ones into tasks"

---

## Idempotency Examples

**Scenario 1: Re-running on same epic**
```bash
# First run: Created S10-S150 (15 stories)
ls .github/copilot/plan/E05-billing-subscriptions/
# Output: S10, S20, ..., S150

# Second run (same epic):
# Agent checks existence, finds all 15 stories exist
# Agent: "All stories for E05 already exist. No new stories created."
```

**Scenario 2: Epic scope expanded, new stories added**
```bash
# Original: S10-S150 exist
# Epic README updated: Add "Multi-Currency Support" feature

# Run prompt again:
# Agent checks: S10-S150 exist, new feature story missing
# Agent creates: S160-multi-currency-support/ (next semantic number)
# Agent does NOT modify S10-S150
```

**Scenario 3: Story split due to complexity**
```bash
# Original: S40 Infrastructure Layer (1800 lines, too large)

# User: "Split S40 into smaller stories"
# Agent:
# - Renames S40 to S40-repositories/ (700 lines)
# - Creates S41-external-adapters/ (600 lines)
# - Creates S42-caching-layer/ (500 lines)
# - Adjusts S50, S60... numbering (keep them, they depend on S40-S42 collectively)
```

**Scenario 4: Story marked complete**
```bash
# S30-domain-layer/README.md has:
# **Status:** Complete

# Run prompt again (even if S30 scope changes):
# Agent: "S30 marked Complete, skipping modification"
# Agent: Does not overwrite completed work
# If new work needed: Create S31 or S160 (new story)
```

---

## Error Handling

**Missing epic README:**
```
Agent: "Cannot find epic README at .github/copilot/plan/E05-billing-subscriptions/README.md
Please verify epic directory or run `analysis-to-epics` prompt first."
```

**Unclear story pattern from epic:**
```
Agent: "Epic README does not clearly indicate technology stack or architecture pattern.
Defaulting to backend domain implementation pattern (15 stories).
If this is incorrect, please update epic README with:
- Technology Stack: [Backend/Frontend/Infrastructure/Data]
- Architecture: [Microservices/Monolith/SPA/Pipeline]
Then re-run this prompt."
```

**Conflicting story numbers:**
```
Agent: "Found S40 Infrastructure Layer but also S40-repositories/. 
This suggests a split occurred. Using S41, S42 for new stories to avoid conflicts."
```

---

## Output Summary Template

**After completion, agent reports:**

```
✅ Epic-to-Stories Complete

📊 Stories Created: [Number]
- S40: Infrastructure Layer (1580 lines, 16h effort)
- S50: Application Layer (1487 lines, 20h effort)
- S60: API Layer (453 lines, 12h effort)
- S70-S110: 5 Feature Stories (6557 lines, 68h effort)
- S120: Integration Testing (1389 lines, 14h effort)
- S130: Security (1504 lines, 18h effort)
- S140: Production (1686 lines, 22h effort)
- S150: Gap Analysis (800 lines, 10h effort)

📊 Stories Skipped: [Number] (already exist)
- S10: Analysis ✅
- S20: Design ✅
- S30: Domain Layer ✅

📂 Location: .github/copilot/plan/E05-billing-subscriptions/

🔄 Next Steps:
1. Break down S150 into tasks: "Break down E05 S150 into tasks"
2. Or start implementation from S40 using stories as roadmap

⏱️ Total Effort: 180 hours
- Implementation: 160 hours (S40-S140)
- Planning: 20 hours (S10-S20 already complete)

📋 Story Pattern Used: Backend Domain Implementation (15 stories)
- Analysis/Design: S10-S20
- Core Layers: S30-S60
- Features: S70-S110
- Quality: S120-S140
- Migration: S150
```

---

**This prompt is self-contained, idempotent, and ready for a fresh GitHub Copilot agent with no prior context.**
