namespace AI.AGUI.Auth;

/// <summary>
/// Response returned when an MCP tool requires OAuth authorization that the user has not yet granted.
/// </summary>
public sealed record OAuthConsentRequired
{
    /// <summary>
    /// Gets the type identifier for this response.
    /// </summary>
    public string Type { get; init; } = "oauth_consent_required";

    /// <summary>
    /// Gets the name of the MCP server requiring authorization.
    /// </summary>
    public required string McpServerName { get; init; }

    /// <summary>
    /// Gets the human-readable display name for the MCP server.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the scopes being requested.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Gets the URL to initiate the OAuth authorization flow.
    /// </summary>
    public required string AuthorizeUrl { get; init; }

    /// <summary>
    /// Gets a user-friendly message describing what authorization is needed.
    /// </summary>
    public required string Message { get; init; }
}
