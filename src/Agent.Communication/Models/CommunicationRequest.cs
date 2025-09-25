namespace Agent.Communication.Models;

/// <summary>
/// Represents a communication request that carries data, routing information, and metadata
/// between agents or services through communication channels. Provides a standardized structure
/// for inter-agent messaging with support for various communication patterns and payload types.
/// </summary>
/// <remarks>
/// <para>
/// CommunicationRequest serves as the fundamental messaging unit in the GenericAiAgents framework,
/// enabling structured communication between distributed components. It encapsulates all necessary
/// information for routing, processing, and responding to inter-agent requests.
/// </para>
/// <para>
/// **Key Components:**
/// - **Routing Information**: Source and target identifiers for message routing
/// - **Message Classification**: MessageType and Action for processing categorization  
/// - **Payload Data**: Flexible object payload supporting any serializable data
/// - **Metadata Management**: Extensible headers and context for additional information
/// - **Quality of Service**: Priority, timeout, and processing hints
/// </para>
/// <para>
/// **Message Patterns:**
/// - **Request-Response**: Synchronous communication expecting a response
/// - **Fire-and-Forget**: Asynchronous one-way messages without response expectation
/// - **Event Notifications**: Broadcasting state changes or system events
/// - **Command Messages**: Instructional messages for action execution
/// </para>
/// <para>
/// **Serialization and Transport:**
/// All properties are designed to be serialization-friendly, supporting JSON, XML,
/// and binary serialization for transport across different communication channels
/// including HTTP, message queues, gRPC, and in-memory channels.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic request-response pattern
/// var request = new CommunicationRequest
/// {
///     Id = Guid.NewGuid().ToString(),
///     Source = "order-service",
///     Target = "inventory-service",
///     MessageType = "CheckInventory",
///     Payload = new { ProductId = "ABC123", Quantity = 5 },
///     Priority = 1, // High priority
///     TimeoutSeconds = 10
/// };
/// 
/// var response = await channel.SendRequestAsync(request);
/// 
/// // Event notification example
/// var notification = new CommunicationRequest
/// {
///     Id = Guid.NewGuid().ToString(),
///     Source = "payment-service",
///     Target = "order-service",
///     MessageType = "PaymentProcessed",
///     Payload = new 
///     { 
///         OrderId = "ORD-12345", 
///         Amount = 99.99m, 
///         TransactionId = "TXN-789" 
///     },
///     Headers = new Dictionary&lt;string, string&gt;
///     {
///         ["CorrelationId"] = "CORR-456",
///         ["EventType"] = "Domain.PaymentCompleted"
///     }
/// };
/// 
/// await channel.SendAsync(notification);
/// 
/// // Complex workflow request with context
/// var workflowRequest = new CommunicationRequest
/// {
///     Id = Guid.NewGuid().ToString(),
///     Source = "workflow-engine",
///     Target = "document-processor",
///     MessageType = "ProcessDocument",
///     Action = "ExtractAndClassify",
///     Payload = new 
///     { 
///         DocumentPath = "/uploads/contract.pdf",
///         ProcessingOptions = new { ExtractTables = true, OCRQuality = "High" }
///     },
///     Context = new Dictionary&lt;string, object&gt;
///     {
///         ["WorkflowId"] = "WF-001",
///         ["StepId"] = "STEP-003",
///         ["UserId"] = "user@company.com",
///         ["TenantId"] = "tenant-123"
///     },
///     Headers = new Dictionary&lt;string, string&gt;
///     {
///         ["Authorization"] = "Bearer eyJ...",
///         ["TraceId"] = "trace-abc123",
///         ["SpanId"] = "span-def456"
///     }
/// };
/// 
/// var result = await channel.SendRequestAsync(workflowRequest);
/// </code>
/// </example>
/// <seealso cref="CommunicationResponse"/>
/// <seealso cref="ICommunicationChannel"/>
/// <seealso cref="BaseChannel"/>
public class CommunicationRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for this communication request.
    /// This identifier is used for request tracking, response correlation, and audit logging.
    /// </summary>
    /// <value>
    /// A unique string identifier, typically a GUID. Automatically generates a new GUID if not explicitly set.
    /// </value>
    /// <remarks>
    /// The request ID is crucial for correlating responses with their originating requests in
    /// asynchronous communication patterns. It should be unique within the scope of the communication
    /// session and is often used in distributed tracing and monitoring systems.
    /// </remarks>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the UTC timestamp indicating when this request was created.
    /// Used for processing order, timeout calculations, and audit trails.
    /// </summary>
    /// <value>
    /// A DateTime in UTC format representing the request creation time. Defaults to the current UTC time.
    /// </value>
    /// <remarks>
    /// The timestamp is essential for implementing timeout behaviors, SLA monitoring, and ensuring
    /// proper message ordering in scenarios where processing order matters. Always stored in UTC
    /// to avoid timezone-related issues in distributed systems.
    /// </remarks>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the target agent, service, or channel that should process this request.
    /// This is used for message routing and delivery to the appropriate handler.
    /// </summary>
    /// <value>
    /// A string identifier for the target recipient. Should match the channel identifier or service name.
    /// </value>
    /// <remarks>
    /// The target identifier must match a registered channel or service identifier for successful
    /// message delivery. In microservice architectures, this typically corresponds to service names,
    /// while in agent systems it represents agent identifiers or roles.
    /// </remarks>
    /// <example>
    /// <code>
    /// Target = "inventory-management-service"  // Service-based targeting
    /// Target = "agent-coordinator"             // Agent-based targeting  
    /// Target = "workflow-step-processor"       // Workflow-based targeting
    /// </code>
    /// </example>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the agent, service, or channel that originated this request.
    /// Used for routing responses and tracking message flow.
    /// </summary>
    /// <value>
    /// A string identifier for the message originator. Should be the sender's channel or service identifier.
    /// </value>
    /// <remarks>
    /// The source identifier enables response routing and helps build audit trails for message flow
    /// tracking. It's particularly important for security and monitoring to understand message origins.
    /// </remarks>
    /// <example>
    /// <code>
    /// Source = "user-interface-service"        // UI-initiated requests
    /// Source = "scheduled-job-processor"       // Background job requests
    /// Source = "external-api-gateway"          // External system requests
    /// </code>
    /// </example>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type or category that describes the nature of this communication.
    /// Used for message classification, routing decisions, and handler selection.
    /// </summary>
    /// <value>
    /// A string describing the message type or category. Should follow consistent naming conventions.
    /// </value>
    /// <remarks>
    /// MessageType provides semantic meaning to the communication, enabling receivers to determine
    /// how to process the message without examining the payload. It's often used in conjunction with
    /// message routing patterns and handler registration systems.
    /// </remarks>
    /// <example>
    /// <code>
    /// MessageType = "DataProcessingRequest"    // Processing requests
    /// MessageType = "StatusUpdate"             // State notifications
    /// MessageType = "SystemCommand"            // Control commands
    /// MessageType = "UserInteraction"          // User-driven actions
    /// </code>
    /// </example>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific action or operation to be performed by the target handler.
    /// Provides granular control over processing behavior within a message type.
    /// </summary>
    /// <value>
    /// A string specifying the action to perform. Can be empty for simple message types.
    /// </value>
    /// <remarks>
    /// The Action property allows for fine-grained control over message processing, enabling
    /// a single message type to support multiple operations. This is useful for implementing
    /// command patterns and reducing the number of distinct message types.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Document processing actions
    /// Action = "ExtractText"
    /// Action = "GenerateThumbnail" 
    /// Action = "ValidateFormat"
    /// 
    /// // User management actions
    /// Action = "CreateUser"
    /// Action = "UpdateProfile"
    /// Action = "DeactivateAccount"
    /// </code>
    /// </example>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the main data payload for this communication request.
    /// Can contain any serializable object representing the business data or parameters.
    /// </summary>
    /// <value>
    /// An object containing the request data. Can be null for requests that don't require payload data.
    /// </value>
    /// <remarks>
    /// The payload contains the actual business data or parameters needed for request processing.
    /// It should be serializable for transport across different communication channels and should
    /// follow consistent data contracts for reliable processing.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple data payload
    /// Payload = new { UserId = 123, Action = "UpdateProfile" };
    /// 
    /// // Complex business object
    /// Payload = new OrderProcessingRequest
    /// {
    ///     OrderId = "ORD-001",
    ///     Items = new[] { new { ProductId = "P001", Quantity = 2 } },
    ///     ShippingAddress = new Address { ... },
    ///     PaymentMethod = new PaymentInfo { ... }
    /// };
    /// 
    /// // File processing payload
    /// Payload = new { FilePath = "/uploads/document.pdf", Options = new { Quality = "High" } };
    /// </code>
    /// </example>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets additional context information that provides supplementary data for request processing.
    /// Used for metadata that doesn't belong in the main payload but is needed for processing.
    /// </summary>
    /// <value>
    /// A dictionary containing context key-value pairs. Empty dictionary if no context is provided.
    /// </value>
    /// <remarks>
    /// Context provides a way to pass metadata and environmental information that supports
    /// request processing without being part of the core business data. This includes user
    /// context, security tokens, workflow state, and processing hints.
    /// </remarks>
    /// <example>
    /// <code>
    /// Context = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["UserId"] = "user@example.com",
    ///     ["TenantId"] = "tenant-123",
    ///     ["WorkflowStep"] = "approval-required",
    ///     ["RetryCount"] = 2,
    ///     ["ProcessingHints"] = new { FastMode = true }
    /// };
    /// </code>
    /// </example>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Gets or sets transport-level headers and metadata for this communication request.
    /// Used for protocol-specific information, tracing, and system metadata.
    /// </summary>
    /// <value>
    /// A dictionary containing header key-value pairs. Empty dictionary if no headers are provided.
    /// </value>
    /// <remarks>
    /// Headers are typically used for transport-level metadata such as authentication tokens,
    /// correlation IDs, tracing information, and protocol-specific directives. Unlike Context,
    /// headers are often standardized and may be processed by infrastructure components.
    /// </remarks>
    /// <example>
    /// <code>
    /// Headers = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIs...",
    ///     ["CorrelationId"] = "corr-12345",
    ///     ["TraceId"] = "trace-67890",
    ///     ["Content-Type"] = "application/json",
    ///     ["X-Request-Source"] = "mobile-app"
    /// };
    /// </code>
    /// </example>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum number of seconds to wait for request processing completion.
    /// Used by communication channels to implement timeout behavior and prevent resource leaks.
    /// </summary>
    /// <value>
    /// An integer representing the timeout in seconds. Default is 30 seconds.
    /// </value>
    /// <remarks>
    /// The timeout value is used by communication channels to implement cancellation and prevent
    /// indefinite blocking. It should be set based on expected processing time and system SLAs.
    /// Different message types may require different timeout values based on their complexity.
    /// </remarks>
    /// <example>
    /// <code>
    /// TimeoutSeconds = 5;    // Fast operations like validation
    /// TimeoutSeconds = 30;   // Standard business operations  
    /// TimeoutSeconds = 300;  // Long-running operations like file processing
    /// TimeoutSeconds = 3600; // Background batch operations
    /// </code>
    /// </example>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the priority level for this request, influencing processing order in queued scenarios.
    /// Lower numbers indicate higher priority (0 = highest priority, 10 = lowest priority).
    /// </summary>
    /// <value>
    /// An integer from 0 to 10 representing the priority level. Default is 5 (medium priority).
    /// </value>
    /// <remarks>
    /// Priority is used by communication channels and message processing systems to determine
    /// the order of message processing when multiple messages are queued. This enables
    /// implementation of quality-of-service policies and ensures critical messages are
    /// processed before routine operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// Priority = 0;  // Critical system alerts, emergency operations
    /// Priority = 1;  // High-priority user requests, SLA-critical operations  
    /// Priority = 5;  // Standard business operations (default)
    /// Priority = 8;  // Background maintenance, cleanup operations
    /// Priority = 10; // Lowest priority, defer until system idle
    /// </code>
    /// </example>
    public int Priority { get; set; } = 5;

    #region Backward Compatibility Aliases

    /// <summary>
    /// Gets or sets the unique request identifier. This is an alias for the Id property
    /// maintained for backward compatibility with existing code.
    /// </summary>
    /// <value>The same value as the Id property.</value>
    /// <remarks>
    /// This property exists solely for backward compatibility. New code should use the Id property directly.
    /// Both properties reference the same underlying field, so changes to either will affect both.
    /// </remarks>
    [Obsolete("Use Id property instead. This alias is maintained for backward compatibility only.")]
    public string RequestId
    {
        get => Id;
        set => Id = value;
    }

    #endregion
}