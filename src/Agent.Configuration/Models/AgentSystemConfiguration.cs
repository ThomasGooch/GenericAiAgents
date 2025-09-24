using System.ComponentModel.DataAnnotations;

namespace Agent.Configuration.Models;

/// <summary>
/// Root configuration model for the agent system
/// </summary>
public class AgentSystemConfiguration
{
    /// <summary>
    /// System-level settings
    /// </summary>
    public AgentSystemSettings AgentSystem { get; set; } = new();

    /// <summary>
    /// Agent runtime settings
    /// </summary>
    public AgentSettings Agents { get; set; } = new();

    /// <summary>
    /// Database configuration
    /// </summary>
    public DatabaseSettings? Database { get; set; }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Monitoring and observability settings
    /// </summary>
    public MonitoringSettings? Monitoring { get; set; }

    /// <summary>
    /// Security settings
    /// </summary>
    public SecuritySettings? Security { get; set; }
}

/// <summary>
/// System-level configuration settings
/// </summary>
public class AgentSystemSettings
{
    /// <summary>
    /// System name identifier
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// System version (semantic versioning)
    /// </summary>
    [Required]
    [RegularExpression(@"^\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?$", ErrorMessage = "Version must be in semantic version format (e.g., 1.0.0)")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Deployment environment
    /// </summary>
    [Required]
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the system
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Contact information for system maintainers
    /// </summary>
    public string? Contact { get; set; }
}

/// <summary>
/// Agent runtime configuration settings
/// </summary>
public class AgentSettings
{
    /// <summary>
    /// Maximum number of agents that can run concurrently
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MaxConcurrentAgents must be positive")]
    public int MaxConcurrentAgents { get; set; } = 10;

    /// <summary>
    /// Default timeout for agent operations
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval for periodic health checks
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum retry attempts for failed operations
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetryAttempts must be between 0 and 10")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Database connection string
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of database connections in pool
    /// </summary>
    [Range(1, 1000, ErrorMessage = "MaxConnections must be between 1 and 1000")]
    public int MaxConnections { get; set; } = 20;

    /// <summary>
    /// Connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Command timeout for database operations
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to enable connection pooling
    /// </summary>
    public bool EnablePooling { get; set; } = true;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Logging level (Trace, Debug, Information, Warning, Error, Critical)
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Enabled logging providers
    /// </summary>
    public List<string> Providers { get; set; } = new() { "Console" };

    /// <summary>
    /// File logging settings
    /// </summary>
    public FileLoggingSettings? File { get; set; }

    /// <summary>
    /// Whether to include scopes in log output
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Whether to include timestamps in log output
    /// </summary>
    public bool IncludeTimestamps { get; set; } = true;
}

/// <summary>
/// File logging specific settings
/// </summary>
public class FileLoggingSettings
{
    /// <summary>
    /// Path for log files
    /// </summary>
    public string Path { get; set; } = "logs";

    /// <summary>
    /// Maximum size of individual log files
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Maximum number of log files to retain
    /// </summary>
    public int MaxRetainedFiles { get; set; } = 10;

    /// <summary>
    /// Log file name pattern
    /// </summary>
    public string FileNamePattern { get; set; } = "agent-{Date}.log";
}

/// <summary>
/// Monitoring and observability settings
/// </summary>
public class MonitoringSettings
{
    /// <summary>
    /// Whether monitoring is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Metrics collection interval
    /// </summary>
    public TimeSpan MetricsInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Health check endpoint settings
    /// </summary>
    public HealthCheckSettings? HealthChecks { get; set; }

    /// <summary>
    /// Distributed tracing settings
    /// </summary>
    public TracingSettings? Tracing { get; set; }
}

/// <summary>
/// Health check endpoint settings
/// </summary>
public class HealthCheckSettings
{
    /// <summary>
    /// Whether health check endpoints are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// Detailed health check endpoint path
    /// </summary>
    public string DetailedPath { get; set; } = "/health/detailed";

    /// <summary>
    /// Health check timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Distributed tracing settings
/// </summary>
public class TracingSettings
{
    /// <summary>
    /// Whether tracing is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Tracing service name
    /// </summary>
    public string ServiceName { get; set; } = "agent-system";

    /// <summary>
    /// Tracing endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Sampling ratio (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double SamplingRatio { get; set; } = 0.1;
}

/// <summary>
/// Security configuration settings
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// JWT token settings
    /// </summary>
    public JwtSettings? Jwt { get; set; }

    /// <summary>
    /// Whether to require HTTPS
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Allowed origins for CORS
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new();
}

/// <summary>
/// JWT token configuration
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT signing key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromHours(24);
}