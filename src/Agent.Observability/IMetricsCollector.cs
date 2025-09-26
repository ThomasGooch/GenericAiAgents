using Agent.Observability.Models;
using System.Diagnostics;

namespace Agent.Observability;

/// <summary>
/// Defines the contract for comprehensive metrics collection, monitoring, and observability within
/// the GenericAiAgents framework. Provides standardized interfaces for collecting performance metrics,
/// distributed tracing, system health monitoring, and integration with enterprise observability platforms.
/// </summary>
/// <remarks>
/// <para>
/// The IMetricsCollector interface serves as the central hub for all observability data collection
/// in AI agent systems. It supports multiple metric types, distributed tracing, real-time monitoring,
/// and seamless integration with popular observability platforms including Prometheus, Grafana,
/// Application Insights, DataDog, and OpenTelemetry-compatible systems.
/// </para>
/// <para>
/// **Supported Metric Types:**
/// - **Counters**: Monotonically increasing values for event counting and rate calculation
/// - **Histograms**: Distribution analysis for response times, payload sizes, and value ranges
/// - **Gauges**: Current state values for resource utilization, queue lengths, and system status
/// - **Summaries**: Statistical aggregations with percentile calculations and trend analysis
/// </para>
/// <para>
/// **Enterprise Observability Features:**
/// - **Distributed Tracing**: Full request lifecycle tracking across agent interactions
/// - **Custom Dimensions**: Rich tagging and labeling for metric segmentation and filtering
/// - **Real-time Events**: Live metric streaming for dashboards and alerting systems
/// - **Multiple Export Formats**: Support for Prometheus, OpenMetrics, JSON, and custom formats
/// - **Performance Optimization**: Efficient collection with minimal application overhead
/// </para>
/// <para>
/// **Integration Patterns:**
/// The metrics collector integrates seamlessly with monitoring infrastructure through standardized
/// protocols and formats, enabling comprehensive observability stacks for production AI agent systems.
/// It supports both push and pull-based metric collection patterns.
/// </para>
/// <para>
/// **Performance Considerations:**
/// Implementations are designed for high-throughput scenarios with minimal performance impact on
/// agent operations. Metrics collection uses efficient buffering, batching, and asynchronous
/// processing to maintain system responsiveness under load.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Dependency injection setup
/// services.AddSingleton&lt;IMetricsCollector, PrometheusMetricsCollector&gt;();
/// services.Configure&lt;MetricsConfiguration&gt;(options =&gt;
/// {
///     options.Enabled = true;
///     options.ExportInterval = TimeSpan.FromSeconds(15);
///     options.DefaultTags = new Dictionary&lt;string, object&gt;
///     {
///         ["Environment"] = "Production",
///         ["Service"] = "AI-Agent-System",
///         ["Version"] = "1.0.0"
///     };
/// });
/// 
/// // Basic metrics collection in agent implementation
/// public class DocumentProcessingAgent : BaseAgent
/// {
///     private readonly IMetricsCollector _metrics;
///     
///     public DocumentProcessingAgent(IMetricsCollector metrics)
///     {
///         _metrics = metrics;
///     }
///     
///     protected override async Task&lt;AgentResult&gt; ExecuteInternalAsync(
///         AgentRequest request, CancellationToken cancellationToken)
///     {
///         var stopwatch = Stopwatch.StartNew();
///         
///         // Start distributed trace
///         using var activity = _metrics.StartActivity("document-processing", new Dictionary&lt;string, object&gt;
///         {
///             ["DocumentType"] = request.Context.GetValueOrDefault("DocumentType", "unknown"),
///             ["RequestId"] = request.RequestId,
///             ["UserId"] = request.Context.GetValueOrDefault("UserId", "anonymous")
///         });
///         
///         try
///         {
///             // Increment processing counter
///             _metrics.IncrementCounter("documents.processing.started", 1, new Dictionary&lt;string, object&gt;
///             {
///                 ["DocumentType"] = request.Context.GetValueOrDefault("DocumentType"),
///                 ["AgentType"] = "DocumentProcessor"
///             });
///             
///             // Set current processing gauge
///             _metrics.SetGaugeValue("documents.processing.active", GetActiveProcessingCount());
///             
///             var result = await ProcessDocumentAsync(request, cancellationToken);
///             
///             // Record processing duration
///             _metrics.RecordValue("documents.processing.duration_ms", stopwatch.ElapsedMilliseconds, 
///                 new Dictionary&lt;string, object&gt;
///                 {
///                     ["DocumentType"] = request.Context.GetValueOrDefault("DocumentType"),
///                     ["Success"] = true
///                 });
///             
///             // Record success metrics
///             _metrics.IncrementCounter("documents.processing.completed", 1);
///             
///             // Record agent-specific performance metrics
///             _metrics.RecordAgentMetrics(new AgentPerformanceMetrics
///             {
///                 AgentId = AgentId,
///                 OperationType = "DocumentProcessing",
///                 ExecutionTime = stopwatch.Elapsed,
///                 Success = true,
///                 RequestSize = CalculateRequestSize(request),
///                 ResponseSize = CalculateResponseSize(result),
///                 ResourcesUsed = GetResourceUtilization()
///             });
///             
///             return result;
///         }
///         catch (Exception ex)
///         {
///             // Record error metrics
///             _metrics.IncrementCounter("documents.processing.failed", 1, new Dictionary&lt;string, object&gt;
///             {
///                 ["ErrorType"] = ex.GetType().Name,
///                 ["DocumentType"] = request.Context.GetValueOrDefault("DocumentType")
///             });
///             
///             _metrics.RecordValue("documents.processing.duration_ms", stopwatch.ElapsedMilliseconds,
///                 new Dictionary&lt;string, object&gt; { ["Success"] = false });
///             
///             activity.SetError(ex.Message, ex);
///             throw;
///         }
///     }
/// }
/// 
/// // Workflow metrics collection
/// public class WorkflowOrchestrator
/// {
///     private readonly IMetricsCollector _metrics;
///     
///     public async Task&lt;WorkflowResult&gt; ExecuteWorkflowAsync(WorkflowDefinition workflow)
///     {
///         var stopwatch = Stopwatch.StartNew();
///         
///         using var activity = _metrics.StartActivity("workflow-execution", new Dictionary&lt;string, object&gt;
///         {
///             ["WorkflowId"] = workflow.Id,
///             ["WorkflowName"] = workflow.Name,
///             ["StepCount"] = workflow.Steps.Count
///         });
///         
///         try
///         {
///             var result = await ExecuteWorkflowInternalAsync(workflow);
///             
///             // Record comprehensive workflow metrics
///             _metrics.RecordWorkflowMetrics(new WorkflowMetrics
///             {
///                 WorkflowId = workflow.Id.ToString(),
///                 WorkflowName = workflow.Name,
///                 ExecutionTime = stopwatch.Elapsed,
///                 StepCount = workflow.Steps.Count,
///                 SuccessfulSteps = result.StepResults.Count(s =&gt; s.Value.IsSuccess),
///                 FailedSteps = result.StepResults.Count(s =&gt; !s.Value.IsSuccess),
///                 TotalDataProcessed = CalculateTotalDataSize(result),
///                 ResourceUtilization = GetWorkflowResourceUsage()
///             });
///             
///             return result;
///         }
///         catch (Exception ex)
///         {
///             _metrics.IncrementCounter("workflows.execution.failed", 1);
///             activity.SetError(ex.Message, ex);
///             throw;
///         }
///     }
/// }
/// 
/// // Monitoring and alerting integration
/// public class MetricsMonitoringService
/// {
///     private readonly IMetricsCollector _metrics;
///     
///     public MetricsMonitoringService(IMetricsCollector metrics)
///     {
///         _metrics = metrics;
///         
///         // Subscribe to real-time metrics events
///         _metrics.MetricsCollected += OnMetricsCollected;
///     }
///     
///     private void OnMetricsCollected(object? sender, MetricsCollectedEventArgs e)
///     {
///         // Real-time alerting based on metric values
///         if (e.MetricName == "documents.processing.failed" &amp;&amp; e.Value &gt; 10)
///         {
///             TriggerAlert("High document processing failure rate detected");
///         }
///         
///         if (e.MetricName.EndsWith("duration_ms") &amp;&amp; e.Value &gt; 30000)
///         {
///             TriggerAlert($"Slow operation detected: {e.MetricName} took {e.Value}ms");
///         }
///     }
///     
///     public void GenerateHealthReport()
///     {
///         var summary = _metrics.GetMetricsSummary();
///         var report = new HealthReport
///         {
///             Timestamp = DateTime.UtcNow,
///             SystemHealth = summary.OverallHealth,
///             Metrics = summary,
///             Recommendations = AnalyzeMetricsForRecommendations(summary)
///         };
///         
///         // Export to monitoring systems
///         var prometheusMetrics = _metrics.ExportMetrics(MetricsFormat.Prometheus);
///         var jsonMetrics = _metrics.ExportMetrics(MetricsFormat.Json);
///         
///         PublishToMonitoringSystems(prometheusMetrics, jsonMetrics);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AgentPerformanceMetrics"/>
/// <seealso cref="WorkflowMetrics"/>
/// <seealso cref="MetricsSummary"/>
/// <seealso cref="ActivitySpan"/>
public interface IMetricsCollector : IDisposable
{
    /// <summary>
    /// Increments a counter metric by the specified value, tracking cumulative events and rates.
    /// Counters are monotonically increasing and ideal for measuring event frequency and totals.
    /// </summary>
    /// <param name="name">
    /// The unique name of the counter metric following dot-notation conventions (e.g., "requests.total", "errors.validation").
    /// Should be descriptive and follow consistent naming patterns for effective monitoring.
    /// </param>
    /// <param name="value">
    /// The value to add to the counter. Defaults to 1.0 for simple event counting.
    /// Must be non-negative as counters can only increase.
    /// </param>
    /// <param name="tags">
    /// Optional dictionary of tags/labels to add dimensions to the metric for filtering and aggregation.
    /// Common tags include "environment", "service", "operation_type", "error_type", etc.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or contains invalid characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative.</exception>
    /// <remarks>
    /// <para>
    /// **Use Cases:**
    /// - Request counts, error counts, completion events
    /// - Rate calculations (requests per second, errors per minute)
    /// - Business metrics (users registered, documents processed)
    /// - System events (cache misses, database queries)
    /// </para>
    /// <para>
    /// **Best Practices:**
    /// - Use descriptive names with consistent dot notation
    /// - Include relevant tags for dimensional analysis
    /// - Increment by meaningful values (batch sizes, user counts)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple event counting
    /// _metrics.IncrementCounter("requests.received");
    /// _metrics.IncrementCounter("documents.processed");
    /// 
    /// // Batch processing with custom values
    /// _metrics.IncrementCounter("records.processed", batchSize, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["BatchType"] = "ImportUsers",
    ///     ["Source"] = "CSV"
    /// });
    /// 
    /// // Error tracking with categorization
    /// _metrics.IncrementCounter("errors.occurred", 1, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["ErrorType"] = ex.GetType().Name,
    ///     ["Severity"] = "Critical",
    ///     ["Component"] = "DocumentProcessor"
    /// });
    /// </code>
    /// </example>
    void IncrementCounter(string name, double value = 1.0, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a value in a histogram metric for statistical distribution analysis.
    /// Histograms track value distributions and enable percentile calculations, ideal for performance metrics.
    /// </summary>
    /// <param name="name">
    /// The unique name of the histogram metric. Should describe the measured quantity
    /// (e.g., "request.duration_ms", "payload.size_bytes", "queue.wait_time_ms").
    /// </param>
    /// <param name="value">
    /// The measured value to record in the histogram. Can be positive or negative
    /// depending on the metric type (durations, sizes, temperatures, etc.).
    /// </param>
    /// <param name="tags">
    /// Optional dictionary of tags/labels for metric dimensions. Enables analysis
    /// across different categories, services, or operational contexts.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or invalid.</exception>
    /// <remarks>
    /// <para>
    /// **Statistical Analysis:**
    /// Histograms automatically calculate percentiles (P50, P95, P99), mean, standard deviation,
    /// and value distribution across configurable buckets for comprehensive performance analysis.
    /// </para>
    /// <para>
    /// **Common Measurements:**
    /// - Response times and latencies
    /// - Request/response payload sizes  
    /// - Processing durations and queue wait times
    /// - Resource utilization metrics
    /// - Business value distributions (order amounts, user scores)
    /// </para>
    /// <para>
    /// **Performance Impact:**
    /// Histogram recording is optimized for high-frequency measurements with minimal overhead.
    /// Use appropriate sampling for extremely high-volume metrics if needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Response time tracking
    /// var stopwatch = Stopwatch.StartNew();
    /// await ProcessRequestAsync(request);
    /// _metrics.RecordValue("request.duration_ms", stopwatch.ElapsedMilliseconds, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["Endpoint"] = "/api/documents/process",
    ///     ["Method"] = "POST",
    ///     ["StatusCode"] = responseCode
    /// });
    /// 
    /// // Payload size monitoring
    /// _metrics.RecordValue("request.payload_size_bytes", requestSize, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["ContentType"] = "application/json",
    ///     ["Compressed"] = false
    /// });
    /// 
    /// // Business metrics
    /// _metrics.RecordValue("order.amount_usd", order.TotalAmount, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["Currency"] = order.Currency,
    ///     ["CustomerTier"] = customer.Tier,
    ///     ["PaymentMethod"] = order.PaymentType
    /// });
    /// </code>
    /// </example>
    void RecordValue(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Sets the current value of a gauge metric, representing point-in-time measurements.
    /// Gauges track current state and can increase or decrease, ideal for resource monitoring.
    /// </summary>
    /// <param name="name">
    /// The unique name of the gauge metric describing the measured resource or state
    /// (e.g., "memory.usage_bytes", "connections.active", "queue.length").
    /// </param>
    /// <param name="value">
    /// The current value to set for the gauge. Can increase, decrease, or remain constant
    /// based on the current state of the measured resource or system property.
    /// </param>
    /// <param name="tags">
    /// Optional dictionary of tags/labels for metric categorization and filtering.
    /// Useful for tracking gauges across multiple instances, services, or resource types.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when name is null, empty, or invalid.</exception>
    /// <remarks>
    /// <para>
    /// **Gauge Characteristics:**
    /// - Represents current state at measurement time
    /// - Values can go up, down, or stay the same
    /// - Latest value represents current system state
    /// - Ideal for resource utilization and capacity metrics
    /// </para>
    /// <para>
    /// **Common Use Cases:**
    /// - Memory and CPU usage percentages
    /// - Active connection counts and queue lengths
    /// - Available disk space and resource capacity
    /// - Current user sessions and active processes
    /// - Temperature, pressure, and environmental readings
    /// </para>
    /// <para>
    /// **Update Frequency:**
    /// Gauges should be updated regularly to provide accurate current state information.
    /// Consider automated collection for system resources and manual updates for business metrics.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // System resource monitoring
    /// var memoryUsage = GC.GetTotalMemory(false);
    /// _metrics.SetGaugeValue("memory.usage_bytes", memoryUsage, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["Process"] = Process.GetCurrentProcess().ProcessName,
    ///     ["Type"] = "ManagedMemory"
    /// });
    /// 
    /// // Active connection tracking
    /// _metrics.SetGaugeValue("connections.active", GetActiveConnectionCount(), new Dictionary&lt;string, object&gt;
    /// {
    ///     ["ConnectionType"] = "Database",
    ///     ["Pool"] = "Primary"
    /// });
    /// 
    /// // Queue length monitoring
    /// _metrics.SetGaugeValue("queue.length", processingQueue.Count, new Dictionary&lt;string, object&gt;
    /// {
    ///     ["QueueName"] = "DocumentProcessing",
    ///     ["Priority"] = "High"
    /// });
    /// 
    /// // Business state metrics
    /// _metrics.SetGaugeValue("users.active_sessions", GetActiveUserCount(), new Dictionary&lt;string, object&gt;
    /// {
    ///     ["Environment"] = "Production",
    ///     ["Region"] = "US-East"
    /// });
    /// </code>
    /// </example>
    void SetGaugeValue(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records comprehensive performance metrics for individual agent operations,
    /// providing detailed insights into agent efficiency, resource utilization, and execution patterns.
    /// </summary>
    /// <param name="metrics">
    /// Complete agent performance metrics including execution time, success rates,
    /// resource usage, and operational context for comprehensive performance analysis.
    /// </param>
    /// <remarks>
    /// This method records structured agent performance data that enables detailed analysis
    /// of individual agent behavior, performance trends, and resource consumption patterns.
    /// Essential for agent optimization and system capacity planning.
    /// </remarks>
    void RecordAgentMetrics(AgentPerformanceMetrics metrics);

    /// <summary>
    /// Records comprehensive workflow execution metrics including timing, step analysis,
    /// resource utilization, and success rates for workflow performance monitoring and optimization.
    /// </summary>
    /// <param name="metrics">
    /// Complete workflow metrics including execution timing, step counts, success rates,
    /// and resource utilization data for comprehensive workflow performance analysis.
    /// </param>
    /// <remarks>
    /// This method captures structured workflow performance data enabling analysis of
    /// workflow efficiency, bottleneck identification, and optimization opportunities
    /// across complex multi-step AI agent orchestration scenarios.
    /// </remarks>
    void RecordWorkflowMetrics(WorkflowMetrics metrics);

    /// <summary>
    /// Initiates a distributed tracing activity span for tracking operations across agent boundaries.
    /// Provides correlation context for request flow analysis and performance monitoring.
    /// </summary>
    /// <param name="operationName">
    /// Descriptive name for the operation being traced (e.g., "document-processing", "user-query-analysis").
    /// </param>
    /// <param name="tags">
    /// Optional tags providing additional context for the trace span including user IDs,
    /// operation types, and relevant business context for comprehensive tracing analysis.
    /// </param>
    /// <returns>
    /// An ActivitySpan that must be disposed when the operation completes to properly
    /// close the trace span and record timing information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// **Distributed Tracing:**
    /// Activities enable full request lifecycle tracking across multiple agents and services,
    /// providing correlation IDs and timing information essential for debugging and performance analysis.
    /// </para>
    /// <para>
    /// **Usage Pattern:**
    /// Always use activities within using statements to ensure proper disposal and span closure.
    /// Activities support nested spans for detailed operation breakdown and timing analysis.
    /// </para>
    /// </remarks>
    ActivitySpan StartActivity(string operationName, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Retrieves a comprehensive summary of current system metrics including counters, gauges,
    /// and statistical aggregations for monitoring dashboards and health assessments.
    /// </summary>
    /// <returns>
    /// A MetricsSummary containing current system state, performance indicators,
    /// and aggregated statistics for operational monitoring and alerting systems.
    /// </returns>
    /// <remarks>
    /// This method provides a point-in-time snapshot of system health and performance metrics,
    /// ideal for monitoring dashboards, health checks, and automated alerting systems.
    /// </remarks>
    MetricsSummary GetMetricsSummary();

    /// <summary>
    /// Exports current metrics in the specified format for integration with monitoring
    /// and observability platforms including Prometheus, Grafana, and custom systems.
    /// </summary>
    /// <param name="format">
    /// The desired export format (Prometheus, OpenMetrics, JSON, etc.) for compatibility
    /// with target monitoring and analysis systems.
    /// </param>
    /// <returns>
    /// A formatted string containing all current metrics in the specified format,
    /// ready for consumption by monitoring platforms and analysis tools.
    /// </returns>
    /// <remarks>
    /// <para>
    /// **Supported Formats:**
    /// Multiple export formats enable integration with diverse monitoring ecosystems
    /// and analysis tools, supporting both industry standards and custom requirements.
    /// </para>
    /// <para>
    /// **Export Frequency:**
    /// Consider caching and refresh intervals for high-frequency export scenarios
    /// to balance data freshness with system performance requirements.
    /// </para>
    /// </remarks>
    string ExportMetrics(MetricsFormat format);

    /// <summary>
    /// Resets all metric counters and clears accumulated data for testing scenarios
    /// and system maintenance operations. Use with caution in production environments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Reset Behavior:**
    /// - Counters reset to zero
    /// - Histograms clear all recorded values
    /// - Gauges retain their last set values (as they represent current state)
    /// - Trace activities and ongoing operations are not affected
    /// </para>
    /// <para>
    /// **Production Considerations:**
    /// Resetting metrics in production can cause data gaps in monitoring systems.
    /// Consider the impact on dashboards, alerting, and trend analysis before using.
    /// </para>
    /// </remarks>
    void Reset();

    /// <summary>
    /// Enables or disables metrics collection system-wide, providing runtime control
    /// over observability overhead for performance-sensitive scenarios.
    /// </summary>
    /// <param name="enabled">
    /// True to enable metrics collection and processing; false to disable collection
    /// and reduce system overhead for performance-critical operations.
    /// </param>
    /// <remarks>
    /// <para>
    /// **Performance Impact:**
    /// Disabling metrics collection can improve performance in high-throughput scenarios
    /// where observability overhead becomes significant relative to operation time.
    /// </para>
    /// <para>
    /// **Operational Considerations:**
    /// Disabling metrics affects monitoring, alerting, and troubleshooting capabilities.
    /// Consider the trade-off between performance and operational visibility.
    /// </para>
    /// </remarks>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Event triggered when metrics are collected, enabling real-time monitoring,
    /// custom alerting logic, and integration with external observability systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Real-time Processing:**
    /// This event enables immediate reaction to metric values for custom alerting,
    /// automatic scaling decisions, and real-time operational adjustments.
    /// </para>
    /// <para>
    /// **Event Handling:**
    /// Subscribers should process events quickly to avoid impacting system performance.
    /// Consider asynchronous processing for complex event handling logic.
    /// </para>
    /// </remarks>
    event EventHandler<MetricsCollectedEventArgs>? MetricsCollected;
}

/// <summary>
/// Event arguments containing detailed information about metrics collection events,
/// enabling real-time monitoring, alerting, and custom processing of metric data.
/// </summary>
/// <remarks>
/// <para>
/// MetricsCollectedEventArgs provides comprehensive context for each metric collection event,
/// enabling subscribers to implement real-time alerting, custom aggregation logic,
/// and integration with external monitoring and alerting systems.
/// </para>
/// <para>
/// **Event Processing:**
/// Events are fired synchronously during metric collection, so event handlers should
/// execute quickly to avoid impacting application performance. Consider asynchronous
/// processing for complex logic or external integrations.
/// </para>
/// </remarks>
public class MetricsCollectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the unique name of the collected metric, following the application's
    /// metric naming conventions and hierarchy.
    /// </summary>
    /// <value>
    /// The metric name used for identification, filtering, and routing in monitoring systems.
    /// </value>
    /// <remarks>
    /// Metric names should follow consistent dot notation patterns (e.g., "requests.duration_ms",
    /// "system.memory.usage_bytes") for effective organization and filtering.
    /// </remarks>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric value of the collected metric, representing the measured
    /// quantity or state at the time of collection.
    /// </summary>
    /// <value>
    /// The metric value as a double-precision number, suitable for all metric types.
    /// </value>
    /// <remarks>
    /// The interpretation of this value depends on the metric type: counters represent
    /// incremental values, gauges represent current state, and histograms represent
    /// individual measurements for distribution analysis.
    /// </remarks>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the collection of tags/labels associated with this metric,
    /// providing dimensional data for filtering, grouping, and analysis.
    /// </summary>
    /// <value>
    /// A dictionary of tag key-value pairs providing additional context for the metric.
    /// </value>
    /// <remarks>
    /// Tags enable dimensional analysis of metrics, allowing filtering and aggregation
    /// across different categories, services, environments, or business dimensions.
    /// Common tags include environment, service, operation_type, and error_type.
    /// </remarks>
    public Dictionary<string, object> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC timestamp indicating when this metric was collected,
    /// providing temporal context for time-series analysis and correlation.
    /// </summary>
    /// <value>
    /// A DateTime in UTC representing the exact moment of metric collection.
    /// </value>
    /// <remarks>
    /// Accurate timestamps are essential for time-series databases, trend analysis,
    /// and correlation with other system events. All timestamps should use UTC
    /// to ensure consistency across distributed systems and time zones.
    /// </remarks>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the type of metric being collected, indicating how the value
    /// should be interpreted and processed by monitoring systems.
    /// </summary>
    /// <value>
    /// The MetricType enumeration value indicating the metric's semantic type.
    /// </value>
    /// <remarks>
    /// The metric type determines how monitoring systems should aggregate, display,
    /// and alert on the metric values. Different types have different mathematical
    /// properties and use cases in observability scenarios.
    /// </remarks>
    public MetricType Type { get; set; }
}

/// <summary>
/// Defines the fundamental types of metrics supported by the observability system,
/// each with distinct mathematical properties, use cases, and aggregation behaviors
/// in monitoring and alerting scenarios.
/// </summary>
/// <remarks>
/// <para>
/// MetricType categorizes measurements based on their semantic meaning and mathematical
/// properties, enabling monitoring systems to apply appropriate aggregation, visualization,
/// and alerting strategies. Understanding metric types is essential for effective
/// observability design and accurate performance analysis.
/// </para>
/// <para>
/// **Type Selection Guidelines:**
/// - Use Counter for events and cumulative measurements
/// - Use Histogram for latency, size, and duration measurements
/// - Use Gauge for current state and resource utilization
/// - Use Summary for pre-calculated statistical aggregations
/// </para>
/// </remarks>
public enum MetricType
{
    /// <summary>
    /// Represents a monotonically increasing counter that tracks cumulative events or quantities.
    /// Counters can only increase (or reset to zero) and are ideal for measuring rates and totals.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Characteristics:**
    /// - Values only increase over time (monotonic)
    /// - Ideal for counting events, requests, errors, completions
    /// - Rate calculations are primary analysis method
    /// - Resets to zero on application restart
    /// </para>
    /// <para>
    /// **Common Examples:**
    /// - Total HTTP requests received
    /// - Total errors encountered
    /// - Bytes transferred or processed
    /// - Database queries executed
    /// </para>
    /// <para>
    /// **Monitoring Patterns:**
    /// Counters are typically analyzed using rate functions (requests/second, errors/minute)
    /// and are essential for SLA monitoring, capacity planning, and trend analysis.
    /// </para>
    /// </remarks>
    Counter,

    /// <summary>
    /// Represents a histogram that captures the distribution of measured values across configurable buckets.
    /// Histograms enable statistical analysis including percentiles, averages, and distribution patterns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Characteristics:**
    /// - Captures value distributions across predefined buckets
    /// - Enables percentile calculations (P50, P95, P99)
    /// - Supports mean, standard deviation, and bucket analysis
    /// - Higher memory usage due to bucket storage requirements
    /// </para>
    /// <para>
    /// **Common Examples:**
    /// - HTTP response latencies and processing times
    /// - Request payload sizes and response sizes
    /// - Queue wait times and processing durations
    /// - Resource utilization measurements
    /// </para>
    /// <para>
    /// **Analysis Capabilities:**
    /// Histograms provide rich statistical analysis including tail latency detection,
    /// SLA compliance measurement, and performance trend identification across
    /// different percentile thresholds.
    /// </para>
    /// </remarks>
    Histogram,

    /// <summary>
    /// Represents a gauge that tracks the current value of a quantity that can increase or decrease.
    /// Gauges capture point-in-time measurements and current system state information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Characteristics:**
    /// - Values can increase, decrease, or remain constant
    /// - Represents current state at measurement time
    /// - Latest value is most significant for analysis
    /// - No inherent mathematical constraints on value changes
    /// </para>
    /// <para>
    /// **Common Examples:**
    /// - Current memory usage and CPU utilization
    /// - Active database connections and queue lengths
    /// - Current user session counts
    /// - Available disk space and resource capacity
    /// </para>
    /// <para>
    /// **Monitoring Patterns:**
    /// Gauges are essential for capacity monitoring, resource alerting, and system
    /// health dashboards where current state visibility is critical for operations.
    /// </para>
    /// </remarks>
    Gauge,

    /// <summary>
    /// Represents a summary metric that provides pre-calculated statistical aggregations
    /// including quantiles, totals, and counts for efficient high-level analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **Characteristics:**
    /// - Pre-calculated statistical summaries (mean, quantiles, count)
    /// - Lower storage requirements compared to histograms
    /// - Configurable quantile calculations (0.5, 0.95, 0.99)
    /// - Efficient for high-volume metric scenarios
    /// </para>
    /// <para>
    /// **Common Examples:**
    /// - Request latency summaries with P95/P99 quantiles
    /// - Batch processing summaries with count and duration
    /// - Resource utilization summaries across time windows
    /// - Business metric summaries (revenue, conversion rates)
    /// </para>
    /// <para>
    /// **Trade-offs:**
    /// Summaries offer better performance and storage efficiency than histograms
    /// but with less flexibility for ad-hoc analysis and custom aggregations
    /// since statistics are pre-calculated at collection time.
    /// </para>
    /// </remarks>
    Summary
}