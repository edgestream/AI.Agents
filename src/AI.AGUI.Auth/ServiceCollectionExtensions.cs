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

    /// <summary>
    /// Registers the MCP authorization service for checking OAuth requirements.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpAuthorizationService(this IServiceCollection services)
    {
        services.TryAddSingleton<IMcpAuthorizationService, McpAuthorizationService>();
        return services;
    }
}
