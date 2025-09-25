# 🚀 GenericAgents Basic Integration Sample

**A complete working example of integrating GenericAgents into an ASP.NET Core Web API**

This sample demonstrates the correct usage patterns, API integration, and best practices for using GenericAgents in your applications.

---

## 🎯 What This Sample Demonstrates

### ✅ **Core Integration**
- ✅ Correct service registration using `AddAgentServices()`
- ✅ Proper namespace imports (`using Agent.DI;`)
- ✅ AI service configuration and usage
- ✅ Tool registry integration
- ✅ Security features (JWT, secret management)

### ✅ **Advanced Features**
- ✅ Custom agent implementation (`CustomerServiceAgent`)
- ✅ Multiple controller endpoints with different use cases
- ✅ Error handling and logging
- ✅ Health checks and monitoring
- ✅ Batch processing capabilities
- ✅ Tool execution framework

### ✅ **Production Patterns**
- ✅ Configuration management (appsettings.json + environment variables)
- ✅ Proper exception handling
- ✅ Structured logging
- ✅ API documentation with Swagger
- ✅ Security best practices

---

## 🏃‍♂️ Quick Start

### 1. **Prerequisites**
- .NET 8.0 SDK
- OpenAI API key (or other AI provider)

### 2. **Clone and Setup**
```bash
# Navigate to the sample
cd samples/BasicIntegration

# Restore packages
dotnet restore

# Update your API key in appsettings.json
# Replace "your-openai-api-key-here" with your actual API key
```

### 3. **Configure API Key**

**Option A: Update appsettings.json**
```json
{
  "AI": {
    "ApiKey": "sk-your-actual-openai-api-key"
  }
}
```

**Option B: Use Environment Variable (Recommended)**
```bash
export AI__ApiKey="sk-your-actual-openai-api-key"
# On Windows: set AI__ApiKey=sk-your-actual-openai-api-key
```

### 4. **Run the Application**
```bash
dotnet run
```

### 5. **Test the Integration**
Open your browser to:
- **Swagger UI**: https://localhost:5001/swagger
- **Health Check**: https://localhost:5001/health
- **AI Health Check**: https://localhost:5001/health/ai

---

## 🧪 Testing the API

### **Text Analysis Example**
```bash
curl -X POST "https://localhost:5001/api/SmartAnalytics/analyze-text" \
     -H "Content-Type: application/json" \
     -d '{"text": "I love this product! It works perfectly and exceeded my expectations."}'
```

### **Content Generation Example**
```bash
curl -X POST "https://localhost:5001/api/SmartAnalytics/generate-content" \
     -H "Content-Type: application/json" \
     -d '{
       "prompt": "Create a blog post about AI in customer service",
       "contentType": "blog-post",
       "targetAudience": "business owners",
       "tone": "professional",
       "maxLength": 500
     }'
```

### **Customer Service Agent Example**
```bash
curl -X POST "https://localhost:5001/api/CustomerService/process-inquiry" \
     -H "Content-Type: application/json" \
     -d '{
       "message": "I am having trouble with my recent order. It was supposed to arrive yesterday but I havent received it yet.",
       "customerEmail": "customer@example.com",
       "inquiryType": "order_issue",
       "priority": "high"
     }'
```

### **Tool Execution Example**
```bash
# List available tools
curl "https://localhost:5001/api/SmartAnalytics/available-tools"

# Execute file system tool
curl -X POST "https://localhost:5001/api/SmartAnalytics/execute-tool" \
     -H "Content-Type: application/json" \
     -d '{
       "toolName": "file-system",
       "parameters": {
         "operation": "list",
         "path": "."
       }
     }'
```

### **Health Check Examples**
```bash
# General health
curl "https://localhost:5001/health"

# AI service health
curl "https://localhost:5001/health/ai"

# Customer service agent health
curl "https://localhost:5001/api/CustomerService/agent-health"

# Analytics service health
curl "https://localhost:5001/api/SmartAnalytics/health"
```

---

## 📁 Project Structure

```
BasicIntegration/
├── Controllers/
│   ├── SmartAnalyticsController.cs    # AI-powered analytics endpoints
│   └── CustomerServiceController.cs   # Custom agent integration
├── CustomerServiceAgent.cs            # Custom agent implementation
├── Program.cs                          # Application setup and configuration
├── appsettings.json                   # Configuration (replace API key)
├── appsettings.Development.json       # Development overrides
├── BasicIntegration.csproj           # Project dependencies
└── README.md                         # This file
```

---

## 🔧 Key Implementation Details

### **Service Registration (Program.cs)**
```csharp
// CORRECT API - this is what actually works
builder.Services.AddAgentServices(builder.Configuration);
builder.Services.AddAgentToolDiscovery();
builder.Services.AddEnvironmentSecretManagement();
builder.Services.AddLocalJwtAuthentication("signing-key");
```

### **Using AI Service**
```csharp
public class SmartAnalyticsController : ControllerBase
{
    private readonly IAIService _aiService; // Correct interface
    
    public SmartAnalyticsController(IAIService aiService)
    {
        _aiService = aiService;
    }
    
    [HttpPost("analyze-text")]
    public async Task<IActionResult> AnalyzeText([FromBody] AnalysisRequest request)
    {
        var aiResponse = await _aiService.ProcessRequestAsync(prompt, HttpContext.RequestAborted);
        return Ok(aiResponse.Content);
    }
}
```

### **Custom Agent Implementation**
```csharp
public class CustomerServiceAgent : BaseAgent
{
    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        // Your custom logic here
        var aiResponse = await _aiService.ProcessRequestAsync(prompt, cancellationToken);
        return AgentResult.CreateSuccess(aiResponse.Content);
    }
}
```

### **Tool Usage**
```csharp
[HttpPost("execute-tool")]
public async Task<IActionResult> ExecuteTool([FromBody] ToolExecutionRequest request)
{
    var tool = await _toolRegistry.GetToolAsync(request.ToolName);
    var result = await tool.ExecuteAsync(request.Parameters);
    return result.IsSuccess ? Ok(result) : BadRequest(result);
}
```

---

## 🔒 Security Configuration

### **Environment Variables for Production**
```bash
# AI Configuration
AI__ApiKey=sk-your-production-key
AI__Provider=OpenAI
AI__ModelId=gpt-4

# JWT Security
JWT__SigningKey=your-production-signing-key-minimum-256-bits

# Secret Management
AGENT_SECRET_DATABASE_CONNECTION=your-database-connection
AGENT_SECRET_EXTERNAL_API=your-external-api-key
```

### **Azure Key Vault (Production)**
```csharp
// In production, use Azure Key Vault
builder.Services.AddAzureKeyVaultSecretManagement(
    "https://your-keyvault.vault.azure.net/",
    useManagedIdentity: true
);
```

---

## 📊 Expected Responses

### **Text Analysis Response**
```json
{
  "originalText": "I love this product!...",
  "aiAnalysis": "{\"sentiment\":\"positive\",\"confidence\":0.95,...}",
  "processingInfo": {
    "modelUsed": "gpt-4",
    "tokensUsed": 150,
    "processingTime": "00:00:01.2500000",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "requestId": "guid-here"
}
```

### **Customer Service Response**
```json
{
  "requestId": "guid-here",
  "customerInquiry": "I am having trouble with my recent order...",
  "customerEmail": "customer@example.com",
  "agentResponse": "{\"sentiment\":\"negative\",\"category\":\"order_issue\",\"urgency_level\":\"high\",...}",
  "status": "success",
  "processingTime": "00:00:02.1000000",
  "timestamp": "2024-01-15T10:30:00Z",
  "agentInfo": {
    "agentId": "customer-service-agent",
    "agentName": "customer-service-agent",
    "agentDescription": "AI-powered customer service assistant..."
  }
}
```

---

## ❗ Common Issues and Solutions

### **Issue: "AddGenericAgents() not found"**
**❌ Wrong:**
```csharp
builder.Services.AddGenericAgents(); // This doesn't exist!
```
**✅ Correct:**
```csharp
using Agent.DI; // Add this namespace
builder.Services.AddAgentServices(builder.Configuration);
```

### **Issue: "AI service not responding"**
1. Check your API key in `appsettings.json`
2. Verify the AI provider is set correctly (`OpenAI`, `AzureOpenAI`, etc.)
3. Test the health endpoint: `GET /health/ai`

### **Issue: "Tools not discovered"**
```csharp
// Make sure you call this:
builder.Services.AddAgentToolDiscovery();
```

### **Issue: "Authentication errors"**
- Ensure JWT signing key is at least 32 characters
- Verify the key matches between configuration and usage

---

## 🎯 Next Steps

1. **📖 Study the Code**: Examine how the controllers, agent, and configuration work
2. **🔧 Customize**: Modify the `CustomerServiceAgent` for your use case  
3. **🛠️ Add Tools**: Create custom tools by inheriting from `BaseTool`
4. **🔒 Secure**: Implement proper authentication for production
5. **📊 Monitor**: Add observability and metrics collection
6. **🚀 Deploy**: Use the production configuration patterns for deployment

---

## 📚 Additional Resources

- **[API Reference](../../docs/API_REFERENCE.md)**: Complete API documentation
- **[Configuration Guide](../../docs/CONFIGURATION.md)**: Detailed configuration options
- **[Quick Start Guide](../../docs/QUICKSTART.md)**: Step-by-step integration guide
- **[Main README](../../README.md)**: Full project documentation

---

**🎉 This sample shows the CORRECT way to integrate GenericAgents!**

Use this as your reference for proper API usage, configuration patterns, and implementation best practices.