using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AI.MCP.Client;

/// <summary>
/// Extension methods for registering the MCP client infrastructure with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP client infrastructure and binds <see cref="McpClientOptions"/>
    /// from the specified configuration section.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="sectionName">The configuration section that contains the MCP server map.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMCPClient(this IServiceCollection services, string sectionName = "McpServers")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddHttpClient();
        services.AddOptions<McpClientOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                options.Servers = configuration.GetSection(sectionName).Get<Dictionary<string, McpServerOptions>>() ?? []);

        services.TryAddSingleton<McpClientRegistry>();
        services.TryAddTransient<ToolDiscoveryService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, HostingService>());

        return services;
    }

    /// <summary>
    /// Registers the MCP client infrastructure and allows callers to configure the options in code.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">The delegate used to configure <see cref="McpClientOptions"/>.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMCPClient(this IServiceCollection services, Action<McpClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddMCPClient();
        services.Configure(configure);

        return services;
    }
}