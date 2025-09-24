namespace Agent.AI.Models;

/// <summary>
/// Response from AI model completion
/// </summary>
public class AIResponse
{
    /// <summary>
    /// The generated text response
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Indicates if the response was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Error message if the response failed
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Number of tokens used in the request
    /// </summary>
    public int TokensUsed { get; }

    /// <summary>
    /// Model that generated the response
    /// </summary>
    public string ModelUsed { get; }

    /// <summary>
    /// Time taken for the request
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Additional metadata from the response
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    private AIResponse(string content, bool isSuccess, string? errorMessage, int tokensUsed, string modelUsed, TimeSpan duration, Dictionary<string, object>? metadata = null)
    {
        Content = content;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        TokensUsed = tokensUsed;
        ModelUsed = modelUsed;
        Duration = duration;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a successful AI response
    /// </summary>
    public static AIResponse CreateSuccess(string content, int tokensUsed, string modelUsed, TimeSpan duration, Dictionary<string, object>? metadata = null)
    {
        return new AIResponse(content, true, null, tokensUsed, modelUsed, duration, metadata);
    }

    /// <summary>
    /// Creates a failed AI response
    /// </summary>
    public static AIResponse CreateError(string errorMessage, TimeSpan duration, string modelUsed = "", int tokensUsed = 0)
    {
        return new AIResponse(string.Empty, false, errorMessage, tokensUsed, modelUsed, duration);
    }
}