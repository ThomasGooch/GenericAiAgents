namespace Agent.AI.Models;

/// <summary>
/// Configuration for AI model integration
/// </summary>
public class AIConfiguration
{
    /// <summary>
    /// AI model provider (e.g., "AzureOpenAI", "OpenAI", "Anthropic")
    /// </summary>
    public string Provider { get; set; } = "AzureOpenAI";

    /// <summary>
    /// Model deployment name or model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Maximum tokens for completion
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Temperature for response randomness (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Top-p sampling parameter
    /// </summary>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Additional settings specific to the provider
    /// </summary>
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();
}