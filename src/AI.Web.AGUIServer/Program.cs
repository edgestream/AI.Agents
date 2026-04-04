using AI.Web.AGUIServer;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

builder.AddAIClient();
builder.AddAIAgent("AGUIAgent", (sp, key) =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var displaySourcesTool = AIFunctionFactory.Create(CitationTool.DisplaySources);
    ChatClientAgentOptions agentOptions = new()
    {
        Name = key,
        ChatOptions = new ChatOptions
        {
            Instructions = """
                You are a helpful assistant.

                When the user asks for sources, references, or citations and you can provide concrete supporting links,
                call the DisplaySources tool before your final answer.

                Each source must include Title, Url, and Snippet.
                Only include sources you actually relied on.
                Do not invent URLs.
                Do not mention tool names in the final answer.
                """,
            Tools = [displaySourcesTool, new HostedWebSearchTool()]
        }
    };

    AIAgent agent = new ChatClientAgent(sp.GetRequiredService<IChatClient>(), agentOptions, loggerFactory, services: sp);
    return new AnnotationCitationAgent(agent, loggerFactory.CreateLogger<AnnotationCitationAgent>());
});
builder.Services.AddAGUI();

WebApplication app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("AGUIAgent"));

await app.RunAsync();

public partial class Program { }