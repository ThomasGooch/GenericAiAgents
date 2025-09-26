# 🎯 Advanced Workflow Design Patterns & Production Best Practices

## Table of Contents

1. [Enterprise Workflow Patterns](#enterprise-workflow-patterns)
2. [Production Architecture Patterns](#production-architecture-patterns)
3. [Error Handling & Resilience Patterns](#error-handling--resilience-patterns)
4. [Performance Optimization Patterns](#performance-optimization-patterns)
5. [Monitoring & Observability Patterns](#monitoring--observability-patterns)
6. [Security & Compliance Patterns](#security--compliance-patterns)
7. [Scaling & Load Management Patterns](#scaling--load-management-patterns)
8. [Best Practices Checklist](#best-practices-checklist)

## Enterprise Workflow Patterns

### Pattern 1: Circuit Breaker Workflow
Prevents cascade failures by temporarily disabling failing components.

```csharp
public class CircuitBreakerWorkflow
{
    private readonly ICircuitBreakerService _circuitBreaker;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ILogger<CircuitBreakerWorkflow> _logger;

    public async Task<WorkflowResult> ExecuteWithCircuitBreakerAsync(WorkflowDefinition workflow)
    {
        var circuitBreakerKey = $"workflow_{workflow.Name}";
        
        if (!await _circuitBreaker.IsClosedAsync(circuitBreakerKey))
        {
            _logger.LogWarning("Circuit breaker open for workflow {WorkflowName}", workflow.Name);
            return WorkflowResult.CreateError("Circuit breaker open", TimeSpan.Zero);
        }

        try
        {
            var result = await _workflowEngine.ExecuteWorkflowAsync(workflow);
            
            if (!result.Success)
            {
                await _circuitBreaker.RecordFailureAsync(circuitBreakerKey);
            }
            else
            {
                await _circuitBreaker.RecordSuccessAsync(circuitBreakerKey);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            await _circuitBreaker.RecordFailureAsync(circuitBreakerKey);
            throw;
        }
    }
}

// Circuit breaker configuration
public class CircuitBreakerConfiguration
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public int SampleSize { get; set; } = 10;
}
```

### Pattern 2: Saga Pattern for Distributed Transactions
Ensures data consistency across multiple services with compensating actions.

```csharp
public class PaymentProcessingSaga
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ISagaOrchestrator _sagaOrchestrator;

    public async Task<SagaResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var saga = new SagaDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Payment Processing Saga",
            CompensationTimeout = TimeSpan.FromMinutes(5),
            
            Steps = new List<SagaStep>
            {
                new()
                {
                    Name = "Reserve Inventory",
                    AgentId = "inventory-service",
                    Input = new { ProductId = request.ProductId, Quantity = request.Quantity },
                    CompensationAction = new CompensationAction
                    {
                        AgentId = "inventory-service",
                        Action = "release-reservation",
                        Input = new { ReservationId = "{{current_step.output.reservation_id}}" }
                    }
                },
                new()
                {
                    Name = "Process Payment",
                    AgentId = "payment-service",
                    Input = new { Amount = request.Amount, PaymentMethod = request.PaymentMethod },
                    CompensationAction = new CompensationAction
                    {
                        AgentId = "payment-service", 
                        Action = "refund-payment",
                        Input = new { TransactionId = "{{current_step.output.transaction_id}}" }
                    }
                },
                new()
                {
                    Name = "Update Customer Account",
                    AgentId = "customer-service",
                    Input = new { CustomerId = request.CustomerId, Purchase = "{{step_1.output}}" },
                    CompensationAction = new CompensationAction
                    {
                        AgentId = "customer-service",
                        Action = "revert-account-update",
                        Input = new { CustomerId = request.CustomerId, TransactionId = "{{step_2.output.transaction_id}}" }
                    }
                },
                new()
                {
                    Name = "Send Confirmation",
                    AgentId = "notification-service",
                    Input = new { 
                        CustomerId = request.CustomerId,
                        OrderDetails = "{{step_1.output}}",
                        PaymentDetails = "{{step_2.output}}"
                    },
                    CompensationAction = new CompensationAction
                    {
                        AgentId = "notification-service",
                        Action = "send-cancellation-notice",
                        Input = new { CustomerId = request.CustomerId }
                    }
                }
            }
        };

        return await _sagaOrchestrator.ExecuteSagaAsync(saga);
    }
}

// Saga step with compensation
public class SagaStep : WorkflowStep
{
    public CompensationAction CompensationAction { get; set; } = new();
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

public class CompensationAction
{
    public string AgentId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public object Input { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
}
```

### Pattern 3: Event-Driven Workflow Orchestration
Responds to business events with appropriate workflow execution.

```csharp
public class EventDrivenOrchestrator
{
    private readonly IEventBus _eventBus;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly ILogger<EventDrivenOrchestrator> _logger;

    public EventDrivenOrchestrator(
        IEventBus eventBus,
        IWorkflowEngine workflowEngine,
        IWorkflowRegistry workflowRegistry,
        ILogger<EventDrivenOrchestrator> logger)
    {
        _eventBus = eventBus;
        _workflowEngine = workflowEngine;
        _workflowRegistry = workflowRegistry;
        _logger = logger;
        
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _eventBus.Subscribe<CustomerRegisteredEvent>(OnCustomerRegistered);
        _eventBus.Subscribe<OrderPlacedEvent>(OnOrderPlaced);
        _eventBus.Subscribe<PaymentFailedEvent>(OnPaymentFailed);
        _eventBus.Subscribe<InventoryLowEvent>(OnInventoryLow);
    }

    private async Task OnCustomerRegistered(CustomerRegisteredEvent @event)
    {
        var workflow = _workflowRegistry.GetWorkflow("customer-onboarding");
        
        var workflowInstance = workflow.CreateInstance(new
        {
            CustomerId = @event.CustomerId,
            Email = @event.Email,
            RegistrationDate = @event.Timestamp
        });

        await _workflowEngine.ExecuteWorkflowAsync(workflowInstance);
    }

    private async Task OnOrderPlaced(OrderPlacedEvent @event)
    {
        // Execute parallel workflows for order processing
        var workflows = new[]
        {
            _workflowRegistry.GetWorkflow("inventory-check"),
            _workflowRegistry.GetWorkflow("payment-processing"), 
            _workflowRegistry.GetWorkflow("shipping-preparation")
        };

        var tasks = workflows.Select(workflow =>
        {
            var instance = workflow.CreateInstance(@event);
            return _workflowEngine.ExecuteWorkflowAsync(instance);
        });

        var results = await Task.WhenAll(tasks);
        
        // Check if all workflows succeeded
        if (results.All(r => r.Success))
        {
            await _eventBus.PublishAsync(new OrderProcessingCompletedEvent(@event.OrderId));
        }
        else
        {
            await HandleOrderProcessingFailure(@event, results);
        }
    }

    private async Task OnPaymentFailed(PaymentFailedEvent @event)
    {
        var retryWorkflow = _workflowRegistry.GetWorkflow("payment-retry");
        var compensationWorkflow = _workflowRegistry.GetWorkflow("order-cancellation");
        
        if (@event.RetryAttempt < 3)
        {
            // Retry payment with exponential backoff
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, @event.RetryAttempt)));
            await _workflowEngine.ExecuteWorkflowAsync(retryWorkflow.CreateInstance(@event));
        }
        else
        {
            // Cancel order and compensate
            await _workflowEngine.ExecuteWorkflowAsync(compensationWorkflow.CreateInstance(@event));
        }
    }
}

// Event definitions
public record CustomerRegisteredEvent(Guid CustomerId, string Email, DateTime Timestamp);
public record OrderPlacedEvent(Guid OrderId, Guid CustomerId, List<OrderItem> Items);
public record PaymentFailedEvent(Guid OrderId, string Reason, int RetryAttempt);
public record InventoryLowEvent(string ProductId, int CurrentStock, int MinimumThreshold);
```

## Production Architecture Patterns

### Pattern 4: Multi-Tenant Workflow Isolation
Ensures secure isolation between tenants in SaaS environments.

```csharp
public class MultiTenantWorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowEngine _underlyingEngine;
    private readonly ITenantContextProvider _tenantContext;
    private readonly ITenantResourceManager _resourceManager;
    private readonly ILogger<MultiTenantWorkflowEngine> _logger;

    public async Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        var tenantConfig = await _resourceManager.GetTenantConfigurationAsync(tenantId);
        
        // Apply tenant-specific resource limits
        workflow = ApplyTenantResourceLimits(workflow, tenantConfig);
        
        // Add tenant isolation tags
        workflow = AddTenantIsolationTags(workflow, tenantId);
        
        // Execute in tenant-specific context
        using var tenantScope = _tenantContext.CreateTenantScope(tenantId);
        
        var result = await _underlyingEngine.ExecuteWorkflowAsync(workflow, cancellationToken);
        
        // Audit tenant workflow execution
        await LogTenantWorkflowExecution(tenantId, workflow, result);
        
        return result;
    }

    private WorkflowDefinition ApplyTenantResourceLimits(WorkflowDefinition workflow, TenantConfiguration config)
    {
        // Apply tenant-specific timeout limits
        foreach (var step in workflow.Steps)
        {
            if (step.Timeout > config.MaxStepTimeout)
            {
                step.Timeout = config.MaxStepTimeout;
            }
        }
        
        if (workflow.Timeout > config.MaxWorkflowTimeout)
        {
            workflow.Timeout = config.MaxWorkflowTimeout;
        }
        
        // Limit parallel execution based on tenant tier
        if (workflow.ExecutionMode == WorkflowExecutionMode.Parallel)
        {
            var maxParallel = config.MaxParallelSteps;
            // Implement parallel step limiting logic here
        }
        
        return workflow;
    }

    private WorkflowDefinition AddTenantIsolationTags(WorkflowDefinition workflow, string tenantId)
    {
        workflow.Configuration["tenant_id"] = tenantId;
        workflow.Configuration["isolation_level"] = "tenant";
        
        foreach (var step in workflow.Steps)
        {
            step.Tags = step.Tags ?? new Dictionary<string, string>();
            step.Tags["tenant_id"] = tenantId;
        }
        
        return workflow;
    }
}

// Tenant configuration model
public class TenantConfiguration
{
    public string TenantId { get; set; } = string.Empty;
    public TenantTier Tier { get; set; }
    public TimeSpan MaxWorkflowTimeout { get; set; }
    public TimeSpan MaxStepTimeout { get; set; }
    public int MaxParallelSteps { get; set; }
    public int MaxConcurrentWorkflows { get; set; }
    public Dictionary<string, object> CustomLimits { get; set; } = new();
}

public enum TenantTier
{
    Basic,
    Professional,
    Enterprise
}
```

### Pattern 5: Workflow State Persistence & Recovery
Ensures workflows can survive application restarts and failures.

```csharp
public class PersistentWorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowEngine _underlyingEngine;
    private readonly IWorkflowStateStore _stateStore;
    private readonly IWorkflowCheckpointService _checkpointService;
    private readonly ILogger<PersistentWorkflowEngine> _logger;

    public async Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid();
        
        // Save initial workflow state
        await _stateStore.SaveWorkflowStateAsync(new WorkflowState
        {
            ExecutionId = executionId,
            WorkflowDefinition = workflow,
            Status = WorkflowStatus.Starting,
            StartTime = DateTime.UtcNow
        });

        try
        {
            // Execute workflow with checkpointing
            var result = await ExecuteWithCheckpoints(workflow, executionId, cancellationToken);
            
            // Update final state
            await _stateStore.UpdateWorkflowStatusAsync(executionId, WorkflowStatus.Completed, result);
            
            return result;
        }
        catch (Exception ex)
        {
            await _stateStore.UpdateWorkflowStatusAsync(executionId, WorkflowStatus.Failed, null, ex);
            throw;
        }
    }

    private async Task<WorkflowResult> ExecuteWithCheckpoints(
        WorkflowDefinition workflow, 
        Guid executionId, 
        CancellationToken cancellationToken)
    {
        var checkpointInterval = TimeSpan.FromMinutes(1);
        var lastCheckpoint = DateTime.UtcNow;
        
        // Enhanced workflow with checkpoint callbacks
        var monitoredWorkflow = new WorkflowDefinition
        {
            Id = workflow.Id,
            Name = workflow.Name,
            ExecutionMode = workflow.ExecutionMode,
            Steps = workflow.Steps.Select(step => new WorkflowStep
            {
                Id = step.Id,
                Name = step.Name,
                AgentId = step.AgentId,
                Input = step.Input,
                OnStepCompleted = async (stepResult) =>
                {
                    // Create checkpoint after each step
                    await _checkpointService.CreateCheckpointAsync(executionId, step.Id, stepResult);
                    
                    // Periodic full state checkpoint
                    if (DateTime.UtcNow - lastCheckpoint > checkpointInterval)
                    {
                        await _stateStore.UpdateWorkflowProgressAsync(executionId, stepResult);
                        lastCheckpoint = DateTime.UtcNow;
                    }
                }
            }).ToList()
        };

        return await _underlyingEngine.ExecuteWorkflowAsync(monitoredWorkflow, cancellationToken);
    }

    public async Task<WorkflowResult> ResumeWorkflowAsync(Guid executionId)
    {
        var workflowState = await _stateStore.GetWorkflowStateAsync(executionId);
        if (workflowState == null)
        {
            throw new WorkflowNotFoundException($"Workflow {executionId} not found");
        }

        var lastCheckpoint = await _checkpointService.GetLatestCheckpointAsync(executionId);
        
        // Resume from last completed step
        var resumedWorkflow = CreateResumedWorkflow(workflowState.WorkflowDefinition, lastCheckpoint);
        
        return await ExecuteWorkflowAsync(resumedWorkflow);
    }
}

// Workflow state models
public class WorkflowState
{
    public Guid ExecutionId { get; set; }
    public WorkflowDefinition WorkflowDefinition { get; set; } = new();
    public WorkflowStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public List<StepCheckpoint> Checkpoints { get; set; } = new();
}

public class StepCheckpoint
{
    public Guid StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Completed { get; set; }
    public object? Result { get; set; }
    public Exception? Error { get; set; }
}

public enum WorkflowStatus
{
    Starting,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}
```

## Error Handling & Resilience Patterns

### Pattern 6: Comprehensive Error Recovery
Handles different types of failures with appropriate recovery strategies.

```csharp
public class ResilientWorkflowExecutor
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IDeadLetterQueue _deadLetterQueue;
    private readonly IAlertingService _alertingService;

    public async Task<WorkflowResult> ExecuteResilientWorkflowAsync(WorkflowDefinition workflow)
    {
        var context = new WorkflowExecutionContext
        {
            WorkflowId = workflow.Id,
            StartTime = DateTime.UtcNow,
            RetryAttempts = new Dictionary<Guid, int>()
        };

        return await ExecuteWithRetryAndFallback(workflow, context);
    }

    private async Task<WorkflowResult> ExecuteWithRetryAndFallback(
        WorkflowDefinition workflow, 
        WorkflowExecutionContext context)
    {
        try
        {
            return await _workflowEngine.ExecuteWorkflowAsync(workflow);
        }
        catch (TransientException ex)
        {
            // Retry transient failures
            return await HandleTransientFailure(workflow, context, ex);
        }
        catch (ValidationException ex)
        {
            // Don't retry validation errors
            await _alertingService.SendValidationAlert(workflow, ex);
            return WorkflowResult.CreateError($"Validation failed: {ex.Message}", TimeSpan.Zero);
        }
        catch (ResourceExhaustedException ex)
        {
            // Handle resource constraints
            return await HandleResourceExhaustion(workflow, context, ex);
        }
        catch (ExternalServiceException ex)
        {
            // Handle external service failures
            return await HandleExternalServiceFailure(workflow, context, ex);
        }
        catch (Exception ex)
        {
            // Handle unexpected errors
            return await HandleUnexpectedError(workflow, context, ex);
        }
    }

    private async Task<WorkflowResult> HandleTransientFailure(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context,
        TransientException ex)
    {
        var retryCount = context.RetryAttempts.GetValueOrDefault(workflow.Id, 0);
        
        if (retryCount >= 3)
        {
            await _deadLetterQueue.SendAsync(workflow, ex, "Max retries exceeded");
            return WorkflowResult.CreateError("Max retries exceeded", context.ElapsedTime);
        }

        // Exponential backoff
        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
        await Task.Delay(delay);
        
        context.RetryAttempts[workflow.Id] = retryCount + 1;
        
        return await ExecuteWithRetryAndFallback(workflow, context);
    }

    private async Task<WorkflowResult> HandleResourceExhaustion(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context, 
        ResourceExhaustedException ex)
    {
        // Scale down workflow or queue for later execution
        var scaledWorkflow = await ScaleDownWorkflow(workflow, ex.ResourceType);
        
        if (scaledWorkflow != null)
        {
            return await ExecuteWithRetryAndFallback(scaledWorkflow, context);
        }
        
        // Queue for execution when resources are available
        await QueueWorkflowForLaterExecution(workflow, ex.ResourceType);
        return WorkflowResult.CreatePending("Queued due to resource constraints", context.ElapsedTime);
    }

    private async Task<WorkflowResult> HandleExternalServiceFailure(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context,
        ExternalServiceException ex)
    {
        // Check if fallback service is available
        var fallbackWorkflow = await CreateFallbackWorkflow(workflow, ex.ServiceName);
        
        if (fallbackWorkflow != null)
        {
            return await ExecuteWithRetryAndFallback(fallbackWorkflow, context);
        }
        
        // Degrade gracefully by skipping non-critical steps
        var degradedWorkflow = CreateDegradedWorkflow(workflow, ex.ServiceName);
        return await ExecuteWithRetryAndFallback(degradedWorkflow, context);
    }
}

// Exception hierarchy for proper error handling
public class TransientException : Exception
{
    public TransientException(string message, Exception? innerException = null) 
        : base(message, innerException) { }
}

public class ResourceExhaustedException : Exception
{
    public string ResourceType { get; }
    
    public ResourceExhaustedException(string resourceType, string message) 
        : base(message)
    {
        ResourceType = resourceType;
    }
}

public class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    
    public ExternalServiceException(string serviceName, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}
```

## Performance Optimization Patterns

### Pattern 7: Intelligent Resource Management
Optimizes resource utilization for high-throughput scenarios.

```csharp
public class OptimizedWorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowEngine _underlyingEngine;
    private readonly IResourceManager _resourceManager;
    private readonly IWorkflowOptimizer _optimizer;
    private readonly IPerformanceMonitor _performanceMonitor;

    public async Task<WorkflowResult> ExecuteWorkflowAsync(
        WorkflowDefinition workflow, 
        CancellationToken cancellationToken = default)
    {
        // Pre-execution optimization
        var optimizedWorkflow = await OptimizeWorkflow(workflow);
        
        // Resource allocation
        using var resourceReservation = await _resourceManager.ReserveResourcesAsync(
            optimizedWorkflow.GetResourceRequirements());

        // Performance monitoring
        using var performanceTracker = _performanceMonitor.StartTracking(workflow.Id);
        
        try
        {
            return await _underlyingEngine.ExecuteWorkflowAsync(optimizedWorkflow, cancellationToken);
        }
        finally
        {
            // Collect performance metrics for future optimizations
            var metrics = performanceTracker.GetMetrics();
            await _optimizer.UpdatePerformanceModelAsync(workflow, metrics);
        }
    }

    private async Task<WorkflowDefinition> OptimizeWorkflow(WorkflowDefinition workflow)
    {
        var optimizations = new List<IWorkflowOptimization>
        {
            new ParallelizationOptimization(),
            new BatchingOptimization(),
            new CachingOptimization(),
            new ResourceLocalityOptimization()
        };

        var optimizedWorkflow = workflow;
        
        foreach (var optimization in optimizations)
        {
            if (await optimization.CanOptimizeAsync(optimizedWorkflow))
            {
                optimizedWorkflow = await optimization.OptimizeAsync(optimizedWorkflow);
            }
        }

        return optimizedWorkflow;
    }
}

// Optimization implementations
public class ParallelizationOptimization : IWorkflowOptimization
{
    public async Task<bool> CanOptimizeAsync(WorkflowDefinition workflow)
    {
        // Check if sequential steps can be parallelized
        return workflow.ExecutionMode == WorkflowExecutionMode.Sequential &&
               await HasParallelizableSteps(workflow);
    }

    public async Task<WorkflowDefinition> OptimizeAsync(WorkflowDefinition workflow)
    {
        var parallelGroups = await IdentifyParallelGroups(workflow.Steps);
        
        return new WorkflowDefinition
        {
            Id = workflow.Id,
            Name = $"{workflow.Name} (Optimized)",
            ExecutionMode = WorkflowExecutionMode.Dependency,
            Steps = CreateParallelizedSteps(parallelGroups)
        };
    }
}

public class BatchingOptimization : IWorkflowOptimization
{
    public async Task<bool> CanOptimizeAsync(WorkflowDefinition workflow)
    {
        return await HasBatchableOperations(workflow);
    }

    public async Task<WorkflowDefinition> OptimizeAsync(WorkflowDefinition workflow)
    {
        var batchGroups = await IdentifyBatchableSteps(workflow.Steps);
        
        var optimizedSteps = new List<WorkflowStep>();
        
        foreach (var group in batchGroups)
        {
            if (group.Count > 1)
            {
                // Create a single batched step
                optimizedSteps.Add(new WorkflowStep
                {
                    Id = Guid.NewGuid(),
                    Name = $"Batched {group.First().Name}",
                    AgentId = group.First().AgentId,
                    Input = new BatchRequest { Items = group.Select(s => s.Input).ToList() },
                    BatchSize = group.Count
                });
            }
            else
            {
                optimizedSteps.AddRange(group);
            }
        }

        workflow.Steps = optimizedSteps;
        return workflow;
    }
}

// Resource management
public class ResourceManager : IResourceManager
{
    private readonly SemaphoreSlim _cpuSemaphore;
    private readonly SemaphoreSlim _memorySemaphore;
    private readonly SemaphoreSlim _networkSemaphore;

    public ResourceManager(ResourceConfiguration config)
    {
        _cpuSemaphore = new SemaphoreSlim(config.MaxConcurrentCpuTasks);
        _memorySemaphore = new SemaphoreSlim(config.MaxConcurrentMemoryTasks);
        _networkSemaphore = new SemaphoreSlim(config.MaxConcurrentNetworkTasks);
    }

    public async Task<IResourceReservation> ReserveResourcesAsync(ResourceRequirements requirements)
    {
        var reservations = new List<SemaphoreSlim>();

        if (requirements.RequiresCpu)
        {
            await _cpuSemaphore.WaitAsync();
            reservations.Add(_cpuSemaphore);
        }

        if (requirements.RequiresMemory)
        {
            await _memorySemaphore.WaitAsync();
            reservations.Add(_memorySemaphore);
        }

        if (requirements.RequiresNetwork)
        {
            await _networkSemaphore.WaitAsync();
            reservations.Add(_networkSemaphore);
        }

        return new ResourceReservation(reservations);
    }
}
```

## Best Practices Checklist

### Development Phase
- [ ] **Workflow Design**
  - [ ] Single responsibility per workflow
  - [ ] Clear input/output contracts
  - [ ] Proper error boundaries
  - [ ] Testable step isolation

- [ ] **Error Handling**
  - [ ] Transient vs permanent error classification
  - [ ] Appropriate retry strategies
  - [ ] Circuit breaker implementation
  - [ ] Graceful degradation paths

- [ ] **Resource Management**
  - [ ] Resource requirement specification
  - [ ] Timeout configuration
  - [ ] Memory usage optimization
  - [ ] Connection pooling

### Production Deployment
- [ ] **Monitoring**
  - [ ] End-to-end workflow tracing
  - [ ] Performance metrics collection
  - [ ] Business metric tracking
  - [ ] Alert configuration

- [ ] **Scalability**
  - [ ] Horizontal scaling support
  - [ ] Load balancing configuration
  - [ ] Resource limit definition
  - [ ] Capacity planning

- [ ] **Security**
  - [ ] Input validation
  - [ ] Authorization checks
  - [ ] Audit logging
  - [ ] Secret management

- [ ] **Operational Excellence**
  - [ ] Health check endpoints
  - [ ] Graceful shutdown
  - [ ] Configuration management
  - [ ] Deployment automation

### Production Monitoring
- [ ] **Key Metrics**
  - [ ] Workflow success rate > 99%
  - [ ] Average execution time < SLA
  - [ ] Error rate < 1%
  - [ ] Resource utilization < 80%

- [ ] **Alerting Thresholds**
  - [ ] Critical: Success rate < 95%
  - [ ] Warning: Execution time > 2x average
  - [ ] Info: New error patterns detected
  - [ ] Critical: Resource exhaustion

This comprehensive guide provides enterprise-grade patterns and practices for building robust, scalable, and maintainable workflow systems. Each pattern addresses real-world production scenarios and includes complete implementation examples.