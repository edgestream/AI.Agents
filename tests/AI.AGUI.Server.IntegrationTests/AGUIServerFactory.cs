using AI.AGUI.Hosting;
using AI.MCP.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.AGUI.Server.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces
/// <see cref="IChatClient"/> with <see cref="FakeChatClient"/> and injects
/// dummy Azure OpenAI configuration so the server can start without real
/// Azure credentials. Also removes <see cref="HostingService"/> so
/// no real MCP connections are attempted during tests.
/// </summary>
internal sealed class AGUIServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Foundry:ProjectEndpoint", "");
        builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
        builder.UseSetting("AzureOpenAI:DeploymentName", "fake-deployment");

        builder.ConfigureServices(services =>
        {
            // Replace the real IChatClient with the fake.
            var chatClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IChatClient));
            if (chatClientDescriptor is not null)
                services.Remove(chatClientDescriptor);
            services.AddSingleton<IChatClient>(new FakeChatClient());

            // Remove the MCP hosted service so no connections are attempted in tests.
            var mcpDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(HostingService));
            if (mcpDescriptor is not null) services.Remove(mcpDescriptor);
        });
    }
}
