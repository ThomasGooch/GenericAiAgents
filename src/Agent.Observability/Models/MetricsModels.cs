using System.Diagnostics;

namespace Agent.Observability.Models;

/// <summary>
/// Agent performance metrics
/// </summary>
public class AgentPerformanceMetrics
{
    /// <summary>
    /// Agent identifier
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Execution time for the operation
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// CPU usage percentage (0.0 to 1.0)
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Timestamp when metrics were collected
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional custom metrics
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Workflow execution metrics
/// </summary>
public class WorkflowMetrics
{
    /// <summary>
    /// Workflow identifier
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Total number of steps in workflow
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Number of completed steps
    /// </summary>
    public int CompletedSteps { get; set; }

    /// <summary>
    /// Number of failed steps
    /// </summary>
    public int FailedSteps { get; set; }

    /// <summary>
    /// Total execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Whether workflow completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Workflow start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Workflow completion time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Average step execution time
    /// </summary>
    public TimeSpan AverageStepTime => CompletedSteps > 0 ? TimeSpan.FromTicks(ExecutionTime.Ticks / CompletedSteps) : TimeSpan.Zero;
}

/// <summary>
/// System-wide metrics summary
/// </summary>
public class MetricsSummary
{
    /// <summary>
    /// Total number of requests processed
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Number of failed requests
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Number of active connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Current memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Current CPU usage percentage
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate => TotalRequests > 0 ? (double)(TotalRequests - FailedRequests) / TotalRequests * 100 : 0;

    /// <summary>
    /// When this summary was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional system metrics
    /// </summary>
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Health check result
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Whether the health check passed
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Health check message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to perform the health check
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Additional health check data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Exception that occurred during health check (if any)
    /// </summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// System health status
/// </summary>
public class SystemHealthStatus
{
    /// <summary>
    /// Overall system health
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Overall system health level
    /// </summary>
    public SystemHealthLevel OverallStatus { get; set; }

    /// <summary>
    /// Health status of individual components
    /// </summary>
    public Dictionary<string, ComponentHealthStatus> ComponentHealth { get; set; } = new();

    /// <summary>
    /// When the health check was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total time taken for all health checks
    /// </summary>
    public TimeSpan TotalCheckTime { get; set; }

    /// <summary>
    /// Additional system information
    /// </summary>
    public Dictionary<string, object> SystemInfo { get; set; } = new();
}

/// <summary>
/// Individual component health status
/// </summary>
public class ComponentHealthStatus
{
    /// <summary>
    /// Component name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether component is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Health status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Response time for health check
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Component-specific details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Exception that occurred (if any)
    /// </summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// System health levels
/// </summary>
public enum SystemHealthLevel
{
    /// <summary>
    /// All components are healthy
    /// </summary>
    Healthy,

    /// <summary>
    /// Some components have warnings but system is operational
    /// </summary>
    Warning,

    /// <summary>
    /// Some components are degraded but core functionality works
    /// </summary>
    Degraded,

    /// <summary>
    /// Critical components are failing
    /// </summary>
    Critical
}

/// <summary>
/// Comprehensive health report
/// </summary>
public class HealthReport
{
    /// <summary>
    /// System health status
    /// </summary>
    public SystemHealthStatus SystemHealth { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public MetricsSummary Metrics { get; set; } = new();

    /// <summary>
    /// When the report was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Report generation duration
    /// </summary>
    public TimeSpan GenerationTime { get; set; }

    /// <summary>
    /// Additional report details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Metrics export formats
/// </summary>
public enum MetricsFormat
{
    /// <summary>
    /// Prometheus text format
    /// </summary>
    Prometheus,

    /// <summary>
    /// JSON format
    /// </summary>
    Json,

    /// <summary>
    /// OpenTelemetry format
    /// </summary>
    OpenTelemetry,

    /// <summary>
    /// Plain text format
    /// </summary>
    Text
}

/// <summary>
/// Activity tracking for distributed tracing
/// </summary>
public class ActivitySpan : IDisposable
{
    private readonly Activity _activity;
    private bool _disposed = false;

    public ActivitySpan(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>
    /// Operation name
    /// </summary>
    public string OperationName => _activity.OperationName;

    /// <summary>
    /// Activity duration
    /// </summary>
    public TimeSpan Duration => _activity.Duration;

    /// <summary>
    /// Activity tags
    /// </summary>
    public IEnumerable<KeyValuePair<string, object?>> Tags => _activity.Tags.Select(t => new KeyValuePair<string, object?>(t.Key, t.Value));

    /// <summary>
    /// Add tag to activity
    /// </summary>
    public void AddTag(string key, object? value)
    {
        _activity.SetTag(key, value?.ToString());
    }

    /// <summary>
    /// Mark activity as failed
    /// </summary>
    public void SetError(string error, Exception? exception = null)
    {
        _activity.SetStatus(ActivityStatusCode.Error, error);
        if (exception != null)
        {
            _activity.SetTag("exception.type", exception.GetType().Name);
            _activity.SetTag("exception.message", exception.Message);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _activity?.Dispose();
            _disposed = true;
        }
    }
}