using AI.Web.AGUIServer;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

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

builder.Services.AddSingleton<McpClientRegistry>();
builder.Services.AddSingleton<IList<AITool>>(sp => {
    var mcpServers = builder.Configuration.GetSection("McpServers").Get<McpServerOptions[]>() ?? [];
    var mcpRegistry = sp.GetRequiredService<McpClientRegistry>();
    List<AITool> tools = [];
    foreach (var server in mcpServers)
    {
        IClientTransport transport = server.Transport.ToLowerInvariant() switch
        {
            "stdio" => new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = server.Name,
                Command = server.Command ?? throw new InvalidOperationException($"MCP server '{server.Name}' uses stdio transport but has no Command."),
                Arguments = server.Arguments ?? [],
            }),
            "http" => new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = server.Name,
                Endpoint = new Uri(server.Url ?? throw new InvalidOperationException($"MCP server '{server.Name}' uses http transport but has no Url.")),
            }),
            _ => throw new InvalidOperationException($"Unsupported MCP transport '{server.Transport}' for server '{server.Name}'.")
        };
        var mcpClient = McpClient.CreateAsync(transport).GetAwaiter().GetResult();
        mcpRegistry.Add(mcpClient);
        var mcpTools = mcpClient.ListToolsAsync().GetAwaiter().GetResult();
        tools.AddRange(mcpTools.Select(t => (AITool)t));
    }
    return tools;
});

builder.Services.AddSingleton<AIAgent>(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var tools = sp.GetRequiredService<IList<AITool>>();
    return chatClient.AsAIAgent(
        name: "AGUIAssistant",
        instructions: "You are a helpful assistant.",
        tools: tools);
});

WebApplication app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() => app.Services.GetRequiredService<McpClientRegistry>().DisposeAsync().AsTask().GetAwaiter().GetResult());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredService<AIAgent>());

await app.RunAsync();

public partial class Program { }
