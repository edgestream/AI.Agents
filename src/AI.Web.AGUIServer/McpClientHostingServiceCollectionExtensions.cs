using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer;

public static class McpClientHostingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP client hosting infrastructure:
    /// <see cref="McpClientRegistry"/>, <see cref="McpClientHostingService"/>,
    /// and <see cref="McpClientToolsAIContextProvider"/>.
    /// </summary>
    public static IServiceCollection AddMcpClientHosting(this IServiceCollection services)
    {
        services.AddSingleton<McpClientRegistry>();
        services.AddHostedService<McpClientHostingService>();
        services.AddSingleton<McpClientToolsAIContextProvider>();
        return services;
    }
}
