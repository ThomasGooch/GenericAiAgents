using Agent.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicIntegration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerServiceController : ControllerBase
{
    private readonly CustomerServiceAgent _agent;
    private readonly ILogger<CustomerServiceController> _logger;

    public CustomerServiceController(
        CustomerServiceAgent agent,
        ILogger<CustomerServiceController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Processes a customer inquiry using the AI-powered customer service agent
    /// </summary>
    [HttpPost("process-inquiry")]
    public async Task<IActionResult> ProcessInquiry([FromBody] CustomerInquiry inquiry)
    {
        try
        {
            // Initialize agent if not already done
            if (!_agent.IsInitialized)
            {
                await _agent.InitializeAsync(new AgentConfiguration
                {
                    Name = "customer-service",
                    Description = "AI-powered customer service agent",
                    Timeout = TimeSpan.FromMinutes(2),
                    Settings = new Dictionary<string, object>
                    {
                        { "max_retries", 3 },
                        { "enable_logging", true },
                        { "response_format", "json" }
                    }
                });
            }

            // Create agent request
            var agentRequest = new AgentRequest
            {
                Message = inquiry.Message,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                UserId = inquiry.CustomerEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "customer_email", inquiry.CustomerEmail },
                    { "inquiry_type", inquiry.InquiryType ?? "general" },
                    { "priority", inquiry.Priority ?? "normal" },
                    { "source", "api" }
                }
            };

            _logger.LogInformation("Processing customer inquiry from {CustomerEmail} with RequestId {RequestId}",
                inquiry.CustomerEmail, agentRequest.RequestId);

            // Execute the agent
            var result = await _agent.ExecuteAsync(agentRequest);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Customer inquiry processed successfully: {RequestId} in {ProcessingTime}ms",
                    agentRequest.RequestId, result.ProcessingTime.TotalMilliseconds);

                return Ok(new CustomerServiceResponse
                {
                    RequestId = agentRequest.RequestId,
                    CustomerInquiry = inquiry.Message,
                    CustomerEmail = inquiry.CustomerEmail,
                    AgentResponse = result.Data,
                    ProcessingTime = result.ProcessingTime,
                    Status = "success",
                    Timestamp = DateTime.UtcNow,
                    AgentInfo = new AgentInfo
                    {
                        AgentId = _agent.Id,
                        AgentName = _agent.Name,
                        AgentDescription = _agent.Description
                    }
                });
            }
            else
            {
                _logger.LogWarning("Customer inquiry processing failed: {RequestId} - {ErrorMessage}",
                    agentRequest.RequestId, result.ErrorMessage);

                return BadRequest(new CustomerServiceResponse
                {
                    RequestId = agentRequest.RequestId,
                    CustomerInquiry = inquiry.Message,
                    CustomerEmail = inquiry.CustomerEmail,
                    Status = "error",
                    ErrorMessage = result.ErrorMessage,
                    ProcessingTime = result.ProcessingTime,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing customer inquiry from {CustomerEmail}",
                inquiry.CustomerEmail);

            return StatusCode(500, new CustomerServiceResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                CustomerInquiry = inquiry.Message,
                CustomerEmail = inquiry.CustomerEmail,
                Status = "error",
                ErrorMessage = $"Internal error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Gets the current health status of the customer service agent
    /// </summary>
    [HttpGet("agent-health")]
    public async Task<IActionResult> GetAgentHealth()
    {
        try
        {
            var health = await _agent.CheckHealthAsync();

            return Ok(new
            {
                AgentHealth = health,
                AgentInfo = new
                {
                    Id = _agent.Id,
                    Name = _agent.Name,
                    Description = _agent.Description,
                    IsInitialized = _agent.IsInitialized
                },
                Capabilities = _agent.GetCapabilities(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking agent health");
            return StatusCode(500, new { Error = "Failed to check agent health", Details = ex.Message });
        }
    }

    /// <summary>
    /// Gets information about the customer service agent's capabilities
    /// </summary>
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        try
        {
            var capabilities = _agent.GetCapabilities();
            
            return Ok(new
            {
                AgentInfo = new
                {
                    Id = _agent.Id,
                    Name = _agent.Name,
                    Description = _agent.Description,
                    IsInitialized = _agent.IsInitialized
                },
                Capabilities = capabilities,
                SupportedOperations = new[]
                {
                    "sentiment_analysis",
                    "issue_categorization",
                    "urgency_assessment",
                    "response_generation",
                    "escalation_detection"
                },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agent capabilities");
            return StatusCode(500, new { Error = "Failed to retrieve capabilities", Details = ex.Message });
        }
    }

    /// <summary>
    /// Processes multiple customer inquiries in batch
    /// </summary>
    [HttpPost("process-batch")]
    public async Task<IActionResult> ProcessBatchInquiries([FromBody] BatchInquiryRequest batchRequest)
    {
        try
        {
            // Initialize agent if not already done
            if (!_agent.IsInitialized)
            {
                await _agent.InitializeAsync(new AgentConfiguration
                {
                    Name = "customer-service-batch",
                    Description = "Batch processing customer service agent",
                    Timeout = TimeSpan.FromMinutes(5),
                    MaxRetries = 2
                });
            }

            var batchId = Guid.NewGuid().ToString();
            _logger.LogInformation("Processing batch of {Count} inquiries with BatchId {BatchId}",
                batchRequest.Inquiries.Count, batchId);

            var results = new List<CustomerServiceResponse>();
            var tasks = new List<Task<CustomerServiceResponse>>();

            // Process inquiries concurrently (but limit concurrency)
            var semaphore = new SemaphoreSlim(Math.Min(batchRequest.Inquiries.Count, 5));

            foreach (var inquiry in batchRequest.Inquiries)
            {
                tasks.Add(ProcessSingleInquiryAsync(inquiry, batchId, semaphore));
            }

            var batchResults = await Task.WhenAll(tasks);

            return Ok(new
            {
                BatchId = batchId,
                TotalInquiries = batchRequest.Inquiries.Count,
                SuccessfullyProcessed = batchResults.Count(r => r.Status == "success"),
                Failed = batchResults.Count(r => r.Status == "error"),
                Results = batchResults,
                ProcessingStarted = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch inquiries");
            return StatusCode(500, new { Error = "Batch processing failed", Details = ex.Message });
        }
    }

    private async Task<CustomerServiceResponse> ProcessSingleInquiryAsync(
        CustomerInquiry inquiry, 
        string batchId, 
        SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var agentRequest = new AgentRequest
            {
                Message = inquiry.Message,
                RequestId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                UserId = inquiry.CustomerEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "batch_id", batchId },
                    { "customer_email", inquiry.CustomerEmail },
                    { "inquiry_type", inquiry.InquiryType ?? "general" },
                    { "priority", inquiry.Priority ?? "normal" }
                }
            };

            var result = await _agent.ExecuteAsync(agentRequest);

            return new CustomerServiceResponse
            {
                RequestId = agentRequest.RequestId,
                CustomerInquiry = inquiry.Message,
                CustomerEmail = inquiry.CustomerEmail,
                AgentResponse = result.IsSuccess ? result.Data : null,
                Status = result.IsSuccess ? "success" : "error",
                ErrorMessage = result.IsSuccess ? null : result.ErrorMessage,
                ProcessingTime = result.ProcessingTime,
                Timestamp = DateTime.UtcNow
            };
        }
        finally
        {
            semaphore.Release();
        }
    }
}

// Request/Response Models
public class CustomerInquiry
{
    public string Message { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? InquiryType { get; set; }
    public string? Priority { get; set; }
}

public class CustomerServiceResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string CustomerInquiry { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? AgentResponse { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public DateTime Timestamp { get; set; }
    public AgentInfo? AgentInfo { get; set; }
}

public class AgentInfo
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string AgentDescription { get; set; } = string.Empty;
}

public class BatchInquiryRequest
{
    public List<CustomerInquiry> Inquiries { get; set; } = new();
}