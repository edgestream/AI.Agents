namespace AI.Web.AGUIServer;

/// <summary>
/// Strongly-typed configuration for a single MCP server entry in appsettings.json.
/// </summary>
public sealed class McpServerOptions
{
    public required string Name { get; set; }
    public required string Transport { get; set; }
    public string? Command { get; set; }
    public string[]? Arguments { get; set; }
    public string? Url { get; set; }
}
