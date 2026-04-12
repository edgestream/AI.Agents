using AI.AGUI.Hosting;
using AI.MAF.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider is marked experimental

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
builder.AddAIClient();
builder.Services.AddHttpClient();
builder.AddAIAgent("agui-agent", (sp, id) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var skillsPath = configuration["Skills:Path"] ?? "skills";
    var skillsProvider = new AgentSkillsProvider(
        Path.Combine(AppContext.BaseDirectory, skillsPath),
        loggerFactory: loggerFactory);
    var contextProviders = new List<Microsoft.Agents.AI.AIContextProvider> { skillsProvider };
    if (sp.GetService<AI.MCP.Client.McpClientRegistry>() is { } registry)
        contextProviders.Insert(0, new McpClientToolsAIContextProvider(registry));
    var agentOptions = new ChatClientAgentOptions
    {
        Name = id,
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a helpful assistant.",
            Tools = [FetchAIFunctionFactory.CreateAIFunction(sp)]
        },
        AIContextProviders = [..contextProviders],
    };
    return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services: sp);
});

var app = builder.Build();
app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("agui-agent"));
await app.RunAsync();

#pragma warning restore MAAI001

public partial class Program { }
