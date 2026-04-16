namespace AI.AGUI.Auth;

/// <summary>
/// Provides storage and retrieval of OAuth tokens for external MCP tools on a per-user basis.
/// </summary>
public interface IOAuthTokenStore
{
    /// <summary>
    /// Retrieves the OAuth token for the specified user and MCP server.
    /// </summary>
    /// <param name="userId">The unique user identifier.</param>
    /// <param name="mcpServerName">The name of the MCP server (configuration key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored token, or null if no token exists.</returns>
    ValueTask<OAuthToken?> GetTokenAsync(string userId, string mcpServerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an OAuth token for the specified user and MCP server.
    /// </summary>
    /// <param name="userId">The unique user identifier.</param>
    /// <param name="mcpServerName">The name of the MCP server (configuration key).</param>
    /// <param name="token">The token to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SetTokenAsync(string userId, string mcpServerName, OAuthToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the OAuth token for the specified user and MCP server.
    /// </summary>
    /// <param name="userId">The unique user identifier.</param>
    /// <param name="mcpServerName">The name of the MCP server (configuration key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RemoveTokenAsync(string userId, string mcpServerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a valid (non-expired) token exists for the specified user and MCP server.
    /// </summary>
    /// <param name="userId">The unique user identifier.</param>
    /// <param name="mcpServerName">The name of the MCP server (configuration key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<bool> HasValidTokenAsync(string userId, string mcpServerName, CancellationToken cancellationToken = default);
}
