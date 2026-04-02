using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer;

internal static class McpClientHostingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP client hosting infrastructure:
    /// <see cref="McpClientRegistry"/>, <see cref="McpHostingService"/>,
    /// and <see cref="McpToolsContextProvider"/>.
    /// </summary>
    public static IServiceCollection AddMcpClientHosting(this IServiceCollection services)
    {
        services.AddSingleton<McpClientRegistry>();
        services.AddHostedService<McpHostingService>();
        services.AddSingleton<McpToolsContextProvider>();
        return services;
    }
}
