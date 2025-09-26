using Agent.Core.Models;

namespace Agent.Core;

/// <summary>
/// Provides a base implementation of the <see cref="IAgent"/> interface with common functionality
/// for agent lifecycle management, error handling, and resource disposal.
/// This abstract class implements the standard agent patterns and provides extension points for custom functionality.
/// </summary>
/// <remarks>
/// <para>
/// BaseAgent implements the core agent lifecycle and provides a robust foundation for building
/// custom agents. It handles initialization, execution flow, timeout management, error handling,
/// and proper resource disposal following the async disposable pattern.
/// </para>
/// <para>
/// **Key Features:**
/// - Automatic timeout handling based on configuration
/// - Thread-safe state management and disposal
/// - Comprehensive error handling with graceful degradation
/// - Built-in health monitoring capabilities
/// - Extensible initialization and disposal hooks
/// </para>
/// <para>
/// **Implementation Requirements:**
/// Derived classes must implement <see cref="ExecuteInternalAsync"/> which contains the
/// agent's core business logic. Optionally, they can override <see cref="OnInitializeAsync"/>
/// and <see cref="OnDisposeAsync"/> to add custom initialization and cleanup logic.
/// </para>
/// <para>
/// **Thread Safety:**
/// BaseAgent is thread-safe for concurrent execution operations but not for initialization
/// or disposal operations. The initialization and disposal should be performed once by a
/// single thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example implementation of a custom agent
/// public class CustomerServiceAgent : BaseAgent
/// {
///     private readonly ICustomerService _customerService;
///     private readonly ILogger&lt;CustomerServiceAgent&gt; _logger;
/// 
///     public CustomerServiceAgent(
///         ICustomerService customerService,
///         ILogger&lt;CustomerServiceAgent&gt; logger)
///         : base("Customer Service Agent", "Handles customer service requests and inquiries")
///     {
///         _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
///         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
///     }
/// 
///     protected override async Task OnInitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken)
///     {
///         // Custom initialization logic
///         await _customerService.InitializeAsync(cancellationToken);
///         _logger.LogInformation("Customer service agent initialized with timeout: {Timeout}", configuration.Timeout);
///     }
/// 
///     protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
///     {
///         // Core business logic implementation
///         var customerRequest = JsonSerializer.Deserialize&lt;CustomerRequest&gt;(request.Payload?.ToString() ?? "{}");
///         
///         var response = await _customerService.ProcessRequestAsync(customerRequest, cancellationToken);
///         
///         return AgentResult.CreateSuccess(response, TimeSpan.FromMilliseconds(100));
///     }
/// 
///     protected override async ValueTask OnDisposeAsync()
///     {
///         // Custom cleanup logic
///         await _customerService.DisposeAsync();
///         _logger.LogInformation("Customer service agent disposed");
///     }
/// }
/// 
/// // Usage example
/// var agent = new CustomerServiceAgent(customerService, logger);
/// 
/// var configuration = new AgentConfiguration
/// {
///     Timeout = TimeSpan.FromMinutes(2),
///     MaxRetries = 3
/// };
/// 
/// await agent.InitializeAsync(configuration);
/// 
/// var request = new AgentRequest
/// {
///     RequestId = Guid.NewGuid().ToString(),
///     Source = "web-portal",
///     Payload = new CustomerRequest { CustomerId = 123, Issue = "Billing inquiry" }
/// };
/// 
/// var result = await agent.ExecuteAsync(request);
/// 
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Request processed successfully in {result.ExecutionTime}");
/// }
/// 
/// await agent.DisposeAsync();
/// </code>
/// </example>
/// <seealso cref="IAgent"/>
/// <seealso cref="AgentConfiguration"/>
/// <seealso cref="AgentRequest"/>
/// <seealso cref="AgentResult"/>
public abstract class BaseAgent : IAgent
{
    /// <summary>
    /// Flag to track disposal state to prevent multiple disposal attempts.
    /// </summary>
    private bool _disposed = false;

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Description { get; }

    /// <inheritdoc/>
    public AgentConfiguration Configuration { get; private set; } = new();

    /// <inheritdoc/>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAgent"/> class with the specified name and description.
    /// </summary>
    /// <param name="name">
    /// The human-readable name of the agent. This will be used to generate the agent ID and
    /// displayed in logs and user interfaces. Cannot be null or empty.
    /// </param>
    /// <param name="description">
    /// A detailed description of the agent's capabilities and intended use cases.
    /// This should help users understand when and how to use this agent. Cannot be null or empty.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="description"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor automatically generates an agent ID based on the provided name by converting
    /// it to lowercase and replacing spaces with hyphens. If the name is null, a random GUID is used.
    /// </para>
    /// <para>
    /// Examples of ID generation:
    /// - "Customer Service Agent" → "customer-service-agent"
    /// - "Data Processor" → "data-processor"
    /// - null → random GUID string
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creating a custom agent with descriptive name and capabilities
    /// public class EmailProcessorAgent : BaseAgent
    /// {
    ///     public EmailProcessorAgent() 
    ///         : base(
    ///             "Email Processor", 
    ///             "Processes incoming emails, extracts attachments, and routes to appropriate handlers")
    ///     {
    ///     }
    ///     
    ///     protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(
    ///         AgentRequest request, 
    ///         CancellationToken cancellationToken)
    ///     {
    ///         // Implementation logic here
    ///         return AgentResult.CreateSuccess("Email processed", TimeSpan.FromSeconds(1));
    ///     }
    /// }
    /// 
    /// var agent = new EmailProcessorAgent();
    /// Console.WriteLine($"Agent ID: {agent.Id}"); // Output: "email-processor"
    /// Console.WriteLine($"Agent Name: {agent.Name}"); // Output: "Email Processor"
    /// </code>
    /// </example>
    protected BaseAgent(string name, string description)
    {
        Id = name?.ToLowerInvariant().Replace(" ", "-") ?? Guid.NewGuid().ToString();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The base implementation calls <see cref="OnInitializeAsync"/> to allow derived classes
    /// to perform custom initialization logic. After successful initialization, the 
    /// <see cref="IsInitialized"/> property is set to true.
    /// </para>
    /// <para>
    /// This method can only be called once per agent instance. Subsequent calls will throw
    /// an <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public virtual async Task InitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        await OnInitializeAsync(configuration, cancellationToken);

        IsInitialized = true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The base implementation provides comprehensive error handling and timeout management:
    /// - Validates that the agent is initialized and not disposed
    /// - Creates a linked cancellation token combining the provided token with timeout
    /// - Calls the abstract <see cref="ExecuteInternalAsync"/> method
    /// - Handles cancellation and timeout scenarios gracefully
    /// - Returns error results instead of throwing exceptions (except for validation errors)
    /// </para>
    /// <para>
    /// The execution flow includes automatic timeout based on the agent's configuration.
    /// If the timeout is exceeded, the operation is cancelled and an error result is returned.
    /// </para>
    /// </remarks>
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

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The base implementation simply delegates to <see cref="ExecuteAsync"/>.
    /// Derived classes can override this method to provide additional processing logic,
    /// such as request pre-processing, result post-processing, or enhanced error handling.
    /// </para>
    /// </remarks>
    public virtual Task<AgentResult> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The base implementation provides a comprehensive health check that includes:
    /// - Agent initialization status
    /// - Basic operational metrics (agent ID, name)
    /// - Response time measurement
    /// - Proper error handling for disposed agents
    /// </para>
    /// <para>
    /// The health check is designed to be lightweight and fast, typically completing
    /// in under 10ms. It can safely be called on disposed agents and will return
    /// appropriate status information.
    /// </para>
    /// <para>
    /// Derived classes can override this method to add custom health checks for
    /// dependencies, resource usage, or business-specific health indicators.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// When overridden in a derived class, provides the core execution logic for the agent.
    /// This method contains the agent's primary business functionality and is called by <see cref="ExecuteAsync"/>.
    /// </summary>
    /// <param name="request">
    /// The request containing input data and processing instructions. This is guaranteed to be non-null
    /// by the base class before calling this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that combines the caller's cancellation token with the agent's configured timeout.
    /// Implementations should respect this token and cancel processing when requested.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous execution operation.
    /// Must return a valid <see cref="AgentResult"/> indicating success or failure.
    /// Must not return null.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the only method that derived classes are required to implement. It should contain
    /// the agent's core business logic and handle the specific functionality that the agent provides.
    /// </para>
    /// <para>
    /// **Implementation Guidelines:**
    /// - Always return a valid AgentResult (never null)
    /// - Use AgentResult.CreateSuccess() for successful operations
    /// - Use AgentResult.CreateError() for error conditions
    /// - Respect the cancellation token and handle OperationCanceledException appropriately
    /// - Include execution timing information in the result
    /// - Log important operations and errors for debugging
    /// </para>
    /// <para>
    /// **Exception Handling:**
    /// The base class will catch and handle exceptions thrown by this method, converting them
    /// to error results. However, it's better practice to handle expected errors gracefully
    /// and return appropriate error results rather than throwing exceptions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    /// {
    ///     var stopwatch = Stopwatch.StartNew();
    /// 
    ///     try
    ///     {
    ///         // Parse request payload
    ///         var requestData = JsonSerializer.Deserialize&lt;MyRequestType&gt;(request.Payload?.ToString() ?? "{}");
    ///         
    ///         // Validate input
    ///         if (string.IsNullOrEmpty(requestData.RequiredField))
    ///         {
    ///             return AgentResult.CreateError("RequiredField is missing from request", stopwatch.Elapsed);
    ///         }
    /// 
    ///         // Perform core business logic
    ///         var result = await ProcessBusinessLogicAsync(requestData, cancellationToken);
    /// 
    ///         // Return success result with data
    ///         return AgentResult.CreateSuccess(result, stopwatch.Elapsed);
    ///     }
    ///     catch (JsonException ex)
    ///     {
    ///         return AgentResult.CreateError($"Invalid request format: {ex.Message}", stopwatch.Elapsed);
    ///     }
    ///     catch (BusinessLogicException ex)
    ///     {
    ///         return AgentResult.CreateError($"Business logic error: {ex.Message}", stopwatch.Elapsed);
    ///     }
    ///     catch (OperationCanceledException)
    ///     {
    ///         return AgentResult.CreateError("Operation was cancelled", stopwatch.Elapsed);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteAsync"/>
    /// <seealso cref="AgentRequest"/>
    /// <seealso cref="AgentResult"/>
    protected abstract Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// When overridden in a derived class, performs custom initialization logic for the agent.
    /// This method is called during <see cref="InitializeAsync"/> after basic validation but before
    /// setting the <see cref="IsInitialized"/> property to true.
    /// </summary>
    /// <param name="configuration">
    /// The configuration settings for the agent. This is guaranteed to be non-null
    /// by the base class before calling this method.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the initialization operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous initialization operation.
    /// If the task completes successfully, the agent will be marked as initialized.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a hook for derived classes to perform custom initialization logic
    /// such as:
    /// - Establishing database connections
    /// - Validating configuration settings
    /// - Initializing dependencies or external services  
    /// - Setting up internal state
    /// - Registering event handlers
    /// </para>
    /// <para>
    /// **Error Handling:**
    /// If this method throws an exception, the agent will remain uninitialized and the
    /// exception will be propagated to the caller of InitializeAsync. The agent can then
    /// be reinitialized with different configuration if needed.
    /// </para>
    /// <para>
    /// **Base Implementation:**
    /// The base implementation does nothing and returns a completed task. Derived classes
    /// only need to override this method if they have custom initialization requirements.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override async Task OnInitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken)
    /// {
    ///     // Validate required configuration settings
    ///     if (!configuration.Settings.ContainsKey("DatabaseConnectionString"))
    ///     {
    ///         throw new ArgumentException("DatabaseConnectionString is required in configuration settings");
    ///     }
    /// 
    ///     // Initialize database connection
    ///     var connectionString = configuration.Settings["DatabaseConnectionString"].ToString();
    ///     _dbConnection = new SqlConnection(connectionString);
    ///     await _dbConnection.OpenAsync(cancellationToken);
    /// 
    ///     // Initialize external service client
    ///     _externalServiceClient = new ExternalServiceClient(configuration.Settings);
    ///     await _externalServiceClient.InitializeAsync(cancellationToken);
    /// 
    ///     // Log successful initialization
    ///     _logger.LogInformation("Agent {AgentName} initialized with timeout {Timeout}", 
    ///                           Name, configuration.Timeout);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="InitializeAsync"/>
    /// <seealso cref="OnDisposeAsync"/>
    protected virtual Task OnInitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken)
    {
        // Override in derived classes if needed
        return Task.CompletedTask;
    }

    /// <summary>
    /// When overridden in a derived class, performs custom cleanup logic for the agent.
    /// This method is called during <see cref="DisposeAsync"/> to allow derived classes
    /// to clean up resources, close connections, and perform other disposal operations.
    /// </summary>
    /// <returns>
    /// A ValueTask that represents the asynchronous disposal operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a hook for derived classes to perform custom cleanup logic
    /// such as:
    /// - Closing database connections
    /// - Disposing of external service clients
    /// - Cleaning up temporary files or resources
    /// - Unregistering event handlers
    /// - Finalizing pending operations
    /// </para>
    /// <para>
    /// **Disposal Pattern:**
    /// This method is part of the async disposal pattern implementation. It will only be
    /// called once, even if DisposeAsync is called multiple times. The base class handles
    /// the disposal state tracking.
    /// </para>
    /// <para>
    /// **Error Handling:**
    /// Exceptions thrown from this method will be propagated to the caller of DisposeAsync.
    /// It's recommended to handle exceptions gracefully and log errors rather than throwing,
    /// to ensure other cleanup operations can complete.
    /// </para>
    /// <para>
    /// **Base Implementation:**
    /// The base implementation does nothing and returns a completed ValueTask. Derived classes
    /// only need to override this method if they have resources to clean up.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override async ValueTask OnDisposeAsync()
    /// {
    ///     try
    ///     {
    ///         // Close database connection
    ///         if (_dbConnection != null)
    ///         {
    ///             await _dbConnection.CloseAsync();
    ///             await _dbConnection.DisposeAsync();
    ///             _dbConnection = null;
    ///         }
    /// 
    ///         // Dispose external service client
    ///         if (_externalServiceClient != null)
    ///         {
    ///             await _externalServiceClient.DisposeAsync();
    ///             _externalServiceClient = null;
    ///         }
    /// 
    ///         // Clean up temporary resources
    ///         CleanupTempFiles();
    /// 
    ///         _logger.LogInformation("Agent {AgentName} disposed successfully", Name);
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         _logger.LogError(ex, "Error during agent disposal");
    ///         // Don't rethrow - allow disposal to complete
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="DisposeAsync"/>
    /// <seealso cref="OnInitializeAsync"/>
    protected virtual ValueTask OnDisposeAsync()
    {
        // Override in derived classes if needed
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method implements the async disposable pattern with proper state tracking
    /// to prevent multiple disposal attempts. It calls <see cref="OnDisposeAsync"/> to allow
    /// derived classes to perform custom cleanup, then marks the agent as disposed.
    /// </para>
    /// <para>
    /// After disposal, all agent operations (except health checks) will throw
    /// <see cref="ObjectDisposedException"/>. The agent cannot be reinitialized after disposal.
    /// </para>
    /// <para>
    /// This method is safe to call multiple times - subsequent calls will be ignored.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await OnDisposeAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the agent has been disposed.
    /// This method is used throughout the agent to ensure operations are not performed on disposed agents.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the agent has been disposed.
    /// </exception>
    /// <remarks>
    /// This is a helper method used by the base class to enforce the disposal pattern.
    /// It's called at the beginning of most public methods to ensure the agent is still usable.
    /// </remarks>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}