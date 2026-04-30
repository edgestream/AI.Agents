using AI.Agents.AGUI;
using AI.Agents.Microsoft;
using AI.Agents.Microsoft.Authentication;
using AI.Agents.Server.Authorization;
using AI.Agents.Server.Configuration;
using AI.Agents.Server.RemoteAgents;
using AI.Agents.Server.Tools;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var defaultModelId = builder.Configuration["OpenAI:ModelId"]
    ?? builder.Configuration["AzureOpenAI:DeploymentName"]
    ?? builder.Configuration["Foundry:ModelId"]
    ?? "gpt-5.3-chat";

builder.Services.AddOptions<AuthSettings>().BindConfiguration("Auth");
builder.Services.AddHttpClient();
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
builder.Services.AddRemoteAgents(builder.Configuration);
builder.Services.AddAIAgent("clerk", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var tools = new List<AITool>
    {
        UserProfileFunctionFactory.Create(sp),
        FetchAIFunctionFactory.CreateAIFunction(sp)
    };
    tools.AddRange(RemoteAgentServiceCollectionExtensions.CreateRemoteAgentTools(sp));

    return chatClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "Generic Agent",
            ChatOptions = new()
            {
                ModelId = defaultModelId,
                Instructions = """You are a helpful assistant.""",
                Tools = tools
            },
            AIContextProviders = [sp.GetRequiredService<AGUIAIContextProvider>()]
        },
        services: sp);
});

var app = builder.Build();

app.UseEntraAuth();
app.UseAGUIRequestMiddleware();

var clerkAgent = app.Services.GetRequiredKeyedService<AIAgent>("clerk");
var aguiEndpoint = app.MapAGUI("/", clerkAgent);
var authSettings = app.Services.GetRequiredService<IOptions<AuthSettings>>().Value;
if (authSettings.AgentRequiresAuthentication)
{
    aguiEndpoint.RequireAuthorization(AuthorizationPolicies.AgentAuthenticated);
}
app.MapGet("/api/health", () => "OK");
app.MapGraphUserProfileEndpoint("/api/me");

await app.RunAsync();
