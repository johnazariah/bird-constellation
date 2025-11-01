# ADR-002: .NET Aspire for Orchestration and Observability

## Status
Accepted

## Date
2025-11-01

## Context

During design discussions, we realized that while Owlet needs to be deployable as a Windows Service for production use, it also needs to support:

- Local development with hot reload and debugging capabilities
- Future integration with other Constellation services (Lumen, Cygnet, Eaglet, Raven)
- Built-in observability (logging, metrics, health checks, tracing)
- Service discovery for inter-service communication
- Consistent deployment patterns across development and production environments

The question arose whether .NET Aspire could work with Windows Services, or if we needed to choose between development convenience and production deployment requirements.

Research revealed that Aspire and Windows Services are fully compatible and complementary technologies.

## Decision

We will use .NET Aspire as the orchestration and observability framework for Owlet, integrated with Windows Service deployment:

### Development Experience
- **Owlet.AppHost**: Aspire orchestration project for local development and service composition
- **Aspire Dashboard**: Rich debugging experience with real-time logs, metrics, and service topology
- **Service Discovery**: Automatic configuration of service-to-service communication
- **Hot Reload**: Fast development iteration with Aspire's development patterns

### Production Deployment
- **Owlet.Service**: Pure Windows Service with minimal dependencies for end-user installation
- **Owlet.AppHost**: Aspire orchestration for development and constellation scenarios
- **Dual Architecture**: Same core libraries (Owlet.Api, Owlet.Indexer) hosted differently
- **Minimal Dependencies**: End-user MSI ships without Aspire packages to reduce complexity

### Future Constellation Support
- **Service Registry**: Automatic discovery of other Constellation services
- **Event Bus**: Built-in patterns for inter-service communication
- **Shared Patterns**: Consistent observability and configuration across all Constellation applications

## Consequences

### Positive
- **Development Excellence**: Aspire provides superior development experience with dashboard and service discovery
- **Constellation Ready**: Built-in patterns for future service integration without architectural changes
- **Dual Deployment**: Can ship minimal Windows Service for end users and rich Aspire experience for developers
- **Clean Separation**: Same business logic works in both hosting models without modification
- **Future-Proof**: Easy migration from simple service to full constellation when needed

### Negative
- **Dual Maintenance**: Need to maintain two hosting approaches (though they share core libraries)
- **Complexity**: More complex project structure compared to single hosting model
- **Testing Overhead**: Need to test both hosting scenarios

## Alternatives Considered

### Pure Windows Service without Aspire
- **Pros**: Simpler project structure, fewer dependencies
- **Cons**: Custom observability implementation, manual service discovery, difficult future Constellation integration

### Aspire Only (No Windows Service)
- **Pros**: Excellent development experience, built-in orchestration
- **Cons**: Not suitable for end-user installation, requires technical knowledge to deploy

### Custom Orchestration Framework
- **Pros**: Complete control over orchestration patterns
- **Cons**: Significant development overhead, reinventing solved problems, maintenance burden

## Implementation Architecture

```
Owlet.AppHost (Aspire Orchestration - Development & Constellation)
├── Development: Rich debugging and service coordination
├── Constellation: Multi-service orchestration when ecosystem expands
└── Testing: Integration testing with DistributedApplicationTestingBuilder

Owlet.Service (Pure Windows Service - End User Installation)
├── Minimal dependencies for reliable deployment
├── Direct hosting of Owlet.Api and Owlet.Indexer
├── Built-in configuration and logging without Aspire overhead
└── Professional Windows Service patterns for production use

Shared Libraries (Core Business Logic)
├── Owlet.Api: Web endpoints and UI (shared by both hosts)
├── Owlet.Indexer: File monitoring and processing (shared by both hosts)
├── Owlet.Core: Domain logic and business rules
└── Owlet.Infrastructure: Data access and external concerns
```

## Migration Strategy

1. **Phase 0**: Establish Aspire AppHost and Service Defaults projects
2. **Phase 1**: Build core Owlet.Service with Aspire integration
3. **Phase 2**: Add Windows Service installation and packaging
4. **Phase 3**: Validate production deployment maintains Aspire observability
5. **Future**: Add Constellation services to AppHost for automatic orchestration