using Agent.Communication.Models;
using System.Collections.Concurrent;

namespace Agent.Communication;

/// <summary>
/// Provides an in-memory communication channel implementation optimized for testing, development,
/// and scenarios requiring high-performance local inter-agent communication without network overhead.
/// Uses shared static registry for multi-channel communication simulation and concurrent message queuing.
/// </summary>
/// <remarks>
/// <para>
/// The InMemoryChannel class offers a lightweight, thread-safe implementation of the communication
/// channel interface that operates entirely within the application memory space. This makes it ideal
/// for unit testing, integration testing, development environments, and high-throughput scenarios
/// where network latency would be prohibitive.
/// </para>
/// <para>
/// **Key Features:**
/// - **Zero Network Latency**: All communication happens in-process for maximum performance
/// - **Shared Channel Registry**: Static registry enables multiple channels to discover and communicate
/// - **Concurrent Message Processing**: Uses ConcurrentQueue for thread-safe message handling
/// - **Background Processing**: Queued fire-and-forget messages processed asynchronously
/// - **Testing Support**: Registry inspection and clearing methods for test isolation
/// </para>
/// <para>
/// **Communication Patterns:**
/// - **Request-Response**: Direct synchronous processing with immediate responses
/// - **Fire-and-Forget**: Asynchronous queuing with background processing
/// - **Multi-Channel**: Multiple channels can communicate through shared registry
/// - **Error Simulation**: Supports error scenarios for testing failure cases
/// </para>
/// <para>
/// **Thread Safety:**
/// The implementation uses thread-safe collections (ConcurrentDictionary, ConcurrentQueue)
/// and proper synchronization to support concurrent operations from multiple threads
/// while maintaining message ordering and delivery guarantees.
/// </para>
/// <para>
/// **Use Cases:**
/// - Unit and integration testing of agent communication
/// - Development environment with multiple agents
/// - High-performance in-process communication scenarios
/// - Communication pattern prototyping and validation
/// - Load testing without network infrastructure dependencies
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic in-memory channel setup for testing
/// var agentA = new InMemoryChannel("agent-a");
/// var agentB = new InMemoryChannel("agent-b");
/// 
/// // Initialize and connect both channels
/// await agentA.InitializeAsync(new Dictionary&lt;string, object&gt;());
/// await agentB.InitializeAsync(new Dictionary&lt;string, object&gt;());
/// await agentA.ConnectAsync();
/// await agentB.ConnectAsync();
/// 
/// // Set up agent B to handle incoming requests
/// await agentB.StartListeningAsync(async (request) =&gt;
/// {
///     Console.WriteLine($"Agent B received: {request.MessageType}");
///     
///     return new CommunicationResponse
///     {
///         Id = Guid.NewGuid().ToString(),
///         RequestId = request.Id,
///         IsSuccess = true,
///         Payload = new { Result = "Processed by Agent B", Timestamp = DateTime.UtcNow },
///         Source = "agent-b",
///         Target = request.Source
///     };
/// });
/// 
/// // Agent A sends request to Agent B
/// var request = new CommunicationRequest
/// {
///     Id = Guid.NewGuid().ToString(),
///     Source = "agent-a",
///     Target = "agent-b",
///     MessageType = "ProcessData",
///     Payload = new { Data = "important information", Priority = "High" }
/// };
/// 
/// var response = await agentA.SendRequestAsync(request);
/// Console.WriteLine($"Response: {response.Payload}");
/// 
/// // Fire-and-forget messaging
/// var notification = new CommunicationRequest
/// {
///     Id = Guid.NewGuid().ToString(),
///     Source = "agent-a",
///     Target = "agent-b",
///     MessageType = "Notification",
///     Payload = new { Message = "Task completed successfully" }
/// };
/// 
/// await agentA.SendAsync(notification);
/// 
/// // Multi-channel communication testing
/// var channels = InMemoryChannel.GetRegisteredChannels();
/// Console.WriteLine($"Active channels: {string.Join(", ", channels)}");
/// 
/// // Cleanup for test isolation
/// await agentA.DisposeAsync();
/// await agentB.DisposeAsync();
/// InMemoryChannel.ClearRegistry();
/// </code>
/// </example>
/// <seealso cref="BaseChannel"/>
/// <seealso cref="ICommunicationChannel"/>
/// <seealso cref="CommunicationRequest"/>
/// <seealso cref="CommunicationResponse"/>
public class InMemoryChannel : BaseChannel
{
    /// <summary>
    /// Indicates whether this channel is currently connected and ready for communication.
    /// </summary>
    private bool _isConnected = false;

    /// <summary>
    /// Indicates whether this channel is currently listening for incoming requests.
    /// </summary>
    private bool _isListening = false;

    /// <summary>
    /// Handler function for processing incoming communication requests when listening.
    /// </summary>
    private Func<CommunicationRequest, Task<CommunicationResponse>>? _requestHandler;

    /// <summary>
    /// Thread-safe queue for storing fire-and-forget messages awaiting background processing.
    /// </summary>
    private readonly ConcurrentQueue<CommunicationRequest> _messageQueue = new();

    /// <summary>
    /// Static registry that maintains all active InMemoryChannel instances, enabling
    /// inter-channel communication discovery and message routing within the application domain.
    /// Thread-safe dictionary keyed by channel identifier.
    /// </summary>
    /// <remarks>
    /// This registry serves as a simple service discovery mechanism for in-memory channels,
    /// allowing channels to find and communicate with each other without external infrastructure.
    /// The registry is automatically managed during channel connection/disconnection lifecycle.
    /// </remarks>
    private static readonly ConcurrentDictionary<string, InMemoryChannel> _channelRegistry = new();

    /// <inheritdoc/>
    public override bool IsConnected => _isConnected;

    /// <summary>
    /// Initializes a new instance of the InMemoryChannel class with an optional channel identifier.
    /// </summary>
    /// <param name="channelId">
    /// Optional unique identifier for this channel. If null, a unique identifier will be
    /// automatically generated based on the class name and a GUID.
    /// </param>
    /// <remarks>
    /// The channel identifier is used for registration in the shared channel registry,
    /// enabling other channels to discover and send messages to this channel.
    /// Choose meaningful identifiers that reflect the agent or service this channel represents.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create channels with meaningful identifiers
    /// var orderProcessor = new InMemoryChannel("order-processor");
    /// var inventoryManager = new InMemoryChannel("inventory-manager");
    /// var paymentService = new InMemoryChannel("payment-service");
    /// 
    /// // Create channel with auto-generated identifier
    /// var tempChannel = new InMemoryChannel(); // Results in "InMemoryChannel-{guid}"
    /// </code>
    /// </example>
    public InMemoryChannel(string? channelId = null) : base(channelId)
    {
    }

    protected override Task OnInitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken)
    {
        // In-memory channel doesn't need special initialization
        return Task.CompletedTask;
    }

    protected override Task OnConnectAsync(CancellationToken cancellationToken)
    {
        _isConnected = true;
        _channelRegistry.TryAdd(ChannelId, this);
        return Task.CompletedTask;
    }

    protected override Task OnDisconnectAsync(CancellationToken cancellationToken)
    {
        _isConnected = false;
        _isListening = false;
        _channelRegistry.TryRemove(ChannelId, out _);
        return Task.CompletedTask;
    }

    protected override async Task<CommunicationResponse> OnSendRequestAsync(CommunicationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Target))
        {
            return CommunicationResponse.CreateError(request.Id, "Target channel not specified", ChannelId, request.Target);
        }

        // Find target channel
        if (!_channelRegistry.TryGetValue(request.Target, out var targetChannel))
        {
            return CommunicationResponse.CreateError(request.Id, $"Target channel '{request.Target}' not found", ChannelId, request.Target);
        }

        if (!targetChannel._isListening || targetChannel._requestHandler == null)
        {
            return CommunicationResponse.CreateError(request.Id, $"Target channel '{request.Target}' is not listening", ChannelId, request.Target);
        }

        try
        {
            // Process request directly in target channel
            var response = await targetChannel._requestHandler(request);
            response.Source = request.Target;
            response.Target = ChannelId;
            return response;
        }
        catch (Exception ex)
        {
            return CommunicationResponse.CreateError(request.Id, $"Request processing failed: {ex.Message}", request.Target, ChannelId);
        }
    }

    protected override Task OnSendAsync(CommunicationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Target))
        {
            throw new ArgumentException("Target channel not specified");
        }

        // Find target channel
        if (!_channelRegistry.TryGetValue(request.Target, out var targetChannel))
        {
            throw new InvalidOperationException($"Target channel '{request.Target}' not found");
        }

        if (!targetChannel._isListening)
        {
            throw new InvalidOperationException($"Target channel '{request.Target}' is not listening");
        }

        // Queue message for processing
        targetChannel._messageQueue.Enqueue(request);

        return Task.CompletedTask;
    }

    protected override Task OnStartListeningAsync(Func<CommunicationRequest, Task<CommunicationResponse>> requestHandler, CancellationToken cancellationToken)
    {
        _requestHandler = requestHandler;
        _isListening = true;

        // Start background processing of queued messages
        _ = Task.Run(async () => await ProcessQueuedMessagesAsync(cancellationToken), cancellationToken);

        return Task.CompletedTask;
    }

    protected override Task OnStopListeningAsync(CancellationToken cancellationToken)
    {
        _isListening = false;
        _requestHandler = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Continuously processes fire-and-forget messages from the message queue in a background task.
    /// This method runs while the channel is listening and processes queued messages asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the background processing.</param>
    /// <returns>A task that completes when background processing stops.</returns>
    /// <remarks>
    /// This method implements the background processing loop for fire-and-forget messages.
    /// It dequeues messages from the concurrent queue and processes them using the registered
    /// request handler. Errors during message processing are logged but do not stop the loop.
    /// The method includes a small delay when no messages are available to prevent CPU spinning.
    /// </remarks>
    private async Task ProcessQueuedMessagesAsync(CancellationToken cancellationToken)
    {
        while (_isListening && !cancellationToken.IsCancellationRequested)
        {
            if (_messageQueue.TryDequeue(out var request) && _requestHandler != null)
            {
                try
                {
                    await _requestHandler(request);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Error processing queued message: {ex.Message}");
                }
            }
            else
            {
                // No messages, wait a bit to prevent CPU spinning
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Retrieves the identifiers of all currently registered channels in the shared registry.
    /// This method is primarily intended for testing, debugging, and monitoring purposes.
    /// </summary>
    /// <returns>
    /// An enumerable collection of channel identifiers for all currently active channels.
    /// Returns a snapshot of the registry at the time of the call.
    /// </returns>
    /// <remarks>
    /// This method provides visibility into the active channel registry for testing scenarios
    /// where you need to verify channel registration, debug communication issues, or monitor
    /// the state of the in-memory communication infrastructure. The returned collection is
    /// a snapshot and may become stale if channels connect or disconnect after the call.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check which channels are currently active
    /// var activeChannels = InMemoryChannel.GetRegisteredChannels();
    /// Console.WriteLine($"Active channels: {string.Join(", ", activeChannels)}");
    /// 
    /// // Verify specific channels are registered
    /// var expectedChannels = new[] { "agent-a", "agent-b", "coordinator" };
    /// var missingChannels = expectedChannels.Except(activeChannels);
    /// if (missingChannels.Any())
    /// {
    ///     Console.WriteLine($"Missing channels: {string.Join(", ", missingChannels)}");
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<string> GetRegisteredChannels()
    {
        return _channelRegistry.Keys.ToList();
    }

    /// <summary>
    /// Clears all registered channels from the shared registry, effectively disconnecting all
    /// in-memory channels. This method is primarily intended for test cleanup and reset scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this method in test teardown to ensure clean test isolation by removing all
    /// channel registrations from the static registry. This prevents test interference
    /// where channels from one test might affect subsequent tests.
    /// </para>
    /// <para>
    /// **Warning**: This method affects all InMemoryChannel instances across the entire
    /// application domain. Use with caution in production scenarios where multiple
    /// components might be using in-memory channels simultaneously.
    /// </para>
    /// <para>
    /// Channels that are cleared from the registry will lose the ability to communicate
    /// with other channels until they reconnect, which will re-register them.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Test]
    /// public async Task TestAgentCommunication()
    /// {
    ///     try
    ///     {
    ///         // Test setup and execution
    ///         var agentA = new InMemoryChannel("test-agent-a");
    ///         var agentB = new InMemoryChannel("test-agent-b");
    ///         
    ///         await agentA.ConnectAsync();
    ///         await agentB.ConnectAsync();
    ///         
    ///         // Run tests...
    ///         
    ///         await agentA.DisposeAsync();
    ///         await agentB.DisposeAsync();
    ///     }
    ///     finally
    ///     {
    ///         // Ensure clean state for next test
    ///         InMemoryChannel.ClearRegistry();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static void ClearRegistry()
    {
        _channelRegistry.Clear();
    }
}