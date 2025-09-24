using Agent.Communication.Models;

namespace Agent.Communication;

/// <summary>
/// Base interface for communication channels
/// </summary>
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