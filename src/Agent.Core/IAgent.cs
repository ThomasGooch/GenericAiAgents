using Agent.Core.Models;

namespace Agent.Core;

public interface IAgent : IAsyncDisposable
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    AgentConfiguration Configuration { get; }
    bool IsInitialized { get; }

    Task InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default);
    Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<AgentResult> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<AgentHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Health status of an agent
/// </summary>
public class AgentHealthStatus
{
    /// <summary>
    /// Whether the agent is healthy and available
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Health status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the health check
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Response time for the health check
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Additional health metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();
}