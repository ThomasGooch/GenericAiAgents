using Agent.Tools;
using System.Reflection;

namespace Agent.Registry;

/// <summary>
/// Defines the contract for managing tool registration, discovery, and lifecycle within the GenericAgents framework.
/// 
/// The tool registry serves as the central repository for all available tools in the system, providing
/// comprehensive capabilities for tool management including automatic discovery, registration, retrieval,
/// and lifecycle management. This interface enables agents to dynamically discover and utilize tools
/// based on their capabilities and requirements.
/// 
/// <para><strong>Core Responsibilities:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Tool Registration</strong>: Register tools manually or through automatic discovery</description></item>
/// <item><description><strong>Tool Discovery</strong>: Automatically discover tools from assemblies using reflection</description></item>
/// <item><description><strong>Tool Retrieval</strong>: Provide efficient access to registered tools by name</description></item>
/// <item><description><strong>Lifecycle Management</strong>: Manage tool registration, updates, and removal</description></item>
/// <item><description><strong>Validation</strong>: Ensure tool integrity and prevent registration conflicts</description></item>
/// </list>
/// 
/// <para><strong>Integration Patterns:</strong></para>
/// The registry integrates seamlessly with dependency injection containers, background services for
/// automatic discovery, and agent orchestration systems. It supports both singleton and scoped tool
/// lifecycles depending on the implementation requirements and tool characteristics.
/// </summary>
/// <remarks>
/// <para><strong>Implementation Guidelines:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Thread Safety</strong>: All operations must be thread-safe for concurrent access</description></item>
/// <item><description><strong>Performance</strong>: Tool lookup operations should be optimized for frequent access</description></item>
/// <item><description><strong>Memory Management</strong>: Proper disposal of tool instances and cleanup of resources</description></item>
/// <item><description><strong>Error Handling</strong>: Graceful handling of registration failures and conflicts</description></item>
/// <item><description><strong>Observability</strong>: Comprehensive logging and metrics for monitoring</description></item>
/// </list>
/// 
/// <para><strong>Usage Patterns:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Startup Registration</strong>: Register tools during application initialization</description></item>
/// <item><description><strong>Dynamic Discovery</strong>: Discover tools from loaded assemblies at runtime</description></item>
/// <item><description><strong>Agent Integration</strong>: Provide tools to agents for task execution</description></item>
/// <item><description><strong>Hot Swapping</strong>: Support runtime tool updates and replacements</description></item>
/// </list>
/// 
/// <para><strong>Security Considerations:</strong></para>
/// Implementations should validate tool authenticity, prevent malicious tool registration,
/// and ensure proper access controls for sensitive operations. Tool discovery should be
/// restricted to trusted assemblies and sources.
/// 
/// <para><strong>Scalability Features:</strong></para>
/// The registry design supports horizontal scaling through distributed caching, tool
/// replication across instances, and efficient serialization for tool metadata exchange
/// between distributed components.
/// </remarks>
/// <example>
/// <code>
/// // Basic tool registry usage in application startup
/// public class Startup
/// {
///     public void ConfigureServices(IServiceCollection services)
///     {
///         // Register the tool registry
///         services.AddSingleton&lt;IToolRegistry, ToolRegistry&gt;();
///         
///         // Register specific tools
///         services.AddTransient&lt;ITool, TextProcessorTool&gt;();
///         services.AddTransient&lt;ITool, HttpClientTool&gt;();
///     }
/// 
///     public async Task ConfigureAsync(IServiceProvider serviceProvider)
///     {
///         var registry = serviceProvider.GetRequiredService&lt;IToolRegistry&gt;();
///         
///         // Discover and register all tools from current assemblies
///         await registry.DiscoverAndRegisterToolsAsync();
///         
///         // Manual tool registration for specific instances
///         var customTool = new CustomProcessorTool(configuration);
///         await registry.RegisterToolAsync(customTool);
///     }
/// }
/// 
/// // Agent using the tool registry
/// public class IntelligentAgent
/// {
///     private readonly IToolRegistry _toolRegistry;
///     
///     public IntelligentAgent(IToolRegistry toolRegistry)
///     {
///         _toolRegistry = toolRegistry;
///     }
///     
///     public async Task&lt;AgentResult&gt; ProcessRequestAsync(AgentRequest request)
///     {
///         // Find the most appropriate tool for the task
///         var availableTools = await _toolRegistry.GetAllToolsAsync();
///         var selectedTool = SelectBestTool(request.Task, availableTools);
///         
///         if (selectedTool == null)
///         {
///             return AgentResult.CreateError("No suitable tool found for the requested task");
///         }
///         
///         // Execute the selected tool
///         var toolResult = await selectedTool.ExecuteAsync(request.Parameters);
///         return AgentResult.CreateSuccess(toolResult);
///     }
///     
///     private ITool? SelectBestTool(string task, IEnumerable&lt;ITool&gt; tools)
///     {
///         // AI-powered tool selection logic based on task requirements
///         return tools.FirstOrDefault(tool =&gt; 
///             AnalyzeCompatibility(task, tool.Description) &gt; 0.8);
///     }
/// }
/// 
/// // Background service for tool discovery and management
/// public class ToolManagementService : BackgroundService
/// {
///     private readonly IToolRegistry _toolRegistry;
///     private readonly ILogger&lt;ToolManagementService&gt; _logger;
///     
///     public ToolManagementService(IToolRegistry toolRegistry, ILogger&lt;ToolManagementService&gt; logger)
///     {
///         _toolRegistry = toolRegistry;
///         _logger = logger;
///     }
///     
///     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
///     {
///         // Periodic tool discovery and health checks
///         while (!stoppingToken.IsCancellationRequested)
///         {
///             try
///             {
///                 // Rediscover tools to pick up any new assemblies
///                 await _toolRegistry.DiscoverAndRegisterToolsAsync(stoppingToken);
///                 
///                 // Validate tool health and remove failed tools
///                 await ValidateToolHealthAsync(stoppingToken);
///                 
///                 await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
///             }
///             catch (Exception ex)
///             {
///                 _logger.LogError(ex, "Error during tool management cycle");
///             }
///         }
///     }
///     
///     private async Task ValidateToolHealthAsync(CancellationToken cancellationToken)
///     {
///         var tools = await _toolRegistry.GetAllToolsAsync(cancellationToken);
///         
///         foreach (var tool in tools)
///         {
///             try
///             {
///                 // Perform health check on each tool
///                 var healthResult = await PerformHealthCheck(tool, cancellationToken);
///                 if (!healthResult.IsHealthy)
///                 {
///                     _logger.LogWarning("Tool {ToolName} failed health check: {Reason}", 
///                                       tool.Name, healthResult.Reason);
///                     await _toolRegistry.UnregisterToolAsync(tool.Name, cancellationToken);
///                 }
///             }
///             catch (Exception ex)
///             {
///                 _logger.LogError(ex, "Health check failed for tool {ToolName}", tool.Name);
///             }
///         }
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="ITool"/>
/// <seealso cref="BaseTool"/>
/// <seealso cref="ToolAttribute"/>
/// <seealso cref="DescriptionAttribute"/>
public interface IToolRegistry
{
    /// <summary>
    /// Registers a pre-configured tool instance with the registry for immediate availability to agents.
    /// 
    /// This method registers a fully constructed tool instance that is ready for execution. The tool
    /// will be validated for proper attribute configuration and registered using its name from the
    /// <see cref="ToolAttribute"/>. If a tool with the same name already exists, the behavior depends
    /// on the implementation (may replace, reject, or version the tool).
    /// </summary>
    /// <param name="tool">
    /// The tool instance to register. Must not be null and must be properly decorated with
    /// <see cref="ToolAttribute"/> and <see cref="DescriptionAttribute"/> for successful registration.
    /// The tool should be fully configured and ready for execution.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the registration operation. This is particularly
    /// useful for long-running registration processes that may involve validation or persistence operations.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous registration operation. The task completes when the tool
    /// has been successfully registered and is available for agent usage.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tool"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tool is missing required attributes, has invalid configuration, or when a tool
    /// with the same name is already registered (depending on implementation policy).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para><strong>Registration Process:</strong></para>
    /// <list type="number">
    /// <item><description>Validate tool attributes and configuration</description></item>
    /// <item><description>Check for name conflicts with existing tools</description></item>
    /// <item><description>Store tool instance in the registry</description></item>
    /// <item><description>Update internal indexes and caches</description></item>
    /// <item><description>Trigger registration events and notifications</description></item>
    /// </list>
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Custom Configuration</strong>: Tools requiring specific configuration or dependencies</description></item>
    /// <item><description><strong>Pre-configured Instances</strong>: Tools with complex initialization logic</description></item>
    /// <item><description><strong>Dynamic Registration</strong>: Tools discovered or created at runtime</description></item>
    /// <item><description><strong>Testing Scenarios</strong>: Mock tools for unit testing</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// Registration should be performed during application startup or initialization phases
    /// rather than during request processing to minimize latency impact on agent operations.
    /// 
    /// <para><strong>Thread Safety:</strong></para>
    /// This method must be thread-safe and handle concurrent registration attempts gracefully.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register a pre-configured tool instance
    /// var databaseTool = new DatabaseQueryTool(connectionString, logger);
    /// await toolRegistry.RegisterToolAsync(databaseTool);
    /// 
    /// // Register a tool with custom configuration
    /// var httpTool = new HttpClientTool(httpClient, new HttpClientOptions 
    /// { 
    ///     Timeout = TimeSpan.FromSeconds(30),
    ///     MaxRetries = 3 
    /// });
    /// await toolRegistry.RegisterToolAsync(httpTool);
    /// 
    /// // Register multiple tools in parallel
    /// var tools = new ITool[] 
    /// {
    ///     new TextProcessorTool(),
    ///     new FileReaderTool(fileSystemOptions),
    ///     new EmailSenderTool(smtpConfiguration)
    /// };
    /// 
    /// var registrationTasks = tools.Select(tool => toolRegistry.RegisterToolAsync(tool));
    /// await Task.WhenAll(registrationTasks);
    /// </code>
    /// </example>
    Task RegisterToolAsync(ITool tool, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a tool by its type, allowing the registry to handle tool instantiation and lifecycle management.
    /// 
    /// This method registers a tool type rather than a pre-configured instance, enabling the registry
    /// to control tool creation through dependency injection containers or factory patterns. The registry
    /// will instantiate the tool when needed and manage its lifecycle according to the configured policy.
    /// </summary>
    /// <param name="toolType">
    /// The type of tool to register. Must implement <see cref="ITool"/> interface and be decorated
    /// with required attributes (<see cref="ToolAttribute"/> and <see cref="DescriptionAttribute"/>).
    /// The type must have a public constructor compatible with the dependency injection container.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the registration operation. This is particularly
    /// useful for operations that involve type validation, attribute discovery, or other potentially
    /// long-running registration processes.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous registration operation. The task completes when the tool
    /// type has been successfully registered and the registry is ready to instantiate tools of this type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="toolType"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="toolType"/> does not implement <see cref="ITool"/> interface or
    /// is missing required attributes.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a tool with the same name is already registered or when the tool type cannot
    /// be instantiated (missing constructors, abstract class, etc.).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para><strong>Registration Process:</strong></para>
    /// <list type="number">
    /// <item><description>Validate that type implements ITool interface</description></item>
    /// <item><description>Extract and validate tool attributes (name, description)</description></item>
    /// <item><description>Check for constructor compatibility with DI container</description></item>
    /// <item><description>Register type with the internal type registry</description></item>
    /// <item><description>Update metadata indexes for efficient lookup</description></item>
    /// </list>
    /// 
    /// <para><strong>Lifecycle Management:</strong></para>
    /// Tools registered by type are typically instantiated on-demand and may be cached
    /// or recreated based on the registry's lifecycle policy. This enables efficient
    /// memory usage and supports singleton, transient, or scoped tool lifecycles.
    /// 
    /// <para><strong>Dependency Injection Integration:</strong></para>
    /// This method is designed to work seamlessly with dependency injection containers,
    /// allowing tools to declare dependencies that will be resolved during instantiation.
    /// 
    /// <para><strong>Performance Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Memory Efficiency</strong>: Tools created only when needed</description></item>
    /// <item><description><strong>Scalability</strong>: Supports large numbers of registered tool types</description></item>
    /// <item><description><strong>Flexibility</strong>: Enables different lifecycle strategies per tool</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register tool types for DI-based instantiation
    /// await toolRegistry.RegisterToolAsync(typeof(TextProcessorTool));
    /// await toolRegistry.RegisterToolAsync(typeof(DatabaseQueryTool));
    /// await toolRegistry.RegisterToolAsync(typeof(HttpClientTool));
    /// 
    /// // Register tools discovered through reflection
    /// var toolTypes = Assembly.GetExecutingAssembly()
    ///     .GetTypes()
    ///     .Where(t => typeof(ITool).IsAssignableFrom(t) && 
    ///                 !t.IsAbstract && 
    ///                 t.GetCustomAttribute&lt;ToolAttribute&gt;() != null);
    /// 
    /// foreach (var toolType in toolTypes)
    /// {
    ///     await toolRegistry.RegisterToolAsync(toolType);
    /// }
    /// 
    /// // Register with cancellation support
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    /// 
    /// try
    /// {
    ///     await toolRegistry.RegisterToolAsync(typeof(ComplexAnalysisTool), cts.Token);
    /// }
    /// catch (OperationCanceledException)
    /// {
    ///     // Handle registration timeout
    ///     logger.LogWarning("Tool registration timed out");
    /// }
    /// </code>
    /// </example>
    Task RegisterToolAsync(Type toolType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a registered tool by its unique name identifier.
    /// 
    /// This method provides efficient lookup of tools by their registered name, enabling agents
    /// to dynamically access tools based on task requirements. The lookup is optimized for
    /// frequent access patterns and supports both cached and on-demand tool instantiation
    /// depending on the registry implementation.
    /// </summary>
    /// <param name="name">
    /// The unique name of the tool to retrieve, as specified in the tool's <see cref="ToolAttribute"/>.
    /// The name comparison is typically case-sensitive and must match exactly. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the lookup operation. Useful for preventing
    /// long-running lookups or when tools require instantiation through dependency injection.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous lookup operation. The task result contains the
    /// requested tool instance if found, or <c>null</c> if no tool with the specified name is registered.
    /// For type-registered tools, a new instance may be created on each call depending on the lifecycle policy.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when tool instantiation fails (for type-registered tools) due to missing dependencies
    /// or constructor issues.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Basic tool lookup
    /// var textTool = await toolRegistry.GetToolAsync("text-processor");
    /// if (textTool != null)
    /// {
    ///     var result = await textTool.ExecuteAsync(parameters);
    /// }
    /// 
    /// // Safe tool lookup with null checking
    /// var httpTool = await toolRegistry.GetToolAsync("http-client");
    /// if (httpTool != null)
    /// {
    ///     // Use the tool...
    /// }
    /// else
    /// {
    ///     // Handle missing tool scenario
    ///     logger.LogWarning("HTTP client tool not found");
    /// }
    /// </code>
    /// </example>
    Task<ITool?> GetToolAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all currently registered tools in the registry.
    /// 
    /// This method provides access to the complete collection of available tools, enabling agents
    /// to discover capabilities, perform tool selection based on descriptions, and implement
    /// comprehensive tool management operations. The returned collection represents a snapshot
    /// of registered tools at the time of the call.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation. This is particularly useful
    /// when the registry contains many tools or when tool instantiation is required.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable
    /// collection of all registered tools. For type-registered tools, instances may be created
    /// on-demand. The collection may be empty if no tools are registered.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Get all tools for agent decision making
    /// var availableTools = await toolRegistry.GetAllToolsAsync();
    /// var selectedTool = SelectBestTool(taskDescription, availableTools);
    /// 
    /// // List all available tools
    /// var tools = await toolRegistry.GetAllToolsAsync();
    /// foreach (var tool in tools)
    /// {
    ///     Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
    /// }
    /// 
    /// // Filter tools by capability
    /// var tools = await toolRegistry.GetAllToolsAsync();
    /// var textTools = tools.Where(t => t.Description.Contains("text", StringComparison.OrdinalIgnoreCase));
    /// </code>
    /// </example>
    Task<IEnumerable<ITool>> GetAllToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically discovers and registers all tools from assemblies in the current application domain.
    /// 
    /// This method performs comprehensive tool discovery by scanning all loaded assemblies for types
    /// decorated with <see cref="ToolAttribute"/> and <see cref="DescriptionAttribute"/>. It provides
    /// a convenient way to register all available tools without manual registration, supporting
    /// plugin architectures and dynamic tool loading scenarios.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the discovery operation. This is important
    /// for long-running discovery operations that scan many assemblies or perform complex validation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous discovery and registration operation. The task completes
    /// when all discoverable tools have been processed (successfully registered or skipped due to errors).
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para><strong>Discovery Process:</strong></para>
    /// <list type="number">
    /// <item><description>Enumerate all assemblies in the current AppDomain</description></item>
    /// <item><description>Scan each assembly for types implementing <see cref="ITool"/></description></item>
    /// <item><description>Validate tool attributes and requirements</description></item>
    /// <item><description>Register valid tools using type-based registration</description></item>
    /// <item><description>Log warnings for invalid or conflicting tools</description></item>
    /// </list>
    /// 
    /// <para><strong>Error Handling:</strong></para>
    /// The discovery process is designed to be resilient - individual tool registration failures
    /// do not stop the overall discovery process. Failed registrations are logged but do not
    /// throw exceptions unless the entire discovery process fails.
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// This operation involves reflection and can be expensive for applications with many assemblies.
    /// It should typically be called during application startup rather than during request processing.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Discover all tools during application startup
    /// public async Task ConfigureToolsAsync(IServiceProvider serviceProvider)
    /// {
    ///     var toolRegistry = serviceProvider.GetRequiredService&lt;IToolRegistry&gt;();
    ///     
    ///     // Discover and register all available tools
    ///     await toolRegistry.DiscoverAndRegisterToolsAsync();
    ///     
    ///     // Verify tools were registered
    ///     var tools = await toolRegistry.GetAllToolsAsync();
    ///     logger.LogInformation("Discovered {ToolCount} tools", tools.Count());
    /// }
    /// 
    /// // Discovery with timeout
    /// using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    /// try
    /// {
    ///     await toolRegistry.DiscoverAndRegisterToolsAsync(cts.Token);
    /// }
    /// catch (OperationCanceledException)
    /// {
    ///     logger.LogWarning("Tool discovery timed out");
    /// }
    /// </code>
    /// </example>
    Task DiscoverAndRegisterToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and registers tools from a specified collection of assemblies.
    /// 
    /// This method provides fine-grained control over tool discovery by allowing specific assemblies
    /// to be scanned rather than the entire application domain. This is useful for plugin scenarios,
    /// selective tool loading, or when working with dynamically loaded assemblies.
    /// </summary>
    /// <param name="assemblies">
    /// The collection of assemblies to scan for tool implementations. Must not be null, but can be empty.
    /// Each assembly will be scanned for types decorated with tool attributes. Assemblies that cannot
    /// be scanned will be logged as warnings but will not stop the overall discovery process.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the discovery operation during long-running
    /// assembly scanning or tool registration processes.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous discovery and registration operation. The task completes
    /// when all specified assemblies have been processed and valid tools have been registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="assemblies"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para><strong>Selective Discovery Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Performance</strong>: Scan only relevant assemblies instead of all loaded assemblies</description></item>
    /// <item><description><strong>Security</strong>: Control which assemblies are allowed to provide tools</description></item>
    /// <item><description><strong>Plugin Support</strong>: Enable dynamic loading of tool plugins from specific assemblies</description></item>
    /// <item><description><strong>Testing</strong>: Register tools from test assemblies in unit testing scenarios</description></item>
    /// </list>
    /// 
    /// <para><strong>Assembly Scanning Process:</strong></para>
    /// <list type="number">
    /// <item><description>Iterate through each provided assembly</description></item>
    /// <item><description>Get all types that implement <see cref="ITool"/></description></item>
    /// <item><description>Filter types with required tool attributes</description></item>
    /// <item><description>Validate tool configuration and dependencies</description></item>
    /// <item><description>Register valid tools with the registry</description></item>
    /// </list>
    /// 
    /// <para><strong>Error Resilience:</strong></para>
    /// Individual assembly scanning failures or tool registration failures do not stop
    /// the overall process. Each failure is logged appropriately while continuing with
    /// remaining assemblies and tools.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register tools from specific plugin assemblies
    /// var pluginAssemblies = new[]
    /// {
    ///     Assembly.LoadFrom("MyPlugin.Tools.dll"),
    ///     Assembly.LoadFrom("AnotherPlugin.Tools.dll")
    /// };
    /// 
    /// await toolRegistry.DiscoverAndRegisterToolsAsync(pluginAssemblies);
    /// 
    /// // Register tools from currently executing assembly only
    /// var currentAssembly = Assembly.GetExecutingAssembly();
    /// await toolRegistry.DiscoverAndRegisterToolsAsync(new[] { currentAssembly });
    /// 
    /// // Dynamic plugin loading scenario
    /// public async Task LoadPluginToolsAsync(string pluginPath)
    /// {
    ///     var assembly = Assembly.LoadFrom(pluginPath);
    ///     await toolRegistry.DiscoverAndRegisterToolsAsync(new[] { assembly });
    ///     
    ///     logger.LogInformation("Loaded tools from plugin: {PluginPath}", pluginPath);
    /// }
    /// 
    /// // Register tools from multiple sources
    /// var assemblies = new List&lt;Assembly&gt;
    /// {
    ///     typeof(TextProcessorTool).Assembly,    // Core tools
    ///     typeof(DatabaseTool).Assembly,         // Database tools
    ///     typeof(AnalyticsTool).Assembly         // Analytics tools
    /// };
    /// 
    /// await toolRegistry.DiscoverAndRegisterToolsAsync(assemblies);
    /// </code>
    /// </example>
    Task DiscoverAndRegisterToolsAsync(IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a tool with the specified name is currently registered in the registry.
    /// 
    /// This method provides a lightweight way to check tool availability without retrieving
    /// the actual tool instance, which is useful for conditional logic, validation scenarios,
    /// and agent capability assessment.
    /// </summary>
    /// <param name="name">
    /// The unique name of the tool to check, as specified in the tool's <see cref="ToolAttribute"/>.
    /// The name comparison is typically case-sensitive. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the check operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous check operation. The task result is <c>true</c>
    /// if a tool with the specified name is registered; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is null or empty.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Check if a specific tool is available before using it
    /// if (await toolRegistry.IsRegisteredAsync("database-query"))
    /// {
    ///     var dbTool = await toolRegistry.GetToolAsync("database-query");
    ///     // Use the tool...
    /// }
    /// 
    /// // Validate required tools during application startup
    /// var requiredTools = new[] { "text-processor", "http-client", "file-reader" };
    /// var missingTools = new List&lt;string&gt;();
    /// 
    /// foreach (var toolName in requiredTools)
    /// {
    ///     if (!await toolRegistry.IsRegisteredAsync(toolName))
    ///     {
    ///         missingTools.Add(toolName);
    ///     }
    /// }
    /// 
    /// if (missingTools.Any())
    /// {
    ///     throw new InvalidOperationException($"Missing required tools: {string.Join(", ", missingTools)}");
    /// }
    /// </code>
    /// </example>
    Task<bool> IsRegisteredAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a tool from the registry, making it unavailable for agent usage.
    /// 
    /// This method enables dynamic tool management by allowing tools to be removed at runtime.
    /// This is useful for scenarios involving plugin unloading, tool updates, maintenance operations,
    /// or when tools become unavailable due to dependency issues.
    /// </summary>
    /// <param name="name">
    /// The unique name of the tool to unregister, as specified in the tool's <see cref="ToolAttribute"/>.
    /// The name comparison is typically case-sensitive. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the unregistration operation. This may be
    /// important if the unregistration involves cleanup operations or resource disposal.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous unregistration operation. The task result is <c>true</c>
    /// if the tool was found and successfully removed; <c>false</c> if no tool with the specified
    /// name was registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is null or empty.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para><strong>Unregistration Process:</strong></para>
    /// <list type="number">
    /// <item><description>Locate the tool in the registry</description></item>
    /// <item><description>Perform any necessary cleanup or resource disposal</description></item>
    /// <item><description>Remove the tool from internal storage and indexes</description></item>
    /// <item><description>Update caches and notify any listeners</description></item>
    /// </list>
    /// 
    /// <para><strong>Safety Considerations:</strong></para>
    /// Unregistering a tool while it may be in use by agents could lead to runtime errors.
    /// Implementations should consider tool usage tracking and graceful shutdown procedures.
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Plugin Management</strong>: Unload tools when plugins are disabled</description></item>
    /// <item><description><strong>Maintenance</strong>: Temporarily remove tools during updates</description></item>
    /// <item><description><strong>Error Recovery</strong>: Remove failed or corrupted tools</description></item>
    /// <item><description><strong>Resource Management</strong>: Free resources used by unused tools</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Remove a specific tool
    /// bool removed = await toolRegistry.UnregisterToolAsync("obsolete-tool");
    /// if (removed)
    /// {
    ///     logger.LogInformation("Successfully removed obsolete tool");
    /// }
    /// 
    /// // Safe tool removal with verification
    /// if (await toolRegistry.IsRegisteredAsync("temp-tool"))
    /// {
    ///     await toolRegistry.UnregisterToolAsync("temp-tool");
    ///     logger.LogInformation("Temporary tool removed");
    /// }
    /// 
    /// // Plugin unloading scenario
    /// public async Task UnloadPluginAsync(string pluginName)
    /// {
    ///     var pluginTools = await GetPluginToolsAsync(pluginName);
    ///     
    ///     foreach (var toolName in pluginTools)
    ///     {
    ///         await toolRegistry.UnregisterToolAsync(toolName);
    ///     }
    ///     
    ///     logger.LogInformation("Unloaded {ToolCount} tools from plugin {PluginName}", 
    ///                          pluginTools.Count, pluginName);
    /// }
    /// </code>
    /// </example>
    Task<bool> UnregisterToolAsync(string name, CancellationToken cancellationToken = default);
}