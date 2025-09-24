namespace Agent.Orchestration.Models;

/// <summary>
/// Represents a single step in a workflow
/// </summary>
public class WorkflowStep
{
    /// <summary>
    /// Unique identifier for the step
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the step
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ID of the agent that will execute this step
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Input data for the agent
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Execution order (lower numbers execute first)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Dependencies that must complete before this step
    /// </summary>
    public List<Guid> Dependencies { get; set; } = new();

    /// <summary>
    /// Step-specific configuration
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Maximum execution time for this step
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Whether step failure should stop workflow execution
    /// </summary>
    public bool ContinueOnFailure { get; set; } = false;

    /// <summary>
    /// Expected output validation rules
    /// </summary>
    public List<OutputValidationRule> ValidationRules { get; set; } = new();
}

/// <summary>
/// Rule for validating step output
/// </summary>
public class OutputValidationRule
{
    /// <summary>
    /// Type of validation to perform
    /// </summary>
    public ValidationType Type { get; set; }

    /// <summary>
    /// Value to validate against
    /// </summary>
    public string ExpectedValue { get; set; } = string.Empty;

    /// <summary>
    /// Error message if validation fails
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Types of output validation
/// </summary>
public enum ValidationType
{
    Contains,
    Equals,
    StartsWith,
    EndsWith,
    Regex,
    NotEmpty,
    IsJson,
    IsXml
}