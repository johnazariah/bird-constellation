# Analysis-to-Epics Prompt

**Purpose:** Convert an analysis document into Epic-level breakdown. Each Epic represents a logical, closely-related chunk of business value that can be independently planned and tracked.

**Invocation:** "Create epics from analysis" or "Break down [analysis-file] into epics"

---

## Step 0: Load Project Parameters

**CRITICAL: Read project parameters first to configure this prompt for the current project.**

```bash
# Read project-specific configuration
cat .github/copilot/prompts/project-parameters.json
```

**Extract and use these parameters throughout:**
- `paths.analysis` - Where to find analysis documents (default: `.github/copilot/analysis/`)
- `paths.plan` - Where to create epic folders (default: `.github/copilot/plan/`)
- `paths.instructions` - Project coding standards (default: `.github/copilot-instructions.md`)
- `naming.epicPrefix` - Epic prefix (default: `E`)
- `naming.numberingGap` - Numbering gap (default: `10`)
- `naming.epicFormat` - Epic folder format (default: `E{number:D2}-{name-kebab-case}`)
- `project.name` - Project name for examples
- `technology.*` - Tech stack for examples
- `patterns.*` - Coding patterns for examples
- `namespace.root` - Namespace for code examples
- `businessMetrics.currency` - Currency symbol for revenue
- `commitConventions.epicFormat` - Commit message format

**If project-parameters.json not found:**
```
Agent: "⚠️ project-parameters.json not found at .github/copilot/prompts/
Using default parameters for generic project.
To customize for your project, create project-parameters.json (see README.md)"
```

Then use these defaults:
- Analysis path: `.github/copilot/analysis/`
- Plan path: `.github/copilot/plan/`
- Epic format: `E{number:D2}-{name}`
- Numbering gap: 10

---

## Prerequisites

Before starting, the agent must have:
1. **Project parameters loaded** (from project-parameters.json)
2. Path to analysis document (from `{paths.analysis}` parameter or user-specified)
3. Destination folder for epics (from `{paths.plan}` parameter)
4. Project coding standards (from `{paths.instructions}` parameter)

---

## Execution Sequence

### Step 1: Context Gathering

**Read these files in this order:**

1. **Project Parameters** (REQUIRED - already loaded in Step 0)
   - Contains: Paths, naming conventions, tech stack, patterns
   - Used throughout: Replace hardcoded values with parameter references

2. **Analysis Document** (REQUIRED)
   - Location: `{paths.analysis}` from parameters (e.g., `.github/copilot/analysis/`) or user-specified path
   - Contains: Epic definitions, business requirements, domain boundaries
   - Extract: Epic numbers, epic names, epic descriptions, business value

3. **Project Instructions** (REQUIRED)
   - Location: `{paths.instructions}` from parameters (e.g., `.github/copilot-instructions.md`)
   - Contains: Coding standards, architectural patterns, naming conventions
   - Extract: Technology stack, paradigms (functional/OOP), file naming rules

4. **Existing Epics** (CHECK FOR IDEMPOTENCY)
   ```bash
   # Use paths.plan from parameters
   ls -la {paths.plan}
   ```
   - Output: List of existing epic directories ({epicPrefix}01-name/, {epicPrefix}02-name/, etc.)
   - Decision: **Skip epics that already exist**, only create missing ones

### Step 2: Identify Epics to Create

**From analysis document, extract:**
- Epic number (using `{epicPrefix}` from parameters: {epicPrefix}01, {epicPrefix}02, {epicPrefix}03...)
- Epic name (short, kebab-case: `billing-subscriptions`, `user-management`)
- Epic description (1-2 paragraphs)
- Business value (revenue impact using `{businessMetrics.currency}` and `{businessMetrics.revenueFormat}`, user impact, compliance)
- Dependencies on other epics ({epicPrefix}01 must complete before {epicPrefix}05, etc.)

**Check existence:**
```bash
# For each epic in analysis document
# Use paths.plan and naming.epicFormat from parameters
ls {paths.plan}/{epicPrefix}05-billing-subscriptions/

# If directory exists with README.md -> SKIP
# If directory missing -> CREATE
```

**Determine epic numbering:**
- Use `{naming.numberingGap}` from parameters (default: 10)
- Use numbers from analysis document as-is ({epicPrefix}10, {epicPrefix}20, {epicPrefix}30...)
- If adding new epics to existing plan:
  - Find highest existing epic number (e.g., {epicPrefix}90)
  - New epic starts at {epicPrefix}(90 + numberingGap) = {epicPrefix}100
  - Example: {epicPrefix}90 exists, new epic is {epicPrefix}100, next new epic is {epicPrefix}110

### Step 3: Epic Content Structure

**Each epic README.md contains:**

```markdown
# [Epic Number]: [Epic Name]

**Business Value:** [Revenue/user/compliance impact]
**Priority:** Critical/High/Medium/Low
**Dependencies:** [E01, E03] (must complete first) or None
**Estimated Effort:** X weeks
**Status:** Not Started/In Progress/Complete

## Overview

[2-3 paragraphs describing what this epic accomplishes and why it matters]

## Business Context

### Current State
[What exists today, pain points, limitations]

### Desired State
[What will exist after epic completion, benefits, success metrics]

### Revenue/User Impact
- [Specific metric 1: e.g., "Enable ₹5L/month recurring revenue"]
- [Specific metric 2: e.g., "Support 10K concurrent users"]
- [Specific metric 3: e.g., "Reduce churn by 15%"]

## Scope

### In Scope
- [Feature/capability 1 with brief description]
- [Feature/capability 2 with brief description]
- [Feature/capability 3 with brief description]

### Out of Scope
- [What this epic explicitly does NOT cover]
- [Deferred to future epics]

## Domain Model (if applicable)

### Aggregates
- **[AggregateName]**: [Description, key responsibilities]
- **[AggregateName]**: [Description, key responsibilities]

### Value Objects
- **[ValueObjectName]**: [Description, validation rules]
- **[ValueObjectName]**: [Description, validation rules]

### Domain Events (if event-driven)
- **[EventName]**: [When triggered, what data it carries]
- **[EventName]**: [When triggered, what data it carries]

## Technical Considerations

### Architecture
[Clean Architecture, microservices, event-driven, monolith, etc.]

### Technology Stack
[Use `{technology.*}` from parameters]
- Primary Language: {technology.primaryLanguage}
- Framework: {technology.framework}
- Architecture: {technology.architecture}
- Database: {technology.database}
- Cache: {technology.cache}
- Message Broker: {technology.messageBroker}
- Cloud Provider: {technology.cloudProvider}
- Containerization: {technology.containerization}

### Integrations
- [External system 1: API/SDK, authentication method]
- [External system 2: webhooks, message queue using {technology.messageBroker}]

### Performance Requirements
[Use `{businessMetrics.performanceTarget}` from parameters]
- Latency targets: {businessMetrics.performanceTarget}
- Throughput targets: 1000 req/sec
- Concurrency: 5K concurrent users
- Availability: {businessMetrics.availabilityTarget}

### Security Considerations
[Use `{patterns.errorHandling}` and `{compliance.*}` from parameters]
- Authentication: JWT, OAuth2
- Authorization: RBAC, permissions
- Data encryption: at rest, in transit
- Compliance: {compliance.dataProtection}, {compliance.payment}, {compliance.taxCompliance}
- Retention: {compliance.retention}

## Story Breakdown Preview

**Estimated Stories:** [Number, e.g., 10-12 stories]

**Story Categories (typical patterns):**
1. **Analysis & Design** (S01-S02): Research, domain modeling, architecture decisions
2. **Core Implementation** (S03-S06): Domain layer, infrastructure, application layer, API layer
3. **Feature Stories** (S07-S11): Specific features extending core (varies by epic)
4. **Cross-Cutting Concerns** (S12-S14): Testing, security, production readiness
5. **Gap Analysis** (S15): Legacy migration tasks (if applicable)

**Note:** Detailed stories will be created by `epic-to-stories` prompt.

## Success Criteria

- [ ] [Specific, measurable acceptance criterion 1]
- [ ] [Specific, measurable acceptance criterion 2]
- [ ] [Specific, measurable acceptance criterion 3]
- [ ] All tests passing (unit >80%, integration >85%)
- [ ] Performance benchmarks met
- [ ] Security review completed
- [ ] Production deployment successful

## Risks & Mitigations

### High Risk: [Risk description]
**Mitigation:** [How to address this risk]

### Medium Risk: [Risk description]
**Mitigation:** [How to address this risk]

## Dependencies

### Must Complete First
- [E01: User Authentication] - Need auth tokens for API calls
- [E03: Database Migration] - Need schema for data storage

### Blocks These Epics
- [E08: Advanced Analytics] - Cannot analyze billing data without this epic
- [E10: Mobile App] - Mobile app needs subscription APIs

## Next Steps

1. Run `epic-to-stories` prompt to break down into detailed stories
2. Assign epic owner (engineering lead)
3. Schedule sprint planning for first stories
4. Set up epic tracking board

---

**Epic Created:** [Date] by [GitHub Copilot Agent]
**Last Updated:** [Date]
```

### Step 4: Context Length Management

**If epic README.md approaches 1000 lines during creation:**

1. **Stop writing the epic README**
2. **Create the epic directory immediately:**
   ```bash
   mkdir -p .github/copilot/plan/E05-billing-subscriptions
   ```
3. **Write a shorter epic README (500-700 lines):**
   - Keep: Overview, Business Context, Scope, Success Criteria
   - Reduce: Domain Model (high-level only), Technical Considerations (summary)
   - Remove: Detailed code examples, extensive story preview
4. **Immediately invoke `epic-to-stories` prompt:**
   - This will create detailed stories that capture the full epic scope
   - User command: "Break down E05 into stories" or agent auto-invokes

**Why this matters:**
- Prevents context length overflow (>1000 lines = hard to manage)
- Keeps epic README as executive summary
- Detailed content lives in stories (where it belongs)

### Step 5: Create Epic Directories and Files

**For each new epic:**

```bash
# Create directory (using paths.plan and naming.epicFormat from parameters)
mkdir -p {paths.plan}/{epicPrefix}05-billing-subscriptions

# Create README.md
# (Write content from Step 3 template)

# Commit immediately (using commitConventions.epicFormat from parameters)
git add {paths.plan}/{epicPrefix}05-billing-subscriptions/
git commit -m "{commitConventions.epicFormat}

Business Value: Enable {businessMetrics.currency}5L/month recurring revenue
Scope: Subscription packages, billing cycles, payment gateway integration
Stories: 12-15 estimated (analysis, design, implementation, testing, production)
Dependencies: {epicPrefix}01 (User Management) must complete first"

# Example with parameters substituted:
# git commit -m "feat: E05 Billing & Subscriptions epic created
# Business Value: Enable ₹5L/month recurring revenue
# ..."
```

### Step 6: Idempotency Verification

**Before completing, verify:**

```bash
# Check what was created
ls .github/copilot/plan/

# Expected output for new epics only:
# E20-new-epic-name/ (with README.md inside)
# E30-another-epic/ (with README.md inside)

# Existing epics untouched:
# E01-user-management/ (unchanged)
# E05-billing-subscriptions/ (unchanged)
```

**Idempotency rules:**
- If epic directory exists with README.md -> **DO NOT recreate or modify**
- If epic in analysis but missing from plan/ -> **CREATE**
- If epic marked "Complete" in README.md -> **DO NOT modify**
- If adding new epics, number them E20, E30, E40... (jump by 10)

### Step 7: Quality Checklist

Before marking complete, verify:
- [ ] All new epics from analysis document have directories in `plan/`
- [ ] Each epic has README.md with all required sections
- [ ] Epic numbers jump by 10 (E10, E20, E30) for future interleaving
- [ ] Existing epics were NOT modified
- [ ] Business value quantified (revenue, users, compliance)
- [ ] Dependencies on other epics documented
- [ ] Success criteria are specific and measurable
- [ ] Technology stack matches project standards
- [ ] Each epic committed with detailed commit message

---

## Example Execution

**User:** "Create epics from analysis document"

**Agent Response:**

1. **Load project parameters:**
   ```bash
   cat .github/copilot/prompts/project-parameters.json
   # Extract: paths.analysis = ".github/copilot/analysis/"
   #          paths.plan = ".github/copilot/plan/"
   #          naming.epicPrefix = "E"
   #          naming.numberingGap = 10
   #          project.name = "EduJournal vNext"
   ```

2. **Read analysis document:**
   ```bash
   # Find analysis document using paths.analysis parameter
   ls {paths.analysis}
   # Output: modernization_plan.md, vnext_requirements.md
   # Agent: Read modernization_plan.md
   ```

3. **Extract epics:**
   - Analysis lists: {epicPrefix}10-{epicPrefix}90 (9 epics total, using numberingGap=10)
   - Epic names: User Management, Content Delivery, Institute Management, Student Enrollment, Billing & Subscriptions, Communication, Analytics, Admin Dashboard, Mobile App

4. **Check existing epics:**
   ```bash
   # Use paths.plan parameter
   ls {paths.plan}
   # Output: E10-user-management/, E20-content-delivery/, E30-institute-management/
   # Decision: Create E40-E90 only (E10-E30 already exist)
   ```

4. **Create E04 Institute Management:**
   - Create directory: `.github/copilot/plan/E04-institute-management/`
   - Write README.md (850 lines): Overview, business context (₹2L/month revenue), scope (hierarchy, branding, permissions), domain model (Institute aggregate, Address value object), technical considerations (PostgreSQL, multi-tenancy), 10-12 stories estimated
   - Commit: `feat: E04 Institute Management epic created`

5. **Create E05 Billing & Subscriptions:**
   - Create directory: `.github/copilot/plan/E05-billing-subscriptions/`
   - Write README.md (920 lines): Similar structure to E04
   - Commit: `feat: E05 Billing & Subscriptions epic created`

6. **Continue for E06-E09** (4 more epics)

7. **Final verification:**
   ```bash
   ls .github/copilot/plan/
   # Output: E01, E02, E03, E04, E05, E06, E07, E08, E09 (all present)
   ```

8. **Report to user:**
   ```
   Created 6 new epics (E04-E09):
   - E04: Institute Management (₹2L/month revenue, 10 stories)
   - E05: Billing & Subscriptions (₹5L/month revenue, 12 stories)
   - E06: Communication & Messaging (100K notifications/day, 8 stories)
   - E07: Analytics & Reporting (15 dashboards, 9 stories)
   - E08: Admin Dashboard (React UI, 7 stories)
   - E09: Mobile App (iOS/Android, 12 stories)
   
   Next step: Run `epic-to-stories` prompt for each epic to create detailed stories.
   Example: "Break down E04 into stories"
   ```

---

## Technology-Specific Adaptations

### Backend-Heavy Epics (Microservices, APIs, Databases)
- Emphasize: Domain model, aggregates, events, database schema
- Story preview: Analysis, Design, Domain, Infrastructure, Application, API, Features (3-5), Testing, Security, Production

### Frontend-Heavy Epics (React, Angular, Vue)
- Emphasize: Component hierarchy, state management, API integration, UX/accessibility
- Story preview: Design System, Component Library, Pages/Views (3-5), API Integration, Testing, Performance, Deployment

### Infrastructure Epics (Kubernetes, CI/CD, Monitoring)
- Emphasize: IaC, deployment pipelines, observability, security hardening
- Story preview: Design, IaC Implementation, CI/CD Pipeline, Monitoring, Security, Disaster Recovery

### Data Epics (ETL, Analytics, ML Pipelines)
- Emphasize: Data sources, transformations, storage, query performance
- Story preview: Data Model, Ingestion Pipeline, Transformation Logic, Storage Layer, Query API, Visualization, Performance Tuning

---

## Prompt Chaining

**After this prompt completes, user typically runs:**
- `epic-to-stories` for first epic: "Break down E04 into stories"
- Once stories created, `story-to-tasks` for complex stories: "Break down E04 S15 into tasks"

**Agent can auto-invoke next prompt if:**
- Epic README.md approaching 1000 lines (context overflow risk)
- User says: "Create epics and break down the first one"
- Analysis document specifies: "Create full plan with stories"

---

## Idempotency Examples

**Scenario 1: Re-running on same analysis**
```bash
# First run: Created E01-E09
ls .github/copilot/plan/
# Output: E01, E02, E03, E04, E05, E06, E07, E08, E09

# Second run (same analysis):
# Agent checks existence, finds all 9 epics exist
# Agent: "All epics from analysis already exist. No new epics created."
```

**Scenario 2: Analysis updated with new epic**
```bash
# Analysis now lists E01-E10 (E10: AI Tutoring System added)

# Run prompt again:
# Agent checks: E01-E09 exist, E10 missing
# Agent creates: E10-ai-tutoring/ with README.md
# Agent numbers: E10 (since E09 is the last existing epic, E10 is next in sequence)
```

**Scenario 3: Adding epics months later**
```bash
# Original: E01-E09 exist
# New requirement: Add "Gamification" epic

# Analysis updated: E10 Gamification (using next number in sequence)
# OR if we want flexibility for future: E20 Gamification (jump by 10)

# Run prompt:
# Agent creates E20-gamification/ (leaves room for E10-E19 if needed)
```

**Scenario 4: Epic marked complete**
```bash
# E05-billing-subscriptions/README.md has:
# **Status:** Complete

# Run prompt again (even if analysis changes E05 description):
# Agent: "E05 marked Complete, skipping modification"
# Agent: Does not overwrite completed work
```

---

## Error Handling

**Missing analysis document:**
```
Agent: "Cannot find analysis document. Please provide path."
User: ".github/copilot/analysis/requirements.md"
Agent: [Reads file and proceeds]
```

**Invalid epic structure in analysis:**
```
Agent: "Analysis document does not contain epic breakdown. Expected format:
# E01: Epic Name
Description...

Please structure analysis with epic sections."
```

**Conflicting epic numbers:**
```
Agent: "Analysis lists E05 as 'Billing' but E05-payment-gateway/ already exists.
Options:
1. Skip E05 (assume same epic, different name)
2. Renumber new epic to E20
3. Manual resolution required

Which option? (default: skip)"
```

---

## Output Summary Template

**After completion, agent reports:**

```
✅ Analysis-to-Epics Complete

📊 Epics Created: [Number]
- E04: Institute Management (850 lines, ₹2L/month revenue)
- E05: Billing & Subscriptions (920 lines, ₹5L/month revenue)
- E06: Communication (780 lines, 100K msgs/day)

📊 Epics Skipped: [Number] (already exist)
- E01: User Management ✅
- E02: Content Delivery ✅
- E03: Student Enrollment ✅

📂 Location: .github/copilot/plan/

🔄 Next Step:
Run `epic-to-stories` prompt for each epic to create detailed stories.
Example: "Break down E04 into stories"

⏱️ Estimated Effort: [Total weeks across all epics]
```

---

**This prompt is self-contained, idempotent, and ready for a fresh GitHub Copilot agent with no prior context.**
