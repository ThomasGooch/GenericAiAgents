using Agent.AI.Models;

namespace Agent.AI;

/// <summary>
/// Defines the contract for AI service integration, providing standardized access to various AI providers
/// and models for text completion, streaming responses, and model management within the GenericAiAgents framework.
/// </summary>
/// <remarks>
/// <para>
/// The IAIService interface abstracts AI provider differences, enabling agents to work with multiple
/// AI services (OpenAI, Azure OpenAI, Anthropic, local models, etc.) through a unified interface.
/// This design supports provider switching, multi-model scenarios, and consistent error handling.
/// </para>
/// <para>
/// **Key Capabilities:**
/// - **Text Completion**: Single-turn text generation with customizable parameters
/// - **Streaming Responses**: Real-time token streaming for responsive user experiences
/// - **Provider Abstraction**: Unified interface across different AI providers
/// - **Connection Management**: Health checking and connectivity validation
/// - **Model Introspection**: Runtime model information and capabilities discovery
/// </para>
/// <para>
/// **Implementation Patterns:**
/// Implementations should handle provider-specific authentication, rate limiting, error handling,
/// and response formatting. They must be thread-safe and support concurrent operations while
/// respecting provider API limits and best practices.
/// </para>
/// <para>
/// **Configuration Management:**
/// AI services are configured through the AIConfiguration class, which contains provider-specific
/// settings, authentication details, and operational parameters. Configuration is typically
/// provided during service initialization and remains immutable during operation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic AI service usage
/// var aiService = serviceProvider.GetRequiredService&lt;IAIService&gt;();
/// 
/// var configuration = new AIConfiguration
/// {
///     Provider = AIProvider.OpenAI,
///     ApiKey = "your-api-key",
///     Model = "gpt-4",
///     MaxTokens = 1000,
///     Temperature = 0.7
/// };
/// 
/// await aiService.InitializeAsync(configuration);
/// 
/// // Test connectivity
/// if (await aiService.IsConfiguredAsync() &amp;&amp; await aiService.TestConnectionAsync())
/// {
///     var response = await aiService.GetCompletionAsync("Explain quantum computing in simple terms.");
///     Console.WriteLine($"Response: {response.Content}");
///     Console.WriteLine($"Tokens used: {response.TokensUsed}");
/// }
/// 
/// // Streaming response for real-time applications
/// await foreach (var chunk in aiService.GetStreamingCompletionAsync("Write a story about AI"))
/// {
///     Console.Write(chunk);
///     await Task.Delay(50); // Simulate typing effect
/// }
/// 
/// // Advanced usage with custom settings
/// var customResponse = await aiService.GetCompletionWithSettingsAsync(
///     "Generate a technical summary",
///     maxTokens: 500,
///     temperature: 0.3);
/// 
/// // Model information for debugging/monitoring
/// var modelInfo = await aiService.GetModelInfoAsync();
/// Console.WriteLine($"Model: {modelInfo["name"]}, Version: {modelInfo["version"]}");
/// </code>
/// </example>
/// <seealso cref="AIConfiguration"/>
/// <seealso cref="AIResponse"/>
/// <seealso cref="SemanticKernelAIService"/>
public interface IAIService
{
    /// <summary>
    /// Initializes the AI service with the specified configuration, establishing connections
    /// and validating credentials with the AI provider.
    /// </summary>
    /// <param name="configuration">
    /// The AI configuration settings containing provider details, authentication credentials,
    /// model parameters, and operational settings. Cannot be null.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the initialization operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous initialization operation.
    /// The task completes when the service is fully initialized and ready for use.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the configuration contains invalid settings or missing required parameters.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the API key or authentication credentials are invalid.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when there are network connectivity issues with the AI provider.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service has already been initialized or the provider is not supported.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Initialization performs several critical operations:
    /// - Validates configuration parameters and required settings
    /// - Authenticates with the AI provider using provided credentials
    /// - Establishes connection pools and HTTP clients
    /// - Verifies model availability and permissions
    /// - Sets up rate limiting and retry policies
    /// </para>
    /// <para>
    /// This method should be called once during service startup. Subsequent calls
    /// will throw InvalidOperationException unless the service is disposed and recreated.
    /// </para>
    /// <para>
    /// **Provider-Specific Considerations:**
    /// - **OpenAI**: Requires valid API key and organization ID (optional)
    /// - **Azure OpenAI**: Requires endpoint, API key, and deployment name
    /// - **Anthropic**: Requires API key and model version specification
    /// - **Local Models**: Requires endpoint URL and model path validation
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // OpenAI configuration
    /// var openAiConfig = new AIConfiguration
    /// {
    ///     Provider = AIProvider.OpenAI,
    ///     ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
    ///     Model = "gpt-4-turbo-preview",
    ///     MaxTokens = 4000,
    ///     Temperature = 0.7,
    ///     TopP = 1.0
    /// };
    /// 
    /// try
    /// {
    ///     await aiService.InitializeAsync(openAiConfig);
    ///     Console.WriteLine("AI service initialized successfully");
    /// }
    /// catch (UnauthorizedAccessException)
    /// {
    ///     Console.WriteLine("Invalid API key or insufficient permissions");
    /// }
    /// catch (HttpRequestException ex)
    /// {
    ///     Console.WriteLine($"Network error during initialization: {ex.Message}");
    /// }
    /// 
    /// // Azure OpenAI configuration
    /// var azureConfig = new AIConfiguration
    /// {
    ///     Provider = AIProvider.AzureOpenAI,
    ///     Endpoint = "https://your-resource.openai.azure.com/",
    ///     ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY"),
    ///     DeploymentName = "gpt-4-deployment",
    ///     ApiVersion = "2024-02-15-preview"
    /// };
    /// 
    /// await aiService.InitializeAsync(azureConfig);
    /// </code>
    /// </example>
    Task InitializeAsync(AIConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a text completion from the AI model using the provided prompt and default configuration settings.
    /// This is the primary method for single-turn text generation with standard model parameters.
    /// </summary>
    /// <param name="prompt">
    /// The input prompt text to send to the AI model. Should be well-formed and provide
    /// clear context or instructions for the desired response. Cannot be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the completion request.
    /// Respects both the provided token and any configured request timeouts.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous completion operation.
    /// The result contains the AI-generated text, token usage information, metadata,
    /// and performance metrics from the completion request.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="prompt"/> is null, empty, or exceeds model limits.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service has not been initialized or is not properly configured.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when authentication fails or quota limits are exceeded.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when there are network connectivity issues or API errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>
    /// or when configured timeouts are exceeded.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses the default settings configured during initialization, including:
    /// - Model selection and version
    /// - Token limits and generation parameters
    /// - Temperature and creativity settings
    /// - Safety filters and content moderation
    /// </para>
    /// <para>
    /// **Performance Considerations:**
    /// - Response times vary based on prompt complexity and model size
    /// - Token usage directly impacts cost and rate limiting
    /// - Longer prompts may require more processing time
    /// - Consider using streaming for real-time applications
    /// </para>
    /// <para>
    /// **Best Practices:**
    /// - Use clear, specific prompts for better results
    /// - Include necessary context and examples in the prompt
    /// - Monitor token usage for cost optimization
    /// - Implement appropriate retry logic for production use
    /// - Cache responses when appropriate to reduce API calls
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic text completion
    /// var prompt = "Explain the concept of machine learning in simple terms.";
    /// var response = await aiService.GetCompletionAsync(prompt);
    /// 
    /// Console.WriteLine($"Response: {response.Content}");
    /// Console.WriteLine($"Tokens used: {response.TokensUsed}");
    /// Console.WriteLine($"Response time: {response.ProcessingTime.TotalMilliseconds}ms");
    /// 
    /// // Check for content filtering or moderation flags
    /// if (response.Metadata.ContainsKey("ContentFiltered"))
    /// {
    ///     Console.WriteLine("Response was filtered for content policy compliance");
    /// }
    /// 
    /// // Advanced prompt with structured request
    /// var structuredPrompt = @"
    /// Task: Generate a product description
    /// Product: Wireless Bluetooth Headphones
    /// Target Audience: Tech professionals
    /// Tone: Professional but approachable
    /// Length: 2-3 paragraphs
    /// 
    /// Requirements:
    /// - Highlight key features
    /// - Include technical specifications
    /// - Mention use cases
    /// ";
    /// 
    /// var productResponse = await aiService.GetCompletionAsync(structuredPrompt);
    /// 
    /// // Error handling for production use
    /// try
    /// {
    ///     var result = await aiService.GetCompletionAsync(userPrompt);
    ///     return result.Content;
    /// }
    /// catch (UnauthorizedAccessException)
    /// {
    ///     // Handle authentication/quota issues
    ///     logger.LogWarning("AI service authentication failed or quota exceeded");
    ///     return "Service temporarily unavailable";
    /// }
    /// catch (HttpRequestException ex)
    /// {
    ///     // Handle network/API issues
    ///     logger.LogError(ex, "AI service request failed");
    ///     return "Unable to process request at this time";
    /// }
    /// </code>
    /// </example>
    Task<AIResponse> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a text completion with custom model parameters that override the default configuration.
    /// This method provides fine-grained control over generation settings for specific use cases.
    /// </summary>
    /// <param name="prompt">The input prompt text. Cannot be null or empty.</param>
    /// <param name="maxTokens">Maximum tokens to generate, overriding default. Must be positive and within model limits.</param>
    /// <param name="temperature">Controls randomness (0.0-2.0). Lower values are more deterministic, higher values more creative.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the AI response with completion text and metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid or out of range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is not initialized.</exception>
    /// <remarks>
    /// Use this method when you need to adjust model behavior for specific prompts without
    /// changing the global configuration. Common scenarios include creative vs analytical tasks.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creative writing with high temperature
    /// var story = await aiService.GetCompletionWithSettingsAsync(
    ///     "Write a creative story about space exploration", 
    ///     maxTokens: 2000, 
    ///     temperature: 1.2);
    /// 
    /// // Analytical response with low temperature
    /// var analysis = await aiService.GetCompletionWithSettingsAsync(
    ///     "Analyze the quarterly financial data", 
    ///     maxTokens: 500, 
    ///     temperature: 0.1);
    /// </code>
    /// </example>
    Task<AIResponse> GetCompletionWithSettingsAsync(string prompt, int? maxTokens = null, double? temperature = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming text completion, returning response tokens as they are generated
    /// for real-time user experiences and responsive applications.
    /// </summary>
    /// <param name="prompt">The input prompt text. Cannot be null or empty.</param>
    /// <param name="cancellationToken">Cancellation token to stop streaming.</param>
    /// <returns>An async enumerable that yields text chunks as they are generated by the model.</returns>
    /// <exception cref="ArgumentException">Thrown when prompt is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when service is not initialized.</exception>
    /// <remarks>
    /// Streaming is ideal for chat applications, real-time text generation, and scenarios
    /// where you want to display content as it's generated rather than waiting for completion.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Display response in real-time
    /// await foreach (var chunk in aiService.GetStreamingCompletionAsync("Explain AI to a 5-year-old"))
    /// {
    ///     Console.Write(chunk);
    ///     await Task.Delay(50); // Simulate typing effect
    /// }
    /// </code>
    /// </example>
    IAsyncEnumerable<string> GetStreamingCompletionAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the AI service has been properly initialized and configured for use.
    /// This performs local validation without making external API calls.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the service is configured and ready for requests; otherwise, false.</returns>
    /// <remarks>
    /// This method checks internal configuration state and should be used before making
    /// AI requests to ensure the service is ready. Use TestConnectionAsync() to verify
    /// actual connectivity to the AI provider.
    /// </remarks>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the AI provider by making a lightweight API call
    /// to verify authentication, network connectivity, and service availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the connection test.</param>
    /// <returns>True if the connection test succeeds; otherwise, false.</returns>
    /// <remarks>
    /// This method makes an actual API call to validate the service is reachable
    /// and properly authenticated. Use for health checks and startup validation.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (!await aiService.TestConnectionAsync())
    /// {
    ///     logger.LogError("AI service connection failed");
    ///     return ServiceResult.Unavailable();
    /// }
    /// </code>
    /// </example>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves detailed information about the currently configured AI model,
    /// including capabilities, limits, and version details.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A dictionary containing model metadata such as name, version, token limits, and capabilities.</returns>
    /// <remarks>
    /// Model information is useful for debugging, monitoring, and adapting behavior
    /// based on model capabilities. Common keys include "name", "version", "max_tokens",
    /// "supports_streaming", and "context_window".
    /// </remarks>
    /// <example>
    /// <code>
    /// var info = await aiService.GetModelInfoAsync();
    /// Console.WriteLine($"Model: {info["name"]} v{info["version"]}");
    /// Console.WriteLine($"Max tokens: {info["max_tokens"]}");
    /// Console.WriteLine($"Context window: {info["context_window"]}");
    /// </code>
    /// </example>
    Task<Dictionary<string, object>> GetModelInfoAsync(CancellationToken cancellationToken = default);
}