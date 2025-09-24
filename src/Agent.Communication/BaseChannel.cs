using Agent.Communication.Models;

namespace Agent.Communication;

/// <summary>
/// Base implementation for communication channels
/// </summary>
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
            return CommunicationResponse.CreateError(request.RequestId, ex.Message, ChannelId, request.Target);
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

    // Protected virtual methods for derived classes to implement
    protected abstract Task OnConnectAsync(CancellationToken cancellationToken);
    protected abstract Task OnDisconnectAsync(CancellationToken cancellationToken);
    protected abstract Task<CommunicationResponse> OnSendRequestAsync(CommunicationRequest request, CancellationToken cancellationToken);
    protected abstract Task OnSendAsync(CommunicationRequest request, CancellationToken cancellationToken);
    protected abstract Task OnStartListeningAsync(Func<CommunicationRequest, Task<CommunicationResponse>> requestHandler, CancellationToken cancellationToken);
    protected abstract Task OnStopListeningAsync(CancellationToken cancellationToken);
    
    protected virtual Task OnInitializeAsync(Dictionary<string, object> configuration, CancellationToken cancellationToken)
    {
        // Override in derived classes if needed
        return Task.CompletedTask;
    }

    protected virtual ValueTask OnDisposeAsync()
    {
        // Override in derived classes if needed
        return ValueTask.CompletedTask;
    }

    protected virtual void OnConnectionStatusChanged(bool isConnected)
    {
        ConnectionStatusChanged?.Invoke(this, isConnected);
    }

    protected virtual void OnErrorOccurred(string error)
    {
        ErrorOccurred?.Invoke(this, error);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}