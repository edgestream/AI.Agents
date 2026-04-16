using AI.AGUI.Auth;
using AI.MAF.Client;
using AI.MAF.Skills;
using AI.MCP.Client;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider is marked experimental

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

// Add authentication services
builder.Services.AddUserContext();
builder.Services.AddMcpOAuth();

// Add MCP client for OAuth configuration lookup
builder.Services.AddMCPClient();

builder.Services.AddAIProjectClient();
builder.Services.AddAIAgentSkill<DateTimeSkill>();
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

var app = builder.Build();

// Add user context middleware early in the pipeline
app.UseUserContext();

app.MapGet("/health", () => "OK");

// Map user info endpoint for frontend
app.MapGet("/api/me", (IUserContextAccessor userContextAccessor) =>
{
    var userContext = userContextAccessor.UserContext;
    if (!userContext.IsAuthenticated)
    {
        return Results.Json(new
        {
            authenticated = false
        });
    }
    return Results.Json(new
    {
        authenticated = true,
        userId = userContext.UserId,
        displayName = userContext.DisplayName,
        email = userContext.Email,
        picture = userContext.Picture
    });
});

// Map OAuth endpoints
app.MapOAuthEndpoints();

app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("agui-agent"));

await app.RunAsync();