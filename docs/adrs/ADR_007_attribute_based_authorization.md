# ADR-007: Attribute-Based Authorization System

## Status
**Accepted** - September 23, 2025

## Author
Thomas Gooch

## Context
We needed fine-grained authorization control for our agent system operations. We considered role-based access control (RBAC), attribute-based access control (ABAC), and simple permission-based systems. The system needed to support both coarse-grained (Admin/User) and fine-grained permissions.

## Decision
We will implement an attribute-based authorization system using custom attributes (`[RequireAdmin]`, `[RequirePermission]`) that integrate with ASP.NET Core's authorization framework while supporting custom permission logic.

## Rationale
1. **Declarative Security**: Authorization requirements are clearly visible in code
2. **Fine-Grained Control**: Support for specific permissions like "workflow:manage"
3. **Role Support**: Built-in support for Admin and User roles
4. **Extensible**: Easy to add new authorization requirements
5. **Integration**: Works seamlessly with ASP.NET Core authorization

## Consequences
### Positive
- Clear, declarative authorization requirements
- Support for both roles and permissions
- Easy to audit security requirements
- Consistent with ASP.NET Core patterns
- Extensible for custom authorization logic

### Negative
- Multiple authorization concepts to understand (roles vs permissions)
- Attribute proliferation for complex scenarios  
- Need to maintain permission definitions
- Testing complexity for authorization scenarios

## Implementation
- `RequirePermissionAttribute` for fine-grained permissions
- `RequireAdminAttribute` for administrative operations
- `PermissionAuthorizationHandler` processes permission requirements
- `ResourceAuthorizationHandler` for resource-based authorization
- Integration with JWT claims for user permissions