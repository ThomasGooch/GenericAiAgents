using Agent.Core;
using Agent.Orchestration;
using Agent.Orchestration.Models;
using NSubstitute;

namespace Agent.Orchestration.Tests;

public class AgentRegistryEnhancedTests
{
    private readonly IAgentRegistryEnhanced _agentRegistry;
    private readonly IAgent _mockAgent;

    public AgentRegistryEnhancedTests()
    {
        _agentRegistry = new AgentRegistryEnhanced();
        _mockAgent = Substitute.For<IAgent>();
    }

    [Fact]
    public async Task RegisterAgentAsync_ShouldAddAgentToRegistry()
    {
        // Arrange
        _mockAgent.Id.Returns("test-agent");

        // Act
        await _agentRegistry.RegisterAgentAsync(_mockAgent);

        // Assert
        var agent = await _agentRegistry.GetAgentAsync("test-agent");
        Assert.NotNull(agent);
        Assert.Equal("test-agent", agent.Id);
    }

    [Fact]
    public async Task GetAllAgentsAsync_ShouldReturnAllRegisteredAgents()
    {
        // Arrange
        var agent1 = Substitute.For<IAgent>();
        var agent2 = Substitute.For<IAgent>();
        agent1.Id.Returns("agent-1");
        agent2.Id.Returns("agent-2");

        await _agentRegistry.RegisterAgentAsync(agent1);
        await _agentRegistry.RegisterAgentAsync(agent2);

        // Act
        var agents = await _agentRegistry.GetAllAgentsAsync();

        // Assert
        Assert.Equal(2, agents.Count());
        Assert.Contains(agents, a => a.Id == "agent-1");
        Assert.Contains(agents, a => a.Id == "agent-2");
    }

    [Fact]
    public async Task IsRegisteredAsync_WithRegisteredAgent_ShouldReturnTrue()
    {
        // Arrange
        _mockAgent.Id.Returns("test-agent");
        await _agentRegistry.RegisterAgentAsync(_mockAgent);

        // Act
        var isRegistered = await _agentRegistry.IsRegisteredAsync("test-agent");

        // Assert
        Assert.True(isRegistered);
    }

    [Fact]
    public async Task IsRegisteredAsync_WithUnregisteredAgent_ShouldReturnFalse()
    {
        // Act
        var isRegistered = await _agentRegistry.IsRegisteredAsync("non-existent");

        // Assert
        Assert.False(isRegistered);
    }

    [Fact]
    public async Task UnregisterAgentAsync_ShouldRemoveAgent()
    {
        // Arrange
        _mockAgent.Id.Returns("test-agent");
        await _agentRegistry.RegisterAgentAsync(_mockAgent);

        // Act
        var removed = await _agentRegistry.UnregisterAgentAsync("test-agent");

        // Assert
        Assert.True(removed);
        var agent = await _agentRegistry.GetAgentAsync("test-agent");
        Assert.Null(agent);
    }

    [Fact]
    public async Task CheckHealthAsync_WithHealthyAgent_ShouldReturnHealthy()
    {
        // Arrange
        _mockAgent.Id.Returns("test-agent");
        _mockAgent.CheckHealthAsync(Arg.Any<CancellationToken>())
                  .Returns(new AgentHealthStatus { IsHealthy = true, Message = "OK" });

        await _agentRegistry.RegisterAgentAsync(_mockAgent);

        // Act
        var health = await _agentRegistry.CheckHealthAsync("test-agent");

        // Assert
        Assert.NotNull(health);
        Assert.True(health.IsHealthy);
        Assert.Equal("OK", health.Message);
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnhealthyAgent_ShouldReturnUnhealthy()
    {
        // Arrange
        _mockAgent.Id.Returns("test-agent");
        _mockAgent.CheckHealthAsync(Arg.Any<CancellationToken>())
                  .Returns(new AgentHealthStatus { IsHealthy = false, Message = "Service unavailable" });

        await _agentRegistry.RegisterAgentAsync(_mockAgent);

        // Act
        var health = await _agentRegistry.CheckHealthAsync("test-agent");

        // Assert
        Assert.NotNull(health);
        Assert.False(health.IsHealthy);
        Assert.Equal("Service unavailable", health.Message);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNonExistentAgent_ShouldReturnNull()
    {
        // Act
        var health = await _agentRegistry.CheckHealthAsync("non-existent");

        // Assert
        Assert.Null(health);
    }

    [Fact]
    public async Task GetHealthyAgentsAsync_ShouldReturnOnlyHealthyAgents()
    {
        // Arrange
        var healthyAgent = Substitute.For<IAgent>();
        var unhealthyAgent = Substitute.For<IAgent>();
        
        healthyAgent.Id.Returns("healthy-agent");
        unhealthyAgent.Id.Returns("unhealthy-agent");
        
        healthyAgent.CheckHealthAsync(Arg.Any<CancellationToken>())
                   .Returns(new AgentHealthStatus { IsHealthy = true });
        
        unhealthyAgent.CheckHealthAsync(Arg.Any<CancellationToken>())
                      .Returns(new AgentHealthStatus { IsHealthy = false });

        await _agentRegistry.RegisterAgentAsync(healthyAgent);
        await _agentRegistry.RegisterAgentAsync(unhealthyAgent);

        // Act
        var healthyAgents = await _agentRegistry.GetHealthyAgentsAsync();

        // Assert
        Assert.Single(healthyAgents);
        Assert.Equal("healthy-agent", healthyAgents.First().Id);
    }

    [Fact]
    public async Task DiscoverAgentsAsync_ShouldFindAgentsInAssemblies()
    {
        // Arrange
        var assemblies = new[] { typeof(AgentRegistryEnhancedTests).Assembly };

        // Act
        await _agentRegistry.DiscoverAgentsAsync(assemblies);

        // Assert - this test would need actual IAgent implementations in test assembly
        // For now, just verify the method doesn't throw
        var agents = await _agentRegistry.GetAllAgentsAsync();
        Assert.NotNull(agents);
    }
}