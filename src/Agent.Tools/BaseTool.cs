using Agent.Tools.Models;
using System.Reflection;

namespace Agent.Tools;

public abstract class BaseTool : ITool
{
    public string Name { get; }
    public string Description { get; }

    private readonly Dictionary<string, Type> _parameterSchema;

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

    public Dictionary<string, Type> GetParameterSchema()
    {
        return new Dictionary<string, Type>(_parameterSchema);
    }

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

    protected abstract Dictionary<string, Type> DefineParameterSchema();
    protected abstract Task<ToolResult> ExecuteInternalAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken);

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