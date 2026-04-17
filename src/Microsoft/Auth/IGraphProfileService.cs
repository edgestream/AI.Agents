namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Service for retrieving enriched user profile information from Microsoft Graph.
/// </summary>
public interface IGraphProfileService
{
    /// <summary>
    /// Retrieves the current user's profile from Microsoft Graph using a delegated access token.
    /// </summary>
    /// <param name="accessToken">The delegated access token with Graph User.Read scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enriched profile, or null if Graph is unavailable or the token lacks required scope.</returns>
    Task<GraphUserProfile?> GetCurrentUserProfileAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current user's profile photo from Microsoft Graph as a data URL.
    /// </summary>
    /// <param name="accessToken">The delegated access token with Graph User.Read scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A data URL (e.g., data:image/jpeg;base64,...) or null if no photo exists.</returns>
    Task<string?> GetCurrentUserPhotoAsDataUrlAsync(string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// User profile data retrieved from Microsoft Graph.
/// </summary>
public sealed record GraphUserProfile(
    string? DisplayName,
    string? Mail,
    string? UserPrincipalName,
    string? GivenName,
    string? Surname,
    string? Id);