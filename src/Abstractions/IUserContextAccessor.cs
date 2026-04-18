namespace AI.Agents.Abstractions;

/// <summary>
/// Provides access to <see cref="IUserContext"/> from the current HTTP context.
/// </summary>
public interface IUserContextAccessor
{
    /// <summary>
    /// Gets the current user context, or <see cref="UnauthenticatedUserContext.Anonymous"/> if not available.
    /// </summary>
    IUserContext UserContext { get; }
}
