namespace Agent.AI.Models;

/// <summary>
/// Represents comprehensive configuration settings for AI model integration, supporting multiple providers
/// and containing all necessary parameters for authentication, model selection, and generation behavior.
/// This class provides a unified configuration structure for various AI providers within the framework.
/// </summary>
/// <remarks>
/// <para>
/// AIConfiguration serves as the central configuration point for all AI service integrations.
/// It abstracts provider-specific settings while maintaining flexibility for advanced configurations
/// through the AdditionalSettings property.
/// </para>
/// <para>
/// **Supported Providers:**
/// - **OpenAI**: Direct OpenAI API integration with models like GPT-4, GPT-3.5-turbo
/// - **Azure OpenAI**: Microsoft Azure-hosted OpenAI models with enterprise features
/// - **Anthropic**: Claude models with advanced reasoning capabilities
/// - **Local Models**: Self-hosted models via compatible APIs (Ollama, LocalAI, etc.)
/// - **Custom Providers**: Extensible through provider-specific implementations
/// </para>
/// <para>
/// **Configuration Lifecycle:**
/// Configuration is typically loaded from app settings, environment variables, or secrets management
/// during application startup. It remains immutable during AI service operation to ensure consistency.
/// </para>
/// <para>
/// **Security Considerations:**
/// API keys and sensitive settings should be stored securely using configuration providers,
/// environment variables, or dedicated secrets management systems. Never hardcode credentials.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // OpenAI configuration
/// var openAiConfig = new AIConfiguration
/// {
///     Provider = "OpenAI",
///     ModelId = "gpt-4-turbo-preview",
///     ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
///     MaxTokens = 4000,
///     Temperature = 0.7,
///     TimeoutSeconds = 60,
///     AdditionalSettings = new Dictionary&lt;string, object&gt;
///     {
///         ["organization"] = "org-123456",
///         ["user"] = "user-agent-system"
///     }
/// };
/// 
/// // Azure OpenAI configuration
/// var azureConfig = new AIConfiguration
/// {
///     Provider = "AzureOpenAI",
///     Endpoint = "https://myresource.openai.azure.com/",
///     ModelId = "gpt-4-deployment",
///     ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY"),
///     MaxTokens = 2000,
///     Temperature = 0.5,
///     AdditionalSettings = new Dictionary&lt;string, object&gt;
///     {
///         ["api_version"] = "2024-02-15-preview",
///         ["deployment_name"] = "gpt-4-deployment"
///     }
/// };
/// 
/// // High-creativity configuration for creative tasks
/// var creativeConfig = new AIConfiguration
/// {
///     Provider = "OpenAI",
///     ModelId = "gpt-4",
///     ApiKey = secretManager.GetSecret("openai-key"),
///     MaxTokens = 3000,
///     Temperature = 1.2,  // High creativity
///     TopP = 0.9,         // Diverse token selection
///     TimeoutSeconds = 120 // Longer timeout for complex generation
/// };
/// 
/// // Production configuration with monitoring
/// var productionConfig = new AIConfiguration
/// {
///     Provider = "AzureOpenAI",
///     Endpoint = configuration["AI:Endpoint"],
///     ModelId = configuration["AI:Model"],
///     ApiKey = keyVault.GetSecret("ai-api-key"),
///     MaxTokens = 1500,
///     Temperature = 0.3,  // Consistent, focused responses
///     TimeoutSeconds = 30,
///     AdditionalSettings = new Dictionary&lt;string, object&gt;
///     {
///         ["retry_attempts"] = 3,
///         ["enable_content_filter"] = true,
///         ["log_requests"] = true,
///         ["environment"] = "production"
///     }
/// };
/// </code>
/// </example>
/// <seealso cref="IAIService"/>
/// <seealso cref="AIResponse"/>
/// <seealso cref="SemanticKernelAIService"/>
public class AIConfiguration
{
    /// <summary>
    /// Gets or sets the AI model provider identifier that determines which AI service implementation to use.
    /// This property controls which provider-specific logic is applied during service initialization and operation.
    /// </summary>
    /// <value>
    /// A string identifying the AI provider. Common values include "OpenAI", "AzureOpenAI", "Anthropic",
    /// "LocalAI", or custom provider names. Defaults to "AzureOpenAI".
    /// </value>
    /// <remarks>
    /// <para>
    /// The provider value determines:
    /// - Which authentication method is used
    /// - How API endpoints are constructed
    /// - Which model names and parameters are valid
    /// - Provider-specific features and limitations
    /// </para>
    /// <para>
    /// **Standard Provider Values:**
    /// - **"OpenAI"**: Direct OpenAI API integration
    /// - **"AzureOpenAI"**: Microsoft Azure OpenAI Service
    /// - **"Anthropic"**: Anthropic Claude models
    /// - **"LocalAI"**: Self-hosted compatible API endpoints
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // OpenAI provider configuration
    /// config.Provider = "OpenAI";
    /// 
    /// // Azure OpenAI provider configuration
    /// config.Provider = "AzureOpenAI";
    /// 
    /// // Custom local provider
    /// config.Provider = "LocalAI";
    /// </code>
    /// </example>
    public string Provider { get; set; } = "AzureOpenAI";

    /// <summary>
    /// Gets or sets the model identifier or deployment name used to specify which AI model to use.
    /// The format and valid values depend on the selected provider.
    /// </summary>
    /// <value>
    /// A string specifying the model to use. Format varies by provider:
    /// - OpenAI: Model name (e.g., "gpt-4", "gpt-3.5-turbo")
    /// - Azure OpenAI: Deployment name as configured in Azure
    /// - Anthropic: Model version (e.g., "claude-3-opus-20240229")
    /// </value>
    /// <remarks>
    /// <para>
    /// **Provider-Specific Model Identifiers:**
    /// - **OpenAI**: Use official model names like "gpt-4-turbo-preview", "gpt-3.5-turbo-1106"
    /// - **Azure OpenAI**: Use the deployment name you created in your Azure resource
    /// - **Anthropic**: Use model identifiers like "claude-3-opus-20240229"
    /// - **Local Models**: Use model names as configured in your local API
    /// </para>
    /// <para>
    /// Model selection affects capabilities, cost, speed, and context window size.
    /// Choose models appropriate for your use case and performance requirements.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // OpenAI model selection
    /// config.ModelId = "gpt-4-turbo-preview";
    /// 
    /// // Azure OpenAI deployment name
    /// config.ModelId = "my-gpt4-deployment";
    /// 
    /// // Anthropic Claude model
    /// config.ModelId = "claude-3-opus-20240229";
    /// </code>
    /// </example>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API endpoint URL for the AI provider service.
    /// Required for Azure OpenAI and local model providers; optional for direct OpenAI usage.
    /// </summary>
    /// <value>
    /// A valid HTTP/HTTPS URL pointing to the AI service endpoint. Empty string if not required.
    /// Must include protocol (https://) and should not include trailing slashes.
    /// </value>
    /// <remarks>
    /// <para>
    /// **Endpoint Requirements by Provider:**
    /// - **OpenAI**: Not required (uses default https://api.openai.com)
    /// - **Azure OpenAI**: Required - your Azure resource endpoint
    /// - **Anthropic**: Not required (uses default https://api.anthropic.com)
    /// - **Local Models**: Required - your self-hosted service URL
    /// </para>
    /// <para>
    /// **Security Considerations:**
    /// - Always use HTTPS endpoints in production
    /// - Verify SSL certificates are valid
    /// - Consider using private endpoints for Azure OpenAI in enterprise environments
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Azure OpenAI endpoint
    /// config.Endpoint = "https://mycompany.openai.azure.com/";
    /// 
    /// // Local model endpoint
    /// config.Endpoint = "http://localhost:11434";
    /// 
    /// // Custom enterprise endpoint
    /// config.Endpoint = "https://ai-gateway.company.com/v1";
    /// </code>
    /// </example>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key used for authentication with the AI provider.
    /// This sensitive credential should be stored securely and never hardcoded.
    /// </summary>
    /// <value>
    /// The API key string as provided by the AI service provider. Should be kept secure
    /// and loaded from environment variables, configuration, or secrets management.
    /// </value>
    /// <remarks>
    /// <para>
    /// **Security Best Practices:**
    /// - Never hardcode API keys in source code
    /// - Use environment variables or secure configuration providers
    /// - Implement key rotation policies for production environments
    /// - Monitor API key usage for security and cost management
    /// - Use least-privilege access when creating API keys
    /// </para>
    /// <para>
    /// **Provider-Specific Key Formats:**
    /// - **OpenAI**: Starts with "sk-" followed by alphanumeric characters
    /// - **Azure OpenAI**: 32-character hexadecimal string
    /// - **Anthropic**: Starts with "sk-ant-" followed by encoded data
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Load from environment variable (recommended)
    /// config.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    /// 
    /// // Load from configuration
    /// config.ApiKey = configuration["AI:ApiKey"];
    /// 
    /// // Load from secrets manager (production)
    /// config.ApiKey = await secretsManager.GetSecretAsync("ai-api-key");
    /// </code>
    /// </example>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the AI response.
    /// This controls response length and helps manage costs and performance.
    /// </summary>
    /// <value>
    /// The maximum number of tokens for completion. Must be positive and within model limits.
    /// Defaults to 2000 tokens, which typically represents 1500-3000 words depending on content.
    /// </value>
    /// <remarks>
    /// <para>
    /// **Token Considerations:**
    /// - Tokens represent pieces of words (approximately 0.75 words per token for English)
    /// - Higher values allow longer responses but increase cost and latency
    /// - Model-specific limits apply (e.g., GPT-4: 4096-8192 tokens, Claude: up to 100k+)
    /// - Total tokens include both prompt and response
    /// </para>
    /// <para>
    /// **Recommended Values:**
    /// - **Short responses**: 100-500 tokens
    /// - **Standard responses**: 500-2000 tokens
    /// - **Long-form content**: 2000-4000 tokens
    /// - **Document processing**: 4000+ tokens (model permitting)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Short, focused responses
    /// config.MaxTokens = 500;
    /// 
    /// // Standard chat responses
    /// config.MaxTokens = 1500;
    /// 
    /// // Long-form content generation
    /// config.MaxTokens = 4000;
    /// </code>
    /// </example>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the temperature parameter that controls randomness and creativity in AI responses.
    /// Lower values produce more focused and deterministic outputs; higher values increase creativity and variation.
    /// </summary>
    /// <value>
    /// A double value between 0.0 and 2.0, where:
    /// - 0.0: Maximum determinism and consistency
    /// - 1.0: Balanced creativity and consistency  
    /// - 2.0: Maximum creativity and randomness
    /// Defaults to 0.7 for balanced performance.
    /// </value>
    /// <remarks>
    /// <para>
    /// **Temperature Guidelines:**
    /// - **0.0-0.3**: Analytical tasks, factual responses, code generation
    /// - **0.4-0.7**: Balanced responses, general chat, explanations
    /// - **0.8-1.2**: Creative writing, brainstorming, varied responses
    /// - **1.3-2.0**: Highly creative, experimental, artistic content
    /// </para>
    /// <para>
    /// **Use Case Examples:**
    /// - Code review and technical analysis: 0.1-0.3
    /// - Customer support responses: 0.3-0.5
    /// - Content creation and marketing: 0.7-1.0
    /// - Creative writing and storytelling: 1.0-1.5
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Deterministic, factual responses
    /// config.Temperature = 0.1;
    /// 
    /// // Balanced creativity and consistency
    /// config.Temperature = 0.7;
    /// 
    /// // High creativity for content generation
    /// config.Temperature = 1.2;
    /// </code>
    /// </example>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the top-p (nucleus) sampling parameter that controls token selection diversity.
    /// This works alongside temperature to fine-tune response generation behavior.
    /// </summary>
    /// <value>
    /// A double value between 0.0 and 1.0, where lower values focus on higher probability tokens
    /// and higher values include more diverse token possibilities. Defaults to 1.0 (no filtering).
    /// </value>
    /// <remarks>
    /// <para>
    /// Top-p sampling selects from the smallest set of tokens whose cumulative probability
    /// exceeds the specified threshold. This provides more consistent quality than pure randomness.
    /// </para>
    /// <para>
    /// **Top-P Guidelines:**
    /// - **0.1-0.3**: Very focused, high-quality tokens only
    /// - **0.4-0.7**: Balanced quality with some diversity
    /// - **0.8-0.9**: Good diversity while maintaining quality
    /// - **1.0**: No filtering (all tokens considered)
    /// </para>
    /// <para>
    /// **Interaction with Temperature:**
    /// - Use lower top-p with higher temperature for creative but coherent responses
    /// - Use higher top-p with lower temperature for consistent but varied responses
    /// - Generally, adjust one parameter at a time for predictable results
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Focused, high-quality responses
    /// config.TopP = 0.3;
    /// 
    /// // Balanced diversity and quality
    /// config.TopP = 0.8;
    /// 
    /// // Maximum diversity (default)
    /// config.TopP = 1.0;
    /// </code>
    /// </example>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the timeout duration in seconds for AI API requests.
    /// This prevents requests from hanging indefinitely and ensures responsive application behavior.
    /// </summary>
    /// <value>
    /// The timeout duration in seconds. Must be positive. Defaults to 30 seconds for standard operations.
    /// Consider longer timeouts for complex prompts or slower models.
    /// </value>
    /// <remarks>
    /// <para>
    /// **Timeout Considerations:**
    /// - Shorter timeouts improve responsiveness but may fail on complex requests
    /// - Longer timeouts allow complex processing but may impact user experience
    /// - Network conditions and model complexity affect actual response times
    /// - Streaming requests may need longer timeouts for complete responses
    /// </para>
    /// <para>
    /// **Recommended Timeouts:**
    /// - **Simple queries**: 15-30 seconds
    /// - **Standard chat**: 30-60 seconds
    /// - **Complex analysis**: 60-120 seconds
    /// - **Document processing**: 120-300 seconds
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Quick responses for real-time chat
    /// config.TimeoutSeconds = 15;
    /// 
    /// // Standard timeout for most operations
    /// config.TimeoutSeconds = 30;
    /// 
    /// // Extended timeout for complex processing
    /// config.TimeoutSeconds = 120;
    /// </code>
    /// </example>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets additional provider-specific configuration settings that extend the base configuration.
    /// This dictionary allows for flexible provider-specific customization without modifying the core configuration structure.
    /// </summary>
    /// <value>
    /// A dictionary containing provider-specific settings as key-value pairs. Keys should use descriptive names
    /// following provider documentation. Defaults to an empty dictionary.
    /// </value>
    /// <remarks>
    /// <para>
    /// The AdditionalSettings dictionary provides extensibility for provider-specific features
    /// that aren't covered by the standard configuration properties. This allows the framework
    /// to support new provider features without breaking changes.
    /// </para>
    /// <para>
    /// **Common Additional Settings by Provider:**
    /// </para>
    /// <para>
    /// **OpenAI:**
    /// - "organization": Organization ID for billing and access control
    /// - "user": User identifier for monitoring and abuse detection  
    /// - "logit_bias": Token bias adjustments
    /// - "stream": Enable streaming responses
    /// </para>
    /// <para>
    /// **Azure OpenAI:**
    /// - "api_version": API version (e.g., "2024-02-15-preview")
    /// - "deployment_name": Alternative to ModelId
    /// - "content_filter": Enable/disable content filtering
    /// - "entra_auth": Use Entra ID authentication instead of API key
    /// </para>
    /// <para>
    /// **Anthropic:**
    /// - "anthropic_version": API version header
    /// - "max_tokens_to_sample": Alternative token limit parameter
    /// - "stop_sequences": Custom stop sequences
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // OpenAI-specific settings
    /// config.AdditionalSettings = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["organization"] = "org-123456789",
    ///     ["user"] = "agent-system-v1",
    ///     ["stream"] = true,
    ///     ["logit_bias"] = new Dictionary&lt;string, int&gt; { ["50256"] = -100 }
    /// };
    /// 
    /// // Azure OpenAI-specific settings
    /// config.AdditionalSettings = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["api_version"] = "2024-02-15-preview",
    ///     ["deployment_name"] = "gpt-4-deployment",
    ///     ["content_filter"] = true,
    ///     ["entra_auth"] = false
    /// };
    /// 
    /// // Custom retry and logging settings
    /// config.AdditionalSettings = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["max_retries"] = 3,
    ///     ["retry_delay_ms"] = 1000,
    ///     ["log_requests"] = true,
    ///     ["log_responses"] = false,
    ///     ["enable_caching"] = true,
    ///     ["cache_duration_minutes"] = 60
    /// };
    /// </code>
    /// </example>
    public Dictionary<string, object> AdditionalSettings { get; set; } = new();
}