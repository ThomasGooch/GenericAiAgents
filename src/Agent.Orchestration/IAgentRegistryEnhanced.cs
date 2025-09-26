using Agent.Core;
using Agent.Orchestration.Models;
using System.Reflection;

namespace Agent.Orchestration;

/// <summary>
/// Advanced enterprise agent registry that provides comprehensive agent lifecycle management, 
/// service discovery, health monitoring, and operational intelligence for distributed agent systems.
/// 
/// This enhanced registry extends basic agent management with enterprise-grade features including
/// automatic service discovery, continuous health monitoring, load balancing capabilities, and
/// comprehensive operational visibility. It serves as the central nervous system for agent
/// orchestration in production environments.
/// 
/// Key Enterprise Features:
/// - **Service Discovery**: Automatic detection and registration of agents from assemblies and runtime
/// - **Health Monitoring**: Continuous health checking with configurable intervals and alerting
/// - **Load Balancing**: Intelligent agent selection based on health status and performance metrics
/// - **Event-Driven Architecture**: Real-time notifications for agent state changes and system events
/// - **Operational Intelligence**: Comprehensive health reporting and system analytics
/// - **Graceful Degradation**: Fallback mechanisms and circuit breaker patterns for resilience
/// - **Dynamic Scaling**: Support for dynamic agent registration and deregistration
/// - **Centralized Management**: Single point of control for distributed agent ecosystem
/// 
/// Production Deployment Features:
/// - **High Availability**: Fault-tolerant design with automatic failover capabilities
/// - **Performance Monitoring**: Real-time metrics collection and performance analytics
/// - **Security Integration**: Role-based access control and secure agent communication
/// - **Compliance Support**: Comprehensive audit logging and change tracking
/// - **Resource Management**: Efficient memory usage and connection pooling
/// - **Distributed Architecture**: Support for multi-node and cloud-native deployments
/// 
/// Health Monitoring Capabilities:
/// - **Proactive Monitoring**: Continuous background health checks with configurable intervals
/// - **Multi-Level Health Assessment**: Component-level, agent-level, and system-level health tracking
/// - **Predictive Analytics**: Early warning systems for potential failures and performance degradation
/// - **Custom Health Checks**: Extensible health check framework for domain-specific requirements
/// - **Health History**: Historical health data for trend analysis and capacity planning
/// 
/// Service Discovery Patterns:
/// - **Assembly Scanning**: Automatic discovery of agents through reflection and attribute-based metadata
/// - **Runtime Registration**: Dynamic agent registration and deregistration during application lifecycle
/// - **Dependency Resolution**: Automatic resolution of agent dependencies and service requirements
/// - **Version Management**: Support for multiple agent versions and upgrade scenarios
/// - **Configuration Integration**: Integration with configuration management for environment-specific settings
/// 
/// Integration Capabilities:
/// - **Workflow Engine Integration**: Seamless integration with workflow orchestration systems
/// - **Monitoring Platform Integration**: Export health and performance data to external monitoring systems
/// - **Load Balancer Integration**: Intelligent routing based on agent health and performance characteristics
/// - **Service Mesh Compatibility**: Integration with modern service mesh architectures
/// </summary>
/// <example>
/// Basic agent registry usage with health monitoring:
/// <code>
/// // Initialize enhanced registry with dependency injection
/// var services = new ServiceCollection();
/// services.AddSingleton&lt;IAgentRegistryEnhanced, AgentRegistryEnhanced&gt;();
/// services.AddLogging();
/// var serviceProvider = services.BuildServiceProvider();
/// 
/// var registry = serviceProvider.GetService&lt;IAgentRegistryEnhanced&gt;();
/// 
/// // Register agents manually
/// await registry.RegisterAgentAsync(new DataProcessingAgent(), cancellationToken);
/// await registry.RegisterAgentAsync(new AnalyticsAgent(), cancellationToken);
/// await registry.RegisterAgentAsync(new NotificationAgent(), cancellationToken);
/// 
/// // Start continuous health monitoring
/// await registry.StartHealthMonitoringAsync(TimeSpan.FromMinutes(1), cancellationToken);
/// 
/// // Subscribe to health change events
/// registry.AgentHealthChanged += (sender, args) =&gt;
/// {
///     Console.WriteLine($"Agent {args.AgentId} health changed from {args.PreviousHealth.Status} to {args.CurrentHealth.Status}");
///     
///     if (args.CurrentHealth.Status == HealthLevel.Critical)
///     {
///         // Trigger emergency response procedures
///         await NotifyOperationsTeam(args.AgentId, args.CurrentHealth);
///     }
/// };
/// 
/// // Get healthy agents for workflow execution
/// var healthyAgents = await registry.GetHealthyAgentsAsync(cancellationToken);
/// Console.WriteLine($"Available healthy agents: {healthyAgents.Count()}");
/// </code>
/// 
/// Advanced service discovery with automatic agent registration:
/// <code>
/// // Discover and register agents from loaded assemblies
/// await registry.DiscoverAgentsAsync(cancellationToken);
/// 
/// // Discover agents from specific assemblies
/// var targetAssemblies = new[]
/// {
///     typeof(DataProcessingAgent).Assembly,
///     typeof(MLInferenceAgent).Assembly,
///     Assembly.LoadFrom("CustomAgents.dll")
/// };
/// 
/// await registry.DiscoverAgentsAsync(targetAssemblies, cancellationToken);
/// 
/// // Verify discovered agents
/// var allAgents = await registry.GetAllAgentsAsync(cancellationToken);
/// foreach (var agent in allAgents)
/// {
///     Console.WriteLine($"Discovered agent: {agent.Id} - {agent.GetType().Name}");
///     
///     // Check initial health status
///     var health = await registry.CheckHealthAsync(agent.Id, cancellationToken);
///     Console.WriteLine($"  Health: {health?.Status} - {health?.Message}");
/// }
/// </code>
/// 
/// Enterprise health monitoring with comprehensive reporting:
/// <code>
/// // Generate comprehensive health report
/// var healthReport = await registry.GetHealthReportAsync(cancellationToken);
/// 
/// Console.WriteLine($"System Health Report - {healthReport.Timestamp}");
/// Console.WriteLine($"Total Agents: {healthReport.Summary.TotalAgents}");
/// Console.WriteLine($"Healthy: {healthReport.Summary.HealthyAgents}");
/// Console.WriteLine($"Unhealthy: {healthReport.Summary.UnhealthyAgents}");
/// Console.WriteLine($"Overall Health: {healthReport.Summary.HealthPercentage:F1}%");
/// Console.WriteLine($"System Status: {healthReport.Summary.OverallStatus}");
/// 
/// // Detailed per-agent health information
/// foreach (var agentHealth in healthReport.AgentHealth)
/// {
///     var agent = agentHealth.Key;
///     var health = agentHealth.Value;
///     
///     Console.WriteLine($"\nAgent: {agent}");
///     Console.WriteLine($"  Status: {health.Status}");
///     Console.WriteLine($"  Last Check: {health.LastChecked}");
///     Console.WriteLine($"  Response Time: {health.ResponseTime}ms");
///     
///     if (health.Status != HealthLevel.Healthy)
///     {
///         Console.WriteLine($"  Issues: {health.Message}");
///         
///         // Log unhealthy agents for investigation
///         logger.LogWarning("Agent {AgentId} is unhealthy: {Issues}", agent, health.Message);
///     }
/// }
/// </code>
/// 
/// Production health monitoring with alerting integration:
/// <code>
/// public class ProductionAgentMonitoringService
/// {
///     private readonly IAgentRegistryEnhanced _registry;
///     private readonly IAlertingService _alerting;
///     private readonly IMetricsCollector _metrics;
///     
///     public async Task InitializeMonitoringAsync()
///     {
///         // Start health monitoring with production intervals
///         await _registry.StartHealthMonitoringAsync(TimeSpan.FromSeconds(30));
///         
///         // Subscribe to health changes for alerting
///         _registry.AgentHealthChanged += async (sender, args) =&gt;
///         {
///             // Record health change metrics
///             _metrics.IncrementCounter("agent.health.changes", 1, new Dictionary&lt;string, object&gt;
///             {
///                 ["agent_id"] = args.AgentId,
///                 ["previous_status"] = args.PreviousHealth.Status.ToString(),
///                 ["current_status"] = args.CurrentHealth.Status.ToString()
///             });
///             
///             // Trigger alerts for critical issues
///             if (args.CurrentHealth.Status == HealthLevel.Critical)
///             {
///                 await _alerting.SendCriticalAlert(
///                     $"Agent {args.AgentId} is in critical state",
///                     $"Health declined from {args.PreviousHealth.Status} to {args.CurrentHealth.Status}. " +
///                     $"Error: {args.CurrentHealth.Message}"
///                 );
///             }
///             
///             // Update load balancer if agent becomes unhealthy
///             if (args.CurrentHealth.Status == HealthLevel.Unhealthy ||
///                 args.CurrentHealth.Status == HealthLevel.Critical)
///             {
///                 await RemoveFromLoadBalancer(args.AgentId);
///             }
///             else if (args.PreviousHealth.Status != HealthLevel.Healthy &amp;&amp;
///                      args.CurrentHealth.Status == HealthLevel.Healthy)
///             {
///                 await AddToLoadBalancer(args.AgentId);
///             }
///         };
///     }
/// }
/// </code>
/// 
/// High-availability agent selection for critical operations:
/// <code>
/// public class HighAvailabilityAgentSelector
/// {
///     public async Task&lt;IAgent?&gt; SelectBestAvailableAgentAsync(string agentType, CancellationToken cancellationToken)
///     {
///         // Get all healthy agents of the required type
///         var healthyAgents = await _registry.GetHealthyAgentsAsync(cancellationToken);
///         var candidateAgents = healthyAgents.Where(a =&gt; a.GetType().Name == agentType).ToList();
///         
///         if (!candidateAgents.Any())
///         {
///             // Fall back to degraded agents if no healthy ones available
///             var degradedAgents = await _registry.GetAgentsByHealthAsync(HealthLevel.Degraded, cancellationToken);
///             candidateAgents = degradedAgents.Where(a =&gt; a.GetType().Name == agentType).ToList();
///             
///             if (!candidateAgents.Any())
///             {
///                 throw new InvalidOperationException($"No available agents of type {agentType}");
///             }
///         }
///         
///         // Select agent with best health metrics
///         IAgent? bestAgent = null;
///         var bestResponseTime = TimeSpan.MaxValue;
///         
///         foreach (var agent in candidateAgents)
///         {
///             var health = await _registry.CheckHealthAsync(agent.Id, cancellationToken);
///             if (health != null &amp;&amp; health.ResponseTime &amp;lt; bestResponseTime)
///             {
///                 bestAgent = agent;
///                 bestResponseTime = health.ResponseTime;
///             }
///         }
///         
///         return bestAgent;
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// Thread Safety and Concurrency:
/// - All registry operations are thread-safe and support concurrent access
/// - Health monitoring runs on background threads without blocking registry operations
/// - Event notifications are delivered asynchronously to prevent performance impact
/// - Registry state is protected by appropriate synchronization mechanisms
/// 
/// Performance Considerations:
/// - Health checks are performed asynchronously to avoid blocking registry operations
/// - Agent lookup operations are optimized with efficient data structures
/// - Memory usage is managed through configurable cleanup and garbage collection policies
/// - Background monitoring threads are managed efficiently to minimize resource usage
/// 
/// Scalability and High Availability:
/// - Registry supports horizontal scaling through partitioning and replication strategies
/// - Health monitoring adapts to system load and adjusts check frequencies automatically
/// - Circuit breaker patterns prevent cascade failures during system stress
/// - Graceful degradation ensures system continues operating even with partial agent failures
/// 
/// Monitoring and Observability:
/// - Comprehensive metrics collection for registry operations and health status
/// - Integration with external monitoring systems through standardized interfaces
/// - Detailed logging for troubleshooting and audit purposes
/// - Health trend analysis and predictive failure detection capabilities
/// 
/// Security Considerations:
/// - Agent registration includes validation and authorization checks
/// - Health check communications use secure channels and authentication
/// - Event notifications include appropriate security context and filtering
/// - Registry access is controlled through role-based authorization policies
/// 
/// Production Deployment:
/// - Support for multiple deployment topologies including clustered and distributed configurations
/// - Integration with container orchestration platforms (Kubernetes, Docker Swarm)
/// - Configuration management integration for environment-specific settings
/// - Comprehensive health endpoints for external monitoring and load balancing
/// </remarks>
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