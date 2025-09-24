using Agent.Tools;
using Agent.Tools.Models;
using Agent.Tools.Samples;

namespace Agent.Tools.Samples.Tests;

public class TextManipulationToolTests
{
    [Fact]
    public void TextManipulationTool_ShouldHaveCorrectMetadata()
    {
        var tool = new TextManipulationTool();
        
        Assert.Equal("text-manipulation", tool.Name);
        Assert.Equal("Performs text manipulation operations like transform, search, replace, and format text", tool.Description);
    }

    [Fact]
    public void GetParameterSchema_ShouldReturnExpectedSchema()
    {
        var tool = new TextManipulationTool();
        
        var schema = tool.GetParameterSchema();
        
        Assert.Contains("operation", schema.Keys);
        Assert.Contains("text", schema.Keys);
        Assert.Contains("search", schema.Keys);
        Assert.Contains("replace", schema.Keys);
        
        Assert.Equal(typeof(string), schema["operation"]);
        Assert.Equal(typeof(string), schema["text"]);
        Assert.Equal(typeof(string), schema["search"]);
        Assert.Equal(typeof(string), schema["replace"]);
    }

    [Fact]
    public async Task ExecuteAsync_WithUpperCaseOperation_ShouldReturnUpperCase()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "uppercase" },
            { "text", "Hello World" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("HELLO WORLD", result.Data?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WithLowerCaseOperation_ShouldReturnLowerCase()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "lowercase" },
            { "text", "Hello World" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("hello world", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithReplaceOperation_ShouldReplaceText()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "replace" },
            { "text", "Hello World! Hello Universe!" },
            { "search", "Hello" },
            { "replace", "Hi" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("Hi World! Hi Universe!", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithTrimOperation_ShouldTrimWhitespace()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "trim" },
            { "text", "   Hello World   " },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("\"Result\": \"Hello World\"", result.Output);
        Assert.Contains("\"OriginalText\": \"   Hello World   \"", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithReverseOperation_ShouldReverseText()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "reverse" },
            { "text", "Hello" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("olleH", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithCountOperation_ShouldCountCharacters()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "count" },
            { "text", "Hello World" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("11", result.Output); // "Hello World" has 11 characters
    }

    [Fact]
    public async Task ExecuteAsync_WithWordsOperation_ShouldSplitIntoWords()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "words" },
            { "text", "Hello World Test" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("Hello", result.Output);
        Assert.Contains("World", result.Output);
        Assert.Contains("Test", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithLinesOperation_ShouldSplitIntoLines()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "lines" },
            { "text", "Line 1\nLine 2\nLine 3" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("Line 1", result.Output);
        Assert.Contains("Line 2", result.Output);
        Assert.Contains("Line 3", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithContainsOperation_ShouldCheckContains()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "contains" },
            { "text", "Hello World" },
            { "search", "World" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("true", result.Output.ToLowerInvariant());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidOperation_ShouldReturnError()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "invalid" },
            { "text", "Hello World" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.Contains("Unsupported operation", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyText_ShouldHandleGracefully()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "uppercase" },
            { "text", "" },
            { "search", "" },
            { "replace", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WithReplaceNoMatch_ShouldReturnOriginal()
    {
        var tool = new TextManipulationTool();
        var parameters = new Dictionary<string, object>
        {
            { "operation", "replace" },
            { "text", "Hello World" },
            { "search", "xyz" },
            { "replace", "abc" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.True(result.Success);
        Assert.Contains("Hello World", result.Output);
    }
}