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
    ?? "gpt-5.3-chat";

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
    var clerkAgent = chatClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "Clerk agent handles front inquiries.",
            ChatOptions = new()
            {
                ModelId = defaultModelId,
                Instructions = """
                You are routing requests to other agents.
                If there are no agents available to handle a request, try to handle it yourself.
                """
            },
            AIContextProviders = [
                sp.GetRequiredService<AGUIAIContextProvider>()
            ]
        },
        services: sp
    );
    var agentCatalog = sp.GetService<AgentCatalog>();
    if (agentCatalog is not null && agentCatalog.AgentDefinitions.Count > 0)
    {
        var agents = agentCatalog.AgentDefinitions.Select(d => sp.GetRequiredKeyedService<AIAgent>(d.Name)).ToList();
        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(clerkAgent)
            .WithHandoffs(clerkAgent, agents)
            .Build();
        return workflow.AsAIAgent(
            id: "default", 
            name: "default", 
            description: "Agent with handoffs to sub-agents"
        );
    }
    else
    {
        return clerkAgent;
    }
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
