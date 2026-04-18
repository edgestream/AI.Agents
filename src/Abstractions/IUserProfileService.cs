namespace AI.Agents.Abstractions;

/// <summary>
/// Service for retrieving user profile information.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Retrieves the current user's profile using an access token.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User context or null if it is unavailable or the token lacks required scope.</returns>
    Task<IUserContext?> GetCurrentUserProfileAsync(string accessToken, CancellationToken cancellationToken = default);
}