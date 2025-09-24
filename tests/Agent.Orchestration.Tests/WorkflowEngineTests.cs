using Agent.Core;
using Agent.Core.Models;
using Agent.Orchestration.Models;
using Agent.Orchestration;
using NSubstitute;

namespace Agent.Orchestration.Tests;

public class WorkflowEngineTests
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IAgent _mockAgent1;
    private readonly IAgent _mockAgent2;

    public WorkflowEngineTests()
    {
        _workflowEngine = new WorkflowEngine();
        _mockAgent1 = Substitute.For<IAgent>();
        _mockAgent2 = Substitute.For<IAgent>();
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithSequentialWorkflow_ShouldExecuteInOrder()
    {
        // Arrange
        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Sequential Test Workflow",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = new List<WorkflowStep>
            {
                new() { Id = Guid.NewGuid(), AgentId = "agent1", Input = "Step 1 input", Order = 1 },
                new() { Id = Guid.NewGuid(), AgentId = "agent2", Input = "Step 2 input", Order = 2 }
            }
        };

        _mockAgent1.Id.Returns("agent1");
        _mockAgent2.Id.Returns("agent2");

        _mockAgent1.ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
                  .Returns(new AgentResult { Success = true, Output = "Agent 1 result" });
        
        _mockAgent2.ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
                  .Returns(new AgentResult { Success = true, Output = "Agent 2 result" });

        // Register agents with workflow engine
        await _workflowEngine.RegisterAgentAsync(_mockAgent1);
        await _workflowEngine.RegisterAgentAsync(_mockAgent2);

        // Act
        var result = await _workflowEngine.ExecuteWorkflowAsync(workflow, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.StepResults.Count);
        Assert.Equal("Agent 1 result", result.StepResults[0].Output);
        Assert.Equal("Agent 2 result", result.StepResults[1].Output);
        
        Received.InOrder(() =>
        {
            _mockAgent1.ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>());
            _mockAgent2.ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithParallelWorkflow_ShouldExecuteConcurrently()
    {
        // Arrange
        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Parallel Test Workflow",
            ExecutionMode = WorkflowExecutionMode.Parallel,
            Steps = new List<WorkflowStep>
            {
                new() { Id = Guid.NewGuid(), AgentId = "agent1", Input = "Step 1 input", Order = 1 },
                new() { Id = Guid.NewGuid(), AgentId = "agent2", Input = "Step 2 input", Order = 1 }
            }
        };

        _mockAgent1.Id.Returns("agent1");
        _mockAgent2.Id.Returns("agent2");

        var tcs1 = new TaskCompletionSource<AgentResult>();
        var tcs2 = new TaskCompletionSource<AgentResult>();

        _mockAgent1.ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
                  .Returns(tcs1.Task);
        
        _mockAgent2.ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
                  .Returns(tcs2.Task);

        // Register agents with workflow engine
        await _workflowEngine.RegisterAgentAsync(_mockAgent1);
        await _workflowEngine.RegisterAgentAsync(_mockAgent2);

        // Act
        var workflowTask = _workflowEngine.ExecuteWorkflowAsync(workflow, CancellationToken.None);
        
        // Complete both agents simultaneously
        tcs1.SetResult(new AgentResult { Success = true, Output = "Agent 1 result" });
        tcs2.SetResult(new AgentResult { Success = true, Output = "Agent 2 result" });
        
        var result = await workflowTask;

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.StepResults.Count);
        
        // Verify both agents were called concurrently
        await _mockAgent1.Received(1).ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>());
        await _mockAgent2.Received(1).ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithFailedStep_ShouldStopExecution()
    {
        // Arrange
        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Failing Workflow",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = new List<WorkflowStep>
            {
                new() { Id = Guid.NewGuid(), AgentId = "agent1", Input = "Step 1 input", Order = 1 },
                new() { Id = Guid.NewGuid(), AgentId = "agent2", Input = "Step 2 input", Order = 2 }
            }
        };

        _mockAgent1.Id.Returns("agent1");
        _mockAgent2.Id.Returns("agent2");

        _mockAgent1.ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
                  .Returns(new AgentResult { Success = false, Error = "Agent 1 failed" });

        // Register agents with workflow engine
        await _workflowEngine.RegisterAgentAsync(_mockAgent1);
        await _workflowEngine.RegisterAgentAsync(_mockAgent2);

        // Act
        var result = await _workflowEngine.ExecuteWorkflowAsync(workflow, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.StepResults);
        Assert.Contains("Agent 1 failed", result.Error);
        
        // Agent 2 should not have been called
        await _mockAgent2.DidNotReceive().ProcessAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAgentAsync_ShouldAddAgentToRegistry()
    {
        // Arrange
        var agent = _mockAgent1;
        agent.Id.Returns("test-agent");

        // Act
        await _workflowEngine.RegisterAgentAsync(agent);

        // Assert
        var registeredAgent = await _workflowEngine.GetAgentAsync("test-agent");
        Assert.NotNull(registeredAgent);
        Assert.Equal("test-agent", registeredAgent.Id);
    }

    [Fact]
    public async Task GetAgentAsync_WithNonExistentAgent_ShouldReturnNull()
    {
        // Act
        var agent = await _workflowEngine.GetAgentAsync("non-existent");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithMissingAgent_ShouldFail()
    {
        // Arrange
        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Missing Agent Workflow",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = new List<WorkflowStep>
            {
                new() { Id = Guid.NewGuid(), AgentId = "missing-agent", Input = "test", Order = 1 }
            }
        };

        // Act
        var result = await _workflowEngine.ExecuteWorkflowAsync(workflow, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("missing-agent", result.Error);
    }
}