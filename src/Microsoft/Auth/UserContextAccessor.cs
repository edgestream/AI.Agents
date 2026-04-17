using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Provides access to <see cref="IUserContext"/> from the current HTTP context.
/// </summary>
public interface IUserContextAccessor
{
    /// <summary>
    /// Gets the current user context, or <see cref="UserContext.Anonymous"/> if not available.
    /// </summary>
    IUserContext UserContext { get; }
}

/// <summary>
/// Default implementation of <see cref="IUserContextAccessor"/> that retrieves
/// the user context from <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class UserContextAccessor : IUserContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IUserContext UserContext
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(typeof(IUserContext), out var userContext) == true)
            {
                return (IUserContext)userContext!;
            }

            return global::AI.Agents.Microsoft.Auth.UserContext.Anonymous;
        }
    }
}