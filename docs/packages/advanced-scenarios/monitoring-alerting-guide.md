# 📊 Production Monitoring & Alerting Setup Guide

## Table of Contents

1. [Monitoring Architecture](#monitoring-architecture)
2. [Metrics Collection Setup](#metrics-collection-setup)
3. [Health Check Configuration](#health-check-configuration)
4. [Alerting Strategy](#alerting-strategy)
5. [Dashboard Configuration](#dashboard-configuration)
6. [Log Aggregation](#log-aggregation)
7. [Performance Monitoring](#performance-monitoring)
8. [Troubleshooting Playbooks](#troubleshooting-playbooks)

## Monitoring Architecture

### Enterprise Monitoring Stack
Complete observability solution for production agent systems.

```csharp
public class ProductionMonitoringSetup
{
    public static IServiceCollection AddProductionMonitoring(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Core monitoring services
        services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();
        services.AddSingleton<IHealthCheckService, EnterpriseHealthCheckService>();
        services.AddSingleton<IAlertingService, AlertingService>();
        services.AddSingleton<IDashboardService, GrafanaDashboardService>();
        
        // Distributed tracing
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("GenericAgents.*")
                .SetSampler(new TraceIdRatioBasedSampler(0.1))
                .AddJaegerExporter(options =>
                {
                    options.AgentHost = configuration["Jaeger:AgentHost"];
                    options.AgentPort = configuration.GetValue<int>("Jaeger:AgentPort");
                })
            );
        
        // Structured logging
        services.AddLogging(builder =>
        {
            builder.AddStructuredConsole();
            builder.AddElasticsearch(configuration.GetConnectionString("Elasticsearch"));
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Application Insights (Azure)
        if (!string.IsNullOrEmpty(configuration["ApplicationInsights:InstrumentationKey"]))
        {
            services.AddApplicationInsightsTelemetry(configuration["ApplicationInsights:InstrumentationKey"]);
        }
        
        // Custom monitoring components
        services.AddSingleton<IBusinessMetricsCollector, BusinessMetricsCollector>();
        services.AddSingleton<IPerformanceAnalyzer, PerformanceAnalyzer>();
        services.AddSingleton<IAnomalyDetector, AnomalyDetector>();
        
        return services;
    }
}
```

### Monitoring Configuration
Environment-specific monitoring setup with proper resource allocation.

```json
{
  "Monitoring": {
    "MetricsInterval": "00:00:30",
    "HealthCheckInterval": "00:01:00",
    "RetentionDays": 30,
    "AlertingEnabled": true,
    "DashboardRefreshRate": "00:00:10"
  },
  "Prometheus": {
    "Endpoint": "http://localhost:9090",
    "PushGateway": "http://localhost:9091",
    "ScrapeInterval": "15s",
    "MetricsPath": "/metrics"
  },
  "Grafana": {
    "Endpoint": "http://localhost:3000",
    "ApiKey": "${GRAFANA_API_KEY}",
    "OrganizationId": 1
  },
  "Alerting": {
    "Slack": {
      "WebhookUrl": "${SLACK_WEBHOOK_URL}",
      "Channel": "#operations-alerts",
      "Username": "GenericAgents Monitor"
    },
    "PagerDuty": {
      "IntegrationKey": "${PAGERDUTY_INTEGRATION_KEY}",
      "Severity": "critical"
    },
    "Email": {
      "SmtpHost": "smtp.company.com",
      "SmtpPort": 587,
      "Recipients": ["ops-team@company.com"]
    }
  }
}
```

## Metrics Collection Setup

### Comprehensive Metrics Strategy
Enterprise-grade metrics covering all aspects of agent system performance.

```csharp
public class EnterpriseMetricsCollector : IMetricsCollector
{
    private readonly IMetricsRoot _metrics;
    private readonly ILogger<EnterpriseMetricsCollector> _logger;
    private readonly Timer _businessMetricsTimer;

    public EnterpriseMetricsCollector(IMetricsRoot metrics, ILogger<EnterpriseMetricsCollector> logger)
    {
        _metrics = metrics;
        _logger = logger;
        
        // Collect business metrics every 60 seconds
        _businessMetricsTimer = new Timer(CollectBusinessMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        
        SetupCustomMetrics();
    }

    private void SetupCustomMetrics()
    {
        // Agent performance metrics
        _metrics.Measure.Gauge.SetValue(AgentMetrics.ActiveAgents, () => GetActiveAgentCount());
        _metrics.Measure.Gauge.SetValue(AgentMetrics.TotalMemoryUsage, () => GC.GetTotalMemory(false));
        _metrics.Measure.Gauge.SetValue(AgentMetrics.ThreadPoolThreads, () => ThreadPool.ThreadCount);
        
        // Business metrics
        _metrics.Measure.Counter.Increment(BusinessMetrics.ProcessedDocuments, 0);
        _metrics.Measure.Counter.Increment(BusinessMetrics.SuccessfulTransactions, 0);
        _metrics.Measure.Counter.Increment(BusinessMetrics.FailedTransactions, 0);
        
        // System health metrics
        _metrics.Measure.Gauge.SetValue(SystemMetrics.CpuUsage, () => GetCpuUsage());
        _metrics.Measure.Gauge.SetValue(SystemMetrics.DiskUsage, () => GetDiskUsage());
        _metrics.Measure.Gauge.SetValue(SystemMetrics.NetworkConnections, () => GetNetworkConnections());
    }

    public void RecordAgentExecution(AgentExecutionMetrics metrics)
    {
        var tags = new MetricTags(
            new[] { "agent_id", "agent_type", "success", "environment" },
            new[] { metrics.AgentId, metrics.AgentType, metrics.Success.ToString(), Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown" }
        );

        // Execution time histogram
        _metrics.Measure.Histogram.Update(
            AgentMetrics.ExecutionTime,
            tags,
            metrics.ExecutionTime.TotalMilliseconds
        );

        // Success/failure counters
        if (metrics.Success)
        {
            _metrics.Measure.Counter.Increment(AgentMetrics.SuccessfulExecutions, tags);
        }
        else
        {
            _metrics.Measure.Counter.Increment(AgentMetrics.FailedExecutions, tags);
            
            // Track error types
            var errorTags = tags.With("error_type", metrics.ErrorType ?? "Unknown");
            _metrics.Measure.Counter.Increment(AgentMetrics.ErrorsByType, errorTags);
        }

        // Resource usage
        _metrics.Measure.Gauge.SetValue(AgentMetrics.MemoryUsage, tags, metrics.MemoryUsage);
        _metrics.Measure.Gauge.SetValue(AgentMetrics.CpuUsage, tags, metrics.CpuUsage);
    }

    private void CollectBusinessMetrics(object? state)
    {
        try
        {
            // Collect real-time business metrics
            var businessMetrics = GetCurrentBusinessMetrics();
            
            _metrics.Measure.Gauge.SetValue(BusinessMetrics.ActiveUsers, businessMetrics.ActiveUsers);
            _metrics.Measure.Gauge.SetValue(BusinessMetrics.QueueDepth, businessMetrics.QueueDepth);
            _metrics.Measure.Gauge.SetValue(BusinessMetrics.ProcessingRate, businessMetrics.ProcessingRate);
            _metrics.Measure.Gauge.SetValue(BusinessMetrics.ErrorRate, businessMetrics.ErrorRate);
            
            // SLA metrics
            _metrics.Measure.Gauge.SetValue(BusinessMetrics.SlaCompliance, businessMetrics.SlaCompliance);
            _metrics.Measure.Histogram.Update(BusinessMetrics.ResponseTime, businessMetrics.AverageResponseTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting business metrics");
        }
    }
}

// Metric definitions
public static class AgentMetrics
{
    public static readonly GaugeOptions ActiveAgents = new()
    {
        Name = "agents_active_total",
        MeasurementUnit = Unit.Items,
        Context = "Agents"
    };

    public static readonly HistogramOptions ExecutionTime = new()
    {
        Name = "agent_execution_duration_ms",
        MeasurementUnit = Unit.Custom("milliseconds"),
        Context = "Agents"
    };

    public static readonly CounterOptions SuccessfulExecutions = new()
    {
        Name = "agent_executions_success_total",
        MeasurementUnit = Unit.Calls,
        Context = "Agents"
    };

    public static readonly CounterOptions FailedExecutions = new()
    {
        Name = "agent_executions_failed_total",
        MeasurementUnit = Unit.Calls,
        Context = "Agents"
    };
}

public static class BusinessMetrics
{
    public static readonly GaugeOptions ProcessingRate = new()
    {
        Name = "business_processing_rate_per_minute",
        MeasurementUnit = Unit.Custom("items/minute"),
        Context = "Business"
    };

    public static readonly GaugeOptions SlaCompliance = new()
    {
        Name = "business_sla_compliance_percentage", 
        MeasurementUnit = Unit.Percent,
        Context = "Business"
    };

    public static readonly CounterOptions ProcessedDocuments = new()
    {
        Name = "business_documents_processed_total",
        MeasurementUnit = Unit.Items,
        Context = "Business"
    };
}
```

## Health Check Configuration

### Multi-Layer Health Monitoring
Comprehensive health checks for all system components.

```csharp
public class ProductionHealthChecks
{
    public static IServiceCollection AddProductionHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Database health
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                name: "database",
                timeout: TimeSpan.FromSeconds(5),
                tags: new[] { "critical", "database" })
            
            // Redis cache health
            .AddRedis(
                configuration.GetConnectionString("Redis"),
                name: "redis-cache",
                timeout: TimeSpan.FromSeconds(3),
                tags: new[] { "important", "cache" })
            
            // External service health
            .AddUrlGroup(
                new Uri($"{configuration["ExternalServices:PaymentGateway:BaseUrl"]}/health"),
                name: "payment-gateway",
                timeout: TimeSpan.FromSeconds(10),
                tags: new[] { "external", "payment" })
            
            // Custom business health checks
            .AddCheck<AgentRegistryHealthCheck>("agent-registry", tags: new[] { "critical", "agents" })
            .AddCheck<WorkflowEngineHealthCheck>("workflow-engine", tags: new[] { "critical", "workflows" })
            .AddCheck<MessageQueueHealthCheck>("message-queue", tags: new[] { "important", "messaging" })
            
            // System resource health
            .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: new[] { "system", "resources" })
            .AddCheck<MemoryHealthCheck>("memory-usage", tags: new[] { "system", "resources" })
            .AddCheck<CpuHealthCheck>("cpu-usage", tags: new[] { "system", "resources" });

        // Health check UI for monitoring dashboard
        services.AddHealthChecksUI(setup =>
        {
            setup.AddHealthCheckEndpoint("GenericAgents API", "/health");
            setup.SetEvaluationTimeInSeconds(30);
            setup.MaximumHistoryEntriesPerEndpoint(100);
        })
        .AddInMemoryStorage(); // Use SQL Server in production

        return services;
    }
}

// Custom health check implementations
public class AgentRegistryHealthCheck : IHealthCheck
{
    private readonly IAgentRegistryEnhanced _agentRegistry;
    private readonly ILogger<AgentRegistryHealthCheck> _logger;

    public AgentRegistryHealthCheck(IAgentRegistryEnhanced agentRegistry, ILogger<AgentRegistryHealthCheck> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var agents = await _agentRegistry.GetAllAgentsAsync(cancellationToken);
            var healthyAgents = await _agentRegistry.GetHealthyAgentsAsync(cancellationToken);
            
            var totalAgents = agents.Count();
            var healthyCount = healthyAgents.Count();
            var healthPercentage = totalAgents > 0 ? (double)healthyCount / totalAgents * 100 : 0;

            var data = new Dictionary<string, object>
            {
                ["total_agents"] = totalAgents,
                ["healthy_agents"] = healthyCount,
                ["health_percentage"] = healthPercentage,
                ["check_time"] = DateTime.UtcNow
            };

            if (healthPercentage < 50)
            {
                return HealthCheckResult.Unhealthy($"Only {healthPercentage:F1}% of agents are healthy", null, data);
            }
            
            if (healthPercentage < 80)
            {
                return HealthCheckResult.Degraded($"{healthPercentage:F1}% of agents are healthy", null, data);
            }

            return HealthCheckResult.Healthy($"Agent registry is healthy ({healthyCount}/{totalAgents} agents)", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent registry health check failed");
            return HealthCheckResult.Unhealthy("Agent registry health check failed", ex);
        }
    }
}

public class WorkflowEngineHealthCheck : IHealthCheck
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IMetricsCollector _metrics;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Create a simple test workflow
            var testWorkflow = new WorkflowDefinition
            {
                Id = Guid.NewGuid(),
                Name = "Health Check Workflow",
                ExecutionMode = WorkflowExecutionMode.Sequential,
                Timeout = TimeSpan.FromSeconds(10),
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Health Check Step",
                        AgentId = "health-check-agent",
                        Input = new { message = "health check" },
                        Timeout = TimeSpan.FromSeconds(5)
                    }
                }
            };

            var result = await _workflowEngine.ExecuteWorkflowAsync(testWorkflow, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            var data = new Dictionary<string, object>
            {
                ["test_duration_ms"] = duration.TotalMilliseconds,
                ["test_successful"] = result.Success,
                ["check_time"] = DateTime.UtcNow
            };

            if (!result.Success)
            {
                return HealthCheckResult.Unhealthy("Workflow engine test execution failed", null, data);
            }

            if (duration > TimeSpan.FromSeconds(5))
            {
                return HealthCheckResult.Degraded($"Workflow engine responding slowly ({duration.TotalSeconds:F2}s)", null, data);
            }

            return HealthCheckResult.Healthy("Workflow engine is responding normally", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Workflow engine health check failed", ex);
        }
    }
}
```

## Alerting Strategy

### Multi-Channel Alert Management
Production-grade alerting with escalation and notification management.

```csharp
public class AlertingService : IAlertingService
{
    private readonly ISlackNotifier _slackNotifier;
    private readonly IPagerDutyNotifier _pagerDutyNotifier;
    private readonly IEmailNotifier _emailNotifier;
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<AlertingService> _logger;
    private readonly AlertingConfiguration _config;

    public AlertingService(
        ISlackNotifier slackNotifier,
        IPagerDutyNotifier pagerDutyNotifier,
        IEmailNotifier emailNotifier,
        IMetricsCollector metrics,
        ILogger<AlertingService> logger,
        IOptions<AlertingConfiguration> config)
    {
        _slackNotifier = slackNotifier;
        _pagerDutyNotifier = pagerDutyNotifier;
        _emailNotifier = emailNotifier;
        _metrics = metrics;
        _logger = logger;
        _config = config.Value;
    }

    public async Task SendAlertAsync(Alert alert)
    {
        var alertContext = new AlertContext
        {
            Alert = alert,
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            CorrelationId = Guid.NewGuid()
        };

        _logger.LogWarning("Sending alert: {AlertType} - {Message}", alert.Type, alert.Message);
        
        // Record alert metrics
        _metrics.IncrementCounter("alerts.sent.total", 1, new Dictionary<string, object>
        {
            ["alert_type"] = alert.Type.ToString(),
            ["severity"] = alert.Severity.ToString(),
            ["environment"] = alertContext.Environment
        });

        try
        {
            // Determine notification channels based on severity
            var channels = GetNotificationChannels(alert.Severity);
            
            // Send notifications concurrently
            var tasks = channels.Select(channel => SendToChannelAsync(channel, alertContext));
            await Task.WhenAll(tasks);
            
            // Log successful alert delivery
            _logger.LogInformation("Alert {CorrelationId} sent successfully to {ChannelCount} channels", 
                alertContext.CorrelationId, channels.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert {CorrelationId}", alertContext.CorrelationId);
            _metrics.IncrementCounter("alerts.failed.total", 1);
            throw;
        }
    }

    private async Task SendToChannelAsync(NotificationChannel channel, AlertContext context)
    {
        try
        {
            switch (channel.Type)
            {
                case NotificationType.Slack:
                    await _slackNotifier.SendAlertAsync(context.Alert, channel.Configuration);
                    break;
                
                case NotificationType.PagerDuty:
                    await _pagerDutyNotifier.TriggerIncidentAsync(context.Alert, channel.Configuration);
                    break;
                
                case NotificationType.Email:
                    await _emailNotifier.SendAlertEmailAsync(context.Alert, channel.Configuration);
                    break;
                
                case NotificationType.Teams:
                    await SendTeamsNotificationAsync(context.Alert, channel.Configuration);
                    break;
            }
            
            _metrics.IncrementCounter("alerts.channel.success.total", 1, new Dictionary<string, object>
            {
                ["channel_type"] = channel.Type.ToString(),
                ["severity"] = context.Alert.Severity.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert to {ChannelType}", channel.Type);
            _metrics.IncrementCounter("alerts.channel.failed.total", 1, new Dictionary<string, object>
            {
                ["channel_type"] = channel.Type.ToString(),
                ["error"] = ex.GetType().Name
            });
            throw;
        }
    }

    private List<NotificationChannel> GetNotificationChannels(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => _config.CriticalChannels,
            AlertSeverity.High => _config.HighSeverityChannels,
            AlertSeverity.Medium => _config.MediumSeverityChannels,
            AlertSeverity.Low => _config.LowSeverityChannels,
            _ => _config.DefaultChannels
        };
    }
}

// Alert management and escalation
public class AlertManager
{
    private readonly IAlertingService _alertingService;
    private readonly IAlertRepository _alertRepository;
    private readonly Timer _escalationTimer;

    public AlertManager(IAlertingService alertingService, IAlertRepository alertRepository)
    {
        _alertingService = alertingService;
        _alertRepository = alertRepository;
        
        // Check for alert escalation every 5 minutes
        _escalationTimer = new Timer(CheckAlertEscalation, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    public async Task ProcessAlertAsync(AlertTrigger trigger)
    {
        // Check for alert suppression (prevent spam)
        if (await IsAlertSuppressedAsync(trigger))
        {
            return;
        }

        var alert = await CreateAlertFromTriggerAsync(trigger);
        
        // Save alert to repository for tracking
        await _alertRepository.SaveAlertAsync(alert);
        
        // Send immediate notification
        await _alertingService.SendAlertAsync(alert);
        
        // Schedule escalation if needed
        if (alert.Severity >= AlertSeverity.High)
        {
            await ScheduleEscalationAsync(alert);
        }
    }

    private async void CheckAlertEscalation(object? state)
    {
        try
        {
            var unacknowledgedAlerts = await _alertRepository.GetUnacknowledgedAlertsAsync();
            
            foreach (var alert in unacknowledgedAlerts)
            {
                if (ShouldEscalateAlert(alert))
                {
                    await EscalateAlertAsync(alert);
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw in timer callback
            _logger.LogError(ex, "Error during alert escalation check");
        }
    }

    private bool ShouldEscalateAlert(Alert alert)
    {
        var timeSinceCreated = DateTime.UtcNow - alert.CreatedAt;
        
        return alert.Severity switch
        {
            AlertSeverity.Critical => timeSinceCreated > TimeSpan.FromMinutes(15),
            AlertSeverity.High => timeSinceCreated > TimeSpan.FromMinutes(30),
            AlertSeverity.Medium => timeSinceCreated > TimeSpan.FromHours(2),
            _ => false
        };
    }
}
```

## Dashboard Configuration

### Comprehensive Monitoring Dashboards
Production-ready Grafana dashboards for complete system visibility.

```csharp
public class DashboardProvisioning
{
    public static async Task ProvisionDashboardsAsync(IGrafanaClient grafanaClient)
    {
        // Agent System Overview Dashboard
        var systemOverview = new DashboardDefinition
        {
            Title = "GenericAgents - System Overview",
            Tags = new[] { "agents", "system", "overview" },
            Panels = new[]
            {
                // Key metrics row
                CreateSingleStatPanel("Total Agents", "agents_active_total", 0, 0, 6, 3),
                CreateSingleStatPanel("Success Rate", "rate(agent_executions_success_total[5m])", 6, 0, 6, 3),
                CreateSingleStatPanel("Avg Response Time", "rate(agent_execution_duration_ms_sum[5m])/rate(agent_execution_duration_ms_count[5m])", 12, 0, 6, 3),
                CreateSingleStatPanel("Error Rate", "rate(agent_executions_failed_total[5m])", 18, 0, 6, 3),
                
                // Agent execution trends
                CreateGraphPanel("Agent Executions", new[]
                {
                    new QueryDefinition("rate(agent_executions_success_total[5m])", "Successful Executions"),
                    new QueryDefinition("rate(agent_executions_failed_total[5m])", "Failed Executions")
                }, 0, 3, 12, 6),
                
                // Response time distribution
                CreateHistogramPanel("Response Time Distribution", "agent_execution_duration_ms", 12, 3, 12, 6),
                
                // System resources
                CreateGraphPanel("System Resources", new[]
                {
                    new QueryDefinition("system_cpu_usage_percentage", "CPU Usage"),
                    new QueryDefinition("system_memory_usage_percentage", "Memory Usage"),
                    new QueryDefinition("system_disk_usage_percentage", "Disk Usage")
                }, 0, 9, 24, 6)
            }
        };
        
        await grafanaClient.CreateDashboardAsync(systemOverview);

        // Business Metrics Dashboard
        var businessMetrics = new DashboardDefinition
        {
            Title = "GenericAgents - Business Metrics",
            Tags = new[] { "agents", "business", "kpi" },
            Panels = new[]
            {
                // Business KPIs
                CreateSingleStatPanel("Processing Rate", "business_processing_rate_per_minute", 0, 0, 6, 3),
                CreateSingleStatPanel("SLA Compliance", "business_sla_compliance_percentage", 6, 0, 6, 3),
                CreateSingleStatPanel("Active Users", "business_active_users_total", 12, 0, 6, 3),
                CreateSingleStatPanel("Queue Depth", "business_queue_depth_items", 18, 0, 6, 3),
                
                // Throughput trends
                CreateGraphPanel("Business Throughput", new[]
                {
                    new QueryDefinition("business_documents_processed_total", "Documents Processed"),
                    new QueryDefinition("business_transactions_completed_total", "Transactions Completed"),
                    new QueryDefinition("business_api_requests_total", "API Requests")
                }, 0, 3, 24, 6),
                
                // Error analysis
                CreateTablePanel("Error Breakdown", new[]
                {
                    new QueryDefinition("agent_executions_failed_total", "Failed Executions", "agent_id"),
                    new QueryDefinition("business_transactions_failed_total", "Failed Transactions", "transaction_type")
                }, 0, 9, 24, 6)
            }
        };
        
        await grafanaClient.CreateDashboardAsync(businessMetrics);

        // Infrastructure Health Dashboard  
        var infraHealth = new DashboardDefinition
        {
            Title = "GenericAgents - Infrastructure Health",
            Tags = new[] { "agents", "infrastructure", "health" },
            Panels = new[]
            {
                // Health status overview
                CreateStatusPanel("Component Health Status", "component_health_status", 0, 0, 24, 3),
                
                // Resource utilization
                CreateGraphPanel("Resource Utilization", new[]
                {
                    new QueryDefinition("container_cpu_usage_seconds_total", "CPU Usage"),
                    new QueryDefinition("container_memory_usage_bytes", "Memory Usage"),
                    new QueryDefinition("container_network_receive_bytes_total", "Network In"),
                    new QueryDefinition("container_network_transmit_bytes_total", "Network Out")
                }, 0, 3, 12, 6),
                
                // Database performance
                CreateGraphPanel("Database Performance", new[]
                {
                    new QueryDefinition("database_connections_active", "Active Connections"),
                    new QueryDefinition("database_query_duration_seconds", "Query Duration"),
                    new QueryDefinition("database_transactions_total", "Transactions")
                }, 12, 3, 12, 6),
                
                // External service health
                CreateServiceMapPanel("External Services Health", "external_service_health", 0, 9, 24, 6)
            }
        };
        
        await grafanaClient.CreateDashboardAsync(infraHealth);
    }
}

// Dashboard panel creation helpers
public static class PanelHelpers
{
    public static Panel CreateSingleStatPanel(string title, string query, int x, int y, int width, int height)
    {
        return new Panel
        {
            Title = title,
            Type = "singlestat",
            GridPos = new GridPos { X = x, Y = y, W = width, H = height },
            Targets = new[] 
            {
                new QueryTarget
                {
                    Expr = query,
                    RefId = "A"
                }
            },
            Options = new SingleStatOptions
            {
                ValueMappings = new ValueMapping[0],
                Thresholds = new Threshold[]
                {
                    new() { Color = "green", Value = 0 },
                    new() { Color = "yellow", Value = 80 },
                    new() { Color = "red", Value = 95 }
                }
            }
        };
    }

    public static Panel CreateGraphPanel(string title, QueryDefinition[] queries, int x, int y, int width, int height)
    {
        return new Panel
        {
            Title = title,
            Type = "graph",
            GridPos = new GridPos { X = x, Y = y, W = width, H = height },
            Targets = queries.Select((q, i) => new QueryTarget
            {
                Expr = q.Query,
                LegendFormat = q.Legend,
                RefId = ((char)('A' + i)).ToString()
            }).ToArray(),
            YAxes = new YAxis[]
            {
                new() { Label = "Value", Min = 0 },
                new() { Show = false }
            },
            Lines = true,
            Fill = 1,
            LineWidth = 2,
            PointRadius = 5
        };
    }
}
```

This comprehensive monitoring and alerting guide provides enterprise-grade observability for production agent systems. It includes complete setup instructions, configuration examples, and best practices for maintaining operational excellence.