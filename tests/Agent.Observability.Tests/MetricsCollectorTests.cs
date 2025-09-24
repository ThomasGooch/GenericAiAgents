using Agent.Observability;
using Agent.Observability.Models;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Agent.Observability.Tests;

public class MetricsCollectorTests : IDisposable
{
    private readonly MetricsCollector _metricsCollector;

    public MetricsCollectorTests()
    {
        _metricsCollector = new MetricsCollector();
    }

    [Fact]
    public void IncrementCounter_ShouldRecordCounterValue()
    {
        // Arrange
        var counterName = "test.counter";
        var tags = new Dictionary<string, object> { ["environment"] = "test" };

        // Act
        _metricsCollector.IncrementCounter(counterName, 1, tags);
        _metricsCollector.IncrementCounter(counterName, 2, tags);

        // Assert
        var totalValue = _metricsCollector.GetCounterValue(counterName);
        Assert.Equal(3, totalValue); // 1 + 2 = 3 (counters are cumulative)
    }

    [Fact]
    public void RecordValue_ShouldRecordHistogramValue()
    {
        // Arrange
        var histogramName = "test.duration";
        var value1 = 100.5;
        var value2 = 250.0;

        // Act
        _metricsCollector.RecordValue(histogramName, value1);
        _metricsCollector.RecordValue(histogramName, value2);

        // Assert
        var values = _metricsCollector.GetHistogramValues(histogramName);
        Assert.Equal(2, values.Count);
        Assert.Contains(value1, values);
        Assert.Contains(value2, values);
    }

    [Fact]
    public void SetGaugeValue_ShouldUpdateGaugeValue()
    {
        // Arrange
        var gaugeName = "test.memory.usage";
        var initialValue = 1024.0;
        var updatedValue = 2048.0;

        // Act
        _metricsCollector.SetGaugeValue(gaugeName, initialValue);
        _metricsCollector.SetGaugeValue(gaugeName, updatedValue);

        // Assert
        var currentValue = _metricsCollector.GetGaugeValue(gaugeName);
        Assert.Equal(updatedValue, currentValue);
    }

    [Fact]
    public void RecordAgentMetrics_ShouldCaptureAgentPerformanceData()
    {
        // Arrange
        var agentId = "test-agent";
        var metrics = new AgentPerformanceMetrics
        {
            AgentId = agentId,
            ExecutionTime = TimeSpan.FromMilliseconds(150),
            Success = true,
            MemoryUsage = 1024 * 1024, // 1MB
            CpuUsage = 0.25 // 25%
        };

        // Act
        _metricsCollector.RecordAgentMetrics(metrics);

        // Assert
        var executionTimes = _metricsCollector.GetHistogramValues("agent.execution.duration");
        Assert.Single(executionTimes);
        Assert.Equal(150.0, executionTimes[0]);

        var successCounter = _metricsCollector.GetCounterValue("agent.execution.success");
        Assert.Equal(1, successCounter);

        var memoryGauge = _metricsCollector.GetGaugeValue("agent.memory.usage");
        Assert.Equal(1024 * 1024, memoryGauge);
    }

    [Fact]
    public void RecordWorkflowMetrics_ShouldCaptureWorkflowData()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var metrics = new WorkflowMetrics
        {
            WorkflowId = workflowId,
            TotalSteps = 5,
            CompletedSteps = 3,
            FailedSteps = 1,
            ExecutionTime = TimeSpan.FromSeconds(30),
            Success = false
        };

        // Act
        _metricsCollector.RecordWorkflowMetrics(metrics);

        // Assert
        var stepsCompleted = _metricsCollector.GetCounterValue("workflow.steps.completed");
        Assert.Equal(3, stepsCompleted);

        var stepsFailed = _metricsCollector.GetCounterValue("workflow.steps.failed");
        Assert.Equal(1, stepsFailed);

        var executionTimes = _metricsCollector.GetHistogramValues("workflow.execution.duration");
        Assert.Single(executionTimes);
        Assert.Equal(30000.0, executionTimes[0]); // 30 seconds in milliseconds
    }

    [Fact]
    public void GetMetricsSummary_ShouldReturnAggregatedMetrics()
    {
        // Arrange - Record agent metrics to populate summary fields
        var successfulAgent = new AgentPerformanceMetrics
        {
            AgentId = "agent-1",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            Success = true,
            MemoryUsage = 1024,
            CpuUsage = 0.25
        };
        
        var failedAgent = new AgentPerformanceMetrics
        {
            AgentId = "agent-2", 
            ExecutionTime = TimeSpan.FromMilliseconds(200),
            Success = false,
            MemoryUsage = 2048,
            CpuUsage = 0.5
        };

        _metricsCollector.RecordAgentMetrics(successfulAgent);
        _metricsCollector.RecordAgentMetrics(failedAgent);
        _metricsCollector.SetGaugeValue("active.connections", 5);

        // Act
        var summary = _metricsCollector.GetMetricsSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.True(summary.TotalRequests > 0);
        Assert.True(summary.FailedRequests > 0);
        Assert.True(summary.AverageResponseTime > 0);
        Assert.True(summary.ActiveConnections >= 0);
        Assert.True(summary.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void StartActivitySpan_ShouldCreateAndTrackActivity()
    {
        // Arrange
        var operationName = "test.operation";
        var tags = new Dictionary<string, object> { ["user_id"] = "123" };

        // Act
        using var activity = _metricsCollector.StartActivity(operationName, tags);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(operationName, activity.OperationName);
        Assert.True(activity.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public void ExportMetrics_ShouldReturnMetricsInSpecifiedFormat()
    {
        // Arrange
        _metricsCollector.IncrementCounter("test.metric", 5);
        _metricsCollector.RecordValue("test.histogram", 123.45);

        // Act
        var prometheusExport = _metricsCollector.ExportMetrics(MetricsFormat.Prometheus);
        var jsonExport = _metricsCollector.ExportMetrics(MetricsFormat.Json);

        // Assert
        Assert.NotNull(prometheusExport);
        Assert.NotNull(jsonExport);
        Assert.Contains("test_metric", prometheusExport);
        Assert.Contains("test_histogram", prometheusExport); // Prometheus format converts dots to underscores
        Assert.Contains("\"test.metric\"", jsonExport);
        Assert.Contains("123.45", jsonExport);
    }

    public void Dispose()
    {
        _metricsCollector?.Dispose();
    }
}