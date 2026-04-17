namespace AI.Agents.OAuth;

/// <summary>
/// Represents an OAuth 2.0 token for an external MCP tool.
/// </summary>
public sealed record OAuthToken
{
    /// <summary>
    /// Gets the access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the refresh token, if available.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Gets the token type (e.g., "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Gets the time at which the access token expires, in UTC.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Gets the granted scopes.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Returns true if the token is expired or about to expire (within 5 minutes).
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow.AddMinutes(5);
}