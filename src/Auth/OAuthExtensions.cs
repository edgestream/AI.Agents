using AI.Agents.Abstractions;
using AI.Agents.MCP;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AI.Agents.Auth;

/// <summary>
/// Extension methods for registering OAuth service and endpoints.
/// </summary>
public static class OAuthExtensions
{
    /// <summary>
    /// Registers the OAuth service and related dependencies for MCP OAuth flows.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpOAuth(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<IOAuthService, OAuthService>();
        services.TryAddSingleton<IOAuthStateStore, InMemoryOAuthStateStore>();
        services.AddOAuthTokenStore();
        services.AddMcpAuthorizationService();
        return services;
    }

    /// <summary>
    /// Maps OAuth endpoints for initiating and completing authorization flows.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="basePath">The base path for OAuth endpoints (default: "/oauth").</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapOAuthEndpoints(
        this IEndpointRouteBuilder endpoints,
        string basePath = "/oauth")
    {
        var group = endpoints.MapGroup(basePath);

        // GET /oauth/status/{mcpServerName} - Check if user has valid token for MCP server
        group.MapGet("/status/{mcpServerName}", async (
            HttpContext context,
            IOAuthTokenStore tokenStore,
            IUserContextAccessor userContextAccessor,
            string mcpServerName) =>
        {
            var userContext = userContextAccessor.UserContext;
            if (!userContext.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var hasToken = await tokenStore.HasValidTokenAsync(userContext.UserId, mcpServerName);
            return Results.Ok(new
            {
                mcpServerName,
                authorized = hasToken
            });
        });

        // GET /oauth/authorize/{mcpServerName} - Initiate OAuth authorization flow
        group.MapGet("/authorize/{mcpServerName}", async (
            HttpContext context,
            IOAuthService oauthService,
            IUserContextAccessor userContextAccessor,
            IOptions<MCPClientOptions> mcpOptions,
            string mcpServerName,
            string? redirectUri) =>
        {
            var userContext = userContextAccessor.UserContext;
            if (!userContext.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            // Look up the MCP server configuration
            if (!mcpOptions.Value.Servers.TryGetValue(mcpServerName, out var serverOptions))
            {
                return Results.NotFound(new { error = $"MCP server '{mcpServerName}' not found." });
            }

            if (serverOptions.Auth is null || !serverOptions.Auth.IsConfigured)
            {
                return Results.BadRequest(new { error = $"MCP server '{mcpServerName}' does not have OAuth configured." });
            }

            // Generate the callback URL
            var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}{basePath}/callback";

            var result = await oauthService.GenerateAuthorizationUrlAsync(
                userContext.UserId,
                mcpServerName,
                serverOptions.Auth,
                callbackUrl);

            return Results.Ok(new
            {
                authorizationUrl = result.AuthorizationUrl,
                stateId = result.StateId,
                mcpServerName,
                redirectUri // Pass through for frontend to use after callback
            });
        });

        // GET /oauth/callback - Handle OAuth provider callback
        group.MapGet("/callback", async (
            HttpContext context,
            IOAuthService oauthService,
            IOAuthStateStore stateStore,
            IOptions<MCPClientOptions> mcpOptions,
            string? code,
            string? state,
            string? error,
            string? error_description) =>
        {
            // Handle OAuth errors from provider
            if (!string.IsNullOrEmpty(error))
            {
                return Results.BadRequest(new
                {
                    error,
                    error_description
                });
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return Results.BadRequest(new
                {
                    error = "invalid_request",
                    error_description = "Missing code or state parameter."
                });
            }

            // Peek at the state to get the MCP server name (without consuming it)
            var oauthState = await stateStore.ConsumeStateAsync(state);
            if (oauthState is null)
            {
                return Results.BadRequest(new
                {
                    error = "invalid_state",
                    error_description = "Invalid or expired state parameter."
                });
            }

            // Look up the MCP server configuration
            if (!mcpOptions.Value.Servers.TryGetValue(oauthState.McpServerName, out var serverOptions) ||
                serverOptions.Auth is null)
            {
                return Results.BadRequest(new
                {
                    error = "invalid_request",
                    error_description = $"MCP server '{oauthState.McpServerName}' configuration not found."
                });
            }

            // Re-store the state so ExchangeCodeAsync can consume it
            await stateStore.StoreStateAsync(oauthState);

            // The callback URL must match what was used in the authorization request
            var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}{basePath}/callback";

            var result = await oauthService.ExchangeCodeAsync(
                state,
                code,
                serverOptions.Auth,
                callbackUrl);

            if (!result.Success)
            {
                return Results.BadRequest(new
                {
                    error = "token_exchange_failed",
                    error_description = result.Error
                });
            }

            // Return HTML that closes the popup and notifies the parent window
            var html = $@"
<!DOCTYPE html>
<html>
<head><title>Authorization Complete</title></head>
<body>
<script>
  if (window.opener) {{
    window.opener.postMessage({{
      type: 'oauth_complete',
      mcpServerName: '{result.McpServerName}',
      success: true
    }}, '*');
    window.close();
  }} else {{
    document.body.innerHTML = '<h1>Authorization Complete</h1><p>You can close this window.</p>';
  }}
</script>
<noscript>
  <h1>Authorization Complete</h1>
  <p>You may now close this window.</p>
</noscript>
</body>
</html>";

            return Results.Content(html, "text/html");
        });

        // DELETE /oauth/revoke/{mcpServerName} - Revoke user's token for MCP server
        group.MapDelete("/revoke/{mcpServerName}", async (
            HttpContext context,
            IOAuthTokenStore tokenStore,
            IUserContextAccessor userContextAccessor,
            string mcpServerName) =>
        {
            var userContext = userContextAccessor.UserContext;
            if (!userContext.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            await tokenStore.RemoveTokenAsync(userContext.UserId, mcpServerName);
            return Results.Ok(new
            {
                mcpServerName,
                revoked = true
            });
        });

        return endpoints;
    }
}
