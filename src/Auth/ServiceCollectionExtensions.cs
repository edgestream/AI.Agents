using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.Agents.Auth;

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
