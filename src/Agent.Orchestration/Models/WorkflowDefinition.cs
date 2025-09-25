namespace Agent.Orchestration.Models;

/// <summary>
/// Represents a comprehensive workflow definition that encapsulates the complete blueprint for orchestrating
/// complex multi-agent processes in enterprise environments. This class serves as the declarative configuration
/// model that defines workflow structure, execution behavior, error handling policies, and operational parameters.
/// 
/// The workflow definition provides a flexible and powerful framework for describing sophisticated automation
/// processes that span multiple agents, services, and systems. It supports various execution patterns, advanced
/// error handling strategies, and enterprise-grade operational features including timeout management, retry
/// policies, and comprehensive configuration options.
/// 
/// Key Design Principles:
/// - **Declarative Configuration**: Workflow structure is defined through configuration rather than code
/// - **Execution Mode Flexibility**: Support for sequential, parallel, and dependency-based execution patterns
/// - **Fault Tolerance**: Built-in support for retry policies and error handling at multiple levels
/// - **Enterprise Integration**: Comprehensive configuration options for enterprise system integration
/// - **Operational Visibility**: Rich metadata and timestamping for monitoring and audit requirements
/// - **Scalability**: Design supports both simple linear workflows and complex multi-step processes
/// 
/// Execution Patterns:
/// - **Sequential Workflows**: Steps execute in a defined linear order, suitable for data pipelines and linear processes
/// - **Parallel Workflows**: All steps execute concurrently, optimal for independent operations and fan-out scenarios
/// - **Dependency Workflows**: Steps execute based on dependency relationships, enabling complex workflow topologies
/// 
/// Enterprise Features:
/// - **Timeout Management**: Granular timeout controls at both workflow and individual step levels
/// - **Retry Policies**: Configurable retry strategies with exponential backoff and circuit breaker patterns
/// - **Configuration Parameters**: Flexible key-value configuration system for environment-specific parameters
/// - **Audit Trail**: Complete timestamping and change tracking for compliance and operational monitoring
/// - **Validation Support**: Structure enables comprehensive pre-execution validation of workflow integrity
/// 
/// Production Considerations:
/// - Workflow definitions should be versioned and stored in configuration management systems
/// - Large workflows should be decomposed into smaller, reusable sub-workflows for maintainability
/// - Configuration parameters enable environment-specific behavior without code changes
/// - Timeout and retry policies should align with enterprise SLA requirements
/// - Consider implementing workflow templates for common patterns to ensure consistency
/// </summary>
/// <example>
/// Basic sequential workflow for data processing pipeline:
/// <code>
/// var workflow = new WorkflowDefinition
/// {
///     Id = Guid.NewGuid(),
///     Name = "Customer Data Processing Pipeline",
///     Description = "Processes customer data through validation, enrichment, and storage steps",
///     ExecutionMode = WorkflowExecutionMode.Sequential,
///     Timeout = TimeSpan.FromHours(2), // Maximum 2 hours for entire workflow
///     RetryPolicy = new RetryPolicy 
///     { 
///         MaxAttempts = 3, 
///         DelayBetweenAttempts = TimeSpan.FromMinutes(5) 
///     },
///     Configuration = new Dictionary&lt;string, object&gt;
///     {
///         ["Environment"] = "Production",
///         ["BatchSize"] = 1000,
///         ["EnableValidation"] = true
///     },
///     Steps = new List&lt;WorkflowStep&gt;
///     {
///         new()
///         {
///             Id = Guid.NewGuid(),
///             Name = "Extract Customer Data",
///             AgentId = "data-extractor",
///             Order = 1,
///             Input = new { Source = "CustomerDatabase", Query = "SELECT * FROM Customers WHERE Modified > @LastRun" },
///             Timeout = TimeSpan.FromMinutes(30),
///             ContinueOnFailure = false
///         },
///         new()
///         {
///             Id = Guid.NewGuid(),
///             Name = "Validate Data Quality",
///             AgentId = "data-validator",
///             Order = 2,
///             Input = new { ValidationRules = "StandardCustomerValidation", ReportErrors = true },
///             Timeout = TimeSpan.FromMinutes(15),
///             ContinueOnFailure = false
///         },
///         new()
///         {
///             Id = Guid.NewGuid(),
///             Name = "Enrich Customer Profiles",
///             AgentId = "data-enrichment",
///             Order = 3,
///             Input = new { EnrichmentServices = new[] { "AddressValidation", "CompanyData" } },
///             Timeout = TimeSpan.FromMinutes(45),
///             ContinueOnFailure = true // Continue even if enrichment fails
///         }
///     }
/// };
/// </code>
/// 
/// Advanced parallel workflow with complex error handling:
/// <code>
/// var parallelAnalysis = new WorkflowDefinition
/// {
///     Id = Guid.NewGuid(),
///     Name = "Multi-Channel Analytics Workflow",
///     Description = "Parallel analysis of sales, marketing, and customer service data",
///     ExecutionMode = WorkflowExecutionMode.Parallel,
///     Timeout = TimeSpan.FromHours(1),
///     RetryPolicy = new RetryPolicy 
///     { 
///         MaxAttempts = 2, 
///         DelayBetweenAttempts = TimeSpan.FromMinutes(10),
///         ExponentialBackoff = true
///     },
///     Configuration = new Dictionary&lt;string, object&gt;
///     {
///         ["AnalysisDate"] = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
///         ["OutputFormat"] = "JSON",
///         ["IncludeVisualizations"] = true,
///         ["NotificationEmail"] = "analytics-team@company.com"
///     },
///     Steps = new List&lt;WorkflowStep&gt;
///     {
///         new()
///         {
///             Id = Guid.NewGuid(),
///             Name = "Sales Performance Analysis",
///             AgentId = "sales-analytics",
///             Input = new { 
///                 DateRange = "Yesterday", 
///                 Metrics = new[] { "Revenue", "Units", "Margin" },
///                 Segments = new[] { "Region", "Product", "Channel" }
///             },
///             Timeout = TimeSpan.FromMinutes(30),
///             ContinueOnFailure = true
///         },
///         new()
///         {
///             Id = Guid.NewGuid(),
///             Name = "Marketing Campaign Analysis",
///             AgentId = "marketing-analytics",
///             Input = new { 
///                 CampaignTypes = new[] { "Email", "Social", "Display" },
///                 Metrics = new[] { "CTR", "Conversion", "ROI" }
///             },
///             Timeout = TimeSpan.FromMinutes(25),
///             ContinueOnFailure = true
///         },
///         new()
///         {
///             Id = Guid.NewGuid(),
///             Name = "Customer Service Metrics",
///             AgentId = "service-analytics", 
///             Input = new { 
///                 Metrics = new[] { "ResponseTime", "Resolution", "Satisfaction" },
///                 Channels = new[] { "Phone", "Email", "Chat", "Social" }
///             },
///             Timeout = TimeSpan.FromMinutes(20),
///             ContinueOnFailure = false // Critical for daily reporting
///         }
///     }
/// };
/// </code>
/// 
/// Complex dependency-based workflow for microservices integration:
/// <code>
/// var microserviceOrchestration = new WorkflowDefinition
/// {
///     Id = Guid.NewGuid(),
///     Name = "Order Processing Microservices Flow",
///     Description = "Orchestrates order processing across inventory, payment, and fulfillment services",
///     ExecutionMode = WorkflowExecutionMode.Dependency,
///     Timeout = TimeSpan.FromMinutes(10), // Fast processing for customer experience
///     RetryPolicy = new RetryPolicy 
///     { 
///         MaxAttempts = 3, 
///         DelayBetweenAttempts = TimeSpan.FromSeconds(30),
///         ExponentialBackoff = true
///     },
///     Configuration = new Dictionary&lt;string, object&gt;
///     {
///         ["Environment"] = "Production",
///         ["CircuitBreakerEnabled"] = true,
///         ["EnableDistributedTracing"] = true,
///         ["CustomerNotifications"] = true
///     },
///     Steps = new List&lt;WorkflowStep&gt;
///     {
///         new()
///         {
///             Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
///             Name = "Validate Order",
///             AgentId = "order-validation-service",
///             Dependencies = new List&lt;Guid&gt;(), // No dependencies - runs first
///             Input = new { ValidateInventory = true, ValidateCustomer = true },
///             Timeout = TimeSpan.FromSeconds(30),
///             ContinueOnFailure = false
///         },
///         new()
///         {
///             Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
///             Name = "Check Inventory",
///             AgentId = "inventory-service",
///             Dependencies = new List&lt;Guid&gt; { Guid.Parse("10000000-0000-0000-0000-000000000001") },
///             Input = new { ReservationTimeout = TimeSpan.FromMinutes(5) },
///             Timeout = TimeSpan.FromSeconds(45),
///             ContinueOnFailure = false
///         },
///         new()
///         {
///             Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
///             Name = "Process Payment",
///             AgentId = "payment-service",
///             Dependencies = new List&lt;Guid&gt; 
///             { 
///                 Guid.Parse("10000000-0000-0000-0000-000000000001"),
///                 Guid.Parse("10000000-0000-0000-0000-000000000002")
///             },
///             Input = new { PaymentMethod = "CreditCard", RequireAuthorization = true },
///             Timeout = TimeSpan.FromSeconds(60),
///             ContinueOnFailure = false
///         },
///         new()
///         {
///             Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
///             Name = "Initiate Fulfillment",
///             AgentId = "fulfillment-service",
///             Dependencies = new List&lt;Guid&gt; { Guid.Parse("10000000-0000-0000-0000-000000000003") },
///             Input = new { ShippingMethod = "Standard", GenerateTracking = true },
///             Timeout = TimeSpan.FromSeconds(30),
///             ContinueOnFailure = false
///         }
///     }
/// };
/// </code>
/// 
/// Workflow template pattern for reusable configurations:
/// <code>
/// public static class WorkflowTemplates
/// {
///     public static WorkflowDefinition CreateDataProcessingTemplate(string name, string sourceTable, string[] validationRules)
///     {
///         return new WorkflowDefinition
///         {
///             Id = Guid.NewGuid(),
///             Name = name,
///             Description = $"Data processing workflow for {sourceTable}",
///             ExecutionMode = WorkflowExecutionMode.Sequential,
///             Timeout = TimeSpan.FromHours(4),
///             RetryPolicy = new RetryPolicy { MaxAttempts = 2, DelayBetweenAttempts = TimeSpan.FromMinutes(5) },
///             Configuration = new Dictionary&lt;string, object&gt;
///             {
///                 ["SourceTable"] = sourceTable,
///                 ["ValidationRules"] = validationRules,
///                 ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
///             },
///             Steps = CreateStandardDataProcessingSteps(sourceTable, validationRules)
///         };
///     }
/// }
/// 
/// // Usage
/// var customerWorkflow = WorkflowTemplates.CreateDataProcessingTemplate(
///     "Customer Data Processing", 
///     "Customers", 
///     new[] { "EmailValidation", "PhoneValidation", "AddressValidation" }
/// );
/// </code>
/// </example>
/// <remarks>
/// Design Patterns and Best Practices:
/// - Use meaningful, descriptive names for workflows and steps to improve maintainability
/// - Configure appropriate timeouts based on expected execution times and SLA requirements
/// - Implement retry policies that balance resilience with resource utilization
/// - Use configuration parameters for environment-specific values rather than hardcoding
/// - Group related configuration parameters using consistent naming conventions
/// 
/// Validation and Error Handling:
/// - Workflow definitions should be validated before execution to catch configuration errors
/// - Step dependencies must form a valid directed acyclic graph (no circular dependencies)
/// - Timeout values should be reasonable and allow for expected processing variations
/// - Retry policies should include appropriate delays to avoid overwhelming downstream systems
/// 
/// Performance and Scalability:
/// - Large workflows benefit from decomposition into smaller, focused sub-workflows
/// - Parallel execution can significantly improve throughput for independent operations
/// - Dependency-based workflows optimize execution order for complex process topologies
/// - Consider step granularity - too fine-grained steps add overhead, too coarse-grained reduces flexibility
/// 
/// Monitoring and Observability:
/// - Include sufficient metadata in workflow definitions for operational monitoring
/// - Use consistent naming conventions to enable effective monitoring and alerting
/// - Configuration parameters should include operational settings like logging levels and notification preferences
/// - Timestamp fields enable comprehensive audit trails for compliance and troubleshooting
/// 
/// Enterprise Integration:
/// - Store workflow definitions in version-controlled configuration management systems
/// - Implement approval workflows for production workflow changes
/// - Use environment-specific configuration parameters to promote workflows across environments
/// - Consider implementing workflow definition schemas for validation and tooling support
/// 
/// Security Considerations:
/// - Avoid storing sensitive information directly in workflow definitions
/// - Use configuration references to secret management systems for sensitive parameters
/// - Implement appropriate access controls for workflow definition management
/// - Consider encryption for workflow definitions containing sensitive business logic
/// </remarks>
public class WorkflowDefinition
{
    /// <summary>
    /// Unique identifier for the workflow
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable name for the workflow
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the workflow
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Execution mode for the workflow steps
    /// </summary>
    public WorkflowExecutionMode ExecutionMode { get; set; } = WorkflowExecutionMode.Sequential;

    /// <summary>
    /// Collection of workflow steps
    /// </summary>
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>
    /// Maximum execution time for the entire workflow
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Retry policy for failed steps
    /// </summary>
    public RetryPolicy? RetryPolicy { get; set; }

    /// <summary>
    /// Workflow-level configuration parameters
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Timestamp when workflow was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when workflow was last modified
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}