using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Test module that wires the full fan-out/fan-in news pipeline using
/// <see cref="FakeChatClient"/> instances in place of real agents.
/// Mirrors the structure of <see cref="NewsPipelineModule"/> but without
/// real Foundry or HTTP dependencies.
/// </summary>
internal sealed class NewsPipelineTestModule : IAgentModule
{
    public FakeChatClient ClerkChatClient { get; } = new();
    public FakeChatClient TagesschauChatClient { get; } = new();
    public FakeChatClient HeiseChatClient { get; } = new();
    public FakeChatClient DigestChatClient { get; } = new();

    public void Register(IHostApplicationBuilder builder)
    {
        // Register a placeholder IChatClient so that the production AddAIClient()
        // call in Program.cs succeeds.
        builder.Services.AddSingleton<IChatClient>(ClerkChatClient);

        builder.AddAIAgent("AGUIAgent", (sp, key) =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var clerkAgent = new ChatClientAgent(
                ClerkChatClient,
                name: "ClerkAgent",
                instructions: "You are a helpful clerk assistant.");

            var tagesschauAgent = new ChatClientAgent(
                TagesschauChatClient,
                name: "TagesschauAgent",
                instructions: "You fetch Tagesschau news and return a JSON article list.");

            var heiseAgent = new ChatClientAgent(
                HeiseChatClient,
                name: "HeiseNewsAgent",
                instructions: "You fetch Heise News and return a JSON article list.");

            var digestAgent = new ChatClientAgent(
                DigestChatClient,
                name: "NewsDigestAgent",
                instructions: "You create a news digest from the provided article list.");

            // Fan-out/fan-in: Tagesschau + Heise in parallel → merged messages.
            var newsSourcesWorkflow = AgentWorkflowBuilder.BuildConcurrent(
                new AIAgent[] { tagesschauAgent, heiseAgent },
                MergeMessages);

            var newsSourcesAgent = newsSourcesWorkflow.AsAIAgent(
                "NewsSourcesAgent", "NewsSourcesAgent",
                "Parallel news sources (fake)");

            // Sequential pipeline: news sources → digest.
            var newsPipelineWorkflow = AgentWorkflowBuilder.BuildSequential(
                "NewsPipeline", new AIAgent[] { newsSourcesAgent, digestAgent });

            var newsPipelineAgent = newsPipelineWorkflow.AsAIAgent(
                "NewsPipelineAgent", "NewsPipelineAgent",
                "News pipeline: sources → digest (fake)");

            // Full workflow: clerk → news pipeline.
            var mainWorkflow = AgentWorkflowBuilder.BuildSequential(
                key, new AIAgent[] { clerkAgent, newsPipelineAgent });

            return mainWorkflow.AsAIAgent(key, key, "Full news pipeline (test)");
        });
    }

    private static List<ChatMessage> MergeMessages(IList<List<ChatMessage>> agentResults)
    {
        var texts = agentResults
            .Select(msgs => msgs.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text ?? "")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var merged = string.Join("; ", texts);
        return [new ChatMessage(ChatRole.User, $"Digest request: {merged}")];
    }
}
