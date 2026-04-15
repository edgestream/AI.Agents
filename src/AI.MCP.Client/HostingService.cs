using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace AI.MCP.Client;

/// <summary>
/// Hosted service that reads MCP server configuration, creates transports,
/// establishes client connections, and registers them into <see cref="McpClientRegistry"/>.
/// For each connected client a <see cref="ToolDiscoveryService"/> instance is resolved
/// from a dedicated DI scope, run concurrently, and disposed with its scope on completion.
/// </summary>
/// <remarks>
/// Client lifecycle is owned by <see cref="McpClientRegistry"/>, which is disposed
/// by the DI container on shutdown.
/// </remarks>
public sealed class HostingService(
    McpClientRegistry registry,
    IServiceProvider serviceProvider,
    IOptions<McpClientOptions> options,
    ILogger<HostingService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var (name, serverOptions) in options.Value.Servers)
        {
            var transport = CreateTransport(name, serverOptions);
            var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
            registry.AddClient(name, client);
            logger.LogInformation("Connected to MCP server '{Name}' ({Type}).", name, serverOptions.Type);
            var scope = serviceProvider.CreateScope();
            var discoveryTask = scope.ServiceProvider
                                     .GetRequiredService<ToolDiscoveryService>()
                                     .DiscoverAsync(name, client, cancellationToken);

            _ = discoveryTask.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        logger.LogError(
                            t.Exception,
                            "Tool discovery failed for MCP server '{Name}'.",
                            name);
                    }
                    else if (t.IsCanceled)
                    {
                        logger.LogDebug(
                            "Tool discovery canceled for MCP server '{Name}'.",
                            name);
                    }
                }
                finally
                {
                    scope.Dispose();
                }
            }, TaskScheduler.Default);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IClientTransport CreateTransport(string name, McpServerOptions serverOptions)
    {
        if (string.IsNullOrEmpty(serverOptions.Type))
            throw new InvalidOperationException(
                $"MCP server '{name}' is missing required 'type' configuration.");

        return serverOptions.Type.ToLowerInvariant() switch
        {
            "stdio" => new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = name,
                Command = serverOptions.Command ?? throw new InvalidOperationException(
                    $"MCP server '{name}' uses stdio type but has no command."),
                Arguments = serverOptions.Args ?? [],
                EnvironmentVariables = serverOptions.Env,
            }),
            "http" or "sse" => new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = name,
                Endpoint = new Uri(serverOptions.Url ?? throw new InvalidOperationException(
                    $"MCP server '{name}' uses http type but has no url.")),
            }),
            _ => throw new InvalidOperationException(
                $"Unsupported MCP type '{serverOptions.Type}' for server '{name}'.")
        };
    }
}
