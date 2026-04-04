using AI.MCP.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostApplicationBuilder"/> to configure AI client support.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the MCP client infrastructure:
    /// <see cref="McpClientRegistry"/>, <see cref="HostingService"/>,
    /// and <see cref="ToolDiscoveryService"/>.
    /// </summary>
    public static IHostApplicationBuilder AddMCPClient(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddOptions<McpClientOptions>().Configure<IConfiguration>((options, config) => options.Servers = config.GetSection("McpServers").Get<Dictionary<string, McpServerOptions>>() ?? []);
        builder.Services.AddSingleton<McpClientRegistry>();
        builder.Services.AddTransient<ToolDiscoveryService>();
        builder.Services.AddHostedService<HostingService>();
        return builder;
    }
}