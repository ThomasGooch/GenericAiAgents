using Agent.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Agent.DI;

/// <summary>
/// Hosted service for automatic tool discovery and registration at application startup
/// </summary>
public class ToolDiscoveryHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ToolDiscoveryHostedService> _logger;

    public ToolDiscoveryHostedService(IServiceProvider serviceProvider, ILogger<ToolDiscoveryHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting tool discovery service...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var toolRegistry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();

            _logger.LogDebug("Discovering tools from loaded assemblies...");
            await toolRegistry.DiscoverAndRegisterToolsAsync(cancellationToken);

            var tools = await toolRegistry.GetAllToolsAsync(cancellationToken);
            var toolCount = tools.Count();

            _logger.LogInformation("Tool discovery completed. Registered {ToolCount} tools", toolCount);

            foreach (var tool in tools)
            {
                _logger.LogDebug("Registered tool: {ToolName} - {ToolDescription}", tool.Name, tool.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete tool discovery");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tool discovery service stopped");
        return Task.CompletedTask;
    }
}