using AI.AGUI.Hosting;
using MealPlanner;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider is experimental

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("applications.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
builder.WebHost.UseUrls("http://localhost:8000");
builder.AddAIClient();
builder.Services.AddHttpClient();
builder.AddAGUIApplication("meal-planner", "Meal Planner", (sp, id) =>
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
            Instructions = """
                You are a helpful meal planning assistant.

                When the user asks for a recipe, call the search_recipes tool.
                The tool returns a rendered recipe card that is displayed automatically — do not re-summarize the recipe as text.
                """,
            Tools = [SearchRecipesFunction.CreateAIFunction()],
        },
        AIContextProviders = [..contextProviders],
    };
    return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services: sp);
});

var app = builder.Build();
app.MapGet("/health", () => "OK");
app.MapAGUI();
await app.RunAsync();

#pragma warning restore MAAI001
