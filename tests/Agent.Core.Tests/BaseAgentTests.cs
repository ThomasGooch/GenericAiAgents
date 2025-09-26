using Agent.Core;
using Agent.Core.Models;
using NSubstitute;

namespace Agent.Core.Tests;

public class BaseAgentTests
{
    private class TestAgent : BaseAgent
    {
        public TestAgent() : base("test-agent", "Test Agent for unit testing")
        {
        }

        protected override async Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            return AgentResult.CreateSuccess($"Processed: {request.Payload}");
        }
    }

    [Fact]
    public void BaseAgent_ShouldHaveRequiredProperties()
    {
        var agent = new TestAgent();

        Assert.NotEmpty(agent.Id);
        Assert.Equal("test-agent", agent.Name);
        Assert.Equal("Test Agent for unit testing", agent.Description);
        Assert.False(agent.IsInitialized);
        Assert.NotNull(agent.Configuration);
    }

    [Fact]
    public async Task BaseAgent_ShouldInitializeCorrectly()
    {
        var agent = new TestAgent();
        var config = new AgentConfiguration
        {
            Name = "test-config",
            Description = "Test configuration",
            Settings = new Dictionary<string, object> { { "key", "value" } }
        };

        await agent.InitializeAsync(config);

        Assert.True(agent.IsInitialized);
        Assert.Equal(config.Name, agent.Configuration.Name);
        Assert.Equal(config.Description, agent.Configuration.Description);
        Assert.Single(agent.Configuration.Settings);
    }

    [Fact]
    public async Task BaseAgent_ShouldThrowIfNotInitialized()
    {
        var agent = new TestAgent();
        var request = new AgentRequest { Payload = "test" };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => agent.ExecuteAsync(request));

        Assert.Contains("not been initialized", exception.Message);
    }

    [Fact]
    public async Task BaseAgent_ShouldExecuteSuccessfully()
    {
        var agent = new TestAgent();
        var config = new AgentConfiguration { Name = "test" };
        await agent.InitializeAsync(config);

        var request = new AgentRequest { Payload = "test input" };
        var result = await agent.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Processed: test input", result.Data);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task BaseAgent_ShouldHandleExecutionTimeout()
    {
        var config = new AgentConfiguration
        {
            Name = "test",
            Timeout = TimeSpan.FromMilliseconds(50)
        };

        var slowAgent = new SlowTestAgent();
        await slowAgent.InitializeAsync(config);

        var request = new AgentRequest { Payload = "test" };
        var result = await slowAgent.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BaseAgent_ShouldHandleExecutionException()
    {
        var agent = new ExceptionTestAgent();
        var config = new AgentConfiguration { Name = "test" };
        await agent.InitializeAsync(config);

        var request = new AgentRequest { Payload = "test" };
        var result = await agent.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Test exception", result.ErrorMessage);
    }

    [Fact]
    public async Task BaseAgent_ShouldDisposeCorrectly()
    {
        var agent = new TestAgent();
        var config = new AgentConfiguration { Name = "test" };
        await agent.InitializeAsync(config);

        await agent.DisposeAsync();

        // After disposal, agent should not be usable
        var request = new AgentRequest { Payload = "test" };
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => agent.ExecuteAsync(request));
    }

    private class SlowTestAgent : BaseAgent
    {
        public SlowTestAgent() : base("slow-agent", "Slow test agent")
        {
        }

        protected override async Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken);  // Longer than timeout
            return AgentResult.CreateSuccess("Done");
        }
    }

    private class ExceptionTestAgent : BaseAgent
    {
        public ExceptionTestAgent() : base("exception-agent", "Exception test agent")
        {
        }

        protected override Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test exception");
        }
    }
}