namespace Agent.Core.Models;

public class AgentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Input { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public CancellationToken CancellationToken { get; set; } = default;
}