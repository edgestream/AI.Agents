using System.Collections.Concurrent;
using ModelContextProtocol.Client;

namespace AI.MCP.Client;

/// <summary>
/// Owns the lifecycle of all live <see cref="McpClient"/> instances and the
/// <see cref="McpClientTool"/> instances discovered from them.
/// Disposes clients gracefully on application shutdown.
/// </summary>
/// <remarks>
/// All mutations and reads are thread-safe: <see cref="_clients"/> uses a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> and <see cref="_tools"/> is
/// guarded by <see cref="_toolsLock"/>. <see cref="Tools"/> returns a snapshot
/// so callers enumerate a stable copy.
/// </remarks>
public sealed class McpClientRegistry : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, McpClient> _clients = new();
    private readonly Lock _toolsLock = new();
    private List<McpClientTool> _tools = [];

    /// <summary>Gets all registered MCP clients keyed by their configuration name.</summary>
    public IReadOnlyDictionary<string, McpClient> Clients => _clients;

    /// <summary>
    /// Gets a point-in-time snapshot of all tools discovered across every registered MCP server.
    /// The snapshot is safe to enumerate concurrently with ongoing discovery.
    /// </summary>
    public IReadOnlyList<McpClientTool> Tools
    {
        get { lock (_toolsLock) return [.._tools]; }
    }

    /// <summary>Registers a connected client under its configuration name.</summary>
    public void AddClient(string name, McpClient client) => _clients[name] = client;

    /// <summary>
    /// Adds a single tool to the registry, prefixing its name with
    /// <paramref name="serverName"/> to avoid collisions across servers.
    /// </summary>
    public void AddTool(string serverName, McpClientTool tool)
    {
        lock (_toolsLock) _tools.Add(tool.WithName($"{serverName}__{tool.Name}"));
    }

    /// <summary>
    /// Adds multiple tools to the registry atomically, prefixing each name with
    /// <paramref name="serverName"/>.
    /// </summary>
    public void AddTools(string serverName, IEnumerable<McpClientTool> tools)
    {
        var renamed = tools.Select(t => t.WithName($"{serverName}__{t.Name}")).ToList();
        lock (_toolsLock) _tools.AddRange(renamed);
    }

    /// <summary>Disposes all registered MCP clients.</summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
            await client.DisposeAsync();
        _clients.Clear();
        lock (_toolsLock) _tools.Clear();
    }
}
