using Agent.AI.Models;

namespace Agent.AI;

/// <summary>
/// Service for AI model integration and chat completion
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Initializes the AI service with configuration
    /// </summary>
    /// <param name="configuration">AI configuration settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(AIConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a completion from the AI model
    /// </summary>
    /// <param name="prompt">The input prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with completion</returns>
    Task<AIResponse> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a completion from the AI model with custom settings
    /// </summary>
    /// <param name="prompt">The input prompt</param>
    /// <param name="maxTokens">Maximum tokens override</param>
    /// <param name="temperature">Temperature override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with completion</returns>
    Task<AIResponse> GetCompletionWithSettingsAsync(string prompt, int? maxTokens = null, double? temperature = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming completion from the AI model
    /// </summary>
    /// <param name="prompt">The input prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    IAsyncEnumerable<string> GetStreamingCompletionAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the AI service is properly configured
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if configured and ready</returns>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the AI service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the configured model
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model information</returns>
    Task<Dictionary<string, object>> GetModelInfoAsync(CancellationToken cancellationToken = default);
}