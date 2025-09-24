namespace Agent.Orchestration.Models;

/// <summary>
/// Defines a workflow with steps and execution parameters
/// </summary>
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