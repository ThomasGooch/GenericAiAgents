namespace Agent.Core.Models;

/// <summary>
/// Represents configuration settings for an agent instance, including operational parameters,
/// custom settings, and behavioral controls. This class provides comprehensive configuration
/// options for agent initialization, execution timeouts, retry policies, and extensible settings.
/// </summary>
/// <remarks>
/// <para>
/// AgentConfiguration is the primary mechanism for customizing agent behavior and providing
/// runtime settings. It supports both standard operational parameters (timeout, retries) and
/// extensible custom settings through the Settings dictionary.
/// </para>
/// <para>
/// **Configuration Lifecycle:**
/// Configuration is typically created before agent initialization and remains immutable
/// during the agent's lifetime. To change configuration, agents must be disposed and
/// recreated with new configuration settings.
/// </para>
/// <para>
/// **Validation:**
/// Configuration values should be validated by the consuming agent during initialization.
/// Invalid configuration should result in initialization failure with descriptive error messages.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic agent configuration with standard settings
/// var basicConfig = new AgentConfiguration
/// {
///     Name = "Customer Service Agent",
///     Description = "Handles customer inquiries and support requests",
///     Timeout = TimeSpan.FromMinutes(2),
///     MaxRetries = 3
/// };
/// 
/// // Advanced configuration with custom settings for specialized agents
/// var advancedConfig = new AgentConfiguration
/// {
///     Name = "Data Processing Agent",
///     Description = "Processes large datasets with AI analysis",
///     Timeout = TimeSpan.FromMinutes(30), // Longer timeout for data processing
///     MaxRetries = 1, // Fewer retries for expensive operations
///     Settings = new Dictionary&lt;string, object&gt;
///     {
///         ["DatabaseConnectionString"] = "Server=localhost;Database=Analytics;",
///         ["BatchSize"] = 1000,
///         ["EnableParallelProcessing"] = true,
///         ["MaxConcurrency"] = 4,
///         ["TempDirectory"] = "/tmp/agent-processing",
///         ["LogLevel"] = "Information"
///     },
///     RequiredTools = new List&lt;string&gt; { "database-tool", "file-processor", "data-validator" }
/// };
/// 
/// // Configuration for external service integration
/// var serviceConfig = new AgentConfiguration
/// {
///     Name = "External API Agent",
///     Description = "Integrates with external REST APIs",
///     Timeout = TimeSpan.FromSeconds(30),
///     MaxRetries = 5, // More retries for network operations
///     Settings = new Dictionary&lt;string, object&gt;
///     {
///         ["ApiBaseUrl"] = "https://api.external-service.com",
///         ["ApiKey"] = "your-api-key-here",
///         ["RateLimitPerSecond"] = 10,
///         ["RetryDelayMs"] = 1000,
///         ["EnableCircuitBreaker"] = true,
///         ["CircuitBreakerThreshold"] = 5
///     }
/// };
/// 
/// // Using configuration with agents
/// var agent = new MyCustomAgent();
/// await agent.InitializeAsync(advancedConfig);
/// 
/// // Configuration can be serialized/deserialized for persistence
/// var json = JsonSerializer.Serialize(basicConfig, new JsonSerializerOptions { WriteIndented = true });
/// var deserializedConfig = JsonSerializer.Deserialize&lt;AgentConfiguration&gt;(json);
/// </code>
/// </example>
public class AgentConfiguration
{
    /// <summary>
    /// Gets or sets the name of the agent configuration.
    /// This is typically the same as the agent name but can be customized for different deployment scenarios.
    /// </summary>
    /// <value>
    /// A descriptive name for this configuration. Can be empty if not specified.
    /// </value>
    /// <remarks>
    /// <para>
    /// The configuration name is useful for:
    /// - Identifying configurations in logs and monitoring
    /// - Supporting multiple configurations for the same agent type
    /// - Configuration management and deployment scenarios
    /// </para>
    /// <para>
    /// Examples:
    /// - "Production Database Agent Config"
    /// - "Development API Integration Config"
    /// - "High Performance Data Processing Config"
    /// </para>
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a detailed description of this configuration and its intended use case.
    /// This helps operators understand when and how to use this configuration.
    /// </summary>
    /// <value>
    /// A comprehensive description of the configuration's purpose, settings, and usage scenarios.
    /// Can be empty if not specified.
    /// </value>
    /// <remarks>
    /// The description should include:
    /// - What type of workload this configuration is optimized for
    /// - Any special requirements or dependencies
    /// - Performance characteristics or limitations
    /// - Deployment or environment-specific notes
    /// </remarks>
    /// <example>
    /// <code>
    /// config.Description = @"
    /// High-performance configuration for batch data processing agents.
    /// - Optimized for large datasets (1M+ records)
    /// - Requires dedicated database connection pool
    /// - Uses parallel processing with 8-core CPU minimum
    /// - Suitable for overnight batch jobs and ETL operations
    /// ";
    /// </code>
    /// </example>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a dictionary of custom configuration settings specific to the agent implementation.
    /// This provides an extensible mechanism for passing agent-specific configuration parameters.
    /// </summary>
    /// <value>
    /// A dictionary containing custom settings as key-value pairs. Keys should use descriptive names
    /// and values can be any serializable object. Defaults to an empty dictionary.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Settings dictionary is the primary extension point for agent-specific configuration.
    /// It allows agents to receive custom parameters without modifying the core configuration structure.
    /// </para>
    /// <para>
    /// **Best Practices for Settings:**
    /// - Use descriptive, PascalCase key names (e.g., "DatabaseConnectionString", "MaxBatchSize")
    /// - Include data type and units in key names when helpful (e.g., "TimeoutSeconds", "MaxMemoryMB")
    /// - Use consistent naming patterns across related agents
    /// - Validate all settings during agent initialization
    /// - Provide sensible defaults when settings are missing
    /// </para>
    /// <para>
    /// **Common Setting Categories:**
    /// - **Connection Strings**: Database, message queue, external service URLs
    /// - **Performance Parameters**: Batch sizes, concurrency limits, memory limits
    /// - **Feature Flags**: Enable/disable optional functionality
    /// - **File Paths**: Working directories, log files, temporary storage
    /// - **Credentials**: API keys, service account information (consider using ISecretManager)
    /// - **Thresholds**: Error rates, performance limits, capacity thresholds
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Performance and capacity settings
    /// config.Settings["MaxConcurrentRequests"] = 10;
    /// config.Settings["BatchSizeLimit"] = 1000;
    /// config.Settings["MaxMemoryUsageMB"] = 512;
    /// config.Settings["EnableCaching"] = true;
    /// 
    /// // External service configuration
    /// config.Settings["DatabaseConnectionString"] = connectionString;
    /// config.Settings["ApiEndpoint"] = "https://api.service.com/v1";
    /// config.Settings["ApiTimeoutSeconds"] = 30;
    /// config.Settings["RetryAttempts"] = 3;
    /// 
    /// // File and directory paths
    /// config.Settings["WorkingDirectory"] = "/app/data/processing";
    /// config.Settings["LogFilePath"] = "/var/log/agent.log";
    /// config.Settings["TempDirectory"] = "/tmp/agent-temp";
    /// 
    /// // Business logic parameters
    /// config.Settings["DefaultLanguage"] = "en-US";
    /// config.Settings["EnableAdvancedAnalytics"] = false;
    /// config.Settings["DataRetentionDays"] = 90;
    /// 
    /// // Accessing settings in agent implementation
    /// protected override async Task OnInitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken)
    /// {
    ///     // Extract and validate required settings
    ///     if (!configuration.Settings.TryGetValue("DatabaseConnectionString", out var connStringObj))
    ///     {
    ///         throw new ArgumentException("DatabaseConnectionString is required in configuration settings");
    ///     }
    ///     
    ///     var connectionString = connStringObj.ToString();
    ///     var batchSize = configuration.Settings.TryGetValue("BatchSizeLimit", out var batchObj) 
    ///                     ? Convert.ToInt32(batchObj) 
    ///                     : 100; // Default value
    ///     
    ///     // Initialize with extracted settings
    ///     await InitializeDatabaseConnection(connectionString);
    ///     SetBatchSize(batchSize);
    /// }
    /// </code>
    /// </example>
    public Dictionary<string, object> Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of tool names that are required for this agent to function properly.
    /// These tools must be registered and available during agent initialization.
    /// </summary>
    /// <value>
    /// A list of tool names (identifiers) that the agent depends on. Defaults to an empty list.
    /// Tool names should match the names used in tool registration.
    /// </value>
    /// <remarks>
    /// <para>
    /// Required tools represent hard dependencies that must be available for the agent
    /// to initialize successfully. During initialization, the agent framework will verify
    /// that all required tools are registered and accessible.
    /// </para>
    /// <para>
    /// **Tool Naming Conventions:**
    /// - Use lowercase with hyphens (e.g., "database-tool", "file-processor")
    /// - Use descriptive names that clearly indicate functionality
    /// - Maintain consistency across agents and deployments
    /// </para>
    /// <para>
    /// **Dependency Management:**
    /// - Tools should be registered before agent initialization
    /// - Missing required tools will cause initialization failure
    /// - Consider using optional tools through the Settings dictionary for non-critical dependencies
    /// - Tool dependencies can affect agent deployment and scaling strategies
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic tool dependencies
    /// config.RequiredTools = new List&lt;string&gt;
    /// {
    ///     "database-tool",           // For data persistence
    ///     "email-sender",           // For notifications
    ///     "pdf-generator"           // For report creation
    /// };
    /// 
    /// // Advanced agent with multiple tool categories
    /// config.RequiredTools = new List&lt;string&gt;
    /// {
    ///     // Data processing tools
    ///     "csv-parser",
    ///     "json-transformer",
    ///     "data-validator",
    ///     
    ///     // External integrations
    ///     "rest-client",
    ///     "ftp-client",
    ///     "message-queue-client",
    ///     
    ///     // File operations
    ///     "file-archiver",
    ///     "image-processor",
    ///     "document-converter"
    /// };
    /// 
    /// // Agent initialization with tool validation
    /// public override async Task InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken)
    /// {
    ///     // Framework automatically validates required tools are available
    ///     await base.InitializeAsync(configuration, cancellationToken);
    ///     
    ///     // Access tools after validation
    ///     _databaseTool = toolRegistry.GetTool("database-tool");
    ///     _emailSender = toolRegistry.GetTool("email-sender");
    ///     
    ///     // Tools are guaranteed to be available at this point
    /// }
    /// </code>
    /// </example>
    public List<string> RequiredTools { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum time allowed for agent execution operations.
    /// This timeout applies to individual execution requests and helps prevent runaway operations.
    /// </summary>
    /// <value>
    /// The timeout duration for agent execution operations. 
    /// Defaults to 5 minutes. Must be a positive value.
    /// </value>
    /// <remarks>
    /// <para>
    /// The timeout value is critical for operational stability and resource management:
    /// - **Execution Timeout**: Applied to individual ExecuteAsync calls
    /// - **Automatic Cancellation**: Operations exceeding timeout are automatically cancelled
    /// - **Resource Protection**: Prevents agents from consuming resources indefinitely
    /// - **SLA Compliance**: Ensures predictable response times for downstream systems
    /// </para>
    /// <para>
    /// **Timeout Selection Guidelines:**
    /// - **Interactive Operations**: 30 seconds to 2 minutes for user-facing operations
    /// - **Data Processing**: 5 minutes to 1 hour depending on data volume
    /// - **External API Calls**: 30 seconds to 5 minutes based on service SLAs
    /// - **File Processing**: Scale with expected file sizes and processing complexity
    /// - **Machine Learning**: Can range from minutes to hours for training operations
    /// </para>
    /// <para>
    /// **Considerations:**
    /// - Set timeouts based on 95th percentile expected execution times
    /// - Account for network latency and external service response times  
    /// - Consider retry behavior when setting timeouts
    /// - Monitor timeout occurrences to adjust values appropriately
    /// - Balance between operational stability and functionality
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Interactive web request processing
    /// config.Timeout = TimeSpan.FromSeconds(30);
    /// 
    /// // Standard business logic processing
    /// config.Timeout = TimeSpan.FromMinutes(5);
    /// 
    /// // Large data processing operations
    /// config.Timeout = TimeSpan.FromMinutes(30);
    /// 
    /// // Long-running batch operations
    /// config.Timeout = TimeSpan.FromHours(2);
    /// 
    /// // Very short timeout for health checks
    /// var healthCheckConfig = new AgentConfiguration
    /// {
    ///     Timeout = TimeSpan.FromSeconds(10)
    /// };
    /// 
    /// // Timeout handling in agent implementation
    /// protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    /// {
    ///     try
    ///     {
    ///         // This operation is subject to the configured timeout
    ///         var result = await LongRunningOperation(cancellationToken);
    ///         return AgentResult.CreateSuccess(result, stopwatch.Elapsed);
    ///     }
    ///     catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    ///     {
    ///         // Handle timeout gracefully
    ///         return AgentResult.CreateError($"Operation timed out after {Configuration.Timeout}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed operations.
    /// This applies to automatic retry logic implemented by the agent or framework.
    /// </summary>
    /// <value>
    /// The maximum number of retry attempts. Defaults to 3. 
    /// A value of 0 disables retries. Must be non-negative.
    /// </value>
    /// <remarks>
    /// <para>
    /// Retry configuration is essential for building resilient agents that can handle
    /// transient failures gracefully. The retry behavior depends on the agent implementation
    /// and the type of operations being performed.
    /// </para>
    /// <para>
    /// **Retry Strategy Considerations:**
    /// - **Transient vs. Permanent Failures**: Only retry transient failures (network issues, temporary service unavailability)
    /// - **Exponential Backoff**: Implement increasing delays between retries to avoid overwhelming failing services
    /// - **Circuit Breaker Pattern**: Consider implementing circuit breakers for repeated failures
    /// - **Cost Implications**: Each retry consumes resources and may have financial costs
    /// - **User Experience**: Balance retry attempts against response time expectations
    /// </para>
    /// <para>
    /// **Recommended Retry Counts by Operation Type:**
    /// - **Database Operations**: 3-5 retries for transient connection issues
    /// - **HTTP API Calls**: 3-5 retries for network and server errors (5xx)
    /// - **File Operations**: 2-3 retries for temporary file locks or permissions
    /// - **Message Queue Operations**: 3-5 retries for connection and delivery issues
    /// - **Expensive Computations**: 0-1 retries due to resource costs
    /// - **User Input Validation**: 0 retries (permanent failures)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // High reliability external service integration
    /// config.MaxRetries = 5;
    /// 
    /// // Standard business operations
    /// config.MaxRetries = 3;
    /// 
    /// // Expensive operations with limited retries
    /// config.MaxRetries = 1;
    /// 
    /// // Operations that should not be retried
    /// config.MaxRetries = 0;
    /// 
    /// // Implementing retry logic in agent
    /// protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    /// {
    ///     var maxRetries = Configuration.MaxRetries;
    ///     var attempt = 0;
    ///     Exception lastException = null;
    /// 
    ///     while (attempt &lt;= maxRetries)
    ///     {
    ///         try
    ///         {
    ///             var result = await PerformOperation(request, cancellationToken);
    ///             return AgentResult.CreateSuccess(result, stopwatch.Elapsed);
    ///         }
    ///         catch (TransientException ex) when (attempt &lt; maxRetries)
    ///         {
    ///             lastException = ex;
    ///             attempt++;
    ///             
    ///             // Exponential backoff with jitter
    ///             var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 1000 + Random.Next(0, 1000));
    ///             await Task.Delay(delay, cancellationToken);
    ///             
    ///             _logger.LogWarning("Operation failed, attempt {Attempt}/{MaxRetries}: {Error}", 
    ///                              attempt, maxRetries, ex.Message);
    ///         }
    ///         catch (PermanentException ex)
    ///         {
    ///             // Don't retry permanent failures
    ///             return AgentResult.CreateError($"Permanent failure: {ex.Message}", stopwatch.Elapsed);
    ///         }
    ///     }
    /// 
    ///     return AgentResult.CreateError($"Operation failed after {maxRetries} retries: {lastException?.Message}", 
    ///                                   stopwatch.Elapsed);
    /// }
    /// </code>
    /// </example>
    public int MaxRetries { get; set; } = 3;
}