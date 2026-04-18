using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Default implementation of <see cref="IUserContextAccessor"/> that retrieves
/// the user context from <see cref="HttpContext.Items"/>.
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
            if (httpContext?.Items.TryGetValue(typeof(IUserContext), out var userContext) == true)
            {
                return (IUserContext)userContext!;
            }

            return UnauthenticatedUserContext.Anonymous;
        }
    }
}