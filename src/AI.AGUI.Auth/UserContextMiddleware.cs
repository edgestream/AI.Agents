using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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
        var userContext = ExtractUserContext(context);
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

    private IUserContext ExtractUserContext(HttpContext context)
    {
        // Try Easy Auth headers first
        var principalId = context.Request.Headers[PrincipalIdHeader].FirstOrDefault();
        var principalName = context.Request.Headers[PrincipalNameHeader].FirstOrDefault();
        var accessToken = context.Request.Headers[AccessTokenHeader].FirstOrDefault();
        var idToken = context.Request.Headers[IdTokenHeader].FirstOrDefault();

        // If no Easy Auth headers, try Authorization header (frontend-forwarded token)
        if (string.IsNullOrEmpty(principalId) && string.IsNullOrEmpty(accessToken))
        {
            var authHeader = context.Request.Headers[AuthorizationHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                accessToken = authHeader["Bearer ".Length..];
            }
        }

        // Extract user info from ID token if available
        string? displayName = null;
        string? email = null;
        string? picture = null;

        if (!string.IsNullOrEmpty(idToken))
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(idToken))
                {
                    var jwt = tokenHandler.ReadJwtToken(idToken);

                    principalId ??= jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
                    displayName = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                    email = jwt.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "preferred_username")?.Value;
                    picture = jwt.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ID token for user claims extraction.");
            }
        }

        // Also try to extract from access token if ID token didn't provide everything
        if (!string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(principalId))
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(accessToken))
                {
                    var jwt = tokenHandler.ReadJwtToken(accessToken);
                    principalId ??= jwt.Subject ?? jwt.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
                    displayName ??= jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                    email ??= jwt.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "upn")?.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse access token for user claims extraction.");
            }
        }

        // Use principal name as fallback for display name
        displayName ??= principalName;
        email ??= principalName;

        if (string.IsNullOrEmpty(principalId))
        {
            return UserContext.Anonymous;
        }

        return new UserContext(
            userId: principalId,
            displayName: displayName,
            email: email,
            picture: picture,
            accessToken: accessToken);
    }
}
