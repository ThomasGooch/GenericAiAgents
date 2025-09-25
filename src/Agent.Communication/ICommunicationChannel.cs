using Agent.Communication.Models;

namespace Agent.Communication;

/// <summary>
/// Defines the contract for communication channels that enable message passing between agents,
/// services, and external systems. Provides standardized methods for connection management,
/// message sending, request-response patterns, and event-driven communication within the framework.
/// </summary>
/// <remarks>
/// <para>
/// The ICommunicationChannel interface abstracts various communication mechanisms (in-memory,
/// HTTP, message queues, WebSockets, gRPC, etc.) behind a unified interface. This enables
/// agents to communicate consistently regardless of the underlying transport mechanism.
/// </para>
/// <para>
/// **Key Communication Patterns:**
/// - **Request-Response**: Synchronous communication with response handling
/// - **Fire-and-Forget**: Asynchronous message sending without waiting for responses
/// - **Event-Driven**: Bidirectional communication with event notifications
/// - **Streaming**: Continuous data flow for real-time scenarios
/// </para>
/// <para>
/// **Connection Management:**
/// Implementations must handle connection lifecycle including initialization, connection
/// establishment, health monitoring, automatic reconnection, and graceful shutdown.
/// The channel should provide reliable delivery guarantees appropriate to the transport.
/// </para>
/// <para>
/// **Error Handling:**
/// Communication failures, network issues, and protocol errors should be handled gracefully
/// with appropriate retry logic, circuit breaker patterns, and error event notifications.
/// </para>
/// <para>
/// **Thread Safety:**
/// Implementations should be thread-safe and support concurrent operations from multiple
/// agents while maintaining message ordering and delivery guarantees where appropriate.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic channel usage pattern
/// ICommunicationChannel channel = new InMemoryChannel();
/// 
/// var config = new Dictionary&lt;string, object&gt;
/// {
///     ["BufferSize"] = 1000,
///     ["Timeout"] = TimeSpan.FromSeconds(30),
///     ["EnableEvents"] = true
/// };
/// 
/// // Initialize and connect
/// await channel.InitializeAsync(config);
/// await channel.ConnectAsync();
/// 
/// // Send request and wait for response
/// var request = new CommunicationRequest
/// {
///     Id = Guid.NewGuid().ToString(),
///     Source = "agent-a",
///     Target = "agent-b",
///     MessageType = "ProcessData",
///     Payload = new { Data = "example", Priority = "High" }
/// };
/// 
/// var response = await channel.SendRequestAsync(request);
/// Console.WriteLine($"Response: {response.Payload}");
/// 
/// // Fire-and-forget messaging
/// var notification = new CommunicationRequest
/// {
///     Id = Guid.NewGuid().ToString(),
///     Source = "system",
///     Target = "all-agents",
///     MessageType = "SystemNotification",
///     Payload = new { Message = "System maintenance in 10 minutes" }
/// };
/// 
/// await channel.SendAsync(notification);
/// 
/// // Start listening for incoming requests
/// await channel.StartListeningAsync(async (incomingRequest) =>
/// {
///     Console.WriteLine($"Received: {incomingRequest.MessageType} from {incomingRequest.Source}");
///     
///     // Process request and return response
///     var result = await ProcessIncomingRequest(incomingRequest);
///     return new CommunicationResponse
///     {
///         Id = Guid.NewGuid().ToString(),
///         RequestId = incomingRequest.Id,
///         IsSuccess = true,
///         Payload = result
///     };
/// });
/// 
/// // Event handling
/// channel.ConnectionStatusChanged += (sender, isConnected) =>
/// {
///     Console.WriteLine($"Connection status: {(isConnected ? "Connected" : "Disconnected")}");
/// };
/// 
/// channel.ErrorOccurred += (sender, error) =>
/// {
///     Console.WriteLine($"Channel error: {error}");
/// };
/// 
/// // Cleanup
/// await channel.StopListeningAsync();
/// await channel.DisconnectAsync();
/// await channel.DisposeAsync();
/// </code>
/// </example>
/// <seealso cref="BaseChannel"/>
/// <seealso cref="InMemoryChannel"/>
/// <seealso cref="CommunicationRequest"/>
/// <seealso cref="CommunicationResponse"/>
public interface ICommunicationChannel : IAsyncDisposable
{
    /// <summary>
    /// Channel identifier
    /// </summary>
    string ChannelId { get; }

    /// <summary>
    /// Indicates if the channel is currently connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Channel configuration
    /// </summary>
    Dictionary<string, object> Configuration { get; }

    /// <summary>
    /// Initializes the communication channel
    /// </summary>
    /// <param name="configuration">Channel configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the communication endpoint
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the communication endpoint
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request and waits for response
    /// </summary>
    /// <param name="request">Communication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Communication response</returns>
    Task<CommunicationResponse> SendRequestAsync(CommunicationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request without waiting for response (fire and forget)
    /// </summary>
    /// <param name="request">Communication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendAsync(CommunicationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts listening for incoming requests
    /// </summary>
    /// <param name="requestHandler">Handler for incoming requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartListeningAsync(Func<CommunicationRequest, Task<CommunicationResponse>> requestHandler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops listening for incoming requests
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when connection status changes
    /// </summary>
    event EventHandler<bool> ConnectionStatusChanged;

    /// <summary>
    /// Event fired when an error occurs
    /// </summary>
    event EventHandler<string> ErrorOccurred;
}