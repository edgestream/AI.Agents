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

builder.Services.AddControllers();
builder.Services.AddGraphUserProfileService();
builder.Services.AddAIProjectClient();
builder.Services.AddAIAgentSkill<UserProfileSkill>();
builder.Services.AddAIAgent("agui-agent", (sp, key) =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    var skillsProvider = sp.GetRequiredService<AgentSkillsProvider>();
    return projectClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "Generic Agent",
            ChatOptions = new()
            {
                ModelId = "gpt-5.3-chat",
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

app.MapControllers();
app.MapGraphProfileEndpoint("/api/me");
app.MapOAuthEndpoints();
app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("agui-agent"));

await app.RunAsync();