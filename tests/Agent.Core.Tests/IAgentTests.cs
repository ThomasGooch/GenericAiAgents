using Agent.Core;
using Agent.Core.Models;

namespace Agent.Core.Tests;

public class IAgentTests
{
    [Fact]
    public void IAgent_ShouldHaveRequiredProperties()
    {
        // This test validates that any implementation of IAgent has the required properties
        var agentType = typeof(IAgent);

        Assert.True(agentType.IsInterface);
        Assert.NotNull(agentType.GetProperty("Id"));
        Assert.NotNull(agentType.GetProperty("Name"));
        Assert.NotNull(agentType.GetProperty("Description"));
        Assert.NotNull(agentType.GetProperty("Configuration"));
        Assert.NotNull(agentType.GetProperty("IsInitialized"));
    }

    [Fact]
    public void IAgent_ShouldHaveRequiredMethods()
    {
        // This test validates that any implementation of IAgent has the required methods
        var agentType = typeof(IAgent);

        Assert.NotNull(agentType.GetMethod("InitializeAsync"));
        Assert.NotNull(agentType.GetMethod("ExecuteAsync"));

        // Check that IAgent inherits from IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(agentType));
    }

    [Fact]
    public void AgentRequest_ShouldHaveRequiredProperties()
    {
        // Test that AgentRequest model exists and has required properties
        var request = new AgentRequest
        {
            Id = Guid.NewGuid(),
            Input = "test input",
            Context = new Dictionary<string, object> { { "key", "value" } },
            Metadata = new Dictionary<string, object> { { "meta", "data" } }
        };

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal("test input", request.Input);
        Assert.Single(request.Context);
        Assert.Single(request.Metadata);
    }

    [Fact]
    public void AgentResult_ShouldHaveRequiredProperties()
    {
        // Test that AgentResult model exists and has required properties
        var result = new AgentResult
        {
            Success = true,
            Output = "test output",
            Error = null,
            Metadata = new Dictionary<string, object> { { "result", "metadata" } }
        };

        Assert.True(result.Success);
        Assert.Equal("test output", result.Output);
        Assert.Null(result.Error);
        Assert.Single(result.Metadata);
    }

    [Fact]
    public void AgentResult_ShouldProvideStaticSuccessMethod()
    {
        var output = "success output";
        var result = AgentResult.CreateSuccess(output);

        Assert.True(result.Success);
        Assert.Equal(output, result.Output);
        Assert.Null(result.Error);
        Assert.NotNull(result.Metadata);
    }

    [Fact]
    public void AgentResult_ShouldProvideStaticErrorMethod()
    {
        var error = "error message";
        var result = AgentResult.CreateError(error);

        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.Equal(error, result.Error);
        Assert.NotNull(result.Metadata);
    }
}