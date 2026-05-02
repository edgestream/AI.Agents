using AI.Agents.AGUI;
using AI.Agents.Microsoft;
using AI.Agents.Microsoft.Authentication;
using AI.Agents.Server.Authorization;
using AI.Agents.Server.Catalog;
using AI.Agents.Server.Configuration;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

#pragma warning disable MAAIW001 // AgentWorkflowBuilder is experimental

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var defaultModelId = builder.Configuration["OpenAI:ModelId"]
    ?? builder.Configuration["AzureOpenAI:DeploymentName"]
    ?? builder.Configuration["Foundry:ModelId"]
    ?? builder.Configuration["Codex:ModelId"]
    ?? "gpt-5.4";

builder.Services.AddHttpClient();
builder.Services.AddOptions<AuthSettings>().BindConfiguration("Auth");
builder.Services.AddEntraAuth();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.AgentAuthenticated, policy =>
    {
        policy
            .AddAuthenticationSchemes(EntraAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser();
    });
builder.Services.AddGraphUserProfileService();
builder.Services.AddAGUIContextProvider();
builder.Services.AddAIClient(builder.Configuration);
builder.Services.AddAIAgents(builder.Configuration);
builder.Services.AddAIAgent("default", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var frontAgent = chatClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "Front agent",
            ChatOptions = new()
            {
                ModelId = defaultModelId,
                Instructions = """
                Route messages to the appropriate agent.
                If the message can't be handled by an agent,
                try to help the user as best as you can.
                """
            },
        },
        services: sp
    );
    var agentCatalog = sp.GetService<AgentCatalog>();
    if (agentCatalog is null || !agentCatalog.AgentDefinitions.Any()) return frontAgent;
    var otherAgents = agentCatalog.AgentDefinitions.Select(x => sp.GetRequiredKeyedService<AIAgent>(x.Name.ToLowerInvariant()));
    return AgentWorkflowBuilder.CreateHandoffBuilderWith(frontAgent)
        .WithHandoffs(frontAgent, otherAgents)
        .Build()
        .AsAIAgent(
            id: key,
            name: key,
            description: "Workflow to route messages to the appropriate agent."
        );
});

var app = builder.Build();

app.UseEntraAuth();
app.UseAGUIRequestMiddleware();

var defaultAgent = app.Services.GetRequiredKeyedService<AIAgent>("default");
var aguiEndpoint = app.MapAGUI("/", defaultAgent);
var authSettings = app.Services.GetRequiredService<IOptions<AuthSettings>>().Value;
if (authSettings.AgentRequiresAuthentication)
{
    aguiEndpoint.RequireAuthorization(AuthorizationPolicies.AgentAuthenticated);
}
app.MapGet("/api/health", () => "OK");
app.MapGraphUserProfileEndpoint("/api/me");

await app.RunAsync();
