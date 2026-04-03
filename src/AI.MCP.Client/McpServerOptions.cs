namespace AI.MCP.Client;

/// <summary>
/// Strongly-typed configuration for a single MCP server entry.
/// Matches the Claude Code <c>.mcp.json</c> dictionary format where the
/// dictionary key becomes the server name used as a tool-name prefix.
/// </summary>
/// <remarks>
/// <para>
/// Configuration is read from the <c>McpServers</c> section of <c>appsettings.json</c>
/// (or any other registered <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
/// source) as a dictionary of <c>string → McpServerOptions</c>:
/// </para>
/// <code>
/// "McpServers": {
///   "filesystem": {
///     "type": "stdio",
///     "command": "npx",
///     "args": ["-y", "@modelcontextprotocol/server-filesystem", "/tmp"],
///     "env": { "DEBUG": "1" }
///   },
///   "my-api": {
///     "type": "http",
///     "url": "https://mcp.example.com/mcp"
///   }
/// }
/// </code>
/// </remarks>
public sealed class McpServerOptions
{
    /// <summary>
    /// Gets or sets the transport type used to connect to the MCP server.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///   <listheader><term>Value</term><description>Behaviour</description></listheader>
    ///   <item>
    ///     <term><c>"stdio"</c></term>
    ///     <description>
    ///       Spawns a local child process and communicates over its stdin/stdout.
    ///       Requires <see cref="Command"/>. Backed by
    ///       <see cref="ModelContextProtocol.Client.StdioClientTransport"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><c>"http"</c></term>
    ///     <description>
    ///       Connects to a remote server using Streamable HTTP, automatically
    ///       falling back to SSE if the server does not support Streamable HTTP.
    ///       Requires <see cref="Url"/>. Backed by
    ///       <see cref="ModelContextProtocol.Client.HttpClientTransport"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><c>"sse"</c></term>
    ///     <description>
    ///       Deprecated alias for <c>"http"</c>. Use <c>"http"</c> for new servers.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the executable to launch when <see cref="Type"/> is <c>"stdio"</c>.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="ModelContextProtocol.Client.StdioClientTransportOptions.Command"/>.
    /// Must be non-empty. Ignored for HTTP/SSE transports.
    /// </remarks>
    public string? Command { get; set; }

    /// <summary>
    /// Gets or sets the command-line arguments passed to the process when
    /// <see cref="Type"/> is <c>"stdio"</c>.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="ModelContextProtocol.Client.StdioClientTransportOptions.Arguments"/>.
    /// Ignored for HTTP/SSE transports.
    /// </remarks>
    public string[]? Args { get; set; }

    /// <summary>
    /// Gets or sets the absolute HTTP or HTTPS endpoint URL when
    /// <see cref="Type"/> is <c>"http"</c> or <c>"sse"</c>.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="ModelContextProtocol.Client.HttpClientTransportOptions.Endpoint"/>.
    /// Must be an absolute URI with an <c>http</c> or <c>https</c> scheme.
    /// Ignored for stdio transports.
    /// </remarks>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets environment variables to inject into the child process when
    /// <see cref="Type"/> is <c>"stdio"</c>.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="ModelContextProtocol.Client.StdioClientTransportOptions.EnvironmentVariables"/>.
    /// The child process inherits the current process environment first; entries in
    /// this dictionary are then merged on top. A <see langword="null"/> value removes
    /// the corresponding variable from the inherited environment.
    /// Ignored for HTTP/SSE transports.
    /// </remarks>
    public Dictionary<string, string?>? Env { get; set; }
}
