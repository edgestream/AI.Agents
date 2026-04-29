using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AI.Agents.Microsoft.Authentication;

/// <summary>
/// Default implementation of <see cref="IUserContextAccessor"/> that retrieves
/// the user context established by the Entra authentication handler.
/// </summary>
public sealed class HttpUserContextAccessor : IUserContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IUserContext UserContext
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(EntraAuthenticationDefaults.UserContextItemKey, out var userContext) == true)
            {
                return (IUserContext)userContext!;
            }
            return UnauthenticatedUserContext.Anonymous;
        }
    }
}
