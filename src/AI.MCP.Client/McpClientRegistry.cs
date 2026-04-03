using ModelContextProtocol.Client;

namespace AI.MCP.Client;

/// <summary>
/// Owns the lifecycle of all live <see cref="McpClient"/> instances and the
/// <see cref="McpClientTool"/> instances discovered from them.
/// Disposes clients gracefully on application shutdown.
/// </summary>
public sealed class McpClientRegistry : IAsyncDisposable
{
    private readonly Dictionary<string, McpClient> _clients = [];
    private readonly List<McpClientTool> _tools = [];

    /// <summary>Gets all registered MCP clients keyed by their configuration name.</summary>
    public IReadOnlyDictionary<string, McpClient> Clients => _clients;

    /// <summary>Gets all tools discovered across every registered MCP server.</summary>
    public IReadOnlyList<McpClientTool> Tools => _tools;

    /// <summary>Registers a connected client under its configuration name.</summary>
    public void AddClient(string name, McpClient client) => _clients.Add(name, client);

    /// <summary>
    /// Adds a single tool to the registry, prefixing its name with
    /// <paramref name="serverName"/> to avoid collisions across servers.
    /// </summary>
    public void AddTool(string serverName, McpClientTool tool) =>
        _tools.Add(tool.WithName($"{serverName}__{tool.Name}"));

    /// <summary>
    /// Adds multiple tools to the registry, prefixing each name with
    /// <paramref name="serverName"/>.
    /// </summary>
    public void AddTools(string serverName, IEnumerable<McpClientTool> tools)
    {
        foreach (var tool in tools) AddTool(serverName, tool);
    }

    /// <summary>Disposes all registered MCP clients.</summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
            await client.DisposeAsync();
        _clients.Clear();
        _tools.Clear();
    }
}
