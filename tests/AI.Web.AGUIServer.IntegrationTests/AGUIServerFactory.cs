using AI.AGUI.Abstractions;
using AI.MCP.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces
/// <see cref="IChatClient"/> with <see cref="FakeChatClient"/> and injects
/// dummy Azure OpenAI configuration so the server can start without real
/// Azure credentials. Also removes <see cref="HostingService"/> so
/// no real MCP connections are attempted during tests.
/// When an <see cref="IAgentModule"/> is supplied it is registered as a
/// singleton and its services take precedence over the config-driven loader.
/// </summary>
internal sealed class AGUIServerFactory : WebApplicationFactory<Program>
{
    private readonly IAgentModule? _module;

    public AGUIServerFactory(IAgentModule? module = null)
    {
        _module = module;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Always provide dummy AzureOpenAI config so Program.cs can boot.
        // When a module is supplied its services replace the production registrations
        // during ConfigureServices below.
        builder.UseSetting("Foundry:ProjectEndpoint", "");
        builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
        builder.UseSetting("AzureOpenAI:DeploymentName", "fake-deployment");

        builder.ConfigureServices(services =>
        {
            if (_module is not null)
            {
                services.AddSingleton<IAgentModule>(_module);

                // Remove production IChatClient — the module provides its own.
                var chatClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IChatClient));
                if (chatClientDescriptor is not null)
                    services.Remove(chatClientDescriptor);

                // Remove the production keyed AIAgent — the module registers its own.
                var agentDescriptors = services
                    .Where(d => d.IsKeyedService
                        && d.ServiceType == typeof(Microsoft.Agents.AI.AIAgent))
                    .ToList();
                foreach (var d in agentDescriptors) services.Remove(d);

                // Apply module registrations using a temporary HostApplicationBuilder
                // so that IAgentModule.Register receives the IHostApplicationBuilder it expects.
                var tempBuilder = new HostApplicationBuilder(
                    new HostApplicationBuilderSettings { DisableDefaults = true });
                int baseline = tempBuilder.Services.Count;
                _module.Register(tempBuilder);

                foreach (var descriptor in tempBuilder.Services.Skip(baseline))
                    services.Add(descriptor);
            }
            else
            {
                // Remove the real IChatClient registration and replace with fake.
                var chatClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IChatClient));
                if (chatClientDescriptor is not null)
                    services.Remove(chatClientDescriptor);

                services.AddSingleton<IChatClient>(new FakeChatClient());
            }

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
