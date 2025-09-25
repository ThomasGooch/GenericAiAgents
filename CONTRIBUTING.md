# 🤝 Contributing to Generic AI Agent System

Thank you for your interest in contributing to the Generic AI Agent System! This document provides guidelines and information for contributors.

## 🌟 Ways to Contribute

### 📝 Documentation & Examples
- **Improve documentation**: Fix typos, clarify explanations, add examples
- **Write tutorials**: Create step-by-step guides for common use cases
- **Integration guides**: Show how to integrate with popular frameworks (ASP.NET, Blazor, etc.)
- **Use case stories**: Share how you're using the system in real projects

### 🐛 Bug Reports & Fixes
- **Report bugs**: Use GitHub Issues with detailed reproduction steps
- **Fix bugs**: Look for issues labeled `bug` or `good first issue`
- **Improve error handling**: Make error messages more helpful
- **Add validation**: Prevent edge cases and improve input validation

### 🚀 Feature Development
- **New components**: Extend the platform with new modules
- **Tool implementations**: Add new tools to the `Agent.Tools.Samples` package
- **Workflow patterns**: Create reusable workflow templates
- **Performance optimizations**: Improve speed and resource usage
- **Security enhancements**: Strengthen authentication, authorization, or data protection

### 🔧 Tool Contributions
We especially welcome new tool implementations! Here's a template:

```csharp
[Tool("your-tool-name")]
[Description("What your tool does")]
public class YourTool : BaseTool
{
    protected override Dictionary<string, object> DefineParameterSchema()
    {
        return new Dictionary<string, object>
        {
            ["input"] = new { type = "string", description = "Input description", required = true },
            ["options"] = new { type = "object", description = "Optional parameters", required = false }
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Your tool implementation
            var input = parameters["input"].ToString();
            var result = await ProcessInput(input, cancellationToken);
            
            return ToolResult.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Tool execution failed: {ex.Message}");
        }
    }
}
```

## 🚀 Getting Started

### Prerequisites
- **.NET 8 SDK**: [Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Git**: For version control
- **Docker** (optional): For running integration tests
- **IDE**: Visual Studio, VS Code, or JetBrains Rider

### Development Setup

1. **Fork the repository**
   ```bash
   # Go to https://github.com/ThomasGooch/GenericAiAgents and click "Fork"
   ```

2. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR_USERNAME/generic-agents.git
   cd generic-agents
   ```

3. **Set up upstream remote**
   ```bash
   git remote add upstream https://github.com/ThomasGooch/GenericAiAgents.git
   ```

4. **Install dependencies**
   ```bash
   dotnet restore
   ```

5. **Build the solution**
   ```bash
   dotnet build
   ```

6. **Run tests**
   ```bash
   dotnet test
   ```

## 📋 Development Workflow

### Making Changes

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/issue-number-description
   ```

2. **Make your changes**
   - Write your code
   - Add/update tests
   - Update documentation if needed

3. **Test your changes**
   ```bash
   # Run all tests
   dotnet test
   
   # Run specific test categories
   dotnet test --filter "Category=Unit"
   dotnet test --filter "Category=Integration"
   
   # Check code coverage
   dotnet test --collect:"XPlat Code Coverage"
   ```

4. **Format your code**
   ```bash
   dotnet format
   ```

5. **Build in release mode**
   ```bash
   dotnet build --configuration Release
   ```

### Commit Guidelines

We use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New features
- `fix:` Bug fixes
- `docs:` Documentation changes
- `test:` Adding or updating tests
- `refactor:` Code refactoring
- `perf:` Performance improvements
- `chore:` Maintenance tasks

Examples:
```bash
git commit -m "feat: add database query tool"
git commit -m "fix: handle null parameters in orchestrator"
git commit -m "docs: add integration example for ASP.NET Core"
```

### Pull Request Process

1. **Push your changes**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request**
   - Go to your fork on GitHub
   - Click "New Pull Request"
   - Fill out the PR template with:
     - Clear description of changes
     - Reference to related issues
     - Screenshots/examples if applicable

3. **PR Requirements**
   - [ ] All tests pass
   - [ ] Code follows style guidelines
   - [ ] Documentation updated (if needed)
   - [ ] Breaking changes documented
   - [ ] PR description is clear and complete

## 🧪 Testing

### Test Categories
- **Unit Tests**: Fast, isolated tests for individual components
- **Integration Tests**: Tests that verify component interactions
- **Security Tests**: Authentication and authorization validation
- **Performance Tests**: Benchmarks and load testing

### Running Tests
```bash
# All tests
dotnet test

# Specific categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Security"
dotnet test --filter "Category=Performance"

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# In Docker (CI environment)
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

### Writing Tests
```csharp
[Fact]
[Trait("Category", "Unit")]
public async Task YourMethod_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var service = new YourService();
    var input = "valid-input";

    // Act
    var result = await service.YourMethod(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("expected-value", result.Value);
}
```

## 📝 Code Standards

### C# Style Guidelines
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful names for variables, methods, and classes
- Write XML documentation for public APIs
- Use `async/await` consistently for asynchronous operations
- Handle exceptions appropriately

### Project Structure
```
src/
├── Agent.Core/           # Core abstractions and interfaces
├── Agent.Tools/          # Tool framework
├── Agent.AI/            # AI service integrations
├── Agent.Security/       # Authentication and authorization
├── Agent.Orchestration/ # Workflow engine
└── ...

tests/
├── Agent.Core.Tests/     # Unit tests
├── Integration/          # Integration tests
├── Performance/          # Performance tests
└── ...
```

### Security Guidelines
- **Never commit secrets**: Use configuration and secret management
- **Validate inputs**: Always validate and sanitize user inputs
- **Use secure defaults**: Default to the most secure configuration
- **Follow OWASP guidelines**: Implement security best practices
- **Write security tests**: Verify authentication and authorization

## 🎯 Issue Labels

We use these labels to organize work:

### Difficulty
- `good first issue`: Great for newcomers
- `help wanted`: Community input welcome
- `advanced`: Requires deep system knowledge

### Type
- `bug`: Something isn't working
- `enhancement`: New feature or improvement
- `documentation`: Improvements or additions to docs
- `question`: Further information is requested
- `security`: Security-related issue

### Priority
- `critical`: Blocks releases or causes data loss
- `high`: Important for next release
- `medium`: Should be addressed soon
- `low`: Nice to have

### Component
- `core`: Agent.Core related
- `tools`: Agent.Tools related
- `ai`: Agent.AI related
- `security`: Agent.Security related
- `orchestration`: Agent.Orchestration related

## 🏆 Recognition

### Contributor Benefits
- Listed in [CONTRIBUTORS.md](./CONTRIBUTORS.md)
- Mentioned in release notes for significant contributions
- Access to contributor-only Discord channels
- Early access to new features and roadmap discussions
- GitHub profile badges for contributions

### Contribution Levels
- **🥉 Contributor**: Made 1-5 meaningful contributions
- **🥈 Regular Contributor**: Made 5-20 contributions over 3+ months
- **🥇 Core Contributor**: Made 20+ contributions, helps with code reviews
- **🏆 Maintainer**: Trusted with repository access and release management

## 💬 Getting Help

### Communication Channels
- **GitHub Issues**: Bug reports, feature requests, and technical questions
- **GitHub Discussions**: General questions, ideas, and community chat
- **Discord** (Coming Soon): Real-time chat with contributors
- **Email**: security@generic-agents.dev for security issues

### Support
If you need help:

1. **Check existing documentation** in the `docs/` folder
2. **Search GitHub Issues** for similar problems
3. **Ask in GitHub Discussions** for general questions
4. **Create a new issue** for bugs or specific problems

### Office Hours
We hold community office hours:
- **When**: First Friday of each month, 2 PM UTC
- **Where**: Discord voice channel
- **What**: Open Q&A, roadmap discussions, contribution guidance

## 📚 Resources

### Documentation
- [Architecture Guide](./docs/architecture.md): System design and components
- [API Reference](./docs/api.md): Complete API documentation
- [Security Guide](./docs/security.md): Security best practices
- [Deployment Guide](./docs/deployment.md): Production deployment strategies

### Learning Resources
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Tutorials](https://docs.microsoft.com/en-us/aspnet/core/)
- [Semantic Kernel Documentation](https://docs.microsoft.com/en-us/semantic-kernel/)
- [Docker Documentation](https://docs.docker.com/)

### Tools
- [Visual Studio](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [JetBrains Rider](https://www.jetbrains.com/rider/) (paid)
- [Git](https://git-scm.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## 🤝 Code of Conduct

### Our Pledge
We are committed to making participation in our project a harassment-free experience for everyone, regardless of age, body size, disability, ethnicity, sex characteristics, gender identity and expression, level of experience, education, socio-economic status, nationality, personal appearance, race, religion, or sexual identity and orientation.

### Our Standards
Examples of behavior that contributes to a positive environment:
- Using welcoming and inclusive language
- Being respectful of differing viewpoints and experiences
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

### Enforcement
Instances of abusive, harassing, or otherwise unacceptable behavior may be reported to the community leaders responsible for enforcement at conduct@generic-agents.dev. All complaints will be reviewed and investigated promptly and fairly.

---

## 🙏 Thank You!

Thank you for contributing to the Generic AI Agent System! Your contributions help make AI agent orchestration accessible to the entire .NET community.

**Happy coding! 🚀**