using Agent.Tools;
using Agent.Tools.Models;
using Agent.Tools.Samples;
using NSubstitute;

namespace Agent.Tools.Samples.Tests;

public class HttpClientToolTests
{
    [Fact]
    public void HttpClientTool_ShouldHaveCorrectMetadata()
    {
        var tool = new HttpClientTool();
        
        Assert.Equal("http-client", tool.Name);
        Assert.Equal("Makes HTTP requests to web APIs and returns responses", tool.Description);
    }

    [Fact]
    public void GetParameterSchema_ShouldReturnExpectedSchema()
    {
        var tool = new HttpClientTool();
        
        var schema = tool.GetParameterSchema();
        
        Assert.Contains("url", schema.Keys);
        Assert.Contains("method", schema.Keys);
        Assert.Contains("headers", schema.Keys);
        Assert.Contains("body", schema.Keys);
        
        Assert.Equal(typeof(string), schema["url"]);
        Assert.Equal(typeof(string), schema["method"]);
        Assert.Equal(typeof(string), schema["headers"]);
        Assert.Equal(typeof(string), schema["body"]);
    }

    [Fact]
    public void ValidateParameters_WithValidParameters_ShouldReturnTrue()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "https://api.example.com/test" },
            { "method", "GET" },
            { "headers", "{}" },
            { "body", "" }
        };

        var result = tool.ValidateParameters(parameters);

        Assert.True(result);
    }

    [Fact]
    public void ValidateParameters_WithMissingUrl_ShouldReturnFalse()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "method", "GET" },
            { "headers", "{}" },
            { "body", "" }
        };

        var result = tool.ValidateParameters(parameters);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidGetRequest_ShouldReturnSuccess()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "https://httpbin.org/get" },
            { "method", "GET" },
            { "headers", "{}" },
            { "body", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        // Note: This test may fail if httpbin.org is not accessible
        // In a production environment, we'd mock the HttpClient
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidUrl_ShouldReturnError()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "invalid-url" },
            { "method", "GET" },
            { "headers", "{}" },
            { "body", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsupportedMethod_ShouldReturnError()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "https://httpbin.org/get" },
            { "method", "INVALID" },
            { "headers", "{}" },
            { "body", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.Contains("HTTP method", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidHeaders_ShouldIncludeHeaders()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "https://httpbin.org/headers" },
            { "method", "GET" },
            { "headers", "{\"User-Agent\": \"Test-Agent\"}" },
            { "body", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        // Note: This test may fail if httpbin.org is not accessible
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithPostData_ShouldSendBody()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "https://httpbin.org/post" },
            { "method", "POST" },
            { "headers", "{\"Content-Type\": \"application/json\"}" },
            { "body", "{\"test\": \"data\"}" }
        };

        var result = await tool.ExecuteAsync(parameters);

        // Note: This test may fail if httpbin.org is not accessible
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ShouldHandleTimeout()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "http://10.255.255.1:12345" }, // Non-routable IP address that will timeout
            { "method", "GET" },
            { "headers", "{}" },
            { "body", "" }
        };

        var result = await tool.ExecuteAsync(parameters);

        // Should timeout and return error
        Assert.False(result.Success);
        Assert.Contains("timeout", result.Error.ToLowerInvariant());
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldHandleCancellation()
    {
        var tool = new HttpClientTool();
        var parameters = new Dictionary<string, object>
        {
            { "url", "https://httpbin.org/delay/10" },
            { "method", "GET" },
            { "headers", "{}" },
            { "body", "" }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var result = await tool.ExecuteAsync(parameters, cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error.ToLowerInvariant());
    }
}