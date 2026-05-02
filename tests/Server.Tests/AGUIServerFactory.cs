using AI.Agents.Abstractions;
using AI.Agents.AGUI;
using AI.Agents.MCP;
using AI.Agents.Server.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Agents.Server.Tests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that injects
/// dummy Foundry configuration and replaces the production keyed agent with a
/// <see cref="ChatClientAgent"/> backed by <see cref="FakeChatClient"/> so
/// tests can run without cloud credentials. Also removes <see cref="HostingService"/>
/// so no real MCP connections are attempted during tests.
/// </summary>
internal sealed class AGUIServerFactory : WebApplicationFactory<Program>
{
    private IUserProfileService? _graphService;
    private bool? _requireAuthenticationForAgent;

    /// <summary>
    /// Sets a custom Graph profile service to be used during tests.
    /// Call this before <see cref="WebApplicationFactory{TEntryPoint}.CreateClient"/>.
    /// </summary>
    public AGUIServerFactory WithUserProfileService(IUserProfileService graphService)
    {
        _graphService = graphService;
        return this;
    }

    /// <summary>
    /// Overrides the <c>Auth:AgentRequiresAuthentication</c> setting for tests.
    /// </summary>
    public AGUIServerFactory WithRequireAuthenticationForAgent(bool requireAuthentication)
    {
        _requireAuthenticationForAgent = requireAuthentication;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Foundry:Endpoint", "https://fake.foundry.endpoint/");

        // Default to false in tests so existing AGUI endpoint tests continue to work
        // unless explicitly overridden via WithRequireAuthenticationForAgent.
        builder.UseSetting(
            "Auth:AgentRequiresAuthentication",
            (_requireAuthenticationForAgent ?? false).ToString().ToLowerInvariant());

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<AGUIAIContextProvider>();

            var agentDescriptors = services
                .Where(d => d.ServiceType == typeof(AIAgent))
                .ToArray();
            foreach (var descriptor in agentDescriptors)
            {
                services.Remove(descriptor);
            }

            var chatClientDescriptors = services
                .Where(d => d.ServiceType == typeof(IChatClient))
                .ToArray();
            foreach (var descriptor in chatClientDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IChatClient, FakeChatClient>();
            services.AddHttpClient("fetch");
            services.AddKeyedSingleton<AIAgent>("default", (sp, key) =>
            {
                var chatClient = sp.GetRequiredService<IChatClient>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                return new ChatClientAgent(
                    chatClient,
                    new ChatClientAgentOptions
                    {
                        Name = key?.ToString() ?? "agui-agent",
                        Description = "Test Agent",
                        ChatOptions = new()
                        {
                            Instructions = "You are a test assistant.",
                            Tools =
                            [
                                UserProfileFunctionFactory.CreateAIFunction(sp),
                                FetchAIFunctionFactory.CreateAIFunction(sp)
                            ]
                        },
                        AIContextProviders = [sp.GetRequiredService<AGUIAIContextProvider>()]
                    },
                    loggerFactory,
                    services: sp);
            });

            services.AddKeyedSingleton<AIAgent>("agui-agent", (sp, key) =>
                sp.GetRequiredKeyedService<AIAgent>("default"));

            // Remove the MCP hosted service so no connections are attempted in tests.
            var mcpDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(HostingService));
            if (mcpDescriptor is not null) services.Remove(mcpDescriptor);

            // If a custom Graph service is provided, replace the production registration.
            if (_graphService is not null)
            {
                var graphDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUserProfileService));
                if (graphDescriptor is not null)
                {
                    services.Remove(graphDescriptor);
                }
                services.AddSingleton(_graphService);
            }
        });
    }
}
