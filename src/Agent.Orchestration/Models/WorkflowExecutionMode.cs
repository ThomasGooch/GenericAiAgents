namespace Agent.Orchestration.Models;

/// <summary>
/// Defines how workflow steps should be executed
/// </summary>
public enum WorkflowExecutionMode
{
    /// <summary>
    /// Execute steps one after another in order
    /// </summary>
    Sequential,

    /// <summary>
    /// Execute all steps concurrently
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute steps based on dependency graph
    /// </summary>
    Dependency
}