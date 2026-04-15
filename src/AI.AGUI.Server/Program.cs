using AI.AGUI.Hosting;
using AI.AGUI.Server;
using AI.MAF.Skills;
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

// Register class-based skills
builder.Services.AddAgentSkill<DateTimeSkill>();

// Register the skills provider (combines file-based and class-based skills)
builder.Services.AddAgentSkillsProvider(options =>
{
    options.FileSkillsPath = builder.Configuration["Skills:Path"] ?? "skills";
});

builder.AddAIAgent("agui-agent", (sp, id) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var skillsProvider = sp.GetRequiredService<AgentSkillsProvider>();
    var contextProviders = new List<Microsoft.Agents.AI.AIContextProvider> { skillsProvider };
    if (sp.GetService<AI.MCP.Client.McpClientRegistry>() is { } registry)
        contextProviders.Insert(0, new McpClientToolsAIContextProvider(registry));
    var agentOptions = new ChatClientAgentOptions
    {
        Name = id,
        ChatOptions = new ChatOptions
        {
            Instructions = """
                You are a helpful assistant.
                When asked to show, generate, or demo a card or A2UI widget, call the generate_test_card tool.
                """,
            Tools = [FetchAIFunctionFactory.CreateAIFunction(sp), A2UITestCardFunction.Create()]
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
