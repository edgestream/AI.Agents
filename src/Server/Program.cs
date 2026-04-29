using AI.Agents.AGUI;
using AI.Agents.Microsoft;
using AI.Agents.Microsoft.Authentication;
using AI.Agents.Server.Authorization;
using AI.Agents.Server.Configuration;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var defaultModelId = builder.Configuration["OpenAI:ModelId"]
    ?? builder.Configuration["AzureOpenAI:DeploymentName"]
    ?? builder.Configuration["Foundry:ModelId"]
    ?? "gpt-5.3-chat";

builder.Services.AddGraphUserProfileService();
builder.Services.AddEntraAuth();
builder.Services.AddOptions<AuthSettings>().BindConfiguration("Auth");
builder.Services.AddAuthorizationBuilder().AddPolicy(
    AuthorizationPolicies.AgentRequiresAuthentication, policy => {
        policy.RequireAssertion(context =>
            !builder.Configuration.GetValue<bool>("Auth:AgentRequiresAuthentication")
            || context.User.Identity?.IsAuthenticated == true);
    }
);
builder.Services.AddAGUIContextProvider();
builder.Services.AddAIClient(builder.Configuration);
builder.Services.AddAIAgent("clerk", (sp, key) =>
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
            },
            AIContextProviders = [sp.GetRequiredService<AGUIAIContextProvider>()]
        },
        services: sp);
});

var app = builder.Build();

app.UseEntraAuth();
app.UseAGUIRequestMiddleware();

var agent = app.Services.GetRequiredKeyedService<AIAgent>("clerk");

app.MapAGUI("/", agent).RequireAuthorization(AuthorizationPolicies.AgentRequiresAuthentication);
app.MapGet("/api/health", () => "OK");
app.MapGraphUserProfileEndpoint("/api/me");

await app.RunAsync();
