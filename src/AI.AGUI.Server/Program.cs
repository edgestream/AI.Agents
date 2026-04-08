using AI.AGUI.Server;
using AI.MAF.Tools;
using AI.MCP.Client;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
builder.AddAIClient();
builder.AddMCPClient();
builder.LoadAgentModule();
if (!builder.Services.Any(static d => d.IsKeyedService && d.ServiceType == typeof(AIAgent)))
{
    builder.AddAIAgent("AGUIAgent", (sp, key) =>
    {
        var clientRegistry = sp.GetRequiredService<McpClientRegistry>();
        var toolsContext = new McpClientToolsAIContextProvider(clientRegistry);
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var agentOptions = new ChatClientAgentOptions
        {
            Name = key,
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a helpful assistant.",
                Tools = [FetchAIFunctionFactory.CreateAIFunction(sp)]
            },
            AIContextProviders = [toolsContext],
        };
        return new ChatClientAgent(sp.GetRequiredService<IChatClient>(), agentOptions, loggerFactory, services: sp);
    });
}
builder.Services.AddAGUI();

WebApplication app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("AGUIAgent"));

await app.RunAsync();

public partial class Program { }