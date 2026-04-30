using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FakeNewsChatClient>();
builder.Services.AddKeyedSingleton<AIAgent>("news", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<FakeNewsChatClient>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    return new ChatClientAgent(
        chatClient,
        new ChatClientAgentOptions
        {
            Name = key?.ToString() ?? "news",
            Description = "Mock news agent that returns structured Fake News stories.",
            ChatOptions = new()
            {
                Instructions = "Return deterministic Fake News stories."
            }
        },
        loggerFactory,
        services: sp);
});

var app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("news"));

await app.RunAsync();
