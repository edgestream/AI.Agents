using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using AI.MCP.Client;
using Microsoft.Extensions.Logging;

namespace AI.AGUI.Auth;

/// <summary>
/// Service that manages OAuth authorization flows for external MCP tools.
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Generates an authorization URL for the user to visit to authorize an MCP tool.
    /// </summary>
    /// <param name="userId">The user ID initiating authorization.</param>
    /// <param name="mcpServerName">The MCP server name to authorize.</param>
    /// <param name="options">The OAuth configuration for the MCP server.</param>
    /// <param name="callbackUrl">The URL to redirect to after authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorization URL and state for tracking.</returns>
    Task<OAuthAuthorizationResult> GenerateAuthorizationUrlAsync(
        string userId,
        string mcpServerName,
        McpOAuthOptions options,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens.
    /// </summary>
    /// <param name="stateId">The state ID from the callback.</param>
    /// <param name="authorizationCode">The authorization code from the callback.</param>
    /// <param name="options">The OAuth configuration for the MCP server.</param>
    /// <param name="callbackUrl">The callback URL used in the authorization request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token exchange result.</returns>
    Task<OAuthTokenExchangeResult> ExchangeCodeAsync(
        string stateId,
        string authorizationCode,
        McpOAuthOptions options,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using the refresh token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="mcpServerName">The MCP server name.</param>
    /// <param name="options">The OAuth configuration for the MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refreshed token, or null if refresh failed.</returns>
    Task<OAuthToken?> RefreshTokenAsync(
        string userId,
        string mcpServerName,
        McpOAuthOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of generating an OAuth authorization URL.
/// </summary>
public sealed record OAuthAuthorizationResult
{
    /// <summary>
    /// Gets the URL the user should visit to authorize the application.
    /// </summary>
    public required string AuthorizationUrl { get; init; }

    /// <summary>
    /// Gets the state ID for tracking this authorization flow.
    /// </summary>
    public required string StateId { get; init; }
}

/// <summary>
/// Result of exchanging an authorization code for tokens.
/// </summary>
public sealed record OAuthTokenExchangeResult
{
    /// <summary>
    /// Gets whether the exchange was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the acquired token on success.
    /// </summary>
    public OAuthToken? Token { get; init; }

    /// <summary>
    /// Gets the user ID from the original authorization request.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the MCP server name from the original authorization request.
    /// </summary>
    public string? McpServerName { get; init; }

    /// <summary>
    /// Gets the error message on failure.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the redirect URI from the original authorization request.
    /// </summary>
    public string? OriginalRedirectUri { get; init; }
}

/// <summary>
/// Default implementation of <see cref="IOAuthService"/>.
/// </summary>
public sealed class OAuthService : IOAuthService
{
    private readonly IOAuthStateStore _stateStore;
    private readonly IOAuthTokenStore _tokenStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OAuthService> _logger;

    public OAuthService(
        IOAuthStateStore stateStore,
        IOAuthTokenStore tokenStore,
        HttpClient httpClient,
        ILogger<OAuthService> logger)
    {
        _stateStore = stateStore;
        _tokenStore = tokenStore;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OAuthAuthorizationResult> GenerateAuthorizationUrlAsync(
        string userId,
        string mcpServerName,
        McpOAuthOptions options,
        string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        var stateId = OAuthState.GenerateStateId();
        var codeVerifier = options.UsePkce ? OAuthState.GenerateCodeVerifier() : null;

        var state = new OAuthState
        {
            StateId = stateId,
            UserId = userId,
            McpServerName = mcpServerName,
            CodeVerifier = codeVerifier,
            RedirectUri = callbackUrl
        };

        await _stateStore.StoreStateAsync(state, cancellationToken);

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = options.ClientId;
        query["response_type"] = "code";
        query["redirect_uri"] = callbackUrl;
        query["state"] = stateId;

        if (options.Scopes is { Length: > 0 })
        {
            query["scope"] = string.Join(" ", options.Scopes);
        }

        if (options.UsePkce && codeVerifier is not null)
        {
            query["code_challenge"] = OAuthState.ComputeCodeChallenge(codeVerifier);
            query["code_challenge_method"] = "S256";
        }

        var authorizationUrl = $"{options.AuthorizationUrl}?{query}";

        _logger.LogInformation(
            "Generated OAuth authorization URL for MCP server '{McpServerName}' for user '{UserId}'.",
            mcpServerName, userId);

        return new OAuthAuthorizationResult
        {
            AuthorizationUrl = authorizationUrl,
            StateId = stateId
        };
    }

    /// <inheritdoc />
    public async Task<OAuthTokenExchangeResult> ExchangeCodeAsync(
        string stateId,
        string authorizationCode,
        McpOAuthOptions options,
        string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        var state = await _stateStore.ConsumeStateAsync(stateId, cancellationToken);
        if (state is null)
        {
            _logger.LogWarning("OAuth state not found or expired: {StateId}", stateId);
            return new OAuthTokenExchangeResult
            {
                Success = false,
                Error = "Invalid or expired state parameter."
            };
        }

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["redirect_uri"] = callbackUrl,
            ["client_id"] = options.ClientId!
        };

        if (!string.IsNullOrEmpty(options.ClientSecret))
        {
            formData["client_secret"] = options.ClientSecret;
        }

        if (options.UsePkce && !string.IsNullOrEmpty(state.CodeVerifier))
        {
            formData["code_verifier"] = state.CodeVerifier;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "OAuth token exchange failed for MCP server '{McpServerName}': {StatusCode} - {Content}",
                    state.McpServerName, response.StatusCode, content);
                return new OAuthTokenExchangeResult
                {
                    Success = false,
                    Error = $"Token exchange failed: {response.StatusCode}",
                    UserId = state.UserId,
                    McpServerName = state.McpServerName
                };
            }

            var tokenResponse = JsonDocument.Parse(content);
            var token = ParseTokenResponse(tokenResponse.RootElement, options.Scopes);

            await _tokenStore.SetTokenAsync(state.UserId, state.McpServerName, token, cancellationToken);

            _logger.LogInformation(
                "OAuth token acquired for MCP server '{McpServerName}' for user '{UserId}'.",
                state.McpServerName, state.UserId);

            return new OAuthTokenExchangeResult
            {
                Success = true,
                Token = token,
                UserId = state.UserId,
                McpServerName = state.McpServerName,
                OriginalRedirectUri = state.RedirectUri
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "OAuth token exchange failed for MCP server '{McpServerName}'.",
                state.McpServerName);
            return new OAuthTokenExchangeResult
            {
                Success = false,
                Error = "Token exchange failed: " + ex.Message,
                UserId = state.UserId,
                McpServerName = state.McpServerName
            };
        }
    }

    /// <inheritdoc />
    public async Task<OAuthToken?> RefreshTokenAsync(
        string userId,
        string mcpServerName,
        McpOAuthOptions options,
        CancellationToken cancellationToken = default)
    {
        var existingToken = await _tokenStore.GetTokenAsync(userId, mcpServerName, cancellationToken);
        if (existingToken?.RefreshToken is null)
        {
            _logger.LogWarning(
                "No refresh token available for MCP server '{McpServerName}' for user '{UserId}'.",
                mcpServerName, userId);
            return null;
        }

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = existingToken.RefreshToken,
            ["client_id"] = options.ClientId!
        };

        if (!string.IsNullOrEmpty(options.ClientSecret))
        {
            formData["client_secret"] = options.ClientSecret;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "OAuth token refresh failed for MCP server '{McpServerName}': {StatusCode}",
                    mcpServerName, response.StatusCode);

                // If refresh fails, remove the stored token so user re-authorizes
                await _tokenStore.RemoveTokenAsync(userId, mcpServerName, cancellationToken);
                return null;
            }

            var tokenResponse = JsonDocument.Parse(content);
            var token = ParseTokenResponse(tokenResponse.RootElement, options.Scopes);

            await _tokenStore.SetTokenAsync(userId, mcpServerName, token, cancellationToken);

            _logger.LogInformation(
                "OAuth token refreshed for MCP server '{McpServerName}' for user '{UserId}'.",
                mcpServerName, userId);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "OAuth token refresh failed for MCP server '{McpServerName}'.",
                mcpServerName);
            return null;
        }
    }

    private static OAuthToken ParseTokenResponse(JsonElement root, string[]? requestedScopes)
    {
        var accessToken = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var tokenType = root.TryGetProperty("token_type", out var tt) ? tt.GetString() ?? "Bearer" : "Bearer";

        DateTimeOffset? expiresAt = null;
        if (root.TryGetProperty("expires_in", out var expiresIn) && expiresIn.TryGetInt32(out var seconds))
        {
            expiresAt = DateTimeOffset.UtcNow.AddSeconds(seconds);
        }

        IReadOnlyList<string> scopes = requestedScopes ?? [];
        if (root.TryGetProperty("scope", out var scopeProp))
        {
            var scopeString = scopeProp.GetString();
            if (!string.IsNullOrEmpty(scopeString))
            {
                scopes = scopeString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        return new OAuthToken
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = tokenType,
            ExpiresAt = expiresAt,
            Scopes = scopes
        };
    }
}
