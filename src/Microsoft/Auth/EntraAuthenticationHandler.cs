using System.Security.Claims;
using System.Text.Encodings.Web;
using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Authentication handler that maps App Service Easy Auth headers to an ASP.NET Core user principal.
/// </summary>
public sealed class EntraAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IUserProfileService _userProfileService;

    public EntraAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserProfileService userProfileService)
        : base(options, logger, encoder)
    {
        _userProfileService = userProfileService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userContext = await ExtractUserContextAsync();
        Context.Items[EntraAuthenticationDefaults.UserContextItemKey] = userContext;

        if (!userContext.IsAuthenticated)
        {
            return AuthenticateResult.NoResult();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userContext.UserId),
            new(EntraAuthenticationDefaults.UserIdClaimType, userContext.UserId)
        };

        if (!string.IsNullOrWhiteSpace(userContext.DisplayName))
        {
            claims.Add(new Claim(ClaimTypes.Name, userContext.DisplayName));
        }

        if (!string.IsNullOrWhiteSpace(userContext.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, userContext.Email));
        }

        if (!string.IsNullOrWhiteSpace(userContext.AccessToken))
        {
            claims.Add(new Claim(EntraAuthenticationDefaults.AccessTokenClaimType, userContext.AccessToken));
        }

        if (!string.IsNullOrWhiteSpace(userContext.Picture))
        {
            claims.Add(new Claim(EntraAuthenticationDefaults.PictureClaimType, userContext.Picture));
        }

        var identity = new ClaimsIdentity(claims, EntraAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, EntraAuthenticationDefaults.AuthenticationScheme);

        Logger.LogDebug("User context established: {UserId}", userContext.UserId);
        return AuthenticateResult.Success(ticket);
    }

    private async Task<IUserContext> ExtractUserContextAsync()
    {
        var principalId = GetNormalizedHeaderValue(Request.Headers, EntraAuthenticationDefaults.PrincipalIdHeader);
        var accessToken = GetNormalizedHeaderValue(Request.Headers, EntraAuthenticationDefaults.AccessTokenHeader);

        if (string.IsNullOrEmpty(principalId))
        {
            Logger.LogTrace("No principal ID header found. User will be treated as anonymous.");
            return UnauthenticatedUserContext.Anonymous;
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            Logger.LogTrace("No access token header found. User will be treated as anonymous.");
            return UnauthenticatedUserContext.Anonymous;
        }

        var userProfile = await _userProfileService.GetCurrentUserProfileAsync(accessToken, Context.RequestAborted);
        if (userProfile is null)
        {
            Logger.LogWarning("No user profile retrieved but access token is present. User will be treated as anonymous.");
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
            Logger.LogDebug("Header {HeaderName} contained duplicated values. Normalizing to a single value.", headerName);
        }
        else
        {
            Logger.LogWarning("Header {HeaderName} contained multiple distinct values. Using the first value.", headerName);
        }

        return distinctValues[0];
    }
}
