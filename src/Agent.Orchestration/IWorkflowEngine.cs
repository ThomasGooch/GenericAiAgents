using Agent.Core;
using Agent.Orchestration.Models;

namespace Agent.Orchestration;

/// <summary>
/// Interface for executing workflows with multiple agents
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Executes a workflow definition
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Workflow execution result</returns>
    Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an agent with the workflow engine
    /// </summary>
    /// <param name="agent">The agent to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterAgentAsync(IAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a registered agent by ID
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent or null if not found</returns>
    Task<IAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered agents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of registered agents</returns>
    Task<IEnumerable<IAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow definition
    /// </summary>
    /// <param name="workflow">The workflow to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any errors</returns>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the execution status of a running workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current execution status</returns>
    Task<WorkflowExecutionStatus?> GetExecutionStatusAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if workflow was cancelled successfully</returns>
    Task<bool> CancelWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of workflow validation
/// </summary>
public class WorkflowValidationResult
{
    /// <summary>
    /// Whether the workflow is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warning messages
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Current execution status of a workflow
/// </summary>
public class WorkflowExecutionStatus
{
    /// <summary>
    /// Workflow ID
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Current execution state
    /// </summary>
    public WorkflowState State { get; set; }

    /// <summary>
    /// Currently executing steps
    /// </summary>
    public List<Guid> ExecutingSteps { get; set; } = new();

    /// <summary>
    /// Completed steps
    /// </summary>
    public List<Guid> CompletedSteps { get; set; } = new();

    /// <summary>
    /// Failed steps
    /// </summary>
    public List<Guid> FailedSteps { get; set; } = new();

    /// <summary>
    /// Execution progress percentage
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Last update time
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Workflow execution states
/// </summary>
public enum WorkflowState
{
    /// <summary>
    /// Workflow is pending execution
    /// </summary>
    Pending,

    /// <summary>
    /// Workflow is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Workflow completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Workflow failed with errors
    /// </summary>
    Failed,

    /// <summary>
    /// Workflow was cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Workflow execution timed out
    /// </summary>
    TimedOut
}