# 🔒 Agent.Security: Enterprise-Grade Protection

## What is Agent.Security?

**Simple explanation:** Agent.Security provides enterprise-grade authentication, authorization, and secret management for your AI agent systems.

**When to use it:** Every production agent system needs robust security - this package ensures only authorized users and systems can access your agents and their capabilities.

**Key concepts in plain English:**
- **Authentication** verifies who you are (login credentials, tokens)
- **Authorization** determines what you can do (permissions, roles)
- **Secret management** securely stores and retrieves sensitive data (API keys, passwords)
- **Production hardening** implements security best practices automatically

## Security Without the Complexity

### The Security Story
"Your AI agents will handle sensitive data, make important decisions, and integrate with critical systems. Here's how to protect it all without making your developers' lives miserable."

```
🔓 Without Agent.Security - Security nightmare:
┌─────────────────────────────────────────────────────────┐
│  Your Agent System                                      │
│  ├─ API keys hardcoded in source code                  │
│  ├─ No authentication on endpoints                     │
│  ├─ Everyone has admin access                          │
│  ├─ Passwords stored in plain text                     │
│  └─ Security = "We'll add that later"                  │
└─────────────────────────────────────────────────────────┘
Result: 🔥 Data breaches, compliance failures, sleepless nights

🔒 With Agent.Security - Security done right:
┌─────────────────────────────────────────────────────────┐
│  Your Agent System                                      │
│  ├─ JWT tokens for authentication                       │
│  ├─ Role-based access control                          │
│  ├─ Azure Key Vault for secrets                        │
│  ├─ Attribute-based authorization                      │
│  └─ Production security checklist ✅                   │
└─────────────────────────────────────────────────────────┘
Result: 😴 Sleep well, pass audits, happy customers
```

## Authentication Patterns

### Pattern 1: Local Development
Perfect for getting started and testing locally:

```csharp
// Program.cs - Development setup
using Agent.Security;
using Agent.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add GenericAgents services
builder.Services.AddAgentServices(builder.Configuration);

// Add local JWT authentication for development
builder.Services.AddLocalJwtAuthentication(
    signingKey: "development-key-minimum-32-characters-for-security",
    issuer: "https://localhost:5001",
    audience: "agent-api"
);

var app = builder.Build();

// Enable authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

**Development Controller Example:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class DevelopmentController : ControllerBase
{
    private readonly IJwtTokenProvider _tokenProvider;

    public DevelopmentController(IJwtTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    // Development endpoint to generate test tokens
    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken([FromBody] TokenRequest request)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.NameIdentifier, request.UserId),
            new Claim(ClaimTypes.Role, request.Role),
            new Claim("permission", "agent:execute"),
            new Claim("permission", "workflow:create")
        };

        var token = await _tokenProvider.GenerateTokenAsync(claims, TimeSpan.FromHours(8));

        return Ok(new { token, expiresIn = 8 * 3600 });
    }
}

public class TokenRequest
{
    public string Username { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
}
```

### Pattern 2: Production with Okta
Enterprise-grade authentication with your existing identity provider:

```csharp
// Program.cs - Okta integration
using Agent.Security;
using Agent.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add GenericAgents services
builder.Services.AddAgentServices(builder.Configuration);

// Add Okta JWT authentication
builder.Services.AddOktaJwtAuthentication(options =>
{
    options.Domain = builder.Configuration["Okta:Domain"];
    options.AuthorizationServerId = builder.Configuration["Okta:AuthorizationServerId"];
    options.ApiToken = builder.Configuration["Okta:ApiToken"]; // For user info lookup
    options.ValidateIssuer = true;
    options.ValidateAudience = true;
    options.ValidAudiences = new[] { "api://default", builder.Configuration["Okta:Audience"] };
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**appsettings.Production.json:**
```json
{
  "Okta": {
    "Domain": "https://your-company.okta.com",
    "AuthorizationServerId": "default",
    "Audience": "api://agent-system",
    "ApiToken": "#{OKTA_API_TOKEN}#" // Token replacement in deployment
  }
}
```

**Production Controller with Okta:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires valid Okta JWT token
public class AgentController : ControllerBase
{
    private readonly CustomerServiceAgent _agent;
    private readonly ILogger<AgentController> _logger;

    public AgentController(CustomerServiceAgent agent, ILogger<AgentController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    [HttpPost("process")]
    [RequirePermission("agent:execute")] // Custom permission check
    public async Task<IActionResult> ProcessRequest([FromBody] AgentRequest request)
    {
        // Get user info from Okta JWT token
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        _logger.LogInformation("Processing agent request for user {UserId} with roles {Roles}", 
            userId, string.Join(", ", userRoles));

        // Add user context to request
        request.Context["userId"] = userId;
        request.Context["userRoles"] = userRoles;

        var result = await _agent.ExecuteAsync(request);
        return Ok(result);
    }

    [HttpGet("user-info")]
    public async Task<IActionResult> GetUserInfo()
    {
        // Access user information from JWT claims
        var userInfo = new
        {
            Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Name = User.FindFirst(ClaimTypes.Name)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            Permissions = User.FindAll("permission").Select(c => c.Value).ToArray()
        };

        return Ok(userInfo);
    }
}
```

### Pattern 3: Azure AD Integration
For organizations using Microsoft's identity platform:

```csharp
// Program.cs - Azure AD setup
using Agent.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAgentServices(builder.Configuration);

// Add Azure AD authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAD"));

// Add authorization policies
builder.Services.AddAgentAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

**appsettings.json for Azure AD:**
```json
{
  "AzureAD": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-company.com",
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "87654321-4321-4321-4321-210987654321",
    "Audience": "api://agent-system"
  }
}
```

## Authorization That Makes Sense

### Role-Based Access Control (RBAC)

**What roles actually mean in AI agent context:**
- **Admin** - Can manage system configuration, view all data, execute any agent
- **Agent Manager** - Can create and configure agents, execute workflows
- **Agent User** - Can execute specific agents they have permission for
- **Viewer** - Can view agent results and system status (read-only)

```csharp
// Define your roles clearly
public static class AgentRoles
{
    public const string Admin = "admin";
    public const string AgentManager = "agent-manager";
    public const string AgentUser = "agent-user";
    public const string Viewer = "viewer";
}

// Define permissions granularly
public static class AgentPermissions
{
    public static class Agent
    {
        public const string Execute = "agent:execute";
        public const string Create = "agent:create";
        public const string Configure = "agent:configure";
        public const string ViewResults = "agent:view-results";
    }

    public static class Workflow
    {
        public const string Create = "workflow:create";
        public const string Execute = "workflow:execute";
        public const string Manage = "workflow:manage";
        public const string ViewResults = "workflow:view-results";
    }

    public static class System
    {
        public const string Configure = "system:configure";
        public const string ViewMetrics = "system:view-metrics";
        public const string ViewLogs = "system:view-logs";
        public const string ManageSecrets = "system:manage-secrets";
    }
}
```

### Permission Design Patterns

#### Pattern 1: Simple Role-Based Authorization
```csharp
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    [HttpPost]
    [RequireAdmin] // Only admins can create workflows
    public async Task<IActionResult> CreateWorkflow([FromBody] WorkflowDefinition workflow)
    {
        // Implementation
        return Ok();
    }

    [HttpGet("{id}")]
    [Authorize(Roles = $"{AgentRoles.Admin},{AgentRoles.AgentManager},{AgentRoles.AgentUser}")]
    public async Task<IActionResult> GetWorkflow(string id)
    {
        // Implementation
        return Ok();
    }

    [HttpDelete("{id}")]
    [RequireAdminOrService] // Admins or service accounts
    public async Task<IActionResult> DeleteWorkflow(string id)
    {
        // Implementation
        return Ok();
    }
}
```

#### Pattern 2: Fine-Grained Permission Control
```csharp
[ApiController]
[Route("api/agents")]
public class AgentExecutionController : ControllerBase
{
    [HttpPost("{agentId}/execute")]
    [RequirePermission("agent:execute")] // Specific permission required
    public async Task<IActionResult> ExecuteAgent(string agentId, [FromBody] AgentRequest request)
    {
        // Additional permission check based on agent type
        if (agentId.StartsWith("admin-") && !User.IsInRole(AgentRoles.Admin))
        {
            return Forbid("Admin agents require admin role");
        }

        // Implementation
        return Ok();
    }

    [HttpGet("{agentId}/results")]
    [RequirePermission("agent:view-results")]
    public async Task<IActionResult> GetAgentResults(string agentId)
    {
        // Implementation
        return Ok();
    }
}
```

#### Pattern 3: Resource-Based Authorization
```csharp
public class ResourceAuthorizationService
{
    private readonly IAuthorizationService _authorizationService;

    public ResourceAuthorizationService(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<bool> CanUserAccessWorkflow(ClaimsPrincipal user, string workflowId)
    {
        // Check if user owns the workflow or has admin access
        var workflow = await GetWorkflowAsync(workflowId);
        if (workflow == null) return false;

        // Owner can always access
        if (workflow.CreatedBy == user.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            return true;

        // Admin can access anything
        if (user.IsInRole(AgentRoles.Admin))
            return true;

        // Check specific permissions
        var authResult = await _authorizationService.AuthorizeAsync(user, workflow, "CanViewWorkflow");
        return authResult.Succeeded;
    }
}

[ApiController]
[Route("api/workflows")]
public class WorkflowController : ControllerBase
{
    private readonly ResourceAuthorizationService _authService;

    public WorkflowController(ResourceAuthorizationService authService)
    {
        _authService = authService;
    }

    [HttpGet("{workflowId}")]
    [Authorize]
    public async Task<IActionResult> GetWorkflow(string workflowId)
    {
        if (!await _authService.CanUserAccessWorkflow(User, workflowId))
        {
            return Forbid("You don't have access to this workflow");
        }

        // Get and return workflow
        return Ok();
    }
}
```

### Common Security Mistakes and How to Avoid Them

#### 1. The "Everyone Is Admin" Mistake
```csharp
// ❌ Wrong - gives everyone admin access
[Authorize] // Any authenticated user can do anything
public class SystemController : ControllerBase
{
    [HttpDelete("database/clear")]
    public async Task<IActionResult> ClearDatabase() 
    {
        // Dangerous operation with no permission check!
    }
}

// ✅ Correct - explicit admin requirement
[RequireAdmin] // Only admins can access this controller
public class SystemController : ControllerBase
{
    [HttpDelete("database/clear")]
    public async Task<IActionResult> ClearDatabase() 
    {
        // Safe - only admins can reach this
    }
}
```

#### 2. The "Token in URL" Mistake
```csharp
// ❌ Wrong - token in URL gets logged everywhere
[HttpGet("agents/{agentId}/status?token={token}")]
public async Task<IActionResult> GetStatus(string agentId, string token) 
{
    // Tokens in URLs are logged, cached, shared accidentally
}

// ✅ Correct - token in header
[HttpGet("agents/{agentId}/status")]
[Authorize] // Token automatically extracted from Authorization header
public async Task<IActionResult> GetStatus(string agentId) 
{
    // Secure - token never appears in logs or URLs
}
```

#### 3. The "Client-Side Security" Mistake
```csharp
// ❌ Wrong - trusting the client
[HttpPost("agents/execute")]
public async Task<IActionResult> ExecuteAgent([FromBody] ExecuteRequest request)
{
    // Trusting request.UserId from client - never do this!
    if (request.UserId == "admin")
    {
        // Anyone can claim to be admin
    }
}

// ✅ Correct - server-side validation
[HttpPost("agents/execute")]
[Authorize]
public async Task<IActionResult> ExecuteAgent([FromBody] ExecuteRequest request)
{
    // Get user ID from authenticated JWT token
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var isAdmin = User.IsInRole(AgentRoles.Admin);
    
    // Trust the server, not the client
}
```

## Protecting Agent Endpoints

### Pattern 1: Public Endpoints (Use Sparingly)
```csharp
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous] // Explicitly mark as public
    public IActionResult Health()
    {
        // Only return non-sensitive information
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("version")]
    [AllowAnonymous]
    public IActionResult Version()
    {
        return Ok(new { version = "1.0.0" });
    }
}
```

### Pattern 2: Protected Endpoints
```csharp
[ApiController]
[Route("api/agents")]
[Authorize] // All endpoints require authentication by default
public class AgentController : ControllerBase
{
    [HttpGet]
    [RequirePermission("agent:view-results")] // Additional permission check
    public async Task<IActionResult> ListAgents()
    {
        // Return list of agents user has access to
    }

    [HttpPost("{agentId}/execute")]
    [RequirePermission("agent:execute")]
    public async Task<IActionResult> ExecuteAgent(string agentId, [FromBody] AgentRequest request)
    {
        // Execute agent with user context
    }

    [HttpGet("admin/all")]
    [RequireAdmin] // Admin-only endpoint
    public async Task<IActionResult> GetAllAgents()
    {
        // Return all agents - admin only
    }
}
```

### Pattern 3: Service-to-Service Communication
```csharp
[ApiController]
[Route("api/internal")]
[RequireService] // Only service accounts can access
public class InternalController : ControllerBase
{
    [HttpPost("agents/batch-execute")]
    public async Task<IActionResult> BatchExecute([FromBody] BatchRequest request)
    {
        // Internal API for service-to-service communication
        // Higher throughput, different authentication
    }

    [HttpGet("metrics")]
    [RequireAdminOrService] // Admin users or services
    public async Task<IActionResult> GetDetailedMetrics()
    {
        // Sensitive metrics data
    }
}
```

## Secret Management in the Real World

### Development Secrets vs Production Secrets

```
🏠 Development Environment:
├─ appsettings.Development.json (safe for dev secrets)
├─ User secrets (dotnet user-secrets)
├─ Environment variables (local only)
└─ Local configuration files (never committed)

🏢 Production Environment:
├─ Azure Key Vault (primary choice)
├─ AWS Secrets Manager (if using AWS)
├─ Environment variables (container/k8s secrets)
└─ Never plain text files or configuration!
```

### Azure Key Vault Integration Walkthrough

#### Step 1: Setup Azure Key Vault Service
```csharp
// Program.cs - Production secret management
using Agent.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAgentServices(builder.Configuration);

// Add Azure Key Vault secret management
builder.Services.AddAzureKeyVaultSecretManagement(options =>
{
    options.VaultUrl = builder.Configuration["Azure:KeyVault:VaultUrl"];
    options.UseManagedIdentity = true; // Recommended for production
    // options.ClientId = "..."; // Alternative: service principal
    // options.TenantId = "...";
});

var app = builder.Build();
```

#### Step 2: Use Secrets in Your Agents
```csharp
public class SecureAgent : BaseAgent
{
    private readonly ISecretManager _secretManager;
    private readonly ILogger<SecureAgent> _logger;

    public SecureAgent(ISecretManager secretManager, ILogger<SecureAgent> logger)
        : base("secure-agent", "Handles sensitive operations securely")
    {
        _secretManager = secretManager;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve secrets securely at runtime
            var apiKey = await _secretManager.GetSecretAsync("external-api-key", cancellationToken);
            var connectionString = await _secretManager.GetSecretAsync("database-connection", cancellationToken);

            if (string.IsNullOrEmpty(apiKey))
            {
                return AgentResult.CreateError("External API key not configured");
            }

            // Use the secret (never log it!)
            _logger.LogInformation("Executing secure operation for request {RequestId}", request.Id);
            
            // Your secure logic here
            var result = await CallExternalApiAsync(apiKey, request.Input, cancellationToken);
            
            return AgentResult.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            // Log error without exposing secrets
            _logger.LogError(ex, "Secure operation failed for request {RequestId}", request.Id);
            return AgentResult.CreateError("Secure operation failed");
        }
    }

    private async Task<string> CallExternalApiAsync(string apiKey, string input, CancellationToken cancellationToken)
    {
        // Implementation with proper secret handling
        // Never log or expose the apiKey
        return "secure-result";
    }
}
```

#### Step 3: Secret Rotation Strategies
```csharp
public class SecretRotationService : BackgroundService
{
    private readonly ISecretManager _secretManager;
    private readonly ILogger<SecretRotationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public SecretRotationService(ISecretManager secretManager, ILogger<SecretRotationService> logger)
    {
        _secretManager = secretManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRotateSecrets(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during secret rotation check");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndRotateSecrets(CancellationToken cancellationToken)
    {
        var secretNames = await _secretManager.ListSecretNamesAsync(cancellationToken);

        foreach (var secretName in secretNames)
        {
            if (secretName.Contains("rotate-weekly"))
            {
                // Check if secret needs rotation based on age/usage
                await CheckSecretRotationNeeded(secretName, cancellationToken);
            }
        }
    }

    private async Task CheckSecretRotationNeeded(string secretName, CancellationToken cancellationToken)
    {
        // Implementation depends on your secret rotation policy
        _logger.LogInformation("Checking rotation for secret: {SecretName}", secretName);
        
        // Example: Rotate database connection strings weekly
        // Example: Rotate API keys monthly  
        // Example: Rotate certificates before expiry
    }
}

// Register the rotation service
builder.Services.AddHostedService<SecretRotationService>();
```

### What to Never Do with Secrets

```csharp
// ❌ NEVER do these things:

// 1. Never hardcode secrets
public class BadAgent : BaseAgent
{
    private const string API_KEY = "sk-1234567890abcdef"; // NEVER!
}

// 2. Never log secrets
_logger.LogInformation("Using API key: {ApiKey}", apiKey); // NEVER!

// 3. Never put secrets in configuration files committed to source control
// appsettings.json - NEVER commit this with real secrets:
{
  "ConnectionStrings": {
    "Database": "Server=prod;User=sa;Password=RealPassword123;" // NEVER!
  }
}

// 4. Never return secrets in API responses
[HttpGet("config")]
public IActionResult GetConfig()
{
    return Ok(new { 
        DatabaseConnection = _connectionString, // NEVER!
        ApiKeys = _apiKeys // NEVER!
    });
}

// 5. Never store secrets in client-side code
// JavaScript/HTML - NEVER:
const API_KEY = "secret-key-123"; // NEVER!
```

### ✅ What TO Do with Secrets
```csharp
// ✅ DO these things:

// 1. Use secret managers
var apiKey = await _secretManager.GetSecretAsync("external-api-key", cancellationToken);

// 2. Log without exposing secrets
_logger.LogInformation("API call completed successfully"); // Good
_logger.LogInformation("Using API key ending in ...{LastFour}", apiKey.Substring(apiKey.Length - 4)); // Acceptable

// 3. Use environment variables for container deployments
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

// 4. Return success/failure, not the secret
[HttpGet("test-connection")]
public async Task<IActionResult> TestConnection()
{
    var connectionString = await _secretManager.GetSecretAsync("database-connection");
    var isValid = await TestDatabaseConnection(connectionString);
    
    return Ok(new { isConnected = isValid }); // Don't return the actual connection string
}

// 5. Use proper authentication for client access
// Clients get JWT tokens, not API keys
```

## Production Security Checklist

### 🔐 Authentication & Authorization
- [ ] **JWT tokens implemented** with proper expiration times
- [ ] **Role-based access control** configured with least-privilege principle
- [ ] **External identity provider** integrated (Okta, Azure AD, etc.)
- [ ] **Service accounts** configured for system-to-system communication
- [ ] **Token validation** includes signature, expiration, and audience checks
- [ ] **Admin endpoints** explicitly protected with admin-only authorization
- [ ] **Public endpoints** minimized and explicitly marked with `[AllowAnonymous]`

### 🔒 Secret Management
- [ ] **Azure Key Vault** or equivalent secret manager configured
- [ ] **No secrets in source code** or configuration files
- [ ] **Environment variables** used only for non-production environments
- [ ] **Managed identity** enabled for Azure resources
- [ ] **Secret rotation** policies implemented
- [ ] **API keys** never logged or returned in responses
- [ ] **Connection strings** retrieved from secure storage

### 🌐 Network Security
- [ ] **HTTPS enforced** for all endpoints (`app.UseHttpsRedirection()`)
- [ ] **CORS policies** configured restrictively
- [ ] **API rate limiting** implemented to prevent abuse
- [ ] **IP whitelisting** for admin endpoints (if applicable)
- [ ] **Security headers** configured (HSTS, X-Frame-Options, etc.)

### 📊 Monitoring & Auditing
- [ ] **Authentication failures** logged and monitored
- [ ] **Authorization failures** tracked and alerted
- [ ] **Secret access** audited and monitored
- [ ] **Admin actions** logged with user details
- [ ] **Suspicious activity** alerts configured
- [ ] **Security events** integrated with SIEM systems

### 🔧 Configuration Security
- [ ] **Production configurations** validated at startup
- [ ] **Security headers** middleware configured
- [ ] **Cookie security** settings applied (if using cookies)
- [ ] **CSRF protection** enabled for state-changing operations
- [ ] **Input validation** implemented on all endpoints
- [ ] **SQL injection** protection verified
- [ ] **XSS protection** headers configured

## Security Implementation Examples

### Complete Secure Agent System Setup
```csharp
// Program.cs - Production-ready security setup
using Agent.DI;
using Agent.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Basic services
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();

// GenericAgents services
builder.Services.AddAgentServices(builder.Configuration);
builder.Services.AddAgentToolDiscovery();

// Production security stack
if (builder.Environment.IsProduction())
{
    // Azure Key Vault for secrets
    builder.Services.AddAzureKeyVaultSecretManagement(options =>
    {
        options.VaultUrl = builder.Configuration["Azure:KeyVault:VaultUrl"];
        options.UseManagedIdentity = true;
    });

    // Okta authentication
    builder.Services.AddOktaJwtAuthentication(options =>
    {
        options.Domain = builder.Configuration["Okta:Domain"];
        options.AuthorizationServerId = "default";
        options.ValidAudiences = new[] { builder.Configuration["Okta:Audience"] };
        options.ApiToken = builder.Configuration["Okta:ApiToken"];
    });
}
else
{
    // Development security
    builder.Services.AddEnvironmentSecretManagement();
    builder.Services.AddLocalJwtAuthentication(
        builder.Configuration["JWT:SigningKey"] ?? "development-key-minimum-32-characters"
    );
}

// Authorization policies
builder.Services.AddAgentAuthorization();

// Security middleware configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            builder.WithOrigins("https://app.company.com")
                  .WithMethods("GET", "POST")
                  .WithHeaders("Authorization", "Content-Type");
        }
    });
});

var app = builder.Build();

// Security middleware pipeline (order matters!)
if (app.Environment.IsProduction())
{
    app.UseHsts(); // HTTP Strict Transport Security
}

app.UseHttpsRedirection(); // Force HTTPS
app.UseCors(); // CORS policy
app.UseAuthentication(); // JWT token validation
app.UseAuthorization(); // Permission checking

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

### Secure Agent Controller Template
```csharp
[ApiController]
[Route("api/v1/agents")]
[Authorize] // All endpoints require authentication
[Produces("application/json")]
public class SecureAgentController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecureAgentController> _logger;
    private readonly ISecretManager _secretManager;

    public SecureAgentController(
        IServiceProvider serviceProvider,
        ILogger<SecureAgentController> logger,
        ISecretManager secretManager)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _secretManager = secretManager;
    }

    [HttpPost("{agentType}/execute")]
    [RequirePermission("agent:execute")]
    [ProducesResponseType(typeof(AgentResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(429)] // Rate limited
    public async Task<IActionResult> ExecuteAgent(
        string agentType, 
        [FromBody] SecureAgentRequest request,
        CancellationToken cancellationToken)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(agentType) || request == null)
        {
            return BadRequest("Agent type and request are required");
        }

        // Get user context from JWT
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        _logger.LogInformation(
            "Agent execution requested - Type: {AgentType}, User: {UserId}, Roles: {Roles}",
            agentType, userId, string.Join(", ", userRoles));

        try
        {
            // Create agent based on type (with proper authorization)
            var agent = await CreateAuthorizedAgent(agentType, userId, userRoles, cancellationToken);
            if (agent == null)
            {
                _logger.LogWarning("Unauthorized agent access attempt - Type: {AgentType}, User: {UserId}", 
                    agentType, userId);
                return Forbid($"Access denied to agent type: {agentType}");
            }

            // Add security context to request
            var secureRequest = new AgentRequest
            {
                Input = request.Input,
                Context = request.Context ?? new Dictionary<string, object>(),
                Metadata = new Dictionary<string, object>
                {
                    ["userId"] = userId,
                    ["userRoles"] = userRoles,
                    ["requestId"] = Guid.NewGuid().ToString(),
                    ["timestamp"] = DateTime.UtcNow,
                    ["ipAddress"] = HttpContext.Connection.RemoteIpAddress?.ToString()
                }
            };

            // Execute with timeout and monitoring
            var result = await agent.ExecuteAsync(secureRequest, cancellationToken);

            _logger.LogInformation(
                "Agent execution completed - Type: {AgentType}, User: {UserId}, Success: {Success}",
                agentType, userId, result.Success);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized agent execution attempt - Type: {AgentType}, User: {UserId}", 
                agentType, userId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Agent execution failed - Type: {AgentType}, User: {UserId}", 
                agentType, userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("health")]
    [AllowAnonymous] // Public health check
    [ProducesResponseType(200)]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0" 
        });
    }

    [HttpGet("user/permissions")]
    [RequirePermission("user:view-permissions")]
    [ProducesResponseType(typeof(UserPermissions), 200)]
    public IActionResult GetUserPermissions()
    {
        var permissions = new UserPermissions
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            Permissions = User.FindAll("permission").Select(c => c.Value).ToList(),
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(
                long.Parse(User.FindFirst("exp")?.Value ?? "0"))
        };

        return Ok(permissions);
    }

    private async Task<IAgent?> CreateAuthorizedAgent(
        string agentType, 
        string userId, 
        List<string> userRoles, 
        CancellationToken cancellationToken)
    {
        // Check if user has permission for this agent type
        var requiredPermission = $"agent:{agentType}:execute";
        if (!User.HasClaim("permission", requiredPermission) && !userRoles.Contains(AgentRoles.Admin))
        {
            return null;
        }

        // Create agent instance based on type
        return agentType.ToLower() switch
        {
            "customer-service" => _serviceProvider.GetService<CustomerServiceAgent>(),
            "data-analysis" => _serviceProvider.GetService<DataAnalysisAgent>(),
            "content-generation" => userRoles.Contains(AgentRoles.AgentManager) 
                ? _serviceProvider.GetService<ContentGenerationAgent>() 
                : null,
            _ => null
        };
    }
}

public class SecureAgentRequest
{
    public string Input { get; set; } = string.Empty;
    public Dictionary<string, object>? Context { get; set; }
}

public class UserPermissions
{
    public string? UserId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public DateTimeOffset ExpiresAt { get; set; }
}
```

## Next Steps

### Integration with Other Packages
- **Agent.Observability** - Monitor security events and authentication metrics
- **Agent.Orchestration** - Secure multi-agent workflows with proper authorization
- **Agent.Configuration** - Secure configuration management with encrypted settings

### Advanced Security Patterns
- **Zero Trust Architecture** - Never trust, always verify every request
- **Multi-Factor Authentication** - Additional security layers for sensitive operations
- **Certificate-Based Authentication** - For high-security environments
- **API Security Scanning** - Automated security testing in CI/CD

### Compliance Considerations
- **GDPR Compliance** - Data protection and user consent management
- **SOX Compliance** - Financial controls and audit trails
- **HIPAA Compliance** - Healthcare data protection
- **SOC 2 Compliance** - Security, availability, and processing integrity

---

**🎯 You now have enterprise-grade security that scales with your AI agent system!**

Remember: Security is not a feature you add later - it's the foundation you build on. With Agent.Security, you get production-ready authentication, authorization, and secret management that grows with your needs while keeping your developers productive and your data secure.