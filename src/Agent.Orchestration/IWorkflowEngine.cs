using Agent.Core;
using Agent.Orchestration.Models;

namespace Agent.Orchestration;

/// <summary>
/// Defines the contract for workflow orchestration, enabling the execution of complex multi-agent workflows
/// with dependency management, parallel processing, error handling, and monitoring capabilities.
/// Provides enterprise-grade workflow automation for AI agent systems.
/// </summary>
/// <remarks>
/// <para>
/// The IWorkflowEngine interface provides comprehensive workflow orchestration capabilities for complex
/// AI agent scenarios. It supports sequential and parallel execution patterns, conditional branching,
/// error recovery, and real-time monitoring of workflow progress.
/// </para>
/// <para>
/// **Key Orchestration Features:**
/// - **Multi-Agent Coordination**: Execute workflows across multiple specialized agents
/// - **Dependency Management**: Handle step dependencies and execution ordering
/// - **Parallel Execution**: Execute independent steps concurrently for performance
/// - **Error Handling**: Built-in retry logic, error recovery, and fallback strategies
/// - **Real-time Monitoring**: Track workflow progress and step-level execution status
/// - **Dynamic Cancellation**: Cancel running workflows and handle graceful shutdown
/// </para>
/// <para>
/// **Workflow Execution Patterns:**
/// - **Sequential Processing**: Step-by-step execution with data flow between agents
/// - **Parallel Processing**: Concurrent execution of independent workflow branches
/// - **Conditional Branching**: Dynamic workflow paths based on intermediate results
/// - **Loop and Iteration**: Repeated execution of workflow segments with conditions
/// - **Event-Driven**: Trigger-based workflow initiation and step execution
/// </para>
/// <para>
/// **Enterprise Integration:**
/// The workflow engine integrates with monitoring systems, audit logging, security frameworks,
/// and external systems to provide production-ready workflow automation suitable for
/// enterprise environments with compliance and governance requirements.
/// </para>
/// <para>
/// **Performance and Scalability:**
/// Implementations should support high-throughput workflow execution with efficient resource
/// utilization, connection pooling, and horizontal scaling capabilities for enterprise workloads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic workflow setup and execution
/// var workflowEngine = serviceProvider.GetRequiredService&lt;IWorkflowEngine&gt;();
/// 
/// // Register specialized agents
/// await workflowEngine.RegisterAgentAsync(new DocumentProcessingAgent(), cancellationToken);
/// await workflowEngine.RegisterAgentAsync(new DataAnalysisAgent(), cancellationToken);
/// await workflowEngine.RegisterAgentAsync(new ReportGenerationAgent(), cancellationToken);
/// 
/// // Define a document analysis workflow
/// var workflow = new WorkflowDefinition
/// {
///     Id = Guid.NewGuid(),
///     Name = "DocumentAnalysisWorkflow",
///     Description = "Process documents through analysis and generate reports",
///     Steps = new List&lt;WorkflowStep&gt;
///     {
///         new WorkflowStep
///         {
///             Id = Guid.NewGuid(),
///             Name = "ExtractContent",
///             AgentId = "document-processor",
///             Input = new { DocumentPath = "/uploads/contract.pdf" },
///             Configuration = new Dictionary&lt;string, object&gt;
///             {
///                 ["ExtractImages"] = true,
///                 ["OCREnabled"] = true,
///                 ["Quality"] = "High"
///             }
///         },
///         new WorkflowStep
///         {
///             Id = Guid.NewGuid(),
///             Name = "AnalyzeContent",
///             AgentId = "data-analysis",
///             Dependencies = new[] { "ExtractContent" }, // Depends on previous step
///             Input = "{{ExtractContent.Output.Text}}", // Data flow from previous step
///             Configuration = new Dictionary&lt;string, object&gt;
///             {
///                 ["AnalysisType"] = "Sentiment",
///                 ["IncludeEntities"] = true
///             }
///         },
///         new WorkflowStep
///         {
///             Id = Guid.NewGuid(),
///             Name = "GenerateReport",
///             AgentId = "report-generator",
///             Dependencies = new[] { "AnalyzeContent" },
///             Input = new 
///             {
///                 OriginalDocument = "{{ExtractContent.Output}}",
///                 Analysis = "{{AnalyzeContent.Output}}",
///                 ReportFormat = "PDF"
///             }
///         }
///     },
///     ExecutionMode = WorkflowExecutionMode.Sequential,
///     TimeoutMinutes = 30,
///     RetryPolicy = new RetryPolicy
///     {
///         MaxRetries = 3,
///         RetryDelaySeconds = 5,
///         ExponentialBackoff = true
///     }
/// };
/// 
/// // Validate workflow before execution
/// var validationResult = await workflowEngine.ValidateWorkflowAsync(workflow);
/// if (!validationResult.IsValid)
/// {
///     throw new InvalidOperationException($"Invalid workflow: {string.Join(", ", validationResult.Errors)}");
/// }
/// 
/// // Execute workflow with monitoring
/// var executionTask = workflowEngine.ExecuteWorkflowAsync(workflow, cancellationToken);
/// 
/// // Monitor execution progress
/// var workflowId = workflow.Id;
/// while (!executionTask.IsCompleted)
/// {
///     var status = await workflowEngine.GetExecutionStatusAsync(workflowId);
///     if (status != null)
///     {
///         Console.WriteLine($"Workflow progress: {status.ProgressPercentage:F1}% - {status.State}");
///         Console.WriteLine($"Completed steps: {status.CompletedSteps.Count}");
///         Console.WriteLine($"Executing steps: {status.ExecutingSteps.Count}");
///         
///         if (status.FailedSteps.Any())
///         {
///             Console.WriteLine($"Failed steps: {status.FailedSteps.Count}");
///         }
///     }
///     
///     await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
/// }
/// 
/// // Get final results
/// var result = await executionTask;
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Workflow completed successfully in {result.ExecutionTime}");
///     Console.WriteLine($"Generated report: {result.Outputs["GenerateReport"]}");
/// }
/// else
/// {
///     Console.WriteLine($"Workflow failed: {result.ErrorMessage}");
///     foreach (var stepError in result.StepResults.Where(s =&gt; !s.Value.IsSuccess))
///     {
///         Console.WriteLine($"Step {stepError.Key} failed: {stepError.Value.ErrorMessage}");
///     }
/// }
/// 
/// // Advanced workflow with parallel execution
/// var parallelWorkflow = new WorkflowDefinition
/// {
///     Id = Guid.NewGuid(),
///     Name = "ParallelProcessingWorkflow",
///     Steps = new List&lt;WorkflowStep&gt;
///     {
///         // Initial data preparation (runs first)
///         new WorkflowStep
///         {
///             Id = Guid.NewGuid(),
///             Name = "PrepareData",
///             AgentId = "data-processor"
///         },
///         
///         // Parallel processing steps (both depend on PrepareData)
///         new WorkflowStep
///         {
///             Id = Guid.NewGuid(),
///             Name = "ProcessImagesParallel",
///             AgentId = "image-processor",
///             Dependencies = new[] { "PrepareData" },
///             Input = "{{PrepareData.Output.Images}}"
///         },
///         new WorkflowStep
///         {
///             Id = Guid.NewGuid(),
///             Name = "ProcessTextParallel", 
///             AgentId = "text-processor",
///             Dependencies = new[] { "PrepareData" },
///             Input = "{{PrepareData.Output.Text}}"
///         },
///         
///         // Final aggregation (depends on both parallel steps)
///         new WorkflowStep
///         {
///             Id = Guid.NewGuid(),
///             Name = "AggregateResults",
///             AgentId = "result-aggregator",
///             Dependencies = new[] { "ProcessImagesParallel", "ProcessTextParallel" },
///             Input = new 
///             {
///                 ImageResults = "{{ProcessImagesParallel.Output}}",
///                 TextResults = "{{ProcessTextParallel.Output}}"
///             }
///         }
///     },
///     ExecutionMode = WorkflowExecutionMode.Parallel
/// };
/// 
/// var parallelResult = await workflowEngine.ExecuteWorkflowAsync(parallelWorkflow);
/// </code>
/// </example>
/// <seealso cref="WorkflowDefinition"/>
/// <seealso cref="WorkflowResult"/>
/// <seealso cref="WorkflowExecutionStatus"/>
/// <seealso cref="IAgent"/>
public interface IWorkflowEngine
{
    /// <summary>
    /// Executes a complete workflow definition with dependency resolution, parallel processing,
    /// error handling, and comprehensive result tracking.
    /// </summary>
    /// <param name="workflow">
    /// The workflow definition containing steps, dependencies, configuration, and execution parameters.
    /// Must be a valid workflow that passes validation checks.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the entire workflow execution.
    /// Cancellation will attempt to gracefully stop all running steps.
    /// </param>
    /// <returns>
    /// A task that resolves to a WorkflowResult containing execution outcomes, step results,
    /// timing information, and any error details from the workflow execution.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workflow"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workflow is invalid, required agents are not registered,
    /// or the workflow engine is not properly initialized.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// **Execution Flow:**
    /// 1. Workflow validation and dependency analysis
    /// 2. Agent availability verification
    /// 3. Step execution according to dependencies and execution mode
    /// 4. Result aggregation and cleanup
    /// </para>
    /// <para>
    /// **Error Handling:**
    /// The engine implements comprehensive error handling with configurable retry policies,
    /// fallback strategies, and partial success scenarios. Failed steps can be retried
    /// based on configuration while successful steps preserve their results.
    /// </para>
    /// <para>
    /// **Performance Considerations:**
    /// - Parallel steps execute concurrently when dependencies allow
    /// - Resource pooling and connection management for optimal performance
    /// - Progress tracking and monitoring without performance impact
    /// - Efficient memory usage for large workflows and data payloads
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await workflowEngine.ExecuteWorkflowAsync(workflow, cancellationToken);
    /// 
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine($"Workflow completed in {result.ExecutionTime}");
    ///     foreach (var output in result.Outputs)
    ///     {
    ///         Console.WriteLine($"Step {output.Key}: {output.Value}");
    ///     }
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Workflow failed: {result.ErrorMessage}");
    ///     LogFailedSteps(result.StepResults);
    /// }
    /// </code>
    /// </example>
    Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an agent with the workflow engine, making it available for workflow execution.
    /// Agents must be registered before being referenced in workflow definitions.
    /// </summary>
    /// <param name="agent">The agent instance to register for workflow execution.</param>
    /// <param name="cancellationToken">Cancellation token for the registration operation.</param>
    /// <returns>A task that completes when the agent has been successfully registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agent"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an agent with the same ID is already registered.</exception>
    Task RegisterAgentAsync(IAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a previously registered agent by its unique identifier.
    /// Used for workflow validation and execution planning.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The registered agent instance, or null if no agent with the specified ID is found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="agentId"/> is null or empty.</exception>
    Task<IAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all agents currently registered with the workflow engine.
    /// Useful for administrative operations and workflow planning.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of all registered agent instances.</returns>
    /// <remarks>
    /// This method returns a snapshot of registered agents at the time of the call.
    /// The collection may become stale if agents are registered or unregistered after the call.
    /// </remarks>
    Task<IEnumerable<IAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow definition to ensure it can be executed successfully.
    /// Performs comprehensive checks including dependency validation, agent availability,
    /// and configuration validation.
    /// </summary>
    /// <param name="workflow">The workflow definition to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the validation operation.</param>
    /// <returns>
    /// A validation result indicating whether the workflow is valid and containing
    /// any errors or warnings discovered during validation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workflow"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// **Validation Checks:**
    /// - Step dependency cycles and ordering
    /// - Agent availability and registration status
    /// - Input/output data flow compatibility
    /// - Configuration parameter validation
    /// - Resource and timeout constraints
    /// </para>
    /// <para>
    /// It's recommended to validate workflows before execution to catch configuration
    /// errors early and ensure successful execution.
    /// </para>
    /// </remarks>
    Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current execution status of a running or completed workflow.
    /// Provides real-time progress information, step status, and execution metrics.
    /// </summary>
    /// <param name="workflowId">The unique identifier of the workflow to query.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// The current execution status of the workflow, or null if no workflow
    /// with the specified ID is found or has been cleaned up.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workflowId"/> is empty.</exception>
    /// <remarks>
    /// <para>
    /// **Status Information Includes:**
    /// - Overall workflow state (Pending, Running, Completed, Failed, Cancelled)
    /// - Progress percentage and step completion status
    /// - Currently executing and completed steps
    /// - Execution timing and performance metrics
    /// </para>
    /// <para>
    /// Status information is updated in real-time during workflow execution and
    /// may be cached for a period after workflow completion for monitoring purposes.
    /// </para>
    /// </remarks>
    Task<WorkflowExecutionStatus?> GetExecutionStatusAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates cancellation of a running workflow, attempting to gracefully stop
    /// all executing steps and clean up resources.
    /// </summary>
    /// <param name="workflowId">The unique identifier of the workflow to cancel.</param>
    /// <param name="cancellationToken">Cancellation token for the cancellation operation itself.</param>
    /// <returns>
    /// True if the workflow cancellation was initiated successfully, false if the workflow
    /// was not found, already completed, or could not be cancelled.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workflowId"/> is empty.</exception>
    /// <remarks>
    /// <para>
    /// **Cancellation Behavior:**
    /// - Currently executing steps receive cancellation tokens and should stop gracefully
    /// - Pending steps are marked as cancelled and will not execute
    /// - Completed steps retain their results
    /// - Resources are cleaned up and the workflow transitions to Cancelled state
    /// </para>
    /// <para>
    /// **Timing Considerations:**
    /// Cancellation is not immediate - it depends on how individual agents handle
    /// cancellation tokens. Some operations may need time to complete gracefully.
    /// </para>
    /// </remarks>
    Task<bool> CancelWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of workflow validation, containing validation status,
/// error messages, warnings, and detailed feedback for workflow definition correction.
/// </summary>
/// <remarks>
/// <para>
/// WorkflowValidationResult provides comprehensive feedback about workflow definition
/// quality, identifying errors that would prevent execution and warnings about potential
/// issues or suboptimal configurations that should be addressed.
/// </para>
/// <para>
/// **Validation Categories:**
/// - **Structural**: Step dependencies, cycles, orphaned steps
/// - **Agent**: Agent availability, capability matching
/// - **Configuration**: Parameter validation, type compatibility
/// - **Performance**: Resource constraints, timeout recommendations
/// - **Security**: Access control, data flow validation
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await engine.ValidateWorkflowAsync(workflow);
/// 
/// if (!result.IsValid)
/// {
///     Console.WriteLine("Workflow validation failed:");
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"  ERROR: {error}");
///     }
/// }
/// 
/// if (result.Warnings.Any())
/// {
///     Console.WriteLine("Workflow warnings:");
///     foreach (var warning in result.Warnings)
///     {
///         Console.WriteLine($"  WARNING: {warning}");
///     }
/// }
/// </code>
/// </example>
public class WorkflowValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the workflow definition is valid and can be executed.
    /// A workflow is considered valid when it has no errors that would prevent execution.
    /// </summary>
    /// <value>
    /// True if the workflow can be executed without errors; false if there are blocking validation errors.
    /// </value>
    /// <remarks>
    /// A workflow with warnings but no errors is still considered valid and can be executed,
    /// though the warnings should be reviewed for optimal performance and maintainability.
    /// </remarks>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the collection of validation error messages that prevent workflow execution.
    /// These are critical issues that must be resolved before the workflow can run.
    /// </summary>
    /// <value>
    /// A list of descriptive error messages explaining what prevents the workflow from executing.
    /// </value>
    /// <remarks>
    /// <para>
    /// **Common Error Types:**
    /// - Missing or unregistered agents
    /// - Circular dependency loops
    /// - Invalid step configurations
    /// - Type mismatches in data flow
    /// - Missing required parameters
    /// </para>
    /// </remarks>
    /// <example>
    /// Typical error messages might include:
    /// - "Agent 'data-processor' referenced in step 'ProcessData' is not registered"
    /// - "Circular dependency detected: Step1 → Step2 → Step3 → Step1"
    /// - "Step 'AnalyzeData' requires input parameter 'DataSource' which is not provided"
    /// </example>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of validation warning messages that indicate potential issues
    /// or suboptimal configurations that should be reviewed but don't prevent execution.
    /// </summary>
    /// <value>
    /// A list of descriptive warning messages about potential workflow issues or improvements.
    /// </value>
    /// <remarks>
    /// <para>
    /// **Common Warning Types:**
    /// - Performance optimization suggestions
    /// - Best practice recommendations
    /// - Potential reliability concerns
    /// - Configuration improvements
    /// - Security considerations
    /// </para>
    /// </remarks>
    /// <example>
    /// Typical warning messages might include:
    /// - "Step 'ProcessLargeFile' has no timeout configured, consider adding one"
    /// - "Workflow has no retry policy configured for failed steps"
    /// - "Step dependencies could be optimized for better parallel execution"
    /// </example>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Represents the real-time execution status of a workflow, providing comprehensive information
/// about workflow progress, step states, timing metrics, and execution context for monitoring
/// and management purposes.
/// </summary>
/// <remarks>
/// <para>
/// WorkflowExecutionStatus provides a complete snapshot of workflow execution at a point in time,
/// enabling real-time monitoring, progress tracking, and operational management of running workflows.
/// This information is essential for dashboards, alerting systems, and operational tooling.
/// </para>
/// <para>
/// **Status Update Frequency:**
/// Status information is updated in real-time as workflow execution progresses, with updates
/// triggered by step state changes, progress milestones, and execution events.
/// </para>
/// <para>
/// **Monitoring Integration:**
/// This status information integrates with monitoring and alerting systems to provide
/// operational visibility into workflow performance, failures, and resource utilization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Monitor workflow progress
/// var workflowId = workflow.Id;
/// WorkflowExecutionStatus? status;
/// 
/// do
/// {
///     status = await engine.GetExecutionStatusAsync(workflowId);
///     if (status != null)
///     {
///         Console.WriteLine($"Workflow {status.WorkflowId}:");
///         Console.WriteLine($"  State: {status.State}");
///         Console.WriteLine($"  Progress: {status.ProgressPercentage:F1}%");
///         Console.WriteLine($"  Executing: {status.ExecutingSteps.Count} steps");
///         Console.WriteLine($"  Completed: {status.CompletedSteps.Count} steps");
///         
///         if (status.FailedSteps.Any())
///         {
///             Console.WriteLine($"  Failed: {status.FailedSteps.Count} steps");
///         }
///         
///         var runtime = status.LastUpdated - status.StartedAt;
///         Console.WriteLine($"  Runtime: {runtime.TotalMinutes:F1} minutes");
///     }
///     
///     await Task.Delay(TimeSpan.FromSeconds(5));
/// }
/// while (status?.State == WorkflowState.Running || status?.State == WorkflowState.Pending);
/// </code>
/// </example>
public class WorkflowExecutionStatus
{
    /// <summary>
    /// Gets or sets the unique identifier of the workflow being monitored.
    /// This corresponds to the WorkflowDefinition.Id of the executing workflow.
    /// </summary>
    /// <value>The unique workflow identifier used for tracking and correlation.</value>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the current execution state of the workflow, indicating the overall
    /// progress and outcome of workflow execution.
    /// </summary>
    /// <value>
    /// The current workflow state from the WorkflowState enumeration, representing
    /// the overall execution status and lifecycle stage.
    /// </value>
    /// <remarks>
    /// State transitions follow a defined lifecycle: Pending → Running → (Completed | Failed | Cancelled | TimedOut).
    /// The state provides the primary indication of workflow health and progress.
    /// </remarks>
    public WorkflowState State { get; set; }

    /// <summary>
    /// Gets or sets the collection of step identifiers for steps that are currently executing.
    /// These steps have been started but have not yet completed or failed.
    /// </summary>
    /// <value>
    /// A list of step GUIDs representing active workflow steps. Empty when no steps are executing.
    /// </value>
    /// <remarks>
    /// In parallel execution modes, multiple steps may execute simultaneously.
    /// In sequential modes, typically only one step executes at a time.
    /// </remarks>
    public List<Guid> ExecutingSteps { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of step identifiers for steps that have completed successfully.
    /// These steps have finished execution and produced valid results.
    /// </summary>
    /// <value>
    /// A list of step GUIDs representing successfully completed workflow steps.
    /// </value>
    /// <remarks>
    /// Completed steps contribute to the overall progress percentage and their results
    /// are available for use by subsequent dependent steps.
    /// </remarks>
    public List<Guid> CompletedSteps { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of step identifiers for steps that have failed during execution.
    /// These steps encountered errors and were unable to complete successfully.
    /// </summary>
    /// <value>
    /// A list of step GUIDs representing failed workflow steps.
    /// </value>
    /// <remarks>
    /// Failed steps may trigger retry logic, error handling, or workflow termination
    /// depending on the workflow configuration and error handling policies.
    /// </remarks>
    public List<Guid> FailedSteps { get; set; } = new();

    /// <summary>
    /// Gets or sets the current execution progress as a percentage of total workflow completion.
    /// Based on the number of completed steps relative to total steps.
    /// </summary>
    /// <value>
    /// A double value between 0.0 and 100.0 representing the completion percentage.
    /// </value>
    /// <remarks>
    /// <para>
    /// Progress calculation considers step weights, complexity, and completion status.
    /// The calculation may be adjusted based on step execution time estimates and
    /// actual performance characteristics.
    /// </para>
    /// <para>
    /// Progress may not advance linearly, especially in parallel execution scenarios
    /// where multiple steps complete simultaneously.
    /// </para>
    /// </remarks>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when workflow execution began.
    /// Used for calculating total execution time and performance metrics.
    /// </summary>
    /// <value>
    /// A DateTime value in UTC representing when the workflow started executing.
    /// </value>
    /// <remarks>
    /// This timestamp is set when the workflow transitions from Pending to Running state
    /// and remains constant throughout the workflow lifecycle.
    /// </remarks>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the most recent status update.
    /// Indicates the freshness of the status information.
    /// </summary>
    /// <value>
    /// A DateTime value in UTC representing when this status information was last updated.
    /// </value>
    /// <remarks>
    /// This timestamp is updated whenever workflow state changes, steps complete,
    /// or progress is recalculated. It helps determine status information staleness.
    /// </remarks>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Defines the possible execution states of a workflow throughout its lifecycle,
/// representing the current status and outcome of workflow processing.
/// </summary>
/// <remarks>
/// <para>
/// WorkflowState provides a standardized way to track workflow progress and outcomes,
/// enabling consistent status reporting, monitoring, and operational management across
/// different workflow implementations and integration scenarios.
/// </para>
/// <para>
/// **State Transitions:**
/// The workflow state follows a defined lifecycle with specific allowed transitions:
/// - Pending → Running (when execution begins)
/// - Running → Completed (when all steps succeed)
/// - Running → Failed (when critical errors occur)
/// - Running → Cancelled (when explicitly cancelled)
/// - Running → TimedOut (when execution time limits are exceeded)
/// </para>
/// <para>
/// **Terminal States:**
/// Completed, Failed, Cancelled, and TimedOut are terminal states. Once a workflow
/// reaches a terminal state, it cannot transition to another state without being
/// restarted as a new workflow instance.
/// </para>
/// </remarks>
public enum WorkflowState
{
    /// <summary>
    /// The workflow has been queued for execution but has not yet started processing.
    /// This is the initial state when a workflow is submitted to the engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Characteristics:**
    /// - No steps have begun execution
    /// - Resources are being allocated and validated
    /// - Dependencies and prerequisites are being checked
    /// - Agent availability is being verified
    /// </para>
    /// <para>
    /// **Typical Duration:**
    /// Usually brief, lasting only until the workflow engine initiates execution.
    /// Extended time in Pending state may indicate resource constraints or system load.
    /// </para>
    /// </remarks>
    Pending,

    /// <summary>
    /// The workflow is actively executing, with one or more steps currently processing.
    /// This indicates normal workflow progression through its defined steps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Characteristics:**
    /// - At least one step is actively executing
    /// - Progress percentage is advancing
    /// - Resources are actively being used
    /// - Intermediate results are being produced
    /// </para>
    /// <para>
    /// **Monitoring:**
    /// This is the primary state for progress monitoring, performance tracking,
    /// and operational oversight of workflow execution.
    /// </para>
    /// </remarks>
    Running,

    /// <summary>
    /// The workflow has finished successfully with all steps completing without errors.
    /// Final results are available and resources have been cleaned up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Characteristics:**
    /// - All required steps completed successfully
    /// - Final outputs and results are available
    /// - Execution time and performance metrics are recorded
    /// - System resources have been released
    /// </para>
    /// <para>
    /// **Result Access:**
    /// Workflow results, step outputs, and execution metadata remain accessible
    /// for a configured retention period after completion.
    /// </para>
    /// </remarks>
    Completed,

    /// <summary>
    /// The workflow has terminated due to one or more unrecoverable errors.
    /// Error details and partial results may be available for analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Failure Scenarios:**
    /// - Step execution errors that exceed retry limits
    /// - Agent unavailability or communication failures
    /// - Data validation or processing errors
    /// - Resource exhaustion or system errors
    /// - Configuration or dependency issues
    /// </para>
    /// <para>
    /// **Recovery and Analysis:**
    /// Failed workflows retain error information and partial results for
    /// troubleshooting, debugging, and potential manual recovery or restart.
    /// </para>
    /// </remarks>
    Failed,

    /// <summary>
    /// The workflow was explicitly cancelled by a user or system request.
    /// Execution was stopped gracefully with cleanup performed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Cancellation Process:**
    /// - Cancellation request was received and processed
    /// - Running steps were signaled to stop gracefully
    /// - Partial results may be preserved
    /// - Resources were properly cleaned up
    /// </para>
    /// <para>
    /// **Partial Results:**
    /// Steps that completed before cancellation may have their results preserved
    /// and accessible, depending on implementation and configuration.
    /// </para>
    /// </remarks>
    Cancelled,

    /// <summary>
    /// The workflow exceeded its configured execution time limit and was automatically terminated.
    /// This protects against runaway processes and resource exhaustion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Timeout Scenarios:**
    /// - Overall workflow timeout exceeded
    /// - Individual step timeouts causing workflow failure
    /// - Resource allocation timeouts
    /// - External service response timeouts
    /// </para>
    /// <para>
    /// **Timeout Management:**
    /// Timeouts are configurable at both workflow and step levels, with
    /// appropriate defaults to prevent system resource exhaustion while
    /// allowing reasonable processing time for complex operations.
    /// </para>
    /// </remarks>
    TimedOut
}