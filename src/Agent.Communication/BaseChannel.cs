using Agent.Communication.Models;

namespace Agent.Communication;

/// <summary>
/// Provides a base implementation for communication channels that establishes common functionality
/// for connection management, error handling, and lifecycle management. Derived classes implement
/// transport-specific logic while inheriting standardized patterns for reliability and consistency.
/// </summary>
/// <remarks>
/// <para>
/// The BaseChannel class implements the Template Method pattern to provide a robust foundation
/// for communication channel implementations. It handles common concerns like disposal patterns,
/// connection state management, error propagation, and event notification while allowing
/// derived classes to focus on transport-specific implementation details.
/// </para>
/// <para>
/// **Key Features:**
/// - **Lifecycle Management**: Proper initialization, connection, and disposal patterns
/// - **Error Handling**: Comprehensive exception handling with error event notifications
/// - **State Management**: Connection state tracking with automatic validation
/// - **Event Propagation**: Standardized event handling for status changes and errors
/// - **Thread Safety**: Safe disposal and state management for concurrent usage
/// </para>
/// <para>
/// **Implementation Pattern:**
/// Derived classes must implement abstract methods for transport-specific operations:
/// - OnConnectAsync/OnDisconnectAsync: Transport connection management
/// - OnSendRequestAsync/OnSendAsync: Message transmission logic
/// - OnStartListeningAsync/OnStopListeningAsync: Request handling lifecycle
/// </para>
/// <para>
/// **Disposal and Cleanup:**
/// The class implements proper IAsyncDisposable patterns, ensuring connections are
/// gracefully closed and resources are cleaned up. Derived classes can override
/// OnDisposeAsync to perform additional cleanup operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example derived channel implementation
/// public class CustomChannel : BaseChannel
/// {
///     private readonly ICustomTransport _transport;
///     private readonly ILogger&lt;CustomChannel&gt; _logger;
///     
///     public CustomChannel(ICustomTransport transport, ILogger&lt;CustomChannel&gt; logger) 
///         : base("custom-channel")
///     {
///         _transport = transport ?? throw new ArgumentNullException(nameof(transport));
///         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
///     }
///     
///     public override bool IsConnected =&gt; _transport.IsConnected;
///     
///     protected override async Task OnConnectAsync(CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("Connecting to transport endpoint");
///         await _transport.ConnectAsync(Configuration, cancellationToken);
///         _logger.LogInformation("Transport connection established");
///     }
///     
///     protected override async Task OnDisconnectAsync(CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("Disconnecting from transport");
///         await _transport.DisconnectAsync(cancellationToken);
///     }
///     
///     protected override async Task&lt;CommunicationResponse&gt; OnSendRequestAsync(
///         CommunicationRequest request, CancellationToken cancellationToken)
///     {
///         var transportResponse = await _transport.SendAsync(request, cancellationToken);
///         return MapToResponse(transportResponse, request);
///     }
/// }
/// 
/// // Usage with proper lifecycle management
/// var channel = new CustomChannel(transport, logger);
/// 
/// await channel.InitializeAsync(new Dictionary&lt;string, object&gt;
/// {
///     ["Endpoint"] = "https://api.example.com",
///     ["Timeout"] = TimeSpan.FromSeconds(30)
/// });
/// 
/// await channel.ConnectAsync();
/// 
/// // Channel is now ready for communication
/// var response = await channel.SendRequestAsync(request);
/// 
/// // Cleanup with proper disposal
/// await channel.DisposeAsync();
/// </code>
/// </example>
/// <seealso cref="ICommunicationChannel"/>
/// <seealso cref="InMemoryChannel"/>
/// <seealso cref="CommunicationRequest"/>
/// <seealso cref="CommunicationResponse"/>
public abstract class BaseChannel : ICommunicationChannel
{
    private bool _disposed = false;

    /// <inheritdoc/>
    public string ChannelId { get; }

    /// <inheritdoc/>
    public abstract bool IsConnected { get; }

    /// <inheritdoc/>
    public Dictionary<string, object> Configuration { get; private set; } = new();

    /// <inheritdoc/>
    public event EventHandler<bool>? ConnectionStatusChanged;

    /// <inheritdoc/>
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Initializes a new instance of the BaseChannel class with an optional channel identifier.
    /// If no identifier is provided, generates a unique identifier based on the class name.
    /// </summary>
    /// <param name="channelId">
    /// Optional channel identifier. If null, generates a unique identifier using the format
    /// "{ClassName}-{Guid}" to ensure uniqueness across channel instances.
    /// </param>
    /// <remarks>
    /// The channel identifier is used for logging, monitoring, and debugging purposes.
    /// It should be unique within the scope of the application to facilitate traceability
    /// of communication operations and error reporting.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create channel with custom identifier
    /// var channel = new MyChannel("payment-processor-channel");
    /// 
    /// // Create channel with auto-generated identifier
    /// var autoChannel = new MyChannel(); // Results in "MyChannel-{guid}"
    /// </code>
    /// </example>
    protected BaseChannel(string? channelId = null)
    {
        ChannelId = channelId ?? $"{GetType().Name}-{Guid.NewGuid():N}";
    }

    /// <inheritdoc/>
    public virtual async Task InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        await OnInitializeAsync(configuration, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsConnected)
            return;

        try
        {
            await OnConnectAsync(cancellationToken);
            OnConnectionStatusChanged(true);
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Connection failed: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
            return;

        try
        {
            await OnDisconnectAsync(cancellationToken);
            OnConnectionStatusChanged(false);
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Disconnection failed: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CommunicationResponse> SendRequestAsync(CommunicationRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(request);

        if (!IsConnected)
            throw new InvalidOperationException("Channel is not connected");

        try
        {
            return await OnSendRequestAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Send request failed: {ex.Message}");
            return CommunicationResponse.CreateError(request.Id, ex.Message, ChannelId, request.Target);
        }
    }

    /// <inheritdoc/>
    public async Task SendAsync(CommunicationRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(request);

        if (!IsConnected)
            throw new InvalidOperationException("Channel is not connected");

        try
        {
            await OnSendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Send failed: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StartListeningAsync(Func<CommunicationRequest, Task<CommunicationResponse>> requestHandler, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(requestHandler);

        if (!IsConnected)
            throw new InvalidOperationException("Channel is not connected");

        try
        {
            await OnStartListeningAsync(requestHandler, cancellationToken);
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Start listening failed: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StopListeningAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            await OnStopListeningAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Stop listening failed: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                if (IsConnected)
                {
                    await DisconnectAsync();
                }

                await OnDisposeAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Dispose failed: {ex.Message}");
            }

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    #region Abstract Methods - Must be implemented by derived classes

    /// <summary>
    /// Establishes the transport-specific connection when ConnectAsync is called.
    /// Derived classes implement the actual connection logic for their transport mechanism.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the connection operation.</param>
    /// <returns>A task that completes when the connection is established.</returns>
    /// <remarks>
    /// This method is called by the base ConnectAsync method after connection state validation.
    /// Implementations should establish the actual transport connection (TCP, HTTP, WebSocket, etc.)
    /// and throw appropriate exceptions for connection failures.
    /// </remarks>
    protected abstract Task OnConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Closes the transport-specific connection when DisconnectAsync is called.
    /// Derived classes implement the actual disconnection logic for their transport mechanism.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the disconnection operation.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    /// <remarks>
    /// This method is called by the base DisconnectAsync method after connection state validation.
    /// Implementations should gracefully close transport connections and release associated resources.
    /// </remarks>
    protected abstract Task OnDisconnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends a request and waits for a response using the transport-specific mechanism.
    /// This method handles the actual request-response communication pattern.
    /// </summary>
    /// <param name="request">The communication request to send.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the send operation.</param>
    /// <returns>The communication response received from the target.</returns>
    /// <remarks>
    /// Implementations must handle request serialization, transport-level sending,
    /// response waiting, and response deserialization. Error responses should be
    /// returned as CommunicationResponse objects rather than throwing exceptions.
    /// </remarks>
    protected abstract Task<CommunicationResponse> OnSendRequestAsync(CommunicationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a fire-and-forget message using the transport-specific mechanism.
    /// This method handles one-way communication without expecting a response.
    /// </summary>
    /// <param name="request">The communication request to send.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the send operation.</param>
    /// <returns>A task that completes when the message has been sent.</returns>
    /// <remarks>
    /// Implementations should send the message without waiting for responses.
    /// This is typically used for notifications, events, or scenarios where
    /// response handling is not required or handled separately.
    /// </remarks>
    protected abstract Task OnSendAsync(CommunicationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Begins listening for incoming requests using the transport-specific mechanism.
    /// Sets up the request handler to process incoming communication requests.
    /// </summary>
    /// <param name="requestHandler">Function to handle incoming requests and generate responses.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the listening operation.</param>
    /// <returns>A task that completes when listening has started.</returns>
    /// <remarks>
    /// Implementations should establish the transport-specific listening mechanism
    /// (server socket, HTTP listener, message queue subscription, etc.) and route
    /// incoming requests to the provided handler function.
    /// </remarks>
    protected abstract Task OnStartListeningAsync(Func<CommunicationRequest, Task<CommunicationResponse>> requestHandler, CancellationToken cancellationToken);

    /// <summary>
    /// Stops listening for incoming requests and closes the listening transport mechanism.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the stop operation.</param>
    /// <returns>A task that completes when listening has stopped.</returns>
    /// <remarks>
    /// Implementations should gracefully stop accepting new requests and close
    /// listening resources. In-flight requests should be allowed to complete
    /// or be cancelled based on the cancellation token.
    /// </remarks>
    protected abstract Task OnStopListeningAsync(CancellationToken cancellationToken);

    #endregion

    #region Virtual Methods - Can be overridden by derived classes

    /// <summary>
    /// Performs transport-specific initialization when InitializeAsync is called.
    /// Derived classes can override to perform custom initialization logic.
    /// </summary>
    /// <param name="configuration">The configuration dictionary containing initialization parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the initialization operation.</param>
    /// <returns>A task that completes when initialization is finished.</returns>
    /// <remarks>
    /// The base implementation does nothing. Override this method to perform
    /// transport-specific setup, validate configuration parameters, or establish
    /// required resources before connection attempts.
    /// </remarks>
    protected virtual Task OnInitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken)
    {
        // Override in derived classes if needed
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs transport-specific cleanup when DisposeAsync is called.
    /// Derived classes can override to release additional resources.
    /// </summary>
    /// <returns>A ValueTask that completes when cleanup is finished.</returns>
    /// <remarks>
    /// The base implementation does nothing. Override this method to release
    /// transport-specific resources, close connections, or perform final cleanup
    /// operations that are not covered by the standard disposal pattern.
    /// </remarks>
    protected virtual ValueTask OnDisposeAsync()
    {
        // Override in derived classes if needed
        return ValueTask.CompletedTask;
    }

    #endregion

    /// <summary>
    /// Raises the ConnectionStatusChanged event to notify subscribers of connection state changes.
    /// Derived classes can override to add custom logic when connection status changes.
    /// </summary>
    /// <param name="isConnected">True if the channel is now connected; false if disconnected.</param>
    /// <remarks>
    /// This method is called automatically by the base class when connection state changes.
    /// Override to add logging, metrics collection, or other custom behavior when
    /// connection status changes, but ensure the base implementation is called
    /// to maintain proper event notification.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnConnectionStatusChanged(bool isConnected)
    /// {
    ///     _logger.LogInformation("Channel {ChannelId} connection status changed to {Status}", 
    ///                           ChannelId, isConnected ? "Connected" : "Disconnected");
    ///     _metrics.IncrementCounter($"channel.connection.{(isConnected ? "connected" : "disconnected")}", 1);
    ///     
    ///     // Call base to ensure event is raised
    ///     base.OnConnectionStatusChanged(isConnected);
    /// }
    /// </code>
    /// </example>
    protected virtual void OnConnectionStatusChanged(bool isConnected)
    {
        ConnectionStatusChanged?.Invoke(this, isConnected);
    }

    /// <summary>
    /// Raises the ErrorOccurred event to notify subscribers of communication errors.
    /// Derived classes can override to add custom error handling logic.
    /// </summary>
    /// <param name="error">Descriptive error message explaining what went wrong.</param>
    /// <remarks>
    /// This method is called automatically by the base class when errors occur during
    /// communication operations. Override to add logging, error tracking, or custom
    /// error handling, but ensure the base implementation is called to maintain
    /// proper error event notification.
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnErrorOccurred(string error)
    /// {
    ///     _logger.LogError("Communication error on channel {ChannelId}: {Error}", ChannelId, error);
    ///     _metrics.IncrementCounter("channel.errors", 1);
    ///     
    ///     // Call base to ensure event is raised
    ///     base.OnErrorOccurred(error);
    /// }
    /// </code>
    /// </example>
    protected virtual void OnErrorOccurred(string error)
    {
        ErrorOccurred?.Invoke(this, error);
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the channel has been disposed.
    /// This method is called at the beginning of public methods to prevent operations on disposed objects.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the channel has been disposed and can no longer be used.
    /// </exception>
    /// <remarks>
    /// This is a defensive programming practice to ensure that disposed channels
    /// cannot be used, preventing undefined behavior and resource access violations.
    /// All public methods should call this method before performing any operations.
    /// </remarks>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}