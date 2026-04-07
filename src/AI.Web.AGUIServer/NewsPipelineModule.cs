#pragma warning disable MAAI001
#pragma warning disable OPENAI001

using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Web.AGUIServer;

/// <summary>
/// <see cref="IAgentModule"/> that packages the complete news digest pipeline:
/// <list type="bullet">
///   <item><see cref="ClerkAgent"/> — routes news intents and answers general queries directly.</item>
///   <item><see cref="TagesschauAgent"/> — MAF <see cref="ChatClientAgent"/> with a local AI function
///   that fetches headlines from the Tagesschau REST API.</item>
///   <item><see cref="HeiseNewsAgent"/> — MAF <see cref="ChatClientAgent"/> with a local AI function
///   that fetches headlines from the Heise News Atom feed.</item>
///   <item><see cref="NewsDigestAgent"/> — Foundry-hosted fan-in agent that merges parallel source
///   outputs into a single readable digest.</item>
/// </list>
/// <para>
/// Activation: set <c>"AgentModule": "AI.Web.AGUIServer"</c> in configuration.
/// </para>
/// <para>
/// When <c>"Foundry:NewsDigestAgent"</c> is absent the module falls back to a single
/// <see cref="ClerkAgent"/> that answers all queries directly, so the server continues to
/// function without Azure Foundry credentials.
/// </para>
/// </summary>
public sealed class NewsPipelineModule : IAgentModule
{
    /// <inheritdoc/>
    public void Register(IHostApplicationBuilder builder)
    {
        // Named HTTP clients — configurable via appsettings, with sensible defaults.
        builder.Services.AddHttpClient("tagesschau", (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["TagesschauAgent:BaseUrl"] ?? FetchTagesschauNewsFunction.DefaultBaseUrl;
            client.BaseAddress = new Uri(baseUrl);
        });

        builder.Services.AddHttpClient("heise", (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["HeiseNewsAgent:BaseUrl"] ?? FetchHeiseNewsFunction.DefaultBaseUrl;
            client.BaseAddress = new Uri(baseUrl);
        });

        // AGUIAgent factory — builds the full fan-out/fan-in workflow when
        // Foundry:NewsDigestAgent is configured, or falls back to ClerkAgent alone.
        builder.AddAIAgent("AGUIAgent", (sp, key) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var config = sp.GetRequiredService<IConfiguration>();
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();

            var newsDigestAgentName = config["Foundry:NewsDigestAgent"];

            // ClerkAgent is always created — used as fallback when no NewsDigestAgent.
            var clerkAgent = CreateClerkAgent(chatClient, loggerFactory, sp);

            if (string.IsNullOrEmpty(newsDigestAgentName))
            {
                // Fallback: single ClerkAgent answers all queries directly.
                var fallbackWorkflow = AgentWorkflowBuilder.BuildSequential(
                    key, new AIAgent[] { clerkAgent });
                return fallbackWorkflow.AsAIAgent(key, key, "General purpose assistant");
            }

            // Build source agents with their fetch functions.
            var tagesschauFn = new FetchTagesschauNewsFunction(
                httpFactory, config["TagesschauAgent:BaseUrl"]).CreateAIFunction();

            var heiseFn = new FetchHeiseNewsFunction(
                httpFactory, config["HeiseNewsAgent:BaseUrl"]).CreateAIFunction();

            var tagesschauAgent = CreateSourceAgent(
                chatClient, loggerFactory, sp, "TagesschauAgent", tagesschauFn,
                "You are a news-fetching assistant. " +
                "Call the FetchTagesschauNews function to retrieve the latest Tagesschau headlines " +
                "and return the result as a raw JSON array of articles. Do not summarise.");

            var heiseAgent = CreateSourceAgent(
                chatClient, loggerFactory, sp, "HeiseNewsAgent", heiseFn,
                "You are a news-fetching assistant. " +
                "Call the FetchHeiseNews function to retrieve the latest Heise News headlines " +
                "and return the result as a raw JSON array of articles. Do not summarise.");

            // Fan-out: run both source agents in parallel and merge their article lists.
            var newsSourcesWorkflow = AgentWorkflowBuilder.BuildConcurrent(
                new AIAgent[] { tagesschauAgent, heiseAgent },
                MergeArticleLists);

            var newsSourcesAgent = newsSourcesWorkflow.AsAIAgent(
                "NewsSourcesAgent", "NewsSourcesAgent",
                "Parallel news source fetcher (Tagesschau + Heise)");

            // NewsDigestAgent — Foundry-hosted fan-in merger.
            var projectEndpoint = config["Foundry:ProjectEndpoint"];
            if (string.IsNullOrEmpty(projectEndpoint))
                throw new InvalidOperationException(
                    "Foundry:ProjectEndpoint must be set when Foundry:NewsDigestAgent is configured.");

            var projectClient = new AIProjectClient(
                new Uri(projectEndpoint), new DefaultAzureCredential());

            var agentReference = new AgentReference(newsDigestAgentName);
            var newsDigestAgent = projectClient.AsAIAgent(agentReference);

            // Fan-in chain: news sources → digest.
            var newsPipelineWorkflow = AgentWorkflowBuilder.BuildSequential(
                "NewsPipeline", new AIAgent[] { newsSourcesAgent, newsDigestAgent });

            var newsPipelineAgent = newsPipelineWorkflow.AsAIAgent(
                "NewsPipelineAgent", "NewsPipelineAgent",
                "Fan-out/fan-in news pipeline: Tagesschau + Heise → digest");

            // Full workflow: clerk routing → news pipeline.
            var mainWorkflow = AgentWorkflowBuilder.BuildSequential(
                key, new AIAgent[] { clerkAgent, newsPipelineAgent });

            return mainWorkflow.AsAIAgent(key, key,
                "News digest pipeline with clerk routing");
        });
    }

    // --- helpers ---

    private static ChatClientAgent CreateClerkAgent(
        IChatClient chatClient,
        ILoggerFactory loggerFactory,
        IServiceProvider sp)
    {
        var options = new ChatClientAgentOptions
        {
            Name = "ClerkAgent",
            ChatOptions = new ChatOptions
            {
                Instructions =
                    "You are a helpful assistant. " +
                    "For general questions, answer directly. " +
                    "When the user asks about news or current events, " +
                    "acknowledge that you will retrieve the latest headlines.",
            },
        };
        return new ChatClientAgent(chatClient, options, loggerFactory, services: sp);
    }

    private static ChatClientAgent CreateSourceAgent(
        IChatClient chatClient,
        ILoggerFactory loggerFactory,
        IServiceProvider sp,
        string name,
        AIFunction fetchFunction,
        string instructions)
    {
        var options = new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Tools = [fetchFunction],
            },
        };
        return new ChatClientAgent(chatClient, options, loggerFactory, services: sp);
    }

    /// <summary>
    /// Aggregator for <see cref="AgentWorkflowBuilder.BuildConcurrent"/>:
    /// merges article JSON arrays from all parallel source agents into one list
    /// and returns it as a single <see cref="ChatRole.User"/> message for
    /// <c>NewsDigestAgent</c>.
    /// </summary>
    private static List<ChatMessage> MergeArticleLists(IList<List<ChatMessage>> agentResults)
    {
        var allArticles = new List<NewsArticle>();

        foreach (var messages in agentResults)
        {
            var lastAssistantText = messages
                .LastOrDefault(m => m.Role == ChatRole.Assistant)
                ?.Text;

            if (string.IsNullOrWhiteSpace(lastAssistantText))
                continue;

            try
            {
                var articles = JsonSerializer.Deserialize<List<NewsArticle>>(
                    lastAssistantText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (articles is not null)
                    allArticles.AddRange(articles);
            }
            catch (JsonException)
            {
                // Non-JSON response from this source agent — skip gracefully.
            }
        }

        var mergedJson = JsonSerializer.Serialize(allArticles);
        return [new ChatMessage(ChatRole.User,
            $"Please create a news digest from the following articles:\n{mergedJson}")];
    }
}
