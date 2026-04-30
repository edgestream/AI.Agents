namespace AI.Agents.Server.RemoteAgents;

internal sealed class RemoteAgentCatalog(IReadOnlyList<RemoteAgentDefinition> agents)
{
    public IReadOnlyList<RemoteAgentDefinition> Agents { get; } = agents;
}
