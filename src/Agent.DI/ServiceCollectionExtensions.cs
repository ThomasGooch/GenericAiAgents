using Agent.AI;
using Agent.AI.Models;
using Agent.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.DI;

/// <summary>
/// Extension methods for IServiceCollection to register agent services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core agent services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAgentToolRegistry();
        
        return services;
    }

    /// <summary>
    /// Adds AI services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentAI(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Ensure logging is available for AI service
        if (!services.Any(s => s.ServiceType == typeof(ILoggerFactory)))
        {
            services.AddLogging();
        }

        services.AddScoped<IAIService, SemanticKernelAIService>();
        
        return services;
    }

    /// <summary>
    /// Adds tool registry services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentToolRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IToolRegistry, ToolRegistry>();
        
        return services;
    }

    /// <summary>
    /// Adds all agent services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Optional configuration for services</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAgentCore();
        services.AddAgentAI();

        // Configure AI service if configuration is provided
        if (configuration != null)
        {
            services.Configure<AIConfiguration>(configuration.GetSection("AI"));
        }
        
        return services;
    }

    /// <summary>
    /// Adds agent services with a specific AI configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="aiConfiguration">AI configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentServices(this IServiceCollection services, AIConfiguration aiConfiguration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(aiConfiguration);

        services.AddAgentCore();
        services.AddAgentAI();

        services.Configure<AIConfiguration>(config =>
        {
            config.Provider = aiConfiguration.Provider;
            config.ModelId = aiConfiguration.ModelId;
            config.ApiKey = aiConfiguration.ApiKey;
            config.Endpoint = aiConfiguration.Endpoint;
            config.MaxTokens = aiConfiguration.MaxTokens;
            config.Temperature = aiConfiguration.Temperature;
            config.TopP = aiConfiguration.TopP;
            config.TimeoutSeconds = aiConfiguration.TimeoutSeconds;
            config.AdditionalSettings = aiConfiguration.AdditionalSettings;
        });
        
        return services;
    }

    /// <summary>
    /// Adds agent services with configuration action
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureAI">Configuration action for AI settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentServices(this IServiceCollection services, Action<AIConfiguration> configureAI)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureAI);

        services.AddAgentCore();
        services.AddAgentAI();

        services.Configure(configureAI);
        
        return services;
    }

    /// <summary>
    /// Adds a hosted service for automatic tool discovery and registration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentToolDiscovery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<ToolDiscoveryHostedService>();
        
        return services;
    }
}