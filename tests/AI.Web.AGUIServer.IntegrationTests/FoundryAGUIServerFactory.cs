using AI.MCP.Client;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> for tests that exercise the
/// Microsoft Foundry provider path. Activates auto-detection by setting
/// <c>Foundry:ProjectEndpoint</c>, prevents real Azure calls by replacing the
/// keyed <see cref="AIAgent"/> with a <see cref="FakeChatClient"/>-backed
/// <see cref="ChatClientAgent"/>, and suppresses the MCP hosted service.
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
            // Replace the keyed AIAgent (registered by AddAIAgent("AGUIAgent", ...))
            // with a fake backed by FakeChatClient so no real Foundry calls are made.
            var agentDescriptor = services.SingleOrDefault(
                d => d.IsKeyedService &&
                     d.ServiceKey is "AGUIAgent" &&
                     d.ServiceType == typeof(AIAgent));
            if (agentDescriptor is not null)
                services.Remove(agentDescriptor);

            services.AddKeyedSingleton<AIAgent>("AGUIAgent", (sp, _) =>
            {
                var options = new ChatClientAgentOptions
                {
                    Name = "AGUIAgent",
                    ChatOptions = new() { Instructions = "You are a helpful assistant." },
                };
                return new ChatClientAgent(
                    new FakeChatClient(),
                    options,
                    sp.GetRequiredService<ILoggerFactory>(),
                    services: sp);
            });

            // Suppress the MCP hosted service — no real connections needed.
            var mcpDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(HostingService));
            if (mcpDescriptor is not null) services.Remove(mcpDescriptor);
        });
    }
}
