using Agent.Core;
using Agent.Core.Models;
using Agent.Orchestration;
using Agent.Orchestration.Models;
using Agent.Registry;
using Agent.Tools.Samples;
using Agent.Configuration;

namespace Agent.Integration.Tests;

/// <summary>
/// End-to-end integration tests for complete workflow execution
/// </summary>
public class EndToEndWorkflowTests : IAsyncLifetime
{
    private IWorkflowEngine _workflowEngine = null!;
    private IToolRegistry _toolRegistry = null!;
    private IAgentRegistryEnhanced _agentRegistry = null!;
    private TestAgent _testAgent1 = null!;
    private TestAgent _testAgent2 = null!;

    public async Task InitializeAsync()
    {
        // Initialize components
        _toolRegistry = new ToolRegistry();
        _agentRegistry = new AgentRegistryEnhanced();
        _workflowEngine = new WorkflowEngine();

        // Register sample tools
        await _toolRegistry.RegisterToolAsync(new HttpClientTool());
        await _toolRegistry.RegisterToolAsync(new FileSystemTool());
        await _toolRegistry.RegisterToolAsync(new TextManipulationTool());

        // Create and register test agents
        _testAgent1 = new TestAgent("test-agent-1", "Test Agent One");
        _testAgent2 = new TestAgent("test-agent-2", "Test Agent Two");

        var config = new AgentConfiguration
        {
            Name = "Integration Test Config",
            Timeout = TimeSpan.FromMinutes(1)
        };

        await _testAgent1.InitializeAsync(config);
        await _testAgent2.InitializeAsync(config);

        await _workflowEngine.RegisterAgentAsync(_testAgent1);
        await _workflowEngine.RegisterAgentAsync(_testAgent2);
        await _agentRegistry.RegisterAgentAsync(_testAgent1);
        await _agentRegistry.RegisterAgentAsync(_testAgent2);
    }

    [Fact]
    public async Task ExecuteCompleteWorkflow_ShouldProcessAllStepsSuccessfully()
    {
        // Arrange
        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Complete Integration Test Workflow",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Data Processing Step",
                    AgentId = "test-agent-1",
                    Input = "Process this data: Hello World",
                    Order = 1
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Data Validation Step",
                    AgentId = "test-agent-2",
                    Input = "Validate processed data",
                    Order = 2
                }
            }
        };

        // Act
        var result = await _workflowEngine.ExecuteWorkflowAsync(workflow);

        // Assert
        Assert.True(result.Success, $"Workflow failed: {result.Error}");
        Assert.Equal(2, result.StepResults.Count);
        Assert.All(result.StepResults, stepResult => Assert.True(stepResult.Success));
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task ParallelWorkflowExecution_ShouldCompleteAllStepsConcurrently()
    {
        // Arrange
        var workflow = new WorkflowDefinition
        {
            Name = "Parallel Integration Test",
            ExecutionMode = WorkflowExecutionMode.Parallel,
            Steps = new List<WorkflowStep>
            {
                new() { AgentId = "test-agent-1", Input = "Task 1", Order = 1 },
                new() { AgentId = "test-agent-2", Input = "Task 2", Order = 1 },
                new() { AgentId = "test-agent-1", Input = "Task 3", Order = 1 }
            }
        };

        var startTime = DateTime.UtcNow;

        // Act
        var result = await _workflowEngine.ExecuteWorkflowAsync(workflow);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.StepResults.Count);
        
        // Parallel execution should be faster than sequential
        var totalSequentialTime = result.StepResults.Sum(s => s.ExecutionTime.TotalMilliseconds);
        Assert.True(result.ExecutionTime.TotalMilliseconds < totalSequentialTime * 0.8);
    }

    [Fact]
    public async Task AgentHealthMonitoring_ShouldTrackAgentStatus()
    {
        // Act
        var healthReport = await _agentRegistry.GetHealthReportAsync();

        // Assert
        Assert.NotNull(healthReport);
        Assert.Equal(2, healthReport.AgentHealth.Count);
        Assert.True(healthReport.AgentHealth.ContainsKey("test-agent-1"));
        Assert.True(healthReport.AgentHealth.ContainsKey("test-agent-2"));
        Assert.All(healthReport.AgentHealth.Values, health => Assert.True(health.IsHealthy));
    }

    [Fact]
    public async Task ToolRegistryIntegration_ShouldDiscoverAndExecuteTools()
    {
        // Act
        var tools = await _toolRegistry.GetAllToolsAsync();
        var httpTool = await _toolRegistry.GetToolAsync("http-client");

        // Assert
        Assert.True(tools.Count() >= 3); // At least the 3 tools we registered
        Assert.NotNull(httpTool);
        Assert.Equal("http-client", httpTool.Name);
    }

    [Fact]
    public async Task WorkflowValidation_ShouldCatchInvalidConfigurations()
    {
        // Arrange - Invalid workflow with missing agent
        var invalidWorkflow = new WorkflowDefinition
        {
            Name = "Invalid Workflow",
            Steps = new List<WorkflowStep>
            {
                new() { AgentId = "non-existent-agent", Input = "test" }
            }
        };

        // Act
        var validationResult = await _workflowEngine.ValidateWorkflowAsync(invalidWorkflow);

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, error => error.Contains("non-existent-agent"));
    }

    [Fact]
    public async Task SystemResilience_ShouldHandleAgentFailures()
    {
        // Arrange - Create a workflow where first step fails but has ContinueOnFailure
        var resilientWorkflow = new WorkflowDefinition
        {
            Name = "Resilience Test",
            ExecutionMode = WorkflowExecutionMode.Sequential,
            Steps = new List<WorkflowStep>
            {
                new()
                {
                    AgentId = "test-agent-1",
                    Input = "FAIL", // Special input to make test agent fail
                    ContinueOnFailure = true,
                    Order = 1
                },
                new()
                {
                    AgentId = "test-agent-2",
                    Input = "Success task",
                    Order = 2
                }
            }
        };

        // Act
        var result = await _workflowEngine.ExecuteWorkflowAsync(resilientWorkflow);

        // Assert
        Assert.True(result.Success); // Overall workflow should succeed
        Assert.Equal(2, result.StepResults.Count);
        Assert.False(result.StepResults[0].Success); // First step should fail
        Assert.True(result.StepResults[1].Success);  // Second step should succeed
    }

    [Fact]
    public async Task ConfigurationIntegration_ShouldLoadAndValidateSettings()
    {
        // Arrange
        var configProvider = new AgentConfigurationProvider();
        var testConfigPath = Path.GetTempFileName();
        
        var testConfig = """
        {
          "AgentSystem": {
            "Name": "Integration Test System",
            "Version": "1.0.0-test",
            "Environment": "Test"
          },
          "Agents": {
            "MaxConcurrentAgents": 5,
            "DefaultTimeout": "00:02:00"
          }
        }
        """;
        
        await File.WriteAllTextAsync(testConfigPath, testConfig);

        try
        {
            // Act
            var config = await configProvider.LoadConfigurationAsync(testConfigPath, false);
            var validationResult = await configProvider.ValidateConfigurationAsync(config);

            // Assert
            Assert.True(validationResult.IsValid, string.Join("; ", validationResult.Errors));
            Assert.Equal("Integration Test System", config.AgentSystem.Name);
            Assert.Equal(5, config.Agents.MaxConcurrentAgents);
            Assert.Equal(TimeSpan.FromMinutes(2), config.Agents.DefaultTimeout);
        }
        finally
        {
            if (File.Exists(testConfigPath))
                File.Delete(testConfigPath);
        }
    }

    public async Task DisposeAsync()
    {
        await _testAgent1.DisposeAsync();
        await _testAgent2.DisposeAsync();
    }
}

/// <summary>
/// Test agent implementation for integration testing
/// </summary>
public class TestAgent : BaseAgent
{
    public TestAgent(string name, string description) : base(name, description)
    {
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        // Simulate processing time
        await Task.Delay(50, cancellationToken);

        // Special case for failure testing
        if (request.Input?.Contains("FAIL") == true)
        {
            return AgentResult.CreateError("Simulated failure for testing");
        }

        // Return success with processed data
        return AgentResult.CreateSuccess($"Processed by {Name}: {request.Input}");
    }
}