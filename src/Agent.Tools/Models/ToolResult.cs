namespace Agent.Tools.Models;

public class ToolResult
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the output data as a string (alias for Data?.ToString())
    /// </summary>
    public string? Output => Data?.ToString();

    public static ToolResult CreateSuccess(object? data = null, Dictionary<string, object>? metadata = null)
    {
        return new ToolResult
        {
            Success = true,
            Data = data,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    public static ToolResult CreateError(string error, Dictionary<string, object>? metadata = null)
    {
        return new ToolResult
        {
            Success = false,
            Error = error,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
}