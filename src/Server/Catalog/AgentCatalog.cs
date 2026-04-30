namespace AI.Agents.Server.Catalog;

internal sealed class AgentCatalog(IReadOnlyList<AgentDefinition> agentDefinitions)
{
    public IReadOnlyList<AgentDefinition> AgentDefinitions { get; } = agentDefinitions;
}
