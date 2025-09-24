using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Agent.Core;
using Agent.Core.Models;
using Agent.Orchestration;
using Agent.Orchestration.Models;

namespace Agent.Performance.Tests;

/// <summary>
/// Performance benchmarks for workflow execution
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class WorkflowPerformanceBenchmarks
{
    private IWorkflowEngine _workflowEngine = null!;
    private WorkflowDefinition _sequentialWorkflow = null!;
    private WorkflowDefinition _parallelWorkflow = null!;
    private TestAgent[] _testAgents = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _workflowEngine = new WorkflowEngine();

        // Create test agents
        _testAgents = new TestAgent[10];
        for (int i = 0; i < 10; i++)
        {
            _testAgents[i] = new TestAgent($"perf-agent-{i}", $"Performance Test Agent {i}");
            var config = new AgentConfiguration { Name = $"Config {i}", Timeout = TimeSpan.FromMinutes(1) };
            await _testAgents[i].InitializeAsync(config);
            await _workflowEngine.RegisterAgentAsync(_testAgents[i]);
        }

        // Setup workflows
        _sequentialWorkflow = CreateSequentialWorkflow(5);
        _parallelWorkflow = CreateParallelWorkflow(5);
    }

    [Benchmark]
    public async Task<WorkflowResult> ExecuteSequentialWorkflow_5Steps()
    {
        return await _workflowEngine.ExecuteWorkflowAsync(_sequentialWorkflow);
    }

    [Benchmark]
    public async Task<WorkflowResult> ExecuteParallelWorkflow_5Steps()
    {
        return await _workflowEngine.ExecuteWorkflowAsync(_parallelWorkflow);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task<WorkflowResult> ExecuteLargeParallelWorkflow(int stepCount)
    {
        var largeWorkflow = CreateParallelWorkflow(stepCount);
        return await _workflowEngine.ExecuteWorkflowAsync(largeWorkflow);
    }

    [Benchmark]
    public async Task<List<WorkflowResult>> ExecuteConcurrentWorkflows()
    {
        var workflows = Enumerable.Range(0, 10)
            .Select(_ => CreateSequentialWorkflow(3))
            .ToList();

        var tasks = workflows.Select(w => _workflowEngine.ExecuteWorkflowAsync(w));
        var results = await Task.WhenAll(tasks);

        return results.ToList();
    }

    private WorkflowDefinition CreateSequentialWorkflow(int stepCount)
    {
        return new WorkflowDefinition
        {
            Name = $"Sequential Workflow {stepCount} Steps",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = Enumerable.Range(0, stepCount)
                .Select(i => new WorkflowStep
                {
                    AgentId = $"perf-agent-{i % _testAgents.Length}",
                    Input = $"Task {i}",
                    Order = i + 1
                })
                .ToList()
        };
    }

    private WorkflowDefinition CreateParallelWorkflow(int stepCount)
    {
        return new WorkflowDefinition
        {
            Name = $"Parallel Workflow {stepCount} Steps",
            ExecutionMode = WorkflowExecutionMode.Parallel,
            Steps = Enumerable.Range(0, stepCount)
                .Select(i => new WorkflowStep
                {
                    AgentId = $"perf-agent-{i % _testAgents.Length}",
                    Input = $"Parallel Task {i}",
                    Order = 1 // All parallel
                })
                .ToList()
        };
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        foreach (var agent in _testAgents)
        {
            await agent.DisposeAsync();
        }
    }
}

/// <summary>
/// XUnit tests for performance validation
/// </summary>
public class WorkflowPerformanceTests
{
    [Fact]
    public void RunPerformanceBenchmarks()
    {
        // This test runs the benchmarks and validates basic performance characteristics
        var summary = BenchmarkRunner.Run<WorkflowPerformanceBenchmarks>();

        Assert.NotNull(summary);
        Assert.True(summary.Reports.Any(), "Benchmark reports should be generated");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    public async Task HighVolumeWorkflowExecution_ShouldMeetPerformanceTargets(int workflowCount)
    {
        // Arrange
        var workflowEngine = new WorkflowEngine();
        var agent = new TestAgent("perf-test-agent", "Performance Test Agent");

        var config = new AgentConfiguration { Timeout = TimeSpan.FromSeconds(30) };
        await agent.InitializeAsync(config);
        await workflowEngine.RegisterAgentAsync(agent);

        var workflows = Enumerable.Range(0, workflowCount)
            .Select(i => new WorkflowDefinition
            {
                Name = $"Performance Test Workflow {i}",
                Steps = new List<WorkflowStep>
                {
                    new() { AgentId = "perf-test-agent", Input = $"Task {i}" }
                }
            })
            .ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = workflows.Select(w => workflowEngine.ExecuteWorkflowAsync(w));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.All(results, result => Assert.True(result.Success));
        Assert.Equal(workflowCount, results.Length);

        // Performance targets
        var averageExecutionTime = stopwatch.ElapsedMilliseconds / (double)workflowCount;
        Assert.True(averageExecutionTime < 1000, $"Average execution time {averageExecutionTime}ms exceeds 1000ms target");

        await agent.DisposeAsync();
    }

    [Fact]
    public async Task ParallelWorkflow_ShouldBeSignificantlyFasterThanSequential()
    {
        // Arrange
        var workflowEngine = new WorkflowEngine();
        var agents = new List<TestAgent>();

        for (int i = 0; i < 5; i++)
        {
            var agent = new TestAgent($"parallel-test-{i}", $"Agent {i}");
            await agent.InitializeAsync(new AgentConfiguration());
            await workflowEngine.RegisterAgentAsync(agent);
            agents.Add(agent);
        }

        var sequentialWorkflow = new WorkflowDefinition
        {
            Name = "Sequential Performance Test Workflow",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = agents.Select((agent, i) => new WorkflowStep
            {
                AgentId = agent.Id,
                Input = $"Sequential Task {i}",
                Order = i + 1
            }).ToList()
        };

        var parallelWorkflow = new WorkflowDefinition
        {
            Name = "Parallel Performance Test Workflow",
            ExecutionMode = WorkflowExecutionMode.Parallel,
            Steps = agents.Select((agent, i) => new WorkflowStep
            {
                AgentId = agent.Id,
                Input = $"Parallel Task {i}",
                Order = 1
            }).ToList()
        };

        // Act
        var sequentialResult = await workflowEngine.ExecuteWorkflowAsync(sequentialWorkflow);
        var parallelResult = await workflowEngine.ExecuteWorkflowAsync(parallelWorkflow);

        // Assert
        Assert.True(sequentialResult.Success);
        Assert.True(parallelResult.Success);

        // Parallel should be significantly faster (at least 50% faster)
        var speedupRatio = sequentialResult.ExecutionTime.TotalMilliseconds / parallelResult.ExecutionTime.TotalMilliseconds;
        Assert.True(speedupRatio > 1.5, $"Parallel execution speedup ratio {speedupRatio:F2} should be > 1.5");

        // Cleanup
        foreach (var agent in agents)
        {
            await agent.DisposeAsync();
        }
    }

    [Fact]
    public async Task MemoryUsage_ShouldRemainStableDuringLongRunningOperations()
    {
        // Arrange
        var workflowEngine = new WorkflowEngine();
        var agent = new TestAgent("memory-test-agent", "Memory Test Agent");
        await agent.InitializeAsync(new AgentConfiguration());
        await workflowEngine.RegisterAgentAsync(agent);

        var initialMemory = GC.GetTotalMemory(true);

        // Act - Execute many workflows to test memory stability
        for (int batch = 0; batch < 10; batch++)
        {
            var workflows = Enumerable.Range(0, 50)
                .Select(i => new WorkflowDefinition
                {
                    Name = $"Memory Test Workflow Batch {batch} Item {i}",
                    Steps = new List<WorkflowStep>
                    {
                        new() { AgentId = "memory-test-agent", Input = $"Memory test {i}" }
                    }
                });

            var tasks = workflows.Select(w => workflowEngine.ExecuteWorkflowAsync(w));
            var results = await Task.WhenAll(tasks);

            Assert.All(results, r => Assert.True(r.Success));

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var finalMemory = GC.GetTotalMemory(true);

        // Assert - Memory growth should be reasonable (less than 10MB increase)
        var memoryGrowth = finalMemory - initialMemory;
        Assert.True(memoryGrowth < 10 * 1024 * 1024, $"Memory growth {memoryGrowth / (1024 * 1024)}MB exceeds 10MB limit");

        await agent.DisposeAsync();
    }
}

/// <summary>
/// Simple test agent for performance testing
/// </summary>
public class TestAgent : BaseAgent
{
    public TestAgent(string name, string description) : base(name, description)
    {
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        // Simulate lightweight processing
        await Task.Delay(10, cancellationToken);
        return AgentResult.CreateSuccess($"Processed: {request.Input}");
    }
}