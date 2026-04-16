using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.AGUI.Auth;

/// <summary>
/// Middleware that extracts user identity from Azure Container Apps Easy Auth headers
/// and populates <see cref="IUserContext"/> for the current request.
/// </summary>
/// <remarks>
/// <para>
/// When deployed to Azure Container Apps with Easy Auth enabled, the ingress
/// forwards identity information via <c>X-MS-TOKEN-AAD-*</c> headers after
/// successful authentication.
/// </para>
/// <para>
/// Supported headers:
/// <list type="bullet">
///   <item><c>X-MS-TOKEN-AAD-ACCESS-TOKEN</c>: The Entra ID access token</item>
///   <item><c>X-MS-TOKEN-AAD-ID-TOKEN</c>: The Entra ID ID token (contains user claims)</item>
///   <item><c>X-MS-CLIENT-PRINCIPAL-NAME</c>: The user's principal name (UPN or email)</item>
///   <item><c>X-MS-CLIENT-PRINCIPAL-ID</c>: The user's unique object ID</item>
/// </list>
/// </para>
/// <para>
/// When <see cref="IGraphProfileService"/> is registered and the access token includes 
/// Microsoft Graph User.Read scope, the middleware enriches the user context with 
/// the user's display name and profile photo from Graph.
/// </para>
/// </remarks>
public sealed class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    /// <summary>
    /// Header name for the Entra ID access token.
    /// </summary>
    public const string AccessTokenHeader = "X-MS-TOKEN-AAD-ACCESS-TOKEN";

    /// <summary>
    /// Header name for the Entra ID ID token.
    /// </summary>
    public const string IdTokenHeader = "X-MS-TOKEN-AAD-ID-TOKEN";

    /// <summary>
    /// Header name for the user's principal name.
    /// </summary>
    public const string PrincipalNameHeader = "X-MS-CLIENT-PRINCIPAL-NAME";

    /// <summary>
    /// Header name for the user's object ID.
    /// </summary>
    public const string PrincipalIdHeader = "X-MS-CLIENT-PRINCIPAL-ID";

    /// <summary>
    /// Header name for Authorization bearer token forwarded from frontend.
    /// </summary>
    public const string AuthorizationHeader = "Authorization";

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userContext = await ExtractUserContextAsync(context);
        context.Items[typeof(IUserContext)] = userContext;

        // Also set the user principal for ASP.NET Core authentication
        if (userContext.IsAuthenticated)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userContext.UserId)
            };

            if (userContext.DisplayName is not null)
                claims.Add(new(ClaimTypes.Name, userContext.DisplayName));

            if (userContext.Email is not null)
                claims.Add(new(ClaimTypes.Email, userContext.Email));

            var identity = new ClaimsIdentity(claims, "EntraEasyAuth");
            context.User = new ClaimsPrincipal(identity);

            _logger.LogDebug("User context established: {UserId}", userContext.UserId);
        }

        await _next(context);
    }

    private async Task<IUserContext> ExtractUserContextAsync(HttpContext context)
    {
        var principalId = context.Request.Headers[PrincipalIdHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(principalId))
        {
            _logger.LogDebug("No principal ID header found. User will be treated as anonymous.");
            return UserContext.Anonymous;
        }

        var accessToken = context.Request.Headers[AccessTokenHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("Principal ID header is present but no access token found. User context will have limited information.");
            return new UserContext(principalId);
        }

        var graphService = context.RequestServices.GetService<IGraphProfileService>();
        if (graphService is null)
        {
            _logger.LogWarning("IGraphProfileService is not registered. User context will have limited information.");
            return new UserContext(principalId);
        }

        var userProfile = await graphService.GetCurrentUserProfileAsync(accessToken, context.RequestAborted);
        if (userProfile is null)
        {
            _logger.LogWarning("Access token is present but Graph profile could not be retrieved. User context will have limited information.");
            return new UserContext(principalId);
        }

        var photoDataUrl = await graphService.GetCurrentUserPhotoAsDataUrlAsync(accessToken, context.RequestAborted);

        return new UserContext(
            userId: principalId,
            displayName: userProfile.DisplayName,
            email: userProfile.Mail,
            picture: photoDataUrl,
            accessToken: accessToken
        );
    }
}
