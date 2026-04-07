using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Test module that wires a <c>ClerkAgent</c> and a <c>NewsAgent</c> into a
/// sequential pipeline via <see cref="AgentWorkflowBuilder.BuildSequential(IEnumerable{AIAgent})"/>.
/// Each agent uses an independent <see cref="FakeChatClient"/> so that clerk
/// and news responses can be scripted separately.
/// </summary>
internal sealed class SequentialPipelineTestModule : IAgentModule
{
    public FakeChatClient ClerkChatClient { get; } = new();
    public FakeChatClient NewsChatClient { get; } = new();

    public void Register(IHostApplicationBuilder builder)
    {
        // Register one of the clients as IChatClient for any code that resolves
        // the generic IChatClient (e.g. the default agent in Program.cs, which is
        // later removed by the factory).
        builder.Services.AddSingleton<IChatClient>(ClerkChatClient);

        builder.AddAIAgent("AGUIAgent", (sp, key) =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var clerkAgent = new ChatClientAgent(
                ClerkChatClient,
                name: "ClerkAgent",
                instructions: "You are a clerk assistant.");

            var newsAgent = new ChatClientAgent(
                NewsChatClient,
                name: "NewsAgent",
                instructions: "You are a news assistant.");

            var workflow = AgentWorkflowBuilder.BuildSequential(
                key, [clerkAgent, newsAgent]);

            return workflow.AsAIAgent(key, key, "Sequential pipeline test workflow");
        });
    }
}
