using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AI.Agents.Microsoft.Auth;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Adds the EntraAuthMiddleware to the application's request pipeline, which authenticates incoming requests using Entra ID and populates the IUserContext for downstream processing.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the middleware to.</param>
    public static void UseEntraAuthMiddleware(this WebApplication app)
    {
        app.UseMiddleware<EntraAuthMiddleware>();
    }

    /// <summary>
    /// Maps a GET endpoint that returns the current user's profile information as JSON.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the endpoint to.</param>
    /// <param name="endpoint">The endpoint path.</param>
    public static WebApplication MapGraphProfileEndpoint(this WebApplication app, string endpoint)
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
