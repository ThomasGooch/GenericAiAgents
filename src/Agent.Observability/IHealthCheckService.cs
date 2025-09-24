using Agent.Observability.Models;

namespace Agent.Observability;

/// <summary>
/// Interface for system health monitoring and reporting
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Check overall system health
    /// </summary>
    /// <param name="timeout">Timeout for health checks</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System health status</returns>
    Task<SystemHealthStatus> CheckSystemHealthAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check health of a specific component
    /// </summary>
    /// <param name="componentName">Name of component to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Component health status</returns>
    Task<ComponentHealthStatus> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get comprehensive health report
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete health report</returns>
    Task<HealthReport> GetHealthReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a custom health check
    /// </summary>
    /// <param name="name">Health check name</param>
    /// <param name="healthCheck">Health check implementation</param>
    void RegisterHealthCheck(string name, IHealthCheck healthCheck);

    /// <summary>
    /// Unregister a health check
    /// </summary>
    /// <param name="name">Health check name to remove</param>
    /// <returns>True if health check was removed</returns>
    bool UnregisterHealthCheck(string name);

    /// <summary>
    /// Start periodic health checks
    /// </summary>
    /// <param name="interval">Check interval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartPeriodicHealthChecksAsync(TimeSpan interval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop periodic health checks
    /// </summary>
    Task StopPeriodicHealthChecksAsync();

    /// <summary>
    /// Event fired when system health changes
    /// </summary>
    event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;

    /// <summary>
    /// Event fired when a health check is performed
    /// </summary>
    event EventHandler<HealthCheckCompletedEventArgs>? HealthCheckCompleted;
}

/// <summary>
/// Interface for individual health checks
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Perform the health check
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Name of the health check
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what this health check validates
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Timeout for this health check
    /// </summary>
    TimeSpan Timeout { get; }
}

/// <summary>
/// Event arguments for health status changes
/// </summary>
public class HealthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous health status
    /// </summary>
    public SystemHealthStatus PreviousStatus { get; set; } = new();

    /// <summary>
    /// Current health status
    /// </summary>
    public SystemHealthStatus CurrentStatus { get; set; } = new();

    /// <summary>
    /// When the status change occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Components that changed status
    /// </summary>
    public List<string> ChangedComponents { get; set; } = new();
}

/// <summary>
/// Event arguments for completed health checks
/// </summary>
public class HealthCheckCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Name of the health check that completed
    /// </summary>
    public string HealthCheckName { get; set; } = string.Empty;

    /// <summary>
    /// Result of the health check
    /// </summary>
    public HealthCheckResult Result { get; set; } = new();

    /// <summary>
    /// When the health check was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Base implementation for custom health checks
/// </summary>
public abstract class BaseHealthCheck : IHealthCheck
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual TimeSpan Timeout { get; } = TimeSpan.FromSeconds(10);

    public abstract Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    protected HealthCheckResult Healthy(string message = "OK", Dictionary<string, object>? data = null)
    {
        return new HealthCheckResult
        {
            IsHealthy = true,
            Message = message,
            Data = data ?? new Dictionary<string, object>()
        };
    }

    protected HealthCheckResult Unhealthy(string message, Exception? exception = null, Dictionary<string, object>? data = null)
    {
        return new HealthCheckResult
        {
            IsHealthy = false,
            Message = message,
            Exception = exception,
            Data = data ?? new Dictionary<string, object>()
        };
    }
}