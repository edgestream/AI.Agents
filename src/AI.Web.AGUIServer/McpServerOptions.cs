namespace AI.Web.AGUIServer;

/// <summary>
/// Strongly-typed configuration for a single MCP server entry.
/// Matches the Claude Code <c>.mcp.json</c> dictionary format where the key
/// is the server name used as a tool-name prefix.
/// </summary>
public sealed class McpServerOptions
{
    public required string Type { get; set; }
    public string? Command { get; set; }
    public string[]? Args { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, string?>? Env { get; set; }
}
