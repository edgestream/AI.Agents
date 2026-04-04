using AI.MCP.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces
/// <see cref="IChatClient"/> with <see cref="FakeChatClient"/> and injects
/// dummy Azure OpenAI configuration so the server can start without real
/// Azure credentials. Also removes <see cref="HostingService"/> so
/// no real MCP connections are attempted during tests.
/// </summary>
internal sealed class AGUIServerFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Gets the <see cref="FakeChatClient"/> injected into the server.
    /// Tests may configure flags on this instance before sending requests.
    /// </summary>
    public FakeChatClient FakeChatClient { get; } = new FakeChatClient();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Force AzureOpenAI provider: clear any Foundry endpoint from appsettings so
        // auto-detection in AddAIClient() selects the Azure OpenAI path.
        builder.UseSetting("Foundry:ProjectEndpoint", "");
        builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
        builder.UseSetting("AzureOpenAI:DeploymentName", "fake-deployment");

        builder.ConfigureServices(services =>
        {
            // Remove the real IChatClient registration and replace with fake.
            var chatClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IChatClient));
            if (chatClientDescriptor is not null)
                services.Remove(chatClientDescriptor);

            services.AddSingleton<IChatClient>(FakeChatClient);

            // Remove the MCP hosted service so no connections are attempted in tests.
            // A fresh McpClientRegistry (registered by Program.cs) starts with an empty
            // clients and tools list, which is the correct test behaviour.
            // ToolDiscoveryService is internal and driven by AI.MCP.Client.HostingService,
            // so removing HostingService is sufficient.
            var mcpDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(HostingService));
            if (mcpDescriptor is not null) services.Remove(mcpDescriptor);
        });
    }
}
