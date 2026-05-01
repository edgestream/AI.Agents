using AI.Agents.Server.Configuration;

namespace AI.Agents.Server.Catalog;

internal sealed record AgentDefinition(
    string Name,
    string Protocol,
    Uri Endpoint,
    string Description)
{
    public static AgentDefinition FromSettings(string name, AgentSettings settings)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Agent configuration contains an empty agent name.");
        }
        if (!string.Equals(settings.Protocol, RemoteAgentProtocol.AGUI, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Remote agent '{name}' uses unsupported protocol '{settings.Protocol}'. Supported protocols: {RemoteAgentProtocol.AGUI}.");
        }
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Remote agent '{name}' must configure an absolute HTTP or HTTPS endpoint URI.");
        }
        return new AgentDefinition(
            name,
            RemoteAgentProtocol.AGUI,
            endpoint,
            string.IsNullOrWhiteSpace(settings.Description)
                ? $"Remote {name} agent."
                : settings.Description);
    }
}
