using Agent.Observability;
using Agent.Observability.Models;
using Agent.Core;
using Agent.Orchestration;
using NSubstitute;

namespace Agent.Observability.Tests;

public class HealthCheckServiceTests
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly IAgentRegistryEnhanced _mockAgentRegistry;
    private readonly IAgent _mockAgent;

    public HealthCheckServiceTests()
    {
        _mockAgentRegistry = Substitute.For<IAgentRegistryEnhanced>();
        _mockAgent = Substitute.For<IAgent>();
        _healthCheckService = new HealthCheckService(_mockAgentRegistry);
    }

    [Fact]
    public async Task CheckSystemHealthAsync_WithHealthyAgents_ShouldReturnHealthy()
    {
        // Arrange
        var healthyAgents = new List<IAgent> { _mockAgent };
        var healthStatus = new AgentHealthStatus
        {
            IsHealthy = true,
            Message = "Agent is healthy",
            Timestamp = DateTime.UtcNow
        };

        _mockAgentRegistry.GetAllAgentsAsync().Returns(healthyAgents);
        _mockAgentRegistry.CheckHealthAsync(_mockAgent.Id).Returns(healthStatus);
        _mockAgent.Id.Returns("test-agent");

        // Act
        var result = await _healthCheckService.CheckSystemHealthAsync();

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal(SystemHealthLevel.Healthy, result.OverallStatus);
        Assert.Single(result.ComponentHealth);
        Assert.Equal("test-agent", result.ComponentHealth.First().Key);
        Assert.True(result.ComponentHealth.First().Value.IsHealthy);
    }

    [Fact]
    public async Task CheckSystemHealthAsync_WithUnhealthyAgents_ShouldReturnDegraded()
    {
        // Arrange
        var agents = new List<IAgent> { _mockAgent };
        var unhealthyStatus = new AgentHealthStatus
        {
            IsHealthy = false,
            Message = "Agent is experiencing issues",
            Timestamp = DateTime.UtcNow
        };

        _mockAgentRegistry.GetAllAgentsAsync().Returns(agents);
        _mockAgentRegistry.CheckHealthAsync(_mockAgent.Id).Returns(unhealthyStatus);
        _mockAgent.Id.Returns("unhealthy-agent");

        // Act
        var result = await _healthCheckService.CheckSystemHealthAsync();

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal(SystemHealthLevel.Critical, result.OverallStatus);
        Assert.Single(result.ComponentHealth);
        Assert.False(result.ComponentHealth.First().Value.IsHealthy);
        Assert.Contains("Agent is experiencing issues", result.ComponentHealth.First().Value.Message);
    }

    [Fact]
    public async Task CheckSystemHealthAsync_WithMixedAgentHealth_ShouldReturnDegraded()
    {
        // Arrange
        var healthyAgent = Substitute.For<IAgent>();
        var unhealthyAgent = Substitute.For<IAgent>();

        healthyAgent.Id.Returns("healthy-agent");
        unhealthyAgent.Id.Returns("unhealthy-agent");

        var agents = new List<IAgent> { healthyAgent, unhealthyAgent };

        _mockAgentRegistry.GetAllAgentsAsync().Returns(agents);
        _mockAgentRegistry.CheckHealthAsync("healthy-agent").Returns(new AgentHealthStatus
        {
            IsHealthy = true,
            Message = "Healthy"
        });
        _mockAgentRegistry.CheckHealthAsync("unhealthy-agent").Returns(new AgentHealthStatus
        {
            IsHealthy = false,
            Message = "Unhealthy"
        });

        // Act
        var result = await _healthCheckService.CheckSystemHealthAsync();

        // Assert
        Assert.False(result.IsHealthy); // System is unhealthy if any component is unhealthy
        Assert.Equal(SystemHealthLevel.Degraded, result.OverallStatus);
        Assert.Equal(2, result.ComponentHealth.Count);
        Assert.True(result.ComponentHealth["healthy-agent"].IsHealthy);
        Assert.False(result.ComponentHealth["unhealthy-agent"].IsHealthy);
    }

    [Fact]
    public async Task CheckComponentHealthAsync_WithValidComponent_ShouldReturnComponentHealth()
    {
        // Arrange
        var componentName = "database";
        var expectedHealth = new ComponentHealthStatus
        {
            Name = componentName,
            IsHealthy = true,
            Message = "Database connection is healthy",
            ResponseTime = TimeSpan.FromMilliseconds(50),
            Details = new Dictionary<string, object>
            {
                ["connection_count"] = 5,
                ["last_query_time"] = DateTime.UtcNow.AddSeconds(-1)
            }
        };

        // Act
        var result = await _healthCheckService.CheckComponentHealthAsync(componentName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(componentName, result.Name);
        Assert.True(result.IsHealthy);
        Assert.True(result.ResponseTime > TimeSpan.Zero);
        Assert.NotEmpty(result.Details);
    }

    [Fact]
    public async Task GetHealthReport_ShouldReturnComprehensiveReport()
    {
        // Arrange
        var agents = new List<IAgent> { _mockAgent };
        var healthStatus = new AgentHealthStatus
        {
            IsHealthy = true,
            Message = "Healthy",
            ResponseTime = TimeSpan.FromMilliseconds(25),
            Metrics = new Dictionary<string, object>
            {
                ["cpu_usage"] = 15.5,
                ["memory_usage"] = 1024 * 1024 * 512 // 512MB
            }
        };

        _mockAgentRegistry.GetAllAgentsAsync().Returns(agents);
        _mockAgentRegistry.CheckHealthAsync(_mockAgent.Id).Returns(healthStatus);
        _mockAgent.Id.Returns("test-agent");

        // Act
        var report = await _healthCheckService.GetHealthReportAsync();

        // Assert
        Assert.NotNull(report);
        Assert.True(report.SystemHealth.IsHealthy);
        Assert.Single(report.SystemHealth.ComponentHealth);
        Assert.True(report.Timestamp <= DateTime.UtcNow);
        Assert.NotEmpty(report.Details);
    }

    [Fact]
    public async Task RegisterHealthCheck_ShouldAllowCustomHealthChecks()
    {
        // Arrange
        var customCheckName = "custom-service";
        var customCheck = Substitute.For<IHealthCheck>();
        var healthResult = new HealthCheckResult
        {
            IsHealthy = true,
            Message = "Custom service is operational"
        };

        customCheck.CheckHealthAsync(Arg.Any<CancellationToken>()).Returns(healthResult);

        // Act
        _healthCheckService.RegisterHealthCheck(customCheckName, customCheck);
        var systemHealth = await _healthCheckService.CheckSystemHealthAsync();

        // Assert
        Assert.True(systemHealth.ComponentHealth.ContainsKey(customCheckName));
        Assert.True(systemHealth.ComponentHealth[customCheckName].IsHealthy);
        Assert.Equal("Custom service is operational", systemHealth.ComponentHealth[customCheckName].Message);
    }

    [Fact]
    public async Task StartPeriodicHealthChecks_ShouldExecuteChecksAtInterval()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(100);
        var tcs = new TaskCompletionSource<bool>();

        _mockAgentRegistry.GetAllAgentsAsync().Returns(new List<IAgent>());

        // Act
        await _healthCheckService.StartPeriodicHealthChecksAsync(interval);

        // Wait a bit to allow multiple checks
        await Task.Delay(350);

        await _healthCheckService.StopPeriodicHealthChecksAsync();

        // Assert
        // Verify that health checks were called multiple times
        await _mockAgentRegistry.Received(Arg.Is<int>(x => x >= 2)).GetAllAgentsAsync();
    }

    [Fact]
    public async Task CheckSystemHealthAsync_WithTimeout_ShouldHandleTimeouts()
    {
        // Arrange
        var slowAgent = Substitute.For<IAgent>();
        slowAgent.Id.Returns("slow-agent");

        var agents = new List<IAgent> { slowAgent };
        _mockAgentRegistry.GetAllAgentsAsync().Returns(agents);

        // Simulate a slow health check
        _mockAgentRegistry.CheckHealthAsync("slow-agent").Returns(Task.FromResult<AgentHealthStatus?>(new AgentHealthStatus { IsHealthy = true }));

        // Act
        var timeout = TimeSpan.FromMilliseconds(500);
        var result = await _healthCheckService.CheckSystemHealthAsync(timeout);

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal(SystemHealthLevel.Degraded, result.OverallStatus);
        Assert.Contains("timeout", result.ComponentHealth["slow-agent"].Message.ToLowerInvariant());
    }
}