using Agent.AI;
using Agent.AI.Models;
using Agent.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent.DI;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to configure and register
/// all GenericAgents framework services in dependency injection containers.
/// 
/// This class offers a comprehensive set of extension methods that simplify the setup of the
/// GenericAgents framework by providing convenient, fluent APIs for service registration.
/// The methods follow .NET dependency injection patterns and support various configuration
/// scenarios from simple defaults to complex enterprise configurations.
/// 
/// <para><strong>Configuration Patterns:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Simple Setup</strong>: Default configurations for rapid prototyping</description></item>
/// <item><description><strong>Configuration-Based</strong>: Integration with IConfiguration for environment-specific settings</description></item>
/// <item><description><strong>Programmatic</strong>: Code-based configuration for complex scenarios</description></item>
/// <item><description><strong>Modular Registration</strong>: Selective service registration for fine-grained control</description></item>
/// </list>
/// 
/// <para><strong>Service Categories:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Core Services</strong>: Essential framework components and tool registry</description></item>
/// <item><description><strong>AI Services</strong>: Integration with various AI providers and models</description></item>
/// <item><description><strong>Background Services</strong>: Automatic tool discovery and management</description></item>
/// <item><description><strong>Configuration Services</strong>: Strongly-typed configuration management</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><strong>Service Lifetimes:</strong></para>
/// The extension methods register services with appropriate lifetimes:
/// <list type="bullet">
/// <item><description><strong>Singleton</strong>: Tool registry, configuration objects</description></item>
/// <item><description><strong>Scoped</strong>: AI services, agents, per-request components</description></item>
/// <item><description><strong>Transient</strong>: Tools, lightweight services</description></item>
/// </list>
/// 
/// <para><strong>Integration Best Practices:</strong></para>
/// <list type="bullet">
/// <item><description>Call extension methods during application startup in Program.cs or Startup.cs</description></item>
/// <item><description>Configure services before building the service provider</description></item>
/// <item><description>Use configuration-based setup for environment-specific settings</description></item>
/// <item><description>Enable logging before registering AI services for proper diagnostics</description></item>
/// </list>
/// 
/// <para><strong>Thread Safety:</strong></para>
/// All extension methods are thread-safe and can be called concurrently during application
/// startup. However, service registration should typically occur before the service provider
/// is built and used.
/// </remarks>
/// <example>
/// <code>
/// // Simple setup with defaults in Program.cs (.NET 6+)
/// var builder = WebApplication.CreateBuilder(args);
/// 
/// // Add all agent services with default configuration
/// builder.Services.AddAgentServices();
/// 
/// // Add automatic tool discovery
/// builder.Services.AddAgentToolDiscovery();
/// 
/// var app = builder.Build();
/// 
/// // Startup.cs style configuration
/// public void ConfigureServices(IServiceCollection services)
/// {
///     // Add logging first for proper AI service diagnostics
///     services.AddLogging();
///     
///     // Add agent services with configuration
///     services.AddAgentServices(Configuration);
///     
///     // Add tool discovery background service
///     services.AddAgentToolDiscovery();
/// }
/// 
/// // Programmatic AI configuration
/// builder.Services.AddAgentServices(ai =>
/// {
///     ai.Provider = AIProvider.OpenAI;
///     ai.ModelId = "gpt-4";
///     ai.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
///     ai.MaxTokens = 2000;
///     ai.Temperature = 0.7f;
/// });
/// 
/// // Modular service registration for fine-grained control
/// builder.Services
///     .AddAgentCore()                    // Essential services
///     .AddAgentAI()                      // AI integration
///     .AddAgentToolRegistry()            // Tool management
///     .AddAgentToolDiscovery();          // Automatic tool discovery
/// 
/// // Enterprise configuration with multiple AI providers
/// public void ConfigureEnterpriseServices(IServiceCollection services, IConfiguration configuration)
/// {
///     // Primary AI service
///     services.AddAgentServices(configuration);
///     
///     // Additional specialized AI services
///     services.Configure&lt;AIConfiguration&gt;("Summarization", 
///         configuration.GetSection("AI:Summarization"));
///     
///     // Custom tool registration
///     services.AddTransient&lt;ITool, CustomDatabaseTool&gt;();
///     services.AddTransient&lt;ITool, EnterpriseReportingTool&gt;();
///     
///     // Background services for tool management
///     services.AddAgentToolDiscovery();
///     services.AddHostedService&lt;ToolHealthMonitoringService&gt;();
/// }
/// </code>
/// </example>
/// <seealso cref="IServiceCollection"/>
/// <seealso cref="IToolRegistry"/>
/// <seealso cref="IAIService"/>
/// <seealso cref="AIConfiguration"/>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers essential GenericAgents framework services required for basic agent functionality.
    /// 
    /// This method adds the fundamental services needed for agent operations, including the tool registry
    /// and core infrastructure components. It serves as the foundation for all other agent services
    /// and must be called before registering additional specialized services.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add the services to. Must not be null.
    /// </param>
    /// <returns>
    /// The same service collection so that multiple calls can be chained together.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para><strong>Registered Services:</strong></para>
    /// <list type="bullet">
    /// <item><description><see cref="IToolRegistry"/> as singleton - Central tool management and discovery</description></item>
    /// </list>
    /// 
    /// This method is typically called as part of <see cref="AddAgentServices(IServiceCollection, IConfiguration?)"/> 
    /// but can be used independently for scenarios requiring only core functionality.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register only core services for minimal setup
    /// services.AddAgentCore();
    /// 
    /// // Chain with additional services
    /// services.AddAgentCore()
    ///         .AddAgentAI()
    ///         .AddAgentToolDiscovery();
    /// </code>
    /// </example>
    public static IServiceCollection AddAgentCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAgentToolRegistry();

        return services;
    }

    /// <summary>
    /// Registers AI services for agent integration with various AI providers and models.
    /// 
    /// This method adds services necessary for AI-powered agent functionality, including the default
    /// AI service implementation and ensures required logging services are available for diagnostics
    /// and monitoring of AI operations.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add the services to. Must not be null.
    /// </param>
    /// <returns>
    /// The same service collection so that multiple calls can be chained together.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para><strong>Registered Services:</strong></para>
    /// <list type="bullet">
    /// <item><description><see cref="IAIService"/> as scoped - AI provider integration service</description></item>
    /// <item><description>Logging services (if not already registered) - For AI operation diagnostics</description></item>
    /// </list>
    /// 
    /// <para><strong>Configuration Requirements:</strong></para>
    /// AI services require proper configuration through <see cref="AIConfiguration"/> to function correctly.
    /// Use the overloaded <see cref="AddAgentServices(IServiceCollection, IConfiguration?)"/> methods or manually configure AI settings.
    /// 
    /// <para><strong>Provider Support:</strong></para>
    /// The default implementation supports multiple AI providers including OpenAI, Azure OpenAI,
    /// and other compatible services through configuration.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Add AI services with manual configuration
    /// services.AddAgentAI();
    /// services.Configure&lt;AIConfiguration&gt;(config =&gt;
    /// {
    ///     config.Provider = AIProvider.OpenAI;
    ///     config.ApiKey = "your-api-key";
    ///     config.ModelId = "gpt-4";
    /// });
    /// 
    /// // Or use the combined method with configuration
    /// services.AddAgentServices(ai =&gt;
    /// {
    ///     ai.Provider = AIProvider.AzureOpenAI;
    ///     ai.Endpoint = "https://your-resource.openai.azure.com/";
    /// });
    /// </code>
    /// </example>
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
    /// Registers the tool registry service for centralized tool management and discovery.
    /// 
    /// This method adds the tool registry as a singleton service, providing a central repository
    /// for tool registration, discovery, and lifecycle management throughout the application.
    /// The registry enables agents to dynamically discover and utilize available tools.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add the services to. Must not be null.
    /// </param>
    /// <returns>
    /// The same service collection so that multiple calls can be chained together.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para><strong>Registered Services:</strong></para>
    /// <list type="bullet">
    /// <item><description><see cref="IToolRegistry"/> as singleton - Tool management and discovery service</description></item>
    /// </list>
    /// 
    /// <para><strong>Singleton Lifetime:</strong></para>
    /// The tool registry is registered as a singleton to ensure consistent tool state across
    /// the entire application and to provide efficient tool lookup operations.
    /// 
    /// <para><strong>Usage Pattern:</strong></para>
    /// After registration, tools can be manually registered with the registry or automatically
    /// discovered using the <see cref="AddAgentToolDiscovery"/> background service.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register tool registry
    /// services.AddAgentToolRegistry();
    /// 
    /// // Manual tool registration after service provider is built
    /// var serviceProvider = services.BuildServiceProvider();
    /// var toolRegistry = serviceProvider.GetRequiredService&lt;IToolRegistry&gt;();
    /// await toolRegistry.RegisterToolAsync(new CustomTool());
    /// 
    /// // Or use automatic discovery
    /// services.AddAgentToolRegistry()
    ///         .AddAgentToolDiscovery(); // Automatically discovers and registers tools
    /// </code>
    /// </example>
    public static IServiceCollection AddAgentToolRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IToolRegistry, ToolRegistry>();

        return services;
    }

    /// <summary>
    /// Registers all essential GenericAgents framework services with optional configuration integration.
    /// 
    /// This is the primary method for setting up the complete GenericAgents framework in applications.
    /// It combines core services, AI integration, and configuration management in a single call,
    /// providing the most convenient way to get started with the framework.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add the services to. Must not be null.
    /// </param>
    /// <param name="configuration">
    /// Optional configuration object containing AI and other service settings. If provided,
    /// the AI configuration section will be bound to <see cref="AIConfiguration"/> for use by AI services.
    /// If null, AI services will need to be configured manually or through other overloads.
    /// </param>
    /// <returns>
    /// The same service collection so that multiple calls can be chained together.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para><strong>Registered Services:</strong></para>
    /// <list type="bullet">
    /// <item><description>All services from <see cref="AddAgentCore"/> - Essential framework components</description></item>
    /// <item><description>All services from <see cref="AddAgentAI"/> - AI provider integration</description></item>
    /// <item><description><see cref="AIConfiguration"/> (if configuration provided) - Strongly-typed AI settings</description></item>
    /// </list>
    /// 
    /// <para><strong>Configuration Structure:</strong></para>
    /// When configuration is provided, the method expects an "AI" section with AI provider settings:
    /// <code>
    /// {
    ///   "AI": {
    ///     "Provider": "OpenAI",
    ///     "ModelId": "gpt-4",
    ///     "ApiKey": "your-api-key",
    ///     "MaxTokens": 2000,
    ///     "Temperature": 0.7
    ///   }
    /// }
    /// </code>
    /// 
    /// <para><strong>Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item><description>Use this method for most applications requiring complete framework functionality</description></item>
    /// <item><description>Store sensitive settings like API keys in secure configuration providers</description></item>
    /// <item><description>Add <see cref="AddAgentToolDiscovery"/> for automatic tool discovery</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Minimal setup with default configuration
    /// services.AddAgentServices();
    /// 
    /// // With configuration integration (.NET 6+ style)
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddAgentServices(builder.Configuration);
    /// 
    /// // Traditional Startup.cs style
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     services.AddAgentServices(Configuration);
    ///     services.AddAgentToolDiscovery(); // Add automatic tool discovery
    /// }
    /// 
    /// // Complete setup with all features
    /// services.AddAgentServices(Configuration)
    ///         .AddAgentToolDiscovery();
    /// </code>
    /// </example>
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
    /// Registers all GenericAgents framework services with a pre-configured AI configuration object.
    /// 
    /// This method provides a way to register agent services with a fully configured AI settings object,
    /// useful for scenarios where configuration is built programmatically or comes from external sources
    /// rather than standard .NET configuration providers.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add the services to. Must not be null.
    /// </param>
    /// <param name="aiConfiguration">
    /// A fully configured <see cref="AIConfiguration"/> object containing all AI provider settings.
    /// Must not be null and should include all required settings for the chosen AI provider.
    /// </param>
    /// <returns>
    /// The same service collection so that multiple calls can be chained together.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="aiConfiguration"/> is null.
    /// </exception>
    /// <remarks>
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>External Configuration</strong>: Settings loaded from databases or external services</description></item>
    /// <item><description><strong>Runtime Configuration</strong>: AI settings determined dynamically at startup</description></item>
    /// <item><description><strong>Testing Scenarios</strong>: Pre-configured settings for unit or integration tests</description></item>
    /// <item><description><strong>Multi-tenant Applications</strong>: Different AI configurations per tenant</description></item>
    /// </list>
    /// 
    /// <para><strong>Configuration Validation:</strong></para>
    /// Ensure the provided configuration object includes all required settings for your chosen
    /// AI provider, including API keys, endpoints, and model identifiers.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Pre-configured AI settings
    /// var aiConfig = new AIConfiguration
    /// {
    ///     Provider = AIProvider.OpenAI,
    ///     ModelId = "gpt-4",
    ///     ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
    ///     MaxTokens = 2000,
    ///     Temperature = 0.7f,
    ///     TimeoutSeconds = 30
    /// };
    /// 
    /// services.AddAgentServices(aiConfig);
    /// 
    /// // Loading configuration from external source
    /// var configService = serviceProvider.GetRequiredService&lt;IExternalConfigService&gt;();
    /// var aiConfig = await configService.GetAIConfigurationAsync();
    /// services.AddAgentServices(aiConfig);
    /// 
    /// // Multi-provider setup
    /// var primaryAI = new AIConfiguration { /* primary settings */ };
    /// var summaryAI = new AIConfiguration { /* specialized for summaries */ };
    /// 
    /// services.AddAgentServices(primaryAI);
    /// services.Configure&lt;AIConfiguration&gt;("Summary", _ =&gt; summaryAI);
    /// </code>
    /// </example>
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
    /// Registers all GenericAgents framework services with AI configuration through a delegate.
    /// 
    /// This method provides the most flexible way to configure AI services using a configuration
    /// action delegate. It allows for programmatic configuration with full access to the
    /// <see cref="AIConfiguration"/> object during service registration.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add the services to. Must not be null.
    /// </param>
    /// <param name="configureAI">
    /// A configuration action that receives an <see cref="AIConfiguration"/> object to configure.
    /// This delegate will be called during service registration to set up AI provider settings.
    /// Must not be null.
    /// </param>
    /// <returns>
    /// The same service collection so that multiple calls can be chained together.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureAI"/> is null.
    /// </exception>
    /// <remarks>
    /// <para><strong>Configuration Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Inline Configuration</strong>: Configure AI settings directly in service registration</description></item>
    /// <item><description><strong>Environment Flexibility</strong>: Use conditional logic based on environment</description></item>
    /// <item><description><strong>Validation</strong>: Apply custom validation during configuration</description></item>
    /// <item><description><strong>Derived Settings</strong>: Calculate configuration values based on other services</description></item>
    /// </list>
    /// 
    /// <para><strong>Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item><description>Use environment variables or secure stores for sensitive data like API keys</description></item>
    /// <item><description>Apply validation to ensure required settings are provided</description></item>
    /// <item><description>Consider using this method for development and testing scenarios</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Inline AI configuration
    /// services.AddAgentServices(ai =&gt;
    /// {
    ///     ai.Provider = AIProvider.OpenAI;
    ///     ai.ModelId = "gpt-4";
    ///     ai.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ///         ?? throw new InvalidOperationException("OpenAI API key not found");
    ///     ai.MaxTokens = 2000;
    ///     ai.Temperature = 0.7f;
    /// });
    /// 
    /// // Environment-based configuration
    /// services.AddAgentServices(ai =&gt;
    /// {
    ///     if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    ///     {
    ///         ai.Provider = AIProvider.OpenAI;
    ///         ai.ModelId = "gpt-3.5-turbo"; // Use cheaper model for development
    ///         ai.Temperature = 1.0f; // More creative for testing
    ///     }
    ///     else
    ///     {
    ///         ai.Provider = AIProvider.AzureOpenAI;
    ///         ai.Endpoint = "https://prod-openai.openai.azure.com/";
    ///         ai.ModelId = "gpt-4";
    ///         ai.Temperature = 0.3f; // More deterministic for production
    ///     }
    ///     
    ///     ai.ApiKey = GetSecureApiKey();
    ///     ai.TimeoutSeconds = 45;
    /// });
    /// 
    /// // Configuration with validation
    /// services.AddAgentServices(ai =&gt;
    /// {
    ///     ai.Provider = AIProvider.OpenAI;
    ///     ai.ModelId = "gpt-4";
    ///     ai.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    ///     
    ///     // Validate configuration
    ///     if (string.IsNullOrEmpty(ai.ApiKey))
    ///         throw new InvalidOperationException("AI API key is required");
    ///     
    ///     if (ai.MaxTokens &lt;= 0)
    ///         ai.MaxTokens = 2000; // Set reasonable default
    /// });
    /// </code>
    /// </example>
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
    /// Registers a background hosted service that automatically discovers and registers tools from loaded assemblies.
    /// 
    /// This method adds a background service that runs during application startup to scan assemblies
    /// for tool implementations and automatically register them with the tool registry. This eliminates
    /// the need for manual tool registration and supports dynamic plugin scenarios.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add the services to. Must not be null.
    /// </param>
    /// <returns>
    /// The same service collection so that multiple calls can be chained together.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para><strong>Background Service Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Automatic Discovery</strong>: No manual tool registration required</description></item>
    /// <item><description><strong>Plugin Support</strong>: Automatically picks up tools from dynamically loaded assemblies</description></item>
    /// <item><description><strong>Startup Integration</strong>: Tools are available immediately when the application starts</description></item>
    /// <item><description><strong>Error Resilience</strong>: Failed tool discoveries don't stop application startup</description></item>
    /// </list>
    /// 
    /// <para><strong>Discovery Process:</strong></para>
    /// The hosted service runs during application startup and:
    /// <list type="number">
    /// <item><description>Scans all loaded assemblies for types implementing <see cref="ITool"/></description></item>
    /// <item><description>Validates tools have required attributes (<see cref="ToolAttribute"/>, <see cref="DescriptionAttribute"/>)</description></item>
    /// <item><description>Registers valid tools with the <see cref="IToolRegistry"/></description></item>
    /// <item><description>Logs registration results and any errors encountered</description></item>
    /// </list>
    /// 
    /// <para><strong>Dependencies:</strong></para>
    /// This service requires <see cref="IToolRegistry"/> to be registered, which is automatically
    /// included when using <see cref="AddAgentCore"/> or <see cref="AddAgentServices(IServiceCollection, IConfiguration?)"/>.
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// Tool discovery uses reflection and can impact startup time for applications with many assemblies.
    /// The service runs asynchronously during startup to minimize blocking.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Complete agent setup with automatic tool discovery
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// builder.Services.AddAgentServices(builder.Configuration)
    ///                 .AddAgentToolDiscovery(); // Automatically discover and register tools
    /// 
    /// var app = builder.Build();
    /// 
    /// // Tools are automatically available after startup
    /// app.MapGet("/tools", async (IToolRegistry registry) =>
    /// {
    ///     var tools = await registry.GetAllToolsAsync();
    ///     return tools.Select(t => new { t.Name, t.Description });
    /// });
    /// 
    /// // Traditional Startup.cs configuration
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     services.AddAgentServices(Configuration);
    ///     services.AddAgentToolDiscovery(); // Essential for automatic tool discovery
    /// }
    /// 
    /// // Minimal API with tool discovery
    /// var builder = WebApplication.CreateBuilder();
    /// 
    /// // Register agent services and tool discovery
    /// builder.Services.AddAgentServices()
    ///                 .AddAgentToolDiscovery();
    /// 
    /// var app = builder.Build();
    /// 
    /// // Verify tools were discovered
    /// app.MapGet("/health/tools", async (IToolRegistry registry, ILogger&lt;Program&gt; logger) =>
    /// {
    ///     var tools = await registry.GetAllToolsAsync();
    ///     var toolCount = tools.Count();
    ///     
    ///     logger.LogInformation("Discovered {ToolCount} tools", toolCount);
    ///     
    ///     return Results.Ok(new 
    ///     { 
    ///         ToolCount = toolCount,
    ///         Tools = tools.Select(t => t.Name).ToArray()
    ///     });
    /// });
    /// 
    /// await app.RunAsync();
    /// </code>
    /// </example>
    /// <seealso cref="ToolDiscoveryHostedService"/>
    /// <seealso cref="IToolRegistry"/>
    /// <seealso cref="IHostedService"/>
    public static IServiceCollection AddAgentToolDiscovery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<ToolDiscoveryHostedService>();

        return services;
    }
}