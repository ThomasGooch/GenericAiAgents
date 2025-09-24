using Agent.Tools;
using Agent.Tools.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Agent.Tools.Samples;

/// <summary>
/// Tool for making HTTP requests to web APIs
/// </summary>
[Tool("http-client")]
[Description("Makes HTTP requests to web APIs and returns responses")]
public class HttpClientTool : BaseTool, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed = false;

    private static readonly string[] _allowedMethods = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };

    public HttpClientTool()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    protected override Dictionary<string, Type> DefineParameterSchema()
    {
        return new Dictionary<string, Type>
        {
            { "url", typeof(string) },
            { "method", typeof(string) },
            { "headers", typeof(string) },
            { "body", typeof(string) }
        };
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        try
        {
            var url = parameters["url"].ToString()!;
            var method = parameters["method"].ToString()!.ToUpperInvariant();
            var headersJson = parameters["headers"].ToString() ?? "{}";
            var body = parameters["body"].ToString() ?? "";

            // Validate HTTP method
            if (!_allowedMethods.Contains(method))
            {
                return ToolResult.CreateError($"Unsupported HTTP method: {method}. Supported methods: {string.Join(", ", _allowedMethods)}");
            }

            // Validate URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return ToolResult.CreateError($"Invalid URL: {url}");
            }

            // Parse headers
            Dictionary<string, string>? headers = null;
            if (!string.IsNullOrEmpty(headersJson) && headersJson != "{}")
            {
                try
                {
                    headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                }
                catch (JsonException ex)
                {
                    return ToolResult.CreateError($"Invalid JSON in headers: {ex.Message}");
                }
            }

            // Create HTTP request
            using var request = new HttpRequestMessage(new HttpMethod(method), uri);

            // Add headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    try
                    {
                        // Try adding as request header first
                        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            // If that fails, try as content header (will be added when content is set)
                            // Store for later addition to content headers
                        }
                    }
                    catch (Exception ex)
                    {
                        return ToolResult.CreateError($"Invalid header '{header.Key}': {ex.Message}");
                    }
                }
            }

            // Add body for methods that support it
            if (!string.IsNullOrEmpty(body) && (method == "POST" || method == "PUT" || method == "PATCH"))
            {
                request.Content = new StringContent(body, Encoding.UTF8);

                // Add content-type headers if specified
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                        {
                            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                // Default content type if not specified
                if (request.Content.Headers.ContentType == null)
                {
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }
            }

            // Send request
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            // Read response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
            foreach (var contentHeader in response.Content.Headers)
            {
                responseHeaders[contentHeader.Key] = string.Join(", ", contentHeader.Value);
            }

            // Create response object
            var result = new
            {
                StatusCode = (int)response.StatusCode,
                StatusDescription = response.ReasonPhrase,
                IsSuccess = response.IsSuccessStatusCode,
                Headers = responseHeaders,
                Content = responseContent,
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength
            };

            var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

            return ToolResult.CreateSuccess(resultJson);
        }
        catch (TaskCanceledException ex)
        {
            if (ex.CancellationToken.IsCancellationRequested)
            {
                return ToolResult.CreateError($"HTTP request was cancelled: {ex.Message}");
            }
            else
            {
                return ToolResult.CreateError($"HTTP request timeout: {ex.Message}");
            }
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.CreateError($"HTTP request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Unexpected error during HTTP request: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}