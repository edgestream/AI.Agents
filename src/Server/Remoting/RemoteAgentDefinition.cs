using AI.Agents.Server.Configuration;

namespace AI.Agents.Server.Remoting;

internal sealed record RemoteAgentDefinition(
    string Name,
    string Protocol,
    Uri Endpoint,
    string Description)
{
    public static RemoteAgentDefinition FromSettings(string name, RemoteAgentSettings settings)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Remote agent configuration contains an empty agent name.");
        }

        if (!string.Equals(settings.Protocol, RemoteAgentProtocols.AGUI, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Remote agent '{name}' uses unsupported protocol '{settings.Protocol}'. Supported protocols: {RemoteAgentProtocols.AGUI}.");
        }

        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Remote agent '{name}' must configure an absolute HTTP or HTTPS endpoint URI.");
        }

        return new RemoteAgentDefinition(
            name,
            RemoteAgentProtocols.AGUI,
            endpoint,
            string.IsNullOrWhiteSpace(settings.Description)
                ? $"Remote {name} agent."
                : settings.Description);
    }
}
