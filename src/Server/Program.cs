using AI.Agents.Microsoft;
using AI.Agents.Microsoft.Auth;
using AI.Agents.Microsoft.Skills;
using AI.Agents.MCP;
using AI.Agents.OAuth;
using AI.Agents.Server;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider is marked experimental

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var defaultModelId = builder.Configuration["OpenAI:ModelId"]
    ?? builder.Configuration["AzureOpenAI:DeploymentName"]
    ?? builder.Configuration["Foundry:ModelId"]
    ?? "gpt-5.3-chat";

builder.Services.AddGraphUserProfileService();
builder.Services.AddAIClient(builder.Configuration);
builder.Services.AddAIAgentSkill<UserProfileSkill>();
builder.Services.AddAIAgent("agui-agent", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var skillsProvider = sp.GetRequiredService<AgentSkillsProvider>();
    return chatClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "Generic Agent",
            ChatOptions = new()
            {
                ModelId = defaultModelId,
                Instructions = """You are a helpful assistant."""
            },
            AIContextProviders = [skillsProvider]
        },
        services: sp);
});
builder.Services.AddMCPClient();
builder.Services.AddMCPOAuth();
builder.Services.AddMCPAuthorizationService();

var app = builder.Build();

app.UseEntraAuthMiddleware();

app.MapGraphProfileEndpoint("/api/me");
app.MapOAuthEndpoints();
app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("agui-agent"));

await app.RunAsync();