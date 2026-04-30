namespace AI.Agents.Server.Configuration;

public sealed class RemoteAgentSettings
{
    public string Protocol { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string? Description { get; set; }
}
