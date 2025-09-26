using Agent.Core.Models;

namespace Agent.Core;

/// <summary>
/// Defines the contract for all AI agents in the GenericAiAgents framework.
/// Agents are autonomous components that can process requests, maintain state, and provide health monitoring.
/// All agents must implement proper initialization, execution, and disposal patterns for enterprise-grade reliability.
/// </summary>
/// <remarks>
/// <para>
/// The IAgent interface establishes the foundational contract for all agent implementations in the framework.
/// It provides a standardized lifecycle (initialize → execute → dispose) and ensures consistent behavior
/// across different agent types whether they're simple processing agents or complex orchestrated workflows.
/// </para>
/// <para>
/// Thread Safety: Implementations should be thread-safe for concurrent operations but are not required
/// to support concurrent initialization or disposal operations.
/// </para>
/// <para>
/// Lifecycle: Agents follow a strict lifecycle: Created → Initialized → Ready → Disposed.
/// Once disposed, agents cannot be reinitialized and should throw ObjectDisposedException.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic agent usage pattern
/// IAgent agent = new MyCustomAgent("my-agent", "Processes customer data");
/// 
/// // Initialize with configuration
/// var configuration = new AgentConfiguration
/// {
///     Timeout = TimeSpan.FromMinutes(5),
///     MaxRetries = 3,
///     Settings = new Dictionary&lt;string, object&gt;
///     {
///         ["CustomSetting"] = "value"
///     }
/// };
/// 
/// await agent.InitializeAsync(configuration);
/// 
/// // Execute requests
/// var request = new AgentRequest
/// {
///     RequestId = Guid.NewGuid().ToString(),
///     Source = "customer-service",
///     Payload = customerData
/// };
/// 
/// var result = await agent.ExecuteAsync(request);
/// 
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Processing completed: {result.Data}");
/// }
/// else
/// {
///     Console.WriteLine($"Processing failed: {result.ErrorMessage}");
/// }
/// 
/// // Check health status
/// var health = await agent.CheckHealthAsync();
/// Console.WriteLine($"Agent health: {health.IsHealthy}");
/// 
/// // Proper disposal
/// await agent.DisposeAsync();
/// </code>
/// </example>
/// <seealso cref="BaseAgent"/>
/// <seealso cref="AgentConfiguration"/>
/// <seealso cref="AgentRequest"/>
/// <seealso cref="AgentResult"/>
public interface IAgent : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this agent instance.
    /// The ID is typically derived from the agent name but guaranteed to be unique within an agent registry.
    /// </summary>
    /// <value>
    /// A unique string identifier that remains constant throughout the agent's lifecycle.
    /// Usually formatted as lowercase with hyphens (e.g., "customer-service-agent").
    /// </value>
    /// <remarks>
    /// The ID is used for agent discovery, logging, metrics collection, and debugging.
    /// It should be URL-safe and suitable for use as a key in distributed systems.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the agent.
    /// This is the display name used in user interfaces, logs, and documentation.
    /// </summary>
    /// <value>
    /// A descriptive name that clearly identifies the agent's purpose and functionality.
    /// </value>
    /// <remarks>
    /// Unlike the ID, the name can contain spaces and special characters.
    /// It should be concise but descriptive enough for users to understand the agent's role.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets a detailed description of the agent's capabilities and intended use cases.
    /// This description should help users understand when and how to use this agent.
    /// </summary>
    /// <value>
    /// A comprehensive description explaining the agent's functionality, input requirements,
    /// expected outputs, and any special considerations or limitations.
    /// </value>
    /// <remarks>
    /// The description is used for agent discovery, documentation generation, and user guidance.
    /// It should include information about supported input formats, processing capabilities,
    /// and any dependencies or prerequisites.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Gets the current configuration settings for this agent.
    /// Configuration includes timeout settings, retry policies, and agent-specific parameters.
    /// </summary>
    /// <value>
    /// The <see cref="AgentConfiguration"/> instance containing all configuration settings.
    /// Returns an empty configuration if the agent has not been initialized.
    /// </value>
    /// <remarks>
    /// Configuration is immutable after initialization. To change configuration,
    /// the agent must be disposed and recreated with new settings.
    /// </remarks>
    /// <seealso cref="AgentConfiguration"/>
    AgentConfiguration Configuration { get; }

    /// <summary>
    /// Gets a value indicating whether the agent has been successfully initialized and is ready to process requests.
    /// </summary>
    /// <value>
    /// <c>true</c> if the agent has been initialized and is ready to process requests;
    /// <c>false</c> if the agent is in an uninitialized state.
    /// </value>
    /// <remarks>
    /// Agents must be initialized before they can execute requests. Attempting to execute
    /// requests on an uninitialized agent will result in an InvalidOperationException.
    /// Once disposed, this property will return false and the agent cannot be reinitialized.
    /// </remarks>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the agent with the specified configuration settings.
    /// This method must be called once before the agent can process any requests.
    /// </summary>
    /// <param name="configuration">
    /// The configuration settings for the agent, including timeout, retry policy, and custom settings.
    /// Cannot be null.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the initialization operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous initialization operation.
    /// The task completes when the agent is fully initialized and ready to process requests.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the agent has already been initialized or has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the initialization is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Initialization is a one-time operation. Once an agent is initialized, subsequent calls
    /// to this method will throw an InvalidOperationException.
    /// </para>
    /// <para>
    /// During initialization, agents may perform resource allocation, establish connections,
    /// validate configuration, and prepare any required dependencies.
    /// </para>
    /// <para>
    /// If initialization fails, the agent remains in an uninitialized state and the operation
    /// can be retried with different configuration if needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AgentConfiguration
    /// {
    ///     Timeout = TimeSpan.FromMinutes(2),
    ///     MaxRetries = 3,
    ///     Settings = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["DatabaseConnection"] = "connection-string",
    ///         ["ApiEndpoint"] = "https://api.example.com"
    ///     }
    /// };
    /// 
    /// try
    /// {
    ///     await agent.InitializeAsync(config, cancellationToken);
    ///     Console.WriteLine("Agent initialized successfully");
    /// }
    /// catch (ArgumentException ex)
    /// {
    ///     Console.WriteLine($"Invalid configuration: {ex.Message}");
    /// }
    /// catch (InvalidOperationException ex)
    /// {
    ///     Console.WriteLine($"Initialization failed: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    Task InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the agent's primary functionality with the provided request data.
    /// This is the main entry point for processing requests and generating results.
    /// </summary>
    /// <param name="request">
    /// The request containing input data, metadata, and processing instructions.
    /// Cannot be null.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the execution operation.
    /// Respects both the provided token and the agent's configured timeout.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous execution operation.
    /// The result contains the processing outcome, data, timing information, and any error details.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the agent has not been initialized or has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>
    /// or when the configured timeout is exceeded.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when attempting to execute on a disposed agent.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The ExecuteAsync method includes built-in timeout handling, retry logic (if configured),
    /// and comprehensive error handling. All exceptions are caught and returned as failed results
    /// rather than being thrown, except for the documented exceptions above.
    /// </para>
    /// <para>
    /// Execution is subject to the timeout specified in the agent configuration. If the timeout
    /// is exceeded, the operation will be cancelled and return a timeout error result.
    /// </para>
    /// <para>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// However, some agent implementations may have limits on concurrent execution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = new AgentRequest
    /// {
    ///     RequestId = Guid.NewGuid().ToString(),
    ///     Source = "web-api",
    ///     Payload = new
    ///     {
    ///         CustomerId = 12345,
    ///         Action = "ProcessOrder",
    ///         Data = orderData
    ///     }
    /// };
    /// 
    /// var result = await agent.ExecuteAsync(request, cancellationToken);
    /// 
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine($"Execution completed in {result.ExecutionTime}");
    ///     Console.WriteLine($"Result: {result.Data}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Execution failed: {result.ErrorMessage}");
    ///     if (result.Exception != null)
    ///     {
    ///         Console.WriteLine($"Exception details: {result.Exception}");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ProcessAsync"/>
    /// <seealso cref="AgentRequest"/>
    /// <seealso cref="AgentResult"/>
    Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a request using the agent's functionality. This method typically delegates to
    /// <see cref="ExecuteAsync"/> but may provide additional processing or transformation logic.
    /// </summary>
    /// <param name="request">
    /// The request containing input data, metadata, and processing instructions.
    /// Cannot be null.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the processing operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous processing operation.
    /// The result contains the processing outcome with any additional transformations applied.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the agent has not been initialized or has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// ProcessAsync is designed to provide a higher-level processing interface that may include
    /// pre-processing, post-processing, or additional business logic around the core execution.
    /// </para>
    /// <para>
    /// In the base implementation, this method simply delegates to ExecuteAsync, but derived
    /// classes may override it to provide specialized processing behavior.
    /// </para>
    /// <para>
    /// Use ProcessAsync when you need the agent's full processing pipeline, including any
    /// additional transformations or business logic. Use ExecuteAsync when you need direct
    /// access to the core agent functionality.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // ProcessAsync may include additional validation or transformation
    /// var request = new AgentRequest
    /// {
    ///     RequestId = Guid.NewGuid().ToString(),
    ///     Source = "batch-processor",
    ///     Payload = rawData
    /// };
    /// 
    /// var result = await agent.ProcessAsync(request, cancellationToken);
    /// 
    /// // Result may include additional metadata from processing pipeline
    /// if (result.IsSuccess &amp;&amp; result.Metadata.ContainsKey("ProcessingSteps"))
    /// {
    ///     var steps = result.Metadata["ProcessingSteps"];
    ///     Console.WriteLine($"Processing steps: {steps}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteAsync"/>
    Task<AgentResult> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check to determine if the agent is operating correctly and is ready to process requests.
    /// This method provides insights into the agent's operational status, performance metrics, and any issues.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the health check operation.
    /// Health checks should be fast, typically completing within a few seconds.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous health check operation.
    /// The result contains health status, diagnostic information, and performance metrics.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Health checks are used by monitoring systems, load balancers, and orchestration frameworks
    /// to determine agent availability and performance. They should be lightweight and fast.
    /// </para>
    /// <para>
    /// A healthy agent should return IsHealthy = true with current metrics.
    /// An unhealthy agent should return IsHealthy = false with diagnostic information.
    /// </para>
    /// <para>
    /// Health checks can be performed on disposed agents and should return appropriate status
    /// indicating the agent is no longer available.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var health = await agent.CheckHealthAsync(cancellationToken);
    /// 
    /// Console.WriteLine($"Agent Health: {(health.IsHealthy ? "Healthy" : "Unhealthy")}");
    /// Console.WriteLine($"Status: {health.Message}");
    /// Console.WriteLine($"Check Time: {health.ResponseTime.TotalMilliseconds}ms");
    /// 
    /// if (health.Metrics.Count > 0)
    /// {
    ///     Console.WriteLine("Health Metrics:");
    ///     foreach (var metric in health.Metrics)
    ///     {
    ///         Console.WriteLine($"  {metric.Key}: {metric.Value}");
    ///     }
    /// }
    /// 
    /// // Use health status for monitoring decisions
    /// if (!health.IsHealthy)
    /// {
    ///     // Consider removing agent from load balancer rotation
    ///     // or triggering alert/remediation procedures
    ///     await NotifyMonitoringSystem(health);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="AgentHealthStatus"/>
    Task<AgentHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the health status and diagnostic information for an agent instance.
/// This class provides comprehensive information about an agent's operational state,
/// including availability, performance metrics, and diagnostic details for monitoring and troubleshooting.
/// </summary>
/// <remarks>
/// <para>
/// AgentHealthStatus is designed to support enterprise monitoring and observability requirements.
/// It provides both basic health indicators (healthy/unhealthy) and detailed metrics that can
/// be consumed by monitoring systems, load balancers, and operational dashboards.
/// </para>
/// <para>
/// Health status is typically generated quickly (under 100ms) to support high-frequency
/// health checks without impacting agent performance. Complex diagnostic operations
/// should be performed separately from basic health checks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Checking and interpreting agent health status
/// var health = await agent.CheckHealthAsync();
/// 
/// Console.WriteLine($"Agent Status: {(health.IsHealthy ? "✓ Healthy" : "✗ Unhealthy")}");
/// Console.WriteLine($"Message: {health.Message}");
/// Console.WriteLine($"Check Duration: {health.ResponseTime.TotalMilliseconds}ms");
/// Console.WriteLine($"Timestamp: {health.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
/// 
/// // Access detailed metrics
/// if (health.Metrics.ContainsKey("RequestCount"))
/// {
///     var requestCount = health.Metrics["RequestCount"];
///     Console.WriteLine($"Total Requests Processed: {requestCount}");
/// }
/// 
/// if (health.Metrics.ContainsKey("AverageResponseTime"))
/// {
///     var avgTime = health.Metrics["AverageResponseTime"];
///     Console.WriteLine($"Average Response Time: {avgTime}ms");
/// }
/// 
/// // Use for operational decisions
/// if (!health.IsHealthy)
/// {
///     logger.LogWarning("Agent {AgentId} is unhealthy: {Message}", 
///                      agent.Id, health.Message);
///     // Take remedial action like restarting or removing from rotation
/// }
/// </code>
/// </example>
public class AgentHealthStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether the agent is healthy and ready to process requests.
    /// </summary>
    /// <value>
    /// <c>true</c> if the agent is operating normally and can process requests;
    /// <c>false</c> if the agent has issues that prevent normal operation.
    /// </value>
    /// <remarks>
    /// <para>
    /// This is the primary indicator used by monitoring systems and load balancers
    /// to determine if an agent should receive traffic or be removed from rotation.
    /// </para>
    /// <para>
    /// An agent is considered healthy when:
    /// - It is properly initialized
    /// - All dependencies are accessible
    /// - Performance metrics are within acceptable ranges
    /// - No critical errors are present
    /// </para>
    /// <para>
    /// An agent is considered unhealthy when:
    /// - Initialization has failed
    /// - Critical dependencies are unavailable
    /// - Performance has degraded below acceptable thresholds
    /// - Unrecoverable errors have occurred
    /// </para>
    /// </remarks>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets a human-readable message describing the current health status.
    /// This message provides context about the agent's condition and any issues detected.
    /// </summary>
    /// <value>
    /// A descriptive message explaining the health status. For healthy agents, this typically
    /// contains a confirmation message. For unhealthy agents, this should contain diagnostic
    /// information to help with troubleshooting.
    /// </value>
    /// <remarks>
    /// <para>
    /// The message should be concise but informative, providing enough context for
    /// operators to understand the agent's state without requiring additional investigation.
    /// </para>
    /// <para>
    /// For healthy agents, typical messages might include:
    /// - "Agent is healthy and operational"
    /// - "All systems normal, processing requests"
    /// </para>
    /// <para>
    /// For unhealthy agents, messages should indicate the specific issue:
    /// - "Database connection failed"
    /// - "Memory usage exceeds threshold (85%)"
    /// - "Agent has not been initialized"
    /// </para>
    /// </remarks>
    /// <example>
    /// Examples of good health status messages:
    /// <code>
    /// // Healthy states
    /// health.Message = "Agent is healthy and operational";
    /// health.Message = "All dependencies accessible, performance normal";
    /// health.Message = "Ready to process requests";
    /// 
    /// // Unhealthy states with specific diagnostic info
    /// health.Message = "Database connection timeout after 30 seconds";
    /// health.Message = "Memory usage at 92% (threshold: 85%)";
    /// health.Message = "Agent initialization failed: missing configuration";
    /// health.Message = "External service dependency unavailable (HTTP 503)";
    /// </code>
    /// </example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this health check was performed.
    /// This provides temporal context for the health status information.
    /// </summary>
    /// <value>
    /// The UTC timestamp of when the health check was executed.
    /// Defaults to the current UTC time when the object is created.
    /// </value>
    /// <remarks>
    /// <para>
    /// The timestamp is crucial for monitoring systems to understand the freshness
    /// of health status information and detect stale health checks.
    /// </para>
    /// <para>
    /// All timestamps should be in UTC to avoid timezone-related confusion in
    /// distributed systems and monitoring dashboards.
    /// </para>
    /// <para>
    /// Monitoring systems can use this timestamp to:
    /// - Detect agents that haven't reported health status recently
    /// - Calculate health check frequency and reliability
    /// - Correlate health status with other system events
    /// </para>
    /// </remarks>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the time taken to complete the health check operation.
    /// This metric helps assess the agent's responsiveness and performance.
    /// </summary>
    /// <value>
    /// The duration of the health check operation. Should typically be under 100ms
    /// for lightweight health checks.
    /// </value>
    /// <remarks>
    /// <para>
    /// Response time is an important performance indicator for health checks.
    /// Consistently slow health checks may indicate:
    /// - Resource contention
    /// - Dependency latency issues
    /// - Agent performance degradation
    /// - Network or infrastructure problems
    /// </para>
    /// <para>
    /// Monitoring systems should track response time trends to:
    /// - Detect performance degradation early
    /// - Set appropriate health check timeouts
    /// - Identify agents requiring attention
    /// </para>
    /// <para>
    /// Typical response time ranges:
    /// - Excellent: &lt; 10ms
    /// - Good: 10-50ms
    /// - Acceptable: 50-100ms
    /// - Slow: 100-500ms
    /// - Critical: &gt; 500ms
    /// </para>
    /// </remarks>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Gets or sets additional health metrics and diagnostic information as key-value pairs.
    /// This extensible collection allows agents to report detailed operational metrics.
    /// </summary>
    /// <value>
    /// A dictionary containing metric names as keys and their corresponding values.
    /// Common metrics include performance counters, resource usage, and operational statistics.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Metrics dictionary provides a flexible way for agents to report detailed
    /// operational information beyond basic health status. This enables rich monitoring
    /// and observability without requiring changes to the core health status interface.
    /// </para>
    /// <para>
    /// Common metric categories include:
    /// - **Performance**: RequestCount, AverageResponseTime, ErrorRate, ThroughputPerSecond
    /// - **Resources**: MemoryUsageBytes, CpuUsagePercent, ThreadCount, ActiveConnections
    /// - **Dependencies**: DatabaseConnectionStatus, ExternalServiceLatency, QueueDepth
    /// - **Operational**: LastRequestTime, UptimeSeconds, ConfigurationVersion
    /// </para>
    /// <para>
    /// Metric values should use standard units and consistent naming conventions:
    /// - Use descriptive but concise names (e.g., "MemoryUsageBytes" not "Memory")
    /// - Include units in names when appropriate (Bytes, Seconds, Percent)
    /// - Use consistent casing (PascalCase recommended)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Performance metrics
    /// health.Metrics["RequestCount"] = 1247;
    /// health.Metrics["AverageResponseTimeMs"] = 45.7;
    /// health.Metrics["ErrorRate"] = 0.02; // 2% error rate
    /// health.Metrics["ThroughputPerSecond"] = 125.3;
    /// 
    /// // Resource usage metrics
    /// health.Metrics["MemoryUsageBytes"] = 134217728; // 128MB
    /// health.Metrics["CpuUsagePercent"] = 23.5;
    /// health.Metrics["ThreadCount"] = 12;
    /// health.Metrics["ActiveConnections"] = 45;
    /// 
    /// // Dependency status
    /// health.Metrics["DatabaseConnectionStatus"] = "Connected";
    /// health.Metrics["DatabaseLatencyMs"] = 12.3;
    /// health.Metrics["ExternalApiStatus"] = "Available";
    /// health.Metrics["QueueDepth"] = 3;
    /// 
    /// // Operational metrics
    /// health.Metrics["UptimeSeconds"] = 3600; // 1 hour uptime
    /// health.Metrics["LastRequestTime"] = DateTime.UtcNow.AddMinutes(-2);
    /// health.Metrics["ConfigurationVersion"] = "v1.2.3";
    /// health.Metrics["AgentVersion"] = "2.1.0";
    /// 
    /// // Custom business metrics
    /// health.Metrics["OrdersProcessedToday"] = 523;
    /// health.Metrics["DataProcessingBacklogSize"] = 12;
    /// </code>
    /// </example>
    public Dictionary<string, object> Metrics { get; set; } = new();
}