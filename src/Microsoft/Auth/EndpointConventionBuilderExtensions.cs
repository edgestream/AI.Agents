using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for adding authentication filters to endpoint conventions.
/// </summary>
public static class EndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds an endpoint filter that checks for an authenticated user context. If the user is not authenticated, it returns an Unauthorized result; otherwise, it proceeds to the next filter or endpoint handler.
    /// </summary>
    /// <param name="builder">The endpoint convention builder to add the filter to.</param>
    /// <returns>The updated endpoint convention builder.</returns>
    public static IEndpointConventionBuilder AddAuthenticatedFilter(this IEndpointConventionBuilder builder)
    {
        builder.AddEndpointFilter(async (invocationContext, next) =>
        {
            var userContext = invocationContext.HttpContext.Items[typeof(IUserContext)] as IUserContext;
            if (userContext is not null && userContext.IsAuthenticated) return await next(invocationContext);
            else return Results.Json(
                new { error = "Authentication required." },
                statusCode: StatusCodes.Status401Unauthorized
            );
        });
        return builder;
    }
}