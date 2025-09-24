using Agent.Core;
using Agent.Orchestration.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace Agent.Orchestration;

/// <summary>
/// Enhanced agent registry with service discovery and health checking capabilities
/// </summary>
public class AgentRegistryEnhanced : IAgentRegistryEnhanced, IDisposable
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();
    private readonly ConcurrentDictionary<string, AgentHealthStatus> _healthCache = new();
    private readonly Timer? _healthCheckTimer;
    private readonly object _healthMonitorLock = new();
    private bool _healthMonitoringActive = false;
    private bool _disposed = false;

    public event EventHandler<AgentHealthChangedEventArgs>? AgentHealthChanged;
    public event EventHandler<AgentRegisteredEventArgs>? AgentRegistered;
    public event EventHandler<AgentUnregisteredEventArgs>? AgentUnregistered;

    public AgentRegistryEnhanced()
    {
        // Initialize with a disabled timer that can be started later
        _healthCheckTimer = new Timer(PerformHealthChecksCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task RegisterAgentAsync(IAgent agent, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        _agents[agent.Id] = agent;

        // Fire registration event
        AgentRegistered?.Invoke(this, new AgentRegisteredEventArgs
        {
            AgentId = agent.Id,
            Agent = agent
        });

        return Task.CompletedTask;
    }

    public Task<IAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<IEnumerable<IAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IAgent>>(_agents.Values.ToList());
    }

    public Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_agents.ContainsKey(agentId));
    }

    public Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        var removed = _agents.TryRemove(agentId, out _);

        if (removed)
        {
            _healthCache.TryRemove(agentId, out _);

            // Fire unregistration event
            AgentUnregistered?.Invoke(this, new AgentUnregisteredEventArgs
            {
                AgentId = agentId
            });
        }

        return Task.FromResult(removed);
    }

    public async Task<AgentHealthStatus?> CheckHealthAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentId, out var agent))
        {
            return null;
        }

        try
        {
            var health = await agent.CheckHealthAsync(cancellationToken);

            // Update cache and check for changes
            if (_healthCache.TryGetValue(agentId, out var previousHealth))
            {
                if (previousHealth.IsHealthy != health.IsHealthy)
                {
                    AgentHealthChanged?.Invoke(this, new AgentHealthChangedEventArgs
                    {
                        AgentId = agentId,
                        PreviousHealth = previousHealth,
                        CurrentHealth = health
                    });
                }
            }

            _healthCache[agentId] = health;
            return health;
        }
        catch (Exception ex)
        {
            var health = new AgentHealthStatus
            {
                IsHealthy = false,
                Message = $"Health check failed: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };

            _healthCache[agentId] = health;
            return health;
        }
    }

    public async Task<IEnumerable<IAgent>> GetHealthyAgentsAsync(CancellationToken cancellationToken = default)
    {
        var healthyAgents = new List<IAgent>();

        foreach (var kvp in _agents)
        {
            var health = await CheckHealthAsync(kvp.Key, cancellationToken);
            if (health?.IsHealthy == true)
            {
                healthyAgents.Add(kvp.Value);
            }
        }

        return healthyAgents;
    }

    public async Task<IEnumerable<IAgent>> GetAgentsByHealthAsync(HealthLevel healthLevel, CancellationToken cancellationToken = default)
    {
        var filteredAgents = new List<IAgent>();

        foreach (var kvp in _agents)
        {
            var health = await CheckHealthAsync(kvp.Key, cancellationToken);

            if (health != null)
            {
                var agentHealthLevel = DetermineHealthLevel(health);

                // Include agents at or above the requested health level
                if (agentHealthLevel >= healthLevel)
                {
                    filteredAgents.Add(kvp.Value);
                }
            }
        }

        return filteredAgents;
    }

    public async Task DiscoverAgentsAsync(IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        foreach (var assembly in assemblies)
        {
            var agentTypes = assembly.GetTypes()
                .Where(type => typeof(IAgent).IsAssignableFrom(type) &&
                              !type.IsInterface &&
                              !type.IsAbstract)
                .ToList();

            foreach (var agentType in agentTypes)
            {
                try
                {
                    // Try to create instance - requires parameterless constructor for discovery
                    if (agentType.GetConstructor(Type.EmptyTypes) != null)
                    {
                        var agent = (IAgent)Activator.CreateInstance(agentType)!;
                        await RegisterAgentAsync(agent, cancellationToken);
                    }
                }
                catch (Exception)
                {
                    // Skip agents that can't be instantiated automatically
                    continue;
                }
            }
        }
    }

    public Task DiscoverAgentsAsync(CancellationToken cancellationToken = default)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        return DiscoverAgentsAsync(assemblies, cancellationToken);
    }

    public Task StartHealthMonitoringAsync(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        lock (_healthMonitorLock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AgentRegistryEnhanced));

            if (_healthMonitoringActive)
                return Task.CompletedTask;

            _healthCheckTimer?.Change(interval, interval);
            _healthMonitoringActive = true;
        }

        return Task.CompletedTask;
    }

    public Task StopHealthMonitoringAsync()
    {
        lock (_healthMonitorLock)
        {
            if (!_healthMonitoringActive)
                return Task.CompletedTask;

            _healthCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _healthMonitoringActive = false;
        }

        return Task.CompletedTask;
    }

    public async Task<AgentHealthReport> GetHealthReportAsync(CancellationToken cancellationToken = default)
    {
        var report = new AgentHealthReport
        {
            Timestamp = DateTime.UtcNow
        };

        var healthTasks = _agents.ToDictionary(
            kvp => kvp.Key,
            kvp => CheckHealthAsync(kvp.Key, cancellationToken)
        );

        await Task.WhenAll(healthTasks.Values);

        foreach (var kvp in healthTasks)
        {
            var health = await kvp.Value;
            if (health != null)
            {
                report.AgentHealth[kvp.Key] = health;
            }
        }

        // Calculate summary
        report.Summary = CalculateHealthSummary(report.AgentHealth);

        return report;
    }

    private async void PerformHealthChecksCallback(object? state)
    {
        if (_disposed || !_healthMonitoringActive)
            return;

        try
        {
            var healthCheckTasks = _agents.Keys.Select(agentId =>
                CheckHealthAsync(agentId, CancellationToken.None));

            await Task.WhenAll(healthCheckTasks);
        }
        catch (Exception)
        {
            // Log error in real implementation
        }
    }

    private static HealthLevel DetermineHealthLevel(AgentHealthStatus health)
    {
        if (health.IsHealthy)
        {
            return health.ResponseTime.TotalSeconds > 5 ? HealthLevel.Warning : HealthLevel.Healthy;
        }

        return health.Message.Contains("disposed", StringComparison.OrdinalIgnoreCase)
            ? HealthLevel.Critical
            : HealthLevel.Unhealthy;
    }

    private static SystemHealthSummary CalculateHealthSummary(Dictionary<string, AgentHealthStatus> agentHealth)
    {
        var summary = new SystemHealthSummary
        {
            TotalAgents = agentHealth.Count
        };

        foreach (var health in agentHealth.Values)
        {
            if (health.IsHealthy)
                summary.HealthyAgents++;
            else
                summary.UnhealthyAgents++;
        }

        summary.UnknownHealthAgents = summary.TotalAgents - summary.HealthyAgents - summary.UnhealthyAgents;
        summary.HealthPercentage = summary.TotalAgents > 0
            ? (double)summary.HealthyAgents / summary.TotalAgents * 100
            : 0;

        summary.OverallStatus = summary.HealthPercentage switch
        {
            >= 95 => HealthLevel.Healthy,
            >= 80 => HealthLevel.Warning,
            >= 60 => HealthLevel.Degraded,
            > 0 => HealthLevel.Unhealthy,
            _ => HealthLevel.Critical
        };

        return summary;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}