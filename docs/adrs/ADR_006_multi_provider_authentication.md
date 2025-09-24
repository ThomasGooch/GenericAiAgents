# ADR-006: Multi-Provider Authentication Strategy

## Status
**Accepted** - September 23, 2025

## Author
Thomas Gooch

## Context
Our agent system needed to support authentication in multiple environments: local development (simple JWT), testing (mock authentication), and production (enterprise identity providers like Okta, Azure AD). We needed a flexible approach that could adapt to different deployment scenarios.

## Decision
We will implement a multi-provider authentication strategy using the `IJwtTokenProvider` abstraction with multiple concrete implementations: `LocalJwtTokenProvider` for development and `OktaJwtTokenProvider` for production scenarios.

## Rationale
1. **Environment Flexibility**: Different authentication needs for dev, test, and production
2. **Enterprise Ready**: Support for enterprise identity providers
3. **Development Friendly**: Simple local authentication for development
4. **Security**: Proper JWT validation and claims handling
5. **Extensibility**: Easy to add new identity providers

## Consequences
### Positive
- Supports multiple deployment scenarios
- Easy development and testing setup
- Enterprise-grade security for production
- Consistent authentication interface across providers
- Can switch providers without changing application code

### Negative
- Multiple authentication implementations to maintain
- Configuration complexity for different providers
- Need to understand each provider's specific requirements
- Testing complexity across multiple providers

## Implementation
- `IJwtTokenProvider` defines authentication contract
- `LocalJwtTokenProvider` for development with configurable signing keys
- `OktaJwtTokenProvider` for Okta integration
- `JwtAuthenticationMiddleware` handles token validation
- Configuration-driven provider selection