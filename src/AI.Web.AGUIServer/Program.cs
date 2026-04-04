using AI.MCP.Client;
using AI.Web.AGUIServer;
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
    var chatClient = sp.GetRequiredService<IChatClient>();
    var clientRegistry = sp.GetRequiredService<McpClientRegistry>();
    var toolsContext = new McpClientToolsAIContextProvider(clientRegistry);
    var chatOptions = new ChatOptions { Instructions = "You are a helpful assistant." };
    var agentOptions = new ChatClientAgentOptions { ChatOptions = chatOptions, AIContextProviders = [toolsContext] };
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services: sp);
});
builder.Services.AddAGUI();

WebApplication app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredService<AIAgent>());

await app.RunAsync();

public partial class Program { }