namespace Agent.Core.Models;

/// <summary>
/// Represents a request sent to an agent for processing, containing input data, metadata, and context information.
/// This class provides a standardized structure for agent communication and request tracking throughout the framework.
/// </summary>
/// <remarks>
/// <para>
/// AgentRequest is the primary data structure for communicating with agents. It encapsulates all the information
/// needed for an agent to process a request, including the payload data, tracking information, and execution context.
/// </para>
/// <para>
/// **Request Lifecycle:**
/// 1. Request creation with unique identifier and source information
/// 2. Payload and metadata population
/// 3. Submission to agent for processing
/// 4. Agent processes request and returns AgentResult
/// 5. Request tracking through logs and monitoring systems
/// </para>
/// <para>
/// **Thread Safety:**
/// AgentRequest instances are not thread-safe and should not be modified concurrently.
/// Each request should be processed by a single agent execution thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic request for simple data processing
/// var basicRequest = new AgentRequest
/// {
///     RequestId = Guid.NewGuid().ToString(),
///     Source = "web-api",
///     Payload = "Process this text data"
/// };
/// 
/// // Complex request with structured data and metadata
/// var complexRequest = new AgentRequest
/// {
///     RequestId = "REQ-2024-001-" + DateTime.UtcNow.Ticks,
///     Source = "batch-processor",
///     Payload = new
///     {
///         CustomerId = 12345,
///         Action = "ProcessOrder",
///         OrderData = new
///         {
///             OrderId = "ORD-2024-12345",
///             Items = new[] { "item1", "item2", "item3" },
///             Total = 299.99m
///         }
///     },
///     Metadata = new Dictionary&lt;string, object&gt;
///     {
///         ["Priority"] = "High",
///         ["CorrelationId"] = "CORR-2024-ABC123",
///         ["UserId"] = "user@example.com",
///         ["RequestTimestamp"] = DateTime.UtcNow,
///         ["ClientVersion"] = "v1.2.3"
///     }
/// };
/// 
/// // Request with context for stateful processing
/// var contextualRequest = new AgentRequest
/// {
///     RequestId = "SESSION-" + sessionId,
///     Source = "chat-interface",
///     Payload = "What is the status of my recent order?",
///     Context = new Dictionary&lt;string, object&gt;
///     {
///         ["SessionId"] = sessionId,
///         ["PreviousInteractions"] = conversationHistory,
///         ["UserPreferences"] = userSettings,
///         ["AuthenticationContext"] = authContext
///     },
///     Metadata = new Dictionary&lt;string, object&gt;
///     {
///         ["ConversationTurn"] = 3,
///         ["Language"] = "en-US",
///         ["Channel"] = "web-chat"
///     }
/// };
/// 
/// // Submitting request to agent
/// var result = await agent.ExecuteAsync(complexRequest, cancellationToken);
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Request {complexRequest.RequestId} processed successfully");
/// }
/// </code>
/// </example>
/// <seealso cref="AgentResult"/>
/// <seealso cref="IAgent.ExecuteAsync"/>
/// <seealso cref="BaseAgent.ExecuteInternalAsync"/>
public class AgentRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for this request.
    /// This ID is used for tracking, logging, and correlating requests across the system.
    /// </summary>
    /// <value>
    /// A unique string identifier for the request. Typically a GUID string, but can be any unique value.
    /// Defaults to a new GUID string when not explicitly set.
    /// </value>
    /// <remarks>
    /// <para>
    /// The RequestId is crucial for:
    /// - **Request Tracking**: Following request flow through logs and monitoring
    /// - **Error Correlation**: Linking errors and results back to specific requests
    /// - **Debugging**: Identifying specific requests during troubleshooting
    /// - **Metrics Collection**: Grouping performance metrics by request
    /// - **Audit Trails**: Maintaining compliance and audit requirements
    /// </para>
    /// <para>
    /// **ID Format Recommendations:**
    /// - Use GUIDs for maximum uniqueness: Guid.NewGuid().ToString()
    /// - Include timestamp for chronological sorting: "REQ-{timestamp}-{random}"
    /// - Add prefixes for categorization: "BATCH-", "API-", "SYSTEM-"
    /// - Maintain consistent format across your application
    /// </para>
    /// <para>
    /// **Best Practices:**
    /// - Always set RequestId for production requests
    /// - Log RequestId in all related operations
    /// - Include RequestId in error messages and exceptions
    /// - Use RequestId for request deduplication when needed
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // GUID-based ID (recommended for most cases)
    /// request.RequestId = Guid.NewGuid().ToString();
    /// // Output: "f47ac10b-58cc-4372-a567-0e02b2c3d479"
    /// 
    /// // Timestamped ID for chronological ordering
    /// request.RequestId = $"REQ-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
    /// // Output: "REQ-20240127-143022-f47ac10b58cc4372a5670e02b2c3d479"
    /// 
    /// // Sequential ID for batch processing
    /// request.RequestId = $"BATCH-{batchId}-{sequenceNumber:D6}";
    /// // Output: "BATCH-PROC-2024-001-000123"
    /// 
    /// // Custom correlation ID from external systems
    /// request.RequestId = $"EXT-{externalSystemId}-{externalRequestId}";
    /// // Output: "EXT-SALESFORCE-SF-REQ-12345"
    /// </code>
    /// </example>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the source system or component that originated this request.
    /// This helps identify where requests come from for routing, logging, and analytics purposes.
    /// </summary>
    /// <value>
    /// A string identifying the request source. Should use consistent naming conventions
    /// across your application. Defaults to an empty string.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Source field provides important context about request origins:
    /// - **Request Routing**: Different sources may need different processing logic
    /// - **Security Context**: Source-based authorization and rate limiting
    /// - **Analytics**: Understanding usage patterns by source system
    /// - **Troubleshooting**: Identifying problematic sources or integration points
    /// - **Load Balancing**: Distributing load based on source characteristics
    /// </para>
    /// <para>
    /// **Source Naming Conventions:**
    /// - Use lowercase with hyphens: "web-api", "background-service"
    /// - Include system boundaries: "external-partner", "internal-system"
    /// - Be specific but concise: "mobile-app-v2", "legacy-migration-tool"
    /// - Maintain consistency across all requests from the same source
    /// </para>
    /// <para>
    /// **Common Source Categories:**
    /// - **User Interfaces**: "web-portal", "mobile-app", "admin-dashboard"
    /// - **API Endpoints**: "rest-api", "graphql-api", "webhook-handler"
    /// - **Background Services**: "scheduled-job", "event-processor", "data-sync"
    /// - **External Systems**: "partner-api", "legacy-system", "third-party-webhook"
    /// - **Internal Components**: "orchestrator", "workflow-engine", "message-handler"
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // User-facing applications
    /// request.Source = "web-portal";
    /// request.Source = "mobile-app";
    /// request.Source = "admin-dashboard";
    /// 
    /// // API and integration sources
    /// request.Source = "rest-api-v1";
    /// request.Source = "webhook-handler";
    /// request.Source = "partner-integration";
    /// 
    /// // Background processing sources
    /// request.Source = "scheduled-batch";
    /// request.Source = "event-processor";
    /// request.Source = "data-migration";
    /// 
    /// // System-to-system communication
    /// request.Source = "orchestration-engine";
    /// request.Source = "workflow-step-3";
    /// request.Source = "error-recovery-service";
    /// 
    /// // Using source for conditional logic
    /// if (request.Source == "external-partner")
    /// {
    ///     // Apply additional security validation
    ///     await ValidatePartnerCredentials(request);
    /// }
    /// 
    /// // Source-based metrics collection
    /// _metrics.IncrementCounter($"requests.{request.Source}.total", 1);
    /// </code>
    /// </example>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the main data payload for the request.
    /// This contains the actual data that the agent should process and can be any serializable object.
    /// </summary>
    /// <value>
    /// Any object containing the request data. Can be a string, complex object, or any serializable type.
    /// Defaults to null.
    /// </value>
    /// <remarks>
    /// <para>
    /// The Payload is the core content of the request and contains the actual data the agent needs to process.
    /// It's designed to be flexible and can accommodate various data types and structures depending on
    /// the agent's requirements.
    /// </para>
    /// <para>
    /// **Payload Design Considerations:**
    /// - **Type Flexibility**: Can be primitive types, complex objects, or collections
    /// - **Serialization**: Must be serializable for logging, caching, and distribution
    /// - **Size Limitations**: Consider memory usage and network transfer for large payloads
    /// - **Schema Evolution**: Design for backward compatibility when payload structures change
    /// - **Security**: Avoid including sensitive data; use references or encrypted values
    /// </para>
    /// <para>
    /// **Common Payload Patterns:**
    /// - **Simple Commands**: String commands or action names
    /// - **Structured Data**: Complex objects with multiple properties
    /// - **File References**: Paths or URIs to external data instead of embedding content
    /// - **Batch Data**: Collections of items to process
    /// - **Query Parameters**: Search criteria or filtering options
    /// </para>
    /// <para>
    /// **Agent Processing:**
    /// Agents typically deserialize the payload to their expected type:
    /// ```csharp
    /// var requestData = JsonSerializer.Deserialize&lt;MyRequestType&gt;(request.Payload?.ToString() ?? "{}");
    /// ```
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple string payload
    /// request.Payload = "Process this text for sentiment analysis";
    /// 
    /// // Simple command payload
    /// request.Payload = "GENERATE_REPORT";
    /// 
    /// // Structured object payload
    /// request.Payload = new CustomerProcessingRequest
    /// {
    ///     CustomerId = 12345,
    ///     Action = "UpdateProfile",
    ///     Data = new
    ///     {
    ///         Email = "customer@example.com",
    ///         Preferences = new { Theme = "dark", Language = "en-US" }
    ///     }
    /// };
    /// 
    /// // Collection payload for batch processing
    /// request.Payload = new
    /// {
    ///     BatchId = "BATCH-2024-001",
    ///     Items = new[]
    ///     {
    ///         new { Id = 1, Data = "item1" },
    ///         new { Id = 2, Data = "item2" },
    ///         new { Id = 3, Data = "item3" }
    ///     }
    /// };
    /// 
    /// // File processing payload with reference
    /// request.Payload = new
    /// {
    ///     FileReference = "s3://bucket/path/to/file.csv",
    ///     ProcessingOptions = new
    ///     {
    ///         SkipHeader = true,
    ///         Delimiter = ",",
    ///         Encoding = "UTF-8"
    ///     }
    /// };
    /// 
    /// // Query/search payload
    /// request.Payload = new
    /// {
    ///     Query = "customer complaints",
    ///     Filters = new
    ///     {
    ///         DateRange = new { Start = "2024-01-01", End = "2024-01-31" },
    ///         Status = "Open",
    ///         Priority = new[] { "High", "Critical" }
    ///     },
    ///     Options = new
    ///     {
    ///         MaxResults = 100,
    ///         IncludeArchived = false
    ///     }
    /// };
    /// 
    /// // Agent processing example
    /// protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    /// {
    ///     // Type-safe deserialization with error handling
    ///     try
    ///     {
    ///         var payloadJson = request.Payload?.ToString() ?? "{}";
    ///         var requestData = JsonSerializer.Deserialize&lt;MyExpectedType&gt;(payloadJson);
    ///         
    ///         if (requestData == null)
    ///         {
    ///             return AgentResult.CreateError("Invalid payload format", stopwatch.Elapsed);
    ///         }
    ///         
    ///         // Process the strongly-typed data
    ///         var result = await ProcessData(requestData, cancellationToken);
    ///         return AgentResult.CreateSuccess(result, stopwatch.Elapsed);
    ///     }
    ///     catch (JsonException ex)
    ///     {
    ///         return AgentResult.CreateError($"Payload deserialization failed: {ex.Message}", stopwatch.Elapsed);
    ///     }
    /// }
    /// </code>
    /// </example>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets contextual information that supports request processing but is not part of the main payload.
    /// Context provides additional state, session information, or environmental data needed for processing.
    /// </summary>
    /// <value>
    /// A dictionary containing contextual key-value pairs that provide additional information for request processing.
    /// Defaults to an empty dictionary.
    /// </value>
    /// <remarks>
    /// <para>
    /// Context information supplements the main payload with additional data that agents may need for
    /// proper processing. Unlike metadata (which is primarily for tracking and management),
    /// context directly influences how the request should be processed.
    /// </para>
    /// <para>
    /// **Context Categories:**
    /// - **Session State**: User sessions, conversation history, temporary data
    /// - **Authentication**: User identity, permissions, security context
    /// - **Environment**: Configuration, feature flags, regional settings
    /// - **Business Context**: Customer data, account information, preferences
    /// - **Processing Hints**: Optimization flags, processing preferences
    /// </para>
    /// <para>
    /// **Context vs. Payload vs. Metadata:**
    /// - **Payload**: The primary data to process (required)
    /// - **Context**: Supporting data that affects processing (optional)
    /// - **Metadata**: Tracking and management information (optional)
    /// </para>
    /// <para>
    /// **Best Practices:**
    /// - Use consistent key naming conventions across requests
    /// - Keep context data relevant to processing logic
    /// - Avoid storing large objects; use references when possible
    /// - Consider security implications of context data
    /// - Document expected context keys for each agent type
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // User session context for stateful interactions
    /// request.Context = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["UserId"] = "user123",
    ///     ["SessionId"] = "session-abc-123",
    ///     ["ConversationHistory"] = previousMessages,
    ///     ["UserPreferences"] = userSettings,
    ///     ["LastInteraction"] = DateTime.UtcNow.AddMinutes(-5)
    /// };
    /// 
    /// // Authentication and authorization context
    /// request.Context = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["AuthenticatedUser"] = currentUser,
    ///     ["Permissions"] = userPermissions,
    ///     ["TenantId"] = "tenant-456",
    ///     ["SecurityLevel"] = "Standard",
    ///     ["AccessToken"] = tokenReference // Don't store actual tokens
    /// };
    /// 
    /// // Business context for personalized processing
    /// request.Context = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["CustomerId"] = 12345,
    ///     ["CustomerTier"] = "Premium",
    ///     ["AccountType"] = "Business",
    ///     ["Region"] = "North America",
    ///     ["Language"] = "en-US",
    ///     ["Timezone"] = "America/New_York"
    /// };
    /// 
    /// // Processing configuration context
    /// request.Context = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["ProcessingMode"] = "HighAccuracy", // vs "FastProcessing"
    ///     ["EnableCaching"] = true,
    ///     ["MaxProcessingTime"] = TimeSpan.FromMinutes(10),
    ///     ["ReturnDetailedResults"] = true,
    ///     ["FeatureFlags"] = new[] { "NewAlgorithm", "BetaFeatures" }
    /// };
    /// 
    /// // Environment and deployment context
    /// request.Context = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["Environment"] = "Production", // vs "Staging", "Development"
    ///     ["DataCenter"] = "US-East",
    ///     ["ApiVersion"] = "v2.1",
    ///     ["ClientVersion"] = "1.2.3",
    ///     ["DeploymentVersion"] = "2024.01.15.1"
    /// };
    /// 
    /// // Accessing context in agent implementation
    /// protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    /// {
    ///     // Extract context information with safe defaults
    ///     var userId = request.Context.TryGetValue("UserId", out var userIdObj) 
    ///                  ? userIdObj.ToString() 
    ///                  : "anonymous";
    ///                  
    ///     var processingMode = request.Context.TryGetValue("ProcessingMode", out var modeObj)
    ///                         ? modeObj.ToString()
    ///                         : "Standard";
    ///     
    ///     var enableCaching = request.Context.TryGetValue("EnableCaching", out var cachingObj) &amp;&amp;
    ///                        Convert.ToBoolean(cachingObj);
    /// 
    ///     // Use context to customize processing
    ///     if (processingMode == "HighAccuracy")
    ///     {
    ///         return await ProcessWithHighAccuracy(request.Payload, userId, cancellationToken);
    ///     }
    ///     else
    ///     {
    ///         return await ProcessWithStandardAccuracy(request.Payload, userId, enableCaching, cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </example>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata about the request that is used for tracking, monitoring, and management purposes.
    /// Metadata typically does not affect processing logic but provides important operational information.
    /// </summary>
    /// <value>
    /// A dictionary containing metadata key-value pairs for operational tracking and management.
    /// Defaults to an empty dictionary.
    /// </value>
    /// <remarks>
    /// <para>
    /// Metadata provides operational and administrative information about requests without affecting
    /// the core processing logic. It's primarily used for monitoring, analytics, debugging, and
    /// compliance tracking.
    /// </para>
    /// <para>
    /// **Common Metadata Categories:**
    /// - **Tracking**: Correlation IDs, trace IDs, parent request references
    /// - **Timing**: Request timestamps, expiration times, processing deadlines
    /// - **Quality**: Priority levels, SLA requirements, quality expectations
    /// - **Source Details**: Client versions, user agents, originating systems
    /// - **Compliance**: Audit information, regulatory requirements, data classification
    /// - **Performance**: Expected processing time, resource hints, optimization flags
    /// </para>
    /// <para>
    /// **Metadata vs. Context:**
    /// - **Metadata**: Operational information (tracking, monitoring, management)
    /// - **Context**: Processing information (business logic, personalization, configuration)
    /// </para>
    /// <para>
    /// **Best Practices:**
    /// - Use standard key names across your organization
    /// - Include timing and correlation information for distributed tracing
    /// - Add client and version information for compatibility tracking
    /// - Include priority and SLA information for queue management
    /// - Use metadata for metrics collection and alerting
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic tracking metadata
    /// request.Metadata = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["CorrelationId"] = "CORR-2024-ABC123",
    ///     ["TraceId"] = "trace-xyz-789",
    ///     ["ParentRequestId"] = "parent-req-456",
    ///     ["RequestTimestamp"] = DateTime.UtcNow,
    ///     ["ClientIP"] = "192.168.1.100"
    /// };
    /// 
    /// // Quality and SLA metadata
    /// request.Metadata = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["Priority"] = "High", // Low, Normal, High, Critical
    ///     ["SLA"] = "Tier1", // Tier1: 1min, Tier2: 5min, Tier3: 30min
    ///     ["MaxProcessingTime"] = TimeSpan.FromMinutes(2),
    ///     ["QualityLevel"] = "Production", // vs "Testing", "Preview"
    ///     ["ExpectedAccuracy"] = 0.95 // 95% accuracy requirement
    /// };
    /// 
    /// // Client and version tracking metadata
    /// request.Metadata = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["ClientName"] = "WebPortal",
    ///     ["ClientVersion"] = "2.1.3",
    ///     ["UserAgent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
    ///     ["ApiVersion"] = "v2",
    ///     ["SDKVersion"] = "1.0.5",
    ///     ["DeviceType"] = "Desktop" // Mobile, Tablet, Desktop
    /// };
    /// 
    /// // Compliance and audit metadata
    /// request.Metadata = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["DataClassification"] = "Internal", // Public, Internal, Confidential, Restricted
    ///     ["ComplianceRequirements"] = new[] { "GDPR", "HIPAA" },
    ///     ["AuditRequired"] = true,
    ///     ["RetentionPeriod"] = TimeSpan.FromDays(2555), // 7 years
    ///     ["DataSubjectId"] = "subject-123" // For GDPR compliance
    /// };
    /// 
    /// // Performance and optimization metadata
    /// request.Metadata = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["ExpectedProcessingTimeMs"] = 500,
    ///     ["MaxMemoryUsageMB"] = 256,
    ///     ["PreferredRegion"] = "US-East",
    ///     ["CacheHint"] = "Cacheable", // Cacheable, NoCache, RefreshCache
    ///     ["BatchSize"] = 100,
    ///     ["ParallelismHint"] = 4
    /// };
    /// 
    /// // Business and operational metadata
    /// request.Metadata = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["BusinessUnit"] = "CustomerService",
    ///     ["CostCenter"] = "CC-12345",
    ///     ["Project"] = "Q1-Initiative",
    ///     ["Environment"] = "Production", // Development, Staging, Production
    ///     ["Owner"] = "team-alpha@company.com",
    ///     ["SupportContact"] = "support@company.com"
    /// };
    /// 
    /// // Using metadata in monitoring and logging
    /// protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken)
    /// {
    ///     // Extract metadata for logging and monitoring
    ///     var correlationId = request.Metadata.TryGetValue("CorrelationId", out var corrObj) 
    ///                        ? corrObj.ToString() 
    ///                        : "unknown";
    ///                        
    ///     var priority = request.Metadata.TryGetValue("Priority", out var priorityObj)
    ///                   ? priorityObj.ToString()
    ///                   : "Normal";
    /// 
    ///     // Log with metadata for tracing
    ///     _logger.LogInformation("Processing request {RequestId} with correlation {CorrelationId} at priority {Priority}",
    ///                           request.RequestId, correlationId, priority);
    /// 
    ///     // Use priority for queue management
    ///     if (priority == "Critical")
    ///     {
    ///         await ProcessWithHighestPriority(request, cancellationToken);
    ///     }
    /// 
    ///     // Include metadata in result for tracking
    ///     var result = await ProcessRequest(request, cancellationToken);
    ///     
    ///     // Pass through important metadata to result
    ///     result.Metadata["CorrelationId"] = correlationId;
    ///     result.Metadata["ProcessingPriority"] = priority;
    ///     
    ///     return result;
    /// }
    /// </code>
    /// </example>
    public Dictionary<string, object> Metadata { get; set; } = new();

    #region Backward Compatibility Aliases

    /// <summary>
    /// Gets or sets the input data for the request. This is an alias for the Payload property
    /// maintained for backward compatibility with existing code.
    /// </summary>
    /// <value>The same value as the Payload property.</value>
    /// <remarks>
    /// This property exists solely for backward compatibility. New code should use the Payload property directly.
    /// Both properties reference the same underlying field, so changes to either will affect both.
    /// </remarks>
    [Obsolete("Use Payload property instead. This alias is maintained for backward compatibility only.")]
    public object? Input
    {
        get => Payload;
        set => Payload = value;
    }

    /// <summary>
    /// Gets or sets the request identifier. This is an alias for the RequestId property
    /// maintained for backward compatibility with existing code.
    /// </summary>
    /// <value>The same value as the RequestId property.</value>
    /// <remarks>
    /// This property exists solely for backward compatibility. New code should use the RequestId property directly.
    /// Both properties reference the same underlying field, so changes to either will affect both.
    /// </remarks>
    [Obsolete("Use RequestId property instead. This alias is maintained for backward compatibility only.")]
    public string Id
    {
        get => RequestId;
        set => RequestId = value;
    }

    /// <summary>
    /// Gets or sets the cancellation token. This is maintained for backward compatibility with existing code.
    /// </summary>
    /// <value>A CancellationToken for the request. Defaults to CancellationToken.None.</value>
    /// <remarks>
    /// This property exists for backward compatibility. Modern implementations should pass the CancellationToken
    /// directly to agent methods rather than storing it in the request object.
    /// </remarks>
    [Obsolete("Pass CancellationToken directly to agent methods instead of storing in request. This property is maintained for backward compatibility only.")]
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

    #endregion
}