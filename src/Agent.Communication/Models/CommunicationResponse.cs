namespace Agent.Communication.Models;

/// <summary>
/// Base communication response model
/// </summary>
public class CommunicationResponse
{
    /// <summary>
    /// Request ID this response corresponds to
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source agent or service identifier
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Target agent or service identifier
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response payload data
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional headers or metadata
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Processing duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static CommunicationResponse CreateSuccess(string requestId, object? data = null, string source = "", string target = "")
    {
        return new CommunicationResponse
        {
            RequestId = requestId,
            Success = true,
            Data = data,
            Source = source,
            Target = target
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static CommunicationResponse CreateError(string requestId, string error, string source = "", string target = "")
    {
        return new CommunicationResponse
        {
            RequestId = requestId,
            Success = false,
            Error = error,
            Source = source,
            Target = target
        };
    }
}