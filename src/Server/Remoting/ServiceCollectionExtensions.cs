using AI.Agents.Server.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using System.Text.RegularExpressions;

namespace AI.Agents.Server.Remoting;

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
        var definitions = ReadDefinitions(configuration);
        services.AddSingleton(new RemoteAgentCatalog(definitions));

        foreach (var definition in definitions)
        {
            services.AddHttpClient(definition.Name, client =>
            {
                client.BaseAddress = definition.Endpoint;
            });

            services.AddAIAgent(definition.Name, (sp, _) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(definition.Name);
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var chatClient = new AGUIChatClient(
                    httpClient,
                    definition.Endpoint.ToString(),
                    loggerFactory,
                    jsonSerializerOptions: null,
                    serviceProvider: sp);

                return chatClient.AsAIAgent(
                    name: definition.Name,
                    description: definition.Description);
            });
        }

        return services;
    }

    public static IReadOnlyList<AIFunction> CreateRemoteAgentTools(IServiceProvider services)
    {
        var catalog = services.GetRequiredService<RemoteAgentCatalog>();

        return catalog.Agents
            .Select(agent =>
            {
                var remoteAgent = services.GetRequiredKeyedService<AIAgent>(agent.Name);
                return remoteAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = $"delegate_to_{SanitizeAgentName(agent.Name)}",
                    Description = $"Delegate the user's request to the remote '{agent.Name}' agent. {agent.Description}"
                });
            })
            .ToArray();
    }

    private static IReadOnlyList<RemoteAgentDefinition> ReadDefinitions(IConfiguration configuration)
    {
        var settings = configuration
            .GetSection("Agents")
            .Get<Dictionary<string, RemoteAgentSettings>>()
            ?? [];

        return settings
            .Select(pair => RemoteAgentDefinition.FromSettings(pair.Key, pair.Value))
            .ToArray();
    }

    private static string SanitizeAgentName(string name)
    {
        var sanitized = Regex.Replace(name, "[^0-9A-Za-z]+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "agent" : sanitized;
    }
}
