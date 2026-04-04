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
    var chatOptions = new ChatOptions { Instructions = "You are a helpful assistant." };
    var agentOptions = new ChatClientAgentOptions { ChatOptions = chatOptions, AIContextProviders = [toolsContext] };
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    // Foundry Responses Agent: AIProjectClient is registered when AI:Provider = "Foundry".
    // AsAIAgent creates a server-side Responses Agent on the Foundry project endpoint;
    // the IChatClient stored in ChatClientAgent.ChatClient carries the Foundry inference
    // channel, and this outer ChatClientAgent layer adds MCP context providers on top.
    var projectClient = sp.GetService<AIProjectClient>();
    if (projectClient is not null)
        return projectClient.AsAIAgent(agentOptions, clientFactory: null, loggerFactory: loggerFactory, services: sp);

    // Azure OpenAI (default): IChatClient is registered directly.
    var chatClient = sp.GetRequiredService<IChatClient>();
    return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services: sp);
});
builder.Services.AddAGUI();

WebApplication app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("AGUIAgent"));

await app.RunAsync();

public partial class Program { }