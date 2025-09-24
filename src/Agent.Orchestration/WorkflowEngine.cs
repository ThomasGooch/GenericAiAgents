using Agent.Core;
using Agent.Core.Models;
using Agent.Orchestration.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Agent.Orchestration;

/// <summary>
/// Workflow execution engine for orchestrating multi-agent processes
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();
    private readonly ConcurrentDictionary<Guid, WorkflowExecutionStatus> _executionStatus = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _workflowCancellations = new();

    public async Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var workflowResult = new WorkflowResult
        {
            StartedAt = startTime
        };

        // Track workflow execution
        var status = new WorkflowExecutionStatus
        {
            WorkflowId = workflow.Id,
            State = WorkflowState.Running,
            StartedAt = startTime,
            LastUpdated = startTime
        };
        _executionStatus[workflow.Id] = status;

        // Create cancellation token for this workflow
        var workflowCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _workflowCancellations[workflow.Id] = workflowCts;

        try
        {
            // Validate workflow first
            var validationResult = await ValidateWorkflowAsync(workflow, cancellationToken);
            if (!validationResult.IsValid)
            {
                workflowResult.Success = false;
                workflowResult.Error = string.Join("; ", validationResult.Errors);
                status.State = WorkflowState.Failed;
                return workflowResult;
            }

            // Execute workflow based on execution mode
            switch (workflow.ExecutionMode)
            {
                case WorkflowExecutionMode.Sequential:
                    await ExecuteSequentialWorkflowAsync(workflow, workflowResult, workflowCts.Token);
                    break;

                case WorkflowExecutionMode.Parallel:
                    await ExecuteParallelWorkflowAsync(workflow, workflowResult, workflowCts.Token);
                    break;

                case WorkflowExecutionMode.Dependency:
                    await ExecuteDependencyWorkflowAsync(workflow, workflowResult, workflowCts.Token);
                    break;

                default:
                    workflowResult.Success = false;
                    workflowResult.Error = $"Unsupported execution mode: {workflow.ExecutionMode}";
                    status.State = WorkflowState.Failed;
                    return workflowResult;
            }

            // Determine workflow success: all steps must succeed OR failed steps have ContinueOnFailure = true
            var failedSteps = workflowResult.StepResults.Where(r => !r.Success).ToList();
            var criticalFailures = failedSteps.Where(fs =>
            {
                var step = workflow.Steps.FirstOrDefault(s => s.Id == fs.StepId);
                return step == null || !step.ContinueOnFailure;
            }).ToList();

            workflowResult.Success = !criticalFailures.Any();
            status.State = workflowResult.Success ? WorkflowState.Completed : WorkflowState.Failed;

            if (!workflowResult.Success)
            {
                workflowResult.Error = string.Join("; ", criticalFailures.Select(s => $"Step '{s.StepName}': {s.Error}"));
            }
        }
        catch (OperationCanceledException)
        {
            workflowResult.Success = false;
            workflowResult.Error = "Workflow execution was cancelled";
            status.State = WorkflowState.Cancelled;
        }
        catch (Exception ex)
        {
            workflowResult.Success = false;
            workflowResult.Error = $"Workflow execution failed: {ex.Message}";
            status.State = WorkflowState.Failed;
        }
        finally
        {
            stopwatch.Stop();
            workflowResult.ExecutionTime = stopwatch.Elapsed;
            workflowResult.CompletedAt = DateTime.UtcNow;
            status.LastUpdated = DateTime.UtcNow;

            // Cleanup
            _workflowCancellations.TryRemove(workflow.Id, out _);
            workflowCts.Dispose();
        }

        return workflowResult;
    }

    private async Task ExecuteSequentialWorkflowAsync(WorkflowDefinition workflow, WorkflowResult workflowResult, CancellationToken cancellationToken)
    {
        var orderedSteps = workflow.Steps.OrderBy(s => s.Order).ToList();

        foreach (var step in orderedSteps)
        {
            var stepResult = await ExecuteStepAsync(step, cancellationToken);
            workflowResult.StepResults.Add(stepResult);

            // Update execution status
            if (_executionStatus.TryGetValue(workflow.Id, out var status))
            {
                if (stepResult.Success)
                {
                    status.CompletedSteps.Add(step.Id);
                }
                else
                {
                    status.FailedSteps.Add(step.Id);
                }
                status.ProgressPercentage = (double)status.CompletedSteps.Count / workflow.Steps.Count * 100;
                status.LastUpdated = DateTime.UtcNow;
            }

            // Stop on failure unless configured to continue
            if (!stepResult.Success && !step.ContinueOnFailure)
            {
                break;
            }
        }
    }

    private async Task ExecuteParallelWorkflowAsync(WorkflowDefinition workflow, WorkflowResult workflowResult, CancellationToken cancellationToken)
    {
        var tasks = workflow.Steps.Select(step => ExecuteStepAsync(step, cancellationToken)).ToArray();
        var stepResults = await Task.WhenAll(tasks);

        workflowResult.StepResults.AddRange(stepResults);

        // Update execution status
        if (_executionStatus.TryGetValue(workflow.Id, out var status))
        {
            foreach (var result in stepResults)
            {
                if (result.Success)
                {
                    status.CompletedSteps.Add(result.StepId);
                }
                else
                {
                    status.FailedSteps.Add(result.StepId);
                }
            }
            status.ProgressPercentage = 100.0;
            status.LastUpdated = DateTime.UtcNow;
        }
    }

    private async Task ExecuteDependencyWorkflowAsync(WorkflowDefinition workflow, WorkflowResult workflowResult, CancellationToken cancellationToken)
    {
        var completedSteps = new HashSet<Guid>();
        var remainingSteps = workflow.Steps.ToList();

        while (remainingSteps.Any() && !cancellationToken.IsCancellationRequested)
        {
            var readySteps = remainingSteps
                .Where(step => step.Dependencies.All(dep => completedSteps.Contains(dep)))
                .ToList();

            if (!readySteps.Any())
            {
                throw new InvalidOperationException("Circular dependency detected in workflow steps");
            }

            var tasks = readySteps.Select(step => ExecuteStepAsync(step, cancellationToken)).ToArray();
            var stepResults = await Task.WhenAll(tasks);

            workflowResult.StepResults.AddRange(stepResults);

            foreach (var result in stepResults)
            {
                if (result.Success)
                {
                    completedSteps.Add(result.StepId);
                }

                var step = readySteps.First(s => s.Id == result.StepId);
                remainingSteps.Remove(step);

                // Stop on failure unless configured to continue
                if (!result.Success && !step.ContinueOnFailure)
                {
                    return;
                }
            }

            // Update execution status
            if (_executionStatus.TryGetValue(workflow.Id, out var status))
            {
                status.CompletedSteps = completedSteps.ToList();
                status.ProgressPercentage = (double)completedSteps.Count / workflow.Steps.Count * 100;
                status.LastUpdated = DateTime.UtcNow;
            }
        }
    }

    private async Task<WorkflowStepResult> ExecuteStepAsync(WorkflowStep step, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var result = new WorkflowStepResult
        {
            StepId = step.Id,
            StepName = step.Name,
            AgentId = step.AgentId,
            StartedAt = startTime
        };

        try
        {
            if (!_agents.TryGetValue(step.AgentId, out var agent))
            {
                result.Success = false;
                result.Error = $"Agent '{step.AgentId}' not found";
                return result;
            }

            // Create timeout for step if specified
            using var stepCts = step.Timeout.HasValue
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : null;

            if (stepCts != null && step.Timeout.HasValue)
            {
                stepCts.CancelAfter(step.Timeout.Value);
            }

            var effectiveToken = stepCts?.Token ?? cancellationToken;

            var agentRequest = new AgentRequest
            {
                Input = step.Input,
                Id = Guid.NewGuid(),
                Metadata = step.Configuration,
                CancellationToken = effectiveToken
            };

            var agentResult = await agent.ProcessAsync(agentRequest, effectiveToken);

            result.Success = agentResult.Success;
            result.Output = agentResult.Output?.ToString();
            result.Error = agentResult.Error;
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "Step execution was cancelled or timed out";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"Step execution failed: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    public Task RegisterAgentAsync(IAgent agent, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        _agents[agent.Id] = agent;
        return Task.CompletedTask;
    }

    public Task<IAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<IEnumerable<IAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IAgent>>(_agents.Values.ToList());
    }

    public Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var result = new WorkflowValidationResult { IsValid = true };

        if (workflow == null)
        {
            result.IsValid = false;
            result.Errors.Add("Workflow definition is null");
            return Task.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(workflow.Name))
        {
            result.IsValid = false;
            result.Errors.Add("Workflow name is required");
        }

        if (!workflow.Steps.Any())
        {
            result.IsValid = false;
            result.Errors.Add("Workflow must contain at least one step");
        }

        foreach (var step in workflow.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.AgentId))
            {
                result.IsValid = false;
                result.Errors.Add($"Step '{step.Name}' is missing agent ID");
            }
            else if (!_agents.ContainsKey(step.AgentId))
            {
                result.IsValid = false;
                result.Errors.Add($"Agent '{step.AgentId}' for step '{step.Name}' is not registered");
            }

            // Validate dependencies
            foreach (var dependency in step.Dependencies)
            {
                if (!workflow.Steps.Any(s => s.Id == dependency))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Step '{step.Name}' has invalid dependency: {dependency}");
                }
            }
        }

        return Task.FromResult(result);
    }

    public Task<WorkflowExecutionStatus?> GetExecutionStatusAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        _executionStatus.TryGetValue(workflowId, out var status);
        return Task.FromResult(status);
    }

    public Task<bool> CancelWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        if (_workflowCancellations.TryGetValue(workflowId, out var cts))
        {
            cts.Cancel();

            if (_executionStatus.TryGetValue(workflowId, out var status))
            {
                status.State = WorkflowState.Cancelled;
                status.LastUpdated = DateTime.UtcNow;
            }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}