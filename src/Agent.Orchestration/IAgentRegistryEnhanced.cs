using Agent.Core;
using Agent.Orchestration.Models;
using System.Reflection;

namespace Agent.Orchestration;

/// <summary>
/// Enhanced agent registry with service discovery and health checking capabilities
/// </summary>
public interface IAgentRegistryEnhanced
{
    /// <summary>
    /// Registers an agent with the registry
    /// </summary>
    /// <param name="agent">The agent to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterAgentAsync(IAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an agent by ID
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent or null if not found</returns>
    Task<IAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered agents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all registered agents</returns>
    Task<IEnumerable<IAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an agent is registered
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the agent is registered</returns>
    Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an agent
    /// </summary>
    /// <param name="agentId">The agent ID to unregister</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the agent was removed</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health status of a specific agent
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status or null if agent not found</returns>
    Task<AgentHealthStatus?> CheckHealthAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all healthy agents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of healthy agents</returns>
    Task<IEnumerable<IAgent>> GetHealthyAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agents filtered by health level
    /// </summary>
    /// <param name="healthLevel">Minimum health level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of agents meeting health criteria</returns>
    Task<IEnumerable<IAgent>> GetAgentsByHealthAsync(HealthLevel healthLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and registers agents from assemblies
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for agents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DiscoverAgentsAsync(IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and registers agents from the current app domain
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DiscoverAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors agent health with periodic checks
    /// </summary>
    /// <param name="interval">Health check interval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartHealthMonitoringAsync(TimeSpan interval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops health monitoring
    /// </summary>
    Task StopHealthMonitoringAsync();

    /// <summary>
    /// Gets comprehensive health report for all agents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health report with all agent statuses</returns>
    Task<AgentHealthReport> GetHealthReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when an agent's health status changes
    /// </summary>
    event EventHandler<AgentHealthChangedEventArgs>? AgentHealthChanged;

    /// <summary>
    /// Event fired when an agent is registered
    /// </summary>
    event EventHandler<AgentRegisteredEventArgs>? AgentRegistered;

    /// <summary>
    /// Event fired when an agent is unregistered
    /// </summary>
    event EventHandler<AgentUnregisteredEventArgs>? AgentUnregistered;
}

/// <summary>
/// Comprehensive health report for all agents
/// </summary>
public class AgentHealthReport
{
    /// <summary>
    /// Timestamp of the report
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Health status for each agent
    /// </summary>
    public Dictionary<string, AgentHealthStatus> AgentHealth { get; set; } = new();

    /// <summary>
    /// Overall system health summary
    /// </summary>
    public SystemHealthSummary Summary { get; set; } = new();
}

/// <summary>
/// Summary of overall system health
/// </summary>
public class SystemHealthSummary
{
    /// <summary>
    /// Total number of registered agents
    /// </summary>
    public int TotalAgents { get; set; }

    /// <summary>
    /// Number of healthy agents
    /// </summary>
    public int HealthyAgents { get; set; }

    /// <summary>
    /// Number of unhealthy agents
    /// </summary>
    public int UnhealthyAgents { get; set; }

    /// <summary>
    /// Number of agents with unknown health
    /// </summary>
    public int UnknownHealthAgents { get; set; }

    /// <summary>
    /// Overall system health percentage
    /// </summary>
    public double HealthPercentage { get; set; }

    /// <summary>
    /// Overall system status
    /// </summary>
    public HealthLevel OverallStatus { get; set; }
}

/// <summary>
/// Event args for agent health changes
/// </summary>
public class AgentHealthChangedEventArgs : EventArgs
{
    public string AgentId { get; set; } = string.Empty;
    public AgentHealthStatus PreviousHealth { get; set; } = new();
    public AgentHealthStatus CurrentHealth { get; set; } = new();
}

/// <summary>
/// Event args for agent registration
/// </summary>
public class AgentRegisteredEventArgs : EventArgs
{
    public string AgentId { get; set; } = string.Empty;
    public IAgent Agent { get; set; } = null!;
}

/// <summary>
/// Event args for agent unregistration
/// </summary>
public class AgentUnregisteredEventArgs : EventArgs
{
    public string AgentId { get; set; } = string.Empty;
}