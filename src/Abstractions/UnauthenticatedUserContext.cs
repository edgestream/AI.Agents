namespace AI.Agents.Abstractions;

/// <summary>
/// Shared sentinel user context used when no authenticated user is available.
/// </summary>
public sealed class UnauthenticatedUserContext : IUserContext
{
    private UnauthenticatedUserContext()
    {
    }

    /// <summary>
    /// Gets the shared unauthenticated user context instance.
    /// </summary>
    public static UnauthenticatedUserContext Anonymous { get; } = new();

    public string UserId => string.Empty;

    public string? DisplayName => null;

    public string? Email => null;

    public string? Picture => null;

    public string? AccessToken => null;

    public bool IsAuthenticated => false;
}
