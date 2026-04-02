using AI.Web.AGUIServer;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient().AddLogging();
builder.Services.AddOpenApi();
builder.Services.AddAGUI();

var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"];
if (string.IsNullOrWhiteSpace(endpoint)) throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");
if (string.IsNullOrWhiteSpace(deploymentName)) throw new InvalidOperationException("Azure OpenAI deployment name is not configured.");
builder.Services.AddSingleton<IChatClient>(_ =>
{
    var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];
    AzureOpenAIClient client = string.IsNullOrWhiteSpace(apiKey)
        ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
        : new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
    return client.GetChatClient(deploymentName).AsIChatClient();
});

builder.Services.AddMcpClientHosting();

builder.Services.AddSingleton<AIAgent>(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var mcpProvider = sp.GetRequiredService<McpClientToolsAIContextProvider>();
    return new ChatClientAgent(
        chatClient,
        new ChatClientAgentOptions
        {
            Name = "AGUIAssistant",
            ChatOptions = new ChatOptions { Instructions = "You are a helpful assistant." },
            AIContextProviders = [mcpProvider],
        },
        sp.GetRequiredService<ILoggerFactory>(),
        sp);
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredService<AIAgent>());

await app.RunAsync();

public partial class Program { }