using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces
/// <see cref="IChatClient"/> with <see cref="FakeChatClient"/> and injects
/// dummy Azure OpenAI configuration so the server can start without real
/// Azure credentials. Also overrides MCP tools with an empty list.
/// </summary>
internal sealed class AGUIServerFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide dummy AzureOpenAI config so the startup validation passes.
        builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
        builder.UseSetting("AzureOpenAI:DeploymentName", "fake-deployment");

        // Clear MCP servers so no live connections are attempted in tests.
        builder.UseSetting("McpServers", "");

        builder.ConfigureServices(services =>
        {
            // Remove the real IChatClient registration and replace with fake.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IChatClient));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IChatClient>(new FakeChatClient());

            // Override MCP tools with empty list (no live MCP connections in tests).
            ReplaceService<IList<AITool>>(services, Array.Empty<AITool>());
        });
    }

    private static void ReplaceService<T>(IServiceCollection services, T implementation) where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }
        services.AddSingleton(implementation);
    }
}
