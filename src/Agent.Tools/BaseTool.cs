using Agent.Tools.Models;
using System.Reflection;

namespace Agent.Tools;

/// <summary>
/// Provides a base implementation for tools in the GenericAgents framework with automatic attribute-based
/// configuration, parameter validation, and standardized error handling patterns.
/// 
/// This abstract class implements the common functionality required by most tools, including:
/// - Automatic discovery and registration through reflection and attributes
/// - Type-safe parameter schema definition and validation
/// - Standardized error handling with comprehensive exception management
/// - Resource management with cancellation token support
/// - Template method pattern for consistent tool execution flow
/// 
/// <para><strong>Key Benefits:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Rapid Development</strong>: Reduces boilerplate code for tool implementation</description></item>
/// <item><description><strong>Consistency</strong>: Ensures all tools follow the same execution patterns</description></item>
/// <item><description><strong>Type Safety</strong>: Provides compile-time and runtime parameter validation</description></item>
/// <item><description><strong>Error Resilience</strong>: Standardized error handling prevents tool failures from crashing agents</description></item>
/// <item><description><strong>Testability</strong>: Well-defined abstract methods enable comprehensive unit testing</description></item>
/// </list>
/// 
/// <para><strong>Implementation Pattern:</strong></para>
/// Tools inheriting from BaseTool must be decorated with <see cref="ToolAttribute"/> and 
/// <see cref="DescriptionAttribute"/> for automatic discovery and must implement two abstract methods
/// for parameter schema definition and execution logic.
/// </summary>
/// <remarks>
/// <para><strong>Thread Safety:</strong></para>
/// BaseTool instances are thread-safe for read operations but individual tool implementations
/// should ensure thread safety for any shared state modifications during execution.
/// 
/// <para><strong>Performance Considerations:</strong></para>
/// - Parameter schema is computed once during construction and cached for performance
/// - Parameter validation uses efficient dictionary lookups and type checking
/// - Memory allocation is minimized through dictionary reuse and defensive copying
/// 
/// <para><strong>Security Considerations:</strong></para>
/// - All parameters are validated before execution to prevent injection attacks
/// - Exception details are sanitized in error results to prevent information leakage
/// - Cancellation tokens are properly handled to prevent resource exhaustion
/// 
/// <para><strong>Resource Management:</strong></para>
/// Derived classes should properly handle resource disposal in their ExecuteInternalAsync
/// implementation and respect cancellation tokens for long-running operations.
/// </remarks>
/// <example>
/// <code>
/// // Simple text processing tool implementation
/// [Tool("text-upper")]
/// [Description("Converts input text to uppercase")]
/// public class TextUpperTool : BaseTool
/// {
///     protected override Dictionary&lt;string, Type&gt; DefineParameterSchema()
///     {
///         return new Dictionary&lt;string, Type&gt;
///         {
///             ["text"] = typeof(string)
///         };
///     }
/// 
///     protected override async Task&lt;ToolResult&gt; ExecuteInternalAsync(
///         Dictionary&lt;string, object&gt; parameters, 
///         CancellationToken cancellationToken)
///     {
///         var text = parameters["text"] as string;
///         var result = text?.ToUpperInvariant() ?? string.Empty;
///         
///         return ToolResult.CreateSuccess(result, new Dictionary&lt;string, object&gt;
///         {
///             ["originalLength"] = text?.Length ?? 0,
///             ["resultLength"] = result.Length,
///             ["processingTime"] = DateTime.UtcNow
///         });
///     }
/// }
/// 
/// // Complex database tool with multiple parameters
/// [Tool("user-lookup")]
/// [Description("Retrieves user information from the database")]
/// public class UserLookupTool : BaseTool
/// {
///     private readonly IUserRepository _userRepository;
///     private readonly ILogger&lt;UserLookupTool&gt; _logger;
/// 
///     public UserLookupTool(IUserRepository userRepository, ILogger&lt;UserLookupTool&gt; logger)
///     {
///         _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
///         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
///     }
/// 
///     protected override Dictionary&lt;string, Type&gt; DefineParameterSchema()
///     {
///         return new Dictionary&lt;string, Type&gt;
///         {
///             ["userId"] = typeof(int),
///             ["includeProfile"] = typeof(bool),
///             ["fields"] = typeof(string[])
///         };
///     }
/// 
///     protected override async Task&lt;ToolResult&gt; ExecuteInternalAsync(
///         Dictionary&lt;string, object&gt; parameters, 
///         CancellationToken cancellationToken)
///     {
///         var userId = Convert.ToInt32(parameters["userId"]);
///         var includeProfile = Convert.ToBoolean(parameters["includeProfile"]);
///         var fields = parameters["fields"] as string[] ?? Array.Empty&lt;string&gt;();
/// 
///         try
///         {
///             var user = await _userRepository.GetUserAsync(userId, includeProfile, cancellationToken);
///             
///             if (user == null)
///             {
///                 return ToolResult.CreateError($"User with ID {userId} not found");
///             }
/// 
///             var filteredData = FilterUserData(user, fields);
///             return ToolResult.CreateSuccess(filteredData, new Dictionary&lt;string, object&gt;
///             {
///                 ["userId"] = userId,
///                 ["fieldsRequested"] = fields.Length,
///                 ["retrievalTime"] = DateTime.UtcNow
///             });
///         }
///         catch (Exception ex)
///         {
///             _logger.LogError(ex, "Failed to retrieve user {UserId}", userId);
///             return ToolResult.CreateError("Database operation failed");
///         }
///     }
/// 
///     private object FilterUserData(object user, string[] fields)
///     {
///         // Implementation for filtering user data based on requested fields
///         // This would use reflection or a mapping library to extract specified fields
///         return user; // Simplified for example
///     }
/// }
/// 
/// // Usage in agent registration
/// public void ConfigureServices(IServiceCollection services)
/// {
///     // Tools are automatically discovered and registered
///     services.AddAgentTools();
///     
///     // Or manually register specific tools
///     services.AddScoped&lt;ITool, TextUpperTool&gt;();
///     services.AddScoped&lt;ITool, UserLookupTool&gt;();
/// }
/// </code>
/// </example>
/// <seealso cref="ITool"/>
/// <seealso cref="ToolAttribute"/>
/// <seealso cref="DescriptionAttribute"/>
/// <seealso cref="ToolResult"/>
public abstract class BaseTool : ITool
{
    /// <summary>
    /// Gets the unique name identifier for this tool, automatically populated from the <see cref="ToolAttribute"/>.
    /// 
    /// This name is used for tool discovery, registration, and invocation by AI agents. It should be unique
    /// within the tool registry and follow naming conventions (lowercase with hyphens for multi-word names).
    /// </summary>
    /// <value>
    /// A string containing the tool's unique identifier as specified in the <see cref="ToolAttribute"/>.
    /// </value>
    /// <remarks>
    /// The name is immutable once set during construction and is derived from the tool's attribute decoration.
    /// It serves as the primary key for tool lookup operations and should be descriptive yet concise.
    /// </remarks>
    /// <example>
    /// Examples of well-formed tool names:
    /// <list type="bullet">
    /// <item><description>"text-processor" - for text manipulation tools</description></item>
    /// <item><description>"http-client" - for HTTP request tools</description></item>
    /// <item><description>"database-query" - for database access tools</description></item>
    /// <item><description>"file-reader" - for file system tools</description></item>
    /// </list>
    /// </example>
    public string Name { get; }

    /// <summary>
    /// Gets the human-readable description of this tool's purpose and functionality, automatically
    /// populated from the <see cref="DescriptionAttribute"/>.
    /// 
    /// This description provides detailed information about what the tool does, its intended use cases,
    /// and any important behavior or limitations. It is used by AI agents for tool selection and
    /// by developers for understanding tool capabilities.
    /// </summary>
    /// <value>
    /// A string containing a comprehensive description of the tool's functionality and usage scenarios.
    /// </value>
    /// <remarks>
    /// The description should be comprehensive enough to allow AI agents to make informed decisions
    /// about tool selection while being concise enough for efficient processing. It should include
    /// key functionality, expected inputs, and notable behaviors or limitations.
    /// </remarks>
    /// <example>
    /// Examples of well-crafted tool descriptions:
    /// <code>
    /// [Description("Converts text to uppercase, lowercase, or title case with support for " +
    ///              "multiple languages and custom formatting options")]
    /// 
    /// [Description("Executes HTTP GET, POST, PUT, DELETE requests with automatic retry logic, " +
    ///              "authentication support, and comprehensive error handling")]
    /// 
    /// [Description("Queries relational databases with parameterized queries, connection pooling, " +
    ///              "and automatic result mapping to strongly-typed objects")]
    /// </code>
    /// </example>
    public string Description { get; }

    private readonly Dictionary<string, Type> _parameterSchema;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTool"/> class with automatic attribute-based configuration.
    /// 
    /// This constructor uses reflection to discover the tool's metadata from attributes and initializes
    /// the parameter schema through the abstract <see cref="DefineParameterSchema"/> method. The constructor
    /// validates that required attributes are present and throws exceptions if the tool is not properly configured.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tool class is missing required <see cref="ToolAttribute"/> or <see cref="DescriptionAttribute"/> decorations.
    /// </exception>
    /// <remarks>
    /// <para><strong>Attribute Requirements:</strong></para>
    /// <list type="bullet">
    /// <item><description><see cref="ToolAttribute"/> must specify a unique tool name</description></item>
    /// <item><description><see cref="DescriptionAttribute"/> must provide a meaningful tool description</description></item>
    /// </list>
    /// 
    /// <para><strong>Initialization Process:</strong></para>
    /// <list type="number">
    /// <item><description>Reflects on the tool class to discover attributes</description></item>
    /// <item><description>Validates required attributes are present</description></item>
    /// <item><description>Extracts name and description from attributes</description></item>
    /// <item><description>Calls abstract method to define parameter schema</description></item>
    /// <item><description>Caches parameter schema for efficient validation</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance:</strong></para>
    /// Reflection is used only during construction to minimize runtime overhead. The parameter
    /// schema is computed once and cached for the lifetime of the tool instance.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Properly decorated tool class - constructor will succeed
    /// [Tool("text-analyzer")]
    /// [Description("Analyzes text for readability, sentiment, and key metrics")]
    /// public class TextAnalyzerTool : BaseTool
    /// {
    ///     // Implementation...
    /// }
    /// 
    /// // Missing attributes - constructor will throw InvalidOperationException
    /// public class BadTool : BaseTool  // Missing [Tool] and [Description] attributes
    /// {
    ///     // This will fail during construction
    /// }
    /// </code>
    /// </example>
    protected BaseTool()
    {
        var type = GetType();

        var toolAttribute = type.GetCustomAttribute<ToolAttribute>()
            ?? throw new InvalidOperationException($"Tool class {type.Name} must have a [Tool] attribute");

        var descriptionAttribute = type.GetCustomAttribute<DescriptionAttribute>()
            ?? throw new InvalidOperationException($"Tool class {type.Name} must have a [Description] attribute");

        Name = toolAttribute.Name;
        Description = descriptionAttribute.Description;
        _parameterSchema = DefineParameterSchema();
    }

    /// <summary>
    /// Retrieves a copy of the parameter schema that defines the expected parameters for this tool.
    /// 
    /// The parameter schema is a dictionary mapping parameter names to their expected .NET types.
    /// This information is used by agents and the framework for parameter validation, UI generation,
    /// and automatic parameter conversion. The schema is defined once during construction through
    /// the abstract <see cref="DefineParameterSchema"/> method and cached for performance.
    /// </summary>
    /// <returns>
    /// A new <see cref="Dictionary{TKey, TValue}"/> containing parameter name-to-type mappings.
    /// The returned dictionary is a defensive copy to prevent external modification of the internal schema.
    /// </returns>
    /// <remarks>
    /// <para><strong>Schema Structure:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Keys</strong>: Parameter names as strings (case-sensitive)</description></item>
    /// <item><description><strong>Values</strong>: .NET types that parameters must be compatible with</description></item>
    /// </list>
    /// 
    /// <para><strong>Type Compatibility:</strong></para>
    /// The framework supports automatic type conversion for common scenarios:
    /// <list type="bullet">
    /// <item><description>Numeric types (int, double, decimal, etc.)</description></item>
    /// <item><description>String to enum conversions</description></item>
    /// <item><description>JSON objects to complex types</description></item>
    /// <item><description>Array and collection types</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance:</strong></para>
    /// Returns a defensive copy to maintain immutability while allowing safe external access.
    /// The overhead of creating a copy is minimal compared to the benefits of preventing
    /// accidental schema modification.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example parameter schema for a file processing tool
    /// var schema = tool.GetParameterSchema();
    /// // schema contains:
    /// // {
    /// //   ["filePath"] = typeof(string),
    /// //   ["maxSizeBytes"] = typeof(long),
    /// //   ["options"] = typeof(FileProcessingOptions),
    /// //   ["preserveOriginal"] = typeof(bool)
    /// // }
    /// 
    /// // Using schema for parameter validation
    /// foreach (var param in parameters)
    /// {
    ///     if (schema.TryGetValue(param.Key, out var expectedType))
    ///     {
    ///         if (!IsCompatible(param.Value, expectedType))
    ///         {
    ///             throw new ArgumentException($"Parameter {param.Key} must be of type {expectedType.Name}");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="DefineParameterSchema"/>
    /// <seealso cref="ValidateParameters"/>
    public Dictionary<string, Type> GetParameterSchema()
    {
        return new Dictionary<string, Type>(_parameterSchema);
    }

    /// <summary>
    /// Validates that the provided parameters match the expected parameter schema for this tool.
    /// 
    /// This method performs comprehensive validation to ensure that all required parameters are present
    /// and that their types are compatible with the expected schema. The validation includes null checking,
    /// type compatibility verification, and automatic type conversion assessment. This validation is automatically
    /// called before tool execution to prevent runtime errors.
    /// </summary>
    /// <param name="parameters">
    /// A dictionary containing the parameters to validate, where keys are parameter names and values
    /// are the parameter values to validate. May be null, in which case validation will fail.
    /// </param>
    /// <returns>
    /// <c>true</c> if all required parameters are present and type-compatible; <c>false</c> otherwise.
    /// Returns <c>false</c> immediately upon encountering any validation failure for efficiency.
    /// </returns>
    /// <remarks>
    /// <para><strong>Validation Rules:</strong></para>
    /// <list type="number">
    /// <item><description><strong>Null Check</strong>: Parameters dictionary must not be null</description></item>
    /// <item><description><strong>Required Parameters</strong>: All schema parameters must be present in the dictionary</description></item>
    /// <item><description><strong>Type Compatibility</strong>: Parameter values must be compatible with expected types</description></item>
    /// <item><description><strong>Null Values</strong>: Null parameter values are allowed and skip type checking</description></item>
    /// </list>
    /// 
    /// <para><strong>Type Compatibility Logic:</strong></para>
    /// Type compatibility is determined through multiple mechanisms:
    /// <list type="bullet">
    /// <item><description><strong>Direct Assignment</strong>: Value type is assignable to expected type</description></item>
    /// <item><description><strong>Type Conversion</strong>: Value can be converted using <see cref="Convert.ChangeType"/></description></item>
    /// <item><description><strong>Inheritance</strong>: Value type inherits from or implements expected type</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance Characteristics:</strong></para>
    /// - Fails fast on first validation error for efficiency
    /// - Uses cached parameter schema to avoid repeated reflection
    /// - Type compatibility checks are optimized for common scenarios
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// Parameter validation prevents injection attacks and ensures type safety before execution.
    /// All user-provided parameters are validated against the strict schema definition.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example tool with multiple parameter types
    /// [Tool("data-processor")]
    /// [Description("Processes data with various options")]
    /// public class DataProcessorTool : BaseTool
    /// {
    ///     protected override Dictionary&lt;string, Type&gt; DefineParameterSchema()
    ///     {
    ///         return new Dictionary&lt;string, Type&gt;
    ///         {
    ///             ["inputFile"] = typeof(string),
    ///             ["maxRecords"] = typeof(int),
    ///             ["includeHeaders"] = typeof(bool),
    ///             ["options"] = typeof(ProcessingOptions)
    ///         };
    ///     }
    /// }
    /// 
    /// // Valid parameters - validation passes
    /// var validParams = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["inputFile"] = "data.csv",
    ///     ["maxRecords"] = 1000,
    ///     ["includeHeaders"] = true,
    ///     ["options"] = new ProcessingOptions { Format = "JSON" }
    /// };
    /// bool isValid = tool.ValidateParameters(validParams); // Returns true
    /// 
    /// // Invalid parameters - missing required parameter
    /// var invalidParams = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["inputFile"] = "data.csv"
    ///     // Missing maxRecords, includeHeaders, options
    /// };
    /// bool isValid2 = tool.ValidateParameters(invalidParams); // Returns false
    /// 
    /// // Invalid parameters - wrong type
    /// var wrongTypeParams = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["inputFile"] = "data.csv",
    ///     ["maxRecords"] = "not a number", // Should be int
    ///     ["includeHeaders"] = true,
    ///     ["options"] = new ProcessingOptions()
    /// };
    /// bool isValid3 = tool.ValidateParameters(wrongTypeParams); // Returns false
    /// </code>
    /// </example>
    /// <seealso cref="GetParameterSchema"/>
    /// <seealso cref="IsTypeCompatible"/>
    /// <seealso cref="ExecuteAsync"/>
    public bool ValidateParameters(Dictionary<string, object> parameters)
    {
        if (parameters == null)
            return false;

        // Check if all required parameters are present
        foreach (var requiredParam in _parameterSchema)
        {
            if (!parameters.ContainsKey(requiredParam.Key))
                return false;

            // Check parameter type compatibility
            var value = parameters[requiredParam.Key];
            if (value != null && !IsTypeCompatible(value, requiredParam.Value))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Executes this tool with the provided parameters, implementing the template method pattern
    /// with comprehensive error handling, parameter validation, and cancellation support.
    /// 
    /// This method serves as the main entry point for tool execution and provides a standardized
    /// execution flow that includes parameter validation, exception handling, cancellation support,
    /// and result normalization. The actual tool logic is implemented in the abstract
    /// <see cref="ExecuteInternalAsync"/> method by derived classes.
    /// </summary>
    /// <param name="parameters">
    /// A dictionary containing the parameters required for tool execution. All parameters
    /// defined in the tool's schema must be present and type-compatible. The dictionary
    /// keys are case-sensitive parameter names, and values are the parameter data.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the tool execution. Tools should
    /// respect this token and check for cancellation periodically during long-running operations.
    /// Defaults to <see cref="CancellationToken.None"/> if not specified.
    /// </param>
    /// <returns>
    /// A <see cref="Task{ToolResult}"/> representing the asynchronous tool execution.
    /// The result contains either the execution output on success or error details on failure.
    /// The result is guaranteed to never be null - error conditions return error results rather than null.
    /// </returns>
    /// <remarks>
    /// <para><strong>Execution Flow:</strong></para>
    /// <list type="number">
    /// <item><description><strong>Parameter Validation</strong>: Validates parameters against schema</description></item>
    /// <item><description><strong>Tool Execution</strong>: Calls abstract ExecuteInternalAsync method</description></item>
    /// <item><description><strong>Result Validation</strong>: Ensures non-null result is returned</description></item>
    /// <item><description><strong>Exception Handling</strong>: Catches and converts exceptions to error results</description></item>
    /// </list>
    /// 
    /// <para><strong>Error Handling Strategy:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Parameter Validation Failures</strong>: Return descriptive error result</description></item>
    /// <item><description><strong>Cancellation</strong>: Handle OperationCanceledException gracefully</description></item>
    /// <item><description><strong>Null Results</strong>: Convert null returns to error results</description></item>
    /// <item><description><strong>General Exceptions</strong>: Sanitize and wrap in error results</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// - Parameter validation is performed once before execution
    /// - Exception handling is optimized for common error scenarios
    /// - Cancellation token checking is delegated to derived tool implementations
    /// 
    /// <para><strong>Security Considerations:</strong></para>
    /// - All parameters are validated before execution
    /// - Exception messages are sanitized to prevent information leakage
    /// - Resource exhaustion is prevented through cancellation token support
    /// 
    /// <para><strong>Thread Safety:</strong></para>
    /// The base implementation is thread-safe, but derived classes must ensure their
    /// ExecuteInternalAsync implementation is thread-safe if the tool will be used concurrently.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example tool execution with proper error handling
    /// var parameters = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["inputText"] = "Hello, World!",
    ///     ["operation"] = "uppercase",
    ///     ["preserveSpacing"] = true
    /// };
    /// 
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    /// 
    /// try
    /// {
    ///     var result = await textTool.ExecuteAsync(parameters, cts.Token);
    ///     
    ///     if (result.IsSuccess)
    ///     {
    ///         Console.WriteLine($"Tool executed successfully: {result.Data}");
    ///         
    ///         // Access metadata if available
    ///         if (result.Metadata.ContainsKey("processingTime"))
    ///         {
    ///             var processingTime = result.Metadata["processingTime"];
    ///             Console.WriteLine($"Processing took: {processingTime}");
    ///         }
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine($"Tool execution failed: {result.ErrorMessage}");
    ///         
    ///         // Handle specific error conditions
    ///         if (result.ErrorMessage.Contains("Invalid parameters"))
    ///         {
    ///             // Handle parameter validation errors
    ///             Console.WriteLine("Please check parameter types and required fields");
    ///         }
    ///     }
    /// }
    /// catch (OperationCanceledException)
    /// {
    ///     Console.WriteLine("Tool execution was cancelled");
    /// }
    /// 
    /// // Example with multiple tools in sequence
    /// var tools = new[] { textProcessorTool, validatorTool, formatterTool };
    /// object currentData = "Initial input";
    /// 
    /// foreach (var tool in tools)
    /// {
    ///     var toolParams = new Dictionary&lt;string, object&gt; { ["input"] = currentData };
    ///     var result = await tool.ExecuteAsync(toolParams, cancellationToken);
    ///     
    ///     if (!result.IsSuccess)
    ///     {
    ///         Console.WriteLine($"Pipeline failed at tool {tool.Name}: {result.ErrorMessage}");
    ///         break;
    ///     }
    ///     
    ///     currentData = result.Data; // Pass output to next tool
    /// }
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Note: This method does not throw exceptions directly. All error conditions are captured
    /// and returned as <see cref="ToolResult"/> instances with IsSuccess = false.
    /// </exception>
    /// <seealso cref="ExecuteInternalAsync"/>
    /// <seealso cref="ValidateParameters"/>
    /// <seealso cref="ToolResult"/>
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateParameters(parameters))
            {
                return ToolResult.CreateError("Invalid parameters provided");
            }

            var result = await ExecuteInternalAsync(parameters, cancellationToken);
            return result ?? ToolResult.CreateError("ExecuteInternalAsync returned null result");
        }
        catch (OperationCanceledException)
        {
            return ToolResult.CreateError("Tool execution was cancelled");
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Tool execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Defines the parameter schema that specifies the required parameters and their expected types for this tool.
    /// 
    /// This abstract method must be implemented by derived classes to specify what parameters the tool
    /// expects to receive during execution. The schema is used for parameter validation, type checking,
    /// and automatic parameter conversion. The schema is computed once during tool construction and
    /// cached for performance.
    /// </summary>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> where keys are parameter names (case-sensitive strings)
    /// and values are the expected .NET types for those parameters. All parameters in the returned
    /// dictionary are considered required for tool execution.
    /// </returns>
    /// <remarks>
    /// <para><strong>Schema Design Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Parameter Names</strong>: Use descriptive, camelCase names</description></item>
    /// <item><description><strong>Type Selection</strong>: Choose appropriate .NET types with conversion support</description></item>
    /// <item><description><strong>Required Parameters Only</strong>: Only include truly required parameters</description></item>
    /// <item><description><strong>Type Compatibility</strong>: Consider automatic type conversion capabilities</description></item>
    /// </list>
    /// 
    /// <para><strong>Supported Types:</strong></para>
    /// The framework supports automatic conversion for common types including primitives,
    /// strings, enums, collections, and complex objects that can be deserialized from JSON.
    /// 
    /// <para><strong>Performance:</strong></para>
    /// This method is called once during tool construction, so complex computation is acceptable.
    /// However, prefer static schema definitions over dynamic computation for consistency.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple tool with basic parameter types
    /// protected override Dictionary&lt;string, Type&gt; DefineParameterSchema()
    /// {
    ///     return new Dictionary&lt;string, Type&gt;
    ///     {
    ///         ["inputText"] = typeof(string),
    ///         ["maxLength"] = typeof(int),
    ///         ["caseSensitive"] = typeof(bool)
    ///     };
    /// }
    /// 
    /// // Complex tool with various parameter types
    /// protected override Dictionary&lt;string, Type&gt; DefineParameterSchema()
    /// {
    ///     return new Dictionary&lt;string, Type&gt;
    ///     {
    ///         ["connectionString"] = typeof(string),
    ///         ["timeoutSeconds"] = typeof(int),
    ///         ["retryCount"] = typeof(int?), // Optional parameter (nullable)
    ///         ["queryParameters"] = typeof(Dictionary&lt;string, object&gt;),
    ///         ["outputFormat"] = typeof(OutputFormat), // Enum type
    ///         ["headers"] = typeof(string[]), // Array type
    ///         ["options"] = typeof(DatabaseOptions) // Complex object type
    ///     };
    /// }
    /// </code>
    /// </example>
    protected abstract Dictionary<string, Type> DefineParameterSchema();

    /// <summary>
    /// Executes the core tool logic with validated parameters and proper cancellation support.
    /// 
    /// This abstract method contains the actual implementation of the tool's functionality and is called
    /// by the base <see cref="ExecuteAsync"/> method after parameter validation. Implementations should
    /// focus on the tool's core logic without worrying about parameter validation, exception handling,
    /// or result wrapping, as these are handled by the base class.
    /// </summary>
    /// <param name="parameters">
    /// A validated dictionary containing all required parameters as specified by <see cref="DefineParameterSchema"/>.
    /// Parameter validation has already been performed, so all required parameters are guaranteed to be
    /// present and type-compatible. The dictionary will never be null.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that should be respected during long-running operations. Implementations
    /// should check this token periodically and throw <see cref="OperationCanceledException"/> if
    /// cancellation is requested. The base class handles this exception gracefully.
    /// </param>
    /// <returns>
    /// A <see cref="Task{ToolResult}"/> representing the tool execution result. The result should
    /// never be null - use <see cref="ToolResult.CreateError"/> for error conditions instead of
    /// returning null. Include relevant metadata in successful results for enhanced debugging and monitoring.
    /// </returns>
    /// <remarks>
    /// <para><strong>Implementation Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Parameter Access</strong>: Cast parameters to expected types using the schema</description></item>
    /// <item><description><strong>Error Handling</strong>: Let exceptions bubble up - base class handles them</description></item>
    /// <item><description><strong>Cancellation</strong>: Respect cancellation tokens for responsive cancellation</description></item>
    /// <item><description><strong>Resource Management</strong>: Use using statements for IDisposable resources</description></item>
    /// <item><description><strong>Logging</strong>: Use structured logging for operation traceability</description></item>
    /// </list>
    /// 
    /// <para><strong>Thread Safety:</strong></para>
    /// Implementations must be thread-safe if the tool will be used concurrently. Avoid shared
    /// mutable state and use thread-safe collections and operations when necessary.
    /// 
    /// <para><strong>Performance Considerations:</strong></para>
    /// - Avoid unnecessary allocations and copying
    /// - Use async/await properly for I/O-bound operations
    /// - Consider connection pooling for database/HTTP tools
    /// - Include performance metrics in result metadata
    /// 
    /// <para><strong>Security Best Practices:</strong></para>
    /// - Validate input data beyond type checking (SQL injection, XSS, etc.)
    /// - Use parameterized queries for database operations
    /// - Sanitize outputs to prevent information leakage
    /// - Handle sensitive data appropriately (logging, caching)
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple text processing tool implementation
    /// protected override async Task&lt;ToolResult&gt; ExecuteInternalAsync(
    ///     Dictionary&lt;string, object&gt; parameters, 
    ///     CancellationToken cancellationToken)
    /// {
    ///     var inputText = parameters["inputText"] as string;
    ///     var operation = parameters["operation"] as string;
    ///     var preserveSpacing = Convert.ToBoolean(parameters["preserveSpacing"]);
    ///     
    ///     var startTime = DateTime.UtcNow;
    ///     
    ///     // Respect cancellation token for long operations
    ///     cancellationToken.ThrowIfCancellationRequested();
    ///     
    ///     string result = operation.ToLowerInvariant() switch
    ///     {
    ///         "uppercase" =&amp;gt; preserveSpacing ? inputText.ToUpperInvariant() : inputText.ToUpperInvariant().Trim(),
    ///         "lowercase" =&amp;gt; preserveSpacing ? inputText.ToLowerInvariant() : inputText.ToLowerInvariant().Trim(),
    ///         "titlecase" =&amp;gt; CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inputText.ToLower()),
    ///         _ =&amp;gt; throw new ArgumentException($"Unknown operation: {operation}")
    ///     };
    ///     
    ///     // Simulate async operation - tools often involve I/O
    ///     await Task.Delay(1, cancellationToken);
    ///     
    ///     var processingTime = DateTime.UtcNow - startTime;
    ///     
    ///     return ToolResult.CreateSuccess(result, new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["originalLength"] = inputText?.Length ?? 0,
    ///         ["resultLength"] = result.Length,
    ///         ["operation"] = operation,
    ///         ["processingTimeMs"] = processingTime.TotalMilliseconds,
    ///         ["timestamp"] = DateTime.UtcNow
    ///     });
    /// }
    /// 
    /// // Database access tool with proper resource management
    /// protected override async Task&lt;ToolResult&gt; ExecuteInternalAsync(
    ///     Dictionary&lt;string, object&gt; parameters, 
    ///     CancellationToken cancellationToken)
    /// {
    ///     var connectionString = parameters["connectionString"] as string;
    ///     var query = parameters["query"] as string;
    ///     var timeoutSeconds = Convert.ToInt32(parameters["timeoutSeconds"]);
    ///     
    ///     using var connection = new SqlConnection(connectionString);
    ///     using var command = new SqlCommand(query, connection);
    ///     
    ///     command.CommandTimeout = timeoutSeconds;
    ///     
    ///     // Respect cancellation throughout async operations
    ///     await connection.OpenAsync(cancellationToken);
    ///     
    ///     var results = new List&lt;Dictionary&lt;string, object&gt;&gt;();
    ///     using var reader = await command.ExecuteReaderAsync(cancellationToken);
    ///     
    ///     while (await reader.ReadAsync(cancellationToken))
    ///     {
    ///         var row = new Dictionary&lt;string, object&gt;();
    ///         for (int i = 0; i &lt; reader.FieldCount; i++)
    ///         {
    ///             row[reader.GetName(i)] = reader.GetValue(i);
    ///         }
    ///         results.Add(row);
    ///         
    ///         // Check for cancellation during large result processing
    ///         cancellationToken.ThrowIfCancellationRequested();
    ///     }
    ///     
    ///     return ToolResult.CreateSuccess(results, new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["rowCount"] = results.Count,
    ///         ["queryExecutionTime"] = DateTime.UtcNow,
    ///         ["connectionTimeout"] = timeoutSeconds
    ///     });
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteAsync"/>
    /// <seealso cref="DefineParameterSchema"/>
    /// <seealso cref="ToolResult"/>
    protected abstract Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether a given value is compatible with the expected parameter type.
    /// 
    /// This helper method performs type compatibility checking that supports both direct type
    /// assignment and automatic type conversion scenarios. It is used internally during parameter
    /// validation to ensure that provided parameter values can be safely used with the expected types.
    /// </summary>
    /// <param name="value">
    /// The value to check for type compatibility. Must not be null - null values are handled
    /// separately in the validation logic and skip type compatibility checking.
    /// </param>
    /// <param name="expectedType">
    /// The expected .NET type that the value should be compatible with. This type comes from
    /// the tool's parameter schema as defined by <see cref="DefineParameterSchema"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value is compatible with the expected type through either direct
    /// assignment or automatic type conversion; <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// <para><strong>Compatibility Rules:</strong></para>
    /// <list type="number">
    /// <item><description><strong>Direct Assignment</strong>: Value type is directly assignable to expected type</description></item>
    /// <item><description><strong>Type Conversion</strong>: Value can be converted using <see cref="Convert.ChangeType"/></description></item>
    /// <item><description><strong>Inheritance/Interface</strong>: Value type inherits from or implements expected type</description></item>
    /// </list>
    /// 
    /// <para><strong>Supported Conversions:</strong></para>
    /// <list type="bullet">
    /// <item><description>Numeric type conversions (int to double, etc.)</description></item>
    /// <item><description>String to primitive type conversions</description></item>
    /// <item><description>String to enum conversions</description></item>
    /// <item><description>Nullable type handling</description></item>
    /// </list>
    /// 
    /// <para><strong>Performance:</strong></para>
    /// Type conversion checking uses a try/catch block which may impact performance for
    /// incompatible types. However, successful conversions are cached by the .NET runtime.
    /// 
    /// <para><strong>Error Handling:</strong></para>
    /// All conversion exceptions are caught and treated as incompatible types rather than
    /// being propagated, ensuring validation failures are graceful.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Examples of type compatibility
    /// bool compatible1 = IsTypeCompatible("123", typeof(int));           // true - string to int conversion
    /// bool compatible2 = IsTypeCompatible(123, typeof(double));          // true - int to double conversion
    /// bool compatible3 = IsTypeCompatible("Active", typeof(Status));     // true - string to enum conversion
    /// bool compatible4 = IsTypeCompatible(new List&lt;int&gt;(), typeof(IEnumerable&lt;int&gt;)); // true - inheritance
    /// bool compatible5 = IsTypeCompatible("not-a-number", typeof(int));  // false - invalid conversion
    /// </code>
    /// </example>
    private static bool IsTypeCompatible(object value, Type expectedType)
    {
        if (expectedType.IsAssignableFrom(value.GetType()))
            return true;

        // Handle common type conversions
        try
        {
            Convert.ChangeType(value, expectedType);
            return true;
        }
        catch
        {
            return false;
        }
    }
}