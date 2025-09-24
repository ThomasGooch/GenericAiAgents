using Agent.Core.Models;
using Agent.Tools;
using Agent.Tools.Models;

namespace Agent.Tools.Tests;

public class IToolTests
{
    [Fact]
    public void ITool_ShouldHaveRequiredProperties()
    {
        // This test validates that any implementation of ITool has the required properties
        var toolType = typeof(ITool);
        
        Assert.True(toolType.IsInterface);
        Assert.NotNull(toolType.GetProperty("Name"));
        Assert.NotNull(toolType.GetProperty("Description"));
    }

    [Fact]
    public void ITool_ShouldHaveRequiredMethods()
    {
        // This test validates that any implementation of ITool has the required methods
        var toolType = typeof(ITool);
        
        Assert.NotNull(toolType.GetMethod("ExecuteAsync"));
        Assert.NotNull(toolType.GetMethod("ValidateParameters"));
        Assert.NotNull(toolType.GetMethod("GetParameterSchema"));
    }

    [Fact]
    public void ToolResult_ShouldHaveRequiredProperties()
    {
        // Test that ToolResult model exists and has required properties
        var result = new ToolResult
        {
            Success = true,
            Data = "test data",
            Error = null,
            Metadata = new Dictionary<string, object> { {"key", "value"} }
        };

        Assert.True(result.Success);
        Assert.Equal("test data", result.Data);
        Assert.Null(result.Error);
        Assert.Single(result.Metadata);
    }

    [Fact]
    public void ToolResult_ShouldProvideStaticSuccessMethod()
    {
        var data = "success data";
        var result = ToolResult.CreateSuccess(data);

        Assert.True(result.Success);
        Assert.Equal(data, result.Data);
        Assert.Null(result.Error);
        Assert.NotNull(result.Metadata);
    }

    [Fact]
    public void ToolResult_ShouldProvideStaticErrorMethod()
    {
        var error = "error message";
        var result = ToolResult.CreateError(error);

        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(error, result.Error);
        Assert.NotNull(result.Metadata);
    }

    [Fact]
    public void ToolAttribute_ShouldStoreNameCorrectly()
    {
        var toolName = "test_tool";
        var attribute = new ToolAttribute(toolName);

        Assert.Equal(toolName, attribute.Name);
    }

    [Fact]
    public void DescriptionAttribute_ShouldStoreDescriptionCorrectly()
    {
        var description = "Test tool description";
        var attribute = new DescriptionAttribute(description);

        Assert.Equal(description, attribute.Description);
    }
}