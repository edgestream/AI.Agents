using ModelContextProtocol.Client;

namespace AI.Web.AGUIServer;

/// <summary>
/// Manages the lifecycle of all live <see cref="McpClient"/> instances.
/// Disposes them gracefully on application shutdown.
/// </summary>
public sealed class McpClientRegistry : IAsyncDisposable
{
    private readonly List<McpClient> _clients = [];

    public void Add(McpClient client) => _clients.Add(client);

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
        _clients.Clear();
    }
}
