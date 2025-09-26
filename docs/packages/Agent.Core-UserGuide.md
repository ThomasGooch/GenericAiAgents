# 🧱 Agent.Core: Your Foundation for AI Agents

## What is Agent.Core?

**Simple explanation:** Agent.Core provides the essential building blocks for creating AI agents in .NET applications.

**When to use it:** Every GenericAgents implementation needs this package - it's the foundation that makes everything else work.

**Key concepts in plain English:**
- **Agents** are AI-powered workers that can process requests and return results
- **Lifecycle management** handles initialization, execution, and cleanup automatically
- **Error handling** ensures your agents fail gracefully and provide useful feedback
- **Health monitoring** lets you check if your agents are working properly

## The Big Picture

```
┌─────────────────────────────────────────────────────────┐
│                    Agent.Core Architecture              │
├─────────────────────────────────────────────────────────┤
│  Your Custom Agent (inherits from BaseAgent)           │
│  ┌─────────────────────────────────────────────────┐    │
│  │  ExecuteInternalAsync()                         │    │
│  │  - Your business logic goes here               │    │
│  │  - Process requests                            │    │
│  │  - Return results                              │    │
│  └─────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────┤
│  BaseAgent (handles heavy lifting)                     │
│  ┌─────────────────────────────────────────────────┐    │
│  │  ✓ Initialization & Configuration              │    │
│  │  ✓ Timeout Management                          │    │
│  │  ✓ Error Handling                              │    │
│  │  ✓ Health Checks                               │    │
│  │  ✓ Lifecycle Management                        │    │
│  └─────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────┤
│  Core Models                                            │
│  │  AgentRequest → [Your Agent] → AgentResult          │
│  │     Input + Context         Output + Metadata      │
└─────────────────────────────────────────────────────────┘
```

## Essential Components You'll Use

### BaseAgent Class

**What it does:** Handles the heavy lifting of agent lifecycle management, error handling, and common operations.

**When to inherit:** When you want to create custom AI behavior - this is your starting point for any agent.

**Simple example:**
```csharp
using Agent.Core;
using Agent.Core.Models;

public class MyFirstAgent : BaseAgent
{
    private readonly ILogger<MyFirstAgent> _logger;

    public MyFirstAgent(ILogger<MyFirstAgent> logger) 
        : base("my-first-agent", "A simple agent that processes text")
    {
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing request: {Input}", request.Input);

            // Your business logic here
            var processedText = request.Input.ToUpperCase();
            
            // Return success result
            return AgentResult.CreateSuccess($"Processed: {processedText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process request");
            return AgentResult.CreateError($"Processing failed: {ex.Message}");
        }
    }
}
```

### AgentRequest & AgentResult

**What they are:** The standard input and output format for every agent operation.

**Why they matter:** Consistent data flow across your entire system - every agent speaks the same language.

**Common patterns:**

#### Creating Requests
```csharp
// Simple request
var request = new AgentRequest 
{ 
    Input = "Hello, world!" 
};

// Request with context
var request = new AgentRequest 
{ 
    Input = "Analyze this customer feedback",
    Context = new Dictionary<string, object>
    {
        ["customerId"] = "12345",
        ["priority"] = "high"
    },
    Metadata = new Dictionary<string, object>
    {
        ["source"] = "customer-portal",
        ["timestamp"] = DateTime.UtcNow
    }
};
```

#### Handling Results
```csharp
var result = await agent.ExecuteAsync(request);

if (result.Success)
{
    // Success! Use the output
    var output = result.Output;
    var metadata = result.Metadata;
    
    Console.WriteLine($"Agent succeeded: {output}");
}
else
{
    // Handle errors gracefully
    Console.WriteLine($"Agent failed: {result.Error}");
    
    // Log for debugging
    _logger.LogError("Agent execution failed: {Error}", result.Error);
}
```

#### Error Scenarios
```csharp
// Always check for success before using output
if (!result.Success)
{
    // Common error scenarios:
    // - Invalid input
    // - Timeout exceeded  
    // - External service unavailable
    // - Agent not initialized
    
    switch (result.Error)
    {
        case var error when error.Contains("timeout"):
            // Handle timeout specifically
            await NotifyTimeout();
            break;
        case var error when error.Contains("not initialized"):
            // Reinitialize agent
            await agent.InitializeAsync(config);
            break;
        default:
            // Generic error handling
            _logger.LogError("Unexpected error: {Error}", result.Error);
            break;
    }
}
```

### Agent Lifecycle Management

**Initialization Pattern:**
```csharp
// Create agent
var agent = new MyFirstAgent(logger);

// Configure it
var config = new AgentConfiguration 
{ 
    Name = "Production Text Processor",
    Timeout = TimeSpan.FromSeconds(30) 
};

// Initialize (required before use)
await agent.InitializeAsync(config);

// Now ready to process requests
var result = await agent.ExecuteAsync(request);
```

**Proper Disposal:**
```csharp
// Agents implement IAsyncDisposable
await using var agent = new MyFirstAgent(logger);
await agent.InitializeAsync(config);

// Use agent...
var result = await agent.ExecuteAsync(request);

// Disposal happens automatically
// Or call explicitly: await agent.DisposeAsync();
```

## Common Use Cases

### 1. Creating a Simple AI Assistant

```csharp
public class SimpleAssistant : BaseAgent
{
    private readonly IAIService _aiService;

    public SimpleAssistant(IAIService aiService) 
        : base("simple-assistant", "AI assistant for general questions")
    {
        _aiService = aiService;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        var prompt = $"Please help with this question: {request.Input}";
        var aiResponse = await _aiService.ProcessRequestAsync(prompt, cancellationToken);
        
        return AgentResult.CreateSuccess(aiResponse.Content);
    }
}
```

### 2. Building a Data Processing Agent

```csharp
public class DataProcessor : BaseAgent
{
    private readonly IDataService _dataService;

    public DataProcessor(IDataService dataService) 
        : base("data-processor", "Processes and validates data records")
    {
        _dataService = dataService;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<DataRecord>(request.Input);
        
        // Validate
        if (!IsValidRecord(data))
        {
            return AgentResult.CreateError("Invalid data format");
        }
        
        // Process
        var processed = await _dataService.ProcessAsync(data, cancellationToken);
        
        return AgentResult.CreateSuccess(processed, new Dictionary<string, object>
        {
            ["recordsProcessed"] = 1,
            ["processingTime"] = DateTime.UtcNow
        });
    }
    
    private bool IsValidRecord(DataRecord record) => 
        record != null && !string.IsNullOrEmpty(record.Id);
}
```

### 3. Handling User Interactions

```csharp
public class UserInteractionAgent : BaseAgent
{
    private readonly IUserService _userService;

    public UserInteractionAgent(IUserService userService) 
        : base("user-interaction", "Handles user requests and responses")
    {
        _userService = userService;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = request.Context.GetValueOrDefault("userId")?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return AgentResult.CreateError("User ID required in context");
        }

        var user = await _userService.GetUserAsync(userId, cancellationToken);
        var personalizedResponse = $"Hello {user.Name}, {request.Input}";
        
        return AgentResult.CreateSuccess(personalizedResponse, new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["userName"] = user.Name,
            ["timestamp"] = DateTime.UtcNow
        });
    }
}
```

## Troubleshooting

### Common Errors and Solutions

#### 1. "Agent has not been initialized"
**Problem:** Trying to execute an agent before calling `InitializeAsync`.
**Solution:**
```csharp
// ❌ Wrong - will throw exception
var result = await agent.ExecuteAsync(request);

// ✅ Correct - initialize first
await agent.InitializeAsync(config);
var result = await agent.ExecuteAsync(request);
```

#### 2. "Operation timed out after X seconds"
**Problem:** Agent execution is taking longer than configured timeout.
**Solution:**
```csharp
// Increase timeout for long-running operations
var config = new AgentConfiguration 
{ 
    Timeout = TimeSpan.FromMinutes(5) // Instead of default 30 seconds
};
```

#### 3. "Request cannot be null"
**Problem:** Passing null request to agent.
**Solution:**
```csharp
// ❌ Wrong
var result = await agent.ExecuteAsync(null);

// ✅ Correct - always provide a request
var request = new AgentRequest { Input = "test" };
var result = await agent.ExecuteAsync(request);
```

### Debugging Techniques

#### 1. Enable Detailed Logging
```csharp
// In your agent constructor or initialization
_logger.LogDebug("Agent {AgentName} initialized with config {Config}", 
    Name, JsonSerializer.Serialize(Configuration));

// In ExecuteInternalAsync
_logger.LogInformation("Processing request {RequestId}: {Input}", 
    request.Id, request.Input);
```

#### 2. Use Health Checks
```csharp
// Check if agent is healthy before processing
var health = await agent.CheckHealthAsync();
if (!health.IsHealthy)
{
    _logger.LogWarning("Agent unhealthy: {Message}", health.Message);
    // Handle accordingly
}
```

#### 3. Inspect Metadata
```csharp
var result = await agent.ExecuteAsync(request);
if (result.Success)
{
    // Check processing metadata
    foreach (var item in result.Metadata)
    {
        _logger.LogDebug("Metadata {Key}: {Value}", item.Key, item.Value);
    }
}
```

### Performance Considerations

#### 1. Agent Reuse
```csharp
// ✅ Good - reuse agent instances
private readonly MyAgent _agent;

public async Task ProcessMany(List<AgentRequest> requests)
{
    foreach (var request in requests)
    {
        var result = await _agent.ExecuteAsync(request);
        // Process result
    }
}

// ❌ Avoid - creating new agents each time is expensive
public async Task ProcessOne(AgentRequest request)
{
    var agent = new MyAgent(); // Expensive!
    await agent.InitializeAsync(config);
    var result = await agent.ExecuteAsync(request);
}
```

#### 2. Concurrent Processing
```csharp
// Process multiple requests concurrently (if agent is thread-safe)
var tasks = requests.Select(request => agent.ExecuteAsync(request));
var results = await Task.WhenAll(tasks);
```

#### 3. Resource Management
```csharp
// Always dispose of agents properly
await using var agent = new MyAgent(dependencies);
await agent.InitializeAsync(config);

// Agent will be properly disposed when leaving scope
```

## Next Steps

### Phase 2 Capabilities
Once you're comfortable with Agent.Core, explore these advanced packages:

- **Agent.Security** - Add authentication and authorization to your agents
- **Agent.Orchestration** - Coordinate multiple agents in complex workflows  
- **Agent.AI** - Integrate with AI services like OpenAI and Azure OpenAI
- **Agent.Tools** - Give your agents real-world capabilities

### Advanced Patterns
- **Agent composition** - Combine multiple specialized agents
- **Error recovery** - Implement retry logic and fallback strategies
- **Performance monitoring** - Track agent performance and health
- **Configuration management** - Dynamic agent configuration updates

### Best Practices
1. **Single Responsibility** - Each agent should have one clear purpose
2. **Error Handling** - Always handle exceptions and return meaningful errors
3. **Logging** - Log important operations for debugging and monitoring
4. **Testing** - Write unit tests for your agent logic
5. **Documentation** - Document your agent's purpose and usage

---

**🎯 You're now ready to build your first AI agent with GenericAgents!**

Start with a simple agent, test it thoroughly, then gradually add more complexity as you become comfortable with the patterns and concepts.