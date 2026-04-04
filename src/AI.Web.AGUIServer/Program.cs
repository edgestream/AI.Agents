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
    IChatClient inner = sp.GetRequiredService<IChatClient>();
    IChatClient pipeline = new CitationMiddleware(inner);
    ChatClientAgentOptions agentOptions = new()
    {
        Name = key,
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a helpful assistant.",
            Tools = [new HostedWebSearchTool()]
        }
    };
    return new ChatClientAgent(pipeline, agentOptions, sp.GetRequiredService<ILoggerFactory>(), services: sp);
});
builder.Services.AddAGUI();

WebApplication app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("AGUIAgent"));

await app.RunAsync();

public partial class Program { }