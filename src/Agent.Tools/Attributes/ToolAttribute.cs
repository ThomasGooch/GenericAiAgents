namespace Agent.Tools;

/// <summary>
/// Specifies the unique identifier name for a tool class in the GenericAgents framework.
/// 
/// This attribute is required for all tool implementations derived from <see cref="BaseTool"/> and
/// serves as the primary mechanism for tool discovery, registration, and invocation by AI agents.
/// The tool name acts as a unique identifier within the tool registry and should follow consistent
/// naming conventions for optimal discoverability and usability.
/// 
/// <para><strong>Discovery and Registration:</strong></para>
/// Tools decorated with this attribute are automatically discovered through reflection during
/// application startup and registered in the tool registry. The framework scans assemblies
/// for classes marked with this attribute and validates their implementation contracts.
/// 
/// <para><strong>Agent Integration:</strong></para>
/// AI agents use the tool name to select and invoke appropriate tools based on their capabilities
/// and the current task context. The name should be descriptive enough for agents to understand
/// the tool's purpose while remaining concise for efficient processing.
/// </summary>
/// <remarks>
/// <para><strong>Naming Conventions:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Format</strong>: Use lowercase with hyphens for multi-word names (kebab-case)</description></item>
/// <item><description><strong>Descriptive</strong>: Name should clearly indicate the tool's primary function</description></item>
/// <item><description><strong>Unique</strong>: Must be unique across the entire tool registry</description></item>
/// <item><description><strong>Stable</strong>: Avoid changing names after deployment to prevent breaking changes</description></item>
/// </list>
/// 
/// <para><strong>Best Practices:</strong></para>
/// <list type="bullet">
/// <item><description>Use action-oriented names (e.g., "file-reader", "data-processor", "http-client")</description></item>
/// <item><description>Avoid generic names that don't indicate specific functionality</description></item>
/// <item><description>Consider grouping related tools with consistent prefixes (e.g., "db-query", "db-insert")</description></item>
/// <item><description>Keep names between 5-25 characters for optimal usability</description></item>
/// </list>
/// 
/// <para><strong>Registration Process:</strong></para>
/// During application initialization, the framework performs assembly scanning to discover
/// all classes decorated with ToolAttribute, validates their implementation, and registers
/// them in the tool registry for runtime access by agents.
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// Tool names are used as dictionary keys in the registry, so they should be optimized
/// for efficient string comparison and hashing operations.
/// </remarks>
/// <example>
/// <code>
/// // Proper tool attribute usage with descriptive names
/// [Tool("text-processor")]
/// [Description("Processes text with various transformation operations")]
/// public class TextProcessorTool : BaseTool
/// {
///     // Implementation...
/// }
/// 
/// [Tool("http-client")]
/// [Description("Executes HTTP requests with comprehensive error handling")]
/// public class HttpClientTool : BaseTool
/// {
///     // Implementation...
/// }
/// 
/// [Tool("file-reader")]
/// [Description("Reads and processes files from various sources")]
/// public class FileReaderTool : BaseTool
/// {
///     // Implementation...
/// }
/// 
/// // Database tools with consistent naming convention
/// [Tool("db-query")]
/// [Description("Executes database queries with parameter binding")]
/// public class DatabaseQueryTool : BaseTool
/// {
///     // Implementation...
/// }
/// 
/// [Tool("db-insert")]
/// [Description("Inserts data into database tables with validation")]
/// public class DatabaseInsertTool : BaseTool
/// {
///     // Implementation...
/// }
/// 
/// // Example of tool discovery and registration in Startup.cs
/// public void ConfigureServices(IServiceCollection services)
/// {
///     // Automatic discovery of all tools with ToolAttribute
///     services.AddAgentTools(); // Scans assemblies and registers tools
///     
///     // Tools are now available in the registry:
///     // - "text-processor" -&gt; TextProcessorTool
///     // - "http-client" -&gt; HttpClientTool
///     // - "file-reader" -&gt; FileReaderTool
///     // - "db-query" -&gt; DatabaseQueryTool
///     // - "db-insert" -&gt; DatabaseInsertTool
/// }
/// 
/// // Agent usage example
/// public async Task&lt;AgentResult&gt; ProcessDataAsync(string inputData)
/// {
///     // Agent selects tool by name
///     var textTool = _toolRegistry.GetTool("text-processor");
///     var httpTool = _toolRegistry.GetTool("http-client");
///     
///     // Execute tools in sequence
///     var processedText = await textTool.ExecuteAsync(new() { ["input"] = inputData });
///     var httpResult = await httpTool.ExecuteAsync(new() { ["data"] = processedText.Data });
///     
///     return AgentResult.CreateSuccess(httpResult.Data);
/// }
/// </code>
/// </example>
/// <seealso cref="BaseTool"/>
/// <seealso cref="DescriptionAttribute"/>
/// <seealso cref="Agent.Registry.IToolRegistry"/>
[AttributeUsage(AttributeTargets.Class)]
public class ToolAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier name for the tool.
    /// 
    /// This name serves as the primary key for tool registration and discovery within the
    /// GenericAgents framework. It must be unique across all registered tools and should
    /// follow established naming conventions for consistency and discoverability.
    /// </summary>
    /// <value>
    /// A string containing the tool's unique identifier. The name is immutable once set
    /// and should follow kebab-case naming conventions (lowercase with hyphens).
    /// </value>
    /// <remarks>
    /// The tool name is used extensively throughout the framework for:
    /// <list type="bullet">
    /// <item><description>Registry lookups and tool resolution</description></item>
    /// <item><description>Agent tool selection and invocation</description></item>
    /// <item><description>Logging and monitoring tool usage</description></item>
    /// <item><description>Configuration and policy enforcement</description></item>
    /// </list>
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolAttribute"/> class with the specified tool name.
    /// 
    /// This constructor validates that the provided name is not null and stores it for use during
    /// tool discovery and registration. The name becomes the permanent identifier for the tool
    /// throughout its lifecycle in the application.
    /// </summary>
    /// <param name="name">
    /// The unique identifier name for the tool. Must not be null or empty, and should follow
    /// established naming conventions (kebab-case with descriptive, action-oriented terminology).
    /// The name should be between 5-25 characters and clearly indicate the tool's primary function.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is null, ensuring that all tools have valid identifiers.
    /// </exception>
    /// <remarks>
    /// <para><strong>Validation Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Not Null</strong>: Name parameter must have a value</description></item>
    /// <item><description><strong>Uniqueness</strong>: Name must be unique within the tool registry (enforced at registration)</description></item>
    /// <item><description><strong>Format</strong>: Should follow kebab-case naming convention</description></item>
    /// <item><description><strong>Descriptive</strong>: Should clearly indicate tool functionality</description></item>
    /// </list>
    /// 
    /// <para><strong>Best Practice Examples:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Good Names</strong>: "text-processor", "http-client", "file-reader", "data-validator"</description></item>
    /// <item><description><strong>Poor Names</strong>: "Tool1", "MyTool", "Helper", "Processor" (too generic)</description></item>
    /// </list>
    /// 
    /// <para><strong>Registration Impact:</strong></para>
    /// The provided name becomes the key used for tool lookup operations and must remain
    /// stable across application versions to maintain compatibility with existing agent configurations.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Proper constructor usage with descriptive names
    /// [Tool("text-analyzer")]  // Clear, descriptive, kebab-case
    /// public class TextAnalyzerTool : BaseTool { }
    /// 
    /// [Tool("pdf-generator")]  // Action-oriented, specific functionality
    /// public class PdfGeneratorTool : BaseTool { }
    /// 
    /// [Tool("email-sender")]   // Domain-specific, clear purpose
    /// public class EmailSenderTool : BaseTool { }
    /// 
    /// // Examples of consistent naming for related tools
    /// [Tool("db-reader")]      // Database reading operations
    /// [Tool("db-writer")]      // Database writing operations  
    /// [Tool("db-migrator")]    // Database migration operations
    /// 
    /// // Invalid usage - will throw ArgumentNullException
    /// try
    /// {
    ///     var attribute = new ToolAttribute(null); // Throws exception
    /// }
    /// catch (ArgumentNullException ex)
    /// {
    ///     Console.WriteLine($"Tool name cannot be null: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public ToolAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

/// <summary>
/// Provides a human-readable description of a tool's functionality and purpose within the GenericAgents framework.
/// 
/// This attribute works in conjunction with <see cref="ToolAttribute"/> to provide comprehensive metadata
/// about tool capabilities, usage scenarios, and behavioral characteristics. The description is used by
/// AI agents for tool selection decisions and by developers for understanding tool functionality during
/// development and debugging.
/// 
/// <para><strong>Agent Decision Making:</strong></para>
/// AI agents analyze tool descriptions to determine the most appropriate tool for a given task.
/// Well-crafted descriptions significantly improve agent decision-making accuracy and overall
/// system effectiveness by providing clear context about tool capabilities and limitations.
/// 
/// <para><strong>Developer Experience:</strong></para>
/// Descriptions serve as inline documentation that helps developers understand tool functionality
/// without examining implementation details, improving code maintainability and team collaboration.
/// </summary>
/// <remarks>
/// <para><strong>Content Guidelines:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Comprehensive</strong>: Describe primary functionality, key features, and important behaviors</description></item>
/// <item><description><strong>Clear</strong>: Use plain English that both AI agents and developers can understand</description></item>
/// <item><description><strong>Specific</strong>: Include details about input/output types, processing methods, and constraints</description></item>
/// <item><description><strong>Actionable</strong>: Focus on what the tool accomplishes rather than implementation details</description></item>
/// </list>
/// 
/// <para><strong>Optimal Length:</strong></para>
/// Descriptions should be detailed enough to enable informed tool selection (typically 50-200 words)
/// while remaining concise enough for efficient processing by AI agents during decision making.
/// 
/// <para><strong>Structure Recommendations:</strong></para>
/// <list type="number">
/// <item><description><strong>Primary Function</strong>: Lead with the main capability or purpose</description></item>
/// <item><description><strong>Key Features</strong>: Highlight important features or processing options</description></item>
/// <item><description><strong>Use Cases</strong>: Mention common scenarios or applications</description></item>
/// <item><description><strong>Limitations</strong>: Note any important constraints or requirements</description></item>
/// </list>
/// 
/// <para><strong>Agent Processing:</strong></para>
/// AI agents use natural language processing to analyze descriptions and match them against
/// task requirements, making description quality critical for optimal tool selection accuracy.
/// </remarks>
/// <example>
/// <code>
/// // Excellent description examples with comprehensive details
/// [Tool("text-processor")]
/// [Description("Processes text with various transformation operations including case conversion, " +
///              "whitespace normalization, character encoding, and pattern-based replacements. " +
///              "Supports multiple languages and encoding formats with configurable output options.")]
/// public class TextProcessorTool : BaseTool { }
/// 
/// [Tool("http-client")]
/// [Description("Executes HTTP requests (GET, POST, PUT, DELETE) with automatic retry logic, " +
///              "authentication support, custom headers, and comprehensive error handling. " +
///              "Handles JSON, XML, and form-encoded data with configurable timeout settings.")]
/// public class HttpClientTool : BaseTool { }
/// 
/// [Tool("file-analyzer")]
/// [Description("Analyzes file content and metadata to extract information such as file type, " +
///              "size, encoding, structure, and embedded data. Supports various formats including " +
///              "text, binary, images, and documents with virus scanning capabilities.")]
/// public class FileAnalyzerTool : BaseTool { }
/// 
/// [Tool("database-query")]
/// [Description("Executes parameterized SQL queries against relational databases with connection " +
///              "pooling, transaction support, and result set mapping. Provides protection against " +
///              "SQL injection with comprehensive logging and performance monitoring.")]
/// public class DatabaseQueryTool : BaseTool { }
/// 
/// // Good descriptions for specialized tools
/// [Tool("json-validator")]
/// [Description("Validates JSON documents against schemas with detailed error reporting, " +
///              "format checking, and custom validation rules for data quality assurance.")]
/// public class JsonValidatorTool : BaseTool { }
/// 
/// [Tool("email-sender")]
/// [Description("Sends emails through SMTP with template support, attachment handling, " +
///              "HTML/plain text formatting, and delivery tracking for reliable communication.")]
/// public class EmailSenderTool : BaseTool { }
/// 
/// // Example of agent using descriptions for tool selection
/// public class IntelligentAgent
/// {
///     public async Task&lt;ITool&gt; SelectBestToolAsync(string taskDescription)
///     {
///         var availableTools = _toolRegistry.GetAllTools();
///         
///         foreach (var tool in availableTools)
///         {
///             // AI agent analyzes tool description against task requirements
///             var compatibility = await AnalyzeCompatibility(taskDescription, tool.Description);
///             if (compatibility &amp;gt; 0.8) // High confidence match
///             {
///                 return tool;
///             }
///         }
///         
///         return null; // No suitable tool found
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="ToolAttribute"/>
/// <seealso cref="BaseTool"/>
/// <seealso cref="ITool"/>
[AttributeUsage(AttributeTargets.Class)]
public class DescriptionAttribute : Attribute
{
    /// <summary>
    /// Gets the human-readable description of the tool's functionality and capabilities.
    /// 
    /// This description provides comprehensive information about what the tool does, how it works,
    /// and when it should be used. It serves as both documentation for developers and decision-making
    /// input for AI agents when selecting appropriate tools for specific tasks.
    /// </summary>
    /// <value>
    /// A string containing a detailed description of the tool's purpose, functionality, key features,
    /// and usage scenarios. The description should be informative enough for both human developers
    /// and AI agents to understand the tool's capabilities and appropriate use cases.
    /// </value>
    /// <remarks>
    /// The description content is used in several contexts:
    /// <list type="bullet">
    /// <item><description>AI agent tool selection and task matching</description></item>
    /// <item><description>Developer documentation and IDE IntelliSense</description></item>
    /// <item><description>Tool registry browsing and discovery</description></item>
    /// <item><description>Automated tool testing and validation</description></item>
    /// <item><description>System monitoring and usage analytics</description></item>
    /// </list>
    /// 
    /// Quality descriptions directly impact system effectiveness by enabling better tool selection
    /// decisions and reducing development time through clear capability communication.
    /// </remarks>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptionAttribute"/> class with the specified description text.
    /// 
    /// This constructor validates that the provided description is not null and stores it for use
    /// during tool discovery, registration, and runtime decision-making processes. The description
    /// becomes a permanent part of the tool's metadata throughout its application lifecycle.
    /// </summary>
    /// <param name="description">
    /// The human-readable description of the tool's functionality, capabilities, and usage scenarios.
    /// Must not be null and should provide comprehensive information about the tool's purpose,
    /// features, and appropriate use cases in 50-200 words for optimal effectiveness.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="description"/> is null, ensuring that all tools have
    /// meaningful descriptions for proper functionality within the framework.
    /// </exception>
    /// <remarks>
    /// <para><strong>Content Quality Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Completeness</strong>: Include primary functionality, key features, and constraints</description></item>
    /// <item><description><strong>Clarity</strong>: Use clear, professional language that both humans and AI can understand</description></item>
    /// <item><description><strong>Specificity</strong>: Mention specific capabilities, data types, and processing methods</description></item>
    /// <item><description><strong>Usefulness</strong>: Focus on practical aspects that aid in tool selection decisions</description></item>
    /// </list>
    /// 
    /// <para><strong>Impact on System Performance:</strong></para>
    /// Well-crafted descriptions improve AI agent decision-making accuracy, reduce tool selection
    /// errors, and enhance overall system efficiency by enabling more precise task-to-tool matching.
    /// 
    /// <para><strong>Maintenance Considerations:</strong></para>
    /// Descriptions should be kept up-to-date with tool functionality changes to maintain accuracy
    /// and prevent confusion in agent decision-making processes.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Comprehensive description examples for different tool types
    /// 
    /// // Data processing tool
    /// [Description("Transforms and validates CSV data with support for custom delimiters, " +
    ///              "header detection, data type inference, and error handling. Processes " +
    ///              "large datasets efficiently with streaming capabilities and progress reporting.")]
    /// public class CsvProcessorTool : BaseTool { }
    /// 
    /// // Integration tool  
    /// [Description("Connects to REST APIs with OAuth 2.0 authentication, automatic token refresh, " +
    ///              "rate limiting, and comprehensive error handling. Supports JSON and XML payload " +
    ///              "formats with configurable retry policies and circuit breaker patterns.")]
    /// public class ApiConnectorTool : BaseTool { }
    /// 
    /// // Analysis tool
    /// [Description("Analyzes text sentiment using machine learning models with confidence scoring, " +
    ///              "emotion detection, and language identification. Supports multiple languages " +
    ///              "and provides detailed analysis reports with visualization data.")]
    /// public class SentimentAnalyzerTool : BaseTool { }
    /// 
    /// // System tool
    /// [Description("Monitors system resources including CPU, memory, disk usage, and network " +
    ///              "activity with real-time alerts, historical trending, and performance " +
    ///              "optimization recommendations for production environments.")]
    /// public class SystemMonitorTool : BaseTool { }
    /// 
    /// // Invalid usage example - will throw ArgumentNullException
    /// try
    /// {
    ///     var attribute = new DescriptionAttribute(null); // Throws exception
    /// }
    /// catch (ArgumentNullException ex)
    /// {
    ///     Console.WriteLine($"Description cannot be null: {ex.Message}");
    /// }
    /// 
    /// // Usage in tool selection logic
    /// public class ToolSelectionEngine
    /// {
    ///     public ITool FindBestTool(string requirement, IEnumerable&lt;ITool&gt; availableTools)
    ///     {
    ///         return availableTools
    ///             .Where(tool =&amp;gt; ContainsRelevantKeywords(tool.Description, requirement))
    ///             .OrderByDescending(tool =&amp;gt; CalculateRelevanceScore(tool.Description, requirement))
    ///             .FirstOrDefault();
    ///     }
    /// 
    ///     private bool ContainsRelevantKeywords(string description, string requirement)
    ///     {
    ///         // AI-powered keyword matching using tool descriptions
    ///         return AnalyzeSemanticSimilarity(description, requirement) &amp;gt; 0.6;
    ///     }
    /// }
    /// </code>
    /// </example>
    public DescriptionAttribute(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}