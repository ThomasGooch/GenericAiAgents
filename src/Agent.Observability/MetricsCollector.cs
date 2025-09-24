using Agent.Observability.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Agent.Observability;

/// <summary>
/// Default implementation of metrics collector
/// </summary>
public class MetricsCollector : IMetricsCollector
{
    private readonly ConcurrentDictionary<string, double> _counters = new();
    private readonly ConcurrentDictionary<string, List<double>> _histograms = new();
    private readonly ConcurrentDictionary<string, double> _gauges = new();
    private readonly ActivitySource _activitySource = new("Agent.System");
    private readonly object _metricsLock = new();
    
    private bool _enabled = true;
    private bool _disposed = false;
    private long _totalRequests = 0;
    private long _failedRequests = 0;
    private readonly List<double> _responseTimes = new();

    public event EventHandler<MetricsCollectedEventArgs>? MetricsCollected;

    public MetricsCollector() : this(null)
    {
    }

    internal MetricsCollector(object? testProvider)
    {
        // Test constructor for dependency injection
    }

    public void IncrementCounter(string name, double value = 1.0, Dictionary<string, object>? tags = null)
    {
        if (!_enabled) return;

        _counters.AddOrUpdate(name, value, (key, existingValue) => existingValue + value);
        
        OnMetricsCollected(new MetricsCollectedEventArgs
        {
            MetricName = name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Type = MetricType.Counter
        });
    }

    public void RecordValue(string name, double value, Dictionary<string, object>? tags = null)
    {
        if (!_enabled) return;

        _histograms.AddOrUpdate(name, [value], (key, existingValues) =>
        {
            lock (_metricsLock)
            {
                existingValues.Add(value);
                return existingValues;
            }
        });

        OnMetricsCollected(new MetricsCollectedEventArgs
        {
            MetricName = name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Type = MetricType.Histogram
        });
    }

    public void SetGaugeValue(string name, double value, Dictionary<string, object>? tags = null)
    {
        if (!_enabled) return;

        _gauges[name] = value;
        
        OnMetricsCollected(new MetricsCollectedEventArgs
        {
            MetricName = name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Type = MetricType.Gauge
        });
    }

    public void RecordAgentMetrics(AgentPerformanceMetrics metrics)
    {
        if (!_enabled) return;

        var tags = new Dictionary<string, object>
        {
            ["agent_id"] = metrics.AgentId
        };

        // Record execution time
        RecordValue("agent.execution.duration", metrics.ExecutionTime.TotalMilliseconds, tags);
        
        // Record success/failure
        if (metrics.Success)
        {
            IncrementCounter("agent.execution.success", 1, tags);
        }
        else
        {
            IncrementCounter("agent.execution.failure", 1, tags);
        }

        // Record resource usage
        SetGaugeValue("agent.memory.usage", metrics.MemoryUsage, tags);
        SetGaugeValue("agent.cpu.usage", metrics.CpuUsage * 100, tags);

        // Track overall system metrics
        Interlocked.Increment(ref _totalRequests);
        if (!metrics.Success)
        {
            Interlocked.Increment(ref _failedRequests);
        }

        lock (_metricsLock)
        {
            _responseTimes.Add(metrics.ExecutionTime.TotalMilliseconds);
            
            // Keep only last 1000 response times to prevent memory growth
            if (_responseTimes.Count > 1000)
            {
                _responseTimes.RemoveAt(0);
            }
        }
    }

    public void RecordWorkflowMetrics(WorkflowMetrics metrics)
    {
        if (!_enabled) return;

        var tags = new Dictionary<string, object>
        {
            ["workflow_id"] = metrics.WorkflowId.ToString()
        };

        // Record step counts
        IncrementCounter("workflow.steps.total", metrics.TotalSteps, tags);
        IncrementCounter("workflow.steps.completed", metrics.CompletedSteps, tags);
        IncrementCounter("workflow.steps.failed", metrics.FailedSteps, tags);

        // Record execution time
        RecordValue("workflow.execution.duration", metrics.ExecutionTime.TotalMilliseconds, tags);
        
        // Record average step time
        if (metrics.CompletedSteps > 0)
        {
            RecordValue("workflow.step.average.duration", metrics.AverageStepTime.TotalMilliseconds, tags);
        }

        // Record success/failure
        if (metrics.Success)
        {
            IncrementCounter("workflow.execution.success", 1, tags);
        }
        else
        {
            IncrementCounter("workflow.execution.failure", 1, tags);
        }
    }

    public ActivitySpan StartActivity(string operationName, Dictionary<string, object>? tags = null)
    {
        var activity = _activitySource.StartActivity(operationName);
        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value?.ToString());
            }
        }

        return new ActivitySpan(activity ?? Activity.Current ?? new Activity(operationName));
    }

    public MetricsSummary GetMetricsSummary()
    {
        lock (_metricsLock)
        {
            var summary = new MetricsSummary
            {
                TotalRequests = _totalRequests,
                FailedRequests = _failedRequests,
                ActiveConnections = (int)(_gauges.GetValueOrDefault("active.connections", 0)),
                MemoryUsage = (long)_gauges.GetValueOrDefault("system.memory.usage", 0),
                CpuUsage = _gauges.GetValueOrDefault("system.cpu.usage", 0)
            };

            if (_responseTimes.Any())
            {
                summary.AverageResponseTime = _responseTimes.Average();
            }

            // Add additional metrics from gauges
            foreach (var gauge in _gauges)
            {
                summary.AdditionalMetrics[gauge.Key] = gauge.Value;
            }

            return summary;
        }
    }

    public string ExportMetrics(MetricsFormat format)
    {
        return format switch
        {
            MetricsFormat.Prometheus => ExportPrometheus(),
            MetricsFormat.Json => ExportJson(),
            MetricsFormat.OpenTelemetry => ExportOpenTelemetry(),
            MetricsFormat.Text => ExportText(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public void Reset()
    {
        lock (_metricsLock)
        {
            _counters.Clear();
            _histograms.Clear();
            _gauges.Clear();
            _responseTimes.Clear();
            _totalRequests = 0;
            _failedRequests = 0;
        }
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    private string ExportPrometheus()
    {
        var sb = new StringBuilder();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Export counters
        foreach (var counter in _counters)
        {
            var metricName = counter.Key.Replace('.', '_').Replace('-', '_');
            sb.AppendLine($"# TYPE {metricName} counter");
            sb.AppendLine($"{metricName} {counter.Value} {timestamp}");
        }

        // Export gauges
        foreach (var gauge in _gauges)
        {
            var metricName = gauge.Key.Replace('.', '_').Replace('-', '_');
            sb.AppendLine($"# TYPE {metricName} gauge");
            sb.AppendLine($"{metricName} {gauge.Value} {timestamp}");
        }

        // Export histograms (simplified)
        foreach (var histogram in _histograms)
        {
            var metricName = histogram.Key.Replace('.', '_').Replace('-', '_');
            lock (_metricsLock)
            {
                if (histogram.Value.Any())
                {
                    sb.AppendLine($"# TYPE {metricName} histogram");
                    sb.AppendLine($"{metricName}_count {histogram.Value.Count} {timestamp}");
                    sb.AppendLine($"{metricName}_sum {histogram.Value.Sum()} {timestamp}");
                }
            }
        }

        return sb.ToString();
    }

    private string ExportJson()
    {
        var metrics = new
        {
            timestamp = DateTime.UtcNow,
            counters = _counters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            gauges = _gauges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            histograms = _histograms.ToDictionary(kvp => kvp.Key, kvp => new
            {
                count = kvp.Value.Count,
                sum = kvp.Value.Sum(),
                min = kvp.Value.Any() ? kvp.Value.Min() : 0,
                max = kvp.Value.Any() ? kvp.Value.Max() : 0,
                avg = kvp.Value.Any() ? kvp.Value.Average() : 0
            })
        };

        return JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
    }

    private string ExportOpenTelemetry()
    {
        // Simplified OpenTelemetry format
        return ExportJson(); // For now, use JSON format
    }

    private string ExportText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Metrics Export - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        sb.AppendLine("Counters:");
        foreach (var counter in _counters.OrderBy(c => c.Key))
        {
            sb.AppendLine($"  {counter.Key}: {counter.Value}");
        }

        sb.AppendLine();
        sb.AppendLine("Gauges:");
        foreach (var gauge in _gauges.OrderBy(g => g.Key))
        {
            sb.AppendLine($"  {gauge.Key}: {gauge.Value}");
        }

        sb.AppendLine();
        sb.AppendLine("Histograms:");
        foreach (var histogram in _histograms.OrderBy(h => h.Key))
        {
            lock (_metricsLock)
            {
                if (histogram.Value.Any())
                {
                    sb.AppendLine($"  {histogram.Key}:");
                    sb.AppendLine($"    Count: {histogram.Value.Count}");
                    sb.AppendLine($"    Sum: {histogram.Value.Sum():F2}");
                    sb.AppendLine($"    Min: {histogram.Value.Min():F2}");
                    sb.AppendLine($"    Max: {histogram.Value.Max():F2}");
                    sb.AppendLine($"    Avg: {histogram.Value.Average():F2}");
                }
            }
        }

        return sb.ToString();
    }

    private void OnMetricsCollected(MetricsCollectedEventArgs args)
    {
        MetricsCollected?.Invoke(this, args);
    }

    // Internal methods for testing
    internal double GetCounterValue(string name)
    {
        return _counters.GetValueOrDefault(name, 0);
    }
    
    internal List<double> GetHistogramValues(string name)
    {
        lock (_metricsLock)
        {
            return _histograms.GetValueOrDefault(name, new List<double>()).ToList();
        }
    }
    
    internal double GetGaugeValue(string name)
    {
        return _gauges.GetValueOrDefault(name, 0);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _activitySource?.Dispose();
            _disposed = true;
        }
    }
}