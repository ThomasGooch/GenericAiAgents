using Agent.Tools.Models;

namespace Agent.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }

    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    bool ValidateParameters(Dictionary<string, object> parameters);
    Dictionary<string, Type> GetParameterSchema();
}