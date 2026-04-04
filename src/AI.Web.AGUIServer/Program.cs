using AI.MCP.Client;
using AI.Web.AGUIServer;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

builder.AddAIClient();
builder.AddMCPClient();
builder.AddAIAgent("AGUIAgent", (sp, key) =>
{
    var clientRegistry = sp.GetRequiredService<McpClientRegistry>();
    var toolsContext = new McpClientToolsAIContextProvider(clientRegistry);
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    // Foundry Responses Agent: AIProjectClient is registered when Foundry:ProjectEndpoint is set.
    // AsAIAgent requires ChatOptions.ModelId — read it from config here.
    var projectClient = sp.GetService<AIProjectClient>();
    if (projectClient is not null)
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var foundryOptions = new ChatClientAgentOptions
        {
            Name = key,
            ChatOptions = new ChatOptions
            {
                ModelId = config["Foundry:Model"]
                    ?? throw new InvalidOperationException("Foundry:Model is not configured."),
                Instructions = "You are a helpful assistant.",
            },
            AIContextProviders = [toolsContext],
        };
        return projectClient.AsAIAgent(foundryOptions, clientFactory: null, loggerFactory: loggerFactory, services: sp);
    }

    // Azure OpenAI (default): IChatClient is registered directly.
    var chatOptions = new ChatOptions { Instructions = "You are a helpful assistant." };
    var agentOptions = new ChatClientAgentOptions { Name = key, ChatOptions = chatOptions, AIContextProviders = [toolsContext] };
    var chatClient = sp.GetRequiredService<IChatClient>();
    return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services: sp);
});
builder.Services.AddAGUI();

WebApplication app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("AGUIAgent"));

await app.RunAsync();

public partial class Program { }