namespace AI.MCP.Client;

/// <summary>
/// Top-level options for the MCP client infrastructure.
/// Bound from the <c>McpServers</c> configuration section via
/// <see cref="ServiceCollectionExtensions.AddMCPClient"/>.
/// </summary>
public sealed class McpClientOptions
{
    /// <summary>
    /// Gets or sets the MCP server entries keyed by their configuration name.
    /// Matches the <c>McpServers</c> flat-dictionary format used by
    /// VS Code, GitHub Copilot, and Claude Code.
    /// </summary>
    public Dictionary<string, McpServerOptions> Servers { get; set; } = [];
}
