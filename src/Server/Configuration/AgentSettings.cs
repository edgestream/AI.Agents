namespace AI.Agents.Server.Configuration;

public sealed class AgentSettings
{
    public string Protocol { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? Description { get; set; }
}
