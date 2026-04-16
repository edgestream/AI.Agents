namespace AI.AGUI.Auth;

/// <summary>
/// Represents the authenticated user context for the current request.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique user identifier (sub claim from the identity token).
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the user's display name.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the user's profile picture URL.
    /// </summary>
    string? Picture { get; }

    /// <summary>
    /// Gets the raw access token from the identity provider (e.g., Entra ID).
    /// This is the token forwarded from the frontend, not OAuth tokens for external MCP tools.
    /// </summary>
    string? AccessToken { get; }

    /// <summary>
    /// Gets whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
