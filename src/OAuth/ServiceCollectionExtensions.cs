using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.Agents.OAuth;

/// <summary>
/// Extension methods for registering OAuth services.
/// </summary>
public static class ServiceCollectionExtensions
{
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