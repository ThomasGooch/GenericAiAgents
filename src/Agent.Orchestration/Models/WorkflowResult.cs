namespace Agent.Orchestration.Models;

/// <summary>
/// Result of workflow execution
/// </summary>
public class WorkflowResult
{
    /// <summary>
    /// Whether the workflow completed successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Whether the workflow completed successfully (backward compatibility)
    /// </summary>
    [Obsolete("Use IsSuccess property instead.")]
    public bool Success
    {
        get => IsSuccess;
        set => IsSuccess = value;
    }

    /// <summary>
    /// Error message if workflow failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error message if workflow failed (backward compatibility)
    /// </summary>
    [Obsolete("Use ErrorMessage property instead.")]
    public string? Error
    {
        get => ErrorMessage;
        set => ErrorMessage = value;
    }

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
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Whether the step completed successfully (backward compatibility)
    /// </summary>
    [Obsolete("Use IsSuccess property instead.")]
    public bool Success
    {
        get => IsSuccess;
        set => IsSuccess = value;
    }

    /// <summary>
    /// Output from the step execution
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Output from the step execution (backward compatibility)
    /// </summary>
    [Obsolete("Use Data property instead.")]
    public string? Output
    {
        get => Data;
        set => Data = value;
    }

    /// <summary>
    /// Error message if step failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error message if step failed (backward compatibility)
    /// </summary>
    [Obsolete("Use ErrorMessage property instead.")]
    public string? Error
    {
        get => ErrorMessage;
        set => ErrorMessage = value;
    }

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