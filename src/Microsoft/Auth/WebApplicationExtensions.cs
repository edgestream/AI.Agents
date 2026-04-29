using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace AI.Agents.Microsoft.Auth;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Adds ASP.NET Core authentication and authorization middleware configured for Entra Easy Auth headers.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the middleware to.</param>
    public static void UseEntraAuth(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
    
    /// <summary>
    /// Maps a GET endpoint that returns the current user's profile information as JSON.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the endpoint to.</param>
    /// <param name="endpoint">The endpoint path.</param>
    public static WebApplication MapGraphUserProfileEndpoint(this WebApplication app, string endpoint)
    {
        app.MapGet(endpoint, (IUserContextAccessor userContextAccessor) =>
        {
            var userContext = userContextAccessor.UserContext;
            if (!userContext.IsAuthenticated)
            {
                return Results.Json(new
                {
                    authenticated = false
                });
            }
            return Results.Json(new
            {
                authenticated = true,
                userId = userContext.UserId,
                displayName = userContext.DisplayName,
                email = userContext.Email,
                picture = userContext.Picture
            });
        });
        return app;
    }
}
