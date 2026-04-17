using AI.Agents.Abstractions;

namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Default implementation of <see cref="IUserContext"/> for the current HTTP request.
/// </summary>
public sealed class UserContext : IUserContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="UserContext"/> for an authenticated user.
    /// </summary>
    public UserContext(
        string userId,
        string? displayName = null,
        string? email = null,
        string? picture = null,
        string? accessToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        DisplayName = displayName;
        Email = email;
        Picture = picture;
        AccessToken = accessToken;
        IsAuthenticated = true;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UserContext"/> for an anonymous user.
    /// </summary>
    private UserContext()
    {
        UserId = string.Empty;
        IsAuthenticated = false;
    }

    /// <inheritdoc />
    public string UserId { get; }

    /// <inheritdoc />
    public string? DisplayName { get; }

    /// <inheritdoc />
    public string? Email { get; }

    /// <inheritdoc />
    public string? Picture { get; }

    /// <inheritdoc />
    public string? AccessToken { get; }

    /// <inheritdoc />
    public bool IsAuthenticated { get; }

    /// <summary>
    /// Returns a singleton representing an anonymous (unauthenticated) user context.
    /// </summary>
    public static IUserContext Anonymous { get; } = new UserContext();
}