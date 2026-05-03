using AI.Agents.Abstractions;
using AI.Agents.Microsoft;
using AI.Agents.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.Agents;

/// <summary>
/// Extension methods for registering the configured AI client provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IChatClient"/> from the first registered provider candidate
    /// with valid settings in the dependency injection container.
    /// </summary>
    public static IServiceCollection AddAIClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenAIProvider("OpenAI");
        services.AddCodexProvider("Codex");
        services.AddFoundryAIProvider("Foundry");
        services.AddAzureOpenAIProvider("AzureOpenAI");

        services.TryAddSingleton<IChatClient>(sp =>
        {
            var provider = sp.GetServices<IClientProvider>().FirstOrDefault(x => x.CanCreateChatClient(sp));
            if (provider is not null) return provider.CreateChatClient(sp);

            throw new InvalidOperationException("AI provider not configured. Register an AI client provider with valid options before resolving IChatClient.");
        });

        return services;
    }
}
