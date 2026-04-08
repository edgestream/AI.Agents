using AI.AGUI.Server;
using AI.MAF.Tools;
using AI.MCP.Client;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider is marked experimental; suppress until the API stabilises in a future Microsoft.Agents.AI release

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
        var configuration = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var skillsPath = configuration["Skills:Path"] ?? "skills";
        var skillsProvider = new AgentSkillsProvider(
            Path.Combine(AppContext.BaseDirectory, skillsPath),
            loggerFactory: loggerFactory);
        var agentOptions = new ChatClientAgentOptions
        {
            Name = key,
            ChatOptions = new ChatOptions
            {
                Instructions = "You are a helpful assistant.",
                Tools = [FetchAIFunctionFactory.CreateAIFunction(sp)]
            },
            AIContextProviders = [toolsContext, skillsProvider],
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