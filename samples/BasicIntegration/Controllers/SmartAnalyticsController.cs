using Agent.AI;
using Agent.Core.Models;
using Agent.Registry;
using Microsoft.AspNetCore.Mvc;

namespace BasicIntegration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmartAnalyticsController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<SmartAnalyticsController> _logger;

    public SmartAnalyticsController(
        IAIService aiService,
        IToolRegistry toolRegistry,
        ILogger<SmartAnalyticsController> logger)
    {
        _aiService = aiService;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes text using AI to extract sentiment, topics, and insights
    /// </summary>
    [HttpPost("analyze-text")]
    public async Task<IActionResult> AnalyzeText([FromBody] AnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing text with {Length} characters", request.Text.Length);

            // Create AI prompt for text analysis
            var prompt = $@"
Please analyze the following text and provide a JSON response with:
1. sentiment: (positive/negative/neutral)
2. confidence: (0.0 to 1.0)
3. key_topics: array of main topics
4. summary: brief summary in 1-2 sentences
5. insights: interesting observations

Text to analyze: ""{request.Text}""

Please respond with valid JSON only.
";

            var aiResponse = await _aiService.ProcessRequestAsync(prompt, HttpContext.RequestAborted);

            var result = new
            {
                OriginalText = request.Text,
                AIAnalysis = aiResponse.Content,
                ProcessingInfo = new
                {
                    ModelUsed = aiResponse.ModelId,
                    TokensUsed = aiResponse.TokensUsed,
                    ProcessingTime = aiResponse.ProcessingTime,
                    Timestamp = DateTime.UtcNow
                },
                RequestId = Guid.NewGuid().ToString()
            };

            _logger.LogInformation("Text analysis completed successfully in {ProcessingTime}ms", 
                aiResponse.ProcessingTime.TotalMilliseconds);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing text");
            return StatusCode(500, new 
            { 
                Error = "Analysis failed", 
                Details = ex.Message,
                RequestId = Guid.NewGuid().ToString()
            });
        }
    }

    /// <summary>
    /// Generates content based on a prompt
    /// </summary>
    [HttpPost("generate-content")]
    public async Task<IActionResult> GenerateContent([FromBody] GenerationRequest request)
    {
        try
        {
            _logger.LogInformation("Generating content for type: {ContentType}", request.ContentType);

            var prompt = $@"
Generate {request.ContentType} content based on this request: {request.Prompt}

Requirements:
- Target audience: {request.TargetAudience ?? "General"}
- Tone: {request.Tone ?? "Professional"}
- Length: {request.MaxLength ?? 500} words maximum

Please create engaging, high-quality content that meets these requirements.
";

            var aiResponse = await _aiService.ProcessRequestAsync(prompt, HttpContext.RequestAborted);

            var result = new
            {
                GeneratedContent = aiResponse.Content,
                RequestDetails = new
                {
                    ContentType = request.ContentType,
                    TargetAudience = request.TargetAudience,
                    Tone = request.Tone,
                    MaxLength = request.MaxLength
                },
                ProcessingInfo = new
                {
                    ModelUsed = aiResponse.ModelId,
                    TokensUsed = aiResponse.TokensUsed,
                    ProcessingTime = aiResponse.ProcessingTime,
                    Timestamp = DateTime.UtcNow
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content");
            return StatusCode(500, new { Error = "Content generation failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Lists all available tools in the system
    /// </summary>
    [HttpGet("available-tools")]
    public async Task<IActionResult> GetAvailableTools()
    {
        try
        {
            var tools = await _toolRegistry.GetAllToolsAsync();
            
            var toolList = tools.Select(tool => new
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = tool.GetParameterSchema().Keys.ToArray()
            }).ToArray();

            return Ok(new
            {
                ToolCount = toolList.Length,
                Tools = toolList,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tools");
            return StatusCode(500, new { Error = "Failed to retrieve tools", Details = ex.Message });
        }
    }

    /// <summary>
    /// Executes a specific tool with given parameters
    /// </summary>
    [HttpPost("execute-tool")]
    public async Task<IActionResult> ExecuteTool([FromBody] ToolExecutionRequest request)
    {
        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", request.ToolName);

            var tool = await _toolRegistry.GetToolAsync(request.ToolName);
            if (tool == null)
            {
                return NotFound(new { Error = $"Tool '{request.ToolName}' not found" });
            }

            // Validate parameters
            if (!tool.ValidateParameters(request.Parameters))
            {
                return BadRequest(new 
                { 
                    Error = "Invalid parameters", 
                    ExpectedParameters = tool.GetParameterSchema() 
                });
            }

            var result = await tool.ExecuteAsync(request.Parameters, HttpContext.RequestAborted);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    ToolName = request.ToolName,
                    Result = result.Data,
                    ExecutionTime = result.ExecutionTime,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new
                {
                    ToolName = request.ToolName,
                    Error = result.ErrorMessage,
                    ExecutionTime = result.ExecutionTime
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", request.ToolName);
            return StatusCode(500, new { Error = "Tool execution failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Health check for the analytics service
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            // Check AI service
            var aiHealthy = await _aiService.IsHealthyAsync(HttpContext.RequestAborted);
            
            // Check tool registry
            var tools = await _toolRegistry.GetAllToolsAsync();
            
            return Ok(new
            {
                Status = aiHealthy ? "Healthy" : "Degraded",
                Services = new
                {
                    AIService = aiHealthy ? "Available" : "Unavailable",
                    ToolRegistry = tools.Any() ? "Available" : "No tools registered"
                },
                ToolCount = tools.Count(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
        }
    }
}

// Request/Response Models
public class AnalysisRequest
{
    public string Text { get; set; } = string.Empty;
}

public class GenerationRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string ContentType { get; set; } = "article"; // article, email, social-post, etc.
    public string? TargetAudience { get; set; }
    public string? Tone { get; set; }
    public int? MaxLength { get; set; }
}

public class ToolExecutionRequest
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}