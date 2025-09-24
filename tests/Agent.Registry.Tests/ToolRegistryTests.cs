using Agent.Registry;
using Agent.Tools;
using Agent.Tools.Models;
using NSubstitute;
using System.Reflection;

namespace Agent.Registry.Tests;

public class ToolRegistryTests
{
    [Fact]
    public void ToolRegistry_ShouldHaveRequiredProperties()
    {
        var registry = new ToolRegistry();

        Assert.NotNull(registry);
    }

    [Fact]
    public async Task RegisterToolAsync_WithValidTool_ShouldRegisterSuccessfully()
    {
        var registry = new ToolRegistry();
        var tool = new TestTool();

        await registry.RegisterToolAsync(tool);

        var registered = await registry.GetToolAsync("test-tool");
        Assert.NotNull(registered);
        Assert.Equal("test-tool", registered.Name);
    }

    [Fact]
    public async Task RegisterToolAsync_WithNullTool_ShouldThrowArgumentNullException()
    {
        var registry = new ToolRegistry();

        await Assert.ThrowsAsync<ArgumentNullException>(() => registry.RegisterToolAsync((ITool)null!));
    }

    [Fact]
    public async Task RegisterToolAsync_WithDuplicateName_ShouldOverwriteExisting()
    {
        var registry = new ToolRegistry();
        var tool1 = new TestTool();
        var tool2 = new TestTool();

        await registry.RegisterToolAsync(tool1);
        await registry.RegisterToolAsync(tool2);

        var tools = await registry.GetAllToolsAsync();
        Assert.Single(tools);
    }

    [Fact]
    public async Task RegisterToolAsync_WithType_ShouldCreateAndRegisterInstance()
    {
        var registry = new ToolRegistry();

        await registry.RegisterToolAsync(typeof(TestTool));

        var tool = await registry.GetToolAsync("test-tool");
        Assert.NotNull(tool);
        Assert.IsType<TestTool>(tool);
    }

    [Fact]
    public async Task RegisterToolAsync_WithInvalidType_ShouldThrowArgumentException()
    {
        var registry = new ToolRegistry();

        await Assert.ThrowsAsync<ArgumentException>(() => registry.RegisterToolAsync(typeof(string)));
    }

    [Fact]
    public async Task GetToolAsync_WithNonExistentName_ShouldReturnNull()
    {
        var registry = new ToolRegistry();

        var result = await registry.GetToolAsync("non-existent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetToolAsync_WithNullName_ShouldThrowArgumentException()
    {
        var registry = new ToolRegistry();

        await Assert.ThrowsAsync<ArgumentException>(() => registry.GetToolAsync(null!));
    }

    [Fact]
    public async Task GetAllToolsAsync_WithNoTools_ShouldReturnEmptyCollection()
    {
        var registry = new ToolRegistry();

        var result = await registry.GetAllToolsAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllToolsAsync_WithMultipleTools_ShouldReturnAllTools()
    {
        var registry = new ToolRegistry();
        var tool1 = new TestTool();
        var tool2 = new AnotherTestTool();

        await registry.RegisterToolAsync(tool1);
        await registry.RegisterToolAsync(tool2);

        var result = await registry.GetAllToolsAsync();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.Name == "test-tool");
        Assert.Contains(result, t => t.Name == "another-test-tool");
    }

    [Fact]
    public async Task IsRegisteredAsync_WithRegisteredTool_ShouldReturnTrue()
    {
        var registry = new ToolRegistry();
        var tool = new TestTool();
        await registry.RegisterToolAsync(tool);

        var result = await registry.IsRegisteredAsync("test-tool");

        Assert.True(result);
    }

    [Fact]
    public async Task IsRegisteredAsync_WithUnregisteredTool_ShouldReturnFalse()
    {
        var registry = new ToolRegistry();

        var result = await registry.IsRegisteredAsync("non-existent");

        Assert.False(result);
    }

    [Fact]
    public async Task UnregisterToolAsync_WithRegisteredTool_ShouldReturnTrueAndRemoveTool()
    {
        var registry = new ToolRegistry();
        var tool = new TestTool();
        await registry.RegisterToolAsync(tool);

        var result = await registry.UnregisterToolAsync("test-tool");

        Assert.True(result);
        var removed = await registry.GetToolAsync("test-tool");
        Assert.Null(removed);
    }

    [Fact]
    public async Task UnregisterToolAsync_WithUnregisteredTool_ShouldReturnFalse()
    {
        var registry = new ToolRegistry();

        var result = await registry.UnregisterToolAsync("non-existent");

        Assert.False(result);
    }

    [Fact]
    public async Task DiscoverAndRegisterToolsAsync_ShouldFindAndRegisterToolsInCurrentAssembly()
    {
        var registry = new ToolRegistry();

        await registry.DiscoverAndRegisterToolsAsync();

        var tools = await registry.GetAllToolsAsync();
        Assert.NotEmpty(tools);
    }

    [Fact]
    public async Task RegistryOperations_ShouldBeThreadSafe()
    {
        var registry = new ToolRegistry();
        var tasks = new List<Task>();
        var toolTypes = new[]
        {
            typeof(ThreadTestTool1), typeof(ThreadTestTool2), typeof(ThreadTestTool3),
            typeof(ThreadTestTool4), typeof(ThreadTestTool5)
        };

        // Simulate concurrent registrations
        for (int i = 0; i < toolTypes.Length; i++)
        {
            var toolType = toolTypes[i];
            tasks.Add(Task.Run(async () =>
            {
                await registry.RegisterToolAsync(toolType);
            }));
        }

        await Task.WhenAll(tasks);

        var allTools = await registry.GetAllToolsAsync();
        Assert.Equal(toolTypes.Length, allTools.Count());
    }

    // Test tools for testing
    [Tool("test-tool")]
    [Description("A test tool for unit testing")]
    private class TestTool : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema()
        {
            return new Dictionary<string, Type>
            {
                { "input", typeof(string) }
            };
        }

        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return ToolResult.CreateSuccess("Test result");
        }
    }

    [Tool("another-test-tool")]
    [Description("Another test tool for unit testing")]
    private class AnotherTestTool : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema()
        {
            return new Dictionary<string, Type>();
        }

        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return ToolResult.CreateSuccess("Another test result");
        }
    }

    // Thread safety test tools
    [Tool("thread-test-tool-1")]
    [Description("Thread test tool 1")]
    private class ThreadTestTool1 : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema() => new();
        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return ToolResult.CreateSuccess("Thread test 1");
        }
    }

    [Tool("thread-test-tool-2")]
    [Description("Thread test tool 2")]
    private class ThreadTestTool2 : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema() => new();
        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return ToolResult.CreateSuccess("Thread test 2");
        }
    }

    [Tool("thread-test-tool-3")]
    [Description("Thread test tool 3")]
    private class ThreadTestTool3 : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema() => new();
        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return ToolResult.CreateSuccess("Thread test 3");
        }
    }

    [Tool("thread-test-tool-4")]
    [Description("Thread test tool 4")]
    private class ThreadTestTool4 : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema() => new();
        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return ToolResult.CreateSuccess("Thread test 4");
        }
    }

    [Tool("thread-test-tool-5")]
    [Description("Thread test tool 5")]
    private class ThreadTestTool5 : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema() => new();
        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return ToolResult.CreateSuccess("Thread test 5");
        }
    }
}