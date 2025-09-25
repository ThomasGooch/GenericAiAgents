namespace Agent.Core.Models;

/// <summary>
/// Represents the result of an agent operation, containing success status, output data, error information, 
/// execution metrics, and additional metadata. This class provides a standardized response structure
/// for all agent processing operations within the framework.
/// </summary>
/// <remarks>
/// <para>
/// AgentResult is the standard return type for all agent execution operations. It encapsulates both
/// successful and failed operation results in a consistent format that supports rich error handling,
/// performance monitoring, and result metadata.
/// </para>
/// <para>
/// **Design Principles:**
/// - **Consistent Structure**: Same result format for all agent types and operations
/// - **Rich Error Information**: Detailed error messages and exception context
/// - **Performance Tracking**: Built-in execution timing and performance metrics
/// - **Extensible Metadata**: Additional result information through metadata dictionary
/// - **Type Safety**: Generic payload support for strongly-typed results
/// </para>
/// <para>
/// **Result States:**
/// An AgentResult can be in one of two states:
/// - **Success**: IsSuccess = true, Data contains result, ErrorMessage is null
/// - **Error**: IsSuccess = false, ErrorMessage contains error details, Data may be null
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating successful results
/// var simpleSuccess = AgentResult.CreateSuccess("Operation completed");
/// 
/// var detailedSuccess = AgentResult.CreateSuccess(
///     data: new { CustomerId = 123, Status = "Processed" },
///     executionTime: TimeSpan.FromSeconds(2.5));
///     
/// // Creating error results
/// var simpleError = AgentResult.CreateError("Validation failed");
/// 
/// var detailedError = AgentResult.CreateError(
///     "Database connection timeout",
///     TimeSpan.FromSeconds(30));
/// 
/// // Processing results
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Success! Data: {result.Data}");
///     Console.WriteLine($"Executed in: {result.ExecutionTime.TotalMilliseconds}ms");
///     
///     // Access strongly-typed result data
///     if (result.Data is ProcessingResult processingResult)
///     {
///         Console.WriteLine($"Items processed: {processingResult.ItemsProcessed}");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Error: {result.ErrorMessage}");
///     if (result.Exception != null)
///     {
///         Console.WriteLine($"Exception: {result.Exception.Message}");
///     }
/// }
/// 
/// // Using metadata for additional information
/// result.Metadata["ProcessedBy"] = "DataProcessingAgent";
/// result.Metadata["ProcessingMode"] = "HighAccuracy";
/// result.Metadata["ResourcesUsed"] = new[] { "Database", "Cache", "ExternalAPI" };
/// </code>
/// </example>
/// <seealso cref="AgentRequest"/>
/// <seealso cref="IAgent.ExecuteAsync"/>
/// <seealso cref="BaseAgent.ExecuteInternalAsync"/>
public class AgentResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the agent operation completed successfully.
    /// This is the primary indicator of operation outcome.
    /// </summary>
    /// <value>
    /// <c>true</c> if the operation completed successfully; <c>false</c> if an error occurred.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property should be the first check when processing agent results. It determines
    /// whether to access the result data or handle error information.
    /// </para>
    /// <para>
    /// **Success Criteria:**
    /// - Operation completed without throwing unhandled exceptions
    /// - Business logic validation passed
    /// - Required outputs were generated
    /// - No critical errors were encountered
    /// </para>
    /// <para>
    /// **Failure Criteria:**
    /// - Unhandled exceptions occurred during processing
    /// - Business logic validation failed
    /// - Required resources were unavailable
    /// - Operation was cancelled or timed out
    /// - Input validation failed
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Always check IsSuccess before processing results
    /// var result = await agent.ExecuteAsync(request);
    /// 
    /// if (result.IsSuccess)
    /// {
    ///     // Handle successful outcome
    ///     ProcessSuccessfulResult(result.Data);
    ///     LogSuccess(result.ExecutionTime);
    /// }
    /// else
    /// {
    ///     // Handle error outcome
    ///     LogError(result.ErrorMessage, result.Exception);
    ///     NotifyFailure(result.RequestId);
    /// }
    /// 
    /// // Pattern for different success/error handling
    /// return result.IsSuccess 
    ///     ? await ProcessSuccess(result) 
    ///     : await HandleError(result);
    /// </code>
    /// </example>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the result data produced by the agent operation.
    /// This contains the actual output or processed information when the operation succeeds.
    /// </summary>
    /// <value>
    /// The result data object, which can be any type depending on the agent's functionality.
    /// May be null for operations that don't return data or when errors occur.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Data property contains the primary output of the agent operation. The type and structure
    /// of this data depends on the specific agent implementation and the type of processing performed.
    /// </para>
    /// <para>
    /// **Data Types and Patterns:**
    /// - **Simple Values**: Strings, numbers, booleans for basic operations
    /// - **Complex Objects**: Structured data with multiple properties
    /// - **Collections**: Arrays or lists of processed items
    /// - **References**: URLs, IDs, or paths to external resources
    /// - **Status Information**: Processing status, completion indicators
    /// </para>
    /// <para>
    /// **Null Data Scenarios:**
    /// - Operations that perform actions without returning data (e.g., notifications, updates)
    /// - Error conditions where no meaningful data can be returned
    /// - Operations cancelled before data generation
    /// - Operations that only produce side effects
    /// </para>
    /// <para>
    /// **Type Safety:**
    /// Since Data is of type object, consumers should safely cast or deserialize to expected types:
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Handling different types of result data
    /// if (result.IsSuccess &amp;&amp; result.Data != null)
    /// {
    ///     // String result
    ///     if (result.Data is string textResult)
    ///     {
    ///         Console.WriteLine($"Text result: {textResult}");
    ///     }
    ///     
    ///     // Complex object result
    ///     if (result.Data is ProcessingResult processingResult)
    ///     {
    ///         Console.WriteLine($"Processed {processingResult.ItemCount} items");
    ///         Console.WriteLine($"Success rate: {processingResult.SuccessRate:P2}");
    ///     }
    ///     
    ///     // Collection result
    ///     if (result.Data is IEnumerable&lt;object&gt; collection)
    ///     {
    ///         Console.WriteLine($"Collection contains {collection.Count()} items");
    ///     }
    ///     
    ///     // JSON deserialization for complex types
    ///     try
    ///     {
    ///         var jsonResult = JsonSerializer.Serialize(result.Data);
    ///         var typedResult = JsonSerializer.Deserialize&lt;MyExpectedType&gt;(jsonResult);
    ///         ProcessTypedResult(typedResult);
    ///     }
    ///     catch (JsonException ex)
    ///     {
    ///         // Handle deserialization errors
    ///         Logger.LogWarning("Failed to deserialize result data: {Error}", ex.Message);
    ///     }
    /// }
    /// 
    /// // Safe data access with null checks
    /// var dataValue = result.IsSuccess ? result.Data : null;
    /// var dataString = result.Data?.ToString() ?? "No data";
    /// 
    /// // Type-specific processing
    /// switch (result.Data)
    /// {
    ///     case string str:
    ///         ProcessTextResult(str);
    ///         break;
    ///     case int number:
    ///         ProcessNumericResult(number);
    ///         break;
    ///     case ProcessingResult proc:
    ///         ProcessComplexResult(proc);
    ///         break;
    ///     case null:
    ///         ProcessEmptyResult();
    ///         break;
    ///     default:
    ///         ProcessUnknownResult(result.Data);
    ///         break;
    /// }
    /// </code>
    /// </example>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message describing what went wrong during the operation.
    /// This provides human-readable error information when the operation fails.
    /// </summary>
    /// <value>
    /// A descriptive error message explaining the failure reason. 
    /// Should be null when IsSuccess is true.
    /// </value>
    /// <remarks>
    /// <para>
    /// The ErrorMessage provides clear, actionable information about operation failures.
    /// It should be written for both developers (during debugging) and potentially for
    /// end users (after appropriate filtering and localization).
    /// </para>
    /// <para>
    /// **Error Message Guidelines:**
    /// - **Clear and Specific**: Describe exactly what went wrong
    /// - **Actionable**: Include suggestions for resolution when possible
    /// - **Context-Aware**: Include relevant context (IDs, values, conditions)
    /// - **Professional**: Use professional language suitable for logs and user display
    /// - **Avoid Technical Details**: Keep stack traces and technical details in Exception property
    /// </para>
    /// <para>
    /// **Error Categories:**
    /// - **Validation Errors**: "Customer ID is required and cannot be empty"
    /// - **Business Logic Errors**: "Cannot process order: insufficient inventory"
    /// - **External Service Errors**: "Payment service is currently unavailable"
    /// - **Timeout Errors**: "Operation timed out after 30 seconds"
    /// - **Resource Errors**: "Database connection could not be established"
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Good error messages - specific and actionable
    /// return AgentResult.CreateError("Customer ID 12345 not found in database");
    /// return AgentResult.CreateError("Email address 'invalid-email' is not in valid format");
    /// return AgentResult.CreateError("Order cannot be processed: payment method declined");
    /// return AgentResult.CreateError("External API rate limit exceeded, retry in 60 seconds");
    /// 
    /// // Error message with context
    /// return AgentResult.CreateError($"File processing failed: unsupported format '{fileExtension}'. Supported formats: PDF, DOCX, TXT");
    /// 
    /// // Processing error messages
    /// if (!result.IsSuccess)
    /// {
    ///     // Log for developers
    ///     _logger.LogError("Agent execution failed: {ErrorMessage}", result.ErrorMessage);
    ///     
    ///     // Display to user (after localization/filtering)
    ///     var userMessage = LocalizeErrorMessage(result.ErrorMessage);
    ///     await NotifyUser(userMessage);
    ///     
    ///     // Error categorization for handling
    ///     if (result.ErrorMessage.Contains("timeout"))
    ///     {
    ///         await HandleTimeoutError(result);
    ///     }
    ///     else if (result.ErrorMessage.Contains("not found"))
    ///     {
    ///         await HandleNotFoundError(result);
    ///     }
    /// }
    /// 
    /// // Error message aggregation for multiple failures
    /// var errors = new List&lt;string&gt;();
    /// if (string.IsNullOrEmpty(request.CustomerId))
    ///     errors.Add("Customer ID is required");
    /// if (string.IsNullOrEmpty(request.Email))
    ///     errors.Add("Email address is required");
    /// 
    /// if (errors.Any())
    /// {
    ///     return AgentResult.CreateError($"Validation failed: {string.Join("; ", errors)}");
    /// }
    /// </code>
    /// </example>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the operation to fail, if applicable.
    /// This provides detailed technical information about the failure for debugging and logging.
    /// </summary>
    /// <value>
    /// The exception that caused the failure, or null if no exception occurred or 
    /// the failure was a business logic error rather than an exception.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Exception property captures technical exception details that caused operation failures.
    /// This is primarily used for debugging, logging, and error analysis rather than user display.
    /// </para>
    /// <para>
    /// **Exception Scenarios:**
    /// - **System Exceptions**: Database connection failures, network timeouts, file access errors
    /// - **External Service Exceptions**: API call failures, service unavailability
    /// - **Serialization Exceptions**: JSON parsing errors, type conversion failures
    /// - **Timeout Exceptions**: Operation cancellation due to timeout
    /// - **Security Exceptions**: Authentication or authorization failures
    /// </para>
    /// <para>
    /// **Exception vs. ErrorMessage:**
    /// - **ErrorMessage**: Human-readable description for users and logs
    /// - **Exception**: Technical details for developers and debugging
    /// - Both can be present: user-friendly message + technical exception details
    /// </para>
    /// <para>
    /// **Security Considerations:**
    /// Be careful not to expose sensitive information through exception details in logs
    /// or error responses that might reach end users.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Processing exceptions in results
    /// if (!result.IsSuccess &amp;&amp; result.Exception != null)
    /// {
    ///     // Log full exception details for debugging
    ///     _logger.LogError(result.Exception, 
    ///                     "Agent execution failed: {ErrorMessage}", 
    ///                     result.ErrorMessage);
    ///     
    ///     // Extract specific exception information
    ///     switch (result.Exception)
    ///     {
    ///         case SqlException sqlEx:
    ///             await HandleDatabaseError(sqlEx, result);
    ///             break;
    ///         case HttpRequestException httpEx:
    ///             await HandleNetworkError(httpEx, result);
    ///             break;
    ///         case TimeoutException timeoutEx:
    ///             await HandleTimeoutError(timeoutEx, result);
    ///             break;
    ///         case JsonException jsonEx:
    ///             await HandleSerializationError(jsonEx, result);
    ///             break;
    ///         default:
    ///             await HandleGenericError(result.Exception, result);
    ///             break;
    ///     }
    /// }
    /// 
    /// // Creating results with exceptions in agent implementations
    /// try
    /// {
    ///     var data = await ProcessDataAsync(request);
    ///     return AgentResult.CreateSuccess(data, stopwatch.Elapsed);
    /// }
    /// catch (DatabaseException dbEx)
    /// {
    ///     return AgentResult.CreateError(
    ///         "Database operation failed", 
    ///         stopwatch.Elapsed, 
    ///         dbEx);
    /// }
    /// catch (ValidationException validEx)
    /// {
    ///     return AgentResult.CreateError(
    ///         $"Validation failed: {validEx.Message}",
    ///         stopwatch.Elapsed,
    ///         validEx);
    /// }
    /// 
    /// // Exception analysis for monitoring
    /// if (result.Exception != null)
    /// {
    ///     var exceptionMetrics = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["ExceptionType"] = result.Exception.GetType().Name,
    ///         ["ExceptionMessage"] = result.Exception.Message,
    ///         ["StackTraceHash"] = result.Exception.StackTrace?.GetHashCode(),
    ///         ["InnerException"] = result.Exception.InnerException?.GetType().Name
    ///     };
    ///     
    ///     await _metrics.RecordExceptionAsync(exceptionMetrics);
    /// }
    /// </code>
    /// </example>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the time taken to execute the agent operation.
    /// This provides performance metrics for monitoring and optimization.
    /// </summary>
    /// <value>
    /// The total execution time for the operation. Should be provided for both
    /// successful and failed operations to support performance analysis.
    /// </value>
    /// <remarks>
    /// <para>
    /// ExecutionTime is crucial for performance monitoring, SLA compliance tracking,
    /// and system optimization. It should represent the total time from operation
    /// start to completion, including all processing, I/O, and dependency calls.
    /// </para>
    /// <para>
    /// **Timing Considerations:**
    /// - **Include All Processing**: Full end-to-end operation time
    /// - **Consistent Measurement**: Use same timing mechanism across all agents
    /// - **Failed Operations**: Include timing even for failed operations
    /// - **Precision**: Use high-precision timing (Stopwatch) for accuracy
    /// - **Context**: Consider including breakdown of time spent in different phases
    /// </para>
    /// <para>
    /// **Performance Analysis Uses:**
    /// - **SLA Monitoring**: Tracking compliance with performance requirements
    /// - **Bottleneck Identification**: Finding slow operations and dependencies
    /// - **Capacity Planning**: Understanding resource requirements under load
    /// - **Optimization**: Measuring impact of performance improvements
    /// - **Alerting**: Triggering alerts for abnormally slow operations
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Measuring execution time in agent implementations
    /// protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    /// {
    ///     var stopwatch = Stopwatch.StartNew();
    ///     
    ///     try
    ///     {
    ///         var result = await ProcessRequestAsync(request, cancellationToken);
    ///         
    ///         // Always include execution time in successful results
    ///         return AgentResult.CreateSuccess(result, stopwatch.Elapsed);
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         // Include execution time even for failed operations
    ///         return AgentResult.CreateError(
    ///             $"Processing failed: {ex.Message}", 
    ///             stopwatch.Elapsed, 
    ///             ex);
    ///     }
    /// }
    /// 
    /// // Performance monitoring and analysis
    /// if (result.ExecutionTime.TotalSeconds > 10)
    /// {
    ///     _logger.LogWarning("Slow operation detected: {RequestId} took {ExecutionTime}s",
    ///                       request.RequestId, result.ExecutionTime.TotalSeconds);
    /// }
    /// 
    /// // SLA compliance checking
    /// var slaThreshold = TimeSpan.FromSeconds(5);
    /// if (result.ExecutionTime > slaThreshold)
    /// {
    ///     await RecordSLAViolation(request.RequestId, result.ExecutionTime, slaThreshold);
    /// }
    /// 
    /// // Performance metrics collection
    /// _metrics.RecordValue("agent.execution_time", result.ExecutionTime.TotalMilliseconds, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["agent_type"] = agent.GetType().Name,
    ///     ["success"] = result.IsSuccess,
    ///     ["source"] = request.Source
    /// });
    /// 
    /// // Performance trend analysis
    /// var performanceData = new
    /// {
    ///     RequestId = request.RequestId,
    ///     AgentType = agent.GetType().Name,
    ///     ExecutionTime = result.ExecutionTime,
    ///     Success = result.IsSuccess,
    ///     Timestamp = DateTime.UtcNow
    /// };
    /// 
    /// await _performanceAnalyzer.RecordOperationAsync(performanceData);
    /// </code>
    /// </example>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the operation result.
    /// This extensible collection allows agents to provide supplementary information
    /// about the processing operation, performance metrics, and context details.
    /// </summary>
    /// <value>
    /// A dictionary containing metadata key-value pairs that provide additional information
    /// about the result and processing operation. Defaults to an empty dictionary.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Metadata dictionary provides a flexible mechanism for agents to include
    /// additional information about the result beyond the core success/failure status.
    /// This information supports monitoring, debugging, analytics, and result interpretation.
    /// </para>
    /// <para>
    /// **Common Metadata Categories:**
    /// - **Processing Details**: Steps executed, algorithms used, processing modes
    /// - **Performance Metrics**: Memory usage, CPU time, I/O operations, cache hits
    /// - **Resource Information**: External services called, databases accessed, files processed
    /// - **Quality Metrics**: Confidence scores, accuracy measurements, completeness indicators
    /// - **Debugging Information**: Intermediate results, processing flags, decision points
    /// - **Business Context**: Cost information, SLA tracking, compliance indicators
    /// </para>
    /// <para>
    /// **Metadata Best Practices:**
    /// - Use consistent key naming conventions across agents
    /// - Include units in numeric value keys (e.g., "MemoryUsedMB", "ProcessingTimeMs")
    /// - Provide both raw data and calculated metrics when helpful
    /// - Consider metadata size impact on memory and network usage
    /// - Include metadata that supports troubleshooting and optimization
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Processing performance metadata
    /// result.Metadata["ProcessingSteps"] = new[] { "Validation", "Transformation", "Analysis", "Output" };
    /// result.Metadata["MemoryUsedMB"] = GC.GetTotalMemory(false) / (1024 * 1024);
    /// result.Metadata["DatabaseQueriesExecuted"] = 5;
    /// result.Metadata["ExternalAPICalls"] = 2;
    /// result.Metadata["CacheHitRate"] = 0.85; // 85% cache hit rate
    /// 
    /// // Quality and confidence metadata
    /// result.Metadata["ConfidenceScore"] = 0.92; // 92% confidence
    /// result.Metadata["AccuracyEstimate"] = 0.96; // 96% estimated accuracy
    /// result.Metadata["CompletenessRating"] = "High"; // High, Medium, Low
    /// result.Metadata["DataQualityScore"] = 8.7; // Out of 10
    /// 
    /// // Business and operational metadata
    /// result.Metadata["ProcessingCostUSD"] = 0.025; // Cost in USD
    /// result.Metadata["SLACompliant"] = true;
    /// result.Metadata["ProcessedBy"] = Environment.MachineName;
    /// result.Metadata["ProcessingRegion"] = "US-East";
    /// result.Metadata["TenantId"] = "tenant-123";
    /// 
    /// // Debug and diagnostic metadata
    /// result.Metadata["IntermediateResults"] = intermediateData;
    /// result.Metadata["DecisionPoints"] = decisionLog;
    /// result.Metadata["WarningsEncountered"] = warnings;
    /// result.Metadata["ProcessingMode"] = "HighAccuracy";
    /// 
    /// // Resource utilization metadata
    /// result.Metadata["ThreadsUsed"] = threadCount;
    /// result.Metadata["NetworkBytesReceived"] = networkStats.BytesReceived;
    /// result.Metadata["NetworkBytesSent"] = networkStats.BytesSent;
    /// result.Metadata["DiskIOOperations"] = diskStats.IOOperations;
    /// 
    /// // Result characteristics metadata
    /// result.Metadata["ResultType"] = result.Data?.GetType().Name;
    /// result.Metadata["ResultSize"] = JsonSerializer.Serialize(result.Data).Length;
    /// result.Metadata["ItemsProcessed"] = processedItems.Count;
    /// result.Metadata["ItemsSkipped"] = skippedItems.Count;
    /// result.Metadata["ValidationErrors"] = validationErrors.Count;
    /// 
    /// // Accessing metadata for analysis
    /// if (result.Metadata.TryGetValue("ProcessingSteps", out var stepsObj) &amp;&amp; 
    ///     stepsObj is string[] steps)
    /// {
    ///     Console.WriteLine($"Processing included {steps.Length} steps: {string.Join(" → ", steps)}");
    /// }
    /// 
    /// // Conditional processing based on metadata
    /// if (result.Metadata.TryGetValue("ConfidenceScore", out var confidenceObj) &amp;&amp;
    ///     Convert.ToDouble(confidenceObj) &lt; 0.8)
    /// {
    ///     await RequestManualReview(result);
    /// }
    /// 
    /// // Metadata aggregation for reporting
    /// var performanceMetrics = new Dictionary&lt;string, object&gt;();
    /// foreach (var kvp in result.Metadata)
    /// {
    ///     if (kvp.Key.EndsWith("Time") || kvp.Key.EndsWith("TimeMs"))
    ///     {
    ///         performanceMetrics[kvp.Key] = kvp.Value;
    ///     }
    /// }
    /// 
    /// await _performanceReporter.RecordMetricsAsync(performanceMetrics);
    /// </code>
    /// </example>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a successful agent result with the specified data and execution time.
    /// </summary>
    /// <param name="data">The result data produced by the operation. Can be null for operations that don't return data.</param>
    /// <param name="executionTime">The time taken to execute the operation. Defaults to zero if not provided.</param>
    /// <returns>A new <see cref="AgentResult"/> instance representing a successful operation.</returns>
    /// <remarks>
    /// <para>
    /// This is the primary method for creating successful operation results. It automatically
    /// sets IsSuccess to true and ensures consistent result structure across all agents.
    /// </para>
    /// <para>
    /// **Usage Guidelines:**
    /// - Always provide execution time when available for performance monitoring
    /// - Include meaningful result data that consumers can use
    /// - Consider adding metadata for additional context using the returned result's Metadata property
    /// - Use null data for operations that perform actions without returning information
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple successful result
    /// return AgentResult.CreateSuccess("Processing completed successfully");
    /// 
    /// // Successful result with execution timing
    /// var stopwatch = Stopwatch.StartNew();
    /// var processedData = await ProcessDataAsync(input);
    /// return AgentResult.CreateSuccess(processedData, stopwatch.Elapsed);
    /// 
    /// // Successful result with complex data
    /// var result = new ProcessingResult
    /// {
    ///     ItemsProcessed = 150,
    ///     SuccessRate = 0.98,
    ///     GeneratedFiles = new[] { "output1.pdf", "output2.pdf" }
    /// };
    /// return AgentResult.CreateSuccess(result, stopwatch.Elapsed);
    /// 
    /// // Successful result for action without return data
    /// await SendNotificationAsync(recipient, message);
    /// return AgentResult.CreateSuccess(null, stopwatch.Elapsed); // No data, just success confirmation
    /// </code>
    /// </example>
    public static AgentResult CreateSuccess(object? data = null, TimeSpan executionTime = default)
    {
        return new AgentResult
        {
            IsSuccess = true,
            Data = data,
            ErrorMessage = null,
            Exception = null,
            ExecutionTime = executionTime,
            Metadata = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates a failed agent result with the specified error message, execution time, and optional exception.
    /// </summary>
    /// <param name="errorMessage">A descriptive error message explaining what went wrong. Cannot be null or empty.</param>
    /// <param name="executionTime">The time taken before the operation failed. Defaults to zero if not provided.</param>
    /// <param name="exception">The exception that caused the failure, if applicable. Can be null for business logic errors.</param>
    /// <returns>A new <see cref="AgentResult"/> instance representing a failed operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errorMessage"/> is null or empty.</exception>
    /// <remarks>
    /// <para>
    /// This method creates error results with comprehensive failure information. It automatically
    /// sets IsSuccess to false and ensures consistent error result structure across all agents.
    /// </para>
    /// <para>
    /// **Usage Guidelines:**
    /// - Always provide clear, actionable error messages
    /// - Include execution time even for failed operations for performance analysis
    /// - Include the causing exception when available for debugging
    /// - Consider adding additional context through the returned result's Metadata property
    /// - Use appropriate error message language for your target audience (developers vs. end users)
    /// </para>
    /// <para>
    /// **Error Message Best Practices:**
    /// - Be specific about what failed and why
    /// - Include relevant context (IDs, values, conditions)
    /// - Suggest resolution steps when possible
    /// - Avoid exposing sensitive information in error messages
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple error result
    /// return AgentResult.CreateError("Customer ID not found");
    /// 
    /// // Error result with execution timing
    /// var stopwatch = Stopwatch.StartNew();
    /// try
    /// {
    ///     await ProcessDataAsync(input);
    /// }
    /// catch (Exception ex)
    /// {
    ///     return AgentResult.CreateError("Data processing failed", stopwatch.Elapsed, ex);
    /// }
    /// 
    /// // Business logic error with context
    /// if (customer.Balance &lt; order.Total)
    /// {
    ///     return AgentResult.CreateError(
    ///         $"Insufficient balance: required ${{order.Total:F2}}, available ${{customer.Balance:F2}}",
    ///         stopwatch.Elapsed);
    /// }
    /// 
    /// // Validation error with details
    /// if (validationErrors.Any())
    /// {
    ///     var errorMessage = $"Validation failed: {string.Join("; ", validationErrors)}";
    ///     return AgentResult.CreateError(errorMessage, stopwatch.Elapsed);
    /// }
    /// 
    /// // External service error
    /// catch (HttpRequestException httpEx)
    /// {
    ///     return AgentResult.CreateError(
    ///         "External payment service is currently unavailable", 
    ///         stopwatch.Elapsed, 
    ///         httpEx);
    /// }
    /// 
    /// // Timeout error
    /// catch (TimeoutException timeoutEx)
    /// {
    ///     return AgentResult.CreateError(
    ///         $"Operation timed out after {timeout.TotalSeconds} seconds",
    ///         stopwatch.Elapsed,
    ///         timeoutEx);
    /// }
    /// </code>
    /// </example>
    public static AgentResult CreateError(string errorMessage, TimeSpan executionTime = default, Exception? exception = null)
    {
        if (string.IsNullOrEmpty(errorMessage))
        {
            throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
        }

        return new AgentResult
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = errorMessage,
            Exception = exception,
            ExecutionTime = executionTime,
            Metadata = new Dictionary<string, object>()
        };
    }

    #region Backward Compatibility Aliases

    /// <summary>
    /// Gets or sets the success status. This is an alias for the IsSuccess property
    /// maintained for backward compatibility with existing code.
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
    /// Gets or sets the result data. This is an alias for the Data property
    /// maintained for backward compatibility with existing code.
    /// </summary>
    /// <value>The same value as the Data property.</value>
    /// <remarks>
    /// This property exists solely for backward compatibility. New code should use the Data property directly.
    /// Both properties reference the same underlying field, so changes to either will affect both.
    /// </remarks>
    [Obsolete("Use Data property instead. This alias is maintained for backward compatibility only.")]
    public object? Output
    {
        get => Data;
        set => Data = value;
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

    #endregion
}