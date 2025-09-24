using Agent.Core;
using Agent.Observability.Models;
using Agent.Orchestration;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Agent.Observability;

/// <summary>
/// Default implementation of health check service
/// </summary>
public class HealthCheckService : IHealthCheckService, IDisposable
{
    private readonly IAgentRegistryEnhanced _agentRegistry;
    private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks = new();
    private readonly Timer? _periodicTimer;
    private readonly object _timerLock = new();
    private SystemHealthStatus? _lastHealthStatus;
    private bool _disposed = false;

    public event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;
    public event EventHandler<HealthCheckCompletedEventArgs>? HealthCheckCompleted;

    public HealthCheckService(IAgentRegistryEnhanced agentRegistry)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        
        // Register built-in health checks
        RegisterBuiltInHealthChecks();
        
        // Initialize timer (disabled by default)
        _periodicTimer = new Timer(PeriodicHealthCheckCallback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task<SystemHealthStatus> CheckSystemHealthAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        var systemHealth = new SystemHealthStatus
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Check agents
            await CheckAgentHealthAsync(systemHealth, cts.Token);
            
            // Check custom health checks
            await CheckCustomHealthChecksAsync(systemHealth, cts.Token);
            
            // Check system resources
            await CheckSystemResourcesAsync(systemHealth, cts.Token);

            // Determine overall health
            systemHealth.IsHealthy = systemHealth.ComponentHealth.Values.All(c => c.IsHealthy);
            systemHealth.OverallStatus = DetermineOverallHealthLevel(systemHealth.ComponentHealth);
        }
        catch (OperationCanceledException)
        {
            systemHealth.IsHealthy = false;
            systemHealth.OverallStatus = SystemHealthLevel.Critical;
            systemHealth.ComponentHealth["system"] = new ComponentHealthStatus
            {
                Name = "system",
                IsHealthy = false,
                Message = "Health check timed out",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            systemHealth.IsHealthy = false;
            systemHealth.OverallStatus = SystemHealthLevel.Critical;
            systemHealth.ComponentHealth["system"] = new ComponentHealthStatus
            {
                Name = "system",
                IsHealthy = false,
                Message = $"Health check failed: {ex.Message}",
                Exception = ex,
                ResponseTime = stopwatch.Elapsed
            };
        }
        finally
        {
            stopwatch.Stop();
            systemHealth.TotalCheckTime = stopwatch.Elapsed;
        }

        // Check for health status changes and fire event
        await CheckForHealthStatusChangesAsync(systemHealth);

        return systemHealth;
    }

    public async Task<ComponentHealthStatus> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Check if it's a registered health check
            if (_healthChecks.TryGetValue(componentName, out var healthCheck))
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(healthCheck.Timeout);

                var result = await healthCheck.CheckHealthAsync(cts.Token);
                
                return new ComponentHealthStatus
                {
                    Name = componentName,
                    IsHealthy = result.IsHealthy,
                    Message = result.Message,
                    ResponseTime = stopwatch.Elapsed,
                    Details = result.Data,
                    Exception = result.Exception
                };
            }

            // Check built-in components
            return componentName.ToLowerInvariant() switch
            {
                "database" => await CheckDatabaseHealthAsync(cancellationToken),
                "memory" => CheckMemoryHealth(),
                "disk" => CheckDiskHealth(),
                "cpu" => CheckCpuHealth(),
                _ => new ComponentHealthStatus
                {
                    Name = componentName,
                    IsHealthy = false,
                    Message = $"Unknown component: {componentName}",
                    ResponseTime = stopwatch.Elapsed
                }
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealthStatus
            {
                Name = componentName,
                IsHealthy = false,
                Message = $"Health check failed: {ex.Message}",
                Exception = ex,
                ResponseTime = stopwatch.Elapsed
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<HealthReport> GetHealthReportAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var report = new HealthReport
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Get system health
            report.SystemHealth = await CheckSystemHealthAsync(null, cancellationToken);
            
            // Get metrics (if available)
            // This would typically be injected, but for simplicity we'll create basic metrics
            report.Metrics = new MetricsSummary
            {
                Timestamp = DateTime.UtcNow,
                // Add basic system metrics
                MemoryUsage = GC.GetTotalMemory(false),
                CpuUsage = GetCpuUsage()
            };

            // Add additional details
            report.Details["agent_count"] = (await _agentRegistry.GetAllAgentsAsync()).Count();
            report.Details["healthy_agents"] = (await _agentRegistry.GetHealthyAgentsAsync()).Count();
            report.Details["check_duration"] = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            report.Details["error"] = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            report.GenerationTime = stopwatch.Elapsed;
        }

        return report;
    }

    public void RegisterHealthCheck(string name, IHealthCheck healthCheck)
    {
        _healthChecks[name] = healthCheck;
    }

    public bool UnregisterHealthCheck(string name)
    {
        return _healthChecks.TryRemove(name, out _);
    }

    public async Task StartPeriodicHealthChecksAsync(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        lock (_timerLock)
        {
            _periodicTimer?.Change(interval, interval);
        }

        await Task.CompletedTask;
    }

    public async Task StopPeriodicHealthChecksAsync()
    {
        lock (_timerLock)
        {
            _periodicTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        await Task.CompletedTask;
    }

    private async void PeriodicHealthCheckCallback(object? state)
    {
        try
        {
            await CheckSystemHealthAsync();
        }
        catch (Exception)
        {
            // Log error in real implementation
            // Silently ignore to prevent callback exceptions
        }
    }

    private void RegisterBuiltInHealthChecks()
    {
        // Register built-in health checks would go here
        // For now, we'll handle them in CheckComponentHealthAsync
    }

    private async Task CheckAgentHealthAsync(SystemHealthStatus systemHealth, CancellationToken cancellationToken)
    {
        try
        {
            var agents = await _agentRegistry.GetAllAgentsAsync(cancellationToken);
            
            foreach (var agent in agents)
            {
                try
                {
                    var agentHealth = await _agentRegistry.CheckHealthAsync(agent.Id, cancellationToken);
                    
                    systemHealth.ComponentHealth[agent.Id] = new ComponentHealthStatus
                    {
                        Name = agent.Id,
                        IsHealthy = agentHealth?.IsHealthy ?? false,
                        Message = agentHealth?.Message ?? "Unable to check agent health",
                        ResponseTime = agentHealth?.ResponseTime ?? TimeSpan.Zero,
                        Details = agentHealth?.Metrics ?? new Dictionary<string, object>()
                    };
                }
                catch (Exception ex)
                {
                    systemHealth.ComponentHealth[agent.Id] = new ComponentHealthStatus
                    {
                        Name = agent.Id,
                        IsHealthy = false,
                        Message = $"Agent health check failed: {ex.Message}",
                        Exception = ex
                    };
                }
            }
        }
        catch (Exception ex)
        {
            systemHealth.ComponentHealth["agent_registry"] = new ComponentHealthStatus
            {
                Name = "agent_registry",
                IsHealthy = false,
                Message = $"Failed to check agent registry: {ex.Message}",
                Exception = ex
            };
        }
    }

    private async Task CheckCustomHealthChecksAsync(SystemHealthStatus systemHealth, CancellationToken cancellationToken)
    {
        var healthCheckTasks = _healthChecks.Select(async kvp =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(kvp.Value.Timeout);

                var result = await kvp.Value.CheckHealthAsync(cts.Token);
                
                var componentHealth = new ComponentHealthStatus
                {
                    Name = kvp.Key,
                    IsHealthy = result.IsHealthy,
                    Message = result.Message,
                    ResponseTime = stopwatch.Elapsed,
                    Details = result.Data,
                    Exception = result.Exception
                };

                OnHealthCheckCompleted(new HealthCheckCompletedEventArgs
                {
                    HealthCheckName = kvp.Key,
                    Result = result
                });

                return (kvp.Key, componentHealth);
            }
            catch (Exception ex)
            {
                var componentHealth = new ComponentHealthStatus
                {
                    Name = kvp.Key,
                    IsHealthy = false,
                    Message = $"Health check failed: {ex.Message}",
                    Exception = ex,
                    ResponseTime = stopwatch.Elapsed
                };

                return (kvp.Key, componentHealth);
            }
            finally
            {
                stopwatch.Stop();
            }
        });

        var results = await Task.WhenAll(healthCheckTasks);
        
        foreach (var (name, health) in results)
        {
            systemHealth.ComponentHealth[name] = health;
        }
    }

    private async Task CheckSystemResourcesAsync(SystemHealthStatus systemHealth, CancellationToken cancellationToken)
    {
        // Check memory
        systemHealth.ComponentHealth["memory"] = CheckMemoryHealth();
        
        // Check disk space
        systemHealth.ComponentHealth["disk"] = CheckDiskHealth();
        
        // Check CPU usage
        systemHealth.ComponentHealth["cpu"] = CheckCpuHealth();

        await Task.CompletedTask;
    }

    private async Task<ComponentHealthStatus> CheckDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        // Placeholder for database health check
        // In a real implementation, this would test database connectivity
        await Task.Delay(10, cancellationToken);
        
        return new ComponentHealthStatus
        {
            Name = "database",
            IsHealthy = true,
            Message = "Database connection is healthy",
            ResponseTime = TimeSpan.FromMilliseconds(10),
            Details = new Dictionary<string, object>
            {
                ["connection_count"] = 5,
                ["last_query_time"] = DateTime.UtcNow.AddSeconds(-1)
            }
        };
    }

    private ComponentHealthStatus CheckMemoryHealth()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        var isHealthy = memoryUsage < 1024 * 1024 * 1024; // 1GB threshold
        
        return new ComponentHealthStatus
        {
            Name = "memory",
            IsHealthy = isHealthy,
            Message = isHealthy ? "Memory usage is healthy" : "High memory usage detected",
            Details = new Dictionary<string, object>
            {
                ["total_memory_bytes"] = memoryUsage,
                ["gc_generation_0"] = GC.CollectionCount(0),
                ["gc_generation_1"] = GC.CollectionCount(1),
                ["gc_generation_2"] = GC.CollectionCount(2)
            }
        };
    }

    private ComponentHealthStatus CheckDiskHealth()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var criticalDrives = new List<string>();
            
            foreach (var drive in drives)
            {
                var freeSpacePercentage = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                if (freeSpacePercentage < 10) // Less than 10% free space
                {
                    criticalDrives.Add(drive.Name);
                }
            }

            return new ComponentHealthStatus
            {
                Name = "disk",
                IsHealthy = !criticalDrives.Any(),
                Message = criticalDrives.Any() 
                    ? $"Low disk space on drives: {string.Join(", ", criticalDrives)}" 
                    : "Disk space is healthy",
                Details = drives.ToDictionary<DriveInfo, string, object>(d => d.Name, d => new
                {
                    total_bytes = d.TotalSize,
                    free_bytes = d.AvailableFreeSpace,
                    free_percentage = (double)d.AvailableFreeSpace / d.TotalSize * 100
                })
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealthStatus
            {
                Name = "disk",
                IsHealthy = false,
                Message = $"Failed to check disk health: {ex.Message}",
                Exception = ex
            };
        }
    }

    private ComponentHealthStatus CheckCpuHealth()
    {
        var cpuUsage = GetCpuUsage();
        var isHealthy = cpuUsage < 80; // 80% threshold
        
        return new ComponentHealthStatus
        {
            Name = "cpu",
            IsHealthy = isHealthy,
            Message = isHealthy ? "CPU usage is healthy" : "High CPU usage detected",
            Details = new Dictionary<string, object>
            {
                ["cpu_usage_percentage"] = cpuUsage,
                ["processor_count"] = Environment.ProcessorCount,
                ["thread_count"] = Process.GetCurrentProcess().Threads.Count
            }
        };
    }

    private double GetCpuUsage()
    {
        // Simplified CPU usage calculation
        // In a real implementation, this would use performance counters
        return Random.Shared.NextDouble() * 50; // Mock value between 0-50%
    }

    private SystemHealthLevel DetermineOverallHealthLevel(Dictionary<string, ComponentHealthStatus> componentHealth)
    {
        if (!componentHealth.Any())
            return SystemHealthLevel.Warning;

        var healthyCount = componentHealth.Count(c => c.Value.IsHealthy);
        var totalCount = componentHealth.Count;
        var healthyPercentage = (double)healthyCount / totalCount * 100;

        return healthyPercentage switch
        {
            100 => SystemHealthLevel.Healthy,
            >= 80 => SystemHealthLevel.Warning,
            >= 50 => SystemHealthLevel.Degraded,
            _ => SystemHealthLevel.Critical
        };
    }

    private async Task CheckForHealthStatusChangesAsync(SystemHealthStatus currentStatus)
    {
        if (_lastHealthStatus != null && 
            (_lastHealthStatus.IsHealthy != currentStatus.IsHealthy || 
             _lastHealthStatus.OverallStatus != currentStatus.OverallStatus))
        {
            var changedComponents = currentStatus.ComponentHealth.Keys
                .Where(key => !_lastHealthStatus.ComponentHealth.ContainsKey(key) ||
                             _lastHealthStatus.ComponentHealth[key].IsHealthy != currentStatus.ComponentHealth[key].IsHealthy)
                .ToList();

            OnHealthStatusChanged(new HealthStatusChangedEventArgs
            {
                PreviousStatus = _lastHealthStatus,
                CurrentStatus = currentStatus,
                ChangedComponents = changedComponents
            });
        }

        _lastHealthStatus = currentStatus;
        await Task.CompletedTask;
    }

    private void OnHealthStatusChanged(HealthStatusChangedEventArgs args)
    {
        HealthStatusChanged?.Invoke(this, args);
    }

    private void OnHealthCheckCompleted(HealthCheckCompletedEventArgs args)
    {
        HealthCheckCompleted?.Invoke(this, args);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _periodicTimer?.Dispose();
            _disposed = true;
        }
    }
}