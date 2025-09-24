namespace Agent.Core.Models;

public class AgentConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Settings { get; set; } = new();
    public List<string> RequiredTools { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRetries { get; set; } = 3;
}