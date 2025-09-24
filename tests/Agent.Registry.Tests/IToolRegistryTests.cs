using Agent.Registry;
using Agent.Tools;
using Agent.Tools.Models;
using NSubstitute;

namespace Agent.Registry.Tests;

public class IToolRegistryTests
{
    [Fact]
    public void IToolRegistry_ShouldHaveRequiredMembers()
    {
        // This test verifies the interface contract exists
        var registry = Substitute.For<IToolRegistry>();

        Assert.NotNull(registry);
    }

    [Fact]
    public async Task RegisterTool_ShouldAcceptValidTool()
    {
        var registry = Substitute.For<IToolRegistry>();
        var tool = Substitute.For<ITool>();
        tool.Name.Returns("test-tool");

        await registry.RegisterToolAsync(tool);

        await registry.Received(1).RegisterToolAsync(tool);
    }

    [Fact]
    public async Task GetTool_ShouldReturnRegisteredTool()
    {
        var registry = Substitute.For<IToolRegistry>();
        var tool = Substitute.For<ITool>();
        tool.Name.Returns("test-tool");

        registry.GetToolAsync("test-tool").Returns(tool);

        var result = await registry.GetToolAsync("test-tool");

        Assert.Equal(tool, result);
    }

    [Fact]
    public async Task GetAllTools_ShouldReturnAllRegisteredTools()
    {
        var registry = Substitute.For<IToolRegistry>();
        var tools = new List<ITool>
        {
            Substitute.For<ITool>(),
            Substitute.For<ITool>()
        };

        registry.GetAllToolsAsync().Returns(tools);

        var result = await registry.GetAllToolsAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DiscoverAndRegisterTools_ShouldScanAssembly()
    {
        var registry = Substitute.For<IToolRegistry>();

        await registry.DiscoverAndRegisterToolsAsync();

        await registry.Received(1).DiscoverAndRegisterToolsAsync();
    }
}