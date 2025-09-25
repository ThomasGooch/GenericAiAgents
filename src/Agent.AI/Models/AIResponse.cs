namespace Agent.AI.Models;

/// <summary>
/// Represents a response from an AI model completion request, containing the generated content,
/// performance metrics, token usage information, and metadata for monitoring and optimization.
/// This class provides a standardized structure for all AI service responses within the framework.
/// </summary>
/// <remarks>
/// <para>
/// AIResponse encapsulates both successful and failed AI operation results in a consistent format
/// that supports rich error handling, performance monitoring, and usage analytics. It provides
/// comprehensive information needed for cost tracking, performance optimization, and result processing.
/// </para>
/// <para>
/// **Key Components:**
/// - **Content**: The generated text response from the AI model
/// - **Success Status**: Clear indication of operation success or failure
/// - **Performance Metrics**: Duration and token usage for optimization
/// - **Model Information**: Details about which model generated the response
/// - **Extensible Metadata**: Additional provider-specific information
/// </para>
/// <para>
/// **Usage Patterns:**
/// This class is designed to be immutable after creation, using static factory methods
/// (CreateSuccess/CreateError) to ensure consistent state. This pattern prevents
/// invalid state combinations and provides clear creation semantics.
/// </para>
/// <para>
/// **Cost and Performance Tracking:**
/// The TokensUsed and Duration properties are essential for monitoring API costs,
/// performance optimization, and usage analytics. These should be accurately
/// captured by AI service implementations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Successful AI response processing
/// var response = await aiService.GetCompletionAsync("Explain quantum computing");
/// 
/// if (response.IsSuccess)
/// {
///     Console.WriteLine($"Response: {response.Content}");
///     Console.WriteLine($"Model: {response.ModelUsed}");
///     Console.WriteLine($"Tokens: {response.TokensUsed}");
///     Console.WriteLine($"Duration: {response.Duration.TotalMilliseconds}ms");
///     
///     // Check for additional metadata
///     if (response.Metadata.ContainsKey("finish_reason"))
///     {
///         Console.WriteLine($"Completion reason: {response.Metadata["finish_reason"]}");
///     }
///     
///     // Track usage for analytics
///     await usageTracker.RecordUsageAsync(response.TokensUsed, response.Duration, response.ModelUsed);
/// }
/// else
/// {
///     Console.WriteLine($"AI request failed: {response.ErrorMessage}");
///     Console.WriteLine($"Failed after: {response.Duration.TotalMilliseconds}ms");
///     
///     // Log error for monitoring
///     logger.LogError("AI completion failed: {Error}, Duration: {Duration}ms", 
///                    response.ErrorMessage, response.Duration.TotalMilliseconds);
/// }
/// 
/// // Cost calculation example
/// var estimatedCost = CalculateCost(response.TokensUsed, response.ModelUsed);
/// Console.WriteLine($"Estimated cost: ${estimatedCost:F4}");
/// 
/// // Performance monitoring
/// if (response.Duration.TotalSeconds > 10)
/// {
///     logger.LogWarning("Slow AI response detected: {Duration}s for {Tokens} tokens",
///                      response.Duration.TotalSeconds, response.TokensUsed);
/// }
/// </code>
/// </example>
/// <seealso cref="IAIService"/>
/// <seealso cref="AIConfiguration"/>
public class AIResponse
{
    /// <summary>
    /// Gets the generated text content from the AI model. Contains the actual response text
    /// for successful completions, or an empty string for failed requests.
    /// </summary>
    /// <value>
    /// The AI-generated text response. Never null, but may be empty for error responses.
    /// </value>
    public string Content { get; }

    /// <summary>
    /// Gets a value indicating whether the AI completion request was successful.
    /// Use this property to determine whether to process Content or handle ErrorMessage.
    /// </summary>
    /// <value>True if the request succeeded and Content contains valid data; otherwise, false.</value>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message describing what went wrong if the request failed.
    /// Null for successful requests.
    /// </summary>
    /// <value>A descriptive error message for failed requests, or null for successful ones.</value>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the total number of tokens consumed by the AI request, including both prompt and completion tokens.
    /// This is essential for cost tracking and usage analytics.
    /// </summary>
    /// <value>The total token count, or 0 if the request failed before processing.</value>
    public int TokensUsed { get; }

    /// <summary>
    /// Gets the identifier of the AI model that processed the request.
    /// This may differ from the requested model if fallback or routing logic was applied.
    /// </summary>
    /// <value>The model identifier (e.g., "gpt-4-turbo-preview", "claude-3-opus-20240229").</value>
    public string ModelUsed { get; }

    /// <summary>
    /// Gets the total time taken to process the AI request, from initiation to completion.
    /// Includes network latency, queuing time, and actual generation time.
    /// </summary>
    /// <value>The total processing duration for performance monitoring and optimization.</value>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets additional metadata provided by the AI service, containing provider-specific
    /// information about the response generation process.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs. Common keys include "finish_reason", "prompt_tokens", "completion_tokens".</value>
    /// <remarks>
    /// Common metadata includes:
    /// - "finish_reason": How the generation ended ("stop", "length", "content_filter")
    /// - "prompt_tokens": Tokens used in the input prompt
    /// - "completion_tokens": Tokens generated in the response
    /// - "total_tokens": Combined token count (should match TokensUsed)
    /// - "model_version": Specific model version used
    /// </remarks>
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
    /// Creates a successful AI response with the specified content and metrics.
    /// </summary>
    /// <param name="content">The generated text content. Cannot be null.</param>
    /// <param name="tokensUsed">Total tokens consumed by the request.</param>
    /// <param name="modelUsed">Identifier of the model that generated the response.</param>
    /// <param name="duration">Time taken to process the request.</param>
    /// <param name="metadata">Optional additional metadata from the AI provider.</param>
    /// <returns>A new AIResponse instance representing a successful completion.</returns>
    public static AIResponse CreateSuccess(string content, int tokensUsed, string modelUsed, TimeSpan duration, Dictionary<string, object>? metadata = null)
    {
        return new AIResponse(content, true, null, tokensUsed, modelUsed, duration, metadata);
    }

    /// <summary>
    /// Creates a failed AI response with the specified error information.
    /// </summary>
    /// <param name="errorMessage">Descriptive error message explaining the failure.</param>
    /// <param name="duration">Time taken before the request failed.</param>
    /// <param name="modelUsed">Model identifier if known, empty string otherwise.</param>
    /// <param name="tokensUsed">Tokens consumed before failure, if any.</param>
    /// <returns>A new AIResponse instance representing a failed completion.</returns>
    public static AIResponse CreateError(string errorMessage, TimeSpan duration, string modelUsed = "", int tokensUsed = 0)
    {
        return new AIResponse(string.Empty, false, errorMessage, tokensUsed, modelUsed, duration);
    }
}