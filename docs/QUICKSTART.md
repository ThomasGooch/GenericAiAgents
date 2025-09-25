# 🚀 GenericAgents Quick Start Guide

**Get up and running with GenericAgents in 10 minutes**

This guide shows you how to integrate GenericAgents into your existing .NET application with working code examples.

---

## 📋 Prerequisites

- ✅ .NET 8.0 SDK
- ✅ Existing .NET Web API or Web application
- ✅ AI provider API key (OpenAI, Azure OpenAI, or Anthropic)

---

## 🎯 Step 1: Install NuGet Packages

Add the core packages to your existing project:

```bash
# Essential packages for basic AI integration
dotnet add package GenericAgents.Core
dotnet add package GenericAgents.AI
dotnet add package GenericAgents.DI

# Optional: Add tools and security
dotnet add package GenericAgents.Tools
dotnet add package GenericAgents.Tools.Samples
dotnet add package GenericAgents.Security
```

---

## ⚙️ Step 2: Configure Services

### Update your `Program.cs`:

```csharp
using Agent.DI;
using Agent.AI.Models;
using Agent.Security;

var builder = WebApplication.CreateBuilder(args);

// Your existing services (keep these)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add GenericAgents services - CORRECT API
builder.Services.AddAgentServices(builder.Configuration);

// Optional: Enable automatic tool discovery
builder.Services.AddAgentToolDiscovery();

// Optional: Add security (for production)
builder.Services.AddEnvironmentSecretManagement();
builder.Services.AddLocalJwtAuthentication("your-jwt-signing-key-here");

var app = builder.Build();

// Your existing pipeline (keep these)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add health check
app.MapGet("/health", () => Results.Ok(new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    GenericAgents = "Enabled" 
}));

app.Run();
```

### Update your `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AI": {
    "Provider": "OpenAI",
    "ModelId": "gpt-4",
    "ApiKey": "your-openai-api-key-here",
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "TopP": 1.0,
    "TimeoutSeconds": 30
  },
  "SecretManagement": {
    "Type": "Environment",
    "EnvironmentPrefix": "AGENT_SECRET_",
    "EnableCaching": true
  }
}
```

---

## 🎯 Step 3: Create Your First AI-Enhanced Controller

Create a new controller that uses GenericAgents:

```csharp
using Agent.Core;
using Agent.Core.Models;
using Agent.AI;
using Agent.Registry;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SmartAnalyticsController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<SmartAnalyticsController> _logger;

    public SmartAnalyticsController(
        IAIService aiService, 
        IToolRegistry toolRegistry,
        ILogger<SmartAnalyticsController> logger)
    {
        _aiService = aiService;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    [HttpPost("analyze-text")]
    public async Task<IActionResult> AnalyzeText([FromBody] AnalysisRequest request)
    {
        try
        {
            // Use AI service to analyze the text
            var prompt = $"Analyze the following text for sentiment, key topics, and insights: {request.Text}";
            var aiResponse = await _aiService.ProcessRequestAsync(prompt, HttpContext.RequestAborted);

            // Your existing business logic can be combined here
            var analysis = new
            {
                OriginalText = request.Text,
                AIInsights = aiResponse.Content,
                ProcessedAt = DateTime.UtcNow,
                ModelUsed = aiResponse.ModelId,
                TokensUsed = aiResponse.TokensUsed
            };

            _logger.LogInformation("Text analysis completed for {Length} characters", request.Text.Length);

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing text");
            return StatusCode(500, new { Error = "Analysis failed", Details = ex.Message });
        }
    }

    [HttpGet("available-tools")]
    public async Task<IActionResult> GetAvailableTools()
    {
        var tools = await _toolRegistry.GetAllToolsAsync();
        return Ok(tools.Select(t => new { t.Name, t.Description }));
    }

    [HttpGet("health")]
    public IActionResult CheckHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Services = new
            {
                AIService = _aiService != null ? "Available" : "Not Available",
                ToolRegistry = _toolRegistry != null ? "Available" : "Not Available"
            },
            Timestamp = DateTime.UtcNow
        });
    }
}

public class AnalysisRequest
{
    public string Text { get; set; } = string.Empty;
}
```

---

## 🎯 Step 4: Create a Custom Agent (Advanced)

For more complex scenarios, create a custom agent:

```csharp
using Agent.Core;
using Agent.Core.Models;
using Agent.AI;

public class CustomerServiceAgent : BaseAgent
{
    private readonly IAIService _aiService;
    private readonly ILogger<CustomerServiceAgent> _logger;

    public CustomerServiceAgent(
        IAIService aiService, 
        ILogger<CustomerServiceAgent> logger) 
        : base("customer-service-agent", "AI-powered customer service assistant")
    {
        _aiService = aiService;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing customer service request: {Message}", request.Message);

            // Create a customer service prompt
            var prompt = $@"
You are a helpful customer service agent. Please analyze this customer message and provide:
1. Sentiment analysis (positive, negative, neutral)
2. Category (billing, technical, general inquiry)
3. Urgency level (low, medium, high)
4. Suggested response

Customer message: {request.Message}

Please respond in JSON format.
";

            var aiResponse = await _aiService.ProcessRequestAsync(prompt, cancellationToken);

            return AgentResult.CreateSuccess(aiResponse.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer service request");
            return AgentResult.CreateError($"Processing failed: {ex.Message}");
        }
    }

    protected override async Task OnInitializeAsync(
        AgentConfiguration configuration, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing CustomerServiceAgent with configuration: {Name}", configuration.Name);
        await base.OnInitializeAsync(configuration, cancellationToken);
    }
}
```

### Register and use your custom agent:

```csharp
// In Program.cs, add after other services
builder.Services.AddSingleton<CustomerServiceAgent>();

// In a controller
[ApiController]
[Route("api/[controller]")]
public class CustomerServiceController : ControllerBase
{
    private readonly CustomerServiceAgent _agent;

    public CustomerServiceController(CustomerServiceAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("process-inquiry")]
    public async Task<IActionResult> ProcessInquiry([FromBody] CustomerInquiry inquiry)
    {
        // Initialize agent if not already done
        if (!_agent.IsInitialized)
        {
            await _agent.InitializeAsync(new AgentConfiguration
            {
                Name = "customer-service",
                Description = "Handle customer inquiries",
                Timeout = TimeSpan.FromMinutes(2)
            });
        }

        var request = new AgentRequest
        {
            Message = inquiry.Message,
            RequestId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow
        };

        var result = await _agent.ExecuteAsync(request);

        if (result.IsSuccess)
        {
            return Ok(new
            {
                CustomerInquiry = inquiry.Message,
                AgentResponse = result.Data,
                ProcessingTime = result.ProcessingTime,
                RequestId = request.RequestId
            });
        }

        return BadRequest(new
        {
            Error = result.ErrorMessage,
            RequestId = request.RequestId
        });
    }
}

public class CustomerInquiry
{
    public string Message { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}
```

---

## 🎯 Step 5: Test Your Integration

### 1. Run your application:
```bash
dotnet run
```

### 2. Test the health endpoint:
```bash
curl http://localhost:5000/health
```

### 3. Test AI analysis:
```bash
curl -X POST http://localhost:5000/api/SmartAnalytics/analyze-text \
  -H "Content-Type: application/json" \
  -d '{"text": "I love this product! It works perfectly and exceeded my expectations."}'
```

### 4. Check available tools:
```bash
curl http://localhost:5000/api/SmartAnalytics/available-tools
```

---

## 🎯 Step 6: Environment Variables (Production)

For production, use environment variables instead of hardcoding API keys:

### Create `.env` file (local development):
```bash
AI__ApiKey=your-openai-api-key-here
AI__Provider=OpenAI
AI__ModelId=gpt-4
JWT_SIGNING_KEY=your-jwt-signing-key-here
```

### Update `appsettings.json` to remove secrets:
```json
{
  "AI": {
    "Provider": "OpenAI",
    "ModelId": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.7
  }
}
```

### Load environment variables in `Program.cs`:
```csharp
// Add this before builder.Build()
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddEnvironmentVariables();
}
```

---

## 🎯 Step 7: Add Sample Tools (Optional)

To use the pre-built sample tools:

```csharp
// In Program.cs
using Agent.Tools.Samples;

// Add after AddAgentServices
builder.Services.AddSingleton<FileSystemTool>();
builder.Services.AddSingleton<HttpClientTool>();
builder.Services.AddSingleton<TextManipulationTool>();
```

### Use tools in your controller:

```csharp
[HttpPost("file-operation")]
public async Task<IActionResult> FileOperation([FromBody] FileOperationRequest request)
{
    var fileSystemTool = new FileSystemTool();
    
    var parameters = new Dictionary<string, object>
    {
        { "operation", request.Operation },
        { "path", request.Path },
        { "content", request.Content ?? "" }
    };

    var result = await fileSystemTool.ExecuteAsync(parameters, HttpContext.RequestAborted);
    
    if (result.IsSuccess)
        return Ok(result.Data);
    
    return BadRequest(result.ErrorMessage);
}
```

---

## ✅ Troubleshooting

### Common Issues:

1. **"AddGenericAgents() not found"**
   - ✅ **Solution**: Use `AddAgentServices()` instead
   - ✅ **Add**: `using Agent.DI;`

2. **"AI configuration not found"**
   - ✅ **Check**: `appsettings.json` has correct `"AI"` section
   - ✅ **Verify**: API key is set correctly

3. **"Service not registered"**
   - ✅ **Ensure**: `AddAgentServices()` is called before `builder.Build()`
   - ✅ **Check**: All required using statements are present

4. **"Tool discovery not working"**
   - ✅ **Add**: `AddAgentToolDiscovery()` to services
   - ✅ **Verify**: Tools have `[Tool]` attribute

### Debugging Commands:
```bash
# Check if packages are installed
dotnet list package

# Verify configuration
dotnet run --verbose

# Test specific endpoints
curl -v http://localhost:5000/health
```

---

## 🎉 Next Steps

Now that GenericAgents is integrated:

1. **🔍 Explore Tools**: Check out `GenericAgents.Tools.Samples` for more examples
2. **🔒 Add Security**: Implement JWT authentication for production
3. **📊 Monitor Performance**: Use the built-in metrics and observability features
4. **🏗️ Build Workflows**: Create complex multi-step AI workflows
5. **📚 Read Documentation**: Explore advanced features in the `/docs` folder

### Additional Resources:
- **[API Reference](./API_REFERENCE.md)**: Complete API documentation
- **[Configuration Guide](./CONFIGURATION.md)**: Detailed configuration options
- **[Sample Projects](../samples/)**: Complete working examples
- **[Security Guide](./SECURITY.md)**: Production security setup

---

**🎊 Congratulations! You've successfully integrated GenericAgents into your application!**

Your application now has AI capabilities with enterprise-grade infrastructure. Start building intelligent features that delight your users!