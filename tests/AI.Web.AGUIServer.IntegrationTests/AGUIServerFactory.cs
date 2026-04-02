using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces
/// <see cref="IChatClient"/> with <see cref="FakeChatClient"/> and injects
/// dummy Azure OpenAI configuration so the server can start without real
/// Azure credentials. Also removes <see cref="McpClientHostingService"/> so no
/// real MCP connections are attempted during tests.
/// </summary>
internal sealed class AGUIServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide dummy AzureOpenAI config so the startup validation passes.
        builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
        builder.UseSetting("AzureOpenAI:DeploymentName", "fake-deployment");

        builder.ConfigureServices(services =>
        {
            // Remove the real IChatClient registration and replace with fake.
            var chatClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IChatClient));
            if (chatClientDescriptor is not null)
                services.Remove(chatClientDescriptor);

            services.AddSingleton<IChatClient>(new FakeChatClient());

            // Remove McpHostingService so no MCP connections are attempted in tests.
            // UseSetting("McpServers", "") cannot reliably clear nested configuration
            // keys populated from appsettings.json, so explicitly removing the hosted
            // service is safer. A fresh McpClientRegistry (registered by Program.cs)
            // starts with an empty tools list, which is the correct test behaviour.
            var mcpHostedDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(McpClientHostingService));
            if (mcpHostedDescriptor is not null)
                services.Remove(mcpHostedDescriptor);
        });
    }
}
