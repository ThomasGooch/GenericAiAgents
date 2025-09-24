# ADR-002: Interface-Driven Design Pattern

## Status
**Accepted** - September 23, 2025

## Author
Thomas Gooch

## Context
To ensure loose coupling, testability, and flexibility in our agent system, we needed to decide on the primary design pattern. We considered abstract classes versus interfaces for defining contracts between components.

## Decision
We will use interface-driven design as the primary pattern throughout the system. All major components will be defined by interfaces first (`IAgent`, `ITool`, `IAIService`, `IWorkflowEngine`, etc.) with concrete implementations provided separately.

## Rationale
1. **Testability**: Interfaces enable easy mocking and unit testing
2. **Flexibility**: Multiple implementations of the same interface can coexist
3. **Dependency Injection**: Interfaces work seamlessly with DI containers
4. **Loose Coupling**: Components depend on abstractions, not implementations
5. **SOLID Principles**: Follows Interface Segregation and Dependency Inversion principles

## Consequences
### Positive
- Highly testable codebase with easy mocking
- Multiple implementation strategies possible (e.g., LocalJwtTokenProvider vs OktaJwtTokenProvider)
- Easy to swap implementations without changing dependent code
- Clear contracts between components
- Supports various architectural patterns (Strategy, Factory, etc.)

### Negative
- More initial setup work compared to concrete classes
- Additional abstraction layer may be overkill for simple components
- Developers need to understand interface contracts

## Implementation
- All core services defined as interfaces in respective namespaces
- Concrete implementations in separate files/folders
- Dependency injection configured to wire interfaces to implementations
- Unit tests mock interfaces rather than concrete classes
- Clear naming convention: `I{ComponentName}` for interfaces