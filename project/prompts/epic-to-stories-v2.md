# Epic-to-Stories Methodology

**Purpose:** Convert an Epic README into actionable Story-level work packages. Each Story is a cohesive unit delivering specific technical or business value, completable in 8-24 hours.

**Invocation:** `"Break down [Epic Name/ID] into stories"`

---

## Core Principles

1. **Idempotency:** Never modify or recreate existing stories - only create missing ones
2. **Completeness:** Stories contain full, runnable code examples, not pseudocode or TODOs
3. **Right-Sized:** Stories are 500-1800 lines based on complexity; >1000 lines may need task breakdown
4. **Value-Driven:** Each story delivers independently demonstrable and testable outcomes
5. **Context-Aware:** Adapt to project's technology stack, patterns, and existing conventions

---

## Execution Flow

### Phase 1: Discover Project Context

**Goal:** Understand the project's structure, patterns, and conventions before creating stories.

#### Option A: Structured Parameters (If Available)
Look for project parameters file in common locations:
- `.github/copilot/prompts/project-parameters.json`
- `project/prompts/project-parameters.json`
- `docs/project-parameters.json`

**Extract if present:**
- Epic/story directory paths
- Story naming conventions (prefix, numbering gaps)
- Technology stack and frameworks
- Architectural patterns
- Testing requirements

#### Option B: Contextual Discovery (Fallback)
Gather context from:

1. **Epic README**: Technology stack, architecture, domain model
2. **Project instructions**: `.github/copilot-instructions.md` or similar
3. **Existing stories**: Review similar epics for established patterns
4. **Project structure**: Infer from directory layout and file types

**Use sensible defaults when uncertain:**
- Story numbers: S10, S20, S30... (gaps of 10 for flexibility)
- Default location: `project/plan/` or `.github/copilot/plan/`
- Line targets: 500-1800 lines per story
- Test coverage: >80% unit tests, >85% integration tests

---

### Phase 2: Determine Story Pattern

**Goal:** Select appropriate story breakdown based on epic type and project phase.

Stories vary by context. Analyze the epic to determine which pattern applies:

#### Pattern Recognition Questions:
1. **What is being built?** (Service, UI, Infrastructure, Pipeline, Model)
2. **What phase is this?** (Greenfield, Enhancement, Migration, Refactor)
3. **What's the complexity?** (Simple, Medium, Complex, Very Complex)
4. **What's the risk?** (Low, Medium, High, Critical)

#### Common Story Patterns

**Backend Service (Domain-Driven)**
~10-15 stories for new microservice
- Analysis & Domain Modeling (1-2 stories)
- Architecture & Design (1 story)
- Core Domain Implementation (2-3 stories)
- Infrastructure Layer (1-2 stories)
- Application/API Layer (1-2 stories)
- Feature Extensions (3-5 stories)
- Quality & Operations (2-3 stories)

**Frontend Application (Component-Based)**
~7-10 stories for new UI
- Design System & Architecture (1 story)
- Core Component Library (1-2 stories)
- Feature Pages/Views (3-5 stories)
- Integration & State Management (1 story)
- Testing & Accessibility (1-2 stories)
- Performance & Deployment (1 story)

**Infrastructure/DevOps**
~5-8 stories for platform work
- Architecture & Requirements (1 story)
- Infrastructure as Code (1-2 stories)
- CI/CD & Automation (1-2 stories)
- Observability (1-2 stories)
- Security & Compliance (1 story)
- Runbooks & Documentation (1 story)

**Data Engineering**
~6-9 stories for data pipeline
- Data Model & Sources (1 story)
- Ingestion & ETL (2-3 stories)
- Storage & Query Layer (1-2 stories)
- Quality & Monitoring (1-2 stories)
- Visualization & Reporting (1 story)

**Enhancement/Feature**
~3-6 stories for existing system changes
- Analysis & Design (1 story)
- Implementation (1-3 stories based on scope)
- Testing & Migration (1 story)
- Documentation (1 story if significant)

**Migration/Refactor**
~4-8 stories for technical debt
- Analysis & Gap Assessment (1 story)
- Architecture Planning (1 story)
- Incremental Migration (2-4 stories)
- Testing & Validation (1 story)
- Cutover & Cleanup (1 story)

**Adapt patterns based on:**
- Epic scope (larger = more stories)
- Technical complexity (complex = more design/testing stories)
- Team size (smaller teams = fewer parallel stories)
- Risk level (higher risk = more validation stories)

---

### Phase 3: Check for Existing Stories

**Goal:** Maintain idempotency by never recreating existing work.

For each story in the chosen pattern:

```bash
# Check if story directory and README exist
ls [epic-directory]/S[number]-[story-slug]/README.md
```

**Skip story if:**
- Directory exists with README.md
- Story status is "Complete" or "In Progress"
- Story has substantial content (>200 lines)

**Create story if:**
- Directory doesn't exist
- README is missing or stub-only (<50 lines)
- Status is "Not Started" with minimal content

**Numbering conventions:**
- Use semantic gaps: S10, S20, S30, S40... (allows S21, S22 insertion later)
- Find highest existing number, add new stories with +10 increments
- Maintain consistency with existing story numbers in epic

---

### Phase 4: Craft Story Content

**Goal:** Create comprehensive, actionable stories with complete implementation guidance.

Each story README contains these sections:

```markdown
# [Epic] [Story]: [Story Title]

**Story:** [One-sentence objective]
**Priority:** Critical/High/Medium/Low
**Effort:** X hours
**Status:** Not Started
**Dependencies:** [List] or None

## Objective

[2-3 paragraphs explaining:
- What this story accomplishes
- Why it matters to the epic
- How it fits in the overall architecture
- What value it delivers]

## Business Context

**Revenue Impact:** [Quantified revenue/cost impact if applicable]  
**User Impact:** [Number/type of users affected]  
**Compliance Requirements:** [Regulatory/security requirements]

## [Domain-Specific Implementation Sections]

[Varies by story type - see patterns below]

### For Code Implementation Stories:

#### 1. [Component/Module Name]

[Explanation of purpose, responsibilities, and design decisions]

**Complete Implementation:**

```[language]
// Full, runnable code example with:
// - All imports/using statements
// - Complete type/class definitions
// - Full method implementations (no "// TODO" or "// ... implementation")
// - Error handling
// - Following project patterns
// - Production-ready quality

[Complete working code here]
```

**Key Design Decisions:**
- [Decision 1]: [Rationale]
- [Decision 2]: [Rationale]

#### 2. [Next Component]

[Repeat pattern for each major component]

### For Design/Analysis Stories:

#### Architecture Decisions
[Detailed architecture with diagrams if needed]

#### Technical Specifications
[Schemas, interfaces, contracts]

#### Trade-offs & Alternatives
[What was considered, what was chosen, why]

### For Infrastructure Stories:

#### Configuration
[Complete config files: Kubernetes, Terraform, CI/CD, etc.]

#### Deployment Steps
[Detailed deployment procedures]

#### Verification
[How to verify successful deployment]

## Success Criteria

- [ ] [Specific, testable criterion 1]
- [ ] [Specific, testable criterion 2]
- [ ] [Code quality: compiles, lints pass, formatting correct]
- [ ] [Testing: >X% coverage, all tests pass]
- [ ] [Documentation: inline comments, README updates]
- [ ] [Security: no vulnerabilities, secure patterns used]
- [ ] [Performance: meets defined benchmarks]

## Testing Strategy

### Unit Tests
**What to test:** [Specific components/functions]  
**Mocking strategy:** [What to mock, how]  
**Test data:** [Approach to test data]

**Example Tests:**
```[language]
[Complete test examples showing:
- Arrange-Act-Assert pattern
- Edge cases
- Error conditions
- Happy path]
```

### Integration Tests
**What to test:** [Integration points]  
**Environment setup:** [Test dependencies]  
**Data setup/cleanup:** [Approach]

### E2E Tests (if applicable)
**User workflows:** [Critical paths to test]  
**Test environment:** [Setup requirements]

## Dependencies

### Technical Dependencies
- [Library/Framework with version]
- [External service/API]
- [Infrastructure requirements]

### Story Dependencies
- **Blocks:** [Stories that can't start until this completes]
- **Blocked By:** [Stories that must complete before this starts]
- **Related:** [Stories with loose coupling]

## Implementation Notes

[Any additional context, gotchas, or important considerations for implementers]

## Next Steps

[What comes immediately after - next story, deployment, testing phase]

---

**Story Created:** [Date]  
**Story Completed:** [Date when status = Complete]
```

---

### Phase 5: Manage Story Size

**Goal:** Keep stories implementable and avoid overwhelming context.

**If story approaches 1000 lines while writing:**

1. **Option A: Complete and Flag for Task Breakdown**
   - Finish the current story
   - Note in "Next Steps": "This story should be broken into tasks before implementation"
   - Continue to next story

2. **Option B: Split into Multiple Stories**
   - Stop at natural boundary (e.g., after component section)
   - Create second story: S[N+1] with continuation
   - Adjust subsequent story numbers if needed
   - Link stories in Dependencies section

3. **Option C: Create Sub-Stories**
   - Original: S40 Infrastructure Layer
   - Split: S40 (Repositories), S41 (External Adapters), S42 (Caching)
   - Next story remains S50 (or becomes S45 if natural fit)

**Story size guidelines by type:**
- **Analysis/Design:** 500-1000 lines (more text, less code)
- **Simple Implementation:** 600-1000 lines (focused scope)
- **Complex Implementation:** 1200-1600 lines (multiple components)
- **Testing/Operations:** 800-1200 lines (comprehensive coverage)

**When to break into tasks:**
- Story >1000 lines with many distinct implementation units
- Story has natural sequential phases (each could be a task)
- Story spans multiple developers' expertise areas
- Story has significant unknowns requiring investigation

---

### Phase 6: Create Story Files

**Goal:** Generate story directories and files with proper version control.

**For each new story:**

```bash
# Create story directory
mkdir -p [epic-path]/S[number]-[slug]

# Create README.md with story content
# (Content from Phase 4)

# Add to version control
git add [epic-path]/S[number]-[slug]/
```

**Commit strategy:**
- **Small stories (<600 lines):** Can batch 2-3 per commit
- **Medium stories (600-1000 lines):** 1 story per commit
- **Large stories (>1000 lines):** 1 story per commit with detailed message

**Commit message format:**
```
feat([Epic]): S[number] [Story Title] ([line-count] lines)

[Brief description of story scope]

Key components:
- [Component 1]: [Brief description]
- [Component 2]: [Brief description]
- [Component 3]: [Brief description]

Effort: [X] hours
Dependencies: [List or "None"]
```

---

### Phase 7: Verify Quality

**Goal:** Ensure stories are complete, correct, and ready for implementation.

**Quality checklist:**
- [ ] All stories for chosen pattern created (not skipped arbitrarily)
- [ ] Existing stories unchanged (idempotency maintained)
- [ ] Each story has appropriate depth for complexity
- [ ] Code examples are complete and runnable (no pseudocode)
- [ ] Technology-specific patterns followed correctly
- [ ] Story numbers use consistent gaps (e.g., 10)
- [ ] Dependencies between stories documented
- [ ] Success criteria are specific and testable
- [ ] Testing strategy is comprehensive
- [ ] Effort estimates are realistic
- [ ] Stories >1000 lines flagged for task breakdown
- [ ] All commits have meaningful messages
- [ ] Business rules from early stories flow into later ones
- [ ] Design decisions from design stories appear in implementation

**Cross-reference validation:**
- Architecture from S20 Design → matches S40 Infrastructure implementation
- Domain model from S30 Domain → matches S50 Application usage
- API contracts from S20 Design → matches S60 API implementation
- Test coverage from all stories → meets project standards

---

### Phase 8: Report Completion

**Goal:** Provide clear summary of work done and next steps.

**Report to user:**

```
✅ Epic-to-Stories Complete: [Epic Name]

📊 Stories Created: [Number]
[List each story with: Number, Title, Lines, Effort]

📊 Stories Skipped: [Number] (already exist)
[List skipped with reason]

📂 Location: [Path to epic directory]

📋 Story Pattern Used: [Pattern Name]
[Brief pattern description]

⏱️ Total Effort: [Hours]
- Planning/Design: [Hours] ([Story numbers])
- Implementation: [Hours] ([Story numbers])
- Testing/Quality: [Hours] ([Story numbers])

🔄 Recommended Next Steps:
1. [Most important next action]
2. [Second action]
3. [Third action]

⚠️ Attention Required:
[Any stories needing task breakdown or special attention]

🎯 Ready for Implementation:
[List of stories ready to start immediately]
```

---

## Technology-Specific Adaptations

### Code Example Patterns

**Backend (C#/.NET)**
```csharp
// Result<T> pattern, records, async/await
public record Customer { ... }
public static Result<Customer> Create(...) { ... }
```

**Backend (Java)**
```java
// Records (Java 17+), Optional, Stream API
public record Customer(...) { ... }
public Optional<Customer> findById(...) { ... }
```

**Backend (Python)**
```python
# Dataclasses, type hints, async/await
@dataclass
class Customer:
    id: UUID
    name: str
```

**Backend (Go)**
```go
// Structs, error handling, interfaces
type Customer struct { ... }
func NewCustomer(...) (*Customer, error) { ... }
```

**Frontend (React/TypeScript)**
```typescript
// Hooks, functional components, TypeScript
interface CustomerProps { ... }
export const Customer: FC<CustomerProps> = ({ ... }) => {
  const [state, setState] = useState(...);
  // ...
}
```

**Frontend (Vue)**
```typescript
// Composition API, reactive refs
<script setup lang="ts">
const customer = ref<Customer>()
const fetchCustomer = async () => { ... }
</script>
```

**Infrastructure (Kubernetes)**
```yaml
# Deployments, services, config
apiVersion: apps/v1
kind: Deployment
metadata: { ... }
spec: { ... }
```

**Infrastructure (Terraform)**
```hcl
# Resources, variables, outputs
resource "aws_instance" "server" {
  # ...
}
```

---

## Error Handling

**Missing Epic README:**
```
❌ Cannot find epic README at [path]
Please verify epic directory or create epic first
```

**Ambiguous Pattern:**
```
⚠️ Epic type unclear from README
Defaulting to: [Pattern Name]
If incorrect, update epic README with:
- Technology Stack: [...]
- Architecture: [...]
Then re-run this process
```

**Conflicting Story Numbers:**
```
⚠️ Found both S40 and S40-[name]/ directories
Using S41, S42... for new stories to avoid conflicts
```

**Incomplete Context:**
```
⚠️ Limited project context available
Using default conventions:
- Story prefix: S
- Numbering gap: 10
- Line targets: 500-1800
Verify these match project standards
```

---

## Advanced Scenarios

### Scenario 1: Adding Stories to Existing Epic

**Situation:** Epic has S10-S100, need to add new feature story

**Approach:**
1. Identify highest number: S100
2. Create new story: S110 (next increment)
3. Don't renumber existing stories
4. Link dependencies to existing stories

### Scenario 2: Splitting Oversized Story

**Situation:** S40 is 1800 lines, too large

**Approach:**
1. Rename S40 → S40 (Part 1) with first components
2. Create S41 (Part 2) with remaining components
3. Update S50 dependencies to reference both S40 and S41
4. Keep subsequent numbering (S50, S60...) unchanged

### Scenario 3: Interleaving Forgotten Story

**Situation:** Realized need story between S20 and S30

**Approach:**
1. Create S21 or S25 (use gap between 20 and 30)
2. Don't renumber S30, S40, S50...
3. Update dependencies in subsequent stories
4. This is why we use gaps of 10!

### Scenario 4: Epic Scope Change

**Situation:** Epic expanded with new requirements

**Approach:**
1. Check existing stories: Don't modify completed ones
2. Add new stories at end: S110, S120, S130...
3. Or interleave if needed: S35, S45, S55...
4. Update epic README with scope change notes

---

## Best Practices Summary

**DO:**
- ✅ Read project context before creating stories
- ✅ Check for existing stories (idempotency)
- ✅ Write complete, runnable code examples
- ✅ Use semantic numbering with gaps
- ✅ Include comprehensive testing strategy
- ✅ Document dependencies clearly
- ✅ Commit stories individually or in small batches
- ✅ Flag large stories for task breakdown
- ✅ Adapt to project's technology and patterns
- ✅ Provide clear success criteria

**DON'T:**
- ❌ Recreate existing stories
- ❌ Use pseudocode or TODO comments
- ❌ Create stories >1800 lines without flagging
- ❌ Skip testing strategy sections
- ❌ Use arbitrary numbering (S1, S2, S3...)
- ❌ Ignore existing project patterns
- ❌ Leave dependencies undocumented
- ❌ Forget business context and value
- ❌ Write vague success criteria
- ❌ Modify completed stories

---

## Quick Reference

**Story Size Targets:**
- Analysis/Design: 500-1000 lines
- Simple Implementation: 600-1000 lines
- Complex Implementation: 1200-1600 lines
- Testing/Quality: 800-1200 lines
- >1000 lines: Consider task breakdown

**Common Story Counts:**
- Backend Service: 10-15 stories
- Frontend App: 7-10 stories
- Infrastructure: 5-8 stories
- Data Pipeline: 6-9 stories
- Enhancement: 3-6 stories
- Migration: 4-8 stories

**Time Estimates:**
- Analysis: 8-16 hours per story
- Design: 8-16 hours per story
- Implementation: 12-24 hours per story
- Testing: 8-16 hours per story

**Remember:** These are guidelines, not rules. Adapt based on:
- Project complexity
- Team experience
- Technical risk
- Business criticality

---

**This methodology is framework-agnostic, idempotent, and designed for autonomous agent execution with minimal human intervention.**
