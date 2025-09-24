using Agent.Tools;
using Agent.Tools.Models;

namespace Agent.Tools.Tests;

public class BaseToolTests
{
    [Tool("test_tool")]
    [Description("Test tool for unit testing")]
    private class TestTool : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema()
        {
            return new Dictionary<string, Type>
            {
                { "input", typeof(string) },
                { "count", typeof(int) }
            };
        }

        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var input = parameters["input"].ToString();
            var count = Convert.ToInt32(parameters["count"]);

            await Task.Delay(10, cancellationToken);

            return ToolResult.CreateSuccess($"Processed {input} {count} times");
        }
    }

    private class InvalidTool : BaseTool
    {
        // Missing attributes
        protected override Dictionary<string, Type> DefineParameterSchema()
        {
            return new Dictionary<string, Type>();
        }

        protected override Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(ToolResult.CreateSuccess());
        }
    }

    [Fact]
    public void BaseTool_ShouldExtractNameAndDescriptionFromAttributes()
    {
        var tool = new TestTool();

        Assert.Equal("test_tool", tool.Name);
        Assert.Equal("Test tool for unit testing", tool.Description);
    }

    [Fact]
    public void BaseTool_ShouldThrowForMissingToolAttribute()
    {
        Assert.Throws<InvalidOperationException>(() => new InvalidTool());
    }

    [Fact]
    public void BaseTool_ShouldReturnCorrectParameterSchema()
    {
        var tool = new TestTool();
        var schema = tool.GetParameterSchema();

        Assert.Equal(2, schema.Count);
        Assert.Equal(typeof(string), schema["input"]);
        Assert.Equal(typeof(int), schema["count"]);
    }

    [Fact]
    public void BaseTool_ShouldValidateRequiredParameters()
    {
        var tool = new TestTool();
        var validParams = new Dictionary<string, object>
        {
            { "input", "test" },
            { "count", 5 }
        };
        var invalidParams = new Dictionary<string, object>
        {
            { "input", "test" }
            // Missing count parameter
        };

        Assert.True(tool.ValidateParameters(validParams));
        Assert.False(tool.ValidateParameters(invalidParams));
    }

    [Fact]
    public void BaseTool_ShouldValidateParameterTypes()
    {
        var tool = new TestTool();
        var invalidTypeParams = new Dictionary<string, object>
        {
            { "input", "test" },
            { "count", "not_an_int" }
        };

        Assert.False(tool.ValidateParameters(invalidTypeParams));
    }

    [Fact]
    public async Task BaseTool_ShouldExecuteSuccessfully()
    {
        var tool = new TestTool();
        var parameters = new Dictionary<string, object>
        {
            { "input", "hello" },
            { "count", 3 }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Equal("Processed hello 3 times", result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task BaseTool_ShouldReturnErrorForInvalidParameters()
    {
        var tool = new TestTool();
        var invalidParams = new Dictionary<string, object>
        {
            { "input", "test" }
            // Missing count
        };

        var result = await tool.ExecuteAsync(invalidParams);

        Assert.False(result.Success);
        Assert.Contains("Invalid parameters", result.Error);
    }

    [Fact]
    public async Task BaseTool_ShouldHandleExecutionException()
    {
        var exceptionTool = new ExceptionTestTool();
        var parameters = new Dictionary<string, object>();

        var result = await exceptionTool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.Contains("Test exception", result.Error);
    }

    [Fact]
    public async Task BaseTool_ShouldHandleCancellation()
    {
        var tool = new SlowTestTool();
        var parameters = new Dictionary<string, object>();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        var result = await tool.ExecuteAsync(parameters, cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Tool("exception_tool")]
    [Description("Tool that throws exception")]
    private class ExceptionTestTool : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema()
        {
            return new Dictionary<string, Type>();
        }

        protected override Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    [Tool("slow_tool")]
    [Description("Slow tool for testing cancellation")]
    private class SlowTestTool : BaseTool
    {
        protected override Dictionary<string, Type> DefineParameterSchema()
        {
            return new Dictionary<string, Type>();
        }

        protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken); // Wait longer than cancellation timeout
            return ToolResult.CreateSuccess();
        }
    }
}