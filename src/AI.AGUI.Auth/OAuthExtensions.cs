using AI.MCP.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.AGUI.Auth;

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
        return services;
    }

    /// <summary>
    /// Maps OAuth callback endpoint for handling external provider redirects.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern (default: "/oauth/callback").</param>
    /// <param name="getMcpOAuthOptions">
    /// Function to retrieve <see cref="McpOAuthOptions"/> for a given MCP server name.
    /// </param>
    /// <returns>The route handler builder.</returns>
    public static IEndpointRouteBuilder MapOAuthCallback(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/oauth/callback",
        Func<string, McpOAuthOptions?>? getMcpOAuthOptions = null)
    {
        endpoints.MapGet(pattern, async (
            HttpContext context,
            IOAuthService oauthService,
            IUserContextAccessor userContextAccessor,
            string? code,
            string? state,
            string? error,
            string? error_description) =>
        {
            // Handle OAuth errors
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

            // Note: The OAuth service will consume the state and retrieve the original request info
            // We need to look up the MCP server options from the state
            // For now, we return a response that the frontend can use

            // The callback URL must match what was used in the authorization request
            var callbackUrl = $"{context.Request.Scheme}://{context.Request.Host}{pattern}";

            // Placeholder - in real implementation, we need to look up OAuth options from state
            // This is handled by passing the options through the state or a configuration lookup
            return Results.Ok(new
            {
                message = "OAuth callback received. Use the token exchange endpoint to complete authorization.",
                code,
                state,
                callback_url = callbackUrl
            });
        });

        return endpoints;
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
