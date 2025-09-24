using Agent.Core.Models;

namespace Agent.Core;

public abstract class BaseAgent : IAgent
{
    private bool _disposed = false;

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public AgentConfiguration Configuration { get; private set; } = new();
    public bool IsInitialized { get; private set; }

    protected BaseAgent(string name, string description)
    {
        Id = name?.ToLowerInvariant().Replace(" ", "-") ?? Guid.NewGuid().ToString();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    public virtual async Task InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        await OnInitializeAsync(configuration, cancellationToken);

        IsInitialized = true;
    }

    public async Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsInitialized)
        {
            throw new InvalidOperationException("Agent has not been initialized. Call InitializeAsync first.");
        }

        if (request == null)
        {
            return AgentResult.CreateError("Request cannot be null");
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(Configuration.Timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            var result = await ExecuteInternalAsync(request, linkedCts.Token);

            return result ?? AgentResult.CreateError("ExecuteInternalAsync returned null result");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return AgentResult.CreateError("Operation was cancelled by the caller");
        }
        catch (OperationCanceledException)
        {
            return AgentResult.CreateError($"Operation timed out after {Configuration.Timeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            return AgentResult.CreateError($"Agent execution failed: {ex.Message}");
        }
    }

    public virtual Task<AgentResult> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(request, cancellationToken);
    }

    public virtual Task<AgentHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            ThrowIfDisposed();

            var health = new AgentHealthStatus
            {
                IsHealthy = IsInitialized,
                Message = IsInitialized ? "Agent is healthy and operational" : "Agent is not initialized",
                Timestamp = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime
            };

            health.Metrics["IsInitialized"] = IsInitialized;
            health.Metrics["AgentId"] = Id;
            health.Metrics["AgentName"] = Name;

            return Task.FromResult(health);
        }
        catch (ObjectDisposedException)
        {
            return Task.FromResult(new AgentHealthStatus
            {
                IsHealthy = false,
                Message = "Agent has been disposed",
                Timestamp = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AgentHealthStatus
            {
                IsHealthy = false,
                Message = $"Health check failed: {ex.Message}",
                Timestamp = DateTime.UtcNow,
                ResponseTime = DateTime.UtcNow - startTime
            });
        }
    }

    protected abstract Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken);

    protected virtual Task OnInitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken)
    {
        // Override in derived classes if needed
        return Task.CompletedTask;
    }

    protected virtual ValueTask OnDisposeAsync()
    {
        // Override in derived classes if needed
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await OnDisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}