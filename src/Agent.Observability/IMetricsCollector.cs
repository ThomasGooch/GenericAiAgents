using Agent.Observability.Models;
using System.Diagnostics;

namespace Agent.Observability;

/// <summary>
/// Interface for collecting and managing system metrics
/// </summary>
public interface IMetricsCollector : IDisposable
{
    /// <summary>
    /// Increment a counter metric
    /// </summary>
    /// <param name="name">Counter name</param>
    /// <param name="value">Value to add</param>
    /// <param name="tags">Optional tags for the metric</param>
    void IncrementCounter(string name, double value = 1.0, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Record a histogram value
    /// </summary>
    /// <param name="name">Histogram name</param>
    /// <param name="value">Value to record</param>
    /// <param name="tags">Optional tags for the metric</param>
    void RecordValue(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Set a gauge value
    /// </summary>
    /// <param name="name">Gauge name</param>
    /// <param name="value">Current value</param>
    /// <param name="tags">Optional tags for the metric</param>
    void SetGaugeValue(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Record agent performance metrics
    /// </summary>
    /// <param name="metrics">Agent performance metrics</param>
    void RecordAgentMetrics(AgentPerformanceMetrics metrics);

    /// <summary>
    /// Record workflow execution metrics
    /// </summary>
    /// <param name="metrics">Workflow metrics</param>
    void RecordWorkflowMetrics(WorkflowMetrics metrics);

    /// <summary>
    /// Start an activity for distributed tracing
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="tags">Optional tags for the activity</param>
    /// <returns>Activity span that should be disposed when operation completes</returns>
    ActivitySpan StartActivity(string operationName, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Get current metrics summary
    /// </summary>
    /// <returns>Current system metrics</returns>
    MetricsSummary GetMetricsSummary();

    /// <summary>
    /// Export metrics in specified format
    /// </summary>
    /// <param name="format">Export format</param>
    /// <returns>Metrics in requested format</returns>
    string ExportMetrics(MetricsFormat format);

    /// <summary>
    /// Reset all metrics counters
    /// </summary>
    void Reset();

    /// <summary>
    /// Enable or disable metrics collection
    /// </summary>
    /// <param name="enabled">Whether to enable metrics collection</param>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Event fired when metrics are collected
    /// </summary>
    event EventHandler<MetricsCollectedEventArgs>? MetricsCollected;
}

/// <summary>
/// Event arguments for metrics collection events
/// </summary>
public class MetricsCollectedEventArgs : EventArgs
{
    /// <summary>
    /// Name of the metric
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Metric value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Metric tags
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();

    /// <summary>
    /// When the metric was collected
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of metric
    /// </summary>
    public MetricType Type { get; set; }
}

/// <summary>
/// Types of metrics
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Counter metric (monotonically increasing)
    /// </summary>
    Counter,

    /// <summary>
    /// Histogram metric (distribution of values)
    /// </summary>
    Histogram,

    /// <summary>
    /// Gauge metric (current value)
    /// </summary>
    Gauge,

    /// <summary>
    /// Summary metric (statistical summary)
    /// </summary>
    Summary
}