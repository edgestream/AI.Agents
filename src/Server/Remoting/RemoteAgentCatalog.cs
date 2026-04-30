namespace AI.Agents.Server.Remoting;

internal sealed class RemoteAgentCatalog(IReadOnlyList<RemoteAgentDefinition> agents)
{
    public IReadOnlyList<RemoteAgentDefinition> Agents { get; } = agents;
}
