using AI.MCP.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that exercises the Foundry
/// provider path. Activates auto-detection by setting <c>Foundry:ProjectEndpoint</c>,
/// replaces <see cref="IChatClient"/> with <see cref="FakeChatClient"/> so no real
/// Azure calls are made, and suppresses the MCP hosted service.
/// </summary>
internal sealed class FoundryAGUIServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Activate the Foundry provider via auto-detection.
        builder.UseSetting("Foundry:ProjectEndpoint", "https://fake.foundry.endpoint/");
        builder.UseSetting("Foundry:Model", "fake-model");

        builder.ConfigureServices(services =>
        {
            // Remove the real IChatClient registration (backed by Foundry) and replace with fake.
            var chatClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IChatClient));
            if (chatClientDescriptor is not null)
                services.Remove(chatClientDescriptor);

            services.AddSingleton<IChatClient>(new FakeChatClient());

            // Suppress the MCP hosted service — no real connections needed.
            var mcpDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(HostingService));
            if (mcpDescriptor is not null) services.Remove(mcpDescriptor);
        });
    }
}
