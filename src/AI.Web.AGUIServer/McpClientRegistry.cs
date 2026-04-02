using ModelContextProtocol.Client;

namespace AI.Web.AGUIServer;

/// <summary>
/// Manages the lifecycle of all live <see cref="McpClient"/> instances and the
/// <see cref="McpClientTool"/> instances discovered from them.
/// Disposes clients gracefully on application shutdown.
/// </summary>
public sealed class McpClientRegistry : IAsyncDisposable
{
    private readonly List<McpClient> _clients = [];
    private readonly List<McpClientTool> _tools = [];

    /// <summary>Gets all tools discovered across every registered MCP server.</summary>
    public IReadOnlyList<McpClientTool> Tools => _tools;

    public void Add(McpClient client) => _clients.Add(client);

    /// <summary>
    /// Prefixes each tool name with <paramref name="serverName"/> and appends the
    /// renamed tools to the shared list.
    /// </summary>
    public void AddTools(string serverName, IEnumerable<McpClientTool> tools)
        => _tools.AddRange(tools.Select(t => t.WithName($"{serverName}__{t.Name}")));

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
        _clients.Clear();
        _tools.Clear();
    }
}
