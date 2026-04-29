using AI.Agents.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Agents.Microsoft.Authentication;

/// <summary>
/// Extension methods for registering Microsoft-specific authentication services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services for retrieving user profile information from Microsoft Graph, including a user context accessor that integrates with the current HTTP context.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddGraphUserProfileService(this IServiceCollection services)
    {
        services.AddHttpClient("MicrosoftGraph", client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddHttpContextAccessor();
        services.AddSingleton<IUserContextAccessor, HttpUserContextAccessor>();
        services.AddSingleton<IUserProfileService, GraphUserProfileService>();
        return services;
    }

    /// <summary>
    /// Adds the custom Entra Easy Auth authentication scheme.
    /// </summary>
    public static IServiceCollection AddEntraAuth(this IServiceCollection services)
    {
        services
            .AddAuthentication(EntraAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, EntraAuthenticationHandler>(
                EntraAuthenticationDefaults.AuthenticationScheme,
                _ => { });

        return services;
    }
}
