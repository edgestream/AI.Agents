using AI.Agents.Abstractions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Middleware that extracts user identity from HTTP authentication headers
/// to populate <see cref="IUserContext"/> for the current request.
/// </summary>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="EntraAuthMiddleware"/> class.
/// </para>
/// <para>
/// Supported headers:
/// <list type="bullet">
///   <item><c>X-MS-TOKEN-AAD-ACCESS-TOKEN</c>: The Entra ID access token</item>
///   <item><c>X-MS-CLIENT-PRINCIPAL-ID</c>: The user's unique object ID</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="userProfileService">The user profile service.</param>
public sealed class EntraAuthMiddleware(RequestDelegate next, IUserProfileService userProfileService, ILogger<EntraAuthMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IUserProfileService _userProfileService = userProfileService;
    private readonly ILogger<EntraAuthMiddleware> _logger = logger;

    /// <summary>
    /// Header name for the Entra ID access token.
    /// </summary>
    public const string MS_TOKEN_AAD_ACCESS_TOKEN = "X-MS-TOKEN-AAD-ACCESS-TOKEN";

    /// <summary>
    /// Header name for the user's object ID.
    /// </summary>
    public const string MS_CLIENT_PRINCIPAL_ID = "X-MS-CLIENT-PRINCIPAL-ID";

    /// <summary>
    /// Extracts user context from authentication headers and populates the HTTP context for downstream components.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var userContext = await ExtractUserContextAsync(context);
        context.Items[typeof(IUserContext)] = userContext;

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
        var principalId = GetNormalizedHeaderValue(context.Request.Headers, MS_CLIENT_PRINCIPAL_ID);
        var accessToken = GetNormalizedHeaderValue(context.Request.Headers, MS_TOKEN_AAD_ACCESS_TOKEN);

        if (string.IsNullOrEmpty(principalId))
        {
            _logger.LogTrace("No MS_CLIENT_PRINCIPAL_ID header found. User will be treated as anonymous.");
            return UnauthenticatedUserContext.Anonymous;
        }
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogTrace("No MS_TOKEN_AAD_ACCESS_TOKEN header found. User will be treated as anonymous.");
            return UnauthenticatedUserContext.Anonymous;
        }

        var userProfile = await _userProfileService.GetCurrentUserProfileAsync(accessToken, context.RequestAborted);
        if (userProfile is null)
        {
            _logger.LogWarning("No user profile retrieved but access token is present. User will be treated as anonymous.");
            return UnauthenticatedUserContext.Anonymous;
        }
        return userProfile;
    }

    private string? GetNormalizedHeaderValue(IHeaderDictionary headers, string headerName)
    {
        if (!headers.TryGetValue(headerName, out var values))
        {
            return null;
        }

        var rawValue = values.ToString().Trim();
        if (string.IsNullOrEmpty(rawValue))
        {
            return null;
        }

        if (!rawValue.Contains(','))
        {
            return rawValue;
        }

        var distinctValues = rawValue
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (distinctValues.Length == 0)
        {
            return null;
        }

        if (distinctValues.Length == 1)
        {
            _logger.LogDebug("Header {HeaderName} contained duplicated values. Normalizing to a single value.", headerName);
        }
        else
        {
            _logger.LogWarning("Header {HeaderName} contained multiple distinct values. Using the first value.", headerName);
        }

        return distinctValues[0];
    }
}