# ADR-004: Attribute-Based Tool Discovery

## Status
**Accepted** - September 23, 2025

## Author
Thomas Gooch

## Context
Our agent system needed a way to automatically discover and register tools without requiring manual configuration. We considered several approaches: explicit registration, configuration-based discovery, reflection-based discovery, and attribute-based metadata.

## Decision
We will use attribute-based tool discovery with the `[Tool]` attribute to mark classes as discoverable tools. Tools implement the `ITool` interface and are automatically discovered through reflection.

## Rationale
1. **Convention over Configuration**: Tools are discoverable by following simple conventions
2. **Zero Configuration**: No need to manually register each tool
3. **Metadata Rich**: Attributes can carry additional metadata (name, description, categories)
4. **Flexible**: Supports both automated discovery and manual registration
5. **Familiar Pattern**: Similar to ASP.NET Core's attribute-based routing

## Consequences
### Positive
- New tools are automatically available once implemented
- Rich metadata can be attached via attributes
- Follows familiar .NET patterns
- Supports tool categorization and filtering
- Easy to extend with additional metadata

### Negative
- Reflection-based discovery has runtime performance cost
- Tools must follow strict conventions
- Debugging can be more difficult with automatic registration
- Assembly scanning required at startup

## Implementation
- `[Tool]` attribute marks classes as discoverable tools
- `ToolDiscoveryHostedService` scans assemblies at startup
- `IToolRegistry` manages discovered tools
- Tools can override metadata through attribute properties
- Caching mechanism to minimize reflection overhead