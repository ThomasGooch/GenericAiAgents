# 📚 GenericAgents API Reference

**Complete reference for all GenericAgents APIs, interfaces, and extension methods**

---

## 🎯 Core Concepts

### Package Structure
Each GenericAgents package provides specific functionality:

| Package | Namespace | Purpose |
|---------|-----------|---------|
| `GenericAgents.Core` | `Agent.Core` | Base abstractions and models |
| `GenericAgents.AI` | `Agent.AI` | AI service integration |
| `GenericAgents.DI` | `Agent.DI` | Dependency injection extensions |
| `GenericAgents.Tools` | `Agent.Tools` | Tool framework |
| `GenericAgents.Security` | `Agent.Security` | Authentication & authorization |
| `GenericAgents.Registry` | `Agent.Registry` | Tool registry and discovery |

---

## 🔧 Service Registration APIs

### `Agent.DI.ServiceCollectionExtensions`

#### `AddAgentServices()`
Registers core agent services with the DI container.

```csharp
// Basic registration with configuration
public static IServiceCollection AddAgentServices(
    this IServiceCollection services, 
    IConfiguration? configuration = null)

// With explicit AI configuration
public static IServiceCollection AddAgentServices(
    this IServiceCollection services, 
    AIConfiguration aiConfiguration)

// With configuration action
public static IServiceCollection AddAgentServices(
    this IServiceCollection services, 
    Action<AIConfiguration> configureAI)
```

**Examples:**
```csharp
// From appsettings.json
builder.Services.AddAgentServices(builder.Configuration);

// Explicit configuration
builder.Services.AddAgentServices(new AIConfiguration
{
    Provider = "OpenAI",
    ModelId = "gpt-4",
    ApiKey = "sk-...",
    MaxTokens = 2000,
    Temperature = 0.7
});

// Configuration action
builder.Services.AddAgentServices(config =>
{
    config.Provider = "AzureOpenAI";
    config.Endpoint = "https://your-resource.openai.azure.com";
    config.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
});
```

#### `AddAgentCore()`
Registers only core services (no AI).

```csharp
public static IServiceCollection AddAgentCore(this IServiceCollection services)
```

#### `AddAgentAI()`
Registers AI services only.

```csharp
public static IServiceCollection AddAgentAI(this IServiceCollection services)
```

#### `AddAgentToolRegistry()`
Registers tool registry services.

```csharp
public static IServiceCollection AddAgentToolRegistry(this IServiceCollection services)
```

#### `AddAgentToolDiscovery()`
Enables automatic tool discovery via attributes.

```csharp
public static IServiceCollection AddAgentToolDiscovery(this IServiceCollection services)
```

---

## 🤖 Core Agent APIs

### `Agent.Core.IAgent`

Main interface for all agents.

```csharp
public interface IAgent : IAsyncDisposable
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    AgentConfiguration Configuration { get; }
    bool IsInitialized { get; }

    Task InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default);
    Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<AgentResult> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<AgentHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}
```

### `Agent.Core.BaseAgent`

Abstract base class for creating custom agents.

```csharp
public abstract class BaseAgent : IAgent
{
    protected BaseAgent(string name, string description)
    
    // Abstract method you must implement
    protected abstract Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken);
    
    // Optional override points
    protected virtual Task OnInitializeAsync(
        AgentConfiguration configuration, 
        CancellationToken cancellationToken);
    
    protected virtual ValueTask OnDisposeAsync();
}
```

**Example Custom Agent:**
```csharp
public class MyCustomAgent : BaseAgent
{
    private readonly IAIService _aiService;
    
    public MyCustomAgent(IAIService aiService) 
        : base("my-agent", "Custom agent example")
    {
        _aiService = aiService;
    }
    
    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var aiResponse = await _aiService.ProcessRequestAsync(request.Message, cancellationToken);
            return AgentResult.CreateSuccess(aiResponse.Content);
        }
        catch (Exception ex)
        {
            return AgentResult.CreateError($"Processing failed: {ex.Message}");
        }
    }
}
```

### Core Models

#### `AgentConfiguration`
```csharp
public class AgentConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Settings { get; set; } = new();
    public List<string> RequiredTools { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRetries { get; set; } = 3;
}
```

#### `AgentRequest`
```csharp
public class AgentRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

#### `AgentResult`
```csharp
public class AgentResult
{
    public bool IsSuccess { get; set; }
    public string? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Factory methods
    public static AgentResult CreateSuccess(string? data = null);
    public static AgentResult CreateError(string errorMessage);
}
```

---

## 🧠 AI Service APIs

### `Agent.AI.IAIService`

Main interface for AI operations.

```csharp
public interface IAIService
{
    Task<AIResponse> ProcessRequestAsync(string prompt, CancellationToken cancellationToken = default);
    Task<AIResponse> ProcessRequestAsync(string prompt, AIConfiguration? configuration = null, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
```

### AI Configuration

#### `Agent.AI.Models.AIConfiguration`
```csharp
public class AIConfiguration
{
    public string Provider { get; set; } = "AzureOpenAI"; // "OpenAI", "Anthropic", etc.
    public string ModelId { get; set; } = string.Empty;   // "gpt-4", "claude-3-sonnet", etc.
    public string Endpoint { get; set; } = string.Empty;  // API endpoint URL
    public string ApiKey { get; set; } = string.Empty;    // API key
    public int MaxTokens { get; set; } = 2000;            // Max completion tokens
    public double Temperature { get; set; } = 0.7;        // Response randomness (0.0-1.0)
    public double TopP { get; set; } = 1.0;               // Top-p sampling
    public int TimeoutSeconds { get; set; } = 30;         // Request timeout
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();
}
```

**Provider-specific configurations:**

```csharp
// OpenAI
var openAIConfig = new AIConfiguration
{
    Provider = "OpenAI",
    ModelId = "gpt-4",
    ApiKey = "sk-..."
};

// Azure OpenAI
var azureConfig = new AIConfiguration
{
    Provider = "AzureOpenAI",
    ModelId = "gpt-4",  // Deployment name
    Endpoint = "https://your-resource.openai.azure.com/",
    ApiKey = "your-azure-key"
};

// Anthropic Claude
var anthropicConfig = new AIConfiguration
{
    Provider = "Anthropic",
    ModelId = "claude-3-sonnet-20241022",
    ApiKey = "sk-ant-..."
};
```

#### `Agent.AI.Models.AIResponse`
```csharp
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
```

---

## 🔧 Tools APIs

### `Agent.Tools.ITool`

Base interface for all tools.

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }

    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    bool ValidateParameters(Dictionary<string, object> parameters);
    Dictionary<string, Type> GetParameterSchema();
}
```

### `Agent.Tools.BaseTool`

Abstract base class for creating custom tools.

```csharp
public abstract class BaseTool : ITool
{
    public string Name { get; }
    public string Description { get; }

    protected BaseTool() // Uses [Tool] and [Description] attributes

    protected abstract Dictionary<string, Type> DefineParameterSchema();
    protected abstract Task<ToolResult> ExecuteInternalAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken);
}
```

### Tool Attributes

#### `[Tool]` Attribute
```csharp
[Tool("tool-name")]
public class MyTool : BaseTool
{
    // Tool implementation
}
```

#### `[Description]` Attribute
```csharp
[Tool("database-query")]
[Description("Executes safe database queries with parameterization")]
public class DatabaseTool : BaseTool
{
    // Implementation
}
```

### Custom Tool Example

```csharp
[Tool("weather-lookup")]
[Description("Gets current weather information for a specified location")]
public class WeatherTool : BaseTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherTool> _logger;

    public WeatherTool(IHttpClientFactory httpClientFactory, ILogger<WeatherTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "location", typeof(string) },
            { "units", typeof(string) } // optional: "metric", "imperial", "kelvin"
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        try
        {
            var location = parameters["location"].ToString();
            var units = parameters.ContainsKey("units") ? parameters["units"].ToString() : "metric";

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetStringAsync(
                $"https://api.openweathermap.org/data/2.5/weather?q={location}&units={units}&appid=YOUR_API_KEY",
                cancellationToken);

            return ToolResult.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weather lookup failed for location: {Location}", parameters["location"]);
            return ToolResult.CreateError($"Weather lookup failed: {ex.Message}");
        }
    }
}
```

### `Agent.Tools.Models.ToolResult`

```csharp
public class ToolResult
{
    public bool IsSuccess { get; set; }
    public string? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Factory methods
    public static ToolResult CreateSuccess(string? data = null);
    public static ToolResult CreateError(string errorMessage);
}
```

### `Agent.Registry.IToolRegistry`

Service for managing and discovering tools.

```csharp
public interface IToolRegistry
{
    Task RegisterToolAsync<T>() where T : ITool;
    Task RegisterToolAsync(ITool tool);
    Task<ITool?> GetToolAsync(string name);
    Task<IEnumerable<ITool>> GetAllToolsAsync();
    Task<bool> UnregisterToolAsync(string name);
}
```

**Usage:**
```csharp
public class MyController : ControllerBase
{
    private readonly IToolRegistry _toolRegistry;

    public MyController(IToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    [HttpPost("execute-tool")]
    public async Task<IActionResult> ExecuteTool([FromBody] ToolExecutionRequest request)
    {
        var tool = await _toolRegistry.GetToolAsync(request.ToolName);
        if (tool == null)
            return NotFound($"Tool '{request.ToolName}' not found");

        var result = await tool.ExecuteAsync(request.Parameters);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

---

## 🔒 Security APIs

### JWT Authentication

#### `Agent.Security.Authentication.JwtServiceCollectionExtensions`

```csharp
// Local JWT (development)
public static IServiceCollection AddLocalJwtAuthentication(
    this IServiceCollection services,
    string signingKey,
    string issuer = "agent-system",
    string audience = "agent-system-api",
    TimeSpan? defaultExpiration = null)

// Okta JWT (production)
public static IServiceCollection AddOktaJwtAuthentication(
    this IServiceCollection services,
    string domain,
    string clientId,
    string authorizationServerId = "default",
    params string[] validAudiences)

// Configuration-based JWT
public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<JwtAuthenticationOptions>? configureOptions = null)
```

### Authorization Attributes

```csharp
[RequireAdmin] // Only admin users
public class AdminController : ControllerBase { }

[RequirePermission("metrics:view")] // Specific permission
public IActionResult GetMetrics() { }

[RequireWorkflowManager] // Admin or workflow:manage permission
public IActionResult CreateWorkflow() { }
```

### Secret Management

#### `Agent.Security.ServiceCollectionExtensions`

```csharp
// Environment variables (development)
public static IServiceCollection AddEnvironmentSecretManagement(
    this IServiceCollection services,
    string environmentPrefix = "AGENT_SECRET_",
    bool enableCaching = true)

// Azure Key Vault (production)
public static IServiceCollection AddAzureKeyVaultSecretManagement(
    this IServiceCollection services,
    string vaultUrl,
    bool useManagedIdentity = true,
    bool enableCaching = true)

// Configuration-based
public static IServiceCollection AddSecretManagement(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<SecretManagerOptions>? configureOptions = null)
```

#### `Agent.Security.ISecretManager`

```csharp
public interface ISecretManager
{
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);
    Task SetSecretAsync(string key, string value, CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> SecretExistsAsync(string key, CancellationToken cancellationToken = default);
}
```

---

## 📊 Configuration Reference

### appsettings.json Complete Example

```json
{
  "AI": {
    "Provider": "OpenAI",
    "ModelId": "gpt-4",
    "Endpoint": "https://api.openai.com/v1",
    "ApiKey": "sk-your-api-key-here",
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "TopP": 1.0,
    "TimeoutSeconds": 30,
    "AdditionalSettings": {
      "custom-setting": "value"
    }
  },
  "SecretManagement": {
    "Type": "Environment", // "AzureKeyVault", "AwsSecretsManager", "HashiCorpVault"
    "EnvironmentPrefix": "AGENT_SECRET_",
    "EnableCaching": true,
    "CacheExpirationMinutes": 30,
    "AzureKeyVault": {
      "VaultUrl": "https://your-vault.vault.azure.net/",
      "UseManagedIdentity": true,
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  },
  "JwtAuthentication": {
    "ProviderType": "Local", // "Okta", "AzureAd", "Custom"
    "Local": {
      "SigningKey": "your-256-bit-signing-key-here",
      "Issuer": "agent-system",
      "Audience": "agent-system-api",
      "DefaultExpiration": "08:00:00"
    },
    "Okta": {
      "Domain": "https://yourcompany.okta.com",
      "ClientId": "your-okta-client-id",
      "AuthorizationServerId": "default",
      "ValidAudiences": ["api://default", "agent-system-api"]
    }
  }
}
```

### Environment Variables

For production, use environment variables instead of appsettings.json:

```bash
# AI Configuration
AI__Provider=OpenAI
AI__ModelId=gpt-4
AI__ApiKey=sk-your-api-key
AI__MaxTokens=2000
AI__Temperature=0.7

# JWT Authentication
JWT__SigningKey=your-signing-key
JWT__Issuer=agent-system
JWT__Audience=agent-system-api

# Secret Management
AGENT_SECRET_DATABASE_CONNECTION=Server=...
AGENT_SECRET_EXTERNAL_API_KEY=your-external-api-key
```

---

## 🚀 Complete Integration Example

### Putting It All Together

```csharp
// Program.cs
using Agent.DI;
using Agent.AI.Models;
using Agent.Security;
using Agent.Security.Authentication;
using Agent.Tools.Samples;

var builder = WebApplication.CreateBuilder(args);

// Standard ASP.NET Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// GenericAgents Core Services
builder.Services.AddAgentServices(builder.Configuration);
builder.Services.AddAgentToolDiscovery();

// Security Services
builder.Services.AddEnvironmentSecretManagement();
builder.Services.AddLocalJwtAuthentication(
    builder.Configuration["JWT:SigningKey"] ?? "default-dev-key"
);

// Custom Services
builder.Services.AddSingleton<MyCustomAgent>();
builder.Services.AddSingleton<WeatherTool>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // JWT auth
app.UseAuthorization();  // RBAC
app.MapControllers();

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));
app.MapGet("/health/ai", async (IAIService aiService) => 
    await aiService.IsHealthyAsync() ? Results.Ok("AI service healthy") : Results.Problem("AI service unhealthy"));

app.Run();
```

This API reference provides complete coverage of all GenericAgents APIs with working examples. Use this as your comprehensive guide for integrating and building with the GenericAgents platform.