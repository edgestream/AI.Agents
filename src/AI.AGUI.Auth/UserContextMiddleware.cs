using System.Security.Claims;
using System.Text.Json;
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
    public const string MS_TOKEN_AAD_ACCESS_TOKEN = "X-MS-TOKEN-AAD-ACCESS-TOKEN";

    /// <summary>
    /// Header name for the Entra ID ID token.
    /// </summary>
    public const string MS_TOKEN_AAD_ID_TOKEN = "X-MS-TOKEN-AAD-ID-TOKEN";

    /// <summary>
    /// Header name for the user's principal name.
    /// </summary>
    public const string MS_CLIENT_PRINCIPAL_NAME = "X-MS-CLIENT-PRINCIPAL-NAME";

    /// <summary>
    /// Header name for the user's object ID.
    /// </summary>
    public const string MS_CLIENT_PRINCIPAL_ID = "X-MS-CLIENT-PRINCIPAL-ID";

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
        _logger.LogTrace("Incoming request headers: {Headers}",
            JsonSerializer.Serialize(context.Request.Headers.ToDictionary(
                header => header.Key,
                header => SanitizeHeaderValueForLogging(header.Key, header.Value.ToString()))));

        var principalId = GetNormalizedHeaderValue(context.Request.Headers, MS_CLIENT_PRINCIPAL_ID);
        var principalName = GetRepeatedValueCollapsedHeaderValue(context.Request.Headers, MS_CLIENT_PRINCIPAL_NAME);
        var accessToken = GetNormalizedHeaderValue(context.Request.Headers, MS_TOKEN_AAD_ACCESS_TOKEN);

        if (string.IsNullOrEmpty(principalId))
        {
            _logger.LogDebug("No MS_CLIENT_PRINCIPAL_ID header found. User will be treated as anonymous.");
            return UserContext.Anonymous;
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("No MS_TOKEN_AAD_ACCESS_TOKEN header found. Graph enrichment will be skipped and Easy Auth principal headers will be used.");
            return new UserContext(userId: principalId);
        }

        var graphService = context.RequestServices.GetService<IGraphProfileService>();
        if (graphService is null)
        {
            _logger.LogWarning("IGraphProfileService is not registered. Easy Auth principal headers will be used without Graph enrichment.");
            return new UserContext(userId: principalId, accessToken: accessToken);
        }

        var userProfile = await graphService.GetCurrentUserProfileAsync(accessToken, context.RequestAborted);
        if (userProfile is null)
        {
            _logger.LogWarning("Access token is present but Graph profile could not be retrieved. Easy Auth principal headers will be used.");
            return new UserContext(userId: principalId, accessToken: accessToken);
        }
        else
        {
            _logger.LogTrace("User profile retrieved from graph: {userProfile}", JsonSerializer.Serialize(userProfile));
        }

        var photoDataUrl = await graphService.GetCurrentUserPhotoAsDataUrlAsync(accessToken, context.RequestAborted);
        if (photoDataUrl is null)
        {
            _logger.LogDebug("No photo data URL retrieved for graph profile.");
        }
        else
        {
            _logger.LogTrace("Current user photo retrieved for graph profile.");
        }

        return new UserContext(
            userId: principalId,
            displayName: userProfile.DisplayName,
            email: userProfile.Mail,
            picture: photoDataUrl,
            accessToken: accessToken
        );
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

    private string? GetRepeatedValueCollapsedHeaderValue(IHeaderDictionary headers, string headerName)
    {
        if (!headers.TryGetValue(headerName, out var values))
        {
            return null;
        }

        var rawValue = values.ToString().Trim();
        if (string.IsNullOrEmpty(rawValue) || !rawValue.Contains(','))
        {
            return string.IsNullOrEmpty(rawValue) ? null : rawValue;
        }

        var splitValues = rawValue
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (splitValues.Length > 1 && splitValues.All(value => string.Equals(value, splitValues[0], StringComparison.Ordinal)))
        {
            _logger.LogDebug("Header {HeaderName} contained duplicated values. Normalizing to a single value.", headerName);
            return splitValues[0];
        }

        return rawValue;
    }

    private static string SanitizeHeaderValueForLogging(string headerName, string value)
    {
        if (!IsSensitiveHeader(headerName))
        {
            return value;
        }

        var splitValues = value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(ShortenSensitiveValue);

        return string.Join(", ", splitValues);
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        return headerName.Equals(MS_TOKEN_AAD_ACCESS_TOKEN, StringComparison.OrdinalIgnoreCase)
            || headerName.Equals(MS_TOKEN_AAD_ID_TOKEN, StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("X-MS-TOKEN-AAD-REFRESH-TOKEN", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("X-MS-CLIENT-PRINCIPAL", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase);
    }

    private static string ShortenSensitiveValue(string value)
    {
        var trimmedValue = value.Trim();

        if (trimmedValue.Length <= 5)
        {
            return trimmedValue;
        }

        return $"{trimmedValue[..5]}...";
    }
}
