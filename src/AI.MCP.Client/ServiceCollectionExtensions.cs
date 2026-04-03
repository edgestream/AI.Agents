using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.MCP.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP client infrastructure:
    /// <see cref="McpClientRegistry"/>, <see cref="HostingService"/>,
    /// and <see cref="ToolDiscoveryService"/>.
    /// </summary>
    public static IServiceCollection AddMCPClient(this IServiceCollection services)
    {
        services.AddOptions<McpClientOptions>()
            .Configure<IConfiguration>((options, config) =>
                options.Servers = config
                    .GetSection("McpServers")
                    .Get<Dictionary<string, McpServerOptions>>() ?? []);
        services.AddSingleton<McpClientRegistry>();
        services.AddHostedService<HostingService>();
        return services;
    }
}
