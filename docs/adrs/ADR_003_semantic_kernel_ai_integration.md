# ADR-003: Semantic Kernel for AI Integration

## Status
**Accepted** - September 23, 2025

## Author
Thomas Gooch

## Context
We needed to integrate AI capabilities into our agent system. Several options were considered: direct OpenAI API integration, LangChain, Microsoft Semantic Kernel, or building a custom AI abstraction layer.

## Decision
We will use Microsoft Semantic Kernel as our primary AI integration framework, wrapped behind our own `IAIService` abstraction.

## Rationale
1. **Microsoft Ecosystem**: Aligns with our .NET 8 technology stack
2. **Multi-Model Support**: Supports OpenAI, Azure OpenAI, and other providers
3. **Plugin Architecture**: Matches well with our tool-based agent design
4. **Enterprise Ready**: Designed for production scenarios with proper error handling
5. **Active Development**: Strong Microsoft backing and community support
6. **Abstraction Layer**: Our `IAIService` allows switching AI frameworks if needed

## Consequences
### Positive
- Rapid AI integration without reinventing the wheel
- Support for multiple AI models and providers
- Consistent with Microsoft ecosystem
- Built-in support for function calling (matches our tool system)
- Good documentation and community support

### Negative
- Dependency on Microsoft's roadmap and decisions
- Learning curve for Semantic Kernel concepts
- Additional abstraction layer adds complexity
- Potential vendor lock-in (mitigated by our abstraction)

## Implementation
- `SemanticKernelAIService` implements `IAIService` interface
- Configuration through `AIConfiguration` models
- Support for multiple AI providers through SK's built-in abstractions
- Integration with our tool system via SK's function calling features