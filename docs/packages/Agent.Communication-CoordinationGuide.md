# 📡 Agent.Communication: Seamless Agent Coordination

## What is Agent.Communication?

**Simple explanation:** Agent.Communication provides reliable messaging and coordination capabilities for agents to work together, share information, and coordinate complex multi-agent workflows.

**When to use it:** When you have multiple agents that need to communicate, share data, coordinate tasks, or work together in distributed scenarios.

**Key concepts in plain English:**
- **Communication Channels** are the "pathways" that agents use to send messages to each other
- **Request-Response Pattern** allows agents to ask questions and get answers from other agents
- **Fire-and-Forget Messaging** lets agents send notifications without waiting for responses
- **Message Routing** ensures messages reach the right agent, even in complex distributed systems

## From Isolated to Collaborative Agents

### The Communication Evolution

```
🏝️ Isolated Agents:
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Agent A   │    │   Agent B   │    │   Agent C   │
│             │    │             │    │             │
│ - Processes │    │ - Processes │    │ - Processes │
│   data      │    │   text      │    │   images    │
│ - No sharing│    │ - No sharing│    │ - No sharing│
└─────────────┘    └─────────────┘    └─────────────┘
Result: Duplicated work, no collaboration

🤝 Connected Agents:
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Agent A   │◄──►│   Agent B   │◄──►│   Agent C   │
│             │    │             │    │             │
│ - Processes │    │ - Processes │    │ - Processes │
│   data      │    │   text      │    │   images    │
│ - Shares    │    │ - Requests  │    │ - Enhances  │
│   results   │    │   data      │    │   content   │
└─────────────┘    └─────────────┘    └─────────────┘
Result: Collaborative intelligence, shared knowledge
```

### Real-World Coordination Scenarios

```
📊 Business Intelligence Platform:
┌─────────────────────────────────────────────────────┐
│  Data Collection Agent                              │
│  ├─ Gathers data from multiple sources             │
│  └─ Sends to Processing Agent ──────────────────┐   │
└─────────────────────────────────────────────────│───┘
┌─────────────────────────────────────────────────▼───┐
│  Data Processing Agent                              │
│  ├─ Cleans and validates data                      │
│  ├─ Requests analysis from Analytics Agent ────┐   │
│  └─ Sends results to Report Agent ──────────┐   │   │
└─────────────────────────────────────────────│───│───┘
┌─────────────────────────────────────────────▼───│───┐
│  Analytics Agent                                │   │
│  ├─ Performs statistical analysis               │   │
│  ├─ Generates insights                          │   │
│  └─ Returns analysis results ───────────────────┘   │
└─────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────▼───┐
│  Report Generation Agent                            │
│  ├─ Creates formatted reports                      │
│  ├─ Requests review from Quality Agent ─────────┐   │
│  └─ Publishes final reports                     │   │
└─────────────────────────────────────────────────│───┘
┌─────────────────────────────────────────────────▼───┐
│  Quality Assurance Agent                           │
│  ├─ Reviews reports for accuracy                   │
│  └─ Provides feedback and approval                 │
└─────────────────────────────────────────────────────┘
```

## Communication Patterns

### Pattern 1: Request-Response (Synchronous)
Perfect for when agents need answers before proceeding:

```csharp
using Agent.Communication;
using Agent.Communication.Models;
using Agent.Core;

public class DataProcessingAgent : BaseAgent
{
    private readonly ICommunicationChannel _communicationChannel;
    private readonly ILogger<DataProcessingAgent> _logger;

    public DataProcessingAgent(
        ICommunicationChannel communicationChannel,
        ILogger<DataProcessingAgent> logger)
        : base("data-processing-agent", "Processes data with validation from other agents")
    {
        _communicationChannel = communicationChannel;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing data request: {RequestId}", request.Id);

            // Step 1: Validate data with Validation Agent
            var validationResult = await RequestDataValidationAsync(request.Input, cancellationToken);
            if (!validationResult.IsValid)
            {
                return AgentResult.CreateError($"Data validation failed: {validationResult.Error}");
            }

            // Step 2: Request enhancement from Enhancement Agent
            var enhancementResult = await RequestDataEnhancementAsync(request.Input, cancellationToken);
            if (!enhancementResult.IsValid)
            {
                _logger.LogWarning("Data enhancement failed, proceeding with original data");
            }

            // Step 3: Process the data
            var processedData = ProcessData(enhancementResult.IsValid ? enhancementResult.Data : request.Input);

            // Step 4: Request quality review
            var qualityResult = await RequestQualityReviewAsync(processedData, cancellationToken);

            var result = new
            {
                ProcessedData = processedData,
                ValidationPassed = validationResult.IsValid,
                EnhancementApplied = enhancementResult.IsValid,
                QualityScore = qualityResult.QualityScore,
                ProcessingSteps = new[]
                {
                    "validation",
                    "enhancement",
                    "processing",
                    "quality-review"
                }
            };

            return AgentResult.CreateSuccess(System.Text.Json.JsonSerializer.Serialize(result), new Dictionary<string, object>
            {
                ["validationPassed"] = validationResult.IsValid,
                ["enhancementApplied"] = enhancementResult.IsValid,
                ["qualityScore"] = qualityResult.QualityScore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data processing failed");
            return AgentResult.CreateError($"Data processing error: {ex.Message}");
        }
    }

    private async Task<ValidationResult> RequestDataValidationAsync(string data, CancellationToken cancellationToken)
    {
        try
        {
            var request = new CommunicationRequest
            {
                Target = "data-validation-agent",
                Source = "data-processing-agent",
                Action = "validate",
                Payload = new { data = data, rules = "standard" },
                TimeoutSeconds = 30
            };

            _logger.LogDebug("Requesting data validation from validation agent");
            var response = await _communicationChannel.SendRequestAsync(request, cancellationToken);

            if (response.Success && response.Data != null)
            {
                var validationData = System.Text.Json.JsonSerializer.Deserialize<ValidationResult>(response.Data.ToString()!);
                return validationData ?? new ValidationResult { IsValid = false, Error = "Invalid response format" };
            }

            return new ValidationResult { IsValid = false, Error = response.Error ?? "Validation request failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting data validation");
            return new ValidationResult { IsValid = false, Error = $"Validation request error: {ex.Message}" };
        }
    }

    private async Task<EnhancementResult> RequestDataEnhancementAsync(string data, CancellationToken cancellationToken)
    {
        try
        {
            var request = new CommunicationRequest
            {
                Target = "data-enhancement-agent",
                Source = "data-processing-agent",
                Action = "enhance",
                Payload = new { 
                    data = data, 
                    enhancements = new[] { "normalize", "enrich", "format" } 
                },
                TimeoutSeconds = 45
            };

            _logger.LogDebug("Requesting data enhancement from enhancement agent");
            var response = await _communicationChannel.SendRequestAsync(request, cancellationToken);

            if (response.Success && response.Data != null)
            {
                var enhancementData = System.Text.Json.JsonSerializer.Deserialize<EnhancementResult>(response.Data.ToString()!);
                return enhancementData ?? new EnhancementResult { IsValid = false, Data = data };
            }

            return new EnhancementResult { IsValid = false, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error requesting data enhancement, using original data");
            return new EnhancementResult { IsValid = false, Data = data };
        }
    }

    private async Task<QualityResult> RequestQualityReviewAsync(string processedData, CancellationToken cancellationToken)
    {
        try
        {
            var request = new CommunicationRequest
            {
                Target = "quality-assurance-agent",
                Source = "data-processing-agent",
                Action = "review",
                Payload = new { 
                    data = processedData, 
                    criteria = new[] { "accuracy", "completeness", "format" } 
                },
                TimeoutSeconds = 60
            };

            _logger.LogDebug("Requesting quality review from QA agent");
            var response = await _communicationChannel.SendRequestAsync(request, cancellationToken);

            if (response.Success && response.Data != null)
            {
                var qualityData = System.Text.Json.JsonSerializer.Deserialize<QualityResult>(response.Data.ToString()!);
                return qualityData ?? new QualityResult { QualityScore = 0.5, Issues = new[] { "Unable to parse quality response" } };
            }

            return new QualityResult { QualityScore = 0.6, Issues = new[] { "Quality review unavailable" } };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error requesting quality review");
            return new QualityResult { QualityScore = 0.5, Issues = new[] { $"Quality review error: {ex.Message}" } };
        }
    }

    private string ProcessData(string data)
    {
        // Simulate data processing
        var processed = data.ToUpperInvariant().Trim();
        _logger.LogDebug("Data processing completed, length: {Length}", processed.Length);
        return processed;
    }
}

// Supporting classes
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class EnhancementResult
{
    public bool IsValid { get; set; }
    public string Data { get; set; } = string.Empty;
    public List<string> EnhancementsApplied { get; set; } = new();
}

public class QualityResult
{
    public double QualityScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}
```

### Pattern 2: Fire-and-Forget (Asynchronous)
For notifications and non-critical updates:

```csharp
public class EventPublisherAgent : BaseAgent
{
    private readonly ICommunicationChannel _communicationChannel;
    private readonly ILogger<EventPublisherAgent> _logger;

    public EventPublisherAgent(
        ICommunicationChannel communicationChannel,
        ILogger<EventPublisherAgent> logger)
        : base("event-publisher-agent", "Publishes events to interested agents")
    {
        _communicationChannel = communicationChannel;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var eventData = System.Text.Json.JsonSerializer.Deserialize<EventData>(request.Input);
            if (eventData == null)
            {
                return AgentResult.CreateError("Invalid event data format");
            }

            _logger.LogInformation("Publishing event: {EventType}", eventData.EventType);

            // Publish to multiple interested agents without waiting for responses
            var publishTasks = new List<Task>
            {
                NotifyLoggingAgentAsync(eventData, cancellationToken),
                NotifyAnalyticsAgentAsync(eventData, cancellationToken),
                NotifyMonitoringAgentAsync(eventData, cancellationToken),
                NotifyAuditAgentAsync(eventData, cancellationToken)
            };

            // Execute all notifications concurrently
            await Task.WhenAll(publishTasks);

            var result = new
            {
                EventId = eventData.Id,
                EventType = eventData.EventType,
                NotificationsSent = publishTasks.Count,
                PublishedAt = DateTime.UtcNow,
                Recipients = new[] { "logging-agent", "analytics-agent", "monitoring-agent", "audit-agent" }
            };

            return AgentResult.CreateSuccess(System.Text.Json.JsonSerializer.Serialize(result), new Dictionary<string, object>
            {
                ["eventType"] = eventData.EventType,
                ["notificationsSent"] = publishTasks.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event publishing failed");
            return AgentResult.CreateError($"Event publishing error: {ex.Message}");
        }
    }

    private async Task NotifyLoggingAgentAsync(EventData eventData, CancellationToken cancellationToken)
    {
        try
        {
            var request = new CommunicationRequest
            {
                Target = "logging-agent",
                Source = "event-publisher-agent",
                Action = "log-event",
                Payload = new { 
                    eventId = eventData.Id,
                    eventType = eventData.EventType,
                    data = eventData.Data,
                    level = DetermineLogLevel(eventData.EventType)
                },
                Priority = 7 // Lower priority for logging
            };

            await _communicationChannel.SendAsync(request, cancellationToken);
            _logger.LogDebug("Event notification sent to logging agent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify logging agent");
            // Don't fail the whole operation for logging failures
        }
    }

    private async Task NotifyAnalyticsAgentAsync(EventData eventData, CancellationToken cancellationToken)
    {
        try
        {
            // Only send events that are relevant for analytics
            if (!IsAnalyticsRelevant(eventData.EventType)) return;

            var request = new CommunicationRequest
            {
                Target = "analytics-agent",
                Source = "event-publisher-agent",
                Action = "record-metric",
                Payload = new { 
                    eventId = eventData.Id,
                    eventType = eventData.EventType,
                    timestamp = eventData.Timestamp,
                    metadata = eventData.Metadata
                },
                Priority = 5 // Normal priority for analytics
            };

            await _communicationChannel.SendAsync(request, cancellationToken);
            _logger.LogDebug("Event notification sent to analytics agent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify analytics agent");
        }
    }

    private async Task NotifyMonitoringAgentAsync(EventData eventData, CancellationToken cancellationToken)
    {
        try
        {
            // Only send critical events to monitoring
            if (!IsCriticalEvent(eventData.EventType)) return;

            var request = new CommunicationRequest
            {
                Target = "monitoring-agent",
                Source = "event-publisher-agent",
                Action = "health-check",
                Payload = new { 
                    eventId = eventData.Id,
                    eventType = eventData.EventType,
                    severity = DetermineSeverity(eventData.EventType),
                    affectedSystems = eventData.AffectedSystems
                },
                Priority = 1 // High priority for monitoring
            };

            await _communicationChannel.SendAsync(request, cancellationToken);
            _logger.LogDebug("Event notification sent to monitoring agent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify monitoring agent");
        }
    }

    private async Task NotifyAuditAgentAsync(EventData eventData, CancellationToken cancellationToken)
    {
        try
        {
            // Only audit security-relevant events
            if (!IsSecurityRelevant(eventData.EventType)) return;

            var request = new CommunicationRequest
            {
                Target = "audit-agent",
                Source = "event-publisher-agent",
                Action = "audit-log",
                Payload = new { 
                    eventId = eventData.Id,
                    eventType = eventData.EventType,
                    userId = eventData.UserId,
                    timestamp = eventData.Timestamp,
                    auditData = eventData.AuditData
                },
                Priority = 2 // High priority for audit
            };

            await _communicationChannel.SendAsync(request, cancellationToken);
            _logger.LogDebug("Event notification sent to audit agent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify audit agent - this is critical!");
            // Audit failures are more serious, but still don't fail the operation
        }
    }

    private string DetermineLogLevel(string eventType) => eventType switch
    {
        "error" or "exception" => "Error",
        "warning" => "Warning",
        "info" or "success" => "Information",
        _ => "Debug"
    };

    private bool IsAnalyticsRelevant(string eventType) => eventType switch
    {
        "user-action" or "performance" or "usage" or "success" => true,
        _ => false
    };

    private bool IsCriticalEvent(string eventType) => eventType switch
    {
        "error" or "exception" or "system-failure" or "security-alert" => true,
        _ => false
    };

    private bool IsSecurityRelevant(string eventType) => eventType switch
    {
        "login" or "logout" or "permission-change" or "security-alert" or "data-access" => true,
        _ => false
    };

    private string DetermineSeverity(string eventType) => eventType switch
    {
        "system-failure" => "Critical",
        "error" or "security-alert" => "High",
        "warning" => "Medium",
        _ => "Low"
    };
}

public class EventData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Data { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> AffectedSystems { get; set; } = new();
    public Dictionary<string, object> AuditData { get; set; } = new();
}
```

### Pattern 3: Agent Service Discovery
Dynamic agent discovery and registration:

```csharp
public class AgentDiscoveryService : BaseAgent
{
    private readonly ICommunicationChannel _communicationChannel;
    private readonly ILogger<AgentDiscoveryService> _logger;
    private readonly ConcurrentDictionary<string, AgentInfo> _registeredAgents = new();
    private readonly Timer _healthCheckTimer;

    public AgentDiscoveryService(
        ICommunicationChannel communicationChannel,
        ILogger<AgentDiscoveryService> logger)
        : base("agent-discovery-service", "Manages agent registration and discovery")
    {
        _communicationChannel = communicationChannel;
        _logger = logger;
        
        // Start periodic health checks
        _healthCheckTimer = new Timer(PerformHealthChecksAsync, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        var actionData = System.Text.Json.JsonSerializer.Deserialize<DiscoveryAction>(request.Input);
        if (actionData == null)
        {
            return AgentResult.CreateError("Invalid discovery action format");
        }

        try
        {
            return actionData.Action.ToLowerInvariant() switch
            {
                "register" => await RegisterAgentAsync(actionData, cancellationToken),
                "deregister" => await DeregisterAgentAsync(actionData, cancellationToken),
                "discover" => await DiscoverAgentsAsync(actionData, cancellationToken),
                "health-check" => await PerformHealthCheckAsync(actionData, cancellationToken),
                "get-status" => await GetAgentStatusAsync(actionData, cancellationToken),
                _ => AgentResult.CreateError($"Unknown discovery action: {actionData.Action}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery operation failed: {Action}", actionData.Action);
            return AgentResult.CreateError($"Discovery operation failed: {ex.Message}");
        }
    }

    private async Task<AgentResult> RegisterAgentAsync(DiscoveryAction action, CancellationToken cancellationToken)
    {
        if (action.AgentInfo == null)
        {
            return AgentResult.CreateError("Agent information is required for registration");
        }

        var agentInfo = action.AgentInfo;
        agentInfo.RegisteredAt = DateTime.UtcNow;
        agentInfo.LastHealthCheck = DateTime.UtcNow;
        agentInfo.Status = AgentStatus.Active;

        _registeredAgents.AddOrUpdate(agentInfo.Id, agentInfo, (key, existing) =>
        {
            _logger.LogInformation("Agent {AgentId} re-registered, updating information", key);
            agentInfo.RegisteredAt = existing.RegisteredAt; // Preserve original registration time
            return agentInfo;
        });

        _logger.LogInformation("Agent registered: {AgentId} ({AgentType})", agentInfo.Id, agentInfo.Type);

        // Notify other agents about new registration
        await NotifyAgentRegistrationAsync(agentInfo, cancellationToken);

        var result = new
        {
            Success = true,
            AgentId = agentInfo.Id,
            RegisteredAt = agentInfo.RegisteredAt,
            TotalAgents = _registeredAgents.Count
        };

        return AgentResult.CreateSuccess(System.Text.Json.JsonSerializer.Serialize(result));
    }

    private async Task<AgentResult> DiscoverAgentsAsync(DiscoveryAction action, CancellationToken cancellationToken)
    {
        var activeAgents = _registeredAgents.Values
            .Where(a => a.Status == AgentStatus.Active)
            .ToList();

        // Apply filters if specified
        if (!string.IsNullOrEmpty(action.FilterType))
        {
            activeAgents = activeAgents.Where(a => a.Type.Equals(action.FilterType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrEmpty(action.FilterCapability))
        {
            activeAgents = activeAgents.Where(a => a.Capabilities.Contains(action.FilterCapability)).ToList();
        }

        var discoveryResult = new
        {
            TotalAgents = activeAgents.Count,
            Agents = activeAgents.Select(a => new
            {
                Id = a.Id,
                Type = a.Type,
                Description = a.Description,
                Capabilities = a.Capabilities,
                Endpoint = a.Endpoint,
                Status = a.Status.ToString(),
                LastHealthCheck = a.LastHealthCheck,
                LoadLevel = a.LoadLevel
            }).OrderBy(a => a.LoadLevel).ToList(),
            DiscoveredAt = DateTime.UtcNow
        };

        return AgentResult.CreateSuccess(System.Text.Json.JsonSerializer.Serialize(discoveryResult));
    }

    private async Task<AgentResult> PerformHealthCheckAsync(DiscoveryAction action, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(action.AgentId))
        {
            return AgentResult.CreateError("Agent ID is required for health check");
        }

        if (!_registeredAgents.TryGetValue(action.AgentId, out var agentInfo))
        {
            return AgentResult.CreateError($"Agent {action.AgentId} not found");
        }

        try
        {
            var healthRequest = new CommunicationRequest
            {
                Target = agentInfo.Id,
                Source = "agent-discovery-service",
                Action = "health-check",
                TimeoutSeconds = 10
            };

            var response = await _communicationChannel.SendRequestAsync(healthRequest, cancellationToken);
            var isHealthy = response.Success;

            // Update agent status
            agentInfo.LastHealthCheck = DateTime.UtcNow;
            agentInfo.Status = isHealthy ? AgentStatus.Active : AgentStatus.Unhealthy;

            if (isHealthy && response.Data != null)
            {
                // Update load level if provided
                var healthData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(response.Data.ToString()!);
                if (healthData != null && healthData.TryGetValue("loadLevel", out var loadLevel))
                {
                    if (double.TryParse(loadLevel.ToString(), out var load))
                    {
                        agentInfo.LoadLevel = load;
                    }
                }
            }

            var healthResult = new
            {
                AgentId = agentInfo.Id,
                IsHealthy = isHealthy,
                Status = agentInfo.Status.ToString(),
                LastHealthCheck = agentInfo.LastHealthCheck,
                LoadLevel = agentInfo.LoadLevel,
                ResponseTime = response.Duration.TotalMilliseconds
            };

            return AgentResult.CreateSuccess(System.Text.Json.JsonSerializer.Serialize(healthResult));
        }
        catch (Exception ex)
        {
            agentInfo.Status = AgentStatus.Unhealthy;
            agentInfo.LastHealthCheck = DateTime.UtcNow;
            
            _logger.LogWarning(ex, "Health check failed for agent: {AgentId}", action.AgentId);
            
            return AgentResult.CreateError($"Health check failed: {ex.Message}");
        }
    }

    private async Task NotifyAgentRegistrationAsync(AgentInfo agentInfo, CancellationToken cancellationToken)
    {
        var notificationTasks = _registeredAgents.Values
            .Where(a => a.Id != agentInfo.Id && a.Status == AgentStatus.Active)
            .Select(a => NotifyAgentAsync(a.Id, "agent-registered", agentInfo, cancellationToken));

        await Task.WhenAll(notificationTasks);
    }

    private async Task NotifyAgentAsync(string targetAgentId, string action, object data, CancellationToken cancellationToken)
    {
        try
        {
            var notification = new CommunicationRequest
            {
                Target = targetAgentId,
                Source = "agent-discovery-service",
                Action = action,
                Payload = data,
                Priority = 8 // Low priority notification
            };

            await _communicationChannel.SendAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify agent {AgentId} about {Action}", targetAgentId, action);
        }
    }

    private async void PerformHealthChecksAsync(object? state)
    {
        try
        {
            var healthCheckTasks = _registeredAgents.Values
                .Where(a => a.Status == AgentStatus.Active || a.Status == AgentStatus.Unhealthy)
                .Where(a => DateTime.UtcNow - a.LastHealthCheck > TimeSpan.FromMinutes(2))
                .Select(a => PerformSingleHealthCheckAsync(a.Id, CancellationToken.None));

            await Task.WhenAll(healthCheckTasks);

            // Remove agents that haven't responded to health checks for too long
            var staleAgents = _registeredAgents.Values
                .Where(a => DateTime.UtcNow - a.LastHealthCheck > TimeSpan.FromMinutes(10))
                .ToList();

            foreach (var staleAgent in staleAgents)
            {
                _registeredAgents.TryRemove(staleAgent.Id, out _);
                _logger.LogWarning("Removed stale agent from registry: {AgentId}", staleAgent.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic health checks");
        }
    }

    private async Task PerformSingleHealthCheckAsync(string agentId, CancellationToken cancellationToken)
    {
        var action = new DiscoveryAction { Action = "health-check", AgentId = agentId };
        await PerformHealthCheckAsync(action, cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        _healthCheckTimer?.Dispose();
        await base.DisposeAsync();
    }
}

// Supporting classes
public class DiscoveryAction
{
    public string Action { get; set; } = string.Empty;
    public string? AgentId { get; set; }
    public string? FilterType { get; set; }
    public string? FilterCapability { get; set; }
    public AgentInfo? AgentInfo { get; set; }
}

public class AgentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
    public AgentStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public double LoadLevel { get; set; } = 0.0;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum AgentStatus
{
    Active,
    Inactive,
    Unhealthy,
    Maintenance
}
```

## Advanced Communication Patterns

### Pattern 4: Message Broadcasting with Topics
Event-driven architecture with topic-based messaging:

```csharp
public class TopicBasedCommunicationService : IHostedService
{
    private readonly ICommunicationChannel _communicationChannel;
    private readonly ILogger<TopicBasedCommunicationService> _logger;
    private readonly ConcurrentDictionary<string, List<SubscriberInfo>> _subscriptions = new();
    private bool _isListening;

    public TopicBasedCommunicationService(
        ICommunicationChannel communicationChannel,
        ILogger<TopicBasedCommunicationService> logger)
    {
        _communicationChannel = communicationChannel;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _communicationChannel.InitializeAsync(new Dictionary<string, object>
        {
            ["channelId"] = "topic-communication-service"
        }, cancellationToken);

        await _communicationChannel.ConnectAsync(cancellationToken);

        await _communicationChannel.StartListeningAsync(HandleIncomingRequest, cancellationToken);
        _isListening = true;

        _logger.LogInformation("Topic-based communication service started");
    }

    private async Task<CommunicationResponse> HandleIncomingRequest(CommunicationRequest request)
    {
        try
        {
            return request.Action.ToLowerInvariant() switch
            {
                "subscribe" => await HandleSubscriptionAsync(request),
                "unsubscribe" => await HandleUnsubscriptionAsync(request),
                "publish" => await HandlePublishAsync(request),
                "list-topics" => await HandleListTopicsAsync(request),
                "list-subscribers" => await HandleListSubscribersAsync(request),
                _ => CommunicationResponse.CreateError(request.RequestId, $"Unknown action: {request.Action}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling communication request: {Action}", request.Action);
            return CommunicationResponse.CreateError(request.RequestId, $"Request handling failed: {ex.Message}");
        }
    }

    private async Task<CommunicationResponse> HandleSubscriptionAsync(CommunicationRequest request)
    {
        if (request.Payload == null)
        {
            return CommunicationResponse.CreateError(request.RequestId, "Subscription payload required");
        }

        var subscriptionData = System.Text.Json.JsonSerializer.Deserialize<SubscriptionRequest>(request.Payload.ToString()!);
        if (subscriptionData == null || string.IsNullOrEmpty(subscriptionData.Topic))
        {
            return CommunicationResponse.CreateError(request.RequestId, "Invalid subscription data");
        }

        var subscriber = new SubscriberInfo
        {
            AgentId = request.Source,
            Topic = subscriptionData.Topic,
            Filters = subscriptionData.Filters ?? new(),
            SubscribedAt = DateTime.UtcNow,
            IsActive = true
        };

        _subscriptions.AddOrUpdate(subscriptionData.Topic, 
            new List<SubscriberInfo> { subscriber },
            (topic, existing) =>
            {
                // Remove existing subscription for this agent
                existing.RemoveAll(s => s.AgentId == request.Source);
                existing.Add(subscriber);
                return existing;
            });

        _logger.LogInformation("Agent {AgentId} subscribed to topic: {Topic}", request.Source, subscriptionData.Topic);

        var result = new
        {
            Success = true,
            Topic = subscriptionData.Topic,
            AgentId = request.Source,
            SubscribedAt = subscriber.SubscribedAt,
            TotalSubscribers = _subscriptions[subscriptionData.Topic].Count
        };

        return CommunicationResponse.CreateSuccess(request.RequestId, result);
    }

    private async Task<CommunicationResponse> HandlePublishAsync(CommunicationRequest request)
    {
        if (request.Payload == null)
        {
            return CommunicationResponse.CreateError(request.RequestId, "Publish payload required");
        }

        var publishData = System.Text.Json.JsonSerializer.Deserialize<PublishRequest>(request.Payload.ToString()!);
        if (publishData == null || string.IsNullOrEmpty(publishData.Topic))
        {
            return CommunicationResponse.CreateError(request.RequestId, "Invalid publish data");
        }

        if (!_subscriptions.TryGetValue(publishData.Topic, out var subscribers))
        {
            _logger.LogDebug("No subscribers found for topic: {Topic}", publishData.Topic);
            return CommunicationResponse.CreateSuccess(request.RequestId, new { MessagesSent = 0, Topic = publishData.Topic });
        }

        // Filter subscribers based on message criteria
        var eligibleSubscribers = subscribers
            .Where(s => s.IsActive && ShouldReceiveMessage(s, publishData))
            .ToList();

        // Send message to all eligible subscribers
        var notificationTasks = eligibleSubscribers.Select(subscriber => 
            SendMessageToSubscriberAsync(subscriber, publishData, CancellationToken.None));

        var results = await Task.WhenAll(notificationTasks);
        var successCount = results.Count(r => r);

        _logger.LogInformation("Published message to topic {Topic}: {SuccessCount}/{TotalCount} delivered", 
            publishData.Topic, successCount, eligibleSubscribers.Count);

        var result = new
        {
            MessagesSent = successCount,
            TotalSubscribers = eligibleSubscribers.Count,
            Topic = publishData.Topic,
            PublishedAt = DateTime.UtcNow
        };

        return CommunicationResponse.CreateSuccess(request.RequestId, result);
    }

    private bool ShouldReceiveMessage(SubscriberInfo subscriber, PublishRequest publishData)
    {
        // Apply subscriber filters
        foreach (var filter in subscriber.Filters)
        {
            if (!ApplyFilter(filter, publishData))
            {
                return false;
            }
        }

        return true;
    }

    private bool ApplyFilter(MessageFilter filter, PublishRequest publishData)
    {
        return filter.Type.ToLowerInvariant() switch
        {
            "priority" => CheckPriorityFilter(filter, publishData),
            "source" => CheckSourceFilter(filter, publishData),
            "content" => CheckContentFilter(filter, publishData),
            _ => true // Unknown filters pass by default
        };
    }

    private bool CheckPriorityFilter(MessageFilter filter, PublishRequest publishData)
    {
        if (int.TryParse(filter.Value, out var minPriority))
        {
            return publishData.Priority <= minPriority; // Lower number = higher priority
        }
        return true;
    }

    private bool CheckSourceFilter(MessageFilter filter, PublishRequest publishData)
    {
        return filter.Value == "*" || publishData.Source?.Contains(filter.Value) == true;
    }

    private bool CheckContentFilter(MessageFilter filter, PublishRequest publishData)
    {
        return publishData.Data?.ToString()?.Contains(filter.Value, StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task<bool> SendMessageToSubscriberAsync(SubscriberInfo subscriber, PublishRequest publishData, CancellationToken cancellationToken)
    {
        try
        {
            var message = new CommunicationRequest
            {
                Target = subscriber.AgentId,
                Source = "topic-communication-service",
                Action = "topic-message",
                Payload = new
                {
                    Topic = publishData.Topic,
                    Source = publishData.Source,
                    Data = publishData.Data,
                    Priority = publishData.Priority,
                    Timestamp = DateTime.UtcNow,
                    MessageId = Guid.NewGuid().ToString()
                },
                Priority = publishData.Priority,
                TimeoutSeconds = 15
            };

            await _communicationChannel.SendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send message to subscriber: {AgentId}", subscriber.AgentId);
            
            // Mark subscriber as inactive if delivery fails repeatedly
            subscriber.IsActive = false;
            return false;
        }
    }

    private async Task<CommunicationResponse> HandleListTopicsAsync(CommunicationRequest request)
    {
        var topics = _subscriptions.Keys.Select(topic => new
        {
            Topic = topic,
            SubscriberCount = _subscriptions[topic].Count(s => s.IsActive),
            TotalSubscribers = _subscriptions[topic].Count,
            LastActivity = _subscriptions[topic].Max(s => s.SubscribedAt)
        }).ToList();

        return CommunicationResponse.CreateSuccess(request.RequestId, new { Topics = topics, Count = topics.Count });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_isListening)
        {
            await _communicationChannel.StopListeningAsync(cancellationToken);
        }

        await _communicationChannel.DisconnectAsync(cancellationToken);
        await _communicationChannel.DisposeAsync();

        _logger.LogInformation("Topic-based communication service stopped");
    }
}

// Supporting classes
public class SubscriptionRequest
{
    public string Topic { get; set; } = string.Empty;
    public List<MessageFilter> Filters { get; set; } = new();
}

public class PublishRequest
{
    public string Topic { get; set; } = string.Empty;
    public string? Source { get; set; }
    public object? Data { get; set; }
    public int Priority { get; set; } = 5;
}

public class SubscriberInfo
{
    public string AgentId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public List<MessageFilter> Filters { get; set; } = new();
    public DateTime SubscribedAt { get; set; }
    public bool IsActive { get; set; }
}

public class MessageFilter
{
    public string Type { get; set; } = string.Empty; // priority, source, content, etc.
    public string Value { get; set; } = string.Empty;
}
```

### Pattern 5: Circuit Breaker Communication
Resilient communication with failure recovery:

```csharp
public class ResilientCommunicationWrapper : ICommunicationChannel
{
    private readonly ICommunicationChannel _baseChannel;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILogger<ResilientCommunicationWrapper> _logger;
    private readonly ResilientCommunicationOptions _options;

    public ResilientCommunicationWrapper(
        ICommunicationChannel baseChannel,
        ILogger<ResilientCommunicationWrapper> logger,
        ResilientCommunicationOptions options)
    {
        _baseChannel = baseChannel;
        _logger = logger;
        _options = options;
        _circuitBreaker = new CircuitBreaker(_options.CircuitBreakerOptions, _logger);

        // Forward events
        _baseChannel.ConnectionStatusChanged += (sender, connected) => ConnectionStatusChanged?.Invoke(sender, connected);
        _baseChannel.ErrorOccurred += (sender, error) => ErrorOccurred?.Invoke(sender, error);
    }

    public string ChannelId => _baseChannel.ChannelId;
    public bool IsConnected => _baseChannel.IsConnected;
    public Dictionary<string, object> Configuration => _baseChannel.Configuration;

    public event EventHandler<bool>? ConnectionStatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    public async Task<CommunicationResponse> SendRequestAsync(CommunicationRequest request, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await ExecuteWithRetryAsync(
                () => _baseChannel.SendRequestAsync(request, cancellationToken),
                $"SendRequest to {request.Target}",
                cancellationToken);
        }, cancellationToken);
    }

    public async Task SendAsync(CommunicationRequest request, CancellationToken cancellationToken = default)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            await ExecuteWithRetryAsync(
                async () => 
                {
                    await _baseChannel.SendAsync(request, cancellationToken);
                    return new CommunicationResponse(); // Dummy response for retry logic
                },
                $"Send to {request.Target}",
                cancellationToken);
        }, cancellationToken);
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var maxAttempts = _options.MaxRetryAttempts;

        while (attempt < maxAttempts)
        {
            attempt++;
            
            try
            {
                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("{Operation} succeeded on attempt {Attempt}", operationName, attempt);
                }
                
                return result;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsRetryableException(ex))
            {
                var delay = CalculateDelay(attempt);
                _logger.LogWarning(ex, "{Operation} failed on attempt {Attempt}, retrying in {Delay}ms", 
                    operationName, attempt, delay.TotalMilliseconds);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Operation} failed on attempt {Attempt} (final attempt)", operationName, attempt);
                throw;
            }
        }

        throw new InvalidOperationException($"{operationName} failed after {maxAttempts} attempts");
    }

    private bool IsRetryableException(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            TaskCanceledException when !ex.Message.Contains("user") => true, // Network timeout, not user cancellation
            HttpRequestException => true,
            SocketException => true,
            _ when ex.Message.Contains("connection") => true,
            _ when ex.Message.Contains("timeout") => true,
            _ when ex.Message.Contains("unavailable") => true,
            _ => false
        };
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var baseDelay = _options.BaseRetryDelay;
        var exponentialDelay = TimeSpan.FromMilliseconds(
            baseDelay.TotalMilliseconds * Math.Pow(_options.RetryBackoffMultiplier, attempt - 1));
        
        // Add jitter to prevent thundering herd
        var jitter = Random.Shared.NextDouble() * 0.1 - 0.05; // ±5% jitter
        var finalDelay = TimeSpan.FromMilliseconds(
            exponentialDelay.TotalMilliseconds * (1 + jitter));
        
        return finalDelay > _options.MaxRetryDelay ? _options.MaxRetryDelay : finalDelay;
    }

    // Delegate other methods to base channel
    public Task InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
        => _baseChannel.InitializeAsync(configuration, cancellationToken);

    public Task ConnectAsync(CancellationToken cancellationToken = default)
        => _baseChannel.ConnectAsync(cancellationToken);

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
        => _baseChannel.DisconnectAsync(cancellationToken);

    public Task StartListeningAsync(Func<CommunicationRequest, Task<CommunicationResponse>> requestHandler, CancellationToken cancellationToken = default)
        => _baseChannel.StartListeningAsync(requestHandler, cancellationToken);

    public Task StopListeningAsync(CancellationToken cancellationToken = default)
        => _baseChannel.StopListeningAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _baseChannel.DisposeAsync();
}

public class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger _logger;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private DateTime _nextRetryTime = DateTime.MinValue;
    private readonly object _lock = new object();

    public CircuitBreaker(CircuitBreakerOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Open:
                    if (DateTime.UtcNow < _nextRetryTime)
                    {
                        throw new CircuitBreakerOpenException("Circuit breaker is open");
                    }
                    _state = CircuitBreakerState.HalfOpen;
                    _logger.LogInformation("Circuit breaker moving to half-open state");
                    break;

                case CircuitBreakerState.HalfOpen:
                    // Allow one request to test if service is back
                    break;

                case CircuitBreakerState.Closed:
                    // Normal operation
                    break;
            }
        }

        try
        {
            var result = await operation();
            
            lock (_lock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                    _logger.LogInformation("Circuit breaker closed - service recovered");
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _lastFailureTime = DateTime.UtcNow;
                _failureCount++;

                if (_state == CircuitBreakerState.HalfOpen || _failureCount >= _options.FailureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                    _nextRetryTime = DateTime.UtcNow.Add(_options.OpenTimeout);
                    _logger.LogWarning("Circuit breaker opened after {FailureCount} failures", _failureCount);
                }
            }
            
            throw;
        }
    }
}

public enum CircuitBreakerState
{
    Closed,  // Normal operation
    Open,    // Failing fast
    HalfOpen // Testing if service recovered
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(60);
}

public class ResilientCommunicationOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public CircuitBreakerOptions CircuitBreakerOptions { get; set; } = new();
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

## Production Communication Setup

### Multi-Channel Configuration
Setting up different communication channels for different scenarios:

```csharp
public class CommunicationChannelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommunicationChannelFactory> _logger;

    public CommunicationChannelFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CommunicationChannelFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ICommunicationChannel> CreateChannelAsync(string channelType, string channelId, CancellationToken cancellationToken = default)
    {
        ICommunicationChannel baseChannel = channelType.ToLowerInvariant() switch
        {
            "inmemory" => CreateInMemoryChannel(channelId),
            "signalr" => await CreateSignalRChannelAsync(channelId, cancellationToken),
            "rabbitmq" => await CreateRabbitMQChannelAsync(channelId, cancellationToken),
            "redis" => await CreateRedisChannelAsync(channelId, cancellationToken),
            "servicebus" => await CreateServiceBusChannelAsync(channelId, cancellationToken),
            _ => throw new ArgumentException($"Unknown channel type: {channelType}")
        };

        // Wrap with resilience features for production
        if (_configuration.GetValue<bool>("Communication:EnableResilience", true))
        {
            var options = _configuration.GetSection("Communication:Resilience").Get<ResilientCommunicationOptions>() 
                         ?? new ResilientCommunicationOptions();

            baseChannel = new ResilientCommunicationWrapper(
                baseChannel, 
                _serviceProvider.GetRequiredService<ILogger<ResilientCommunicationWrapper>>(),
                options);
        }

        // Initialize the channel
        var config = _configuration.GetSection($"Communication:Channels:{channelType}").Get<Dictionary<string, object>>()
                     ?? new Dictionary<string, object>();
        
        await baseChannel.InitializeAsync(config, cancellationToken);

        _logger.LogInformation("Created communication channel: {ChannelType} ({ChannelId})", channelType, channelId);
        
        return baseChannel;
    }

    private ICommunicationChannel CreateInMemoryChannel(string channelId)
    {
        return new InMemoryChannel(channelId);
    }

    private async Task<ICommunicationChannel> CreateSignalRChannelAsync(string channelId, CancellationToken cancellationToken)
    {
        // Implementation for SignalR channel
        // This would create a SignalR-based communication channel for real-time web communication
        throw new NotImplementedException("SignalR channel implementation needed");
    }

    private async Task<ICommunicationChannel> CreateRabbitMQChannelAsync(string channelId, CancellationToken cancellationToken)
    {
        // Implementation for RabbitMQ channel
        // This would create a RabbitMQ-based channel for reliable message queuing
        throw new NotImplementedException("RabbitMQ channel implementation needed");
    }

    private async Task<ICommunicationChannel> CreateRedisChannelAsync(string channelId, CancellationToken cancellationToken)
    {
        // Implementation for Redis Pub/Sub channel
        // This would create a Redis-based channel for fast pub/sub messaging
        throw new NotImplementedException("Redis channel implementation needed");
    }

    private async Task<ICommunicationChannel> CreateServiceBusChannelAsync(string channelId, CancellationToken cancellationToken)
    {
        // Implementation for Azure Service Bus channel
        // This would create an enterprise-grade messaging channel
        throw new NotImplementedException("Service Bus channel implementation needed");
    }
}

// Configuration example for appsettings.json
/*
{
  "Communication": {
    "EnableResilience": true,
    "DefaultChannelType": "inmemory",
    "Resilience": {
      "MaxRetryAttempts": 3,
      "BaseRetryDelay": "00:00:01",
      "RetryBackoffMultiplier": 2.0,
      "MaxRetryDelay": "00:00:30",
      "CircuitBreakerOptions": {
        "FailureThreshold": 5,
        "OpenTimeout": "00:01:00"
      }
    },
    "Channels": {
      "inmemory": {
        "maxMessageSize": 1048576
      },
      "rabbitmq": {
        "connectionString": "amqp://localhost",
        "exchangeName": "agent-communication",
        "queuePrefix": "agent-"
      },
      "redis": {
        "connectionString": "localhost:6379",
        "channelPrefix": "agent-channel:"
      }
    }
  }
}
*/

// DI Configuration
public static class CommunicationServiceExtensions
{
    public static IServiceCollection AddAgentCommunication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddSingleton<CommunicationChannelFactory>();
        
        // Register different channel types
        services.AddTransient<InMemoryChannel>();
        
        // Add hosted service for topic-based communication if enabled
        if (configuration.GetValue<bool>("Communication:EnableTopics", false))
        {
            services.AddHostedService<TopicBasedCommunicationService>();
        }

        // Add agent discovery service if enabled
        if (configuration.GetValue<bool>("Communication:EnableDiscovery", true))
        {
            services.AddSingleton<AgentDiscoveryService>();
        }

        return services;
    }
}
```

## Next Steps

### Integration with Other Packages
- **Agent.Security** - Secure communication channels with authentication and encryption
- **Agent.Orchestration** - Coordinate multi-agent workflows with reliable messaging
- **Agent.Observability** - Monitor communication patterns, message flows, and performance

### Advanced Communication Patterns
- **Event Sourcing** - Capture all agent interactions as immutable events
- **CQRS (Command Query Responsibility Segregation)** - Separate read and write communication patterns
- **Saga Patterns** - Manage long-running distributed agent transactions
- **Message Routing** - Intelligent message routing based on content and context

### Production Deployment
- **Load Balancing** - Distribute communication load across multiple channels
- **Message Persistence** - Ensure message delivery even during system failures
- **Monitoring & Alerting** - Track communication health and performance metrics
- **Security** - Encrypt messages and authenticate agent communications

---

**🎯 You now have powerful agent coordination capabilities that enable sophisticated multi-agent systems!**

Agent.Communication transforms isolated agents into collaborative teams that can share information, coordinate tasks, and work together to solve complex problems. The comprehensive communication patterns, resilience features, and production-ready configurations ensure your agent systems can scale and remain reliable even in distributed environments.