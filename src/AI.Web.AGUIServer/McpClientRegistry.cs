using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace AI.Web.AGUIServer;

/// <summary>
/// Manages the lifecycle of all live <see cref="McpClient"/> instances and the
/// <see cref="AITool"/> instances discovered from them.
/// Disposes clients gracefully on application shutdown.
/// </summary>
public sealed class McpClientRegistry : IAsyncDisposable
{
    private readonly List<McpClient> _clients = [];
    private readonly List<AITool> _tools = [];

    /// <summary>Gets all tools discovered across every registered MCP server.</summary>
    public IReadOnlyList<AITool> Tools => _tools;

    public void Add(McpClient client) => _clients.Add(client);

    /// <summary>Appends <paramref name="tools"/> to the shared tool list.</summary>
    public void AddTools(IEnumerable<AITool> tools) => _tools.AddRange(tools);

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
