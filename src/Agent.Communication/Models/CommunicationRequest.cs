namespace Agent.Communication.Models;

/// <summary>
/// Base communication request model
/// </summary>
public class CommunicationRequest
{
    /// <summary>
    /// Unique request identifier
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Target agent or service identifier
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Source agent or service identifier
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Action or method to execute
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Request payload data
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Additional headers or metadata
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Priority level (0 = highest, 10 = lowest)
    /// </summary>
    public int Priority { get; set; } = 5;
}