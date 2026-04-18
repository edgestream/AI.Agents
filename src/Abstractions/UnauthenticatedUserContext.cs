namespace AI.Agents.Abstractions;

public class UnauthenticatedUserContext : IUserContext
{
    public string UserId => string.Empty;

    public string? DisplayName => null;

    public string? Email => null;

    public string? Picture => null;

    public string? AccessToken => null;

    public bool IsAuthenticated => false;
}
