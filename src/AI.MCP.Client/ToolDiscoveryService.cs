using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace AI.MCP.Client;

/// <summary>
/// Discovers tools from a single MCP client and registers them into <see cref="McpClientRegistry"/>.
/// One instance is spawned per connected client by <see cref="HostingService"/>.
/// </summary>
internal sealed class ToolDiscoveryService(
    McpClientRegistry registry,
    ILogger<ToolDiscoveryService> logger)
{
    public async Task DiscoverAsync(
        string serverName,
        McpClient client,
        CancellationToken cancellationToken = default)
    {
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        registry.AddTools(serverName, tools);

        logger.LogInformation(
            "Discovered {Count} tool(s) from MCP server '{Name}'.", tools.Count, serverName);
    }
}

