using System.Text.RegularExpressions;
using AI.Agents.Server.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Agents.Server.RemoteAgents;

internal static class RemoteAgentServiceCollectionExtensions
{
    public static IServiceCollection AddRemoteAgents(
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
