namespace Agent.Orchestration.Models;

/// <summary>
/// Result of workflow execution
/// </summary>
public class WorkflowResult
{
    /// <summary>
    /// Whether the workflow completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if workflow failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Results from each step execution
    /// </summary>
    public List<WorkflowStepResult> StepResults { get; set; } = new();

    /// <summary>
    /// Total execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Timestamp when execution started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when execution completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Workflow metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of a single workflow step execution
/// </summary>
public class WorkflowStepResult
{
    /// <summary>
    /// ID of the executed step
    /// </summary>
    public Guid StepId { get; set; }

    /// <summary>
    /// Name of the executed step
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the agent that executed the step
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the step completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Output from the step execution
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Error message if step failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Step execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Timestamp when step started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when step completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Step-specific metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}