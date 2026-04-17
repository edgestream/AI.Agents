using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Extension methods for registering Microsoft-specific authentication services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Microsoft Graph profile service for enriching user identity with Graph data.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGraphProfileService(this IServiceCollection services)
    {
        services.AddHttpClient("MicrosoftGraph", client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.TryAddSingleton<IGraphProfileService, GraphProfileService>();
        return services;
    }
}

/// <summary>
/// Extension methods for configuring Microsoft-specific authentication middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the user context middleware to the pipeline. Should be called early in the pipeline,
    /// after authentication/authorization middleware if using ASP.NET Core auth.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserContextMiddleware>();
    }
}