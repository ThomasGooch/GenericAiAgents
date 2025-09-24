namespace Agent.Core.Models;

public class AgentResult
{
    public bool Success { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static AgentResult CreateSuccess(object? output = null, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult
        {
            Success = true,
            Output = output,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    public static AgentResult CreateError(string error, Dictionary<string, object>? metadata = null)
    {
        return new AgentResult
        {
            Success = false,
            Error = error,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}