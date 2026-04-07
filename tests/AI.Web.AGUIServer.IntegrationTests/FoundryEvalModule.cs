using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Eval module that wires real Foundry clients for end-to-end validation.
/// Calls <see cref="HostApplicationBuilderExtensions.AddFoundryResponsesAgentClient"/>
/// and registers a <c>ChatClientAgent</c>-backed agent so that the full
/// Foundry provider path is exercised. Intended for eval runs that have
/// real Azure credentials available.
/// </summary>
internal sealed class FoundryEvalModule : IAgentModule
{
    public void Register(IHostApplicationBuilder builder)
    {
        builder.AddFoundryResponsesAgentClient();

        builder.AddAIAgent("AGUIAgent", (sp, key) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var agentOptions = new ChatClientAgentOptions
            {
                Name = key,
                ChatOptions = new ChatOptions
                {
                    Instructions = "You are a helpful assistant."
                },
            };
            return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services: sp);
        });
    }
}
