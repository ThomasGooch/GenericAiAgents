# ⚙️ GenericAgents Configuration Guide

**Complete guide to configuring GenericAgents for development and production**

---

## 📋 Overview

GenericAgents supports multiple configuration methods:
- **appsettings.json**: Development and structured configuration
- **Environment Variables**: Production and container deployments
- **Secret Management**: Secure handling of sensitive values
- **Code Configuration**: Programmatic setup

---

## 🎯 AI Provider Configuration

### OpenAI Configuration

```json
{
  "AI": {
    "Provider": "OpenAI",
    "ModelId": "gpt-4",
    "ApiKey": "sk-your-openai-api-key",
    "MaxTokens": 2000,
    "Temperature": 0.7,
    "TopP": 1.0,
    "TimeoutSeconds": 30
  }
}
```

**Environment Variables:**
```bash
AI__Provider=OpenAI
AI__ModelId=gpt-4
AI__ApiKey=sk-your-openai-api-key
AI__MaxTokens=2000
AI__Temperature=0.7
AI__TopP=1.0
AI__TimeoutSeconds=30
```

**Code Configuration:**
```csharp
builder.Services.AddAgentServices(config =>
{
    config.Provider = "OpenAI";
    config.ModelId = "gpt-4";
    config.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    config.MaxTokens = 2000;
    config.Temperature = 0.7;
});
```

### Azure OpenAI Configuration

```json
{
  "AI": {
    "Provider": "AzureOpenAI",
    "ModelId": "gpt-4-deployment-name",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-api-key",
    "MaxTokens": 4000,
    "Temperature": 0.8
  }
}
```

**Environment Variables:**
```bash
AI__Provider=AzureOpenAI
AI__ModelId=gpt-4-deployment-name
AI__Endpoint=https://your-resource.openai.azure.com/
AI__ApiKey=your-azure-api-key
```

### Anthropic Claude Configuration

```json
{
  "AI": {
    "Provider": "Anthropic",
    "ModelId": "claude-3-sonnet-20241022",
    "ApiKey": "sk-ant-your-anthropic-key",
    "MaxTokens": 4000,
    "Temperature": 0.7
  }
}
```

### Provider-Specific Settings

```json
{
  "AI": {
    "Provider": "OpenAI",
    "ModelId": "gpt-4",
    "ApiKey": "sk-...",
    "AdditionalSettings": {
      "organization": "org-your-org-id",
      "user": "user-identifier",
      "logit_bias": {
        "50256": -100
      },
      "stop_sequences": ["\n", "Human:", "AI:"]
    }
  }
}
```

---

## 🔒 Security Configuration

### JWT Authentication

#### Local JWT (Development)

```json
{
  "JwtAuthentication": {
    "ProviderType": "Local",
    "Local": {
      "SigningKey": "your-256-bit-signing-key-minimum-32-characters",
      "Issuer": "agent-system",
      "Audience": "agent-system-api",
      "DefaultExpiration": "08:00:00"
    }
  }
}
```

**Code Configuration:**
```csharp
builder.Services.AddLocalJwtAuthentication(
    signingKey: "your-256-bit-signing-key-minimum-32-characters",
    issuer: "my-app",
    audience: "my-app-api",
    defaultExpiration: TimeSpan.FromHours(8)
);
```

#### Okta JWT (Production)

```json
{
  "JwtAuthentication": {
    "ProviderType": "Okta",
    "Okta": {
      "Domain": "https://yourcompany.okta.com",
      "ClientId": "your-okta-client-id",
      "AuthorizationServerId": "default",
      "ValidAudiences": ["api://default", "agent-system-api"],
      "ClientSecret": "your-okta-client-secret"
    }
  }
}
```

**Environment Variables:**
```bash
JWT_AUTH__PROVIDER_TYPE=Okta
JWT_AUTH__OKTA__DOMAIN=https://yourcompany.okta.com
JWT_AUTH__OKTA__CLIENT_ID=your-okta-client-id
JWT_AUTH__OKTA__CLIENT_SECRET=your-okta-client-secret
```

### Secret Management

#### Environment Variables (Development)

```json
{
  "SecretManagement": {
    "Type": "Environment",
    "EnvironmentPrefix": "AGENT_SECRET_",
    "EnableCaching": true,
    "CacheExpirationMinutes": 30
  }
}
```

**Environment Variables:**
```bash
# Secret Management Configuration
SECRET_MGMT__TYPE=Environment
SECRET_MGMT__ENVIRONMENT_PREFIX=AGENT_SECRET_
SECRET_MGMT__ENABLE_CACHING=true

# Actual Secrets
AGENT_SECRET_DATABASE_CONNECTION=Server=localhost;Database=MyApp;...
AGENT_SECRET_EXTERNAL_API_KEY=your-external-service-key
AGENT_SECRET_JWT_SIGNING_KEY=your-jwt-signing-key
```

**Code Configuration:**
```csharp
builder.Services.AddEnvironmentSecretManagement(
    environmentPrefix: "MYAPP_SECRET_",
    enableCaching: true
);
```

#### Azure Key Vault (Production)

```json
{
  "SecretManagement": {
    "Type": "AzureKeyVault",
    "EnableCaching": true,
    "CacheExpirationMinutes": 60,
    "AzureKeyVault": {
      "VaultUrl": "https://your-keyvault.vault.azure.net/",
      "UseManagedIdentity": true,
      "ClientId": "your-managed-identity-client-id"
    }
  }
}
```

**For Service Principal Authentication:**
```json
{
  "SecretManagement": {
    "Type": "AzureKeyVault",
    "AzureKeyVault": {
      "VaultUrl": "https://your-keyvault.vault.azure.net/",
      "UseManagedIdentity": false,
      "ClientId": "your-service-principal-client-id",
      "ClientSecret": "your-service-principal-secret",
      "TenantId": "your-azure-tenant-id"
    }
  }
}
```

**Code Configuration:**
```csharp
builder.Services.AddAzureKeyVaultSecretManagement(
    vaultUrl: "https://your-keyvault.vault.azure.net/",
    useManagedIdentity: true,
    enableCaching: true
);
```

---

## 🔧 Tool Configuration

### Tool Discovery

**Automatic Discovery (Recommended):**
```csharp
// Automatically finds all classes with [Tool] attribute
builder.Services.AddAgentToolDiscovery();
```

**Manual Registration:**
```csharp
// Register specific tools
builder.Services.AddSingleton<FileSystemTool>();
builder.Services.AddSingleton<HttpClientTool>();
builder.Services.AddSingleton<MyCustomTool>();
```

### Sample Tools Configuration

```csharp
using Agent.Tools.Samples;

// Register all sample tools
builder.Services.AddSingleton<FileSystemTool>();
builder.Services.AddSingleton<HttpClientTool>();
builder.Services.AddSingleton<TextManipulationTool>();
```

### Custom Tool Settings

```json
{
  "Tools": {
    "FileSystem": {
      "AllowedPaths": ["/app/data", "/tmp"],
      "MaxFileSize": 10485760,
      "AllowedExtensions": [".txt", ".json", ".csv"]
    },
    "HttpClient": {
      "TimeoutSeconds": 30,
      "MaxContentLength": 5242880,
      "AllowedDomains": ["api.example.com", "service.company.com"]
    },
    "Database": {
      "ConnectionString": "Server=localhost;Database=MyApp;...",
      "QueryTimeout": 30,
      "MaxRows": 1000
    }
  }
}
```

---

## 📊 Observability Configuration

### Health Checks

```json
{
  "HealthChecks": {
    "DatabaseConnection": {
      "Enabled": true,
      "TimeoutSeconds": 5
    },
    "AIService": {
      "Enabled": true,
      "TimeoutSeconds": 10
    },
    "ExternalDependencies": {
      "Enabled": true,
      "Services": [
        {
          "Name": "WeatherAPI",
          "Url": "https://api.weather.com/health",
          "TimeoutSeconds": 5
        }
      ]
    }
  }
}
```

### Metrics and Monitoring

```json
{
  "Observability": {
    "Metrics": {
      "Enabled": true,
      "Endpoint": "/metrics",
      "CollectSystemMetrics": true,
      "CustomMetrics": {
        "AgentExecutionTime": true,
        "ToolUsage": true,
        "AIRequestCount": true,
        "ErrorRates": true
      }
    },
    "Logging": {
      "Level": "Information",
      "IncludeScopes": true,
      "StructuredLogging": true,
      "SensitiveDataLogging": false
    }
  }
}
```

---

## 🌍 Environment-Specific Configuration

### Development Configuration

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Agent": "Trace",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AI": {
    "Provider": "OpenAI",
    "ModelId": "gpt-3.5-turbo",
    "ApiKey": "sk-development-key",
    "Temperature": 0.9,
    "TimeoutSeconds": 60
  },
  "SecretManagement": {
    "Type": "Environment",
    "EnvironmentPrefix": "DEV_AGENT_SECRET_"
  },
  "JwtAuthentication": {
    "ProviderType": "Local",
    "Local": {
      "SigningKey": "development-signing-key-32-chars-min",
      "DefaultExpiration": "24:00:00"
    }
  }
}
```

### Production Configuration

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Agent": "Information",
      "Microsoft": "Warning"
    }
  },
  "AI": {
    "Provider": "AzureOpenAI",
    "ModelId": "gpt-4-production",
    "Endpoint": "https://production.openai.azure.com/",
    "TimeoutSeconds": 30,
    "MaxTokens": 2000
  },
  "SecretManagement": {
    "Type": "AzureKeyVault",
    "EnableCaching": true,
    "CacheExpirationMinutes": 60,
    "AzureKeyVault": {
      "VaultUrl": "https://production-keyvault.vault.azure.net/",
      "UseManagedIdentity": true
    }
  },
  "JwtAuthentication": {
    "ProviderType": "Okta",
    "Okta": {
      "Domain": "https://company.okta.com",
      "AuthorizationServerId": "production"
    }
  }
}
```

### Docker Configuration

**docker-compose.yml:**
```yaml
version: '3.8'
services:
  genericagents-app:
    image: your-app:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - AI__Provider=OpenAI
      - AI__ModelId=gpt-4
      - AI__ApiKey=${OPENAI_API_KEY}
      - SECRET_MGMT__TYPE=Environment
      - SECRET_MGMT__ENVIRONMENT_PREFIX=AGENT_SECRET_
      - AGENT_SECRET_DATABASE_CONNECTION=${DATABASE_CONNECTION}
      - JWT__SIGNING_KEY=${JWT_SIGNING_KEY}
    depends_on:
      - postgres
      - redis
```

**.env file for Docker:**
```bash
# AI Configuration
OPENAI_API_KEY=sk-your-production-key
AI_MODEL_ID=gpt-4
AI_MAX_TOKENS=2000

# Database
DATABASE_CONNECTION=Server=postgres;Database=GenericAgents;User Id=appuser;Password=securepassword;

# JWT
JWT_SIGNING_KEY=your-production-jwt-signing-key-minimum-256-bits

# Secrets
AGENT_SECRET_EXTERNAL_API=your-external-api-key
AGENT_SECRET_STORAGE_KEY=your-storage-account-key
```

---

## 🔧 Advanced Configuration

### Multiple AI Providers

```csharp
// Configure multiple AI services
builder.Services.AddAgentServices(); // Default provider from config

// Add additional AI services
builder.Services.AddKeyedSingleton<IAIService>("openai", (provider, key) => 
    new SemanticKernelAIService(
        provider.GetRequiredService<IOptions<AIConfiguration>>(),
        provider.GetRequiredService<ILogger<SemanticKernelAIService>>()));

builder.Services.AddKeyedSingleton<IAIService>("claude", (provider, key) =>
    new ClaudeAIService(new AIConfiguration
    {
        Provider = "Anthropic",
        ModelId = "claude-3-sonnet-20241022",
        ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    }));
```

### Custom Configuration Validation

```csharp
builder.Services.AddOptions<AIConfiguration>()
    .BindConfiguration("AI")
    .ValidateDataAnnotations()
    .Validate(config => !string.IsNullOrEmpty(config.ApiKey), "AI API Key is required")
    .Validate(config => config.MaxTokens > 0 && config.MaxTokens <= 32000, "MaxTokens must be between 1 and 32000")
    .Validate(config => config.Temperature >= 0.0 && config.Temperature <= 2.0, "Temperature must be between 0.0 and 2.0");
```

### Feature Flags

```json
{
  "FeatureFlags": {
    "EnableAIService": true,
    "EnableToolDiscovery": true,
    "EnableMetrics": true,
    "EnableSecretManagement": true,
    "EnableJwtAuthentication": true,
    "EnableCaching": true,
    "ExperimentalFeatures": {
      "EnableAdvancedWorkflows": false,
      "EnableBatchProcessing": true,
      "EnableStreamingResponses": false
    }
  }
}
```

```csharp
// Use feature flags
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("FeatureFlags"));

// Conditionally add services
var featureFlags = builder.Configuration.GetSection("FeatureFlags").Get<FeatureFlags>();
if (featureFlags?.EnableAIService == true)
{
    builder.Services.AddAgentAI();
}
```

---

## 🚀 Configuration Best Practices

### 1. Security Best Practices

```csharp
// ✅ DO: Use secret management for sensitive data
builder.Services.AddAzureKeyVaultSecretManagement("https://vault.vault.azure.net/");

// ✅ DO: Use environment variables in production
var apiKey = Environment.GetEnvironmentVariable("AI_API_KEY") 
    ?? throw new InvalidOperationException("AI_API_KEY environment variable is required");

// ❌ DON'T: Hardcode secrets in appsettings.json
// "ApiKey": "sk-hardcoded-key-bad-practice"
```

### 2. Environment Separation

```csharp
// Use different configurations per environment
public static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    
    if (builder.Environment.IsDevelopment())
    {
        // Development-specific configuration
        builder.Services.AddEnvironmentSecretManagement();
        builder.Services.AddLocalJwtAuthentication("dev-key");
    }
    else if (builder.Environment.IsProduction())
    {
        // Production-specific configuration
        builder.Services.AddAzureKeyVaultSecretManagement("https://prod-vault.vault.azure.net/");
        builder.Services.AddOktaJwtAuthentication("https://company.okta.com", "prod-client-id");
    }
}
```

### 3. Configuration Validation

```csharp
// Validate configuration at startup
builder.Services.AddOptions<AIConfiguration>()
    .BindConfiguration("AI")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Custom validation
builder.Services.AddSingleton<IValidateOptions<AIConfiguration>, AIConfigurationValidator>();

public class AIConfigurationValidator : IValidateOptions<AIConfiguration>
{
    public ValidateOptionsResult Validate(string name, AIConfiguration options)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(options.ApiKey))
            errors.Add("AI API Key is required");
            
        if (options.MaxTokens < 1 || options.MaxTokens > 32000)
            errors.Add("MaxTokens must be between 1 and 32000");
            
        return errors.Any() 
            ? ValidateOptionsResult.Fail(errors) 
            : ValidateOptionsResult.Success;
    }
}
```

### 4. Configuration Monitoring

```csharp
// Monitor configuration changes
builder.Services.Configure<AIConfiguration>(builder.Configuration.GetSection("AI"));
builder.Services.AddSingleton<IOptionsMonitor<AIConfiguration>>();

// React to configuration changes
public class ConfigurationMonitorService : BackgroundService
{
    private readonly IOptionsMonitor<AIConfiguration> _aiConfigMonitor;
    
    public ConfigurationMonitorService(IOptionsMonitor<AIConfiguration> aiConfigMonitor)
    {
        _aiConfigMonitor = aiConfigMonitor;
        _aiConfigMonitor.OnChange(OnAIConfigurationChanged);
    }
    
    private void OnAIConfigurationChanged(AIConfiguration newConfig, string name)
    {
        // Handle configuration changes
        Logger.LogInformation("AI configuration changed: Model={Model}, MaxTokens={MaxTokens}", 
            newConfig.ModelId, newConfig.MaxTokens);
    }
}
```

This configuration guide provides comprehensive coverage of all GenericAgents configuration options with secure, production-ready examples.