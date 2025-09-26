namespace Agent.Communication.Models;

/// <summary>
/// Represents a communication response that carries the results, status, and metadata from processing
/// a communication request. Provides a standardized structure for returning operation results, error
/// information, and performance metrics in inter-agent and service-to-service communication.
/// </summary>
/// <remarks>
/// <para>
/// CommunicationResponse is the counterpart to CommunicationRequest, providing a structured format
/// for returning the results of processing communication requests. It includes success/failure status,
/// return data, error information, and performance metrics for comprehensive response handling.
/// </para>
/// <para>
/// **Key Components:**
/// - **Success Indication**: Clear boolean status indicating operation success or failure
/// - **Response Data**: Flexible payload containing the operation results or return values
/// - **Error Information**: Detailed error messages and exception details for failed operations
/// - **Performance Metrics**: Processing duration and timing information for monitoring
/// - **Routing Information**: Source and target identifiers for response delivery
/// </para>
/// <para>
/// **Response Patterns:**
/// - **Success Responses**: Contain result data with success=true and populated payload
/// - **Error Responses**: Include error messages with success=false and diagnostic information
/// - **Partial Success**: Success=true with warnings or partial results in metadata
/// - **Processing Status**: Intermediate responses for long-running operations
/// </para>
/// <para>
/// **Immutable Factory Pattern:**
/// The class provides static factory methods (CreateSuccess, CreateError) to ensure consistent
/// response creation and prevent invalid state combinations. This pattern promotes reliable
/// error handling and consistent response structure across the application.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Successful operation response
/// var successResponse = CommunicationResponse.CreateSuccess(
///     requestId: "req-12345",
///     payload: new { 
///         Result = "Order processed successfully",
///         OrderId = "ORD-67890",
///         EstimatedDelivery = DateTime.UtcNow.AddDays(3)
///     },
///     source: "order-service",
///     target: "user-interface"
/// );
/// 
/// // Error response with detailed information
/// var errorResponse = CommunicationResponse.CreateError(
///     requestId: "req-12346",
///     errorMessage: "Insufficient inventory for requested quantity",
///     source: "inventory-service", 
///     target: "order-service"
/// );
/// 
/// // Response with additional metadata
/// var response = CommunicationResponse.CreateSuccess(
///     requestId: "req-12347",
///     payload: new { ProcessedRecords = 1500, SkippedRecords = 25 },
///     source: "data-processor",
///     target: "batch-controller"
/// );
/// 
/// response.Duration = TimeSpan.FromSeconds(45.2);
/// response.Metadata["ProcessingRate"] = "33.3 records/second";
/// response.Metadata["WarningCount"] = "25";
/// response.Headers["X-Processing-Node"] = "worker-node-03";
/// 
/// // Handling responses with comprehensive error checking
/// if (response.IsSuccess)
/// {
///     var result = response.Payload;
///     Console.WriteLine($"Operation completed in {response.Duration.TotalMilliseconds}ms");
///     
///     if (response.Metadata.ContainsKey("WarningCount"))
///     {
///         Console.WriteLine($"Processed with {response.Metadata["WarningCount"]} warnings");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Operation failed: {response.ErrorMessage}");
///     
///     if (response.Metadata.ContainsKey("RetryAfter"))
///     {
///         Console.WriteLine($"Retry recommended after: {response.Metadata["RetryAfter"]}");
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="CommunicationRequest"/>
/// <seealso cref="ICommunicationChannel"/>
/// <seealso cref="BaseChannel"/>
public class CommunicationResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this communication response.
    /// Used for response tracking, correlation, and audit logging.
    /// </summary>
    /// <value>
    /// A unique string identifier for this response. Should be unique for each response instance.
    /// </value>
    /// <remarks>
    /// The response ID is independent of the request ID and provides a unique identifier for this
    /// response instance. This is useful for tracking response processing, implementing idempotency,
    /// and correlating responses in distributed tracing systems.
    /// </remarks>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the identifier of the original communication request that this response addresses.
    /// This enables correlation between requests and their corresponding responses.
    /// </summary>
    /// <value>
    /// The request identifier from the original CommunicationRequest. Should not be empty for valid responses.
    /// </value>
    /// <remarks>
    /// The request ID is crucial for correlating responses with their originating requests in
    /// asynchronous communication patterns. This property must match the Id property of the
    /// original CommunicationRequest to enable proper request-response correlation.
    /// </remarks>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp indicating when this response was created or completed.
    /// Used for performance monitoring, SLA tracking, and audit trails.
    /// </summary>
    /// <value>
    /// A DateTime in UTC format representing the response creation or completion time.
    /// </value>
    /// <remarks>
    /// The timestamp should represent when the response processing completed, not when the
    /// response object was instantiated. This enables accurate calculation of processing
    /// duration and supports SLA monitoring and performance analytics.
    /// </remarks>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the agent, service, or channel that generated this response.
    /// This should correspond to the target of the original request.
    /// </summary>
    /// <value>
    /// A string identifier for the response originator. Should match the target of the original request.
    /// </value>
    /// <remarks>
    /// The source identifier enables response routing and audit trail tracking. It should match
    /// the Target property of the original CommunicationRequest to maintain proper request-response
    /// correlation and enable symmetric routing patterns.
    /// </remarks>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the agent, service, or channel that should receive this response.
    /// This should correspond to the source of the original request.
    /// </summary>
    /// <value>
    /// A string identifier for the response recipient. Should match the source of the original request.
    /// </value>
    /// <remarks>
    /// The target identifier ensures proper response delivery by identifying the intended recipient.
    /// It should match the Source property of the original CommunicationRequest to complete the
    /// request-response routing cycle.
    /// </remarks>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the requested operation completed successfully.
    /// This provides a clear success/failure indication for response handling logic.
    /// </summary>
    /// <value>
    /// True if the operation completed successfully and Payload contains valid results;
    /// false if the operation failed and ErrorMessage contains error details.
    /// </value>
    /// <remarks>
    /// The success flag is the primary indicator for response handling logic. Successful responses
    /// should have success=true with result data in Payload, while failed responses should have
    /// success=false with error information in ErrorMessage. This enables consistent error handling
    /// patterns across the application.
    /// </remarks>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the response payload containing the results or return data from the operation.
    /// Contains the actual business data returned by successful operations.
    /// </summary>
    /// <value>
    /// An object containing the response data. Should be null for error responses.
    /// </value>
    /// <remarks>
    /// The payload contains the actual results or return data from processing the request.
    /// It should be populated for successful operations and follow consistent data contracts
    /// for reliable processing. For error responses, this should typically be null, with
    /// error information provided in the ErrorMessage property instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple response payload
    /// Payload = new { Status = "Completed", RecordId = 12345 };
    /// 
    /// // Complex business object response
    /// Payload = new ProcessingResult
    /// {
    ///     ProcessedCount = 1000,
    ///     ErrorCount = 5,
    ///     ProcessingTime = TimeSpan.FromMinutes(2.5),
    ///     Results = new[] { /* processed items */ }
    /// };
    /// 
    /// // File processing response
    /// Payload = new { 
    ///     GeneratedFiles = new[] { "/output/result.pdf", "/output/summary.txt" },
    ///     TotalSize = 2048576 
    /// };
    /// </code>
    /// </example>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets the error message providing details about what went wrong if the operation failed.
    /// Should be null or empty for successful operations.
    /// </summary>
    /// <value>
    /// A descriptive error message explaining the failure, or null for successful operations.
    /// </value>
    /// <remarks>
    /// The error message should provide clear, actionable information about what went wrong
    /// and, when possible, guidance on how to resolve the issue. It should be human-readable
    /// and suitable for logging or display to end users when appropriate.
    /// </remarks>
    /// <example>
    /// <code>
    /// // User-friendly error messages
    /// ErrorMessage = "The requested document could not be found";
    /// ErrorMessage = "Insufficient permissions to access the specified resource";
    /// ErrorMessage = "The operation timed out after 30 seconds";
    /// 
    /// // Technical error messages for debugging
    /// ErrorMessage = "Database connection failed: Connection timeout expired";
    /// ErrorMessage = "Invalid JSON format in request payload at line 15, column 23";
    /// </code>
    /// </example>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata and context information related to the response processing.
    /// Used for supplementary data that doesn't belong in the main payload.
    /// </summary>
    /// <value>
    /// A dictionary containing metadata key-value pairs. Empty dictionary if no metadata is provided.
    /// </value>
    /// <remarks>
    /// Metadata provides a way to include supplementary information about the response processing
    /// that doesn't belong in the main payload. This can include performance metrics, warnings,
    /// processing hints, and other contextual information useful for monitoring and debugging.
    /// </remarks>
    /// <example>
    /// <code>
    /// Metadata = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["ProcessingNode"] = "worker-05",
    ///     ["CacheHit"] = true,
    ///     ["WarningCount"] = 3,
    ///     ["RetryAttempt"] = 2,
    ///     ["PerformanceScore"] = 0.95
    /// };
    /// </code>
    /// </example>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets transport-level headers and protocol-specific metadata for this response.
    /// Used for technical metadata that supports response processing and routing.
    /// </summary>
    /// <value>
    /// A dictionary containing header key-value pairs. Empty dictionary if no headers are provided.
    /// </value>
    /// <remarks>
    /// Headers are typically used for protocol-specific metadata such as content types,
    /// caching directives, correlation IDs, and transport-level information. They may be
    /// processed by infrastructure components and communication channels.
    /// </remarks>
    /// <example>
    /// <code>
    /// Headers = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["Content-Type"] = "application/json",
    ///     ["Cache-Control"] = "max-age=3600",
    ///     ["X-Processing-Time"] = "1.23s",
    ///     ["X-Correlation-ID"] = "corr-67890"
    /// };
    /// </code>
    /// </example>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the total time taken to process the request and generate this response.
    /// Used for performance monitoring, SLA tracking, and optimization analysis.
    /// </summary>
    /// <value>
    /// A TimeSpan representing the processing duration. Should reflect the actual processing time.
    /// </value>
    /// <remarks>
    /// The duration should represent the total time from request receipt to response completion,
    /// including any I/O operations, database queries, and business logic processing. This metric
    /// is essential for performance monitoring, capacity planning, and identifying bottlenecks.
    /// </remarks>
    /// <example>
    /// <code>
    /// Duration = TimeSpan.FromMilliseconds(250);  // Fast operations
    /// Duration = TimeSpan.FromSeconds(2.5);       // Standard operations
    /// Duration = TimeSpan.FromMinutes(5);         // Long-running operations
    /// </code>
    /// </example>
    public TimeSpan Duration { get; set; }

    #region Static Factory Methods

    /// <summary>
    /// Creates a successful communication response with the specified request correlation and payload data.
    /// This factory method ensures consistent response structure for successful operations.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the original request this response corresponds to.
    /// Must match the Id from the originating CommunicationRequest.
    /// </param>
    /// <param name="payload">
    /// Optional payload data containing the operation results or return values.
    /// Can be null for operations that don't return data.
    /// </param>
    /// <param name="source">
    /// Optional identifier of the service or agent generating this response.
    /// Should match the target of the original request for proper routing.
    /// </param>
    /// <param name="target">
    /// Optional identifier of the service or agent that should receive this response.
    /// Should match the source of the original request for proper delivery.
    /// </param>
    /// <returns>
    /// A new CommunicationResponse instance configured as a successful response with
    /// IsSuccess=true, populated payload, and specified routing information.
    /// </returns>
    /// <remarks>
    /// This factory method is the recommended way to create successful responses as it ensures
    /// consistent structure and prevents invalid state combinations. The response will have
    /// IsSuccess=true, ErrorMessage=null, and the current timestamp automatically set.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple success response
    /// var response = CommunicationResponse.CreateSuccess(
    ///     requestId: "req-12345",
    ///     payload: new { Status = "Completed", Id = 67890 },
    ///     source: "processing-service",
    ///     target: "client-app"
    /// );
    /// 
    /// // Success response with complex data
    /// var analysisResult = CommunicationResponse.CreateSuccess(
    ///     requestId: originalRequest.Id,
    ///     payload: new AnalysisResult 
    ///     {
    ///         Confidence = 0.95,
    ///         Categories = new[] { "Technology", "Business" },
    ///         ProcessedAt = DateTime.UtcNow,
    ///         Metadata = new { ModelVersion = "v2.1", ProcessingTime = "1.2s" }
    ///     },
    ///     source: "ai-analysis-engine",
    ///     target: originalRequest.Source
    /// );
    /// 
    /// // Success response without payload
    /// var acknowledgment = CommunicationResponse.CreateSuccess(
    ///     requestId: request.Id,
    ///     source: "notification-service",
    ///     target: request.Source
    /// );
    /// </code>
    /// </example>
    public static CommunicationResponse CreateSuccess(string requestId, object? payload = null, string source = "", string target = "")
    {
        return new CommunicationResponse
        {
            RequestId = requestId,
            IsSuccess = true,
            Payload = payload,
            Source = source,
            Target = target,
            ErrorMessage = null
        };
    }

    /// <summary>
    /// Creates an error communication response with the specified request correlation and error information.
    /// This factory method ensures consistent response structure for failed operations.
    /// </summary>
    /// <param name="requestId">
    /// The identifier of the original request this response corresponds to.
    /// Must match the Id from the originating CommunicationRequest.
    /// </param>
    /// <param name="errorMessage">
    /// A descriptive error message explaining what went wrong during processing.
    /// Should be clear and actionable when possible.
    /// </param>
    /// <param name="source">
    /// Optional identifier of the service or agent generating this error response.
    /// Should match the target of the original request for proper routing.
    /// </param>
    /// <param name="target">
    /// Optional identifier of the service or agent that should receive this error response.
    /// Should match the source of the original request for proper delivery.
    /// </param>
    /// <returns>
    /// A new CommunicationResponse instance configured as an error response with
    /// IsSuccess=false, populated error message, and specified routing information.
    /// </returns>
    /// <remarks>
    /// This factory method is the recommended way to create error responses as it ensures
    /// consistent structure and prevents invalid state combinations. The response will have
    /// IsSuccess=false, Payload=null, and the current timestamp automatically set.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple error response
    /// var errorResponse = CommunicationResponse.CreateError(
    ///     requestId: "req-12345",
    ///     errorMessage: "The requested resource could not be found",
    ///     source: "data-service",
    ///     target: "client-app"
    /// );
    /// 
    /// // Detailed error with context
    /// var validationError = CommunicationResponse.CreateError(
    ///     requestId: request.Id,
    ///     errorMessage: "Validation failed: Email address format is invalid and phone number is required",
    ///     source: "validation-service",
    ///     target: request.Source
    /// );
    /// 
    /// // System error with technical details
    /// var systemError = CommunicationResponse.CreateError(
    ///     requestId: request.Id,
    ///     errorMessage: $"Database connection failed after 3 retry attempts. Last error: Connection timeout",
    ///     source: "database-service",
    ///     target: request.Source
    /// );
    /// 
    /// // Add additional error context using metadata
    /// var enrichedError = CommunicationResponse.CreateError(
    ///     requestId: request.Id,
    ///     errorMessage: "Processing failed due to insufficient resources",
    ///     source: "processing-engine",
    ///     target: request.Source
    /// );
    /// 
    /// enrichedError.Metadata["ErrorCode"] = "RESOURCE_EXHAUSTED";
    /// enrichedError.Metadata["RetryAfter"] = TimeSpan.FromMinutes(5);
    /// enrichedError.Metadata["SupportTicket"] = "TICKET-12345";
    /// </code>
    /// </example>
    public static CommunicationResponse CreateError(string requestId, string errorMessage, string source = "", string target = "")
    {
        return new CommunicationResponse
        {
            RequestId = requestId,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Source = source,
            Target = target,
            Payload = null
        };
    }

    #endregion

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
    /// Gets or sets the response payload. This is an alias for the Payload property
    /// maintained for backward compatibility with existing code.
    /// </summary>
    /// <value>The same value as the Payload property.</value>
    /// <remarks>
    /// This property exists solely for backward compatibility. New code should use the Payload property directly.
    /// Both properties reference the same underlying field, so changes to either will affect both.
    /// </remarks>
    [Obsolete("Use Payload property instead. This alias is maintained for backward compatibility only.")]
    public object? Data
    {
        get => Payload;
        set => Payload = value;
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