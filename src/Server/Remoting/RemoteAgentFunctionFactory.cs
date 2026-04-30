using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text.RegularExpressions;

namespace AI.Agents.Server.Remoting;

internal class RemoteAgentFunctionFactory
{
    public static IReadOnlyList<AIFunction> CreateAIFunctions(IServiceProvider serviceProvider)
    {
        var catalog = serviceProvider.GetRequiredService<RemoteAgentCatalog>();
        return [..catalog.Agents.Select(agent =>
            {
                var remoteAgent = serviceProvider.GetRequiredKeyedService<AIAgent>(agent.Name);
                return remoteAgent.AsAIFunction(new AIFunctionFactoryOptions
                {
                    Name = $"handoff_to_{SanitizeAgentName(agent.Name)}",
                    Description = $"Delegate the user's request to the remote '{agent.Name}' agent. {agent.Description}"
                });
            }
        )];
    }

    private static string SanitizeAgentName(string name)
    {
        var sanitized = Regex.Replace(name, "[^0-9A-Za-z]+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "agent" : sanitized;
    }
}
