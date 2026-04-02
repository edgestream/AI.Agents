using ModelContextProtocol.Client;

namespace AI.Web.AGUIServer;

/// <summary>
/// Hosted service that manages the full lifecycle of MCP client connections.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StartAsync"/> runs during application startup. It connects to every
/// MCP server listed in configuration, fetches their tool manifests, and registers
/// the resulting <see cref="McpClientTool"/> instances via
/// <see cref="McpClientRegistry.AddTools"/>. This ensures the registry is typically
/// populated with tools early in the application's lifetime, but callers should not
/// assume that tool registration has fully completed by the time the first request
/// is processed.
/// </para>
/// <para>
/// MCP client connections are owned by <see cref="McpClientRegistry"/>, which is a
/// singleton <see cref="IAsyncDisposable"/> disposed by the DI container on shutdown.
/// </para>
/// </remarks>
public sealed class McpClientHostingService(
    McpClientRegistry registry,
    IConfiguration configuration,
    ILogger<McpClientHostingService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var mcpServers = configuration
            .GetSection("McpServers")
            .Get<Dictionary<string, McpServerOptions>>() ?? [];

        foreach (var (name, server) in mcpServers)
        {
            var transport = CreateTransport(name, server);
            var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
            registry.Add(client);

            var serverTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            registry.AddTools(name, serverTools);

            logger.LogInformation(
                "Connected to MCP server '{Name}' ({Type}), loaded {Count} tool(s).",
                name, server.Type, serverTools.Count);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IClientTransport CreateTransport(string name, McpServerOptions server)
    {
        if (string.IsNullOrEmpty(server.Type))
            throw new InvalidOperationException(
                $"MCP server '{name}' is missing required 'type' configuration.");

        return server.Type.ToLowerInvariant() switch
        {
            "stdio" => new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = name,
                Command = server.Command ?? throw new InvalidOperationException(
                    $"MCP server '{name}' uses stdio type but has no command."),
                Arguments = server.Args ?? [],
                EnvironmentVariables = server.Env,
            }),
            "http" or "sse" => new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = name,
                Endpoint = new Uri(server.Url ?? throw new InvalidOperationException(
                    $"MCP server '{name}' uses http type but has no url.")),
            }),
            _ => throw new InvalidOperationException(
                $"Unsupported MCP type '{server.Type}' for server '{name}'.")
        };
    }
}
