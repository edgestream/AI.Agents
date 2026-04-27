using AI.Agents.Microsoft;
using AI.Agents.Microsoft.Auth;
using AI.Agents.MCP;
using AI.Agents.OAuth;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using AI.Agents.AGUI;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var defaultModelId = builder.Configuration["OpenAI:ModelId"]
    ?? builder.Configuration["AzureOpenAI:DeploymentName"]
    ?? builder.Configuration["Foundry:ModelId"]
    ?? "gpt-5.3-chat";

builder.Services.AddGraphUserProfileService();
builder.Services.AddAIClient(builder.Configuration);
builder.Services.AddAIAgent("agui-agent", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return chatClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "Generic Agent",
            ChatOptions = new()
            {
                ModelId = defaultModelId,
                Instructions = """You are a helpful assistant.""",
                Tools = [UserProfileFunctionFactory.Create(sp)]
            }
        },
        services: sp);
});

var app = builder.Build();

app.UseEntraAuthMiddleware();
app.UseAGUIRequestMiddleware();

app.MapGraphProfileEndpoint("/api/me");
app.MapGet("/api/health", () => "OK");

var agent = app.Services.GetRequiredKeyedService<AIAgent>("agui-agent");

app.MapAGUI("/", agent).AddAuthenticatedFilter();

await app.RunAsync();