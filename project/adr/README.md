# Architecture Decision Records (ADRs)

This directory contains the architecture decision records for the Owlet project.

## ADR Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [ADR-001](001-production-ready-windows-service.md) | Production-Ready Windows Service Architecture | Accepted | 2025-11-01 |
| [ADR-002](002-aspire-orchestration-framework.md) | .NET Aspire for Orchestration and Observability | Accepted | 2025-11-01 |
| [ADR-003](003-functional-programming-patterns.md) | Functional Programming and Modern C# Patterns | Accepted | 2025-11-01 |
| [ADR-004](004-msi-installer-packaging.md) | MSI Installer with Self-Contained Deployment | Accepted | 2025-11-01 |
| [ADR-005](005-phased-implementation-approach.md) | Phased Implementation Strategy | Accepted | 2025-11-01 |
| [ADR-006](006-ci-cd-pipeline-architecture.md) | CI/CD Pipeline and Testing Strategy | Accepted | 2025-11-01 |
| [ADR-007](007-pure-service-installation.md) | Pure Windows Service for End-User Installation | Accepted | 2025-11-01 |

## ADR Template

When creating new ADRs, use the following template:

```markdown
# ADR-XXX: [Title]

## Status
[Proposed | Accepted | Rejected | Deprecated | Superseded by ADR-XXX]

## Date
YYYY-MM-DD

## Context
[What is the issue that we're seeing that is motivating this decision or change?]

## Decision
[What is the change that we're proposing or have agreed to implement?]

## Consequences
[What becomes easier or more difficult to do and any risks introduced by this change?]

## Alternatives Considered
[What other options were considered and why were they rejected?]
```

## Guidelines

- ADRs are numbered sequentially (001, 002, 003, etc.)
- ADRs should be written when the decision is made, not after implementation
- ADRs are immutable once accepted - if you need to change a decision, create a new ADR that supersedes the old one
- Keep ADRs focused on architectural decisions, not implementation details
- Include enough context so that future team members can understand the reasoning