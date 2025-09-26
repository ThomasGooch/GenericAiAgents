# 🔌 Agent.DI: Dependency Injection Integration Masterclass

## What is Agent.DI?

**Simple explanation:** Agent.DI makes it effortless to integrate GenericAgents with .NET dependency injection containers.

**When to use it:** Every .NET application using GenericAgents - whether it's ASP.NET Core, Worker Services, or Console Apps.

**Key concepts in plain English:**
- **Service registration** automatically sets up all the services your agents need
- **Tool discovery** finds and registers your custom tools without manual configuration
- **Integration patterns** work seamlessly with existing .NET applications
- **Lifecycle management** handles the creation and disposal of agent services

## Dependency Injection Demystified

### What is Dependency Injection?
Think of dependency injection as a "waiter service" for your code:

```
🏪 Without DI - You fetch everything yourself:
┌─────────────────────────────────────────────────┐
│  Your Agent                                     │
│  ├─ Creates its own logger                     │
│  ├─ Creates its own AI service                 │  
│  ├─ Creates its own database connection        │
│  └─ Creates its own HTTP client                │
└─────────────────────────────────────────────────┘
Problems: Hard to test, tightly coupled, duplicated setup

🎯 With DI - Services are delivered to you:
┌─────────────────────────────────────────────────┐
│  DI Container (the "waiter")                    │
│  ├─ Registers ILogger → ConsoleLogger           │
│  ├─ Registers IAIService → OpenAIService        │  
│  ├─ Registers IDatabase → SqlDatabase           │
│  └─ Registers HttpClient → Configured client    │
└─────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────┐
│  Your Agent (receives what it needs)           │
│  ├─ ILogger logger (injected)                  │
│  ├─ IAIService aiService (injected)            │
│  └─ HttpClient httpClient (injected)           │
└─────────────────────────────────────────────────┘
Benefits: Easy to test, loosely coupled, centralized configuration
```

## Service Registration Patterns

### Pattern 1: The Quick Start
For getting up and running fast:

```csharp
// Program.cs - ASP.NET Core
using Agent.DI;

var builder = WebApplication.CreateBuilder(args);

// ✨ One line to add all GenericAgents services
builder.Services.AddAgentServices(builder.Configuration);

// Optional: Enable automatic tool discovery
builder.Services.AddAgentToolDiscovery();

var app = builder.Build();
app.Run();
```

### Pattern 2: The Configured Setup  
For production applications that need specific AI settings:

```csharp
using Agent.DI;
using Agent.AI.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure AI settings explicitly
builder.Services.AddAgentServices(aiConfig =>
{
    aiConfig.Provider = "OpenAI";
    aiConfig.ModelId = "gpt-4";
    aiConfig.ApiKey = builder.Configuration["OpenAI:ApiKey"];
    aiConfig.MaxTokens = 4000;
    aiConfig.Temperature = 0.7f;
    aiConfig.TimeoutSeconds = 30;
});

// Add automatic tool discovery
builder.Services.AddAgentToolDiscovery();

var app = builder.Build();
```

### Pattern 3: The Modular Approach
For applications that need fine-grained control:

```csharp
using Agent.DI;

var builder = WebApplication.CreateBuilder(args);

// Add services piece by piece
builder.Services.AddAgentCore();           // Basic agent infrastructure
builder.Services.AddAgentAI();             // AI service integration
builder.Services.AddAgentToolRegistry();   // Tool management

// Configure AI service specifically
builder.Services.Configure<AIConfiguration>(builder.Configuration.GetSection("AI"));

// Add tool discovery as a hosted service
builder.Services.AddAgentToolDiscovery();

var app = builder.Build();
```

### Pattern 4: The Multi-Environment Setup
For applications that need different configurations per environment:

```csharp
using Agent.DI;
using Agent.AI.Models;
using Agent.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Environment-specific AI configuration
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAgentServices(aiConfig =>
    {
        aiConfig.Provider = "OpenAI";
        aiConfig.ModelId = "gpt-3.5-turbo";  // Cheaper for dev
        aiConfig.ApiKey = "dev-api-key";
        aiConfig.TimeoutSeconds = 15;
    });
}
else if (builder.Environment.IsProduction())
{
    builder.Services.AddAgentServices(aiConfig =>
    {
        aiConfig.Provider = "AzureOpenAI";
        aiConfig.ModelId = "gpt-4";
        aiConfig.Endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
        aiConfig.ApiKey = builder.Configuration["AzureOpenAI:ApiKey"];
        aiConfig.TimeoutSeconds = 60;
    });
}

// Tool discovery in all environments
builder.Services.AddAgentToolDiscovery();

var app = builder.Build();
```

## Tool Discovery Explanation

### What is Tool Discovery?
Tool discovery automatically finds and registers all your custom tools so agents can use them:

```csharp
// Without tool discovery - manual registration (tedious!)
builder.Services.AddSingleton<CalculatorTool>();
builder.Services.AddSingleton<FileProcessorTool>();
builder.Services.AddSingleton<EmailSenderTool>();
builder.Services.AddSingleton<DatabaseTool>();
// ... register 20+ more tools manually

// With tool discovery - automatic (awesome!)
builder.Services.AddAgentToolDiscovery();
// Finds all tools with [Tool] attribute automatically!
```

### How Tool Discovery Works

```
Application Startup
│
▼
ToolDiscoveryHostedService starts
│
▼ 
Scans all loaded assemblies
│
▼
Finds classes with [Tool("name")] attribute
│
▼
Registers each tool in the DI container
│
▼
Agents can now use all discovered tools
```

### Creating Discoverable Tools

```csharp
using Agent.Tools;
using Agent.Tools.Attributes;

// ✅ This tool will be automatically discovered
[Tool("file-processor")]
public class FileProcessorTool : BaseTool
{
    private readonly ILogger<FileProcessorTool> _logger;
    
    // Constructor injection works automatically
    public FileProcessorTool(ILogger<FileProcessorTool> logger)
    {
        _logger = logger;
    }
    
    public override async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken = default)
    {
        var filePath = parameters["filePath"]?.ToString();
        if (string.IsNullOrEmpty(filePath))
        {
            return ToolResult.CreateError("filePath parameter required");
        }
        
        try 
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            _logger.LogInformation("Processed file: {FilePath}", filePath);
            return ToolResult.CreateSuccess($"File processed: {content.Length} characters");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {FilePath}", filePath);
            return ToolResult.CreateError($"File processing failed: {ex.Message}");
        }
    }
}

// ❌ This tool will NOT be discovered (missing [Tool] attribute)
public class UnregisteredTool : BaseTool
{
    // This won't be found automatically
}
```

## Integration with Existing DI Containers

### ASP.NET Core Integration
```csharp
// Startup.cs or Program.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Your existing services
        services.AddControllers();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IUserService, UserService>();
        
        // Add GenericAgents services
        services.AddAgentServices(Configuration);
        services.AddAgentToolDiscovery();
        
        // Register your custom agents
        services.AddScoped<CustomerServiceAgent>();
        services.AddScoped<DataAnalysisAgent>();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Your existing middleware
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        
        // GenericAgents works with your existing pipeline
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

### Worker Service Integration
```csharp
// Program.cs for Worker Service
using Agent.DI;

var builder = Host.CreateApplicationBuilder(args);

// Add GenericAgents to worker service
builder.Services.AddAgentServices(builder.Configuration);
builder.Services.AddAgentToolDiscovery();

// Add your worker service
builder.Services.AddHostedService<AgentWorkerService>();

// Register your agents
builder.Services.AddSingleton<BackgroundProcessingAgent>();

var host = builder.Build();
host.Run();

// Your worker service
public class AgentWorkerService : BackgroundService
{
    private readonly BackgroundProcessingAgent _agent;
    private readonly ILogger<AgentWorkerService> _logger;
    
    public AgentWorkerService(
        BackgroundProcessingAgent agent, 
        ILogger<AgentWorkerService> logger)
    {
        _agent = agent;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize agent
        await _agent.InitializeAsync(new AgentConfiguration { Name = "Background Processor" });
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Process work items
            var request = new AgentRequest { Input = "Process pending items" };
            var result = await _agent.ExecuteAsync(request, stoppingToken);
            
            _logger.LogInformation("Processing result: {Success}", result.Success);
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### Console Application Integration
```csharp
using Agent.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Create a host builder for console app
var builder = Host.CreateApplicationBuilder(args);

// Configure logging for console
builder.Logging.AddConsole();

// Add GenericAgents services
builder.Services.AddAgentServices(aiConfig =>
{
    aiConfig.Provider = "OpenAI";
    aiConfig.ApiKey = args.Length > 0 ? args[0] : throw new ArgumentException("API key required");
});

builder.Services.AddAgentToolDiscovery();

// Register your agent
builder.Services.AddSingleton<ConsoleAgent>();

var host = builder.Build();

// Get the agent from DI
var agent = host.Services.GetRequiredService<ConsoleAgent>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Initialize and use
await agent.InitializeAsync(new AgentConfiguration { Name = "Console Agent" });

logger.LogInformation("Console agent initialized");

// Interactive loop
while (true)
{
    Console.Write("Enter your question (or 'quit' to exit): ");
    var input = Console.ReadLine();
    
    if (input?.ToLower() == "quit") break;
    
    var request = new AgentRequest { Input = input ?? "" };
    var result = await agent.ExecuteAsync(request);
    
    if (result.Success)
    {
        Console.WriteLine($"Agent: {result.Output}");
    }
    else 
    {
        Console.WriteLine($"Error: {result.Error}");
    }
}

await host.StopAsync();
```

## Real-World Agent Registration Examples

### Example 1: Customer Service System
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Infrastructure
    services.AddDbContext<CustomerDbContext>(options => 
        options.UseSqlServer(connectionString));
    services.AddHttpClient<CrmApiClient>();
    
    // GenericAgents setup
    services.AddAgentServices(Configuration);
    services.AddAgentToolDiscovery();
    
    // Business services
    services.AddScoped<ICustomerService, CustomerService>();
    services.AddScoped<ITicketService, TicketService>();
    services.AddScoped<ICrmService, CrmService>();
    
    // Agents with dependency injection
    services.AddScoped<CustomerInquiryAgent>();
    services.AddScoped<TechnicalSupportAgent>();
    services.AddScoped<EscalationAgent>();
    
    // Custom tools
    services.AddScoped<CrmLookupTool>();
    services.AddScoped<TicketCreationTool>();
    services.AddScoped<KnowledgeBaseTool>();
}
```

### Example 2: Data Processing Pipeline
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // External services
    services.AddHttpClient<WeatherApiClient>();
    services.AddSingleton<IMessageBus, ServiceBusMessageBus>();
    
    // GenericAgents
    services.AddAgentServices(aiConfig =>
    {
        aiConfig.Provider = "AzureOpenAI";
        aiConfig.Endpoint = Configuration["AzureOpenAI:Endpoint"];
        aiConfig.ApiKey = Configuration["AzureOpenAI:ApiKey"];
        aiConfig.ModelId = "gpt-4";
    });
    
    services.AddAgentToolDiscovery();
    
    // Pipeline agents
    services.AddSingleton<DataIngestionAgent>();
    services.AddSingleton<DataValidationAgent>();
    services.AddSingleton<DataTransformationAgent>();
    services.AddSingleton<DataOutputAgent>();
    
    // Pipeline orchestrator
    services.AddSingleton<DataPipelineOrchestrator>();
}

public class DataPipelineOrchestrator
{
    private readonly DataIngestionAgent _ingestionAgent;
    private readonly DataValidationAgent _validationAgent; 
    private readonly DataTransformationAgent _transformationAgent;
    private readonly DataOutputAgent _outputAgent;
    
    public DataPipelineOrchestrator(
        DataIngestionAgent ingestionAgent,
        DataValidationAgent validationAgent,
        DataTransformationAgent transformationAgent,
        DataOutputAgent outputAgent)
    {
        _ingestionAgent = ingestionAgent;
        _validationAgent = validationAgent;
        _transformationAgent = transformationAgent;
        _outputAgent = outputAgent;
    }
    
    public async Task ProcessDataAsync(string inputSource)
    {
        // Step 1: Ingest
        var ingestionResult = await _ingestionAgent.ExecuteAsync(
            new AgentRequest { Input = inputSource });
            
        if (!ingestionResult.Success) return;
        
        // Step 2: Validate
        var validationResult = await _validationAgent.ExecuteAsync(
            new AgentRequest 
            { 
                Input = ingestionResult.Output?.ToString() ?? "",
                Context = { ["source"] = inputSource }
            });
            
        if (!validationResult.Success) return;
        
        // Step 3: Transform
        var transformationResult = await _transformationAgent.ExecuteAsync(
            new AgentRequest { Input = validationResult.Output?.ToString() ?? "" });
            
        if (!transformationResult.Success) return;
        
        // Step 4: Output
        await _outputAgent.ExecuteAsync(
            new AgentRequest { Input = transformationResult.Output?.ToString() ?? "" });
    }
}
```

## Advanced DI Patterns

### Pattern 1: Agent Factory
When you need to create agents dynamically:

```csharp
public interface IAgentFactory
{
    T CreateAgent<T>() where T : class, IAgent;
    IAgent CreateAgent(string agentType);
}

public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public AgentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public T CreateAgent<T>() where T : class, IAgent
    {
        return _serviceProvider.GetRequiredService<T>();
    }
    
    public IAgent CreateAgent(string agentType)
    {
        return agentType.ToLower() switch
        {
            "customer-service" => _serviceProvider.GetRequiredService<CustomerServiceAgent>(),
            "technical-support" => _serviceProvider.GetRequiredService<TechnicalSupportAgent>(),
            "data-analysis" => _serviceProvider.GetRequiredService<DataAnalysisAgent>(),
            _ => throw new ArgumentException($"Unknown agent type: {agentType}")
        };
    }
}

// Registration
services.AddSingleton<IAgentFactory, AgentFactory>();

// Usage
public class AgentController : ControllerBase
{
    private readonly IAgentFactory _agentFactory;
    
    public AgentController(IAgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }
    
    [HttpPost("process/{agentType}")]
    public async Task<IActionResult> ProcessRequest(string agentType, [FromBody] AgentRequest request)
    {
        var agent = _agentFactory.CreateAgent(agentType);
        var result = await agent.ExecuteAsync(request);
        
        return result.Success ? Ok(result) : BadRequest(result.Error);
    }
}
```

### Pattern 2: Scoped Agent Context
For multi-tenant applications:

```csharp
public class TenantAgentContext
{
    public string TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> TenantSettings { get; set; } = new();
}

public class TenantAwareAgent : BaseAgent
{
    private readonly TenantAgentContext _tenantContext;
    private readonly IAIService _aiService;
    
    public TenantAwareAgent(TenantAgentContext tenantContext, IAIService aiService)
        : base("tenant-aware-agent", "Handles tenant-specific requests")
    {
        _tenantContext = tenantContext;
        _aiService = aiService;
    }
    
    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        // Use tenant-specific settings
        var tenantSpecificPrompt = $"[Tenant: {_tenantContext.TenantId}] {request.Input}";
        
        // Process with tenant context
        var result = await _aiService.ProcessRequestAsync(tenantSpecificPrompt, cancellationToken);
        
        return AgentResult.CreateSuccess(result.Content, new Dictionary<string, object>
        {
            ["tenantId"] = _tenantContext.TenantId,
            ["processingTime"] = result.ProcessingTime.TotalMilliseconds
        });
    }
}

// Registration with scoped context
services.AddScoped<TenantAgentContext>();
services.AddScoped<TenantAwareAgent>();

// Middleware to set tenant context
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, TenantAgentContext tenantContext)
    {
        // Extract tenant ID from header, JWT, or route
        var tenantId = context.Request.Headers["X-Tenant-ID"].FirstOrDefault() ?? "default";
        tenantContext.TenantId = tenantId;
        
        // Load tenant-specific settings
        tenantContext.TenantSettings = LoadTenantSettings(tenantId);
        
        await _next(context);
    }
    
    private Dictionary<string, object> LoadTenantSettings(string tenantId)
    {
        // Load from database, configuration, etc.
        return new Dictionary<string, object>
        {
            ["maxTokens"] = tenantId == "premium" ? 8000 : 4000,
            ["model"] = tenantId == "premium" ? "gpt-4" : "gpt-3.5-turbo"
        };
    }
}
```

### Pattern 3: Agent Health Monitoring
Integrated health checks for your agents:

```csharp
public class AgentHealthCheckService : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    
    public AgentHealthCheckService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var isHealthy = true;
        var errors = new List<string>();
        
        // Check core agent services
        try
        {
            var aiService = _serviceProvider.GetRequiredService<IAIService>();
            var aiHealthy = await aiService.IsHealthyAsync(cancellationToken);
            healthData["aiService"] = aiHealthy;
            if (!aiHealthy)
            {
                isHealthy = false;
                errors.Add("AI Service is not healthy");
            }
        }
        catch (Exception ex)
        {
            isHealthy = false;
            errors.Add($"AI Service check failed: {ex.Message}");
        }
        
        // Check tool registry
        try
        {
            var toolRegistry = _serviceProvider.GetRequiredService<IToolRegistry>();
            var tools = await toolRegistry.GetAllToolsAsync(cancellationToken);
            var toolCount = tools.Count();
            healthData["registeredTools"] = toolCount;
            
            if (toolCount == 0)
            {
                errors.Add("No tools registered");
            }
        }
        catch (Exception ex)
        {
            isHealthy = false;
            errors.Add($"Tool registry check failed: {ex.Message}");
        }
        
        return isHealthy 
            ? HealthCheckResult.Healthy("All agent services are healthy", healthData)
            : HealthCheckResult.Unhealthy($"Agent health issues: {string.Join(", ", errors)}", data: healthData);
    }
}

// Registration
services.AddHealthChecks()
    .AddCheck<AgentHealthCheckService>("agents");
    
// Usage in ASP.NET Core
app.MapHealthChecks("/health/agents");
```

## Troubleshooting DI Issues

### Common Problems and Solutions

#### 1. "Unable to resolve service"
```csharp
// Problem: Trying to inject a service that wasn't registered
public class MyAgent : BaseAgent 
{
    public MyAgent(IUnregisteredService service) // ❌ This will fail
    {
        // Constructor injection fails
    }
}

// Solution: Register the service
services.AddScoped<IUnregisteredService, UnregisteredService>(); // ✅
```

#### 2. "Circular dependency detected"
```csharp
// Problem: Services depend on each other
public class AgentA : BaseAgent
{
    public AgentA(AgentB agentB) { } // Depends on B
}

public class AgentB : BaseAgent  
{
    public AgentB(AgentA agentA) { } // Depends on A - circular!
}

// Solution: Use a factory or mediator pattern
public interface IAgentMediator 
{
    Task<AgentResult> ProcessAsync(string agentType, AgentRequest request);
}

public class AgentA : BaseAgent
{
    private readonly IAgentMediator _mediator; // ✅ No direct dependency
    
    public AgentA(IAgentMediator mediator) 
    {
        _mediator = mediator;
    }
    
    protected override async Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        // Can call other agents through mediator
        var result = await _mediator.ProcessAsync("agent-b", request);
        return result;
    }
}
```

#### 3. "Tool discovery not finding my tools"
```csharp
// Problem: Tool not being discovered
public class MyTool : BaseTool // ❌ Missing [Tool] attribute
{
    // Won't be found by tool discovery
}

// Solution: Add the Tool attribute
[Tool("my-tool")] // ✅ Will be discovered
public class MyTool : BaseTool
{
    // Now discoverable
}

// Or register manually if attribute isn't suitable
services.AddSingleton<MyTool>(); // Manual registration
```

#### 4. "Service lifetime issues"
```csharp
// Problem: Mismatched service lifetimes
services.AddSingleton<SingletonAgent>(); // Lives forever
services.AddScoped<ScopedService>();     // Lives per request

public class SingletonAgent : BaseAgent
{
    // ❌ Injecting shorter-lived service into longer-lived service
    public SingletonAgent(ScopedService scopedService) 
    {
        // This can cause issues!
    }
}

// Solution: Use appropriate lifetimes or factory pattern
services.AddScoped<SingletonAgent>(); // ✅ Match the lifetime

// Or use a factory for access to scoped services
public class SingletonAgent : BaseAgent
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public SingletonAgent(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory; // ✅ Factory is singleton-safe
    }
    
    protected override async Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var scopedService = scope.ServiceProvider.GetRequiredService<ScopedService>();
        
        // Use scoped service safely
        return AgentResult.CreateSuccess("processed");
    }
}
```

## Performance Optimization Tips

### 1. Choose Appropriate Service Lifetimes
```csharp
// Heavy services - use Singleton
services.AddSingleton<IToolRegistry, ToolRegistry>(); // Expensive to create
services.AddSingleton<IAIService, SemanticKernelAIService>(); // Maintains connections

// Stateful services - use Scoped
services.AddScoped<IUserContext, UserContext>(); // Per-request state
services.AddScoped<ITransactionService, TransactionService>(); // Database transactions

// Lightweight services - use Transient
services.AddTransient<IEmailService, EmailService>(); // Stateless operations
services.AddTransient<IValidationService, ValidationService>(); // Quick operations
```

### 2. Lazy Initialization for Heavy Dependencies
```csharp
public class OptimizedAgent : BaseAgent
{
    private readonly Lazy<IExpensiveService> _expensiveService;
    
    public OptimizedAgent(Lazy<IExpensiveService> expensiveService)
        : base("optimized-agent", "Uses lazy loading")
    {
        _expensiveService = expensiveService; // Not created until used
    }
    
    protected override async Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        // Only create expensive service if needed
        if (request.Input.Contains("complex"))
        {
            var service = _expensiveService.Value; // Created here
            var result = await service.ProcessComplexRequest(request.Input);
            return AgentResult.CreateSuccess(result);
        }
        
        return AgentResult.CreateSuccess("Simple response");
    }
}

// Registration with lazy wrapper
services.AddScoped<IExpensiveService, ExpensiveService>();
services.AddScoped<Lazy<IExpensiveService>>();
```

### 3. Tool Discovery Optimization
```csharp
// For better startup performance, register tools manually in production
if (builder.Environment.IsDevelopment())
{
    // Use discovery in development for convenience
    builder.Services.AddAgentToolDiscovery();
}
else 
{
    // Manual registration in production for better performance
    builder.Services.AddSingleton<FileSystemTool>();
    builder.Services.AddSingleton<HttpClientTool>();
    builder.Services.AddSingleton<DatabaseTool>();
    // etc.
}
```

## Next Steps

### Integration with Other Packages
- **Agent.Security** - Add authentication and authorization to your DI setup
- **Agent.Orchestration** - Register workflow engines and orchestrators
- **Agent.Observability** - Add monitoring and metrics collection services

### Advanced Patterns to Explore
- **Agent Decorators** - Add cross-cutting concerns like caching, retry logic
- **Agent Pipelines** - Chain multiple agents together with DI coordination
- **Configuration-driven Agent Selection** - Choose agents based on configuration
- **Multi-tenant Agent Isolation** - Separate agent instances per tenant

---

**🎯 You now have mastery over dependency injection with GenericAgents!**

Your agents are now properly integrated with .NET's DI container, making them testable, maintainable, and production-ready. The automatic tool discovery saves you configuration time, and the flexible registration patterns let you adapt to any application architecture.