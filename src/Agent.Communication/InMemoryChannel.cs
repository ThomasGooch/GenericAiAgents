using Agent.Communication.Models;
using System.Collections.Concurrent;

namespace Agent.Communication;

/// <summary>
/// In-memory communication channel for testing and development
/// </summary>
public class InMemoryChannel : BaseChannel
{
    private bool _isConnected = false;
    private bool _isListening = false;
    private Func<CommunicationRequest, Task<CommunicationResponse>>? _requestHandler;
    private readonly ConcurrentQueue<CommunicationRequest> _messageQueue = new();

    // Static registry to simulate multiple channels communicating
    private static readonly ConcurrentDictionary<string, InMemoryChannel> _channelRegistry = new();

    /// <inheritdoc/>
    public override bool IsConnected => _isConnected;

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
            return CommunicationResponse.CreateError(request.RequestId, "Target channel not specified", ChannelId, request.Target);
        }

        // Find target channel
        if (!_channelRegistry.TryGetValue(request.Target, out var targetChannel))
        {
            return CommunicationResponse.CreateError(request.RequestId, $"Target channel '{request.Target}' not found", ChannelId, request.Target);
        }

        if (!targetChannel._isListening || targetChannel._requestHandler == null)
        {
            return CommunicationResponse.CreateError(request.RequestId, $"Target channel '{request.Target}' is not listening", ChannelId, request.Target);
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
            return CommunicationResponse.CreateError(request.RequestId, $"Request processing failed: {ex.Message}", request.Target, ChannelId);
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
                // No messages, wait a bit
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Gets all registered channels (for testing purposes)
    /// </summary>
    public static IEnumerable<string> GetRegisteredChannels()
    {
        return _channelRegistry.Keys.ToList();
    }

    /// <summary>
    /// Clears all registered channels (for testing purposes)
    /// </summary>
    public static void ClearRegistry()
    {
        _channelRegistry.Clear();
    }
}