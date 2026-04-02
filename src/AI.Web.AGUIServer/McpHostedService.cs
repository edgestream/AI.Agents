using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace AI.Web.AGUIServer;

/// <summary>
/// Hosted service that manages the full lifecycle of MCP client connections.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StartAsync"/> runs before Kestrel begins accepting connections
/// (guaranteed by the ASP.NET Core host lifecycle). It connects to every MCP
/// server listed in configuration, fetches their tool manifests, and adds the
/// resulting <see cref="AITool"/> instances to the shared <paramref name="tools"/>
/// list that was injected into <see cref="Microsoft.Agents.AI.AIAgent"/> at DI
/// registration time. Because <see cref="StartAsync"/> completes before the first
/// request is served, the agent always sees a fully-populated tools list.
/// </para>
/// <para>
/// <see cref="StopAsync"/> properly <c>await</c>s <see cref="McpClientRegistry.DisposeAsync"/>
/// so MCP connections are closed gracefully without blocking the shutdown thread.
/// </para>
/// </remarks>
public sealed class McpHostedService(
    McpClientRegistry registry,
    IList<AITool> tools,
    IConfiguration configuration,
    ILogger<McpHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var mcpServers = configuration
            .GetSection("McpServers")
            .Get<McpServerOptions[]>() ?? [];

        foreach (var server in mcpServers)
        {
            var transport = CreateTransport(server);
            var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
            registry.Add(client);

            var serverTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            foreach (var tool in serverTools)
                tools.Add(tool);

            logger.LogInformation(
                "Connected to MCP server '{Name}' ({Transport}), loaded {Count} tool(s).",
                server.Name, server.Transport, serverTools.Count);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
        => await registry.DisposeAsync();

    private static IClientTransport CreateTransport(McpServerOptions server) =>
        server.Transport.ToLowerInvariant() switch
        {
            "stdio" => new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = server.Name,
                Command = server.Command ?? throw new InvalidOperationException(
                    $"MCP server '{server.Name}' uses stdio transport but has no Command."),
                Arguments = server.Arguments ?? [],
            }),
            "http" => new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = server.Name,
                Endpoint = new Uri(server.Url ?? throw new InvalidOperationException(
                    $"MCP server '{server.Name}' uses http transport but has no Url.")),
            }),
            _ => throw new InvalidOperationException(
                $"Unsupported MCP transport '{server.Transport}' for server '{server.Name}'.")
        };
}
