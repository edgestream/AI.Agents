using AI.Agents.Server.Configuration;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AI.Agents.Server.Catalog;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds remote agents defined in the configuration to the service collection. Remote agents are expected to be hosted separately and communicate using the AGUI protocol. Each remote agent is registered as an IChatClient-based AIAgent and also exposed as an AIFunction tool for delegation from other agents.
    /// </summary>
    /// <param name="services">The service collection to add the agents to.</param>
    /// <param name="configuration">The configuration containing the remote agent definitions.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAIAgents(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var definitions = BindDefinitions(configuration);
        var catalog = new AgentCatalog(definitions);
        services.AddSingleton(catalog);
        foreach (var definition in definitions)
        {
            switch (definition.Protocol)
            {
                case RemoteAgentProtocol.AGUI:
                    services.AddHttpClient(definition.Name.ToLowerInvariant(), client => { client.BaseAddress = definition.Endpoint; });
                    services.AddAIAgent(definition.Name.ToLowerInvariant(), (sp, key) =>
                    {
                        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(key);
                        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                        var chatClient = new AGUIChatClient(
                            httpClient,
                            definition.Endpoint.ToString(),
                            loggerFactory,
                            jsonSerializerOptions: null,
                            serviceProvider: sp);
                        return chatClient.AsAIAgent(
                            name: key,
                            description: definition.Description);
                    });
                    break;
                default:
                    throw new NotSupportedException($"Unsupported agent protocol: {definition.Protocol}");
            }
        }
        return services;
    }

    private static IReadOnlyList<AgentDefinition> BindDefinitions(IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("Agents")
            .Get<Dictionary<string, AgentSettings>>()
            ?? [];
        return settings
            .Select(pair => AgentDefinition.FromSettings(pair.Key, pair.Value))
            .ToArray();
    }
}
