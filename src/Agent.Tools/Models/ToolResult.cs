namespace Agent.Tools.Models;

/// <summary>
/// Represents the result of a tool execution in the GenericAgents framework, providing standardized
/// success/failure information, output data, error details, and extensible metadata for comprehensive
/// tool operation reporting and analysis.
/// 
/// This class serves as the unified return type for all tool operations, ensuring consistent result
/// handling across the entire framework. It supports both successful operations with data output
/// and error scenarios with detailed diagnostic information, enabling robust error handling and
/// comprehensive system monitoring.
/// 
/// <para><strong>Result Pattern Implementation:</strong></para>
/// ToolResult implements the Result pattern to eliminate exceptions as control flow and provide
/// explicit success/failure semantics that agents and orchestrators can reliably process. This
/// approach ensures predictable behavior and comprehensive error information without throwing exceptions.
/// 
/// <para><strong>Metadata Extensibility:</strong></para>
/// The metadata dictionary allows tools to provide additional context such as performance metrics,
/// processing details, version information, and custom diagnostic data that enhances observability
/// and debugging capabilities in production environments.
/// </summary>
/// <remarks>
/// <para><strong>Design Principles:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Explicit Status</strong>: Success/failure state is always explicitly indicated</description></item>
/// <item><description><strong>Rich Context</strong>: Comprehensive metadata support for operational insights</description></item>
/// <item><description><strong>Type Safety</strong>: Strongly-typed approach to result handling</description></item>
/// <item><description><strong>Immutable Construction</strong>: Results are created through factory methods</description></item>
/// <item><description><strong>Extensible Metadata</strong>: Support for tool-specific diagnostic information</description></item>
/// </list>
/// 
/// <para><strong>Usage Patterns:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Success Results</strong>: Include output data and optional performance metadata</description></item>
/// <item><description><strong>Error Results</strong>: Provide descriptive error messages with diagnostic metadata</description></item>
/// <item><description><strong>Chain Operations</strong>: Results can be chained together in tool pipelines</description></item>
/// <item><description><strong>Monitoring Integration</strong>: Metadata supports comprehensive operational monitoring</description></item>
/// </list>
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// - Metadata dictionary is pre-allocated to minimize memory allocation overhead
/// - Object references are nullable to support efficient memory usage for empty results
/// - Factory methods provide optimized construction patterns for common scenarios
/// 
/// <para><strong>Thread Safety:</strong></para>
/// ToolResult instances are thread-safe for read operations once constructed. However, the metadata
/// dictionary should not be modified concurrently from multiple threads after construction.
/// </remarks>
/// <example>
/// <code>
/// // Creating successful results with data and metadata
/// public async Task&lt;ToolResult&gt; ProcessTextAsync(string input)
/// {
///     var startTime = DateTime.UtcNow;
///     var processedText = input.ToUpperInvariant();
///     var processingTime = DateTime.UtcNow - startTime;
///     
///     return ToolResult.CreateSuccess(processedText, new Dictionary&lt;string, object&gt;
///     {
///         ["originalLength"] = input.Length,
///         ["processedLength"] = processedText.Length,
///         ["processingTimeMs"] = processingTime.TotalMilliseconds,
///         ["operation"] = "uppercase",
///         ["timestamp"] = DateTime.UtcNow
///     });
/// }
/// 
/// // Creating error results with diagnostic information
/// public async Task&lt;ToolResult&gt; ValidateDataAsync(object data)
/// {
///     if (data == null)
///     {
///         return ToolResult.CreateError("Input data cannot be null", new Dictionary&lt;string, object&gt;
///         {
///             ["validationRule"] = "NotNull",
///             ["timestamp"] = DateTime.UtcNow,
///             ["severity"] = "Critical"
///         });
///     }
///     
///     // Validation logic...
///     return ToolResult.CreateSuccess(data);
/// }
/// 
/// // Chaining tool operations with result checking
/// public async Task&lt;ToolResult&gt; ProcessPipelineAsync(string input)
/// {
///     var step1 = await _textProcessor.ExecuteAsync(new() { ["input"] = input });
///     if (!step1.IsSuccess)
///     {
///         return ToolResult.CreateError($"Step 1 failed: {step1.ErrorMessage}");
///     }
///     
///     var step2 = await _validator.ExecuteAsync(new() { ["data"] = step1.Data });
///     if (!step2.IsSuccess)
///     {
///         return ToolResult.CreateError($"Step 2 failed: {step2.ErrorMessage}");
///     }
///     
///     var step3 = await _formatter.ExecuteAsync(new() { ["validated"] = step2.Data });
///     return step3; // Return final result
/// }
/// 
/// // Result analysis and monitoring
/// public void AnalyzeToolResult(ToolResult result)
/// {
///     if (result.IsSuccess)
///     {
///         Console.WriteLine($"Operation succeeded: {result.Output}");
///         
///         // Extract performance metrics from metadata
///         if (result.Metadata.ContainsKey("processingTimeMs"))
///         {
///             var processingTime = result.Metadata["processingTimeMs"];
///             _metrics.RecordProcessingTime("tool_execution", (double)processingTime);
///         }
///         
///         // Log additional metadata for monitoring
///         foreach (var kvp in result.Metadata)
///         {
///             _logger.LogInformation("Tool metadata: {Key} = {Value}", kvp.Key, kvp.Value);
///         }
///     }
///     else
///     {
///         Console.WriteLine($"Operation failed: {result.ErrorMessage}");
///         
///         // Extract error context from metadata
///         if (result.Metadata.ContainsKey("severity"))
///         {
///             var severity = result.Metadata["severity"]?.ToString();
///             if (severity == "Critical")
///             {
///                 _alerting.SendCriticalAlert($"Tool execution failed: {result.ErrorMessage}");
///             }
///         }
///     }
/// }
/// 
/// // Complex result with rich metadata
/// public async Task&lt;ToolResult&gt; AnalyzeFileAsync(string filePath)
/// {
///     try
///     {
///         var fileInfo = new FileInfo(filePath);
///         var content = await File.ReadAllTextAsync(filePath);
///         var analysis = PerformAnalysis(content);
///         
///         return ToolResult.CreateSuccess(analysis, new Dictionary&lt;string, object&gt;
///         {
///             ["filePath"] = filePath,
///             ["fileSize"] = fileInfo.Length,
///             ["lastModified"] = fileInfo.LastWriteTime,
///             ["lineCount"] = content.Split('\n').Length,
///             ["characterCount"] = content.Length,
///             ["analysisType"] = "comprehensive",
///             ["confidence"] = 0.95,
///             ["processingNode"] = Environment.MachineName,
///             ["toolVersion"] = "1.2.0"
///         });
///     }
///     catch (FileNotFoundException)
///     {
///         return ToolResult.CreateError($"File not found: {filePath}", new Dictionary&lt;string, object&gt;
///         {
///             ["filePath"] = filePath,
///             ["errorType"] = "FileNotFound",
///             ["suggestion"] = "Verify file path and permissions",
///             ["timestamp"] = DateTime.UtcNow
///         });
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="ITool"/>
/// <seealso cref="BaseTool"/>
public class ToolResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the tool execution completed successfully.
    /// 
    /// This property provides explicit success/failure semantics that eliminate ambiguity
    /// about operation outcomes. When <c>true</c>, the operation completed successfully
    /// and the <see cref="Data"/> property contains the result. When <c>false</c>, the
    /// operation failed and the <see cref="ErrorMessage"/> property contains error details.
    /// </summary>
    /// <value>
    /// <c>true</c> if the tool execution succeeded; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para><strong>Result Pattern Implementation:</strong></para>
    /// This property is the cornerstone of the Result pattern implementation, providing
    /// explicit success/failure indication that agents and orchestrators can reliably check
    /// before accessing result data or error information.
    /// 
    /// <para><strong>Usage Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Always check this property before accessing <see cref="Data"/> or <see cref="ErrorMessage"/></description></item>
    /// <item><description>Use this for conditional logic in tool pipelines and workflows</description></item>
    /// <item><description>Consider this property in monitoring and alerting logic</description></item>
    /// </list>
    /// 
    /// <para><strong>Thread Safety:</strong></para>
    /// This property is thread-safe for read operations once the result is constructed.
    /// Modification should only occur during result construction.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Proper success checking pattern
    /// var result = await tool.ExecuteAsync(parameters);
    /// 
    /// if (result.IsSuccess)
    /// {
    ///     // Safe to access Data property
    ///     Console.WriteLine($"Operation succeeded: {result.Data}");
    ///     ProcessSuccessfulResult(result.Data);
    /// }
    /// else
    /// {
    ///     // Safe to access ErrorMessage property
    ///     Console.WriteLine($"Operation failed: {result.ErrorMessage}");
    ///     HandleError(result.ErrorMessage);
    /// }
    /// 
    /// // Pipeline processing with success checks
    /// var results = new List&lt;ToolResult&gt;();
    /// 
    /// foreach (var tool in toolPipeline)
    /// {
    ///     var stepResult = await tool.ExecuteAsync(parameters);
    ///     results.Add(stepResult);
    ///     
    ///     if (!stepResult.IsSuccess)
    ///     {
    ///         _logger.LogError("Pipeline failed at tool {ToolName}: {ErrorMessage}", 
    ///                          tool.Name, stepResult.ErrorMessage);
    ///         break; // Stop pipeline on first failure
    ///     }
    /// }
    /// </code>
    /// </example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the output data produced by successful tool execution.
    /// 
    /// This property contains the actual result data when <see cref="IsSuccess"/> is <c>true</c>.
    /// The data can be of any type, allowing tools maximum flexibility in returning appropriate
    /// result types such as strings, complex objects, collections, or computed results.
    /// When <see cref="IsSuccess"/> is <c>false</c>, this property should be <c>null</c>.
    /// </summary>
    /// <value>
    /// The output data from tool execution, or <c>null</c> if the operation failed or produced no output.
    /// The type depends on the specific tool implementation and operation performed.
    /// </value>
    /// <remarks>
    /// <para><strong>Type Flexibility:</strong></para>
    /// This property uses <c>object?</c> type to accommodate the wide variety of data types
    /// that different tools may produce, from simple strings to complex domain objects,
    /// collections, or computed analytical results.
    /// 
    /// <para><strong>Access Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Always check <see cref="IsSuccess"/> before accessing this property</description></item>
    /// <item><description>Use appropriate casting or pattern matching for strongly-typed access</description></item>
    /// <item><description>Handle null values appropriately even in success scenarios</description></item>
    /// <item><description>Consider using the <see cref="Output"/> property for string representation</description></item>
    /// </list>
    /// 
    /// <para><strong>Common Data Types:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Simple Values</strong>: strings, numbers, booleans for basic operations</description></item>
    /// <item><description><strong>Collections</strong>: arrays, lists, dictionaries for bulk operations</description></item>
    /// <item><description><strong>Complex Objects</strong>: domain models, analysis results, structured data</description></item>
    /// <item><description><strong>Binary Data</strong>: byte arrays, streams for file operations</description></item>
    /// </list>
    /// 
    /// <para><strong>Memory Considerations:</strong></para>
    /// Large data objects should be handled carefully to avoid memory pressure. Consider
    /// streaming or pagination patterns for large result sets.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Accessing different data types safely
    /// var textResult = await textProcessor.ExecuteAsync(parameters);
    /// if (textResult.IsSuccess &amp;&amp; textResult.Data is string processedText)
    /// {
    ///     Console.WriteLine($"Processed text: {processedText}");
    /// }
    /// 
    /// var queryResult = await databaseTool.ExecuteAsync(parameters);
    /// if (queryResult.IsSuccess &amp;&amp; queryResult.Data is List&lt;Dictionary&lt;string, object&gt;&gt; rows)
    /// {
    ///     Console.WriteLine($"Retrieved {rows.Count} rows");
    ///     foreach (var row in rows)
    ///     {
    ///         // Process each row...
    ///     }
    /// }
    /// 
    /// var analysisResult = await analyzerTool.ExecuteAsync(parameters);
    /// if (analysisResult.IsSuccess)
    /// {
    ///     // Use pattern matching for complex types
    ///     switch (analysisResult.Data)
    ///     {
    ///         case AnalysisReport report:
    ///             DisplayReport(report);
    ///             break;
    ///         case Dictionary&lt;string, object&gt; metrics:
    ///             DisplayMetrics(metrics);
    ///             break;
    ///         case null:
    ///             Console.WriteLine("Analysis completed with no data");
    ///             break;
    ///         default:
    ///             Console.WriteLine($"Unexpected result type: {analysisResult.Data.GetType()}");
    ///             break;
    ///     }
    /// }
    /// </code>
    /// </example>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message describing why the tool execution failed.
    /// 
    /// This property contains detailed error information when <see cref="IsSuccess"/> is <c>false</c>.
    /// The error message should be descriptive enough for debugging and user feedback while
    /// being sanitized to prevent information leakage in production environments. When
    /// <see cref="IsSuccess"/> is <c>true</c>, this property should be <c>null</c>.
    /// </summary>
    /// <value>
    /// A descriptive error message explaining the failure cause, or <c>null</c> if the operation succeeded.
    /// The message should be human-readable and provide actionable information when possible.
    /// </value>
    /// <remarks>
    /// <para><strong>Error Message Quality:</strong></para>
    /// Effective error messages should be:
    /// <list type="bullet">
    /// <item><description><strong>Descriptive</strong>: Clearly explain what went wrong</description></item>
    /// <item><description><strong>Actionable</strong>: Suggest possible solutions when appropriate</description></item>
    /// <item><description><strong>Safe</strong>: Avoid exposing sensitive information or stack traces</description></item>
    /// <item><description><strong>Consistent</strong>: Follow established formatting and terminology</description></item>
    /// </list>
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// Error messages should be sanitized in production to prevent information disclosure.
    /// Detailed technical information should be logged separately and not exposed through
    /// error messages that may reach end users or external systems.
    /// 
    /// <para><strong>Localization Support:</strong></para>
    /// For internationalized applications, error messages should support localization
    /// through resource files or cultural formatting patterns.
    /// 
    /// <para><strong>Error Categories:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Validation Errors</strong>: Invalid parameters or input data</description></item>
    /// <item><description><strong>System Errors</strong>: Infrastructure or connectivity issues</description></item>
    /// <item><description><strong>Business Logic Errors</strong>: Domain-specific constraint violations</description></item>
    /// <item><description><strong>Resource Errors</strong>: File not found, insufficient permissions, etc.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Examples of well-formed error messages
    /// 
    /// // Parameter validation error
    /// if (string.IsNullOrEmpty(filePath))
    /// {
    ///     return ToolResult.CreateError("File path cannot be null or empty. Please provide a valid file path.");
    /// }
    /// 
    /// // System connectivity error
    /// catch (HttpRequestException ex)
    /// {
    ///     return ToolResult.CreateError("Failed to connect to remote service. Please check network connectivity and service availability.");
    /// }
    /// 
    /// // Business logic error
    /// if (account.Balance &lt; transferAmount)
    /// {
    ///     return ToolResult.CreateError($"Insufficient funds. Account balance ({account.Balance:C}) is less than transfer amount ({transferAmount:C}).");
    /// }
    /// 
    /// // Resource error with suggestion
    /// catch (FileNotFoundException)
    /// {
    ///     return ToolResult.CreateError($"File not found: '{filePath}'. Verify the file exists and you have read permissions.");
    /// }
    /// 
    /// // Handling error results in application logic
    /// var result = await tool.ExecuteAsync(parameters);
    /// if (!result.IsSuccess)
    /// {
    ///     // Log detailed error for debugging
    ///     _logger.LogError("Tool {ToolName} failed: {ErrorMessage}", tool.Name, result.ErrorMessage);
    ///     
    ///     // Return user-friendly message
    ///     return new ApiResponse
    ///     {
    ///         Success = false,
    ///         Message = "Operation failed. Please try again or contact support if the problem persists.",
    ///         ErrorCode = "TOOL_EXECUTION_FAILED"
    ///     };
    /// }
    /// </code>
    /// </example>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata associated with the tool execution result.
    /// 
    /// This dictionary provides extensible storage for tool-specific information such as
    /// performance metrics, processing details, version information, diagnostic data,
    /// and other contextual information that enhances observability, debugging, and
    /// monitoring capabilities in production environments.
    /// </summary>
    /// <value>
    /// A dictionary containing key-value pairs of metadata information. The dictionary
    /// is never null and is initialized to an empty dictionary if not explicitly provided.
    /// Keys should be descriptive strings, and values can be any serializable object type.
    /// </value>
    /// <remarks>
    /// <para><strong>Common Metadata Categories:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Performance Metrics</strong>: execution time, memory usage, processing counts</description></item>
    /// <item><description><strong>Processing Details</strong>: algorithm versions, configuration settings, processing stages</description></item>
    /// <item><description><strong>System Information</strong>: server names, process IDs, thread information</description></item>
    /// <item><description><strong>Version Data</strong>: tool versions, dependency versions, schema versions</description></item>
    /// <item><description><strong>Diagnostic Information</strong>: debug flags, trace identifiers, correlation IDs</description></item>
    /// </list>
    /// 
    /// <para><strong>Usage Patterns:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Monitoring</strong>: Collect metrics for performance dashboards and alerting</description></item>
    /// <item><description><strong>Debugging</strong>: Provide diagnostic context for troubleshooting</description></item>
    /// <item><description><strong>Auditing</strong>: Track operation details for compliance and analysis</description></item>
    /// <item><description><strong>Optimization</strong>: Gather performance data for system tuning</description></item>
    /// </list>
    /// 
    /// <para><strong>Naming Conventions:</strong></para>
    /// Use descriptive, camelCase keys that clearly indicate the metadata purpose:
    /// <list type="bullet">
    /// <item><description>Performance: "processingTimeMs", "memoryUsageBytes", "itemsProcessed"</description></item>
    /// <item><description>Versioning: "toolVersion", "schemaVersion", "apiVersion"</description></item>
    /// <item><description>Context: "correlationId", "requestId", "sessionId"</description></item>
    /// <item><description>Environment: "processingNode", "environment", "region"</description></item>
    /// </list>
    /// 
    /// <para><strong>Serialization Compatibility:</strong></para>
    /// Metadata values should be JSON-serializable for compatibility with logging,
    /// monitoring, and persistence systems. Avoid complex object graphs or circular references.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creating results with comprehensive metadata
    /// public async Task&lt;ToolResult&gt; ProcessLargeDatasetAsync(string dataPath)
    /// {
    ///     var stopwatch = Stopwatch.StartNew();
    ///     var startMemory = GC.GetTotalMemory(false);
    ///     
    ///     try
    ///     {
    ///         var data = await LoadDataAsync(dataPath);
    ///         var results = await ProcessDataAsync(data);
    ///         
    ///         stopwatch.Stop();
    ///         var endMemory = GC.GetTotalMemory(false);
    ///         
    ///         return ToolResult.CreateSuccess(results, new Dictionary&lt;string, object&gt;
    ///         {
    ///             // Performance metrics
    ///             ["processingTimeMs"] = stopwatch.ElapsedMilliseconds,
    ///             ["memoryUsageBytes"] = endMemory - startMemory,
    ///             ["recordsProcessed"] = results.Count,
    ///             ["averageTimePerRecord"] = stopwatch.ElapsedMilliseconds / (double)results.Count,
    ///             
    ///             // Processing details
    ///             ["algorithm"] = "enhanced-processing-v2",
    ///             ["batchSize"] = 1000,
    ///             ["parallelThreads"] = Environment.ProcessorCount,
    ///             
    ///             // System context
    ///             ["processingNode"] = Environment.MachineName,
    ///             ["processId"] = Environment.ProcessId,
    ///             ["timestamp"] = DateTime.UtcNow,
    ///             
    ///             // Version information
    ///             ["toolVersion"] = GetType().Assembly.GetName().Version?.ToString(),
    ///             ["frameworkVersion"] = Environment.Version.ToString()
    ///         });
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         stopwatch.Stop();
    ///         
    ///         return ToolResult.CreateError("Data processing failed", new Dictionary&lt;string, object&gt;
    ///         {
    ///             ["errorType"] = ex.GetType().Name,
    ///             ["processingTimeMs"] = stopwatch.ElapsedMilliseconds,
    ///             ["failureStage"] = "data-processing",
    ///             ["timestamp"] = DateTime.UtcNow
    ///         });
    ///     }
    /// }
    /// 
    /// // Consuming metadata for monitoring
    /// public void ProcessToolResult(ToolResult result)
    /// {
    ///     // Extract performance metrics
    ///     if (result.Metadata.TryGetValue("processingTimeMs", out var timeValue))
    ///     {
    ///         var processingTime = Convert.ToDouble(timeValue);
    ///         _performanceMonitor.RecordMetric("tool_processing_time", processingTime);
    ///         
    ///         if (processingTime &gt; 5000) // Alert if processing takes more than 5 seconds
    ///         {
    ///             _alertService.SendAlert($"Slow tool execution detected: {processingTime}ms");
    ///         }
    ///     }
    ///     
    ///     // Extract processing details for analysis
    ///     if (result.Metadata.TryGetValue("recordsProcessed", out var recordsValue))
    ///     {
    ///         var recordCount = Convert.ToInt32(recordsValue);
    ///         _analyticsService.TrackThroughput("tool_records_processed", recordCount);
    ///     }
    ///     
    ///     // Log comprehensive metadata for debugging
    ///     _logger.LogInformation("Tool execution completed with metadata: {@Metadata}", result.Metadata);
    /// }
    /// </code>
    /// </example>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the output data as a string representation for convenient display and logging purposes.
    /// 
    /// This property provides a convenient way to access the result data as a string without
    /// explicit type casting. It returns the string representation of the <see cref="Data"/>
    /// property using the object's <c>ToString()</c> method, or <c>null</c> if <see cref="Data"/> is null.
    /// </summary>
    /// <value>
    /// The string representation of <see cref="Data"/>, or <c>null</c> if <see cref="Data"/> is null.
    /// For complex objects, this returns the result of their <c>ToString()</c> implementation.
    /// </value>
    /// <remarks>
    /// <para><strong>Convenience Property:</strong></para>
    /// This property is provided for scenarios where a simple string representation of the
    /// result data is needed, such as logging, debugging, or simple display operations.
    /// 
    /// <para><strong>Usage Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Use for logging and debugging scenarios where string representation is sufficient</description></item>
    /// <item><description>Avoid for scenarios requiring strongly-typed access to result data</description></item>
    /// <item><description>Consider that complex objects may not have meaningful ToString() implementations</description></item>
    /// <item><description>Always check <see cref="Success"/> before using this property</description></item>
    /// </list>
    /// 
    /// <para><strong>Formatting Considerations:</strong></para>
    /// The quality of the string output depends on the ToString() implementation of the
    /// underlying data type. Complex objects may require custom formatting or serialization
    /// for meaningful string representation.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using Output property for simple display
    /// var result = await textTool.ExecuteAsync(parameters);
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine($"Tool output: {result.Output}");
    ///     _logger.LogInformation("Processing completed: {Output}", result.Output);
    /// }
    /// 
    /// // Different data types and their string representations
    /// var stringResult = ToolResult.CreateSuccess("Hello World");
    /// Console.WriteLine(stringResult.Output); // "Hello World"
    /// 
    /// var numberResult = ToolResult.CreateSuccess(42);
    /// Console.WriteLine(numberResult.Output); // "42"
    /// 
    /// var listResult = ToolResult.CreateSuccess(new List&lt;int&gt; { 1, 2, 3 });
    /// Console.WriteLine(listResult.Output); // "System.Collections.Generic.List`1[System.Int32]"
    /// 
    /// var customObjectResult = ToolResult.CreateSuccess(new { Name = "Test", Value = 123 });
    /// Console.WriteLine(customObjectResult.Output); // "{ Name = Test, Value = 123 }" (anonymous type)
    /// 
    /// // For better formatting of complex objects, consider JSON serialization
    /// var complexResult = await analysisTool.ExecuteAsync(parameters);
    /// if (complexResult.IsSuccess &amp;&amp; complexResult.Data != null)
    /// {
    ///     var formattedOutput = JsonSerializer.Serialize(complexResult.Data, new JsonSerializerOptions 
    ///     { 
    ///         WriteIndented = true 
    ///     });
    ///     Console.WriteLine($"Analysis result:\n{formattedOutput}");
    /// }
    /// </code>
    /// </example>
    /// <summary>
    /// Gets the output data as a string representation. This is a convenience property for backward compatibility.
    /// </summary>
    /// <value>The string representation of the Data property, or null if Data is null.</value>
    /// <remarks>
    /// This property exists for backward compatibility and returns Data?.ToString(). 
    /// For direct access to result data, use the Data property instead.
    /// </remarks>
    [Obsolete("Use Data property for direct access to result data. This property is maintained for backward compatibility only.")]
    public string? Output => Data?.ToString();

    /// <summary>
    /// Gets or sets a value indicating whether the tool execution completed successfully.
    /// This is an alias for the IsSuccess property maintained for backward compatibility.
    /// </summary>
    /// <value>The same value as the IsSuccess property.</value>
    /// <remarks>
    /// This property exists solely for backward compatibility. New code should use the IsSuccess property directly.
    /// Both properties reference the same underlying field, so changes to either will affect both.
    /// </remarks>
    [Obsolete("Use IsSuccess property instead. This alias is maintained for backward compatibility only.")]
    public bool Success
    {
        get => IsSuccess;
        set => IsSuccess = value;
    }

    /// <summary>
    /// Gets or sets the error message. This is an alias for the ErrorMessage property
    /// maintained for backward compatibility with existing code.
    /// </summary>
    /// <value>The same value as the ErrorMessage property.</value>
    /// <remarks>
    /// This property exists solely for backward compatibility. New code should use the ErrorMessage property directly.
    /// Both properties reference the same underlying field, so changes to either will affect both.
    /// </remarks>
    [Obsolete("Use ErrorMessage property instead. This alias is maintained for backward compatibility only.")]
    public string? Error
    {
        get => ErrorMessage;
        set => ErrorMessage = value;
    }

    /// <summary>
    /// Creates a new <see cref="ToolResult"/> instance representing a successful tool execution.
    /// 
    /// This factory method constructs a result object with <see cref="Success"/> set to <c>true</c>
    /// and optionally includes output data and metadata. This is the preferred way to create
    /// successful tool results as it ensures consistent initialization and proper result semantics.
    /// </summary>
    /// <param name="data">
    /// The output data produced by the tool execution. Can be any type including strings,
    /// complex objects, collections, or computed results. May be <c>null</c> if the operation
    /// succeeded but produced no output data.
    /// </param>
    /// <param name="metadata">
    /// Optional metadata dictionary containing additional information about the execution
    /// such as performance metrics, processing details, or diagnostic data. If <c>null</c>,
    /// an empty dictionary will be assigned to ensure the <see cref="Metadata"/> property is never null.
    /// </param>
    /// <returns>
    /// A new <see cref="ToolResult"/> instance with <see cref="Success"/> set to <c>true</c>,
    /// the specified data and metadata, and <see cref="Error"/> set to <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para><strong>Factory Method Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Consistency</strong>: Ensures all successful results follow the same construction pattern</description></item>
    /// <item><description><strong>Validation</strong>: Guarantees proper initialization of all properties</description></item>
    /// <item><description><strong>Immutability</strong>: Promotes immutable result construction practices</description></item>
    /// <item><description><strong>Null Safety</strong>: Handles null metadata gracefully by providing default empty dictionary</description></item>
    /// </list>
    /// 
    /// <para><strong>Data Type Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Simple Types</strong>: Use for primitive values, strings, or basic computations</description></item>
    /// <item><description><strong>Complex Objects</strong>: Use for structured data, domain models, or analysis results</description></item>
    /// <item><description><strong>Collections</strong>: Use for bulk operations, query results, or batch processing outputs</description></item>
    /// <item><description><strong>Null Data</strong>: Acceptable for operations that succeed but produce no output</description></item>
    /// </list>
    /// 
    /// <para><strong>Metadata Best Practices:</strong></para>
    /// Include relevant operational information to enhance observability and debugging:
    /// <list type="bullet">
    /// <item><description>Performance metrics (execution time, memory usage, throughput)</description></item>
    /// <item><description>Processing details (algorithms used, configuration settings)</description></item>
    /// <item><description>Version information (tool version, schema version)</description></item>
    /// <item><description>System context (processing node, correlation IDs)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple success result with string data
    /// var textResult = ToolResult.CreateSuccess("Hello, World!");
    /// 
    /// // Success result with complex object and metadata
    /// var analysisResult = ToolResult.CreateSuccess(
    ///     new AnalysisReport 
    ///     { 
    ///         Score = 0.95, 
    ///         Summary = "High confidence analysis" 
    ///     },
    ///     new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["processingTimeMs"] = 125,
    ///         ["confidence"] = 0.95,
    ///         ["algorithm"] = "advanced-nlp-v2",
    ///         ["timestamp"] = DateTime.UtcNow
    ///     }
    /// );
    /// 
    /// // Success result with collection data
    /// var queryResults = ToolResult.CreateSuccess(
    ///     new List&lt;Customer&gt; { customer1, customer2, customer3 },
    ///     new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["queryExecutionTimeMs"] = 45,
    ///         ["recordCount"] = 3,
    ///         ["totalResultsAvailable"] = 150
    ///     }
    /// );
    /// 
    /// // Success result with no data but important metadata
    /// var operationResult = ToolResult.CreateSuccess(
    ///     data: null,  // Operation succeeded but has no return data
    ///     metadata: new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["recordsUpdated"] = 42,
    ///         ["operationType"] = "batch-update",
    ///         ["transactionId"] = "tx_12345"
    ///     }
    /// );
    /// 
    /// // Comprehensive result for monitoring and analysis
    /// public async Task&lt;ToolResult&gt; ProcessDocumentAsync(byte[] documentData)
    /// {
    ///     var startTime = DateTime.UtcNow;
    ///     var stopwatch = Stopwatch.StartNew();
    ///     
    ///     try
    ///     {
    ///         var processedDocument = await ProcessDocumentInternalAsync(documentData);
    ///         stopwatch.Stop();
    ///         
    ///         return ToolResult.CreateSuccess(processedDocument, new Dictionary&lt;string, object&gt;
    ///         {
    ///             // Performance metrics
    ///             ["processingTimeMs"] = stopwatch.ElapsedMilliseconds,
    ///             ["inputSizeBytes"] = documentData.Length,
    ///             ["outputSizeBytes"] = processedDocument.Content?.Length ?? 0,
    ///             
    ///             // Processing details
    ///             ["documentType"] = processedDocument.Type,
    ///             ["pageCount"] = processedDocument.PageCount,
    ///             ["extractedImages"] = processedDocument.Images.Count,
    ///             
    ///             // Quality metrics
    ///             ["ocrConfidence"] = processedDocument.OcrConfidence,
    ///             ["processingQuality"] = "high",
    ///             
    ///             // System context
    ///             ["processingStartTime"] = startTime,
    ///             ["processingEndTime"] = DateTime.UtcNow,
    ///             ["processingNode"] = Environment.MachineName,
    ///             ["toolVersion"] = "1.3.0"
    ///         });
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         // Even in error handling, we might use this method for partial success scenarios
    ///         return ToolResult.CreateError($"Document processing failed: {ex.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="CreateError"/>
    /// <seealso cref="Success"/>
    /// <seealso cref="Data"/>
    /// <seealso cref="Metadata"/>
    public static ToolResult CreateSuccess(object? data = null, Dictionary<string, object>? metadata = null)
    {
        return new ToolResult
        {
            IsSuccess = true,
            Data = data,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates a new <see cref="ToolResult"/> instance representing a failed tool execution.
    /// 
    /// This factory method constructs a result object with <see cref="Success"/> set to <c>false</c>
    /// and includes a descriptive error message explaining the failure. Optionally includes
    /// metadata with diagnostic information to aid in troubleshooting and monitoring.
    /// This is the preferred way to create error results as it ensures consistent initialization
    /// and proper error semantics.
    /// </summary>
    /// <param name="error">
    /// A descriptive error message explaining why the tool execution failed. Should be
    /// human-readable and provide actionable information when possible. Must not be null
    /// or empty to ensure meaningful error reporting.
    /// </param>
    /// <param name="metadata">
    /// Optional metadata dictionary containing diagnostic information about the failure
    /// such as error codes, failure stages, system context, or troubleshooting hints.
    /// If <c>null</c>, an empty dictionary will be assigned to ensure the <see cref="Metadata"/>
    /// property is never null.
    /// </param>
    /// <returns>
    /// A new <see cref="ToolResult"/> instance with <see cref="Success"/> set to <c>false</c>,
    /// the specified error message, optional metadata, and <see cref="Data"/> set to <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para><strong>Error Message Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Descriptive</strong>: Clearly explain what went wrong and why</description></item>
    /// <item><description><strong>Actionable</strong>: Provide guidance on how to resolve the issue when possible</description></item>
    /// <item><description><strong>Safe</strong>: Avoid exposing sensitive information, stack traces, or internal details</description></item>
    /// <item><description><strong>Consistent</strong>: Follow established terminology and formatting patterns</description></item>
    /// <item><description><strong>User-Friendly</strong>: Use language appropriate for the intended audience</description></item>
    /// </list>
    /// 
    /// <para><strong>Diagnostic Metadata:</strong></para>
    /// Include relevant diagnostic information to aid in troubleshooting and system monitoring:
    /// <list type="bullet">
    /// <item><description>Error classification (validation, system, business logic, resource)</description></item>
    /// <item><description>Failure context (processing stage, operation type, system state)</description></item>
    /// <item><description>System information (node, process, timestamp, correlation IDs)</description></item>
    /// <item><description>Recovery suggestions (retry recommendations, alternative approaches)</description></item>
    /// </list>
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// Error messages should be sanitized to prevent information disclosure vulnerabilities.
    /// Detailed technical information should be logged separately and not included in user-facing
    /// error messages or results that may be transmitted to external systems.
    /// 
    /// <para><strong>Monitoring Integration:</strong></para>
    /// Error results with comprehensive metadata enable effective monitoring, alerting,
    /// and automated error analysis in production environments.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple error result with descriptive message
    /// var validationError = ToolResult.CreateError("Input parameter 'fileName' cannot be null or empty");
    /// 
    /// // Error result with diagnostic metadata
    /// var systemError = ToolResult.CreateError(
    ///     "Failed to connect to database server",
    ///     new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["errorType"] = "ConnectionTimeout",
    ///         ["serverAddress"] = "db.example.com",
    ///         ["timeoutSeconds"] = 30,
    ///         ["retryCount"] = 3,
    ///         ["timestamp"] = DateTime.UtcNow
    ///     }
    /// );
    /// 
    /// // Business logic error with context
    /// var businessError = ToolResult.CreateError(
    ///     "Cannot process order: insufficient inventory",
    ///     new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["errorCategory"] = "BusinessRule",
    ///         ["requiredQuantity"] = 50,
    ///         ["availableQuantity"] = 25,
    ///         ["productId"] = "PROD-12345",
    ///         ["suggestion"] = "Reduce quantity or check back later"
    ///     }
    /// );
    /// 
    /// // Resource error with recovery guidance
    /// var resourceError = ToolResult.CreateError(
    ///     "File access denied. Insufficient permissions to read the specified file.",
    ///     new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["errorType"] = "UnauthorizedAccess",
    ///         ["filePath"] = "/secure/data/report.pdf",
    ///         ["requiredPermission"] = "Read",
    ///         ["currentUser"] = "serviceaccount@domain.com",
    ///         ["suggestion"] = "Contact administrator to grant read permissions"
    ///     }
    /// );
    /// 
    /// // Comprehensive error handling in tool implementation
    /// public async Task&lt;ToolResult&gt; ProcessFileAsync(string filePath)
    /// {
    ///     var operationId = Guid.NewGuid().ToString();
    ///     var startTime = DateTime.UtcNow;
    ///     
    ///     try
    ///     {
    ///         if (string.IsNullOrWhiteSpace(filePath))
    ///         {
    ///             return ToolResult.CreateError(
    ///                 "File path is required and cannot be empty",
    ///                 new Dictionary&lt;string, object&gt;
    ///                 {
    ///                     ["validationRule"] = "Required",
    ///                     ["parameter"] = "filePath",
    ///                     ["operationId"] = operationId
    ///                 }
    ///             );
    ///         }
    ///         
    ///         if (!File.Exists(filePath))
    ///         {
    ///             return ToolResult.CreateError(
    ///                 $"File not found: {Path.GetFileName(filePath)}",
    ///                 new Dictionary&lt;string, object&gt;
    ///                 {
    ///                     ["errorType"] = "FileNotFound",
    ///                     ["filePath"] = filePath,
    ///                     ["checkedAt"] = DateTime.UtcNow,
    ///                     ["operationId"] = operationId,
    ///                     ["suggestion"] = "Verify file path and ensure file exists"
    ///                 }
    ///             );
    ///         }
    ///         
    ///         // Processing logic here...
    ///         return ToolResult.CreateSuccess(processedData);
    ///     }
    ///     catch (UnauthorizedAccessException)
    ///     {
    ///         return ToolResult.CreateError(
    ///             "Access denied. Insufficient permissions to access the file.",
    ///             new Dictionary&lt;string, object&gt;
    ///             {
    ///                 ["errorType"] = "UnauthorizedAccess",
    ///                 ["filePath"] = filePath,
    ///                 ["operationId"] = operationId,
    ///                 ["processingTime"] = (DateTime.UtcNow - startTime).TotalMilliseconds
    ///             }
    ///         );
    ///     }
    ///     catch (IOException ex)
    ///     {
    ///         return ToolResult.CreateError(
    ///             "File I/O operation failed. The file may be locked or corrupted.",
    ///             new Dictionary&lt;string, object&gt;
    ///             {
    ///                 ["errorType"] = "IOException",
    ///                 ["filePath"] = filePath,
    ///                 ["operationId"] = operationId,
    ///                 ["processingTime"] = (DateTime.UtcNow - startTime).TotalMilliseconds,
    ///                 ["suggestion"] = "Ensure file is not locked by another process"
    ///             }
    ///         );
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="CreateSuccess"/>
    /// <seealso cref="Success"/>
    /// <seealso cref="Error"/>
    /// <seealso cref="Metadata"/>
    public static ToolResult CreateError(string error, Dictionary<string, object>? metadata = null)
    {
        return new ToolResult
        {
            IsSuccess = false,
            ErrorMessage = error,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}