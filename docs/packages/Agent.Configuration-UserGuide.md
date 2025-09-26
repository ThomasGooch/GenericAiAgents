# ⚙️ Agent.Configuration: Never Break in Production

## What is Agent.Configuration?

**Simple explanation:** Agent.Configuration provides bulletproof configuration management for your AI agent systems.

**When to use it:** Every production agent system needs robust configuration - this package ensures your settings are valid, secure, and environment-appropriate.

**Key concepts in plain English:**
- **Configuration validation** ensures your settings are correct before your system starts
- **Environment-specific settings** let you use different configurations for dev, staging, and production
- **Safe defaults** prevent common configuration mistakes that break systems
- **Hot reloading** allows configuration updates without system restarts

## Why Configuration Matters

### Real Story: What Happens When Configuration Fails

```
🔥 Production Incident Timeline:
09:00 - Deploy agent system to production
09:05 - All agents timeout after 30 seconds (dev setting)
09:06 - Customer service agents fail, tickets pile up
09:15 - Emergency rollback, 10 minutes of downtime
```

### The Safety Net: How GenericAgents Protects You

```csharp
// ❌ Without validation - silent failure waiting to happen
var config = LoadConfig("appsettings.json");
var agents = new AgentSystem(config); // What could go wrong?

// ✅ With GenericAgents - catches problems early
var configProvider = new AgentConfigurationProvider();
var config = await configProvider.LoadConfigurationAsync("appsettings.json");
// Validation happens automatically - fails fast if something's wrong
```

## Configuration Patterns That Work

### Pattern 1: The Safe Default

This pattern ensures your system works out of the box with sensible defaults:

```csharp
// 1. Start with environment-appropriate defaults
var configProvider = new AgentConfigurationProvider();
var config = configProvider.GetEnvironmentConfiguration("production");

// 2. Override with your specific settings
var customConfig = await configProvider.LoadConfigurationAsync("appsettings.json");
var finalConfig = configProvider.MergeConfigurations(config, customConfig);

// 3. Validate before using
var validation = await configProvider.ValidateConfigurationAsync(finalConfig);
if (!validation.IsValid)
{
    throw new InvalidOperationException($"Configuration invalid: {string.Join(", ", validation.Errors)}");
}

// 4. Now it's safe to use
var agentSystem = new AgentSystem(finalConfig);
```

### Pattern 2: Environment-Aware Configuration

Different environments need different settings - this pattern handles it automatically:

```csharp
public class ConfigurationService
{
    private readonly IAgentConfigurationProvider _configProvider;
    private readonly string _environment;

    public ConfigurationService(IAgentConfigurationProvider configProvider, string environment)
    {
        _configProvider = configProvider;
        _environment = environment;
    }

    public async Task<AgentSystemConfiguration> GetConfigAsync()
    {
        // Get base configuration for environment
        var baseConfig = _configProvider.GetEnvironmentConfiguration(_environment);
        
        // Load environment-specific overrides
        var configFile = $"appsettings.{_environment}.json";
        if (File.Exists(configFile))
        {
            var overrides = await _configProvider.LoadConfigurationAsync(configFile);
            baseConfig = _configProvider.MergeConfigurations(baseConfig, overrides);
        }

        // Environment-specific validation
        var validation = await _configProvider.ValidateEnvironmentSpecificAsync(baseConfig, _environment);
        if (!validation.IsValid)
        {
            var errors = string.Join(Environment.NewLine, validation.Errors);
            throw new InvalidOperationException($"Configuration validation failed for {_environment}:{Environment.NewLine}{errors}");
        }

        return baseConfig;
    }
}

// Usage in Program.cs
builder.Services.AddSingleton<IAgentConfigurationProvider, AgentConfigurationProvider>();
builder.Services.AddSingleton(serviceProvider =>
{
    var configService = new ConfigurationService(
        serviceProvider.GetRequiredService<IAgentConfigurationProvider>(),
        builder.Environment.EnvironmentName
    );
    return configService.GetConfigAsync().Result;
});
```

### Pattern 3: Configuration with Hot Reload

For systems that need to update configuration without restarts:

```csharp
public class HotReloadConfigurationService : IHostedService
{
    private readonly IAgentConfigurationProvider _configProvider;
    private readonly ILogger<HotReloadConfigurationService> _logger;
    private readonly string _configPath = "appsettings.json";
    private AgentSystemConfiguration _currentConfig;
    private readonly SemaphoreSlim _configLock = new(1, 1);

    public HotReloadConfigurationService(
        IAgentConfigurationProvider configProvider,
        ILogger<HotReloadConfigurationService> logger)
    {
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Load initial configuration
        _currentConfig = await _configProvider.LoadConfigurationAsync(_configPath);
        
        // Start watching for changes
        _ = Task.Run(() => WatchConfigurationChanges(cancellationToken), cancellationToken);
        
        _logger.LogInformation("Configuration service started with hot reload enabled");
    }

    private async Task WatchConfigurationChanges(CancellationToken cancellationToken)
    {
        try
        {
            await _configProvider.WatchConfigurationChangesAsync(_configPath, OnConfigurationChanged, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error watching configuration changes");
        }
    }

    private async void OnConfigurationChanged(AgentSystemConfiguration newConfig)
    {
        await _configLock.WaitAsync();
        try
        {
            _logger.LogInformation("Configuration file changed, validating new settings");
            
            // Validate new configuration
            var validation = await _configProvider.ValidateConfigurationAsync(newConfig);
            if (!validation.IsValid)
            {
                _logger.LogError("New configuration is invalid: {Errors}", string.Join(", ", validation.Errors));
                return;
            }

            // Apply new configuration
            var oldConfig = _currentConfig;
            _currentConfig = newConfig;
            
            _logger.LogInformation("Configuration successfully updated");
            
            // Notify other services of configuration change
            await NotifyConfigurationChange(oldConfig, newConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying new configuration");
        }
        finally
        {
            _configLock.Release();
        }
    }

    public async Task<AgentSystemConfiguration> GetCurrentConfigurationAsync()
    {
        await _configLock.WaitAsync();
        try
        {
            return _currentConfig;
        }
        finally
        {
            _configLock.Release();
        }
    }
}
```

## Validation That Actually Helps

### Beyond Basic Validation: Business Rule Validation

Built-in validation catches obvious errors, but business rules catch the subtle ones:

```csharp
// Add custom business rules
var configProvider = new AgentConfigurationProvider();

// Rule: Production must have at least 20 concurrent agents
configProvider.AddValidationRule(new ConfigurationValidationRule
{
    Field = "Agents.MaxConcurrentAgents",
    Rule = value => (int)value >= 20,
    ErrorMessage = "Production requires at least 20 concurrent agents for proper performance",
    IsWarning = false
});

// Rule: Warn about high timeout values
configProvider.AddValidationRule(new ConfigurationValidationRule
{
    Field = "Agents.DefaultTimeout",
    Rule = value => ((TimeSpan)value).TotalMinutes <= 10,
    ErrorMessage = "Agent timeout greater than 10 minutes may cause client timeouts",
    IsWarning = true // Warning, not error
});

// Rule: Database connection string must not contain passwords in production
configProvider.AddValidationRule(new ConfigurationValidationRule
{
    Field = "Database.ConnectionString",
    Rule = value => !value?.ToString()?.Contains("Password=") == true,
    ErrorMessage = "Connection strings should use managed identity or key vault in production",
    IsWarning = false
});
```

### Custom Validators for Complex Scenarios

For advanced scenarios, create custom validators:

```csharp
public class SecurityConfigurationValidator : IConfigurationValidator
{
    public async Task<ConfigurationValidationResult> ValidateAsync(
        AgentSystemConfiguration configuration, 
        IEnumerable<ConfigurationValidationRule> customRules = null, 
        CancellationToken cancellationToken = default)
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        // Validate JWT settings
        if (configuration.Security?.Jwt != null)
        {
            await ValidateJwtSettings(configuration.Security.Jwt, result);
        }

        // Validate API key strength
        if (!string.IsNullOrEmpty(configuration.Security?.ApiKey))
        {
            ValidateApiKeyStrength(configuration.Security.ApiKey, result);
        }

        // Validate CORS settings
        if (configuration.Security?.AllowedOrigins?.Any() == true)
        {
            ValidateCorsSettings(configuration.Security.AllowedOrigins, result);
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private async Task ValidateJwtSettings(JwtSettings jwt, ConfigurationValidationResult result)
    {
        // Check secret key strength
        if (jwt.SecretKey.Length < 32)
        {
            result.Errors.Add("JWT secret key must be at least 32 characters for security");
        }

        // Validate issuer format
        if (!Uri.IsWellFormedUriString(jwt.Issuer, UriKind.Absolute))
        {
            result.Errors.Add("JWT issuer must be a valid URI");
        }

        // Check expiration time
        if (jwt.ExpirationTime > TimeSpan.FromDays(7))
        {
            result.Warnings.Add("JWT expiration time longer than 7 days increases security risk");
        }
    }

    private void ValidateApiKeyStrength(string apiKey, ConfigurationValidationResult result)
    {
        if (apiKey.Length < 20)
        {
            result.Errors.Add("API key must be at least 20 characters long");
        }

        if (apiKey.All(char.IsLetterOrDigit))
        {
            result.Warnings.Add("API key should contain special characters for increased security");
        }

        // Check for common weak patterns
        if (apiKey.Contains("12345") || apiKey.Contains("password") || apiKey.ToLower().Contains("test"))
        {
            result.Errors.Add("API key contains weak patterns - generate a secure random key");
        }
    }

    private void ValidateCorsSettings(List<string> allowedOrigins, ConfigurationValidationResult result)
    {
        foreach (var origin in allowedOrigins)
        {
            if (origin == "*")
            {
                result.Errors.Add("CORS wildcard (*) not allowed in production environments");
                continue;
            }

            if (!Uri.IsWellFormedUriString(origin, UriKind.Absolute))
            {
                result.Errors.Add($"Invalid CORS origin format: {origin}");
            }
        }
    }
}
```

### Error Messages That Developers Can Act On

Good validation provides actionable feedback:

```csharp
// ❌ Bad error message
"Configuration validation failed"

// ✅ Good error message with action
"Agents.MaxConcurrentAgents: Value must be positive. Current value: -1. Set to a positive integer like 10."

// ✅ Even better - with environment context
"Agents.DefaultTimeout: Production timeout of 30 seconds is too low. Recommended: 5 minutes. Update appsettings.Production.json with 'DefaultTimeout': '00:05:00'"
```

## Real-World Configuration Examples

### Development Configuration
```json
{
  "agentSystem": {
    "name": "Development Agent System",
    "version": "1.0.0-dev",
    "environment": "Development"
  },
  "agents": {
    "maxConcurrentAgents": 3,
    "defaultTimeout": "00:02:00",
    "healthCheckInterval": "00:00:30"
  },
  "logging": {
    "level": "Debug",
    "providers": ["Console", "File"],
    "includeScopes": true,
    "file": {
      "path": "logs",
      "maxFileSizeBytes": 5242880
    }
  },
  "security": {
    "requireHttps": false,
    "allowedOrigins": ["http://localhost:3000"]
  }
}
```

### Production Configuration
```json
{
  "agentSystem": {
    "name": "Production Agent System",
    "version": "1.0.0",
    "environment": "Production"
  },
  "agents": {
    "maxConcurrentAgents": 50,
    "defaultTimeout": "00:10:00",
    "healthCheckInterval": "00:01:00",
    "maxRetryAttempts": 3,
    "retryDelay": "00:00:02"
  },
  "database": {
    "connectionString": "Server=prod-server;Database=Agents;Integrated Security=true;",
    "maxConnections": 100,
    "connectionTimeout": "00:00:30"
  },
  "logging": {
    "level": "Warning",
    "providers": ["Console", "File"],
    "includeScopes": false,
    "file": {
      "path": "/var/log/agents",
      "maxFileSizeBytes": 104857600,
      "maxRetainedFiles": 30
    }
  },
  "monitoring": {
    "enabled": true,
    "metricsInterval": "00:00:30",
    "healthChecks": {
      "enabled": true,
      "path": "/health",
      "timeout": "00:00:10"
    },
    "tracing": {
      "enabled": true,
      "serviceName": "production-agents",
      "samplingRatio": 0.1
    }
  },
  "security": {
    "requireHttps": true,
    "allowedOrigins": ["https://app.company.com"],
    "jwt": {
      "issuer": "https://auth.company.com",
      "audience": "https://api.company.com",
      "expirationTime": "24:00:00"
    }
  }
}
```

## Configuration Management Best Practices

### 1. Use Environment Variables for Secrets
```csharp
// appsettings.json - never put secrets here
{
  "database": {
    "connectionString": "Server=prod-server;Database=Agents;Integrated Security=true;"
  }
}

// Environment variables (Docker/Kubernetes)
DATABASE__CONNECTIONSTRING=Server=prod-server;Database=Agents;User Id=${DB_USER};Password=${DB_PASSWORD};

// In code - automatic override
var config = await configProvider.LoadConfigurationAsync("appsettings.json", useEnvironmentVariables: true);
// Environment variables automatically override JSON values
```

### 2. Validate Early and Often
```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Load and validate configuration at startup
    var configProvider = new AgentConfigurationProvider();
    var config = await configProvider.LoadConfigurationAsync("appsettings.json");
    
    // Add environment-specific validators
    if (Environment.IsProduction())
    {
        var securityValidator = new SecurityConfigurationValidator();
        var securityValidation = await securityValidator.ValidateAsync(config);
        if (!securityValidation.IsValid)
        {
            throw new InvalidOperationException($"Production security validation failed: {string.Join(", ", securityValidation.Errors)}");
        }
    }

    services.AddSingleton(config);
}
```

### 3. Configuration Monitoring
```csharp
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly AgentSystemConfiguration _config;

    public ConfigurationHealthCheck(AgentSystemConfiguration config)
    {
        _config = config;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var checks = new List<string>();

        // Check critical configuration values
        if (_config.Agents.MaxConcurrentAgents <= 0)
            checks.Add("MaxConcurrentAgents is invalid");

        if (_config.Agents.DefaultTimeout <= TimeSpan.Zero)
            checks.Add("DefaultTimeout is invalid");

        if (_config.Database != null && string.IsNullOrEmpty(_config.Database.ConnectionString))
            checks.Add("Database connection string is missing");

        return Task.FromResult(checks.Any() 
            ? HealthCheckResult.Unhealthy($"Configuration issues: {string.Join(", ", checks)}")
            : HealthCheckResult.Healthy("Configuration is valid"));
    }
}

// Register health check
services.AddHealthChecks().AddCheck<ConfigurationHealthCheck>("configuration");
```

### 4. Configuration Documentation
```csharp
/// <summary>
/// Generates configuration documentation from the current settings
/// </summary>
public class ConfigurationDocumentationGenerator
{
    public string GenerateMarkdown(AgentSystemConfiguration config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Agent System Configuration");
        sb.AppendLine();
        
        sb.AppendLine("## System Settings");
        sb.AppendLine($"- **Name**: {config.AgentSystem.Name}");
        sb.AppendLine($"- **Version**: {config.AgentSystem.Version}");
        sb.AppendLine($"- **Environment**: {config.AgentSystem.Environment}");
        sb.AppendLine();
        
        sb.AppendLine("## Agent Runtime");
        sb.AppendLine($"- **Max Concurrent Agents**: {config.Agents.MaxConcurrentAgents}");
        sb.AppendLine($"- **Default Timeout**: {config.Agents.DefaultTimeout}");
        sb.AppendLine($"- **Health Check Interval**: {config.Agents.HealthCheckInterval}");
        sb.AppendLine();
        
        if (config.Database != null)
        {
            sb.AppendLine("## Database");
            sb.AppendLine($"- **Max Connections**: {config.Database.MaxConnections}");
            sb.AppendLine($"- **Connection Timeout**: {config.Database.ConnectionTimeout}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

## Troubleshooting Configuration Issues

### Common Problems and Solutions

#### 1. "Configuration validation failed"
```csharp
// Get detailed validation information
var result = await configProvider.ValidateConfigurationAsync(config);
if (!result.IsValid)
{
    Console.WriteLine("Validation Errors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
    
    Console.WriteLine("Validation Warnings:");
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}
```

#### 2. Environment variables not being picked up
```csharp
// Ensure environment variables are enabled
var config = await configProvider.LoadConfigurationAsync("appsettings.json", useEnvironmentVariables: true);

// Check if specific environment variable is set
var dbConnection = Environment.GetEnvironmentVariable("DATABASE__CONNECTIONSTRING");
if (string.IsNullOrEmpty(dbConnection))
{
    Console.WriteLine("DATABASE__CONNECTIONSTRING environment variable not found");
}
```

#### 3. Configuration not updating during hot reload
```csharp
// Enable file watching with proper error handling
try 
{
    await configProvider.WatchConfigurationChangesAsync("appsettings.json", 
        newConfig => 
        {
            Console.WriteLine($"Configuration updated at {DateTime.Now}");
            // Update your services with new configuration
        }, 
        cancellationToken);
}
catch (FileNotFoundException)
{
    Console.WriteLine("Configuration file not found for watching");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("No permission to watch configuration file");
}
```

## Next Steps

### Integration with Other Packages
- **Agent.Security** - Secure configuration storage with Azure Key Vault
- **Agent.Observability** - Monitor configuration changes and validation metrics
- **Agent.DI** - Automatic configuration injection and dependency setup

### Advanced Configuration Patterns
- **Configuration Templates** - Reusable configuration patterns for different deployment scenarios
- **Dynamic Configuration** - Runtime configuration updates without restarts
- **Configuration Versioning** - Track and rollback configuration changes
- **Multi-tenant Configuration** - Different settings per tenant or customer

---

**🎯 You now have bulletproof configuration management that will keep your agent systems running smoothly in any environment!**

Remember: Good configuration prevents 90% of production issues. Invest the time to get it right.