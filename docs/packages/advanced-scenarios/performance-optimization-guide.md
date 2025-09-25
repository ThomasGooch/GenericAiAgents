# ⚡ Performance Optimization Guide for GenericAgents

## Table of Contents

1. [Performance Architecture](#performance-architecture)
2. [Agent Optimization Strategies](#agent-optimization-strategies)
3. [Workflow Performance Tuning](#workflow-performance-tuning)
4. [Memory Management](#memory-management)
5. [Caching Strategies](#caching-strategies)
6. [Concurrency Optimization](#concurrency-optimization)
7. [Database & I/O Optimization](#database--io-optimization)
8. [Monitoring & Profiling](#monitoring--profiling)
9. [Production Tuning Checklist](#production-tuning-checklist)

## Performance Architecture

### High-Performance System Design
Enterprise-grade architecture optimized for throughput and latency.

```csharp
public class PerformanceOptimizedAgentSystem
{
    public static IServiceCollection AddHighPerformanceAgentServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Object pooling for frequently allocated objects
        services.AddSingleton<ObjectPool<AgentExecutionContext>>(serviceProvider =>
        {
            var policy = new AgentExecutionContextPoolPolicy();
            return new DefaultObjectPool<AgentExecutionContext>(policy, 1000);
        });

        // High-performance workflow engine with optimizations
        services.AddSingleton<IWorkflowEngine>(serviceProvider =>
        {
            var underlyingEngine = new WorkflowEngine(
                serviceProvider.GetRequiredService<IAgentRegistryEnhanced>(),
                serviceProvider.GetRequiredService<ILogger<WorkflowEngine>>()
            );
            
            return new PerformanceOptimizedWorkflowEngine(
                underlyingEngine,
                serviceProvider.GetRequiredService<IMemoryCache>(),
                serviceProvider.GetRequiredService<ObjectPool<AgentExecutionContext>>(),
                serviceProvider.GetRequiredService<IPerformanceProfiler>()
            );
        });

        // Optimized metrics collection with batching
        services.AddSingleton<IMetricsCollector, BatchingMetricsCollector>();

        // High-performance caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "GenericAgents";
        });
        
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000000; // Limit memory cache size
            options.CompactionPercentage = 0.05;
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
        });

        // Connection pooling
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
            {
                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });
        }, poolSize: 100);

        // HTTP client optimization
        services.AddHttpClient<ExternalServiceClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            MaxConnectionsPerServer = 50,
            UseProxy = false
        });

        return services;
    }
}
```

### Performance Configuration
Environment-specific performance tuning parameters.

```json
{
  "Performance": {
    "MaxConcurrentAgents": 100,
    "WorkflowBatchSize": 50,
    "CacheExpirationMinutes": 15,
    "ConnectionPoolSize": 100,
    "MaxRetryAttempts": 3,
    "TimeoutSeconds": 30
  },
  "MemoryManagement": {
    "GCMode": "Server",
    "LargeObjectHeapCompactionMode": "CompactOnce",
    "MaxMemoryUsageMB": 2048,
    "ObjectPoolSize": 1000
  },
  "Threading": {
    "MinWorkerThreads": 50,
    "MaxWorkerThreads": 200,
    "MinCompletionPortThreads": 50,
    "MaxCompletionPortThreads": 200
  }
}
```

## Agent Optimization Strategies

### High-Performance Agent Implementation
Optimized agent base class with performance enhancements.

```csharp
public abstract class HighPerformanceAgent : BaseAgent
{
    private readonly ObjectPool<AgentExecutionContext> _contextPool;
    private readonly IMemoryCache _cache;
    private readonly IPerformanceProfiler _profiler;
    private static readonly ConcurrentDictionary<string, CompiledExpression> _compiledExpressions = new();

    protected HighPerformanceAgent(
        ObjectPool<AgentExecutionContext> contextPool,
        IMemoryCache cache,
        IPerformanceProfiler profiler)
    {
        _contextPool = contextPool;
        _cache = cache;
        _profiler = profiler;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        // Use object pooling for execution context
        var context = _contextPool.Get();
        
        try
        {
            context.Initialize(request, cancellationToken);
            
            // Performance profiling
            using var activity = _profiler.StartActivity($"Agent.{GetType().Name}.Execute");
            
            // Check cache first
            var cacheKey = GenerateCacheKey(request);
            if (ShouldUseCache(request) && _cache.TryGetValue(cacheKey, out AgentResult cachedResult))
            {
                activity.SetTag("cache_hit", "true");
                return cachedResult;
            }

            // Execute with optimization
            var result = await ExecuteOptimizedAsync(context);
            
            // Cache successful results
            if (result.IsSuccess && ShouldCacheResult(request, result))
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = GetCacheExpiration(request),
                    Size = EstimateResultSize(result),
                    Priority = CacheItemPriority.Normal
                };
                
                _cache.Set(cacheKey, result, cacheOptions);
            }

            return result;
        }
        finally
        {
            // Return context to pool
            _contextPool.Return(context);
        }
    }

    protected abstract Task<AgentResult> ExecuteOptimizedAsync(AgentExecutionContext context);

    protected virtual bool ShouldUseCache(AgentRequest request) => true;
    protected virtual bool ShouldCacheResult(AgentRequest request, AgentResult result) => result.IsSuccess;
    protected virtual TimeSpan GetCacheExpiration(AgentRequest request) => TimeSpan.FromMinutes(15);
    protected virtual long EstimateResultSize(AgentResult result) => 1; // Estimate in KB

    protected string GenerateCacheKey(AgentRequest request)
    {
        // Use high-performance hash for cache key generation
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hasher.AppendData(Encoding.UTF8.GetBytes($"{GetType().Name}:{request.RequestId}"));
        hasher.AppendData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request.Payload)));
        
        return Convert.ToBase64String(hasher.GetHashAndReset());
    }
}

// Optimized agent execution context with object pooling
public class AgentExecutionContext
{
    public AgentRequest Request { get; private set; } = new();
    public CancellationToken CancellationToken { get; private set; }
    public Dictionary<string, object> ExecutionData { get; } = new();
    public DateTime StartTime { get; private set; }

    public void Initialize(AgentRequest request, CancellationToken cancellationToken)
    {
        Request = request;
        CancellationToken = cancellationToken;
        StartTime = DateTime.UtcNow;
        ExecutionData.Clear();
    }

    public void Reset()
    {
        Request = new AgentRequest();
        CancellationToken = default;
        ExecutionData.Clear();
        StartTime = default;
    }
}

// Object pool policy for execution contexts
public class AgentExecutionContextPoolPolicy : IPooledObjectPolicy<AgentExecutionContext>
{
    public AgentExecutionContext Create() => new();

    public bool Return(AgentExecutionContext obj)
    {
        obj.Reset();
        return true;
    }
}
```

## Workflow Performance Tuning

### Optimized Workflow Engine
High-performance workflow execution with intelligent optimizations.

```csharp
public class PerformanceOptimizedWorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowEngine _underlyingEngine;
    private readonly IMemoryCache _cache;
    private readonly ObjectPool<AgentExecutionContext> _contextPool;
    private readonly IPerformanceProfiler _profiler;
    private readonly SemaphoreSlim _concurrencyLimiter;

    public PerformanceOptimizedWorkflowEngine(
        IWorkflowEngine underlyingEngine,
        IMemoryCache cache,
        ObjectPool<AgentExecutionContext> contextPool,
        IPerformanceProfiler profiler)
    {
        _underlyingEngine = underlyingEngine;
        _cache = cache;
        _contextPool = contextPool;
        _profiler = profiler;
        _concurrencyLimiter = new SemaphoreSlim(Environment.ProcessorCount * 2);
    }

    public async Task<WorkflowResult> ExecuteWorkflowAsync(
        WorkflowDefinition workflow, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _profiler.StartActivity($"Workflow.{workflow.Name}.Execute");
        
        // Apply performance optimizations
        var optimizedWorkflow = await OptimizeWorkflowAsync(workflow);
        
        // Control concurrency
        await _concurrencyLimiter.WaitAsync(cancellationToken);
        
        try
        {
            // Execute with performance monitoring
            var result = await _underlyingEngine.ExecuteWorkflowAsync(optimizedWorkflow, cancellationToken);
            
            // Record performance metrics
            RecordWorkflowMetrics(workflow, result, activity);
            
            return result;
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task<WorkflowDefinition> OptimizeWorkflowAsync(WorkflowDefinition workflow)
    {
        var optimizations = new[]
        {
            OptimizeStepParallelization(workflow),
            OptimizeBatching(workflow),
            OptimizeCaching(workflow),
            OptimizeResourceUsage(workflow)
        };

        var optimizedWorkflow = workflow;
        
        foreach (var optimization in optimizations)
        {
            optimizedWorkflow = await optimization;
        }

        return optimizedWorkflow;
    }

    private async Task<WorkflowDefinition> OptimizeStepParallelization(WorkflowDefinition workflow)
    {
        if (workflow.ExecutionMode != WorkflowExecutionMode.Sequential)
            return workflow;

        // Analyze dependencies and identify parallelizable steps
        var dependencyGraph = BuildDependencyGraph(workflow.Steps);
        var parallelGroups = IdentifyParallelGroups(dependencyGraph);

        if (parallelGroups.Any(g => g.Count > 1))
        {
            // Convert to dependency-based execution for better parallelization
            return new WorkflowDefinition
            {
                Id = workflow.Id,
                Name = workflow.Name,
                ExecutionMode = WorkflowExecutionMode.Dependency,
                Steps = workflow.Steps, // Dependencies already analyzed
                Timeout = workflow.Timeout,
                Configuration = workflow.Configuration
            };
        }

        return workflow;
    }

    private async Task<WorkflowDefinition> OptimizeBatching(WorkflowDefinition workflow)
    {
        var batchableSteps = workflow.Steps
            .GroupBy(s => s.AgentId)
            .Where(g => g.Count() > 1 && CanBatchSteps(g.ToList()))
            .ToList();

        if (!batchableSteps.Any())
            return workflow;

        var optimizedSteps = new List<WorkflowStep>();

        foreach (var stepGroup in workflow.Steps.GroupBy(s => s.AgentId))
        {
            if (stepGroup.Count() > 1 && CanBatchSteps(stepGroup.ToList()))
            {
                // Create a single batched step
                var batchStep = new WorkflowStep
                {
                    Id = Guid.NewGuid(),
                    Name = $"Batched {stepGroup.Key}",
                    AgentId = stepGroup.Key,
                    Input = new BatchedInput
                    {
                        Items = stepGroup.Select(s => s.Input).ToArray(),
                        BatchSize = stepGroup.Count()
                    },
                    Timeout = TimeSpan.FromMilliseconds(
                        stepGroup.Sum(s => s.Timeout.TotalMilliseconds) * 0.7 // Batching efficiency
                    )
                };

                optimizedSteps.Add(batchStep);
            }
            else
            {
                optimizedSteps.AddRange(stepGroup);
            }
        }

        return new WorkflowDefinition
        {
            Id = workflow.Id,
            Name = workflow.Name,
            ExecutionMode = workflow.ExecutionMode,
            Steps = optimizedSteps,
            Timeout = workflow.Timeout,
            Configuration = workflow.Configuration
        };
    }

    private void RecordWorkflowMetrics(WorkflowDefinition workflow, WorkflowResult result, IActivity activity)
    {
        activity.SetTag("workflow_success", result.Success.ToString());
        activity.SetTag("step_count", workflow.Steps.Count.ToString());
        activity.SetTag("execution_mode", workflow.ExecutionMode.ToString());
        activity.SetTag("duration_ms", result.ExecutionTime.TotalMilliseconds.ToString());
        
        if (!result.Success)
        {
            activity.SetTag("error_type", result.ErrorMessage?.GetType().Name ?? "Unknown");
        }
    }
}
```

## Memory Management

### Advanced Memory Optimization
Comprehensive memory management for high-throughput scenarios.

```csharp
public class MemoryOptimizedAgentPool
{
    private readonly ConcurrentQueue<IAgent> _availableAgents = new();
    private readonly ConcurrentDictionary<string, WeakReference<IAgent>> _activeAgents = new();
    private readonly SemaphoreSlim _poolSemaphore;
    private readonly Timer _cleanupTimer;
    private readonly ILogger<MemoryOptimizedAgentPool> _logger;

    public MemoryOptimizedAgentPool(int maxPoolSize, ILogger<MemoryOptimizedAgentPool> logger)
    {
        _poolSemaphore = new SemaphoreSlim(maxPoolSize);
        _logger = logger;
        
        // Clean up every 5 minutes
        _cleanupTimer = new Timer(CleanupPool, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<IAgent> GetAgentAsync(string agentType, CancellationToken cancellationToken = default)
    {
        await _poolSemaphore.WaitAsync(cancellationToken);

        try
        {
            // Try to reuse existing agent
            if (_availableAgents.TryDequeue(out var agent) && agent.GetType().Name == agentType)
            {
                _activeAgents[Guid.NewGuid().ToString()] = new WeakReference<IAgent>(agent);
                return agent;
            }

            // Create new agent if pool is empty
            agent = CreateAgent(agentType);
            var agentId = Guid.NewGuid().ToString();
            _activeAgents[agentId] = new WeakReference<IAgent>(agent);
            
            return agent;
        }
        catch
        {
            _poolSemaphore.Release();
            throw;
        }
    }

    public void ReturnAgent(IAgent agent)
    {
        try
        {
            // Reset agent state if possible
            if (agent is IResettable resettableAgent)
            {
                resettableAgent.Reset();
            }

            // Return to pool for reuse
            _availableAgents.Enqueue(agent);
        }
        finally
        {
            _poolSemaphore.Release();
        }
    }

    private void CleanupPool(object? state)
    {
        try
        {
            // Remove dead weak references
            var deadReferences = _activeAgents
                .Where(kvp => !kvp.Value.TryGetTarget(out _))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var deadRef in deadReferences)
            {
                _activeAgents.TryRemove(deadRef, out _);
            }

            // Trigger garbage collection if memory usage is high
            var memoryUsage = GC.GetTotalMemory(false);
            if (memoryUsage > 1024 * 1024 * 1024) // 1GB threshold
            {
                GC.Collect(2, GCCollectionMode.Optimized);
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                var newMemoryUsage = GC.GetTotalMemory(true);
                _logger.LogInformation("Memory cleanup: {Before}MB -> {After}MB", 
                    memoryUsage / (1024 * 1024), newMemoryUsage / (1024 * 1024));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pool cleanup");
        }
    }
}

// Memory-efficient data structures
public class MemoryEfficientWorkflowResult : WorkflowResult
{
    private readonly Lazy<Dictionary<string, object>> _outputs;
    private byte[]? _serializedData;

    public MemoryEfficientWorkflowResult()
    {
        _outputs = new Lazy<Dictionary<string, object>>(DeserializeOutputs);
    }

    public override Dictionary<string, object> Outputs => _outputs.Value;

    public void SerializeData()
    {
        if (_outputs.IsValueCreated && _serializedData == null)
        {
            _serializedData = MessagePackSerializer.Serialize(_outputs.Value);
        }
    }

    private Dictionary<string, object> DeserializeOutputs()
    {
        if (_serializedData != null)
        {
            return MessagePackSerializer.Deserialize<Dictionary<string, object>>(_serializedData);
        }
        
        return new Dictionary<string, object>();
    }
}
```

## Caching Strategies

### Multi-Level Caching Implementation
Sophisticated caching architecture for optimal performance.

```csharp
public class MultiLevelCacheService
{
    private readonly IMemoryCache _l1Cache; // In-process cache
    private readonly IDistributedCache _l2Cache; // Redis cache
    private readonly ICacheMetrics _metrics;
    private readonly ILogger<MultiLevelCacheService> _logger;

    public MultiLevelCacheService(
        IMemoryCache l1Cache,
        IDistributedCache l2Cache,
        ICacheMetrics metrics,
        ILogger<MultiLevelCacheService> logger)
    {
        _l1Cache = l1Cache;
        _l2Cache = l2Cache;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GenerateKey<T>(key);
        
        // Try L1 cache first (fastest)
        if (_l1Cache.TryGetValue(fullKey, out T cachedValue))
        {
            _metrics.RecordCacheHit("L1", typeof(T).Name);
            return cachedValue;
        }

        // Try L2 cache (Redis)
        try
        {
            var serializedValue = await _l2Cache.GetAsync(fullKey, cancellationToken);
            if (serializedValue != null)
            {
                var value = JsonSerializer.Deserialize<T>(serializedValue);
                
                // Promote to L1 cache
                var l1Options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Size = EstimateSize(value),
                    Priority = CacheItemPriority.Normal
                };
                
                _l1Cache.Set(fullKey, value, l1Options);
                _metrics.RecordCacheHit("L2", typeof(T).Name);
                return value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "L2 cache retrieval failed for key {Key}", fullKey);
        }

        _metrics.RecordCacheMiss(typeof(T).Name);
        return default;
    }

    public async Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default)
    {
        var fullKey = GenerateKey<T>(key);
        var expirationTime = expiration ?? TimeSpan.FromMinutes(15);

        // Set in L1 cache
        var l1Options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Min(5, expirationTime.TotalMinutes)),
            Size = EstimateSize(value),
            Priority = DetermineCachePriority<T>()
        };
        
        _l1Cache.Set(fullKey, value, l1Options);

        // Set in L2 cache (async, don't wait)
        _ = Task.Run(async () =>
        {
            try
            {
                var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
                var l2Options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                };
                
                await _l2Cache.SetAsync(fullKey, serializedValue, l2Options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 cache storage failed for key {Key}", fullKey);
            }
        }, cancellationToken);
    }

    private string GenerateKey<T>(string key)
    {
        return $"{typeof(T).Name}:{key}";
    }

    private static long EstimateSize<T>(T value)
    {
        // Rough estimation - in production, use more sophisticated sizing
        return value switch
        {
            string s => s.Length * 2, // Unicode characters
            ICollection collection => collection.Count * 50,
            _ => 100 // Default estimate
        };
    }

    private static CacheItemPriority DetermineCachePriority<T>()
    {
        // Prioritize certain types
        return typeof(T).Name switch
        {
            nameof(AgentResult) => CacheItemPriority.High,
            nameof(WorkflowResult) => CacheItemPriority.High,
            _ => CacheItemPriority.Normal
        };
    }
}

// Intelligent cache invalidation
public class CacheInvalidationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ConcurrentDictionary<string, HashSet<string>> _keyDependencies = new();

    public async Task InvalidateByPatternAsync(string pattern)
    {
        // Invalidate memory cache entries matching pattern
        if (_memoryCache is MemoryCache mc)
        {
            var field = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(mc) is object coherentState)
            {
                var entriesCollection = coherentState.GetType().GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                if (entriesCollection?.GetValue(coherentState) is IDictionary entries)
                {
                    var keysToRemove = new List<object>();
                    
                    foreach (DictionaryEntry entry in entries)
                    {
                        if (entry.Key.ToString()?.Contains(pattern) == true)
                        {
                            keysToRemove.Add(entry.Key);
                        }
                    }

                    foreach (var key in keysToRemove)
                    {
                        _memoryCache.Remove(key);
                    }
                }
            }
        }

        // For distributed cache, maintain a registry of keys
        await InvalidateDistributedCacheByPattern(pattern);
    }

    private async Task InvalidateDistributedCacheByPattern(string pattern)
    {
        var matchingDependencies = _keyDependencies
            .Where(kvp => kvp.Key.Contains(pattern))
            .SelectMany(kvp => kvp.Value)
            .ToList();

        var tasks = matchingDependencies.Select(key => _distributedCache.RemoveAsync(key));
        await Task.WhenAll(tasks);
    }
}
```

## Production Tuning Checklist

### Performance Validation Steps

```csharp
public class PerformanceValidator
{
    public async Task<PerformanceReport> ValidateSystemPerformanceAsync()
    {
        var report = new PerformanceReport();
        
        // Memory usage validation
        report.MemoryUsage = ValidateMemoryUsage();
        
        // Thread pool validation  
        report.ThreadPool = ValidateThreadPool();
        
        // Database performance
        report.Database = await ValidateDatabasePerformanceAsync();
        
        // Cache effectiveness
        report.Cache = ValidateCacheEffectiveness();
        
        // Agent throughput
        report.AgentThroughput = await ValidateAgentThroughputAsync();
        
        return report;
    }

    private MemoryUsageReport ValidateMemoryUsage()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        return new MemoryUsageReport
        {
            TotalMemoryMB = totalMemory / (1024 * 1024),
            Gen0Collections = gen0Collections,
            Gen1Collections = gen1Collections,
            Gen2Collections = gen2Collections,
            HealthStatus = totalMemory > 2 * 1024 * 1024 * 1024 ? "Warning" : "Healthy", // 2GB threshold
            Recommendations = GenerateMemoryRecommendations(totalMemory, gen2Collections)
        };
    }

    private async Task<DatabasePerformanceReport> ValidateDatabasePerformanceAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Test basic connectivity and response time
        var connectionTest = await TestDatabaseConnectionAsync();
        var queryTest = await TestQueryPerformanceAsync();
        var connectionPoolTest = TestConnectionPool();
        
        stopwatch.Stop();

        return new DatabasePerformanceReport
        {
            ConnectionTestMs = connectionTest.responseTime,
            QueryTestMs = queryTest.responseTime, 
            ConnectionPoolUtilization = connectionPoolTest.utilization,
            HealthStatus = DetermineDatabaseHealth(connectionTest, queryTest),
            Recommendations = GenerateDatabaseRecommendations(connectionTest, queryTest)
        };
    }
}

// Performance benchmarking
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class AgentPerformanceBenchmark
{
    private IWorkflowEngine _workflowEngine = null!;
    private WorkflowDefinition _testWorkflow = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize test components
        var services = new ServiceCollection();
        services.AddHighPerformanceAgentServices(new ConfigurationBuilder().Build());
        var serviceProvider = services.BuildServiceProvider();
        
        _workflowEngine = serviceProvider.GetRequiredService<IWorkflowEngine>();
        _testWorkflow = CreateBenchmarkWorkflow();
    }

    [Benchmark]
    public async Task<WorkflowResult> ExecuteSimpleWorkflow()
    {
        return await _workflowEngine.ExecuteWorkflowAsync(_testWorkflow);
    }

    [Benchmark]
    public async Task<WorkflowResult[]> ExecuteParallelWorkflows()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _workflowEngine.ExecuteWorkflowAsync(_testWorkflow))
            .ToArray();
            
        return await Task.WhenAll(tasks);
    }

    private WorkflowDefinition CreateBenchmarkWorkflow()
    {
        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Benchmark Workflow",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Benchmark Step",
                    AgentId = "benchmark-agent",
                    Input = new { data = "test" }
                }
            }
        };
    }
}
```

### Production Performance Checklist

#### **Application Level**
- [ ] Object pooling implemented for frequently allocated objects
- [ ] Caching strategy optimized with multi-level approach
- [ ] Database queries optimized with proper indexing
- [ ] Connection pooling configured appropriately
- [ ] Lazy loading implemented where beneficial
- [ ] Async/await patterns used consistently
- [ ] Memory allocations minimized in hot paths
- [ ] Proper disposal of resources implemented

#### **Infrastructure Level**
- [ ] CPU usage < 70% under normal load
- [ ] Memory usage < 80% of available
- [ ] Disk I/O latency < 10ms average
- [ ] Network latency < 100ms to dependencies
- [ ] Database connection pool utilization < 80%
- [ ] Cache hit ratio > 90% for frequently accessed data
- [ ] Garbage collection frequency acceptable (< 5% CPU time)
- [ ] Thread pool starvation avoided

#### **Monitoring & Alerting**
- [ ] Application performance monitoring (APM) configured
- [ ] Custom metrics for business KPIs implemented
- [ ] Performance regression alerts configured
- [ ] Resource utilization monitoring active
- [ ] Database performance monitoring enabled
- [ ] Cache effectiveness metrics tracked
- [ ] Error rate monitoring with thresholds
- [ ] SLA compliance tracking implemented

This comprehensive performance optimization guide ensures your GenericAgents system operates at peak efficiency in production environments while maintaining scalability and reliability.