using Agent.Core;
using Agent.Core.Models;
using Agent.AI;

namespace BasicIntegration;

/// <summary>
/// Custom agent that demonstrates how to create specialized AI agents using GenericAgents
/// </summary>
public class CustomerServiceAgent : BaseAgent
{
    private readonly IAIService _aiService;
    private readonly ILogger<CustomerServiceAgent> _logger;

    public CustomerServiceAgent(
        IAIService aiService,
        ILogger<CustomerServiceAgent> logger)
        : base("customer-service-agent", "AI-powered customer service assistant that analyzes inquiries and provides intelligent responses")
    {
        _aiService = aiService;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing customer service request: {RequestId}", request.RequestId);

            // Create a comprehensive customer service prompt
            var prompt = $@"
You are a professional customer service AI assistant. Analyze this customer message and provide a comprehensive response in JSON format.

Customer Message: ""{request.Message}""

Please provide a JSON response with the following structure:
{{
    ""sentiment"": ""positive/negative/neutral"",
    ""sentiment_confidence"": 0.0-1.0,
    ""category"": ""billing/technical/general_inquiry/complaint/compliment"",
    ""urgency_level"": ""low/medium/high/critical"",
    ""key_issues"": [""list of main issues identified""],
    ""suggested_actions"": [""recommended actions to resolve""],
    ""response_draft"": ""professional response to customer"",
    ""escalation_needed"": true/false,
    ""estimated_resolution_time"": ""time estimate"",
    ""customer_satisfaction_risk"": ""low/medium/high"",
    ""follow_up_required"": true/false
}}

Analyze the message thoroughly and provide actionable insights for customer service representatives.
";

            var aiResponse = await _aiService.ProcessRequestAsync(prompt, cancellationToken);

            // Create comprehensive result
            var processingResult = new
            {
                CustomerMessage = request.Message,
                AIAnalysis = aiResponse.Content,
                ProcessingMetadata = new
                {
                    RequestId = request.RequestId,
                    ProcessedBy = Name,
                    ProcessingTime = aiResponse.ProcessingTime,
                    ModelUsed = aiResponse.ModelId,
                    TokensUsed = aiResponse.TokensUsed,
                    Timestamp = DateTime.UtcNow
                },
                AgentConfiguration = new
                {
                    AgentId = Id,
                    AgentName = Name,
                    AgentDescription = Description,
                    Version = "1.0.0"
                }
            };

            _logger.LogInformation("Customer service request processed successfully: {RequestId} in {ProcessingTime}ms",
                request.RequestId, aiResponse.ProcessingTime.TotalMilliseconds);

            return AgentResult.CreateSuccess(System.Text.Json.JsonSerializer.Serialize(processingResult, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer service request {RequestId}", request.RequestId);
            return AgentResult.CreateError($"Customer service processing failed: {ex.Message}");
        }
    }

    protected override async Task OnInitializeAsync(AgentConfiguration configuration, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing CustomerServiceAgent with configuration: {ConfigName}", configuration.Name);

        // Perform any initialization logic here
        // For example, verify AI service is available
        try
        {
            var isHealthy = await _aiService.IsHealthyAsync(cancellationToken);
            if (!isHealthy)
            {
                _logger.LogWarning("AI service is not healthy during agent initialization");
            }
            else
            {
                _logger.LogInformation("AI service verified as healthy during agent initialization");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify AI service health during initialization");
        }

        await base.OnInitializeAsync(configuration, cancellationToken);
        
        _logger.LogInformation("CustomerServiceAgent initialization completed successfully");
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _logger.LogInformation("Disposing CustomerServiceAgent: {AgentName}", Name);
        
        // Perform any cleanup logic here
        
        await base.OnDisposeAsync();
    }

    /// <summary>
    /// Custom method to get agent capabilities
    /// </summary>
    public Dictionary<string, object> GetCapabilities()
    {
        return new Dictionary<string, object>
        {
            { "sentiment_analysis", true },
            { "issue_categorization", true },
            { "urgency_assessment", true },
            { "response_generation", true },
            { "escalation_detection", true },
            { "supported_categories", new[] { "billing", "technical", "general_inquiry", "complaint", "compliment" } },
            { "supported_languages", new[] { "english" } },
            { "max_message_length", 10000 },
            { "average_processing_time_ms", 2000 }
        };
    }
}