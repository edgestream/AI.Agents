using AI.Agents.MCP;
using Microsoft.Extensions.Options;

namespace AI.Agents.OAuth;

/// <summary>
/// Service for checking MCP OAuth authorization requirements and generating consent prompts.
/// </summary>
public interface IMCPAuthorizationService
{
    /// <summary>
    /// Checks if an MCP server requires OAuth authorization and the user has a valid token.
    /// </summary>
    /// <param name="mcpServerName">The MCP server name.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user is authorized to use the MCP server, false if authorization is needed.</returns>
    Task<bool> IsAuthorizedAsync(string mcpServerName, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the OAuth configuration for an MCP server, if any.
    /// </summary>
    /// <param name="mcpServerName">The MCP server name.</param>
    /// <returns>The OAuth options, or null if the server doesn't require OAuth.</returns>
    MCPOAuthOptions? GetOAuthOptions(string mcpServerName);

    /// <summary>
    /// Generates a consent required response for an MCP server.
    /// </summary>
    /// <param name="mcpServerName">The MCP server name.</param>
    /// <param name="baseUrl">The base URL for generating the authorization URL.</param>
    /// <returns>The consent required response, or null if the server doesn't require OAuth.</returns>
    OAuthConsentRequired? GenerateConsentRequired(string mcpServerName, string baseUrl);

    /// <summary>
    /// Gets the access token for an MCP server if the user is authorized.
    /// </summary>
    /// <param name="mcpServerName">The MCP server name.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token, or null if not authorized.</returns>
    Task<string?> GetAccessTokenAsync(string mcpServerName, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IMCPAuthorizationService"/>.
/// </summary>
public sealed class MCPAuthorizationService : IMCPAuthorizationService
{
    private readonly IOAuthTokenStore _tokenStore;
    private readonly IOptions<MCPClientOptions> _mcpOptions;

    public MCPAuthorizationService(
        IOAuthTokenStore tokenStore,
        IOptions<MCPClientOptions> mcpOptions)
    {
        _tokenStore = tokenStore;
        _mcpOptions = mcpOptions;
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthorizedAsync(string mcpServerName, string userId, CancellationToken cancellationToken = default)
    {
        var oauthOptions = GetOAuthOptions(mcpServerName);
        if (oauthOptions is null)
        {
            return true;
        }

        return await _tokenStore.HasValidTokenAsync(userId, mcpServerName, cancellationToken);
    }

    /// <inheritdoc />
    public MCPOAuthOptions? GetOAuthOptions(string mcpServerName)
    {
        if (!_mcpOptions.Value.Servers.TryGetValue(mcpServerName, out var serverOptions))
        {
            return null;
        }

        return serverOptions.Auth is { IsConfigured: true } ? serverOptions.Auth : null;
    }

    /// <inheritdoc />
    public OAuthConsentRequired? GenerateConsentRequired(string mcpServerName, string baseUrl)
    {
        var oauthOptions = GetOAuthOptions(mcpServerName);
        if (oauthOptions is null)
        {
            return null;
        }

        var authorizeUrl = $"{baseUrl.TrimEnd('/')}/oauth/authorize/{mcpServerName}";

        return new OAuthConsentRequired
        {
            McpServerName = mcpServerName,
            DisplayName = GetDisplayName(mcpServerName),
            Scopes = oauthOptions.Scopes ?? [],
            AuthorizeUrl = authorizeUrl,
            Message = $"To use {GetDisplayName(mcpServerName)} tools, you need to authorize access to your account."
        };
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string mcpServerName, string userId, CancellationToken cancellationToken = default)
    {
        var token = await _tokenStore.GetTokenAsync(userId, mcpServerName, cancellationToken);
        if (token is null || token.IsExpired)
        {
            return null;
        }

        return token.AccessToken;
    }

    private string GetDisplayName(string mcpServerName)
    {
        return mcpServerName switch
        {
            "github" => "GitHub",
            "ms365" => "Microsoft 365",
            "graph" => "Microsoft Graph",
            _ => mcpServerName
        };
    }
}