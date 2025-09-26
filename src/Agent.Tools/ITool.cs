using Agent.Tools.Models;

namespace Agent.Tools;

/// <summary>
/// Defines the contract for implementing tools that can be executed by AI agents in the GenericAgents framework.
/// 
/// Tools represent discrete, reusable capabilities that agents can invoke to perform specific operations
/// such as file system access, HTTP requests, database queries, or custom business logic. The ITool
/// interface provides a standardized way for agents to discover, validate, and execute tools with
/// type-safe parameter handling and comprehensive error reporting.
/// 
/// Key Design Principles:
/// - **Discoverability**: Tools are automatically discovered and registered through reflection and attributes
/// - **Type Safety**: Parameters are strongly typed with schema validation to prevent runtime errors
/// - **Composability**: Tools can be combined and chained together in agent workflows
/// - **Testability**: Tools have well-defined inputs, outputs, and side effects for comprehensive testing
/// - **Extensibility**: New tools can be easily added without modifying existing agent code
/// - **Resource Management**: Tools properly handle resource allocation and cleanup through cancellation tokens
/// 
/// Tool Categories:
/// - **I/O Operations**: File system, network, database access tools
/// - **Data Processing**: Text manipulation, parsing, transformation tools
/// - **Integration**: External service integration and API client tools
/// - **Utility**: Helper tools for common operations like validation, formatting
/// - **Business Logic**: Domain-specific tools implementing business rules and processes
/// 
/// Security Considerations:
/// - Tools should validate all inputs to prevent injection attacks and malicious usage
/// - File system tools must implement path traversal protection
/// - Network tools should respect rate limits and implement proper authentication
/// - Database tools must use parameterized queries to prevent SQL injection
/// - All tools should log their operations for security auditing and compliance
/// 
/// Performance Guidelines:
/// - Tools should be stateless and thread-safe to support concurrent execution
/// - Long-running operations should respect cancellation tokens for responsive cancellation
/// - Resources should be properly disposed through using statements or similar patterns
/// - Tools should implement appropriate caching strategies for expensive operations
/// - Memory usage should be optimized for tools that process large datasets
/// </summary>
/// <example>
/// Basic tool implementation for text manipulation:
/// <code>
/// [Tool("text-upper")]
/// [Description("Converts text to uppercase")]
/// public class TextUpperTool : ITool
/// {
///     public string Name =&amp;gt; "text-upper";
///     public string Description =&amp;gt; "Converts the provided text to uppercase";
/// 
///     public Dictionary&lt;string, Type&gt; GetParameterSchema()
///     {
///         return new Dictionary&lt;string, Type&gt;
///         {
///             ["text"] = typeof(string)
///         };
///     }
/// 
///     public bool ValidateParameters(Dictionary&lt;string, object&gt; parameters)
///     {
///         return parameters.ContainsKey("text") &amp;&amp; 
///                parameters["text"] is string textValue &amp;&amp; 
///                !string.IsNullOrEmpty(textValue);
///     }
/// 
///     public async Task&lt;ToolResult&gt; ExecuteAsync(
///         Dictionary&lt;string, object&gt; parameters, 
///         CancellationToken cancellationToken = default)
///     {
///         if (!ValidateParameters(parameters))
///         {
///             return ToolResult.CreateError("Invalid parameters: 'text' parameter is required and must be a non-empty string");
///         }
/// 
///         var text = parameters["text"].ToString()!;
///         var result = text.ToUpperInvariant();
/// 
///         return ToolResult.CreateSuccess(result, new Dictionary&lt;string, object&gt;
///         {
///             ["original_length"] = text.Length,
///             ["converted_length"] = result.Length,
///             ["operation"] = "uppercase_conversion"
///         });
///     }
/// }
/// </code>
/// 
/// Advanced tool with HTTP client integration:
/// <code>
/// [Tool("http-get")]
/// [Description("Performs HTTP GET requests with timeout and retry support")]
/// public class HttpGetTool : ITool
/// {
///     private readonly HttpClient _httpClient;
///     private readonly ILogger&lt;HttpGetTool&gt; _logger;
/// 
///     public HttpGetTool(HttpClient httpClient, ILogger&lt;HttpGetTool&gt; logger)
///     {
///         _httpClient = httpClient;
///         _logger = logger;
///     }
/// 
///     public string Name =&amp;gt; "http-get";
///     public string Description =&amp;gt; "Performs HTTP GET requests to specified URLs with configurable timeout and headers";
/// 
///     public Dictionary&lt;string, Type&gt; GetParameterSchema()
///     {
///         return new Dictionary&lt;string, Type&gt;
///         {
///             ["url"] = typeof(string),
///             ["headers"] = typeof(Dictionary&lt;string, string&gt;),
///             ["timeout_seconds"] = typeof(int)
///         };
///     }
/// 
///     public bool ValidateParameters(Dictionary&lt;string, object&gt; parameters)
///     {
///         if (!parameters.ContainsKey("url") || parameters["url"] is not string url)
///             return false;
/// 
///         if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || 
///             (uri.Scheme != "http" &amp;&amp; uri.Scheme != "https"))
///             return false;
/// 
///         return true;
///     }
/// 
///     public async Task&lt;ToolResult&gt; ExecuteAsync(
///         Dictionary&lt;string, object&gt; parameters, 
///         CancellationToken cancellationToken = default)
///     {
///         var url = parameters["url"].ToString()!;
///         var timeout = parameters.GetValueOrDefault("timeout_seconds", 30);
///         var headers = parameters.GetValueOrDefault("headers", new Dictionary&lt;string, string&gt;()) as Dictionary&lt;string, string&gt;;
/// 
///         try
///         {
///             using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
///             cts.CancelAfter(TimeSpan.FromSeconds((int)timeout));
/// 
///             using var request = new HttpRequestMessage(HttpMethod.Get, url);
///             
///             if (headers != null)
///             {
///                 foreach (var header in headers)
///                 {
///                     request.Headers.Add(header.Key, header.Value);
///                 }
///             }
/// 
///             using var response = await _httpClient.SendAsync(request, cts.Token);
///             var content = await response.Content.ReadAsStringAsync(cts.Token);
/// 
///             return ToolResult.CreateSuccess(content, new Dictionary&lt;string, object&gt;
///             {
///                 ["status_code"] = (int)response.StatusCode,
///                 ["content_type"] = response.Content.Headers.ContentType?.ToString() ?? "unknown",
///                 ["content_length"] = content.Length,
///                 ["response_time_ms"] = 0 // Would be calculated in real implementation
///             });
///         }
///         catch (TaskCanceledException)
///         {
///             return ToolResult.CreateError($"HTTP request to {url} timed out after {timeout} seconds");
///         }
///         catch (HttpRequestException ex)
///         {
///             _logger.LogError(ex, "HTTP request failed for URL: {Url}", url);
///             return ToolResult.CreateError($"HTTP request failed: {ex.Message}");
///         }
///     }
/// }
/// </code>
/// 
/// Tool usage in agent context:
/// <code>
/// public class DataProcessingAgent : BaseAgent
/// {
///     private readonly IToolRegistry _toolRegistry;
/// 
///     public async Task&lt;AgentResult&gt; ProcessDataAsync(string dataUrl)
///     {
///         // Get HTTP tool to fetch data
///         var httpTool = await _toolRegistry.GetToolAsync("http-get");
///         var httpResult = await httpTool.ExecuteAsync(new Dictionary&lt;string, object&gt;
///         {
///             ["url"] = dataUrl,
///             ["timeout_seconds"] = 60
///         });
/// 
///         if (!httpResult.IsSuccess)
///         {
///             return AgentResult.CreateError($"Failed to fetch data: {httpResult.ErrorMessage}");
///         }
/// 
///         // Get text processing tool to clean data
///         var textTool = await _toolRegistry.GetToolAsync("text-clean");
///         var cleanResult = await textTool.ExecuteAsync(new Dictionary&lt;string, object&gt;
///         {
///             ["text"] = httpResult.Data,
///             ["remove_html"] = true,
///             ["normalize_whitespace"] = true
///         });
/// 
///         return AgentResult.CreateSuccess(cleanResult.Data);
///     }
/// }
/// </code>
/// </example>
/// <remarks>
/// Implementation Guidelines:
/// - Tools should be stateless and thread-safe to support concurrent execution by multiple agents
/// - Parameter validation should be comprehensive and provide clear error messages
/// - Tools should handle edge cases gracefully and provide meaningful error information
/// - Long-running tools should check the cancellation token periodically and honor cancellation requests
/// - Tools should log important operations for debugging and auditing purposes
/// 
/// Error Handling Best Practices:
/// - Return ToolResult.CreateError() for validation failures and operational errors
/// - Include specific error messages that help with debugging and user feedback
/// - Log errors with appropriate severity levels for monitoring and troubleshooting
/// - Handle exceptions gracefully and convert them to appropriate ToolResult responses
/// 
/// Performance Considerations:
/// - Cache expensive resources (database connections, HTTP clients) at the application level
/// - Use streaming for large data operations when possible
/// - Implement appropriate timeouts for external service calls
/// - Consider memory usage when processing large datasets
/// 
/// Security Best Practices:
/// - Validate all inputs thoroughly to prevent injection attacks
/// - Implement appropriate access controls for sensitive operations
/// - Use secure communication protocols for network operations
/// - Log security-relevant operations for audit trails
/// - Follow principle of least privilege for file system and database access
/// 
/// Testing Recommendations:
/// - Write comprehensive unit tests covering normal and edge cases
/// - Test parameter validation thoroughly with valid and invalid inputs
/// - Mock external dependencies for reliable and fast test execution
/// - Test cancellation behavior and timeout handling
/// - Verify proper resource cleanup and disposal
/// </remarks>
public interface ITool
{
    /// <summary>
    /// Gets the unique name identifier for this tool used for registration and discovery.
    /// 
    /// The tool name serves as the primary identifier for tool lookup and should be unique
    /// within the tool registry. Names should follow a consistent naming convention that
    /// clearly indicates the tool's purpose and functionality.
    /// 
    /// Naming Conventions:
    /// - Use lowercase with hyphens for multi-word names (e.g., "file-read", "http-post")
    /// - Include the primary operation or domain (e.g., "text-", "file-", "db-")
    /// - Keep names concise but descriptive
    /// - Avoid special characters except hyphens
    /// - Use consistent prefixes for related tool families
    /// 
    /// The name is used by agents to request specific tools from the tool registry and
    /// should remain stable across different versions to maintain compatibility.
    /// </summary>
    /// <value>
    /// A unique string identifier that represents this tool in the tool registry.
    /// Must be non-null, non-empty, and unique within the application context.
    /// </value>
    /// <example>
    /// Examples of well-formed tool names:
    /// <code>
    /// // File system tools
    /// public string Name =&amp;gt; "file-read";
    /// public string Name =&amp;gt; "file-write";
    /// public string Name =&amp;gt; "directory-list";
    /// 
    /// // HTTP tools  
    /// public string Name =&amp;gt; "http-get";
    /// public string Name =&amp;gt; "http-post";
    /// 
    /// // Text processing tools
    /// public string Name =&amp;gt; "text-upper";
    /// public string Name =&amp;gt; "text-parse-json";
    /// 
    /// // Database tools
    /// public string Name =&amp;gt; "db-query";
    /// public string Name =&amp;gt; "db-execute";
    /// </code>
    /// </example>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of the tool's functionality and purpose.
    /// 
    /// The description should clearly explain what the tool does, when it should be used,
    /// and any important limitations or requirements. This information is used by agents
    /// and developers to understand the tool's capabilities and make informed decisions
    /// about when to use it.
    /// 
    /// Description Guidelines:
    /// - Start with a clear action verb describing the primary function
    /// - Include key capabilities and features
    /// - Mention important limitations or requirements
    /// - Keep it concise but informative (1-2 sentences ideal)
    /// - Use professional, technical language appropriate for developers
    /// - Include relevant technical details that affect usage decisions
    /// 
    /// The description is used in tool discovery interfaces, documentation generation,
    /// and debugging scenarios where tool behavior needs to be understood.
    /// </summary>
    /// <value>
    /// A descriptive string explaining the tool's functionality, capabilities, and usage.
    /// Must be non-null and should provide sufficient information for proper tool selection.
    /// </value>
    /// <example>
    /// Examples of effective tool descriptions:
    /// <code>
    /// // Clear, action-oriented descriptions
    /// public string Description =&amp;gt; "Reads text content from files with encoding detection and error handling";
    /// 
    /// public string Description =&amp;gt; "Performs HTTP GET requests with configurable timeout, headers, and retry logic";
    /// 
    /// public string Description =&amp;gt; "Converts text to uppercase using culture-invariant rules for consistent formatting";
    /// 
    /// public string Description =&amp;gt; "Executes SQL queries against configured databases with parameter binding and result mapping";
    /// 
    /// // Including important limitations
    /// public string Description =&amp;gt; "Lists directory contents with optional filtering; requires read permissions on target directory";
    /// </code>
    /// </example>
    string Description { get; }

    /// <summary>
    /// Executes the tool's primary operation with the provided parameters and returns the result.
    /// 
    /// This method represents the core functionality of the tool and is called by agents when
    /// they need to perform the tool's specific operation. The implementation should be robust,
    /// handle errors gracefully, and provide comprehensive results that include both success
    /// data and relevant metadata.
    /// 
    /// Execution Guidelines:
    /// - Validate parameters before processing (call ValidateParameters first)
    /// - Handle exceptions gracefully and convert to appropriate ToolResult responses
    /// - Respect the cancellation token and check it periodically for long operations
    /// - Log important operations and errors for debugging and monitoring
    /// - Return detailed metadata along with the primary result data
    /// - Ensure thread safety if the tool maintains any state
    /// 
    /// Error Handling:
    /// - Return ToolResult.CreateError() for validation failures and operational errors
    /// - Include specific error messages that help with debugging
    /// - Log errors with appropriate context and severity levels
    /// - Handle timeouts and cancellation appropriately
    /// </summary>
    /// <param name="parameters">
    /// A dictionary containing the input parameters for the tool execution. The keys should
    /// match the parameter names defined in the GetParameterSchema() method, and the values
    /// should be of the types specified in the schema. Parameters may include optional
    /// configuration values with sensible defaults.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the tool execution. Tools should
    /// check this token periodically during long-running operations and honor cancellation
    /// requests promptly. Default value allows for optional cancellation support.
    /// </param>
    /// <returns>
    /// A <see cref="ToolResult"/> containing the execution outcome. On success, includes
    /// the operation result data and relevant metadata. On failure, includes error details
    /// and context information for debugging and user feedback.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parameters"/> is null. Tools should validate this
    /// condition and return appropriate ToolResult.CreateError() responses instead.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// May be thrown when the <paramref name="cancellationToken"/> is cancelled during
    /// execution. Tools should handle cancellation gracefully and return appropriate
    /// ToolResult responses when possible.
    /// </exception>
    /// <example>
    /// Implementing ExecuteAsync with proper error handling:
    /// <code>
    /// public async Task&lt;ToolResult&gt; ExecuteAsync(
    ///     Dictionary&lt;string, object&gt; parameters, 
    ///     CancellationToken cancellationToken = default)
    /// {
    ///     // Validate parameters first
    ///     if (!ValidateParameters(parameters))
    ///     {
    ///         return ToolResult.CreateError("Invalid parameters provided");
    ///     }
    /// 
    ///     try
    ///     {
    ///         var inputData = parameters["data"].ToString();
    ///         var timeout = (int)(parameters.GetValueOrDefault("timeout", 30));
    /// 
    ///         // Check cancellation before starting work
    ///         cancellationToken.ThrowIfCancellationRequested();
    /// 
    ///         // Perform the main operation
    ///         using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    ///         cts.CancelAfter(TimeSpan.FromSeconds(timeout));
    /// 
    ///         var result = await ProcessDataAsync(inputData, cts.Token);
    /// 
    ///         // Return success with metadata
    ///         return ToolResult.CreateSuccess(result, new Dictionary&lt;string, object&gt;
    ///         {
    ///             ["processing_time_ms"] = processingTime.TotalMilliseconds,
    ///             ["input_size"] = inputData.Length,
    ///             ["output_size"] = result.ToString().Length
    ///         });
    ///     }
    ///     catch (OperationCanceledException)
    ///     {
    ///         return ToolResult.CreateError("Operation was cancelled");
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         _logger.LogError(ex, "Tool execution failed");
    ///         return ToolResult.CreateError($"Execution failed: {ex.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the provided parameters against the tool's requirements and constraints.
    /// 
    /// This method performs comprehensive validation of input parameters to ensure they
    /// meet the tool's requirements before execution. It should check for required parameters,
    /// validate data types, verify value ranges, and perform any domain-specific validation
    /// rules that apply to the tool's operation.
    /// 
    /// Validation should be fast and deterministic, as it may be called multiple times
    /// during tool discovery and execution planning. The method should not perform any
    /// side effects or expensive operations.
    /// 
    /// Validation Categories:
    /// - **Presence Validation**: Check that all required parameters are present
    /// - **Type Validation**: Verify that parameter values match expected types
    /// - **Range Validation**: Ensure numeric values are within acceptable ranges
    /// - **Format Validation**: Validate string formats (URLs, file paths, etc.)
    /// - **Business Rule Validation**: Apply domain-specific validation logic
    /// - **Dependency Validation**: Check that parameter combinations are valid
    /// </summary>
    /// <param name="parameters">
    /// A dictionary containing the parameters to validate. Keys represent parameter names
    /// and values represent the parameter data. May contain null values or missing keys
    /// that should be validated according to the tool's requirements.
    /// </param>
    /// <returns>
    /// <c>true</c> if all parameters are valid and the tool can execute successfully;
    /// <c>false</c> if any validation rules fail. When false is returned, the tool's
    /// ExecuteAsync method should not be called with these parameters.
    /// </returns>
    /// <example>
    /// Comprehensive parameter validation implementation:
    /// <code>
    /// public bool ValidateParameters(Dictionary&lt;string, object&gt; parameters)
    /// {
    ///     // Check for null parameters
    ///     if (parameters == null)
    ///         return false;
    /// 
    ///     // Validate required parameters
    ///     if (!parameters.ContainsKey("filename") || 
    ///         parameters["filename"] is not string filename || 
    ///         string.IsNullOrWhiteSpace(filename))
    ///         return false;
    /// 
    ///     // Validate file path format
    ///     if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
    ///         return false;
    /// 
    ///     // Validate optional parameters with defaults
    ///     if (parameters.ContainsKey("max_size"))
    ///     {
    ///         if (parameters["max_size"] is not int maxSize || maxSize &lt;= 0)
    ///             return false;
    ///     }
    /// 
    ///     // Validate enumeration values
    ///     if (parameters.ContainsKey("encoding"))
    ///     {
    ///         if (parameters["encoding"] is not string encoding ||
    ///             !new[] { "utf8", "ascii", "utf16" }.Contains(encoding.ToLowerInvariant()))
    ///             return false;
    ///     }
    /// 
    ///     return true;
    /// }
    /// 
    /// // Validation for HTTP tool with URL and header validation
    /// public bool ValidateParameters(Dictionary&lt;string, object&gt; parameters)
    /// {
    ///     // Validate required URL parameter
    ///     if (!parameters.ContainsKey("url") || parameters["url"] is not string url)
    ///         return false;
    /// 
    ///     // Validate URL format and scheme
    ///     if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
    ///         (uri.Scheme != "http" &amp;&amp; uri.Scheme != "https"))
    ///         return false;
    /// 
    ///     // Validate optional headers parameter
    ///     if (parameters.ContainsKey("headers"))
    ///     {
    ///         if (parameters["headers"] is not Dictionary&lt;string, string&gt; headers)
    ///             return false;
    /// 
    ///         // Validate header names and values
    ///         foreach (var header in headers)
    ///         {
    ///             if (string.IsNullOrWhiteSpace(header.Key) || 
    ///                 header.Value == null)
    ///                 return false;
    ///         }
    ///     }
    /// 
    ///     return true;
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Performance Considerations:
    /// - Keep validation logic fast and avoid expensive operations
    /// - Cache validation results if the same parameters are validated repeatedly
    /// - Use early return patterns to fail fast on invalid parameters
    /// - Avoid I/O operations, network calls, or other side effects during validation
    /// 
    /// Error Reporting:
    /// - This method only returns true/false; detailed error messages should be provided
    ///   in ExecuteAsync when validation fails
    /// - Consider logging validation failures for debugging purposes
    /// - Provide clear error messages to help users correct parameter issues
    /// 
    /// Thread Safety:
    /// - Validation should be thread-safe and stateless
    /// - Multiple threads may call this method concurrently
    /// - Do not modify any shared state during validation
    /// </remarks>
    bool ValidateParameters(Dictionary<string, object> parameters);

    /// <summary>
    /// Gets the parameter schema definition that describes the expected parameters for this tool.
    /// 
    /// The parameter schema serves as a contract that defines what parameters the tool accepts,
    /// their expected types, and provides the foundation for parameter validation, documentation
    /// generation, and IDE support. This schema enables tools to be self-documenting and
    /// supports automatic parameter validation and type checking.
    /// 
    /// Schema Design Principles:
    /// - **Completeness**: Include all parameters the tool accepts, both required and optional
    /// - **Type Safety**: Specify exact types to enable compile-time and runtime validation
    /// - **Consistency**: Use consistent naming conventions across all tools
    /// - **Extensibility**: Design schemas to support future parameter additions
    /// - **Documentation**: Parameter names should be self-descriptive
    /// 
    /// The schema is used by:
    /// - Tool registries for automatic parameter validation
    /// - Development tools for IntelliSense and code completion
    /// - Documentation generators for API reference material
    /// - Testing frameworks for automated test case generation
    /// - Agent frameworks for parameter type checking and conversion
    /// </summary>
    /// <returns>
    /// A dictionary where keys represent parameter names and values represent the expected
    /// .NET types for those parameters. The dictionary should include all parameters that
    /// the tool accepts, including both required and optional parameters.
    /// </returns>
    /// <example>
    /// Parameter schema examples for different tool types:
    /// <code>
    /// // File system tool schema
    /// public Dictionary&lt;string, Type&gt; GetParameterSchema()
    /// {
    ///     return new Dictionary&lt;string, Type&gt;
    ///     {
    ///         ["filename"] = typeof(string),           // Required: file to read
    ///         ["encoding"] = typeof(string),           // Optional: encoding type
    ///         ["max_size"] = typeof(int),              // Optional: maximum file size
    ///         ["include_metadata"] = typeof(bool)      // Optional: include file info
    ///     };
    /// }
    /// 
    /// // HTTP client tool schema
    /// public Dictionary&lt;string, Type&gt; GetParameterSchema()
    /// {
    ///     return new Dictionary&lt;string, Type&gt;
    ///     {
    ///         ["url"] = typeof(string),                           // Required: target URL
    ///         ["method"] = typeof(string),                        // Optional: HTTP method
    ///         ["headers"] = typeof(Dictionary&lt;string, string&gt;),   // Optional: HTTP headers
    ///         ["body"] = typeof(string),                          // Optional: request body
    ///         ["timeout_seconds"] = typeof(int),                 // Optional: timeout value
    ///         ["follow_redirects"] = typeof(bool)                // Optional: redirect behavior
    ///     };
    /// }
    /// 
    /// // Database query tool schema
    /// public Dictionary&lt;string, Type&gt; GetParameterSchema()
    /// {
    ///     return new Dictionary&lt;string, Type&gt;
    ///     {
    ///         ["query"] = typeof(string),                        // Required: SQL query
    ///         ["parameters"] = typeof(Dictionary&lt;string, object&gt;), // Optional: query parameters
    ///         ["connection_name"] = typeof(string),              // Optional: named connection
    ///         ["timeout_seconds"] = typeof(int),                 // Optional: query timeout
    ///         ["return_metadata"] = typeof(bool)                 // Optional: include column info
    ///     };
    /// }
    /// 
    /// // Text processing tool schema
    /// public Dictionary&lt;string, Type&gt; GetParameterSchema()
    /// {
    ///     return new Dictionary&lt;string, Type&gt;
    ///     {
    ///         ["text"] = typeof(string),              // Required: input text
    ///         ["operation"] = typeof(string),         // Required: operation type
    ///         ["options"] = typeof(Dictionary&lt;string, object&gt;), // Optional: operation options
    ///         ["preserve_formatting"] = typeof(bool)  // Optional: formatting behavior
    ///     };
    /// }
    /// </code>
    /// 
    /// Complex parameter types and nested structures:
    /// <code>
    /// // Tool with complex parameter types
    /// public Dictionary&lt;string, Type&gt; GetParameterSchema()
    /// {
    ///     return new Dictionary&lt;string, Type&gt;
    ///     {
    ///         ["items"] = typeof(List&lt;string&gt;),                    // List parameter
    ///         ["configuration"] = typeof(Dictionary&lt;string, object&gt;), // Nested config
    ///         ["filters"] = typeof(string[]),                     // Array parameter
    ///         ["callback_url"] = typeof(Uri),                     // Specific type
    ///         ["execution_date"] = typeof(DateTime),              // Date/time parameter
    ///         ["retry_policy"] = typeof(RetryPolicy)              // Custom type
    ///     };
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Best Practices:
    /// - Use descriptive parameter names that clearly indicate their purpose
    /// - Prefer specific types over generic object types when possible
    /// - Include both required and optional parameters in the schema
    /// - Use consistent naming conventions across all tools (snake_case recommended)
    /// - Document complex parameter types through tool documentation
    /// 
    /// Type Considerations:
    /// - Use primitive types (string, int, bool) for simple parameters
    /// - Use generic collections (Dictionary, List) for complex data structures
    /// - Consider using custom types for domain-specific parameter objects
    /// - Support nullable types for optional parameters where appropriate
    /// 
    /// Schema Evolution:
    /// - Adding new optional parameters should not break existing implementations
    /// - Removing parameters or changing types constitutes a breaking change
    /// - Version your tools appropriately when making schema changes
    /// - Maintain backward compatibility when possible
    /// 
    /// Integration Points:
    /// - Tool registries use schemas for automatic validation and documentation
    /// - IDE extensions can provide IntelliSense based on parameter schemas
    /// - Testing frameworks can generate test cases using schema information
    /// - Documentation tools can automatically generate parameter reference guides
    /// </remarks>
    Dictionary<string, Type> GetParameterSchema();
}