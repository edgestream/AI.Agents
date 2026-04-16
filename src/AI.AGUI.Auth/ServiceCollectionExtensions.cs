using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.AGUI.Auth;

/// <summary>
/// Extension methods for registering authentication services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the user context services for extracting and accessing user identity.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IUserContextAccessor, UserContextAccessor>();
        return services;
    }

    /// <summary>
    /// Registers the OAuth token store for storing per-user tokens for external MCP tools.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOAuthTokenStore(
        this IServiceCollection services,
        Action<OAuthTokenStoreOptions>? configure = null)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<IOAuthTokenStore, InMemoryOAuthTokenStore>();

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<OAuthTokenStoreOptions>();
        }

        return services;
    }
}

/// <summary>
/// Extension methods for configuring authentication middleware.
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
