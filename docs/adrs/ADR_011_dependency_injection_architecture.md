# ADR-011: Dependency Injection Architecture

## Status
**Accepted** - September 23, 2025

## Author
Thomas Gooch

## Context
Our modular architecture requires a way to wire together components across different modules while maintaining loose coupling. We needed to decide between manual dependency management, service locator pattern, or dependency injection container approach.

## Decision
We will use Microsoft's built-in dependency injection container with custom service collection extensions for each module, enabling clean separation and registration of services across our modular architecture.

## Rationale
1. **Native Integration**: Built into .NET, no additional dependencies
2. **Modular Registration**: Each module can define its own service registrations
3. **Loose Coupling**: Components depend on interfaces, not implementations
4. **Testability**: Easy to replace services with mocks for testing
5. **Configuration Driven**: Service registration can be configuration-driven

## Consequences
### Positive
- Clean separation of concerns across modules
- Easy testing with dependency mocking
- Configuration-driven service selection
- Standard .NET pattern, familiar to developers
- Support for different service lifetimes (Singleton, Scoped, Transient)

### Negative
- Dependency injection container learning curve
- Potential circular dependency issues
- Service registration complexity for large systems
- Runtime dependency resolution overhead

## Implementation
- `ServiceCollectionExtensions` in each module for service registration
- Interface-based service registration
- `ToolDiscoveryHostedService` for automated tool registration
- Configuration-driven service selection (e.g., JWT providers)
- Proper service lifetime management