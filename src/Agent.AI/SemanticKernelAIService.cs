using Agent.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Agent.AI;

/// <summary>
/// AI service implementation using Microsoft Semantic Kernel
/// </summary>
public class SemanticKernelAIService : IAIService
{
    private readonly ILogger<SemanticKernelAIService> _logger;
    private Kernel? _kernel;
    private IChatCompletionService? _chatService;
    private AIConfiguration? _configuration;
    private bool _isInitialized;

    public SemanticKernelAIService(ILogger<SemanticKernelAIService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(AIConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            throw new ArgumentException("API Key cannot be null or empty", nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.ModelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(configuration));

        try
        {
            var builder = Kernel.CreateBuilder();

            // Configure the AI service based on provider
            switch (configuration.Provider.ToUpperInvariant())
            {
                case "OPENAI":
                    builder.AddOpenAIChatCompletion(
                        modelId: configuration.ModelId,
                        apiKey: configuration.ApiKey);
                    break;

                case "AZUREOPENAI":
                    if (string.IsNullOrWhiteSpace(configuration.Endpoint))
                        throw new ArgumentException("Endpoint is required for Azure OpenAI provider", nameof(configuration));

                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: configuration.ModelId,
                        endpoint: configuration.Endpoint,
                        apiKey: configuration.ApiKey);
                    break;

                default:
                    throw new ArgumentException($"Unsupported AI provider: {configuration.Provider}", nameof(configuration));
            }

            _kernel = builder.Build();
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
            _configuration = configuration;
            _isInitialized = true;

            _logger.LogInformation("AI service initialized with provider: {Provider}, Model: {ModelId}",
                configuration.Provider, configuration.ModelId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI service with provider: {Provider}", configuration.Provider);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AIResponse> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await GetCompletionWithSettingsAsync(prompt, null, null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AIResponse> GetCompletionWithSettingsAsync(string prompt, int? maxTokens = null, double? temperature = null, CancellationToken cancellationToken = default)
    {
        ThrowIfNotInitialized();

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var settings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = maxTokens ?? _configuration!.MaxTokens,
                Temperature = temperature ?? _configuration!.Temperature,
                TopP = _configuration!.TopP
            };

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var result = await _chatService!.GetChatMessageContentAsync(
                history,
                executionSettings: settings,
                cancellationToken: cancellationToken);

            stopwatch.Stop();

            var tokensUsed = 0;
            if (result.Metadata?.TryGetValue("Usage", out var usage) == true)
            {
                // Try to extract token count from usage metadata
                if (usage != null && usage.ToString()?.Contains("TotalTokens") == true)
                {
                    // Parse token usage from metadata (implementation may vary by provider)
                    tokensUsed = 1; // Placeholder - actual implementation would parse the usage object
                }
            }

            _logger.LogDebug("AI completion successful. Tokens: {Tokens}, Duration: {Duration}ms",
                tokensUsed, stopwatch.ElapsedMilliseconds);

            return AIResponse.CreateSuccess(
                content: result.Content ?? string.Empty,
                tokensUsed: tokensUsed,
                modelUsed: _configuration!.ModelId,
                duration: stopwatch.Elapsed,
                metadata: result.Metadata?.ToDictionary(kv => kv.Key, kv => (object)(kv.Value ?? string.Empty)) ?? new Dictionary<string, object>());
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("AI completion was cancelled after {Duration}ms", stopwatch.ElapsedMilliseconds);
            return AIResponse.CreateError("Operation was cancelled", stopwatch.Elapsed, _configuration!.ModelId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "AI completion failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            return AIResponse.CreateError($"AI completion failed: {ex.Message}", stopwatch.Elapsed, _configuration?.ModelId ?? "unknown");
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GetStreamingCompletionAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfNotInitialized();

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));

        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = _configuration!.MaxTokens,
            Temperature = _configuration!.Temperature,
            TopP = _configuration!.TopP
        };

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        await foreach (var contentPiece in _chatService!.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings: settings,
            cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(contentPiece.Content))
            {
                yield return contentPiece.Content;
            }
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isInitialized && _kernel != null && _chatService != null);
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            return false;
        }

        try
        {
            // Simple test prompt to verify connection
            var testResponse = await GetCompletionWithSettingsAsync("Hello", maxTokens: 5, cancellationToken: cancellationToken);
            return testResponse.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed");
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<Dictionary<string, object>> GetModelInfoAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotInitialized();

        var info = new Dictionary<string, object>
        {
            ["Provider"] = _configuration!.Provider,
            ["ModelId"] = _configuration.ModelId,
            ["Endpoint"] = _configuration.Endpoint,
            ["MaxTokens"] = _configuration.MaxTokens,
            ["Temperature"] = _configuration.Temperature,
            ["TopP"] = _configuration.TopP,
            ["TimeoutSeconds"] = _configuration.TimeoutSeconds,
            ["IsInitialized"] = _isInitialized
        };

        return Task.FromResult(info);
    }

    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("AI service has not been initialized. Call InitializeAsync first.");
        }
    }
}