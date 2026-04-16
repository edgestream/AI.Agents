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

    /// <summary>
    /// Gets or sets the OAuth 2.0 authentication configuration for this MCP server.
    /// </summary>
    /// <remarks>
    /// When configured, the MCP server requires per-user OAuth authorization before
    /// tools can be invoked. Users must complete the OAuth flow to authorize access.
    /// </remarks>
    /// <example>
    /// <code>
    /// "McpServers": {
    ///   "github": {
    ///     "command": "npx",
    ///     "args": ["-y", "@modelcontextprotocol/server-github"],
    ///     "auth": {
    ///       "type": "OAuth",
    ///       "clientId": "...",
    ///       "authorizationUrl": "https://github.com/login/oauth/authorize",
    ///       "tokenUrl": "https://github.com/login/oauth/access_token",
    ///       "scopes": ["repo", "read:user"]
    ///     }
    ///   }
    /// }
    /// </code>
    /// </example>
    public McpOAuthOptions? Auth { get; set; }
}

/// <summary>
/// OAuth 2.0 authentication configuration for an MCP server that requires per-user authorization.
/// </summary>
public sealed class McpOAuthOptions
{
    /// <summary>
    /// Gets or sets the authentication type. Must be "OAuth" for OAuth 2.0 flows.
    /// </summary>
    public string Type { get; set; } = "OAuth";

    /// <summary>
    /// Gets or sets the OAuth client ID registered with the external provider.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client secret registered with the external provider.
    /// Optional for public clients using PKCE.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the authorization endpoint URL for the OAuth provider.
    /// </summary>
    public string? AuthorizationUrl { get; set; }

    /// <summary>
    /// Gets or sets the token endpoint URL for the OAuth provider.
    /// </summary>
    public string? TokenUrl { get; set; }

    /// <summary>
    /// Gets or sets the scopes to request during authorization.
    /// </summary>
    public string[]? Scopes { get; set; }

    /// <summary>
    /// Gets or sets whether to use PKCE (Proof Key for Code Exchange).
    /// Defaults to true for enhanced security.
    /// </summary>
    public bool UsePkce { get; set; } = true;

    /// <summary>
    /// Gets or sets the environment variable name to inject the access token into.
    /// Defaults to the standard name for common MCP servers.
    /// </summary>
    /// <remarks>
    /// For GitHub MCP, this should be "GITHUB_PERSONAL_ACCESS_TOKEN".
    /// For MS Graph MCP, this should be "GRAPH_ACCESS_TOKEN" or similar.
    /// </remarks>
    public string? TokenEnvVar { get; set; }

    /// <summary>
    /// Returns true if this configuration represents a valid OAuth setup.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrEmpty(ClientId) &&
        !string.IsNullOrEmpty(AuthorizationUrl) &&
        !string.IsNullOrEmpty(TokenUrl);
}
